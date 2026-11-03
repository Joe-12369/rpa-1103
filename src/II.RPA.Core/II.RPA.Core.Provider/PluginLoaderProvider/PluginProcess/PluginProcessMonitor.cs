using II.RPA.Core.Log;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.Core.Provider.PluginLoaderProvider.PluginProcess
{
    internal class PluginProcessMonitor
    {
        private PluginProcessFacade facade;
        private CancellationTokenSource tokenKeepLive;
        private Task task;
        private readonly ILogger m_Logger;

        public PluginProcessMonitor(PluginProcessFacade facade, ILogger aLogger)
        {
            this.facade = facade;
            m_Logger = aLogger;
            IsMonitorRunning = false;
        }
        public bool IsMonitorRunning
        {
            get;
            private set;
        }
        public void KeepLiveMonitor()
        {
            StopLiveMonitor();
            this.tokenKeepLive = new CancellationTokenSource();
            this.KeepLiveMonitor(tokenKeepLive);
        }

        public void StopLiveMonitor()
        {
            m_Logger.LogWarning($"KeepLiveMonitor end...");
            this.tokenKeepLive?.Cancel();
            IsMonitorRunning = false;
        }

        private void KeepLiveMonitor(CancellationTokenSource cts)
        {
            if (cts == null)
            {
                throw new NullReferenceException("CancellationTokenSource cannot be null!");
            }
            m_Logger.LogWarning($"KeepLiveMonitor run...");
            task = new Task((tokenObj) =>
            {
                Thread.Sleep(100);
                m_Logger.LogWarning($"CurrentPluginProcessId is {facade.CurrentPluginProcessId}");
                IsMonitorRunning = true;
                Process process = null;
                CancellationTokenSource tokenSrc = tokenObj as CancellationTokenSource;
                while (!tokenSrc.IsCancellationRequested)
                {
                    if (this.facade.CurrentPluginProcessId > 0)
                    {
                        try
                        {
                            tokenSrc.Token.ThrowIfCancellationRequested();
                            process = this.facade.GetProcessByPid(this.facade.CurrentPluginProcessId);
                        }
                        catch (Exception ex)
                        {
                            m_Logger.LogWarning($"未检测到可用的独立进程 {this.facade.CurrentPluginProcessId}:{ex}");
                        }
                        if (process == null || process.Id <= 0 || process.HasExited)
                        {
                            tokenSrc.Token.ThrowIfCancellationRequested();
                            this.facade.ClearProcessStatus();
                            try
                            {
                                this.facade.Start(this.facade.PluginProcessStartArg, false);
                            }
                            catch (Exception ex)
                            {
                                Microsoft.Extensions.Logging.LoggingExtensions.LogError(m_Logger, ex, "保活服务启动进程失败,continue");
                                continue;
                            }
                        }
                    }
                    else
                    {
                        m_Logger.LogWarning($"Current process is null.continue");
                    }
                    Thread.Sleep(200);
                }
                IsMonitorRunning = false;
            }, cts, cts.Token);

            task.Start();
        }
    }
}
