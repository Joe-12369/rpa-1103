using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace II.RPA.MobileProvider.Provider
{
    internal static class MobileSessionRecovery
    {
        private const int DeviceAutomationCleanupTimeoutMilliseconds = 5000;
        private const int DriverQuitTimeoutMilliseconds = 5000;
        private const int ReadOnlySessionRecoveryTimeoutMilliseconds = 10000;
        private readonly static ILogger _logger = Logger.OfContext();

        public static bool ShouldResetAfterCreateFailure(Exception exception)
        {
            if (exception == null) return false;
            string message = exception.ToString().ToLowerInvariant();
            return ContainsAny(message,
                "uiautomator2", "io.appium.uiautomator2.server", "instrumentation process",
                "could not proxy command to the remote server", "cannot proxy command to the remote server",
                "socket hang up", "invalid session id", "session deleted", "session does not exist",
                "connection reset", "econnreset", "forcibly closed by the remote host", "远程主机强迫关闭");
        }

        public static void ResetDetachedSession(string udid, MobileSession detachedSession, string reason)
        {
            ResetDetachedSessionAsync(udid, detachedSession, reason).GetAwaiter().GetResult();
        }

        public static async Task ResetDetachedSessionAsync(string udid, MobileSession detachedSession, string reason)
        {
            _logger.LogWarning($"开始重置手机自动化会话，原因[{reason}]，设备[{udid}]");
            await StopUiAutomator2AndForwardAsync(udid, detachedSession);
            await TryQuitDetachedSessionAsync(detachedSession, udid);
        }

        // 调用方持有每UDID会话锁。旧实例在清理完成前保留在缓存中，
        // 超时只结束本次等待，不允许新Session与后台Quit交错。
        public static async Task RecoverReadOnlySessionAsync(string udid, MobileSession session)
        {
            if (session.ReadOnlySessionRecovery == null
                || session.ReadOnlySessionRecovery.Value.IsFaulted)
            {
                session.ReadOnlySessionRecovery = new Lazy<Task>(() => CleanupReadOnlySessionAsync(udid, session));
            }
            Task recoveryTask = session.ReadOnlySessionRecovery.Value;
            if (await Task.WhenAny(recoveryTask, Task.Delay(ReadOnlySessionRecoveryTimeoutMilliseconds)) != recoveryTask
                && !recoveryTask.IsCompleted)
            {
                _ = ObserveQuitAsync(recoveryTask, udid);
                throw new TimeoutException($"只读超时会话清理超过[{ReadOnlySessionRecoveryTimeoutMilliseconds}ms]，本次不创建新会话、不读取ADB；后续请求继续确认同一清理任务，设备[{udid}]，SessionGeneration[{session.Generation}]");
            }
            await recoveryTask;
        }

        private static async Task CleanupReadOnlySessionAsync(string udid, MobileSession session)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            using (MobileAutomationStateStore.BeginConnectionRecovery(udid))
            {
                session.MarkRequiresRecovery("ReadOnlySessionRecovery");
                _logger.LogWarning($"开始清理只读超时会话，SessionGeneration[{session.Generation}]，设备[{udid}]");
                try
                {
                    await StopUiAutomator2AndForwardAsync(udid, session, failOnError: true);
                    await Task.Run(() => session.CloseDetachedForRecovery());
                    _logger.LogWarning($"只读超时会话清理结束，耗时[{stopwatch.ElapsedMilliseconds}ms]，SessionGeneration[{session.Generation}]，设备[{udid}]");
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(_logger, ex, $"只读超时会话清理失败，保留旧会话隔离，设备[{udid}]");
                    throw;
                }
            }
        }

        private static async Task StopUiAutomator2AndForwardAsync(string udid, MobileSession detachedSession, bool failOnError = false)
        {
            await TryExecuteAdbAsync(udid, $"-s {Quote(udid)} shell am force-stop io.appium.uiautomator2.server", "停止UiAutomator2 server", failOnError);
            await TryExecuteAdbAsync(udid, $"-s {Quote(udid)} shell am force-stop io.appium.uiautomator2.server.test", "停止UiAutomator2 test", failOnError);
            int systemPort = detachedSession?.SystemPort ?? 0;
            if (systemPort <= 0)
            {
                MobileSystemPortAllocator.TryGet(udid, out systemPort);
            }
            if (systemPort > 0)
            {
                if (failOnError)
                {
                    // Appium可能已经移除了旧转发。只删除仍存在且属于本设备的指定端口。
                    string forwards = await AdbDeviceHelper.ExecuteAdbCommandWithTimeoutAsync("forward --list", DeviceAutomationCleanupTimeoutMilliseconds);
                    bool exists = forwards.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(line => line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
                        .Any(parts => parts.Length >= 3 && parts[0] == udid && parts[1] == $"tcp:{systemPort}");
                    if (!exists) return;
                }
                await TryExecuteAdbAsync(udid, $"-s {Quote(udid)} forward --remove tcp:{systemPort}", $"删除ADB forward tcp:{systemPort}", failOnError);
            }
        }

        private static async Task TryQuitDetachedSessionAsync(MobileSession detachedSession, string udid)
        {
            if (detachedSession == null) return;
            Task quitTask = Task.Run(() => detachedSession.CloseDetached());
            Task completed = await Task.WhenAny(quitTask, Task.Delay(DriverQuitTimeoutMilliseconds));
            if (completed != quitTask)
            {
                _logger.LogWarning($"关闭故障Appium Session超过[{DriverQuitTimeoutMilliseconds}ms]，不再阻塞ADB降级，设备[{udid}]");
                _ = ObserveQuitAsync(quitTask, udid);
                return;
            }
            await ObserveQuitAsync(quitTask, udid);
        }

        private static async Task ObserveQuitAsync(Task quitTask, string udid)
        {
            try { await quitTask; }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(_logger, ex, $"后台关闭故障Appium Session失败，设备[{udid}]");
            }
        }

        private static async Task TryExecuteAdbAsync(string udid, string command, string action, bool failOnError = false)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                await AdbDeviceHelper.ExecuteAdbCommandWithTimeoutAsync(command, DeviceAutomationCleanupTimeoutMilliseconds);
                _logger.LogDebug($"设计期降级资源清理完成，步骤[{action}]，耗时[{stopwatch.ElapsedMilliseconds}ms]，设备[{udid}]");
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(_logger, ex, $"{action}失败，耗时[{stopwatch.ElapsedMilliseconds}ms]，严格清理[{failOnError}]，设备[{udid}]");
                if (failOnError) throw;
            }
        }


        private static string Quote(string value)
        {
            return "\"" + (value ?? string.Empty).Replace("\"", "\\\"") + "\"";
        }

        private static bool ContainsAny(string text, params string[] values)
        {
            foreach (string value in values) if (text.Contains(value)) return true;
            return false;
        }
    }
}
