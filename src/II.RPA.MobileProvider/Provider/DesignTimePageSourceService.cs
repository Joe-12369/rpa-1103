using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Xml;

namespace II.RPA.MobileProvider.Provider
{
    internal sealed class DesignTimePageSourceService
    {
        private const int AppiumPageSourceTimeoutMilliseconds = 10000;
        private readonly static ILogger _logger = Logger.OfContext();
        private readonly AdbPageSourceProvider _adbProvider = new AdbPageSourceProvider();

        public Task<MobilePageSourceResult> GetPageSourceAsync(string udid)
        {
            MobileAutomationStateSnapshot state = MobileAutomationStateStore.GetSnapshot(udid);
            if (state.IsRuntimeOwned || state.RuntimeLeaseCount > 0 || state.IsConnectionRecovering)
            {
                _logger.LogDebug($"设计期PageSource在等待会话锁前暂缓，模式[{state.Mode}]，RuntimeOwned[{state.IsRuntimeOwned}]，RuntimeLease[{state.RuntimeLeaseCount}]，设备[{udid}]");
                return Task.FromException<MobilePageSourceResult>(
                    new DesignTimePageSourceDeferredException(udid, state.Mode, state.RuntimeLeaseCount, state.IsRuntimeOwned, state.IsConnectionRecovering));
            }
            if (state.DesignTimeRecoveryExhausted)
                return Task.FromException<MobilePageSourceResult>(CreateRecoveryExhaustedException(udid));
            return MobileSessionCache.WithSessionLockAsync(udid, () => GetPageSourceCoreAsync(udid));
        }

        private async Task<MobilePageSourceResult> GetPageSourceCoreAsync(string udid)
        {
            Stopwatch totalStopwatch = Stopwatch.StartNew();
            MobileAutomationStateSnapshot state = MobileAutomationStateStore.GetSnapshot(udid);
            if (state.IsRuntimeOwned || state.RuntimeLeaseCount > 0 || state.IsConnectionRecovering)
            {
                _logger.LogDebug($"设计期PageSource本次暂缓，不排队、不探测Appium，沿用DeviceManager已有快照，模式[{state.Mode}]，RuntimeOwned[{state.IsRuntimeOwned}]，RuntimeLease[{state.RuntimeLeaseCount}]，设备[{udid}]");
                throw new DesignTimePageSourceDeferredException(udid, state.Mode, state.RuntimeLeaseCount, state.IsRuntimeOwned, state.IsConnectionRecovering);
            }

            if (state.DesignTimeRecoveryExhausted)
                throw CreateRecoveryExhaustedException(udid);

            if (state.Mode == MobileAutomationMode.AdbDegraded)
            {
                MobilePageSourceResult adbResult = await GetAdbPageSourceAsync(udid, state.DegradeReason, state.SessionGeneration);
                _logger.LogDebug($"设计期PageSource请求结束，来源[{adbResult.Provider}]，总耗时[{totalStopwatch.ElapsedMilliseconds}ms]，SessionGeneration[{adbResult.SessionGeneration}]，设备[{udid}]");
                return adbResult;
            }

            // 降级意图必须先于GetValidSession处理，不能因旧Source仍在途而丢失恢复入口。
            if (state.Mode == MobileAutomationMode.AdbFallbackPending || state.Mode == MobileAutomationMode.ReleasingForAdbFallback)
            {
                if (!MobileSessionCache.TryGet(udid, out MobileSession pendingSession, out _)
                    || pendingSession.Generation != state.SessionGeneration)
                    throw DeferFallback(udid, state.SessionGeneration, "PendingSessionChanged");
                return await CompleteAdbFallbackAsync(udid, pendingSession, state.DegradeReason);
            }

            MobileSession session = MobileSessionCache.GetValidSession(udid, force: false, autoReConnect: true);
            if (session == null)
            {
                if (state.Mode == MobileAutomationMode.AdbDegraded)
                    return await GetAdbPageSourceAsync(udid, state.DegradeReason, state.SessionGeneration);
                throw new InvalidOperationException($"设计期PageSource未找到可用手机会话，设备[{udid}]");
            }

            try
            {
                string appiumSource = await GetAppiumPageSourceWithTimeoutAsync(session, udid);
                ValidateXml(appiumSource);
                MobileAutomationStateStore.MarkAppiumSourceSuccess(udid, session.Generation);
                _logger.LogDebug($"设计期PageSource请求结束，来源[Appium]，总耗时[{totalStopwatch.ElapsedMilliseconds}ms]，SessionGeneration[{session.Generation}]，设备[{udid}]");
                return BuildResult(appiumSource, MobilePageSourceProviderKind.Appium, session.Generation, false, string.Empty);
            }
            catch (Exception ex) when (CanFallback(ex))
            {
                string reason = GetFallbackReason(ex);
                _logger.LogWarning($"设计期Appium PageSource失败，准备切换ADB，原因[{reason}]，SessionGeneration[{session.Generation}]，设备[{udid}]");
                return await CompleteAdbFallbackAsync(udid, session, reason);
            }
        }

