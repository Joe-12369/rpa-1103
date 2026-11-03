using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.MobileProvider.Params;
using II.RPA.MobileProvider.Provider;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.MobileProvider.Service
{
    public class MobileServer
    {
        private const int DefaultConnectTimeoutMilliseconds = 60000;
        private const int ReusedSessionProbeTimeoutMilliseconds = 5000;

        private sealed class PreparedConnection
        {
            public MobileInfo Info { get; set; }
            public MobileSession Session { get; set; }
            public bool IsNewSession { get; set; }
            public long Generation { get; set; }
            public string ConnectionKey { get; set; }
        }

        public static async Task<string> CreateConnectSession(MobileInfo mobileInfo, bool reStartAppium = false)
        {
            long generation = MobileSessionCache.BeginConnectionAttempt(mobileInfo?.Udid);
            try
            {
                PreparedConnection prepared = await PrepareConnectSessionCoreAsync(mobileInfo, reStartAppium, generation);
                return PublishPreparedConnection(prepared);
            }
            catch (Exception ex)
            {
                if (ex is RPAException)
                {
                    throw;
                }
                throw new RPAException(eErrorMessageType.Error_MobileConnect, eComponentType.ConnectMobile, ex);
            }
        }

        public static async Task<string> CreateConnectSession(MobileInfo mobileInfo, int timeOut, bool reStartAppium = false)
        {
            int effectiveTimeout = timeOut > 0 ? timeOut : DefaultConnectTimeoutMilliseconds;
            long generation = MobileSessionCache.BeginConnectionAttempt(mobileInfo?.Udid);
            Task<PreparedConnection> connectTask = PrepareConnectSessionCoreAsync(mobileInfo, reStartAppium, generation);
            using (var timeoutCancellationTokenSource = new CancellationTokenSource())
            {
                Task delayTask = Task.Delay(effectiveTimeout, timeoutCancellationTokenSource.Token);
                Task completedTask = await Task.WhenAny(connectTask, delayTask);
                if (completedTask == connectTask || connectTask.IsCompleted)
                {
                    timeoutCancellationTokenSource.Cancel();
                    PreparedConnection prepared = await connectTask;
                    return PublishPreparedConnection(prepared);
                }

                MobileSessionCache.InvalidateConnectionAttempt(mobileInfo?.Udid, generation);
                MobileSession timedOutSession = MobileSessionCache.MarkOperationTimedOut(mobileInfo?.Udid, "ConnectMobile");
                _ = CleanupTimedOutConnectionAsync(connectTask, timedOutSession);
                throw new RPAException(eErrorMessageType.Error_PluginRun_TimeOut);
            }
        }

        private static async Task<PreparedConnection> PrepareConnectSessionCoreAsync(
            MobileInfo mobileInfo,
            bool reStartAppium,
            long generation)
        {
            if (mobileInfo == null)
            {
                throw new ArgumentNullException(nameof(mobileInfo));
            }
            if (string.IsNullOrEmpty(GlobleConfig.AppiumServerPort))
            {
                throw new RPAException(eErrorMessageType.Error_NotInstallMobilePlugin, eComponentType.GetPluginInstallAndDevicesStatus);
            }

            MobileSessionCache.RegisterDeviceInfo(mobileInfo);
            string connectionKey = mobileInfo.Udid;
            EnsureCurrentConnectionAttempt(connectionKey, generation);
            MobileServerManager checkMobileServer = new MobileServerManager();
            if (checkMobileServer.CheckNodeIsClosed() || reStartAppium)
            {
                checkMobileServer.InitMobileServer();
                if (!await checkMobileServer.WaitForAppiumReadyAsync(GlobleConfig.AppiumServerPort))
                {
                    throw new RPAException(
                        eErrorMessageType.Error_MobileConnect,
                        eComponentType.ConnectMobile,
                        new TimeoutException($"Appium server is not ready on port {GlobleConfig.AppiumServerPort}"));
                }
            }
            else if (!MobileServerManager.IsAppiumPortListening(GlobleConfig.AppiumServerPort))
            {
                if (!await checkMobileServer.WaitForAppiumReadyAsync(GlobleConfig.AppiumServerPort))
                {
                    throw new RPAException(
                        eErrorMessageType.Error_MobileConnect,
                        eComponentType.ConnectMobile,
                        new TimeoutException($"Appium server is not ready on port {GlobleConfig.AppiumServerPort}"));
                }
            }

            EnsureCurrentConnectionAttempt(connectionKey, generation);
            string udid = mobileInfo.Udid;
            return await MobileSessionCache.WithSessionLockAsync(udid, async () =>
            {
                EnsureCurrentConnectionAttempt(connectionKey, generation);
                return await PrepareSessionAsync(mobileInfo, generation, udid);
            });
        }

        private static async Task<PreparedConnection> PrepareSessionAsync(MobileInfo mobileInfo, long generation, string connectionKey)
        {
            string udid = mobileInfo.Udid;
            MobileSession mobileSession = null;
            bool isNewSession = false;
            if (!string.IsNullOrEmpty(udid))
            {
                MobileSessionCache.TryGet(udid, out MobileSession cachedSession, out _);
                if (cachedSession != null && (cachedSession.ReadOnlySessionRecovery != null || cachedSession.HasOnlyReadOnlyTimedOutOperations))
                {
                    await RecoverReadOnlySessionAsync(udid, cachedSession, connectionKey, generation);
                    mobileInfo.ForceNewSession = false;
                }
                else if (mobileInfo.ForceNewSession)
                {
                    await ResetDeviceSessionAsync(udid, "ForceReconnect");
                    mobileInfo.ForceNewSession = false;
                }
                else if (cachedSession != null)
                {
                    if (cachedSession.HasTimedOutOperation)
                    {
                        throw new InvalidOperationException($"手机会话仍有业务超时命令在后台执行，暂不能重新连接，设备[{udid}]");
                    }
                    else if (cachedSession.RequiresRecovery)
                    {
                        await ResetDeviceSessionAsync(udid, "SessionUnavailable");
                    }
                    else
                    {
                        mobileSession = MobileSessionCache.GetValidSession(udid, force: false, autoReConnect: false);
                        if (mobileSession != null && !await VerifyCachedSessionAsync(udid, mobileSession, connectionKey, generation))
                        {
                            EnsureCurrentConnectionAttempt(connectionKey, generation);
                            mobileSession = null;
                        }
                    }
                }
            }

            if (mobileSession == null)
            {
                mobileSession = MobileSessionFactory.CreateSession(mobileInfo);
                isNewSession = true;
                if (string.IsNullOrEmpty(mobileInfo.Udid))
                {
                    mobileInfo.Udid = mobileSession.Udid;
                    udid = mobileInfo.Udid;
                }
            }

            try
            {
                EnsureCurrentConnectionAttempt(connectionKey, generation);
                if (mobileInfo.IsAutoUnlock)
                {
                    bool unlockResult = await mobileSession.UnlockAsync(new ParamUnlockModel
                    {
                        unlockType = mobileInfo.UnlockType,
                        unlockPw = mobileInfo.UnlockPw
                    });
                    if (!unlockResult)
                    {
                        throw new Exception(Localize($"{mobileSession.Udid}:解锁失败", "controlType_MobileServer_4C15E69"));
                    }
                }
                EnsureCurrentConnectionAttempt(connectionKey, generation);

                return new PreparedConnection
                {
                    Info = mobileInfo,
                    Session = mobileSession,
                    IsNewSession = isNewSession,
                    Generation = generation,
                    ConnectionKey = connectionKey
                };
            }
            catch
            {
                if (isNewSession)
                {
                    mobileSession.CloseDetached();
                }
                throw;
            }
        }

        private static async Task<bool> VerifyCachedSessionAsync(string udid, MobileSession session, string connectionKey, long generation)
        {
            // A driver command can remain in flight after our timeout. Quarantine the session until it actually ends.
            Task probeTask = Task.Run(() => session.VerifyReusableSession());
            if (await Task.WhenAny(probeTask, Task.Delay(ReusedSessionProbeTimeoutMilliseconds)) != probeTask
                && !probeTask.IsCompleted)
            {
                session.MarkOperationTimedOut("ConnectSessionProbe", isReadOnly: true);
                _ = ObserveTimedOutProbeAsync(session, probeTask);
                await RecoverReadOnlySessionAsync(udid, session, connectionKey, generation);
                return false;
            }

            try
            {
                await probeTask;
                return true;
            }
            catch (Exception ex)
            {
                session.MarkOperationFailed("ConnectSessionProbe", ex);
                if (!session.RequiresRecovery)
                    throw new InvalidOperationException($"连接时无法确认缓存会话有效，设备[{udid}]", ex);

                EnsureCurrentConnectionAttempt(connectionKey, generation);
                await ResetDeviceSessionAsync(udid, "ConnectSessionProbeFailed");
                return false;
            }
        }

        private static async Task ObserveTimedOutProbeAsync(MobileSession session, Task probeTask)
        {
            Exception completionException = null;
            try { await probeTask; }
            catch (Exception ex) { completionException = ex; }
            finally { session.MarkTimedOutOperationCompleted("ConnectSessionProbe", completionException, isReadOnly: true); }
        }
        private static async Task RecoverReadOnlySessionAsync(string udid, MobileSession session, string connectionKey, long generation)
        {
            EnsureCurrentConnectionAttempt(connectionKey, generation);
            await MobileSessionRecovery.RecoverReadOnlySessionAsync(udid, session);
            EnsureCurrentConnectionAttempt(connectionKey, generation);
            if (!MobileSessionCache.TryDetachExpected(udid, session, out _))
                throw new InvalidOperationException($"只读超时恢复期间会话已变化，终止本次连接，设备[{udid}]");
        }

        private static async Task ResetDeviceSessionAsync(string udid, string reason)
        {
            MobileSessionCache.TryDetach(udid, out MobileSession detachedSession, out _);
            await MobileSessionRecovery.ResetDetachedSessionAsync(udid, detachedSession, reason);
        }

        private static string PublishPreparedConnection(PreparedConnection prepared)
        {
            if (prepared == null || prepared.Session == null || prepared.Info == null)
            {
                throw new InvalidOperationException("Prepared mobile connection is invalid");
            }
            bool published = MobileSessionCache.ExecuteIfCurrentConnectionAttempt(
                prepared.ConnectionKey,
                prepared.Generation,
                () =>
                {
                    if (prepared.IsNewSession)
                    {
                        MobileSessionCache.Add(prepared.Info, prepared.Session);
                    }
                    MobileAutomationStateStore.ResetDesignTimeRecovery(prepared.Info.Udid, prepared.Session.Generation);
                });
            if (!published)
            {
                if (prepared.IsNewSession)
                {
                    prepared.Session.CloseDetached();
                }
                throw new InvalidOperationException($"手机连接请求已过期，设备[{prepared.Info.Udid}]");
            }
            return prepared.Session.Udid;
        }

        private static void EnsureCurrentConnectionAttempt(string udid, long generation)
        {
            if (!MobileSessionCache.IsCurrentConnectionAttempt(udid, generation))
            {
                throw new InvalidOperationException($"手机连接请求已过期，设备[{udid}]");
            }
        }

        private static async Task CleanupTimedOutConnectionAsync(
            Task<PreparedConnection> connectTask,
            MobileSession timedOutSession)
        {
            Exception completionException = null;
            try
            {
                PreparedConnection prepared = await connectTask;
                if (prepared?.IsNewSession == true)
                {
                    prepared.Session?.CloseDetached();
                }
            }
            catch (Exception ex)
            {
                completionException = ex;
            }
            finally
            {
                timedOutSession?.MarkTimedOutOperationCompleted("ConnectMobile", completionException);
            }
        }
    }
}
