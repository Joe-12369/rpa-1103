using System;
using System.Collections.Concurrent;
using System.Threading;

namespace II.RPA.MobileProvider.Provider
{
    internal enum MobileAutomationMode
    {
        AppiumHealthy,
        AdbFallbackPending,
        ReleasingForAdbFallback,
        AdbDegraded,
        ReconnectingAppium,
        AppiumRecoveryPending,
        Unavailable
    }

    internal enum MobilePageSourceProviderKind
    {
        Appium,
        AdbDump
    }

    internal sealed class MobileAutomationStateSnapshot
    {
        public MobileAutomationMode Mode { get; set; }
        public long SessionGeneration { get; set; }
        public int RuntimeLeaseCount { get; set; }
        public int DesignTimeReadOnlyLeaseCount { get; set; }
        public bool IsRuntimeOwned { get; set; }
        public bool IsConnectionRecovering { get; set; }
        public string DegradeReason { get; set; }
        public DateTime LastAppiumSourceSuccessAt { get; set; }
        public DateTime LastAdbSourceSuccessAt { get; set; }
        public bool DesignTimeRecoveryAttempted { get; set; }
        public bool DesignTimeRecoveryExhausted { get; set; }
        public DateTime LastSuccessfulCommandAt { get; set; }
    }

    internal sealed class MobilePageSourceResult
    {
        public string PageSource { get; set; }
        public MobilePageSourceProviderKind Provider { get; set; }
        public long SessionGeneration { get; set; }
        public bool ReconnectDeferred { get; set; }
        public string DegradeReason { get; set; }
    }

    internal static class MobileAutomationStateStore
    {
        private sealed class StateEntry
        {
            public readonly object SyncRoot = new object();
            public MobileAutomationMode Mode = MobileAutomationMode.AppiumHealthy;
            public long SessionGeneration;
            public int RuntimeLeaseCount;
            public int DesignTimeReadOnlyLeaseCount;
            public bool IsRuntimeOwned;
            public bool IsConnectionRecovering;
            public string DegradeReason = string.Empty;
            public DateTime LastAppiumSourceSuccessAt = DateTime.MinValue;
            public DateTime LastAdbSourceSuccessAt = DateTime.MinValue;
            public bool DesignTimeRecoveryAttempted;
            public bool DesignTimeRecoveryExhausted;
            public DateTime LastSuccessfulCommandAt = DateTime.MinValue;
        }

        private static readonly ConcurrentDictionary<string, StateEntry> States =
            new ConcurrentDictionary<string, StateEntry>(StringComparer.OrdinalIgnoreCase);

        private static StateEntry GetEntry(string udid)
        {
            return States.GetOrAdd(string.IsNullOrWhiteSpace(udid) ? "__default__" : udid, _ => new StateEntry());
        }

        public static long NextSessionGeneration(string udid)
        {
            StateEntry entry = GetEntry(udid);
            lock (entry.SyncRoot)
            {
                return ++entry.SessionGeneration;
            }
        }

        public static MobileAutomationStateSnapshot GetSnapshot(string udid)
        {
            StateEntry entry = GetEntry(udid);
            lock (entry.SyncRoot)
            {
                return new MobileAutomationStateSnapshot
                {
                    Mode = entry.Mode,
                    SessionGeneration = entry.SessionGeneration,
                    RuntimeLeaseCount = entry.RuntimeLeaseCount,
                    DesignTimeReadOnlyLeaseCount = entry.DesignTimeReadOnlyLeaseCount,
                    IsRuntimeOwned = entry.IsRuntimeOwned,
                    IsConnectionRecovering = entry.IsConnectionRecovering,
                    DegradeReason = entry.DegradeReason,
                    LastAppiumSourceSuccessAt = entry.LastAppiumSourceSuccessAt,
                    LastAdbSourceSuccessAt = entry.LastAdbSourceSuccessAt,
                    DesignTimeRecoveryAttempted = entry.DesignTimeRecoveryAttempted,
                    DesignTimeRecoveryExhausted = entry.DesignTimeRecoveryExhausted,
                    LastSuccessfulCommandAt = entry.LastSuccessfulCommandAt
                };
            }
        }