        private async Task<MobilePageSourceResult> CompleteAdbFallbackAsync(string udid, MobileSession session, string reason)
        {
            if (!MobileAutomationStateStore.TryBeginAdbFallback(udid, session.Generation, reason, out string deferredReason))
                throw DeferFallback(udid, session.Generation, deferredReason);

            // 与连接恢复复用同一个清理任务。只读请求可以被隔离，真实业务操作仍受租约保护。
            session.MarkRequiresRecovery(reason);
            try
            {
                await MobileSessionRecovery.RecoverReadOnlySessionAsync(udid, session);
            }
            catch (TimeoutException)
            {
                throw DeferFallback(udid, session.Generation, "SessionCleanupInFlight");
            }
            if (!MobileSessionCache.TryDetachExpected(udid, session, out _))
                throw DeferFallback(udid, session.Generation, "SessionInstanceChanged");
            return await GetAdbPageSourceAsync(udid, reason, session.Generation);
        }

        private static DesignTimePageSourceDeferredException DeferFallback(string udid, long expectedGeneration, string reason)
        {
            MobileAutomationStateSnapshot state = MobileAutomationStateStore.GetSnapshot(udid);
            _logger.LogWarning($"设计期ADB降级暂缓，原因[{reason}]，模式[{state.Mode}]，RuntimeOwned[{state.IsRuntimeOwned}]，RuntimeLease[{state.RuntimeLeaseCount}]，DesignTimeReadOnlyLease[{state.DesignTimeReadOnlyLeaseCount}]，ConnectionRecovering[{state.IsConnectionRecovering}]，请求Generation[{expectedGeneration}]，当前Generation[{state.SessionGeneration}]，设备[{udid}]");
            return new DesignTimePageSourceDeferredException(udid, state.Mode, state.RuntimeLeaseCount,
                state.IsRuntimeOwned, state.IsConnectionRecovering, reason);
        }

        private static async Task<string> GetAppiumPageSourceWithTimeoutAsync(MobileSession session, string udid)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            _logger.LogDebug($"开始获取Appium PageSource，SessionGeneration[{session.Generation}]，设备[{udid}]");
            Task<string> sourceTask = Task.Run(() => session.GetPageSource());
            Task completedTask = await Task.WhenAny(sourceTask, Task.Delay(AppiumPageSourceTimeoutMilliseconds));
            if (completedTask != sourceTask)
            {
                session.MarkOperationTimedOut("DesignTimePageSource", isReadOnly: true);
                _ = ObserveTimedOutSourceAsync(sourceTask, session);
                throw new TimeoutException($"Appium PageSource获取超过[{AppiumPageSourceTimeoutMilliseconds}ms]，设备[{udid}]");
            }
            string result = await sourceTask;
            stopwatch.Stop();
            _logger.LogDebug($"Appium PageSource获取结束，耗时[{stopwatch.ElapsedMilliseconds}ms]，XML长度[{result?.Length ?? 0}]，SessionGeneration[{session.Generation}]，设备[{udid}]");
            return result;
        }

        private static async Task ObserveTimedOutSourceAsync(Task<string> sourceTask, MobileSession session)
        {
            Exception completionException = null;
            try { await sourceTask; }
            catch (Exception ex) { completionException = ex; }
            finally { session.MarkTimedOutOperationCompleted("DesignTimePageSource", completionException, isReadOnly: true); }
        }

