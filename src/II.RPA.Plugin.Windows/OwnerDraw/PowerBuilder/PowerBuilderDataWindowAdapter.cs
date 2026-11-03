using System;
using System.Drawing;
using System.Text.RegularExpressions;

namespace II.RPA.Plugin.Windows.OwnerDraw.PowerBuilder
{
    internal sealed class PowerBuilderDataWindowAdapter : OwnerDrawAdapterBase
    {
        private static readonly Regex DataWindowClass = new Regex(
            "^pbdw[0-9]+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        internal static readonly OwnerDrawBridgeDescriptor Descriptor
            = new OwnerDrawBridgeDescriptor(
                OwnerDrawSelectorCodec.PowerBuilderAdapterId,
                "MfcGdiAccBridge.x86.dll",
                "MfcGdiAccBridge.x64.dll",
                "MfcGdiAccBridge.dll",
                PowerBuilderGdiBridgeClient.RequiredAbi,
                TryProbeBridge,
                allowX64ControllerToX86Target: true);

        private readonly OwnerDrawBridgeManager bridgeManager;
        private readonly PowerBuilderGdiBridgeClient bridgeClient;

        internal PowerBuilderDataWindowAdapter(OwnerDrawBridgeManager bridgeManager,
            PowerBuilderGdiBridgeClient bridgeClient = null)
        {
            this.bridgeManager = bridgeManager
                ?? throw new ArgumentNullException("bridgeManager");
            this.bridgeClient = bridgeClient ?? new PowerBuilderGdiBridgeClient();
        }

        public override string AdapterId
        {
            get { return OwnerDrawSelectorCodec.PowerBuilderAdapterId; }
        }

        public override int Priority
        {
            get { return 300; }
        }

        internal static bool IsPowerBuilderDataWindowClass(string className)
        {
            return !string.IsNullOrEmpty(className)
                && DataWindowClass.IsMatch(className);
        }

        private static bool TryProbeBridge(int processId,
            OwnerDrawArchitecture expectedArchitecture, out string status)
        {
            return new PowerBuilderGdiBridgeClient().TryPing(processId,
                expectedArchitecture, out status);
        }

        public override bool TryLocateHost(Point screenPoint, IntPtr candidateHwnd,
            out IntPtr hostHwnd, out string status)
        {
            return OwnerDrawWindowProbe.TryLocateHost(screenPoint, candidateHwnd,
                IsPowerBuilderDataWindowClass, out hostHwnd, out _, out _, out status);
        }

        public override OwnerDrawResult<OwnerDrawElementSnapshot> Pick(
            OwnerDrawPickRequest request)
        {
            if (!OwnerDrawFeatureSwitch.IsEnabled(AdapterId, out string enabledStatus))
                return OwnerDrawResult<OwnerDrawElementSnapshot>.NotApplicable(enabledStatus);
            if (request == null)
            {
                return OwnerDrawResult<OwnerDrawElementSnapshot>.NotApplicable(
                    "PowerBuilder pick request is null.");
            }
            if (!OwnerDrawWindowProbe.TryLocateHost(request.ScreenPoint,
                request.CandidateHwnd, IsPowerBuilderDataWindowClass,
                out IntPtr hostHwnd, out string hostClass, out int processId,
                out string probeStatus))
            {
                return OwnerDrawResult<OwnerDrawElementSnapshot>.NotApplicable(probeStatus);
            }

            OwnerDrawResult<OwnerDrawBridgeSession> ready
                = bridgeManager.EnsureReady(hostHwnd, Descriptor);
            if (!ready.IsSuccess)
                return Fail<OwnerDrawElementSnapshot>(ready.Code,
                    probeStatus + "; " + ready.Status);

            bool refreshed = bridgeClient.TryRefresh(processId, hostHwnd,
                out string refreshStatus);
            PowerBuilderGdiBridgeClient.BridgeCell picked = null;
            string pickStatus = string.Empty;
            bool pickedCell = refreshed && bridgeClient.TryGetCellAt(processId, hostHwnd,
                request.ScreenPoint, out picked, out pickStatus);
            if (!refreshed || !pickedCell)
            {
                bridgeManager.Invalidate(AdapterId, processId,
                    ready.Value.Architecture);
                OwnerDrawResult<OwnerDrawBridgeSession> recovered
                    = bridgeManager.EnsureReady(hostHwnd, Descriptor);
                if (recovered.IsSuccess)
                {
                    refreshed = bridgeClient.TryRefresh(processId, hostHwnd,
                        out string retryRefreshStatus);
                    string retryPickStatus = string.Empty;
                    pickedCell = refreshed && bridgeClient.TryGetCellAt(processId,
                        hostHwnd, request.ScreenPoint, out picked,
                        out retryPickStatus);
                    refreshStatus += "; retry=" + retryRefreshStatus;
                    pickStatus += "; retry=" + retryPickStatus;
                }
                if (!refreshed || !pickedCell)
                {
                    return Fail<OwnerDrawElementSnapshot>(
                        OwnerDrawFailureCode.TransportFailure,
                        probeStatus + "; " + ready.Status + "; " + refreshStatus
                        + "; " + pickStatus + "; recovery=" + recovered.Status);
                }
            }

            if (!IsUsableCell(hostHwnd, request.ScreenPoint, picked,
                out string qualityStatus))
            {
                return Fail<OwnerDrawElementSnapshot>(OwnerDrawFailureCode.NotFound,
                    qualityStatus);
            }

            OwnerDrawSelector selector = CreateSelector(hostClass, picked);
            return OwnerDrawResult<OwnerDrawElementSnapshot>.Success(
                BuildSnapshot(request.RequestId, processId, hostHwnd, hostClass,
                    selector, picked),
                probeStatus + "; index=" + picked.Index + " bounds="
                    + picked.ScreenBounds + " textLength=" + picked.Text.Length
                    + "; " + qualityStatus + "; " + pickStatus);
        }

        public override OwnerDrawResult<OwnerDrawElementSnapshot> Resolve(
            IntPtr hostHwnd, OwnerDrawSelector selector)
        {
            if (!CanResolve(selector))
                return OwnerDrawResult<OwnerDrawElementSnapshot>.NotApplicable();
            if (!OwnerDrawFeatureSwitch.IsEnabled(AdapterId, out string enabledStatus))
                return Fail<OwnerDrawElementSnapshot>(OwnerDrawFailureCode.BridgeStopped,
                    enabledStatus);
            if (!OwnerDrawSelectorCodec.TryValidate(selector, out string selectorStatus))
                return Fail<OwnerDrawElementSnapshot>(OwnerDrawFailureCode.InvalidSelector,
                    selectorStatus);
            if (!OwnerDrawWindowProbe.TryValidateHost(hostHwnd,
                IsPowerBuilderDataWindowClass, out hostHwnd, out string hostClass,
                out int processId, out string hostStatus))
            {
                return Fail<OwnerDrawElementSnapshot>(OwnerDrawFailureCode.TargetUnavailable,
                    hostStatus);
            }
            if (!string.IsNullOrEmpty(selector.HostClassName)
                && !string.Equals(selector.HostClassName, hostClass,
                    StringComparison.OrdinalIgnoreCase))
            {
                return Fail<OwnerDrawElementSnapshot>(OwnerDrawFailureCode.IdentityMismatch,
                    "PowerBuilder host class mismatch expected=\""
                    + selector.HostClassName + "\" actual=\"" + hostClass + "\".");
            }

            OwnerDrawResult<OwnerDrawBridgeSession> ready
                = bridgeManager.EnsureReady(hostHwnd, Descriptor);
            if (!ready.IsSuccess)
                return Fail<OwnerDrawElementSnapshot>(ready.Code, ready.Status);
            if (!bridgeClient.TryResolveCell(processId, hostHwnd,
                selector, out PowerBuilderGdiBridgeClient.BridgeCell cell,
                out OwnerDrawFailureCode resolveCode, out string resolveStatus))
            {
                if (!IsBridgeRecoveryCandidate(resolveCode))
                {
                    return Fail<OwnerDrawElementSnapshot>(resolveCode,
                        "PowerBuilder selector did not resolve; " + resolveStatus);
                }
                bridgeManager.Invalidate(AdapterId, processId,
                    ready.Value.Architecture);
                OwnerDrawResult<OwnerDrawBridgeSession> recovered
                    = bridgeManager.EnsureReady(hostHwnd, Descriptor);
                string retryResolveStatus = string.Empty;
                OwnerDrawFailureCode retryResolveCode = resolveCode;
                bool retryResolved = recovered.IsSuccess
                    && bridgeClient.TryResolveCell(processId, hostHwnd,
                        selector, out cell, out retryResolveCode,
                        out retryResolveStatus);
                resolveStatus += "; retry=" + retryResolveStatus;
                if (!retryResolved)
                {
                    OwnerDrawFailureCode finalCode = recovered.IsSuccess
                        ? retryResolveCode : recovered.Code;
                    return Fail<OwnerDrawElementSnapshot>(finalCode,
                        "PowerBuilder selector did not resolve; " + resolveStatus
                        + "; recovery=" + recovered.Status);
                }
            }
            if (selector.EnforceText && !string.Equals(selector.ExpectedText ?? string.Empty,
                cell.Text ?? string.Empty, StringComparison.Ordinal))
            {
                return Fail<OwnerDrawElementSnapshot>(OwnerDrawFailureCode.IdentityMismatch,
                    "PowerBuilder exact text guard failed for kind="
                    + selector.PbKind + ".");
            }
            if (!IsUsableCell(hostHwnd, null, cell, out string qualityStatus))
                return Fail<OwnerDrawElementSnapshot>(OwnerDrawFailureCode.ProtocolMismatch,
                    qualityStatus);

            return OwnerDrawResult<OwnerDrawElementSnapshot>.Success(
                BuildSnapshot(Guid.NewGuid().ToString("N"), processId, hostHwnd,
                    hostClass, selector, cell), hostStatus + "; " + qualityStatus
                    + "; " + resolveStatus);
        }

        private static OwnerDrawElementSnapshot BuildSnapshot(string requestId,
            int processId, IntPtr hostHwnd, string hostClass,
            OwnerDrawSelector selector, PowerBuilderGdiBridgeClient.BridgeCell cell)
        {
            return new OwnerDrawElementSnapshot
            {
                RequestId = requestId,
                AdapterId = OwnerDrawSelectorCodec.PowerBuilderAdapterId,
                ProcessId = processId,
                HostHwnd = hostHwnd,
                HostClassName = hostClass,
                ScreenBounds = cell.ScreenBounds,
                ClientBounds = cell.ClientBounds,
                Text = cell.Text ?? string.Empty,
                TextAvailable = cell.TextAvailable,
                Selector = selector,
                NativeStatus = cell.NativeStatus ?? string.Empty,
            };
        }

        private static OwnerDrawSelector CreateSelector(string hostClass,
            PowerBuilderGdiBridgeClient.BridgeCell cell)
        {
            var selector = new OwnerDrawSelector
            {
                SchemaVersion = OwnerDrawSelectorCodec.CurrentSchemaVersion,
                AdapterId = OwnerDrawSelectorCodec.PowerBuilderAdapterId,
                HostClassName = hostClass,
                IdentityStrength = OwnerDrawIdentityStrength.LogicalCell,
                PbKind = ParseKind(cell.Kind),
                ExpectedText = cell.Text ?? string.Empty,
                EnforceText = false,
            };
            if (selector.PbKind == PowerBuilderCellKind.GridCell)
            {
                selector.PbRow = cell.Row;
                selector.PbCol = cell.Col;
                selector.PbField = cell.Field ?? string.Empty;
            }
            else if (selector.PbKind == PowerBuilderCellKind.Input)
            {
                selector.CellClientBounds = cell.ClientBounds;
            }
            else
            {
                selector.EnforceText = true; // label identity is the exact text
            }
            return selector;
        }

        private static PowerBuilderCellKind ParseKind(string kind)
        {
            if (string.Equals(kind, "label", StringComparison.OrdinalIgnoreCase))
                return PowerBuilderCellKind.Label;
            if (string.Equals(kind, "input", StringComparison.OrdinalIgnoreCase))
                return PowerBuilderCellKind.Input;
            return PowerBuilderCellKind.GridCell;
        }

        private static bool IsBridgeRecoveryCandidate(OwnerDrawFailureCode code)
        {
            return code == OwnerDrawFailureCode.TransportFailure
                || code == OwnerDrawFailureCode.ProtocolMismatch
                || code == OwnerDrawFailureCode.BridgeStopped
                || code == OwnerDrawFailureCode.BridgeStartFailed;
        }

        private static bool IsUsableCell(IntPtr hostHwnd, Point? pickPoint,
            PowerBuilderGdiBridgeClient.BridgeCell cell, out string status)
        {
            if (cell == null || cell.Index < 0 || cell.ScreenBounds.Width <= 0
                || cell.ScreenBounds.Height <= 0)
            {
                status = "PowerBuilder bridge returned an invalid unified cell.";
                return false;
            }
            if (pickPoint.HasValue && !cell.ScreenBounds.Contains(pickPoint.Value))
            {
                status = "PowerBuilder unified cell does not contain the picked point.";
                return false;
            }
            if (OwnerDrawWindowProbe.TryGetWindowBounds(hostHwnd,
                out Rectangle hostBounds) && hostBounds.Width > 0 && hostBounds.Height > 0)
            {
                double ratio = (double)cell.ScreenBounds.Width * cell.ScreenBounds.Height
                    / ((double)hostBounds.Width * hostBounds.Height);
                if (ratio >= 0.72)
                {
                    status = "PowerBuilder candidate covers most of the DataWindow "
                        + "(areaRatio=" + ratio.ToString("0.000") + ") and was rejected.";
                    return false;
                }
                status = "PowerBuilder unified cell accepted areaRatio="
                    + ratio.ToString("0.000") + ".";
                return true;
            }
            status = "PowerBuilder unified cell accepted; host bounds unavailable.";
            return true;
        }

        private static OwnerDrawResult<T> Fail<T>(OwnerDrawFailureCode code,
            string status)
        {
            return OwnerDrawResult<T>.Failed(code, status);
        }
    }
}
