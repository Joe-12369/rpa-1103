using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.MobileProvider.Provider
{
    public class MobileSessionCache
    {
        private static readonly object session_Lock = new object();

        private static readonly ConcurrentDictionary<string, MobileSession> _sessionDict;

        private static readonly ConcurrentDictionary<string, MobileInfo> _deviceDict;

        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _sessionGates;
        private static readonly ConcurrentDictionary<string, object> _sessionResolveGates;
        private static readonly ConcurrentDictionary<string, long> _connectionGenerations;
        private static readonly ConcurrentDictionary<string, long> _operationGenerations;
        private static readonly AsyncLocal<OperationGenerationContext> _operationGenerationContext = new AsyncLocal<OperationGenerationContext>();

        public static async Task<TResult> WithSessionLockAsync<TResult>(string udid, Func<Task<TResult>> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            string gateKey = string.IsNullOrWhiteSpace(udid) ? "__default__" : udid;
            var gate = _sessionGates.GetOrAdd(gateKey, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync();
            try
            {
                return await action();
            }
            finally
            {
                gate.Release();
            }
        }
        public static TResult WithSessionLock<TResult>(string udid, Func<TResult> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            string gateKey = string.IsNullOrWhiteSpace(udid) ? "__default__" : udid;
            var gate = _sessionGates.GetOrAdd(gateKey, _ => new SemaphoreSlim(1, 1));
            gate.Wait();
            try { return action(); }
            finally { gate.Release(); }
        }
        public static MobileSession GetValidSession(string udid, bool force = true, bool autoReConnect = true)
        {
            string gateKey = GetGateKey(udid);
            long operationGeneration = GetExpectedOperationGeneration(gateKey);
            var gate = _sessionResolveGates.GetOrAdd(gateKey, _ => new object());
            lock (gate)
            {
                if (!TryGet(udid, out var session, out var info))
                {
                    if (!autoReConnect || !_deviceDict.TryGetValue(udid, out info))
                    {
                        return null;
                    }
                    MobileAutomationStateStore.MarkReconnecting(udid);
                    MobileSession recreatedSession = MobileSessionFactory.CreateSession(info);
                    bool recreatedPublished = ExecuteIfCurrentOperationGeneration(
                        gateKey,
                        operationGeneration,
                        () => Add(info, recreatedSession));
                    if (!recreatedPublished)
                    {
                        recreatedSession.CloseDetached();
                        return null;
                    }
                    return recreatedSession;
                }
                // 超时命令仍可能在后台执行，不能在其结束前关闭底层Driver。
                if (session.HasTimedOutOperation || session.ReadOnlySessionRecovery != null)
                {
                    return null;
                }
                if (session.IsSessionReusable())
                {
                    return session;
                }

                // 只移除当前缓存中的这个实例，避免误删后续已经发布的新Session。
                if (!TryRemove(udid, session, out MobileInfo removedInfo))
                {
                    return null;
                }
                if (session.RequiresRecovery)
                {
                    MobileSessionRecovery.ResetDetachedSession(udid, session, "SessionUnavailable");
                }
                else
                {
                    session.CloseDetached();
                }

                if (!autoReConnect)
                {
                    return null;
                }

                MobileSession mobileSession = MobileSessionFactory.CreateSession(removedInfo ?? info);
                bool published = ExecuteIfCurrentOperationGeneration(
                    gateKey,
                    operationGeneration,
                    () => Add(removedInfo ?? info, mobileSession));
                if (!published)
                {
                    mobileSession.CloseDetached();
                    return null;
                }
                return mobileSession;
            }
        }

        public static IDisposable BeginOperationGenerationScope(string udid)
        {
            string gateKey = GetGateKey(udid);
            OperationGenerationContext previous = _operationGenerationContext.Value;
            _operationGenerationContext.Value = new OperationGenerationContext(
                gateKey,
                GetOperationGeneration(gateKey));
            return new OperationGenerationScope(previous);
        }

        public static void InvalidateOperationGeneration(string udid)
        {
            string gateKey = GetGateKey(udid);
            lock (GetOperationGenerationGate(gateKey))
            {
                _operationGenerations.AddOrUpdate(gateKey, 1, (_, current) => checked(current + 1));
            }
        }

        private static long GetExpectedOperationGeneration(string gateKey)
        {
            OperationGenerationContext context = _operationGenerationContext.Value;
            if (context != null && string.Equals(context.GateKey, gateKey, StringComparison.Ordinal))
            {
                return context.Generation;
            }
            return GetOperationGeneration(gateKey);
        }

        private static long GetOperationGeneration(string gateKey)
        {
            lock (GetOperationGenerationGate(gateKey))
            {
                return _operationGenerations.GetOrAdd(gateKey, 0);
            }
        }

        private static bool ExecuteIfCurrentOperationGeneration(string gateKey, long generation, Action action)
        {
            lock (GetOperationGenerationGate(gateKey))
            {
                if (_operationGenerations.GetOrAdd(gateKey, 0) != generation)
                {
                    return false;
                }
                action();
                return true;
            }
        }

        private static object GetOperationGenerationGate(string gateKey)
        {
            return _sessionResolveGates.GetOrAdd($"operation:{gateKey}", _ => new object());
        }

        public static MobileSession MarkOperationTimedOut(string udid, string operationName)
        {
            if (string.IsNullOrWhiteSpace(udid))
            {
                return null;
            }
            if (_sessionDict.TryGetValue(udid, out var session))
            {
                session.MarkOperationTimedOut(operationName);
                return session;
            }
            return null;
        }

        public static MobileSession MarkOperationFailed(string udid, string operationName, Exception exception)
        {
            if (string.IsNullOrWhiteSpace(udid))
            {
                return null;
            }
            if (_sessionDict.TryGetValue(udid, out var session))
            {
                session.MarkOperationFailed(operationName, exception);
                return session;
            }
            return null;
        }
        public static long BeginConnectionAttempt(string udid)
        {
            string gateKey = GetGateKey(udid);
            lock (GetConnectionAttemptGate(gateKey))
            {
                return _connectionGenerations.AddOrUpdate(gateKey, 1, (_, current) => checked(current + 1));
            }
        }

        public static bool IsCurrentConnectionAttempt(string udid, long generation)
        {
            string gateKey = GetGateKey(udid);
            lock (GetConnectionAttemptGate(gateKey))
            {
                return _connectionGenerations.TryGetValue(gateKey, out long current)
                    && current == generation;
            }
        }

        public static bool ExecuteIfCurrentConnectionAttempt(string udid, long generation, Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }
            string gateKey = GetGateKey(udid);
            lock (GetConnectionAttemptGate(gateKey))
            {
                if (!_connectionGenerations.TryGetValue(gateKey, out long current) || current != generation)
                {
                    return false;
                }
                action();
                return true;
            }
        }

        public static void InvalidateConnectionAttempt(string udid, long generation)
        {
            string gateKey = GetGateKey(udid);
            lock (GetConnectionAttemptGate(gateKey))
            {
                _connectionGenerations.AddOrUpdate(
                    gateKey,
                    _ => checked(generation + 1),
                    (_, current) => current == generation ? checked(current + 1) : current);
            }
        }

        private static object GetConnectionAttemptGate(string gateKey)
        {
            return _sessionResolveGates.GetOrAdd($"connection:{gateKey}", _ => new object());
        }

        private static string GetGateKey(string udid)
        {
            return string.IsNullOrWhiteSpace(udid) ? "__default__" : udid;
        }
        public static bool TryGet(string udid, out MobileSession session, out MobileInfo info)
        {
            session = null;
            info = null;
            if (_sessionDict.TryGetValue(udid, out session))
            {
                return _deviceDict.TryGetValue(udid, out info);
            }
            return false;
        }

        private static bool TryRemove(string udid, MobileSession expectedSession, out MobileInfo info)
        {
            info = null;
            if (string.IsNullOrWhiteSpace(udid) || expectedSession == null)
            {
                return false;
            }

            if (!_deviceDict.TryGetValue(udid, out info))
            {
                return false;
            }

            var entry = new KeyValuePair<string, MobileSession>(udid, expectedSession);
            bool removed = ((ICollection<KeyValuePair<string, MobileSession>>)_sessionDict).Remove(entry);
            return removed;
        }
        public static bool TryDetach(string udid, out MobileSession session, out MobileInfo info)
        {
            session = null;
            info = null;
            if (string.IsNullOrWhiteSpace(udid))
            {
                return false;
            }

            string gateKey = GetGateKey(udid);
            var gate = _sessionResolveGates.GetOrAdd(gateKey, _ => new object());
            lock (gate)
            {
                if (!TryGet(udid, out session, out info))
                {
                    return false;
                }
                return TryRemove(udid, session, out info);
            }
        }
        public static void Add(MobileInfo info, MobileSession session)
        {
            RegisterDeviceInfo(info);
            if (_sessionDict.ContainsKey(info.Udid))
            {
                _sessionDict[info.Udid] = session;
                if (_deviceDict.TryGetValue(info.Udid, out var value))
                {
                    value.Replace(info);
                }
                return;
            }
            _sessionDict.TryAdd(info.Udid, session);
            _deviceDict.TryAdd(info.Udid, info);
        }

        public static bool TryDetachExpected(string udid, MobileSession expectedSession, out MobileInfo info)
        {
            info = null;
            if (string.IsNullOrWhiteSpace(udid) || expectedSession == null) return false;
            string gateKey = GetGateKey(udid);
            var gate = _sessionResolveGates.GetOrAdd(gateKey, _ => new object());
            lock (gate) { return TryRemove(udid, expectedSession, out info); }
        }

        public static bool TryGetDeviceInfo(string udid, out MobileInfo info)
        {
            return _deviceDict.TryGetValue(udid, out info);
        }

        public static void RegisterDeviceInfo(MobileInfo info)
        {
            if (info == null || string.IsNullOrWhiteSpace(info.Udid)) return;
            _deviceDict.AddOrUpdate(info.Udid, _ => CloneInfo(info), (_, current) =>
            {
                current.Replace(info);
                current.AdditionalCapabilities = info.AdditionalCapabilities == null
                    ? null
                    : new Dictionary<string, string>(info.AdditionalCapabilities);
                current.IsAutoUnlock = info.IsAutoUnlock;
                current.UnlockType = info.UnlockType;
                current.UnlockPw = info.UnlockPw;
                return current;
            });
        }

        private static MobileInfo CloneInfo(MobileInfo info)
        {
            return new MobileInfo
            {
                Udid = info.Udid,
                PlatformName = info.PlatformName,
                PlatformVersion = info.PlatformVersion,
                ForceNewSession = info.ForceNewSession,
                AdditionalCapabilities = info.AdditionalCapabilities == null ? null : new Dictionary<string, string>(info.AdditionalCapabilities),
                IsAutoUnlock = info.IsAutoUnlock,
                UnlockType = info.UnlockType,
                UnlockPw = info.UnlockPw
            };
        }

        public static void MarkAllSessionsRequiresRecovery(string reason)
        {
            foreach (KeyValuePair<string, MobileSession> pair in _sessionDict.ToArray())
            {
                pair.Value?.MarkRequiresRecovery(reason);
                MobileAutomationStateStore.MarkUnavailable(pair.Key, reason);
            }
        }
        public static void ReleaseSessionOnly(string udid)
        {
            _sessionDict.TryRemove(udid, out _);
        }

        public static void Release(string udid)
        {
            _sessionDict.TryRemove(udid, out _);
            _deviceDict.TryRemove(udid, out _);
            MobileAutomationStateStore.Remove(udid);
            MobileSystemPortAllocator.Release(udid);
        }

        private sealed class OperationGenerationContext
        {
            public OperationGenerationContext(string gateKey, long generation)
            {
                GateKey = gateKey;
                Generation = generation;
            }

            public string GateKey { get; }
            public long Generation { get; }
        }

        private sealed class OperationGenerationScope : IDisposable
        {
            private readonly OperationGenerationContext _previous;
            private int _disposed;

            public OperationGenerationScope(OperationGenerationContext previous)
            {
                _previous = previous;
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) == 0)
                {
                    _operationGenerationContext.Value = _previous;
                }
            }
        }

        static MobileSessionCache()
        {
            _sessionDict = new ConcurrentDictionary<string, MobileSession>();
            _deviceDict = new ConcurrentDictionary<string, MobileInfo>();
            _sessionGates = new ConcurrentDictionary<string, SemaphoreSlim>();
            _sessionResolveGates = new ConcurrentDictionary<string, object>();
            _connectionGenerations = new ConcurrentDictionary<string, long>();
            _operationGenerations = new ConcurrentDictionary<string, long>();
        }
    }
}
