using System;

namespace II.RPA.Plugin.Windows.OwnerDraw
{
    /// <summary>Stable public lifecycle state without exposing Bridge internals.</summary>
    public enum OwnerDrawBridgeState
    {
        Unsupported,
        Disabled,
        StoppedOrNotStarted,
        Running,
        LegacyConflict,
    }

    public sealed class OwnerDrawBridgeStatus
    {
        public string AdapterId { get; internal set; }

        public int ProcessId { get; internal set; }

        public string Architecture { get; internal set; }

        public OwnerDrawBridgeState State { get; internal set; }

        public string Detail { get; internal set; }
    }

    /// <summary>
    /// Reversible Bridge controls for product UI and diagnostics.  Stop restores
    /// hooks and stops the pipe but intentionally keeps the DLL resident; removing
    /// or replacing an injected Bridge requires restarting the target process.
    /// </summary>
    public static class OwnerDrawBridgeLifecycle
    {
        public static bool TryResume(IntPtr hostHwnd, out string status)
        {
            OwnerDrawResult<bool> result = OwnerDrawBridgeControl.Resume(hostHwnd);
            status = result.Status;
            return result.IsSuccess;
        }

        public static bool TryStop(IntPtr hostHwnd, out string status)
        {
            OwnerDrawResult<bool> result = OwnerDrawBridgeControl.Stop(hostHwnd);
            status = result.Status;
            return result.IsSuccess;
        }

        public static OwnerDrawBridgeStatus QueryStatus(IntPtr hostHwnd)
        {
            OwnerDrawBridgeRuntimeStatus value
                = OwnerDrawBridgeControl.GetStatus(hostHwnd);
            return new OwnerDrawBridgeStatus
            {
                AdapterId = value.AdapterId ?? string.Empty,
                ProcessId = value.ProcessId,
                Architecture = value.ProcessId <= 0 ? string.Empty
                    : (value.Architecture == OwnerDrawArchitecture.X64
                        ? "x64" : "x86"),
                State = (OwnerDrawBridgeState)value.State,
                Detail = value.Status ?? string.Empty,
            };
        }
    }
}
