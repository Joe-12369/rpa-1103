using II.RPA.Core.Abstract;
using II.RPA.Core.Common;
using II.RPA.Core.Log;
using II.RPA.Core.Runtime;
using II.RPA.IPC;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.Core.Provider.ZBrainProxyProvider
{
    public class ZBrainProxyProvider : IDisposable, IZBrainChildProxyLauncher
    {
        private const int MaxBindRetryAttempts = 5;
        private const int ChildProxyPort = 6442;
        private const string BootName = "ZBrainProxy";

        private readonly ILogger m_Logger;
        private readonly IEndPointProvider m_EndPointProvider;
        private readonly IAdbStartupSignal m_StartupSignal;
        private readonly RpaClientKind m_ClientKind;
        private readonly Func<int, bool, IDisposable> m_ServerFactory;
        private readonly Action<int> m_PortPublisher;
        private readonly object m_LifecycleGate = new object();
        private readonly ProxyLifecycle m_RootProxy = new ProxyLifecycle();
        private readonly ProxyLifecycle m_ChildProxy = new ProxyLifecycle();
        private bool m_Disposed;

        public ZBrainProxyProvider(
            ILogger aLogger,
            IEndPointProvider anEndPointProvider,
            IAdbStartupSignal startupSignal,
            IRpaRuntimeContext runtimeContext)
        {
            m_Logger = aLogger ?? throw new ArgumentNullException(nameof(aLogger));
            m_EndPointProvider = anEndPointProvider ??
                throw new ArgumentNullException(nameof(anEndPointProvider));
            m_StartupSignal = startupSignal ??
                throw new ArgumentNullException(nameof(startupSignal));

            var context = runtimeContext ??
                throw new ArgumentNullException(nameof(runtimeContext));
            m_ClientKind = context.ClientKind;
            m_ServerFactory = CreateServer;
            m_PortPublisher = PublishCVPort;
        }

        internal ZBrainProxyProvider(
            ILogger aLogger,
            IEndPointProvider anEndPointProvider,
            IAdbStartupSignal startupSignal,
            IRpaRuntimeContext runtimeContext,
            Func<int, bool, IDisposable> serverFactory,
            Action<int> portPublisher)
        {
            m_Logger = aLogger ?? throw new ArgumentNullException(nameof(aLogger));
            m_EndPointProvider = anEndPointProvider ??
                throw new ArgumentNullException(nameof(anEndPointProvider));
            m_StartupSignal = startupSignal ??
                throw new ArgumentNullException(nameof(startupSignal));

            var context = runtimeContext ??
                throw new ArgumentNullException(nameof(runtimeContext));
            m_ClientKind = context.ClientKind;
            m_ServerFactory = serverFactory ??
                throw new ArgumentNullException(nameof(serverFactory));
            m_PortPublisher = portPublisher ??
                throw new ArgumentNullException(nameof(portPublisher));
        }

        public void Start()
        {
            ThrowIfDisposed();
            if (m_ClientKind == RpaClientKind.Desktop)
            {
                return;
            }

            StartProxy(m_RootProxy, childSession: false);
        }

        public void StartZdesktopProxy()
        {
            ThrowIfDisposed();

            // Preserve the existing replacement semantics: a new child session always
            // supersedes the previous generation, including one still waiting for ADB.
            StopProxy(m_ChildProxy);
            StartProxy(m_ChildProxy, childSession: true);
        }

        public void StopZdesktopProxy()
        {
            m_Logger.LogInfo("停止子会话ZBrain代理");
            StopProxy(m_ChildProxy);
        }

        public void StartChildProxy() => StartZdesktopProxy();

        public void StopChildProxy() => StopZdesktopProxy();

        public void Stop()
        {
            m_Logger.LogInfo("停止ZBrain代理");
            StopProxy(m_RootProxy);
            StopProxy(m_ChildProxy);
        }

        public void Restart()
        {
            Stop();
            Start();
        }

        public void Dispose()
        {
            lock (m_LifecycleGate)
            {
                if (m_Disposed)
                {
                    return;
                }

                m_Disposed = true;
            }

            StopProxy(m_RootProxy);
            StopProxy(m_ChildProxy);
            GC.SuppressFinalize(this);
        }

        private void StartProxy(ProxyLifecycle lifecycle, bool childSession)
        {
            lock (lifecycle.OperationGate)
            {
                lock (m_LifecycleGate)
                {
                    ThrowIfDisposedLocked();
                    if (lifecycle.Server != null || !lifecycle.StartTask.IsCompleted)
                    {
                        return;
                    }

                    lifecycle.Cancellation?.Dispose();
                    var cancellation = new CancellationTokenSource();
                    lifecycle.Cancellation = cancellation;
                    lifecycle.StartTask = Task.Run(
                        () => StartProxyAsync(lifecycle, cancellation, childSession));
                }
            }
        }

        private async Task StartProxyAsync(
            ProxyLifecycle lifecycle,
            CancellationTokenSource cancellation,
            bool childSession)
        {
            try
            {
                await m_StartupSignal
                    .WaitUntilReadyAsync(cancellation.Token)
                    .ConfigureAwait(false);
                cancellation.Token.ThrowIfCancellationRequested();

                if (childSession)
                {
                    StartChildProxy(lifecycle, cancellation);
                }
                else
                {
                    StartRootProxy(lifecycle, cancellation);
                }
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                // Stop invalidates this generation before cancellation. Cancellation is
                // therefore normal lifecycle completion, not an unobserved task failure.
            }
            catch (Exception ex)
            {
                var description = childSession ? "子会话ZBrain代理" : "ZBrain代理";
                m_Logger.LogError($"{description}启动失败", ex);
            }
        }

        private void StartRootProxy(
            ProxyLifecycle lifecycle,
            CancellationTokenSource cancellation)
        {
            var port = Global.CVPort;
            for (var attempt = 1; attempt <= MaxBindRetryAttempts; attempt++)
            {
                cancellation.Token.ThrowIfCancellationRequested();
                var baseAddress = $"http://127.0.0.1:{port}/";
                IDisposable candidate;
                try
                {
                    m_Logger.LogInfo(
                        $"ZBrain代理[{baseAddress}]...(attempt {attempt}/{MaxBindRetryAttempts})");
                    candidate = m_ServerFactory(port, false);
                    if (candidate == null)
                    {
                        throw new InvalidOperationException("ZBrain代理工厂返回了空实例");
                    }
                }
                catch (Exception ex)
                {
                    if (cancellation.IsCancellationRequested)
                    {
                        return;
                    }

                    m_Logger.LogWarning(
                        $"ZBrain代理[{baseAddress}]启动失败(attempt {attempt}/{MaxBindRetryAttempts})",
                        ex);
                    if (attempt >= MaxBindRetryAttempts)
                    {
                        m_Logger.LogError(
                            $"ZBrain代理在 {MaxBindRetryAttempts} 次尝试后仍无法启动");
                        return;
                    }

                    port = m_EndPointProvider.GetAvailablePort(port, BootName);
                    m_Logger.LogInfo($"ZBrain代理尝试新端口:{port}");
                    continue;
                }

                if (!TryPublishServer(lifecycle, cancellation, candidate))
                {
                    DisposeServer(candidate);
                    return;
                }

                PublishPortSafely(port);
                m_Logger.LogInfo($"ZBrain代理[{baseAddress}]启动成功");
                return;
            }
        }

        private void StartChildProxy(
            ProxyLifecycle lifecycle,
            CancellationTokenSource cancellation)
        {
            var baseAddress = $"http://127.0.0.1:{ChildProxyPort}/";
            m_Logger.LogInfo($"ZBrain代理[{baseAddress}]...");
            var candidate = m_ServerFactory(ChildProxyPort, true);
            if (candidate == null)
            {
                throw new InvalidOperationException("子会话ZBrain代理工厂返回了空实例");
            }

            if (!TryPublishServer(lifecycle, cancellation, candidate))
            {
                DisposeServer(candidate);
                return;
            }

            m_Logger.LogInfo($"ZBrain代理[{baseAddress}]启动成功");
        }

        private bool TryPublishServer(
            ProxyLifecycle lifecycle,
            CancellationTokenSource cancellation,
            IDisposable candidate)
        {
            lock (m_LifecycleGate)
            {
                if (m_Disposed ||
                    cancellation.IsCancellationRequested ||
                    !ReferenceEquals(lifecycle.Cancellation, cancellation))
                {
                    return false;
                }

                lifecycle.Server = candidate;
                return true;
            }
        }

        private void StopProxy(ProxyLifecycle lifecycle)
        {
            lock (lifecycle.OperationGate)
            {
                CancellationTokenSource cancellation;
                Task startTask;
                IDisposable server;
                lock (m_LifecycleGate)
                {
                    // Clear the generation while holding the gate. A task that finishes after
                    // this point can only dispose its candidate; it can no longer publish it.
                    cancellation = lifecycle.Cancellation;
                    startTask = lifecycle.StartTask;
                    server = lifecycle.Server;
                    lifecycle.Cancellation = null;
                    lifecycle.StartTask = Task.CompletedTask;
                    lifecycle.Server = null;
                }

                if (cancellation != null)
                {
                    try
                    {
                        cancellation.Cancel();
                    }
                    catch (Exception ex)
                    {
                        m_Logger.LogWarning("取消ZBrain代理启动任务失败", ex);
                    }
                }

                try
                {
                    startTask?.GetAwaiter().GetResult();
                }
                catch (OperationCanceledException)
                {
                    // A custom startup signal may surface cancellation directly.
                }
                catch (Exception ex)
                {
                    m_Logger.LogError("等待ZBrain代理启动任务退出失败", ex);
                }
                finally
                {
                    cancellation?.Dispose();
                }

                DisposeServer(server);
            }
        }

        private IDisposable CreateServer(int port, bool childSession)
        {
            return childSession
                ? new CoreTalkerKestrelOwinServer(
                    port,
                    app => new ChildStartup().Configuration(app))
                : new CoreTalkerKestrelOwinServer(
                    port,
                    app => new Startup().Configuration(app));
        }

        private void PublishPortSafely(int port)
        {
            try
            {
                m_PortPublisher(port);
            }
            catch (Exception ex)
            {
                // The listener is already active. A persistence failure must not turn a
                // successfully published server into a false startup failure.
                m_Logger.LogWarning($"保存ZBrain代理端口 {port} 失败", ex);
            }
        }

        private void PublishCVPort(int port)
        {
            Global.CVPort = port;
            SaveCVPortToFile(port);
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
                m_Logger.LogError("停止ZBrain代理服务失败", ex);
            }
        }

        private void ThrowIfDisposed()
        {
            lock (m_LifecycleGate)
            {
                ThrowIfDisposedLocked();
            }
        }

        private void ThrowIfDisposedLocked()
        {
            if (m_Disposed)
            {
                throw new ObjectDisposedException(nameof(ZBrainProxyProvider));
            }
        }

        private void SaveCVPortToFile(int port)
        {
            var cvPortFolderPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Z-Core");
            var cvPortFilePath = Path.Combine(cvPortFolderPath, "cv_port.json");
            if (!Directory.Exists(cvPortFolderPath))
            {
                Directory.CreateDirectory(cvPortFolderPath);
            }

            CVPortInfo cvPortInfo;
            try
            {
                cvPortInfo = JsonConvert.DeserializeObject<CVPortInfo>(
                    File.ReadAllText(cvPortFilePath)) ?? new CVPortInfo();
            }
            catch
            {
                cvPortInfo = new CVPortInfo();
            }

            if (m_ClientKind == RpaClientKind.Bot)
            {
                cvPortInfo.ZBot = port;
            }
            else if (m_ClientKind == RpaClientKind.Desktop)
            {
                cvPortInfo.ZDeskTop = port;
            }
            else if (m_ClientKind == RpaClientKind.Admin)
            {
                cvPortInfo.ZAdmin = port;
            }
            else
            {
                cvPortInfo.ZFactory = port;
            }

            try
            {
                File.WriteAllText(
                    cvPortFilePath,
                    JsonConvert.SerializeObject(cvPortInfo, Formatting.Indented));
            }
            catch
            {
            }
        }

        private sealed class ProxyLifecycle
        {
            public readonly object OperationGate = new object();
            public CancellationTokenSource Cancellation;
            public Task StartTask = Task.CompletedTask;
            public IDisposable Server;
        }
    }

    public class CVPortInfo
    {
        [JsonProperty("ZBot")]
        public int ZBot { get; set; }

        [JsonProperty("ZFactory")]
        public int ZFactory { get; set; }

        [JsonProperty("ZDesktop")]
        public int ZDeskTop { get; set; }

        [JsonProperty("Admin")]
        public int ZAdmin { get; set; }
    }
}
