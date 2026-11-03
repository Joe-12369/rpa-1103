using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace II.RPA.Plugin.Windows.OwnerDraw
{
    internal interface IOwnerDrawBridgeActivator
    {
        bool TryCheckLegacyConflict(int processId, string legacyFileName,
            out bool found, out string status);

        bool TryStart(int processId, string bridgePath, string legacyFileName,
            int requiredAbi, int controllerProcessId,
            out string status);

        bool TryStopResident(int processId, string bridgePath,
            string legacyFileName, int requiredAbi,
            out OwnerDrawBridgeStopState stopState, out string status);
    }

    internal enum OwnerDrawBridgeStopState
    {
        NotLoaded,
        StoppedResident,
        LegacyConflict,
    }

    /// <summary>
    /// Owns bridge state without leaking framework-specific logic into GuiOperator.
    /// Sessions are isolated by adapter, target PID, and target architecture. A
    /// failed pipe call should invalidate the session so the next attempt invokes
    /// StartWork again through the architecture-appropriate route.
    /// </summary>
    internal sealed class OwnerDrawBridgeManager
    {
        private const int DefaultStartProbeAttempts = 11;
        private const int DefaultStartProbeDelayMs = 50;
        private const int DefaultStartProbeDeadlineMs = 500;

        private readonly object gate = new object();
        private readonly Dictionary<string, OwnerDrawBridgeSession> sessions
            = new Dictionary<string, OwnerDrawBridgeSession>(StringComparer.OrdinalIgnoreCase);
        private readonly IOwnerDrawBridgeActivator activator;
        private readonly string bridgeDirectory;
        private readonly int startProbeAttempts;
        private readonly int startProbeDelayMs;
        private readonly int startProbeDeadlineMs;

        internal OwnerDrawBridgeManager(string bridgeDirectory = null,
            IOwnerDrawBridgeActivator activator = null,
            int startProbeAttempts = DefaultStartProbeAttempts,
            int startProbeDelayMs = DefaultStartProbeDelayMs,
            int startProbeDeadlineMs = DefaultStartProbeDeadlineMs)
        {
            if (startProbeAttempts <= 0)
                throw new ArgumentOutOfRangeException("startProbeAttempts");
            if (startProbeDelayMs < 0)
                throw new ArgumentOutOfRangeException("startProbeDelayMs");
            if (startProbeDeadlineMs < 0)
                throw new ArgumentOutOfRangeException("startProbeDeadlineMs");
            string assemblyDirectory = Path.GetDirectoryName(
                typeof(OwnerDrawBridgeManager).Assembly.Location) ?? AppDomain.CurrentDomain.BaseDirectory;
            this.bridgeDirectory = Path.GetFullPath(string.IsNullOrWhiteSpace(bridgeDirectory)
                ? Path.Combine(assemblyDirectory, "OwnerDraw")
                : bridgeDirectory);
            this.activator = activator
                ?? new OwnerDrawBridgeActivatorRouter(this.bridgeDirectory);
            this.startProbeAttempts = startProbeAttempts;
            this.startProbeDelayMs = startProbeDelayMs;
            this.startProbeDeadlineMs = startProbeDeadlineMs;
        }

        internal static OwnerDrawBridgeManager Shared { get; }
            = new OwnerDrawBridgeManager();

        internal OwnerDrawResult<OwnerDrawBridgeSession> EnsureReady(IntPtr hostHwnd,
            OwnerDrawBridgeDescriptor descriptor)
        {
            if (descriptor == null)
            {
                return OwnerDrawResult<OwnerDrawBridgeSession>.Failed(
                    OwnerDrawFailureCode.InvalidRequest, "Bridge descriptor is null.");
            }
            if (!OwnerDrawWindowProbe.TryGetProcessId(hostHwnd, out int processId))
            {
                return OwnerDrawResult<OwnerDrawBridgeSession>.Failed(
                    OwnerDrawFailureCode.TargetUnavailable,
                    "Cannot resolve a live target process for hwnd=0x"
                    + hostHwnd.ToInt64().ToString("X") + ".");
            }
            if (!OwnerDrawWindowProbe.TryGetArchitecture(processId,
                out OwnerDrawArchitecture targetArchitecture, out string architectureStatus))
            {
                RemoveAllArchitectureKeys(descriptor.AdapterId, processId);
                return OwnerDrawResult<OwnerDrawBridgeSession>.Failed(
                    OwnerDrawFailureCode.ArchitectureUnknown, architectureStatus);
            }

            OwnerDrawArchitecture controllerArchitecture = Environment.Is64BitProcess
                ? OwnerDrawArchitecture.X64 : OwnerDrawArchitecture.X86;
            if (!OwnerDrawBridgeArchitecturePolicy.TryResolve(controllerArchitecture,
                targetArchitecture, descriptor, out _, out string routeStatus))
            {
                RemoveAllArchitectureKeys(descriptor.AdapterId, processId);
                return OwnerDrawResult<OwnerDrawBridgeSession>.Failed(
                    OwnerDrawFailureCode.ArchitectureMismatch,
                    routeStatus + " pid=" + processId + ".");
            }

            string bridgeFileName = descriptor.GetFileName(targetArchitecture);
            string bridgePath = Path.Combine(bridgeDirectory, bridgeFileName);
            if (string.IsNullOrWhiteSpace(bridgeFileName) || !File.Exists(bridgePath))
            {
                return OwnerDrawResult<OwnerDrawBridgeSession>.Failed(
                    OwnerDrawFailureCode.BridgeNotFound,
                    "Owner-draw bridge is missing for " + ToText(targetArchitecture)
                    + ": \"" + bridgePath + "\".");
            }

            string key = MakeKey(descriptor.AdapterId, processId, targetArchitecture);
            if (!TryGetProcessStartTime(processId, out DateTime processStartTimeUtc))
            {
                RemoveAllArchitectureKeys(descriptor.AdapterId, processId);
                return OwnerDrawResult<OwnerDrawBridgeSession>.Failed(
                    OwnerDrawFailureCode.TargetUnavailable,
                    "Cannot read target process start time for pid=" + processId
                    + "; refusing a PID-reuse-unsafe bridge session.");
            }
            lock (gate)
            {
                if (sessions.TryGetValue(key, out OwnerDrawBridgeSession existing)
                    && IsProcessAlive(processId)
                    && existing.Architecture == targetArchitecture
                    && existing.ProcessStartTimeUtc == processStartTimeUtc
                    && string.Equals(existing.BridgePath, bridgePath,
                        StringComparison.OrdinalIgnoreCase))
                {
                    if (TryProbe(descriptor, processId, targetArchitecture,
                        out string reuseProbeStatus))
                    {
                        existing.HostHwnd = hostHwnd;
                        return OwnerDrawResult<OwnerDrawBridgeSession>.Success(existing,
                            "Reused live owner-draw bridge session; "
                            + architectureStatus + "; " + routeStatus + "; "
                            + reuseProbeStatus);
                    }

                    // A cached session is only advisory. If the resident Bridge was
                    // stopped externally or its pipe failed, force StartWork below.
                    sessions.Remove(key);
                }

                sessions.Remove(key);
                // Module enumeration is activation preflight, not a hover health
                // check. In the x64-controller/x86-target route it launches the x86
                // Broker and dominated every PB pick. A cached session was already
                // identity-checked at activation and its PING verifies ABI, Build ID,
                // architecture, adapter and running state. Re-run this strict module
                // inspection whenever the session is absent or its PING fails.
                if (!TryCheckLegacyConflictCore(processId, descriptor,
                    out bool legacyConflict, out string legacyStatus))
                {
                    return OwnerDrawResult<OwnerDrawBridgeSession>.Failed(
                        OwnerDrawFailureCode.BridgeStartFailed,
                        "Legacy Bridge conflict inspection failed; refusing to load "
                        + "or call a Bridge. " + legacyStatus);
                }
                if (legacyConflict)
                {
                    return OwnerDrawResult<OwnerDrawBridgeSession>.Failed(
                        OwnerDrawFailureCode.LegacyConflict, legacyStatus);
                }

                if (!activator.TryStart(processId, bridgePath,
                    descriptor.LegacyFileName, descriptor.RequiredAbi,
                    GetCurrentControllerProcessId(),
                    out string startStatus))
                {
                    return OwnerDrawResult<OwnerDrawBridgeSession>.Failed(
                        OwnerDrawFailureCode.BridgeStartFailed,
                        "Owner-draw bridge start failed adapter=" + descriptor.AdapterId
                        + "; " + architectureStatus + "; " + routeStatus + "; "
                        + startStatus);
                }
                if (!TryProbeAfterStart(descriptor, processId, targetArchitecture,
                    out string probeStatus))
                {
                    bool rollbackStopped = false;
                    OwnerDrawBridgeStopState rollbackState
                        = OwnerDrawBridgeStopState.NotLoaded;
                    string rollbackStatus;
                    try
                    {
                        rollbackStopped = activator.TryStopResident(processId,
                            bridgePath, descriptor.LegacyFileName,
                            descriptor.RequiredAbi, out rollbackState,
                            out rollbackStatus);
                    }
                    catch (Exception ex)
                    {
                        rollbackStatus = "Rollback EndWork threw "
                            + ex.GetType().Name + ": " + ex.Message;
                    }
                    bool rollbackConfirmed = rollbackStopped
                        && rollbackState == OwnerDrawBridgeStopState.StoppedResident;
                    return OwnerDrawResult<OwnerDrawBridgeSession>.Failed(
                        OwnerDrawFailureCode.BridgeStartFailed,
                        "Owner-draw Bridge accepted StartWork but failed its PING "
                        + "health check adapter=" + descriptor.AdapterId + "; "
                        + architectureStatus + "; " + routeStatus + "; "
                        + startStatus + "; "
                        + probeStatus + "; "
                        + (rollbackConfirmed
                            ? "rollback EndWork completed; the DLL remains resident. "
                            : "rollback EndWork was not confirmed; restart the target "
                                + "before retrying. ")
                        + rollbackStatus);
                }

                var session = new OwnerDrawBridgeSession
                {
                    AdapterId = descriptor.AdapterId,
                    ProcessId = processId,
                    HostHwnd = hostHwnd,
                    Architecture = targetArchitecture,
                    BridgePath = bridgePath,
                    StartedUtc = DateTime.UtcNow,
                    ProcessStartTimeUtc = processStartTimeUtc,
                };
                sessions[key] = session;
                return OwnerDrawResult<OwnerDrawBridgeSession>.Success(session,
                    "Owner-draw bridge ready adapter=" + descriptor.AdapterId
                    + " path=\"" + bridgePath + "\"; " + architectureStatus
                    + "; " + routeStatus + "; " + startStatus + "; "
                    + probeStatus);
            }
        }

        internal void Invalidate(string adapterId, int processId)
        {
            // A PID can exit or be reused between a pipe failure and invalidation.
            // Only one live process can own the PID, so clearing both target-arch
            // keys is the PID-reuse-safe behaviour.
            RemoveAllArchitectureKeys(adapterId, processId);
        }

        internal void Invalidate(string adapterId, int processId,
            OwnerDrawArchitecture targetArchitecture)
        {
            lock (gate)
                sessions.Remove(MakeKey(adapterId, processId, targetArchitecture));
        }

        /// <summary>
        /// Reversible stop only. The injected module deliberately remains resident;
        /// replacing/removing it requires restarting the target process.
        /// </summary>
        internal OwnerDrawResult<bool> StopResident(string adapterId, int processId,
            OwnerDrawBridgeDescriptor descriptor = null)
        {
            if (!OwnerDrawWindowProbe.TryGetArchitecture(processId,
                out OwnerDrawArchitecture architecture, out string architectureStatus))
            {
                RemoveAllArchitectureKeys(adapterId, processId);
                return OwnerDrawResult<bool>.Failed(
                    OwnerDrawFailureCode.ArchitectureUnknown, architectureStatus);
            }
            OwnerDrawArchitecture controllerArchitecture = Environment.Is64BitProcess
                ? OwnerDrawArchitecture.X64 : OwnerDrawArchitecture.X86;
            if (!OwnerDrawBridgeArchitecturePolicy.TryResolve(controllerArchitecture,
                architecture, descriptor, out _, out string routeStatus))
            {
                RemoveAllArchitectureKeys(adapterId, processId);
                return OwnerDrawResult<bool>.Failed(
                    OwnerDrawFailureCode.ArchitectureMismatch,
                    routeStatus + " pid=" + processId + ".");
            }
            string key = MakeKey(adapterId, processId, architecture);
            lock (gate)
            {
                sessions.TryGetValue(key, out OwnerDrawBridgeSession session);
                string bridgePath;
                if (session != null)
                {
                    bridgePath = session.BridgePath;
                }
                else if (descriptor != null)
                {
                    bridgePath = Path.Combine(bridgeDirectory,
                        descriptor.GetFileName(architecture));
                }
                else
                {
                    return OwnerDrawResult<bool>.Failed(OwnerDrawFailureCode.TargetUnavailable,
                        "No active owner-draw session exists for adapter=" + adapterId
                        + " pid=" + processId + ".");
                }
                int requiredAbi = descriptor != null
                    ? descriptor.RequiredAbi : 0;
                if (requiredAbi <= 0)
                {
                    return OwnerDrawResult<bool>.Failed(
                        OwnerDrawFailureCode.InvalidRequest,
                        "Bridge stop requires an exact ABI descriptor; DLL remains resident.");
                }
                if (!TryCheckLegacyConflictCore(processId, descriptor,
                    out bool legacyConflict, out string legacyStatus))
                {
                    sessions.Remove(key);
                    return OwnerDrawResult<bool>.Failed(
                        OwnerDrawFailureCode.BridgeStopped,
                        "Legacy Bridge conflict inspection failed; refusing EndWork. "
                        + legacyStatus);
                }
                if (legacyConflict)
                {
                    sessions.Remove(key);
                    return OwnerDrawResult<bool>.Failed(
                        OwnerDrawFailureCode.LegacyConflict, legacyStatus);
                }
                if (!activator.TryStopResident(processId, bridgePath,
                    descriptor.LegacyFileName, requiredAbi,
                    out OwnerDrawBridgeStopState stopState, out string stopStatus))
                {
                    sessions.Remove(key);
                    if (stopState == OwnerDrawBridgeStopState.LegacyConflict)
                    {
                        return OwnerDrawResult<bool>.Failed(
                            OwnerDrawFailureCode.LegacyConflict, stopStatus);
                    }
                    return OwnerDrawResult<bool>.Failed(OwnerDrawFailureCode.BridgeStopped,
                        "Bridge stop was not confirmed; DLL remains resident. "
                        + stopStatus);
                }
                sessions.Remove(key);
                if (stopState == OwnerDrawBridgeStopState.NotLoaded)
                {
                    return OwnerDrawResult<bool>.Success(true,
                        "Bridge was not loaded; no stop action was required. "
                        + routeStatus + " " + stopStatus);
                }
                return OwnerDrawResult<bool>.Success(true,
                    "Bridge stopped; DLL remains resident and can be resumed. "
                    + routeStatus + " " + stopStatus);
            }
        }

        internal bool TryGetSession(string adapterId, int processId,
            OwnerDrawBridgeDescriptor descriptor,
            out OwnerDrawBridgeSession session, out string status)
        {
            session = null;
            if (!OwnerDrawWindowProbe.TryGetArchitecture(processId,
                out OwnerDrawArchitecture targetArchitecture,
                out string architectureStatus))
            {
                RemoveAllArchitectureKeys(adapterId, processId);
                status = architectureStatus;
                return false;
            }
            OwnerDrawArchitecture controllerArchitecture = Environment.Is64BitProcess
                ? OwnerDrawArchitecture.X64 : OwnerDrawArchitecture.X86;
            if (!OwnerDrawBridgeArchitecturePolicy.TryResolve(controllerArchitecture,
                targetArchitecture, descriptor, out _, out string routeStatus))
            {
                RemoveAllArchitectureKeys(adapterId, processId);
                status = routeStatus;
                return false;
            }
            lock (gate)
            {
                string key = MakeKey(adapterId, processId, targetArchitecture);
                if (!TryCheckLegacyConflictCore(processId, descriptor,
                    out bool legacyConflict, out string legacyStatus))
                {
                    sessions.Remove(key);
                    status = "Legacy Bridge conflict inspection failed; cached "
                        + "session was invalidated. " + legacyStatus;
                    return false;
                }
                if (legacyConflict)
                {
                    sessions.Remove(key);
                    status = legacyStatus;
                    return false;
                }
                if (!sessions.TryGetValue(key, out session))
                {
                    status = "No managed Bridge session exists.";
                    return false;
                }
                if (!TryGetProcessStartTime(processId, out DateTime startTimeUtc)
                    || startTimeUtc != session.ProcessStartTimeUtc)
                {
                    RemoveAllArchitectureKeys(adapterId, processId);
                    session = null;
                    status = "Bridge session was invalidated because the target "
                        + "process exited or its PID was reused.";
                    return false;
                }
                if (descriptor == null || !string.Equals(descriptor.AdapterId,
                    adapterId, StringComparison.OrdinalIgnoreCase))
                {
                    session = null;
                    status = "Bridge session health check requires its matching descriptor.";
                    return false;
                }
                if (!TryProbe(descriptor, processId, targetArchitecture, out status))
                {
                    sessions.Remove(key);
                    session = null;
                    status = "Bridge session PING failed and the cached session was "
                        + "invalidated; " + status;
                    return false;
                }
                status = "Bridge session is live; " + architectureStatus + "; "
                    + routeStatus + "; " + status;
                return true;
            }
        }

        internal bool TryGetLegacyConflict(string adapterId, int processId,
            OwnerDrawBridgeDescriptor descriptor, out bool conflict,
            out string status)
        {
            conflict = false;
            if (!OwnerDrawWindowProbe.TryGetArchitecture(processId,
                out OwnerDrawArchitecture targetArchitecture,
                out string architectureStatus))
            {
                RemoveAllArchitectureKeys(adapterId, processId);
                status = architectureStatus;
                return false;
            }
            OwnerDrawArchitecture controllerArchitecture = Environment.Is64BitProcess
                ? OwnerDrawArchitecture.X64 : OwnerDrawArchitecture.X86;
            if (!OwnerDrawBridgeArchitecturePolicy.TryResolve(controllerArchitecture,
                targetArchitecture, descriptor, out _, out string routeStatus))
            {
                RemoveAllArchitectureKeys(adapterId, processId);
                status = routeStatus;
                return false;
            }
            lock (gate)
            {
                bool inspected = TryCheckLegacyConflictCore(processId, descriptor,
                    out conflict, out status);
                if (!inspected || conflict)
                {
                    sessions.Remove(MakeKey(adapterId, processId,
                        targetArchitecture));
                }
                status = architectureStatus + "; " + routeStatus + "; " + status;
                return inspected;
            }
        }

        private static bool TryProbe(OwnerDrawBridgeDescriptor descriptor,
            int processId, OwnerDrawArchitecture expectedArchitecture,
            out string status)
        {
            try
            {
                return descriptor.HealthProbe(processId, expectedArchitecture,
                    out status);
            }
            catch (Exception ex)
            {
                status = "Bridge PING threw " + ex.GetType().Name + ": "
                    + ex.Message;
                return false;
            }
        }

        private bool TryCheckLegacyConflictCore(int processId,
            OwnerDrawBridgeDescriptor descriptor, out bool conflict,
            out string status)
        {
            conflict = false;
            if (descriptor == null
                || string.IsNullOrWhiteSpace(descriptor.LegacyFileName))
            {
                status = "Legacy Bridge conflict check requires a descriptor and "
                    + "an exact legacy module name.";
                return false;
            }
            try
            {
                return activator.TryCheckLegacyConflict(processId,
                    descriptor.LegacyFileName, out conflict, out status);
            }
            catch (Exception ex)
            {
                status = "Legacy Bridge conflict check threw "
                    + ex.GetType().Name + ": " + ex.Message;
                return false;
            }
        }

        internal string BridgeDirectory
        {
            get { return bridgeDirectory; }
        }

        private static string MakeKey(string adapterId, int processId,
            OwnerDrawArchitecture architecture)
        {
            return (adapterId ?? string.Empty) + ":" + processId + ":"
                + ToText(architecture);
        }

        private static bool IsProcessAlive(int processId)
        {
            try
            {
                using (Process process = Process.GetProcessById(processId))
                    return !process.HasExited;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryGetProcessStartTime(int processId,
            out DateTime startTimeUtc)
        {
            startTimeUtc = DateTime.MinValue;
            try
            {
                using (Process process = Process.GetProcessById(processId))
                {
                    if (process.HasExited) return false;
                    startTimeUtc = process.StartTime.ToUniversalTime();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// StartWork creates the native pipe server on a worker thread. A successful
        /// start can therefore race the first PING by a few milliseconds. Retry only
        /// allow-listed pipe-publication failures within a short overall deadline.
        /// Protocol/identity/authentication and pipe read/write failures are terminal;
        /// normal cached-session health checks also remain single-shot.
        /// </summary>
        private bool TryProbeAfterStart(OwnerDrawBridgeDescriptor descriptor,
            int processId, OwnerDrawArchitecture expectedArchitecture,
            out string status)
        {
            var watch = Stopwatch.StartNew();
            string lastStatus = string.Empty;
            string stopReason = "attempt-limit";
            int attemptsPerformed = 0;
            for (int attempt = 1; attempt <= startProbeAttempts; attempt++)
            {
                attemptsPerformed = attempt;
                if (TryProbe(descriptor, processId, expectedArchitecture,
                    out lastStatus))
                {
                    status = "Bridge startup PING ready attempt=" + attempt
                        + " elapsedMs=" + watch.ElapsedMilliseconds + "; "
                        + lastStatus;
                    return true;
                }

                if (!IsTransientStartupProbeFailure(lastStatus))
                {
                    stopReason = "terminal-failure";
                    break;
                }
                if (attempt >= startProbeAttempts)
                    break;

                long remainingMs = startProbeDeadlineMs
                    - watch.ElapsedMilliseconds;
                if (remainingMs <= 0)
                {
                    stopReason = "retry-deadline";
                    break;
                }
                if (startProbeDelayMs > 0)
                {
                    Thread.Sleep((int)Math.Min(startProbeDelayMs, remainingMs));
                    if (watch.ElapsedMilliseconds >= startProbeDeadlineMs)
                    {
                        stopReason = "retry-deadline";
                        break;
                    }
                }
            }
            status = "Bridge startup PING did not become ready attempts="
                + attemptsPerformed + " elapsedMs=" + watch.ElapsedMilliseconds
                + " stopReason=" + stopReason + "; last=" + lastStatus;
            return false;
        }

        /// <summary>
        /// Health probes expose diagnostic text rather than a typed transport error.
        /// Keep this deliberately fail-closed: only conditions that can occur while
        /// the freshly-created pipe is being published are eligible for retry.
        /// </summary>
        internal static bool IsTransientStartupProbeFailure(string status)
        {
            if (string.IsNullOrWhiteSpace(status)) return false;

            string value = status.ToLowerInvariant();
            if (ContainsAny(value,
                "success=false", "protocol", "identity", "mismatch",
                "rejected", "unauthorized", "access denied", "authentication",
                "getnamedpipeserverprocessid", "endpoint pid", "abi=",
                "buildid", "adapter=", "expectedarch", "arch=", "running=",
                "nativestatus", "malformed", "invaliddataexception",
                "decoderfallbackexception", "pipe read timed out",
                "pipe write timed out", "read timeout", "write timeout"))
            {
                return false;
            }

            if (ContainsAny(value,
                "returned an empty response", "pipe is not published",
                "pipe not published", "pipe is not ready", "pipe not ready",
                "all pipe instances are busy", "pipe is being closed",
                "pipe has been ended", "broken pipe",
                "no process is on the other end", "cannot find the file specified",
                "system cannot find the file specified"))
            {
                return true;
            }

            // NamedPipeClientStream.Connect(timeout) surfaces a plain
            // TimeoutException. Explicit read/write timeout messages were rejected
            // above because repeating a three-second I/O timeout is never useful.
            return value.IndexOf("timeoutexception", StringComparison.Ordinal) >= 0;
        }

        private static bool ContainsAny(string value, params string[] fragments)
        {
            foreach (string fragment in fragments)
            {
                if (value.IndexOf(fragment, StringComparison.Ordinal) >= 0)
                    return true;
            }
            return false;
        }

        private void RemoveAllArchitectureKeys(string adapterId, int processId)
        {
            lock (gate)
            {
                sessions.Remove(MakeKey(adapterId, processId,
                    OwnerDrawArchitecture.X86));
                sessions.Remove(MakeKey(adapterId, processId,
                    OwnerDrawArchitecture.X64));
            }
        }

        private static int GetCurrentControllerProcessId()
        {
            using (Process process = Process.GetCurrentProcess())
                return process.Id;
        }

        private static string ToText(OwnerDrawArchitecture architecture)
        {
            return architecture == OwnerDrawArchitecture.X64 ? "x64" : "x86";
        }
    }
}