        private async Task<MobilePageSourceResult> GetAdbPageSourceAsync(string udid, string reason, long generation)
        {
            try
            {
                string xml = await _adbProvider.GetPageSourceAsync(udid);
                MobilePageSourceResult result = BuildResult(xml, MobilePageSourceProviderKind.AdbDump, generation, true, reason);
                MobileAutomationStateStore.MarkAdbSourceSuccess(udid, generation);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"ADB快照获取失败，不再重试ADB，转入有限Appium恢复，设备[{udid}]，原因[{ex.Message}]");
                if (!MobileAutomationStateStore.TryBeginDesignTimeRecovery(udid, generation))
                {
                    MobileAutomationStateSnapshot state = MobileAutomationStateStore.GetSnapshot(udid);
                    if (state.IsRuntimeOwned || state.RuntimeLeaseCount > 0 || state.IsConnectionRecovering)
                        throw new DesignTimePageSourceDeferredException(udid, state.Mode, state.RuntimeLeaseCount, state.IsRuntimeOwned, state.IsConnectionRecovering);
                    if (state.SessionGeneration == generation && state.DesignTimeRecoveryAttempted)
                    {
                        MobileAutomationStateStore.MarkDesignTimeRecoveryExhausted(udid, generation);
                        throw CreateRecoveryExhaustedException(udid, ex);
                    }
                    throw new InvalidOperationException($"当前会话状态已变化，跳过本次设计期恢复，设备[{udid}]", ex);
                }
                return await RecoverAppiumPageSourceAsync(udid);
            }
        }

        private async Task<MobilePageSourceResult> RecoverAppiumPageSourceAsync(string udid)
        {
            MobileSession session = null;
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                _logger.LogWarning($"ADB快照单次获取失败，开始本轮唯一一次Appium会话自动恢复，设备[{udid}]");
                session = MobileSessionCache.GetValidSession(udid, force: false, autoReConnect: true);
                if (session == null) throw new InvalidOperationException("未能创建可用手机会话");
                string xml = await GetAppiumPageSourceWithTimeoutAsync(session, udid);
                ValidateXml(xml);
                MobilePageSourceResult result = BuildResult(xml, MobilePageSourceProviderKind.Appium, session.Generation, false, string.Empty);
                MobileAutomationStateStore.MarkAppiumSourceSuccess(udid, session.Generation);
                _logger.LogInformation($"设计期有限恢复成功，耗时[{stopwatch.ElapsedMilliseconds}ms]，SessionGeneration[{session.Generation}]，设备[{udid}]");
                return result;
            }
            catch (Exception ex)
            {
                long generation = session?.Generation ?? MobileAutomationStateStore.GetSnapshot(udid).SessionGeneration;
                MobileAutomationStateStore.MarkDesignTimeRecoveryExhausted(udid, generation);
                if (session != null)
                {
                    // 保留缓存隔离，直到旧Quit确认结束，防止其晚到清理误伤新Session。
                    session.MarkRequiresRecovery("DesignTimeRecoveryExhausted");
                    await MobileSessionRecovery.RecoverReadOnlySessionAsync(udid, session);
                    MobileSessionCache.TryDetachExpected(udid, session, out _);
                }
                _logger.LogWarning($"设计期有限恢复失败，本轮停止自动获取结构，请重新连接手机，耗时[{stopwatch.ElapsedMilliseconds}ms]，设备[{udid}]，原因[{ex.Message}]");
                throw CreateRecoveryExhaustedException(udid, ex);
            }
        }

        private static InvalidOperationException CreateRecoveryExhaustedException(string udid, Exception innerException = null)
        {
            return new InvalidOperationException($"当前页面结构获取失败，自动恢复已结束，请重新连接手机后重试，设备[{udid}]", innerException);
        }

        private static MobilePageSourceResult BuildResult(string xml, MobilePageSourceProviderKind provider, long generation, bool reconnectDeferred, string reason)
        {
            XmlDocument document = new XmlDocument();
            document.LoadXml(xml);
            XmlElement root = document.DocumentElement;
            root.SetAttribute("rpaSourceProvider", provider.ToString());
            root.SetAttribute("rpaSessionGeneration", generation.ToString());
            root.SetAttribute("rpaReconnectDeferred", reconnectDeferred ? "true" : "false");
            if (!string.IsNullOrWhiteSpace(reason)) root.SetAttribute("rpaDegradeReason", reason);
            return new MobilePageSourceResult
            {
                PageSource = document.OuterXml,
                Provider = provider,
                SessionGeneration = generation,
                ReconnectDeferred = reconnectDeferred,
                DegradeReason = reason ?? string.Empty
            };
        }

        private static void ValidateXml(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml)) throw new XmlException("Appium PageSource为空");
            XmlDocument document = new XmlDocument();
            document.LoadXml(xml);
            if (document.DocumentElement == null) throw new XmlException("Appium PageSource缺少根节点");
        }

        private static bool CanFallback(Exception exception)
        {
            if (exception is TimeoutException || exception is XmlException) return true;
            string text = exception.ToString().ToLowerInvariant();
            string[] markers = { "uiautomator2", "instrumentation", "socket hang up", "invalid session", "session deleted", "a session is either terminated or not started", "could not proxy", "cannot proxy", "econnreset", "connection reset", "pagesource为空" };
            foreach (string marker in markers) if (text.Contains(marker)) return true;
            return false;
        }

        private static string GetFallbackReason(Exception exception)
        {
            if (exception is TimeoutException) return "PageSourceTimeout";
            if (exception is XmlException) return "InvalidPageSourceXml";
            string text = exception.ToString().ToLowerInvariant();
            if (text.Contains("instrumentation")) return "UiAutomator2InstrumentationFailure";
            if (text.Contains("invalid session") || text.Contains("session deleted") || text.Contains("a session is either terminated or not started")) return "InvalidSession";
            if (text.Contains("socket hang up") || text.Contains("econnreset") || text.Contains("connection reset")) return "UiAutomator2TransportFailure";
            return "AppiumPageSourceFailure";
        }
    }

    internal sealed class DesignTimePageSourceDeferredException : InvalidOperationException
    {
        public DesignTimePageSourceDeferredException(string udid, MobileAutomationMode mode, int runtimeLeaseCount, bool isRuntimeOwned, bool isConnectionRecovering = false, string reason = "RuntimeOrRecoveryInUse")
            : base($"设计期PageSource已暂缓，原因[{reason}]，模式[{mode}]，RuntimeOwned[{isRuntimeOwned}]，RuntimeLease[{runtimeLeaseCount}]，ConnectionRecovering[{isConnectionRecovering}]，设备[{udid}]")
        {
        }
    }
}
