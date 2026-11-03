using II.RPA.Plugin.Windows.OwnerDraw.PowerBuilder;
using II.RPA.Plugin.Windows.OwnerDraw.Vcl;
using System;

namespace II.RPA.Plugin.Windows.OwnerDraw
{
    internal enum OwnerDrawBridgeRuntimeState
    {
        Unsupported,
        Disabled,
        StoppedOrNotStarted,
        Running,
        LegacyConflict,
    }

    internal sealed class OwnerDrawBridgeRuntimeStatus
    {
        internal string AdapterId { get; set; }
        internal int ProcessId { get; set; }
        internal OwnerDrawArchitecture Architecture { get; set; }
        internal OwnerDrawBridgeRuntimeState State { get; set; }
        internal string Status { get; set; }
    }

    /// <summary>
    /// Product-facing lifecycle facade.
    /// Stop is resident-only; Resume reuses the same serialized Manager lifecycle.
    /// </summary>
    internal static class OwnerDrawBridgeControl
    {
        internal static OwnerDrawResult<bool> Resume(IntPtr hostHwnd)
        {
            if (!TryDescribe(hostHwnd, out string adapterId,
                out OwnerDrawBridgeDescriptor descriptor, out _, out string status))
            {
                return OwnerDrawResult<bool>.NotApplicable(status);
            }
            if (!OwnerDrawFeatureSwitch.IsEnabled(adapterId, out string enabledStatus))
            {
                return OwnerDrawResult<bool>.Failed(OwnerDrawFailureCode.BridgeStopped,
                    enabledStatus);
            }
            OwnerDrawResult<OwnerDrawBridgeSession> ready
                = OwnerDrawBridgeManager.Shared.EnsureReady(hostHwnd, descriptor);
            return ready.IsSuccess
                ? OwnerDrawResult<bool>.Success(true, ready.Status)
                : OwnerDrawResult<bool>.Failed(ready.Code, ready.Status);
        }

        internal static OwnerDrawResult<bool> Stop(IntPtr hostHwnd)
        {
            if (!TryDescribe(hostHwnd, out string adapterId, out OwnerDrawBridgeDescriptor descriptor,
                out int processId, out string status))
            {
                return OwnerDrawResult<bool>.NotApplicable(status);
            }
            return OwnerDrawBridgeManager.Shared.StopResident(adapterId, processId,
                descriptor);
        }

        internal static OwnerDrawBridgeRuntimeStatus GetStatus(IntPtr hostHwnd)
        {
            if (!TryDescribe(hostHwnd, out string adapterId,
                out OwnerDrawBridgeDescriptor descriptor,
                out int processId, out string description))
            {
                return new OwnerDrawBridgeRuntimeStatus
                {
                    State = OwnerDrawBridgeRuntimeState.Unsupported,
                    Status = description,
                };
            }
            return GetStatusForDescriptor(adapterId, descriptor, processId,
                description, OwnerDrawBridgeManager.Shared);
        }

