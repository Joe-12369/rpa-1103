using II.RPA.Logging.Definitions;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;

namespace II.RPA.Logging.Configurations
{
    public class AppConfigWatcher : IDisposable
    {
        private readonly FileSystemWatcher watcher;
        private readonly Timer timer;
        private readonly object sync = new object();
        private readonly ILogger logger = Logger.OfContext();
        private bool disposed;
        private int retryCount;

        public AppConfigWatcher(string fileName, Action<bool> action)
        {
            var fileInfo = new FileInfo(fileName);
            timer = new Timer(_ =>
            {
                lock (sync)
                {
                    if (disposed) return;
                    try
                    {
                        // Delay from the last event, so rapid edits apply the final contents.
                        // Read access is sufficient; do not exclusively lock a file shared by processes.
                        using (File.Open(fileName, FileMode.Open, FileAccess.Read,
                            FileShare.ReadWrite | FileShare.Delete)) { }
                        action(true);
                        retryCount = 0;
                        logger.LogInformation("日志配置已成功刷新，路径:{configPath}", fileName);
                    }
                    catch (IOException)
                    {
                        if (++retryCount <= 5) timer.Change(200, Timeout.Infinite);
                        else logger.LogWarning("日志配置刷新失败，保留当前配置，等待后续文件变更:{configPath}", fileName);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(EventIds.ErrorConfig, ex,
                            "应用程序检测到配置文件变更，处理注册的回调程序发生了异常");
                    }
                }
            }, null, Timeout.Infinite, Timeout.Infinite);
            watcher = new FileSystemWatcher(fileInfo.DirectoryName, fileInfo.Name)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size
            };
            watcher.Changed += Schedule;
            watcher.Created += Schedule;
            watcher.Renamed += Schedule;
            watcher.EnableRaisingEvents = true;
        }

        private void Schedule(object sender, FileSystemEventArgs args)
        {
            lock (sync)
            {
                if (!disposed)
                {
                    retryCount = 0;
                    timer.Change(100, Timeout.Infinite);
                }
            }
        }

        public void Dispose()
        {
            lock (sync)
            {
                if (disposed) return;
                disposed = true;
                watcher.Dispose();
                timer.Dispose();
            }
        }
    }
}
