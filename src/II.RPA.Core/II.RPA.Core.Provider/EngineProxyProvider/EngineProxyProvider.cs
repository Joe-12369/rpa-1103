using II.RPA.Core.Abstract;
using II.RPA.Core.Log;
using II.RPA.Core.Provider.ZBrainProxyProvider;
using II.RPA.Core.Runtime;
using II.RPA.Plugin.Core;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.Core.Provider.EngineProxyProvider
{
    public class EngineProxyProvider : IDisposable
    {
        private readonly ILogger m_Logger;
        private readonly IAdbStartupSignal m_StartupSignal;
        private readonly int m_Port;
        private readonly string m_SandboxPath;
        private readonly Func<int, IDisposable> m_ServerFactory;
        private readonly Action m_SandboxCleaner;
        private readonly TimeSpan m_SandboxInterval;
        private readonly object m_OperationGate = new object();
        private readonly object m_LifecycleGate = new object();
        private CancellationTokenSource m_Cancellation;
        private Task m_LifecycleTask = Task.CompletedTask;
        private IDisposable m_CurrentServer;
        private bool m_Disposed;

        public static int SandboxClearInterval = 60 * 60 * 24;
        public static int SandboxKeepDays = 6;

        public EngineProxyProvider(
            ILogger aLogger,
            IAdbStartupSignal startupSignal,
            IRpaRuntimeContext runtimeContext)
        {
            m_Logger = aLogger ?? throw new ArgumentNullException(nameof(aLogger));
            m_StartupSignal = startupSignal ??
                throw new ArgumentNullException(nameof(startupSignal));

            var context = runtimeContext ??
                throw new ArgumentNullException(nameof(runtimeContext));
            m_Port = GetEnginePort(context.ClientKind);
            m_SandboxPath = GetSandboxPath(context.ProducedClientId);
            m_ServerFactory = CreateServer;
            m_SandboxCleaner = CleanSandbox;
            m_SandboxInterval = GetSandboxInterval();
        }

        internal EngineProxyProvider(
            ILogger aLogger,
            IAdbStartupSignal startupSignal,
            IRpaRuntimeContext runtimeContext,
            Func<int, IDisposable> serverFactory,
            Action sandboxCleaner,
            TimeSpan sandboxInterval)
        {
            m_Logger = aLogger ?? throw new ArgumentNullException(nameof(aLogger));
            m_StartupSignal = startupSignal ??
                throw new ArgumentNullException(nameof(startupSignal));

            var context = runtimeContext ??
                throw new ArgumentNullException(nameof(runtimeContext));
            m_Port = GetEnginePort(context.ClientKind);
            m_SandboxPath = GetSandboxPath(context.ProducedClientId);
            m_ServerFactory = serverFactory ??
                throw new ArgumentNullException(nameof(serverFactory));
            m_SandboxCleaner = sandboxCleaner ??
                throw new ArgumentNullException(nameof(sandboxCleaner));
            m_SandboxInterval = sandboxInterval > TimeSpan.Zero
                ? sandboxInterval
                : throw new ArgumentOutOfRangeException(nameof(sandboxInterval));
        }

        public void Start()
        {
            lock (m_OperationGate)
            {
                StartCore();
            }
        }

        public void Stop()
        {
            m_Logger.LogInfo("停止rpa-engine代理");
            lock (m_OperationGate)
            {
                StopCore();
            }
        }

        public void Restart()
        {
            lock (m_OperationGate)
            {
                StopCore();
                StartCore();
            }
        }

        public void Dispose()
        {
            lock (m_OperationGate)
            {
                lock (m_LifecycleGate)
                {
                    if (m_Disposed)
                    {
                        return;
                    }

                    m_Disposed = true;
                }

                StopCore();
            }

            GC.SuppressFinalize(this);
        }

        private void StartCore()
        {
            lock (m_LifecycleGate)
            {
                ThrowIfDisposedLocked();
                if (m_Cancellation != null)
                {
                    return;
                }

                var cancellation = new CancellationTokenSource();
                m_Cancellation = cancellation;
                var serverTask = Task.Run(() => StartServerAsync(cancellation));
                var sandboxTask = Task.Run(
                    () => RunSandboxLoopAsync(cancellation.Token));
                m_LifecycleTask = Task.WhenAll(serverTask, sandboxTask);
            }
        }

        private async Task StartServerAsync(CancellationTokenSource cancellation)
        {
            try
            {
                await m_StartupSignal
                    .WaitUntilReadyAsync(cancellation.Token)
                    .ConfigureAwait(false);
                cancellation.Token.ThrowIfCancellationRequested();

                var candidate = m_ServerFactory(m_Port);
                if (candidate == null)
                {
                    throw new InvalidOperationException("rpa-engine代理工厂返回了空实例");
                }

                if (!TryPublishServer(cancellation, candidate))
                {
                    DisposeServer(candidate);
                    return;
                }

                m_Logger.LogInfo($"rpa-engine代理[{m_Port}]启动成功");
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                // Stop invalidated this lifecycle generation.
            }
            catch (Exception ex)
            {
                if (!cancellation.IsCancellationRequested)
                {
                    m_Logger.LogError($"rpa-engine代理[{m_Port}]启动失败", ex);
                }
            }
        }

        private async Task RunSandboxLoopAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        m_SandboxCleaner();
                    }
                    catch (Exception ex)
                    {
                        m_Logger.LogWarning("清理rpa-engine sandbox失败", ex);
                    }

                    await Task.Delay(m_SandboxInterval, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Normal lifecycle completion.
            }
        }

        private bool TryPublishServer(
            CancellationTokenSource cancellation,
            IDisposable candidate)
        {
            lock (m_LifecycleGate)
            {
                if (m_Disposed ||
                    cancellation.IsCancellationRequested ||
                    !ReferenceEquals(m_Cancellation, cancellation))
                {
                    return false;
                }

                m_CurrentServer = candidate;
                return true;
            }
        }

        private void StopCore()
        {
            CancellationTokenSource cancellation;
            Task lifecycleTask;
            IDisposable server;
            lock (m_LifecycleGate)
            {
                cancellation = m_Cancellation;
                lifecycleTask = m_LifecycleTask;
                server = m_CurrentServer;
                m_Cancellation = null;
                m_LifecycleTask = Task.CompletedTask;
                m_CurrentServer = null;
            }

            if (cancellation != null)
            {
                try
                {
                    cancellation.Cancel();
                }
                catch (Exception ex)
                {
                    m_Logger.LogWarning("取消rpa-engine代理任务失败", ex);
                }
            }

            try
            {
                lifecycleTask?.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                // A custom startup signal may surface cancellation directly.
            }
            catch (Exception ex)
            {
                m_Logger.LogError("等待rpa-engine代理任务退出失败", ex);
            }
            finally
            {
                cancellation?.Dispose();
            }

            DisposeServer(server);
        }

        private IDisposable CreateServer(int port)
        {
            return new CoreTalkerKestrelOwinServer(
                port,
                app => new Startup().Configuration(app));
        }

        private void DisposeServer(IDisposable server)
        {
            if (server == null)
            {
                return;
            }

            try
            {
                server.Dispose();
            }
            catch (Exception ex)
            {
                m_Logger.LogError("停止rpa-engine代理服务失败", ex);
            }
        }

        private void CleanSandbox()
        {
            if (!Directory.Exists(m_SandboxPath))
            {
                return;
            }

            var today = DateTime.Now.Date;
            Directory
                .GetDirectories(m_SandboxPath)
                .Select(path => new DirectoryInfo(path))
                .Select(directory =>
                {
                    DateTime date;
                    return DateTime.TryParse(directory.Name, out date)
                        ? new { Directory = directory, Date = date.Date }
                        : null;
                })
                .Where(item => item != null &&
                    (today - item.Date).TotalDays > SandboxKeepDays)
                .ToList()
                .ForEach(item => item.Directory.Delete(true));
        }

        private static int GetEnginePort(RpaClientKind clientKind)
        {
            return clientKind == RpaClientKind.Bot ? 6445 : 6444;
        }

        private static string GetSandboxPath(string producedClientId)
        {
            return Path.GetFullPath(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                producedClientId,
                "sandbox"));
        }

        private static TimeSpan GetSandboxInterval()
        {
            if (SandboxClearInterval <= 0)
            {
                throw new InvalidOperationException(
                    "SandboxClearInterval必须大于零秒");
            }

            return TimeSpan.FromSeconds(SandboxClearInterval);
        }

        private void ThrowIfDisposedLocked()
        {
            if (m_Disposed)
            {
                throw new ObjectDisposedException(nameof(EngineProxyProvider));
            }
        }
    }
}