        public static IDisposable BeginOperationLease(string udid, string operationName, bool isDesignTimeReadOnly)
        {
            StateEntry entry = GetEntry(udid);
            lock (entry.SyncRoot)
            {
                if (entry.IsConnectionRecovering || entry.Mode == MobileAutomationMode.ReleasingForAdbFallback)
                    throw new InvalidOperationException($"手机会话正在清理恢复，暂不接受操作[{operationName}]，设备[{udid}]");
                if (isDesignTimeReadOnly)
                {
                    if (entry.IsRuntimeOwned || entry.RuntimeLeaseCount > 0 || entry.Mode == MobileAutomationMode.AdbFallbackPending)
                        throw new DesignTimePageSourceDeferredException(udid, entry.Mode, entry.RuntimeLeaseCount,
                            entry.IsRuntimeOwned, entry.IsConnectionRecovering, "DesignTimeReadOnlyDeferred");
                    entry.DesignTimeReadOnlyLeaseCount++;
                }
                else
                {
                    entry.RuntimeLeaseCount++;
                }
            }
            return new OperationLease(entry, isDesignTimeReadOnly);
        }

        // Runtime接管意图不等于已有业务命令；只有实际执行租约才阻止只读故障恢复。
        public static IDisposable BeginConnectionRecovery(string udid)
        {
            StateEntry entry = GetEntry(udid);
            lock (entry.SyncRoot)
            {
                if (entry.RuntimeLeaseCount > 0 || entry.IsConnectionRecovering)
                    throw new InvalidOperationException($"手机仍有业务操作或会话清理正在执行，暂不能恢复连接，设备[{udid}]");
                entry.IsConnectionRecovering = true;
            }
            return new ConnectionRecoveryLease(entry);
        }

        public static void AcquireRuntimeOwnership(string udid)
        {
            StateEntry entry = GetEntry(udid);
            lock (entry.SyncRoot)
            {
                entry.IsRuntimeOwned = true;
            }
        }

        public static void ReleaseRuntimeOwnership(string udid)
        {
            string key = string.IsNullOrWhiteSpace(udid) ? "__default__" : udid;
            if (!States.TryGetValue(key, out StateEntry entry))
            {
                return;
            }
            lock (entry.SyncRoot)
            {
                entry.IsRuntimeOwned = false;
            }
        }

        public static bool TryEnterDesignTime(string udid)
        {
            string key = string.IsNullOrWhiteSpace(udid) ? "__default__" : udid;
            if (!States.TryGetValue(key, out StateEntry entry))
            {
                return true;
            }
            lock (entry.SyncRoot)
            {
                // 显式返回设计期只释放接管标记，不抢占仍在执行的业务命令或会话清理。
                // The fallback phase is not an active lease. Connections serialize on the
                // per-device session gate and can resume a completed, failed cleanup.
                if (entry.RuntimeLeaseCount > 0 || entry.IsConnectionRecovering)
                {
                    return false;
                }
                entry.IsRuntimeOwned = false;
                return true;
            }
        }

        // 调用方持有每UDID会话锁。先保存降级意图，再判断本次是否可执行。
        public static bool TryBeginAdbFallback(string udid, long expectedGeneration, string reason, out string deferredReason)
        {
            StateEntry entry = GetEntry(udid);
            lock (entry.SyncRoot)
            {
                if (entry.SessionGeneration != expectedGeneration)
                {
                    deferredReason = "SessionGenerationChanged";
                    return false;
                }
                entry.Mode = MobileAutomationMode.AdbFallbackPending;
                entry.DegradeReason = reason ?? string.Empty;
                if (entry.IsRuntimeOwned) deferredReason = "RuntimeOwnership";
                else if (entry.RuntimeLeaseCount > 0) deferredReason = "RuntimeOperationInFlight";
                else if (entry.IsConnectionRecovering) deferredReason = "SessionCleanupInFlight";
                else
                {
                    entry.Mode = MobileAutomationMode.ReleasingForAdbFallback;
                    deferredReason = string.Empty;
                    return true;
                }
                return false;
            }
        }

        public static void MarkAdbSourceSuccess(string udid, long generation)
        {
            StateEntry entry = GetEntry(udid);
            lock (entry.SyncRoot)
            {
                if (entry.SessionGeneration != generation) return;
                entry.Mode = MobileAutomationMode.AdbDegraded;
                entry.LastAdbSourceSuccessAt = DateTime.UtcNow;
                entry.DesignTimeRecoveryAttempted = false;
                entry.DesignTimeRecoveryExhausted = false;
            }
        }