        internal static OwnerDrawBridgeRuntimeStatus GetStatusForDescriptor(
            string adapterId, OwnerDrawBridgeDescriptor descriptor, int processId,
            string description, OwnerDrawBridgeManager bridgeManager)
        {
            if (descriptor == null || bridgeManager == null || processId <= 0)
            {
                return new OwnerDrawBridgeRuntimeStatus
                {
                    AdapterId = adapterId ?? string.Empty,
                    ProcessId = processId,
                    State = OwnerDrawBridgeRuntimeState.Unsupported,
                    Status = "Owner-draw Bridge status requires a live process, "
                        + "descriptor, and manager.",
                };
            }

            if (!OwnerDrawWindowProbe.TryGetArchitecture(processId,
                out OwnerDrawArchitecture architecture, out string architectureStatus))
            {
                return new OwnerDrawBridgeRuntimeStatus
                {
                    AdapterId = adapterId,
                    ProcessId = processId,
                    State = OwnerDrawBridgeRuntimeState.StoppedOrNotStarted,
                    Status = "Bridge status is unavailable and lifecycle operations "
                        + "will fail closed; " + architectureStatus + "; " + description,
                };
            }
            OwnerDrawArchitecture controllerArchitecture = Environment.Is64BitProcess
                ? OwnerDrawArchitecture.X64 : OwnerDrawArchitecture.X86;
            if (!OwnerDrawBridgeArchitecturePolicy.TryResolve(controllerArchitecture,
                architecture, descriptor, out _, out string routeStatus))
            {
                return new OwnerDrawBridgeRuntimeStatus
                {
                    AdapterId = adapterId,
                    ProcessId = processId,
                    Architecture = architecture,
                    State = OwnerDrawBridgeRuntimeState.Unsupported,
                    Status = routeStatus + "; " + description,
                };
            }

            if (!bridgeManager.TryGetLegacyConflict(adapterId, processId,
                descriptor, out bool legacyConflict, out string legacyStatus))
            {
                return new OwnerDrawBridgeRuntimeStatus
                {
                    AdapterId = adapterId,
                    ProcessId = processId,
                    Architecture = architecture,
                    State = OwnerDrawBridgeRuntimeState.StoppedOrNotStarted,
                    Status = "Legacy Bridge inspection failed; lifecycle operations "
                        + "will fail closed. " + legacyStatus + "; " + description,
                };
            }
            if (legacyConflict)
            {
                return LegacyConflictStatus(adapterId, processId, architecture,
                    legacyStatus, description);
            }

            if (!OwnerDrawFeatureSwitch.IsEnabled(adapterId, out string enabledStatus))
            {
                return new OwnerDrawBridgeRuntimeStatus
                {
                    AdapterId = adapterId,
                    ProcessId = processId,
                    Architecture = architecture,
                    State = OwnerDrawBridgeRuntimeState.Disabled,
                    Status = enabledStatus,
                };
            }
            if (bridgeManager.TryGetSession(adapterId, processId,
                descriptor, out OwnerDrawBridgeSession session,
                out string healthStatus))
            {
                return new OwnerDrawBridgeRuntimeStatus
                {
                    AdapterId = adapterId,
                    ProcessId = processId,
                    Architecture = session.Architecture,
                    State = OwnerDrawBridgeRuntimeState.Running,
                    Status = "Managed bridge session is active and responsive; "
                    + routeStatus + "; " + healthStatus + "; " + description,
                };
            }
            // Re-check after session inspection so a legacy DLL loaded during the
            // health check cannot be flattened into StoppedOrNotStarted.
            bool postSessionInspection = bridgeManager.TryGetLegacyConflict(
                adapterId, processId, descriptor, out legacyConflict,
                out legacyStatus);
            if (!postSessionInspection)
            {
                return new OwnerDrawBridgeRuntimeStatus
                {
                    AdapterId = adapterId,
                    ProcessId = processId,
                    Architecture = architecture,
                    State = OwnerDrawBridgeRuntimeState.StoppedOrNotStarted,
                    Status = "Legacy Bridge inspection failed after the session "
                        + "health check; lifecycle operations will fail closed. "
                        + legacyStatus + "; " + description,
                };
            }
            if (legacyConflict)
            {
                return LegacyConflictStatus(adapterId, processId, architecture,
                    legacyStatus, description);
            }
            return new OwnerDrawBridgeRuntimeStatus
            {
                AdapterId = adapterId,
                ProcessId = processId,
                Architecture = architecture,
                State = OwnerDrawBridgeRuntimeState.StoppedOrNotStarted,
                Status = "Bridge is stopped, not started, or not responding; "
                    + architectureStatus + "; " + routeStatus + "; " + description,
            };
        }

        private static OwnerDrawBridgeRuntimeStatus LegacyConflictStatus(
            string adapterId, int processId, OwnerDrawArchitecture architecture,
            string legacyStatus, string description)
        {
            return new OwnerDrawBridgeRuntimeStatus
            {
                AdapterId = adapterId,
                ProcessId = processId,
                Architecture = architecture,
                State = OwnerDrawBridgeRuntimeState.LegacyConflict,
                Status = legacyStatus + "; " + description,
            };
        }

        private static bool TryDescribe(IntPtr hostHwnd, out string adapterId,
            out OwnerDrawBridgeDescriptor descriptor, out int processId,
            out string status)
        {
            adapterId = string.Empty;
            descriptor = null;
            processId = 0;
            string className = OwnerDrawWindowProbe.GetClassName(hostHwnd);
            if (PowerBuilderDataWindowAdapter.IsPowerBuilderDataWindowClass(className))
            {
                adapterId = OwnerDrawSelectorCodec.PowerBuilderAdapterId;
                descriptor = PowerBuilderDataWindowAdapter.Descriptor;
            }
            else if (VclGridAdapter.IsLogicalVclGridClass(className))
            {
                adapterId = OwnerDrawSelectorCodec.VclAdapterId;
                descriptor = VclGridAdapter.Descriptor;
            }
            else
            {
                status = "Window class is not an owner-draw bridge host: \""
                    + className + "\".";
                return false;
            }
            if (!OwnerDrawWindowProbe.TryGetProcessId(hostHwnd, out processId))
            {
                status = "Cannot resolve owner-draw host process.";
                return false;
            }
            status = "Owner-draw host adapter=" + adapterId + " class=\""
                + className + "\" pid=" + processId + ".";
            return true;
        }
    }
}