        public static bool TryBeginDesignTimeRecovery(string udid, long generation)
        {
            StateEntry entry = GetEntry(udid);
            lock (entry.SyncRoot)
            {
                if (entry.SessionGeneration != generation || entry.IsRuntimeOwned || entry.RuntimeLeaseCount > 0
                    || entry.DesignTimeRecoveryAttempted || entry.DesignTimeRecoveryExhausted)
                    return false;
                entry.DesignTimeRecoveryAttempted = true;
                entry.Mode = MobileAutomationMode.ReconnectingAppium;
                return true;
            }
        }

        public static void MarkDesignTimeRecoveryExhausted(string udid, long generation)
        {
            StateEntry entry = GetEntry(udid);
            lock (entry.SyncRoot)
            {
                if (entry.SessionGeneration != generation) return;
                entry.DesignTimeRecoveryExhausted = true;
                entry.Mode = MobileAutomationMode.Unavailable;
            }
        }

        public static void ResetDesignTimeRecovery(string udid, long generation)
        {
            StateEntry entry = GetEntry(udid);
            lock (entry.SyncRoot)
            {
                if (entry.SessionGeneration != generation) return;
                entry.DesignTimeRecoveryAttempted = false;
                entry.DesignTimeRecoveryExhausted = false;
            }
        }

        public static void MarkReconnecting(string udid)
        {
            StateEntry entry = GetEntry(udid);
            lock (entry.SyncRoot)
            {
                entry.Mode = MobileAutomationMode.ReconnectingAppium;
            }
        }

        public static void MarkSessionCreated(string udid, long generation)
        {
            StateEntry entry = GetEntry(udid);
            lock (entry.SyncRoot)
            {
                if (generation != entry.SessionGeneration)
                {
                    return;
                }
                entry.DesignTimeRecoveryExhausted = false;
                entry.Mode = string.IsNullOrEmpty(entry.DegradeReason)
                    ? MobileAutomationMode.AppiumHealthy
                    : MobileAutomationMode.AppiumRecoveryPending;
            }
        }

        public static void MarkAppiumSourceSuccess(string udid, long generation)
        {
            StateEntry entry = GetEntry(udid);
            lock (entry.SyncRoot)
            {
                if (generation != entry.SessionGeneration)
                {
                    return;
                }
                entry.Mode = MobileAutomationMode.AppiumHealthy;
                entry.DegradeReason = string.Empty;
                entry.DesignTimeRecoveryAttempted = false;
                entry.DesignTimeRecoveryExhausted = false;
                entry.LastAppiumSourceSuccessAt = DateTime.UtcNow;
            }
        }

        public static void MarkCommandSuccess(string udid, long generation)
        {
            StateEntry entry = GetEntry(udid);
            lock (entry.SyncRoot)
            {
                if (generation == entry.SessionGeneration)
                {
                    entry.LastSuccessfulCommandAt = DateTime.UtcNow;
                }
            }
        }

        public static void MarkUnavailable(string udid, string reason)
        {
            StateEntry entry = GetEntry(udid);
            lock (entry.SyncRoot)
            {
                entry.Mode = MobileAutomationMode.Unavailable;
                entry.DegradeReason = reason ?? string.Empty;
            }
        }

        public static void Remove(string udid)
        {
            States.TryRemove(string.IsNullOrWhiteSpace(udid) ? "__default__" : udid, out _);
        }

        private sealed class ConnectionRecoveryLease : IDisposable
        {
            private StateEntry _entry;
            public ConnectionRecoveryLease(StateEntry entry) { _entry = entry; }
            public void Dispose()
            {
                StateEntry entry = Interlocked.Exchange(ref _entry, null);
                if (entry == null) return;
                lock (entry.SyncRoot) { entry.IsConnectionRecovering = false; }
            }
        }

        private sealed class OperationLease : IDisposable
        {
            private StateEntry _entry;
            private readonly bool _isDesignTimeReadOnly;
            public OperationLease(StateEntry entry, bool isDesignTimeReadOnly)
            {
                _entry = entry;
                _isDesignTimeReadOnly = isDesignTimeReadOnly;
            }
            public void Dispose()
            {
                StateEntry entry = Interlocked.Exchange(ref _entry, null);
                if (entry == null) return;
                lock (entry.SyncRoot)
                {
                    if (_isDesignTimeReadOnly)
                    {
                        if (entry.DesignTimeReadOnlyLeaseCount > 0) entry.DesignTimeReadOnlyLeaseCount--;
                    }
                    else if (entry.RuntimeLeaseCount > 0) entry.RuntimeLeaseCount--;
                }
            }
        }
    }
}
