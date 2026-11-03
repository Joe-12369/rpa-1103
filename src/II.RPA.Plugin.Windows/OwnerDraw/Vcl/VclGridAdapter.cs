using System;
using System.Drawing;

namespace II.RPA.Plugin.Windows.OwnerDraw.Vcl
{
    internal sealed class VclGridAdapter : OwnerDrawAdapterBase
    {
        private static readonly string[] GridClassTokens =
        {
            "TFDBGrid",
            "TDsDBTreeGrid",
            "TDBGrid",
            "TwwDBGrid",
            "TAdvStringGrid",
        };

        internal static readonly OwnerDrawBridgeDescriptor Descriptor
            = new OwnerDrawBridgeDescriptor(
                OwnerDrawSelectorCodec.VclAdapterId,
                "VclGridAccBridge.x86.dll",
                "VclGridAccBridge.x64.dll",
                "VclGridAccBridge.dll",
                VclGridBridgeClient.RequiredAbi,
                TryProbeBridge);

        private readonly OwnerDrawBridgeManager bridgeManager;
        private readonly VclGridBridgeClient bridgeClient;

        internal VclGridAdapter(OwnerDrawBridgeManager bridgeManager,
            VclGridBridgeClient bridgeClient = null)
        {
            this.bridgeManager = bridgeManager
                ?? throw new ArgumentNullException("bridgeManager");
            this.bridgeClient = bridgeClient ?? new VclGridBridgeClient();
        }

        public override string AdapterId
        {
            get { return OwnerDrawSelectorCodec.VclAdapterId; }
        }

        public override int Priority
        {
            get { return 200; }
        }

        internal static bool IsLogicalVclGridClass(string className)
        {
            if (string.IsNullOrEmpty(className)
                || className.IndexOf("InplaceEdit",
                    StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            foreach (string token in GridClassTokens)
            {
                if (className.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        private static bool TryProbeBridge(int processId,
            OwnerDrawArchitecture expectedArchitecture, out string status)
        {
            return new VclGridBridgeClient().TryQueryCapabilities(processId,
                expectedArchitecture, out _, out status);
        }

        public override bool TryLocateHost(Point screenPoint, IntPtr candidateHwnd,
            out IntPtr hostHwnd, out string status)
        {
            return OwnerDrawWindowProbe.TryLocateHost(screenPoint, candidateHwnd,
                IsLogicalVclGridClass, out hostHwnd, out _, out _, out status);
        }

        public override OwnerDrawResult<OwnerDrawElementSnapshot> Pick(
            OwnerDrawPickRequest request)
        {
            if (!OwnerDrawFeatureSwitch.IsEnabled(AdapterId, out string enabledStatus))
                return OwnerDrawResult<OwnerDrawElementSnapshot>.NotApplicable(enabledStatus);
            if (request == null)
                return OwnerDrawResult<OwnerDrawElementSnapshot>.NotApplicable(
                    "VCL pick request is null.");
            if (!OwnerDrawWindowProbe.TryLocateHost(request.ScreenPoint,
                request.CandidateHwnd, IsLogicalVclGridClass,
                out IntPtr gridHwnd, out string gridClass, out int processId,
                out string probeStatus))
            {
                return OwnerDrawResult<OwnerDrawElementSnapshot>.NotApplicable(probeStatus);
            }

            OwnerDrawResult<OwnerDrawBridgeSession> ready
                = bridgeManager.EnsureReady(gridHwnd, Descriptor);
            if (!ready.IsSuccess)
                return Fail<OwnerDrawElementSnapshot>(ready.Code,
                    probeStatus + "; " + ready.Status);
            if (!TryGetCapabilities(gridHwnd, processId,
                ready.Value.Architecture,
                out VclGridBridgeClient.BridgeCapabilities capabilities,
                out string capabilityStatus))
            {
                return Fail<OwnerDrawElementSnapshot>(
                    OwnerDrawFailureCode.ProtocolMismatch,
                    probeStatus + "; " + ready.Status + "; " + capabilityStatus);
            }
            if (!capabilities.SupportsLogicalCellResolve)
            {
                return Fail<OwnerDrawElementSnapshot>(
                    OwnerDrawFailureCode.ProtocolMismatch,
                    "VCL Bridge does not advertise the strict logical-cell "
                    + "pickup/resolve path; " + capabilityStatus);
            }
            if (!OwnerDrawWindowProbe.TryScreenToClient(gridHwnd,
                request.ScreenPoint, out Point clientPoint))
            {
                return Fail<OwnerDrawElementSnapshot>(OwnerDrawFailureCode.TargetUnavailable,
                    "Cannot convert the picked screen point to VCL client coordinates.");
            }
            if (!bridgeClient.TryQuerySemanticCell(processId, gridHwnd,
                request.ScreenPoint, clientPoint, out VclGridBridgeClient.BridgeCell cell,
                out string cellStatus))
            {
                bridgeManager.Invalidate(AdapterId, processId,
                    ready.Value.Architecture);
                OwnerDrawResult<OwnerDrawBridgeSession> recovered
                    = bridgeManager.EnsureReady(gridHwnd, Descriptor);
                string retryStatus = string.Empty;
                bool retrySucceeded = recovered.IsSuccess
                    && bridgeClient.TryQuerySemanticCell(processId, gridHwnd,
                        request.ScreenPoint, clientPoint, out cell, out retryStatus);
                cellStatus += "; retry=" + retryStatus;
                if (!retrySucceeded)
                {
                    return Fail<OwnerDrawElementSnapshot>(
                        OwnerDrawFailureCode.TransportFailure,
                        probeStatus + "; " + ready.Status + "; " + cellStatus
                        + "; recovery=" + recovered.Status);
                }
            }
            if (!ValidateSemanticCell(cell, null, request.ScreenPoint,
                out string identityStatus))
            {
                return Fail<OwnerDrawElementSnapshot>(OwnerDrawFailureCode.IdentityMismatch,
                    identityStatus + "; " + cellStatus);
            }

            var selector = new OwnerDrawSelector
            {
                SchemaVersion = OwnerDrawSelectorCodec.CurrentSchemaVersion,
                AdapterId = AdapterId,
                HostClassName = gridClass,
                IdentityStrength = OwnerDrawIdentityStrength.LogicalCell,
                LogicalRow = cell.LogicalRow,
                LogicalColumn = cell.LogicalColumn,
                FieldName = cell.Field ?? string.Empty,
                IdentitySource = cell.IdentitySource ?? string.Empty,
                ExpectedText = cell.Text ?? string.Empty,
                EnforceText = false,
            };
            return OwnerDrawResult<OwnerDrawElementSnapshot>.Success(
                BuildSnapshot(request.RequestId, processId, gridHwnd, gridClass,
                    selector, cell), probeStatus + "; " + identityStatus
                    + "; textLength=" + cell.Text.Length + "; " + capabilityStatus
                    + "; " + cellStatus);
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
            if (!OwnerDrawWindowProbe.TryValidateHost(hostHwnd, IsLogicalVclGridClass,
                out IntPtr gridHwnd, out string gridClass, out int processId,
                out string hostStatus))
            {
                return Fail<OwnerDrawElementSnapshot>(OwnerDrawFailureCode.TargetUnavailable,
                    hostStatus);
            }
            if (!string.IsNullOrEmpty(selector.HostClassName)
                && !string.Equals(selector.HostClassName, gridClass,
                    StringComparison.OrdinalIgnoreCase))
            {
                return Fail<OwnerDrawElementSnapshot>(OwnerDrawFailureCode.IdentityMismatch,
                    "VCL grid class mismatch expected=\"" + selector.HostClassName
                    + "\" actual=\"" + gridClass + "\".");
            }

            OwnerDrawResult<OwnerDrawBridgeSession> ready
                = bridgeManager.EnsureReady(gridHwnd, Descriptor);
            if (!ready.IsSuccess)
                return Fail<OwnerDrawElementSnapshot>(ready.Code, ready.Status);
            if (!TryGetCapabilities(gridHwnd, processId,
                ready.Value.Architecture,
                out VclGridBridgeClient.BridgeCapabilities capabilities,
                out string capabilityStatus))
            {
                return Fail<OwnerDrawElementSnapshot>(
                    OwnerDrawFailureCode.ProtocolMismatch, capabilityStatus);
            }
            if (!capabilities.SupportsLogicalCellResolve)
            {
                return Fail<OwnerDrawElementSnapshot>(
                    OwnerDrawFailureCode.ProtocolMismatch,
                    "VCL Bridge does not advertise logical-cell resolve; "
                    + capabilityStatus);
            }
            if (!bridgeClient.TryResolveCell(processId, gridHwnd,
                selector.LogicalRow, selector.LogicalColumn, selector.FieldName,
                out VclGridBridgeClient.BridgeCell cell, out string resolveStatus))
            {
                bridgeManager.Invalidate(AdapterId, processId,
                    ready.Value.Architecture);
                OwnerDrawResult<OwnerDrawBridgeSession> recovered
                    = bridgeManager.EnsureReady(gridHwnd, Descriptor);
                string retryStatus = string.Empty;
                bool retrySucceeded = recovered.IsSuccess
                    && bridgeClient.TryResolveCell(processId, gridHwnd,
                        selector.LogicalRow, selector.LogicalColumn,
                        selector.FieldName, out cell, out retryStatus);
                resolveStatus += "; retry=" + retryStatus;
                if (!retrySucceeded)
                {
                    return Fail<OwnerDrawElementSnapshot>(OwnerDrawFailureCode.NotFound,
                        "VCL logical selector did not resolve; " + resolveStatus
                        + "; recovery=" + recovered.Status);
                }
            }
            if (!ValidateSemanticCell(cell, selector, null, out string identityStatus))
            {
                return Fail<OwnerDrawElementSnapshot>(OwnerDrawFailureCode.IdentityMismatch,
                    identityStatus + "; " + resolveStatus);
            }
            return OwnerDrawResult<OwnerDrawElementSnapshot>.Success(
                BuildSnapshot(Guid.NewGuid().ToString("N"), processId, gridHwnd,
                    gridClass, selector, cell), hostStatus + "; " + identityStatus
                    + "; " + capabilityStatus + "; " + resolveStatus);
        }

        public override OwnerDrawResult<string> GetText(IntPtr hostHwnd,
            OwnerDrawSelector selector)
        {
            if (!CanResolve(selector)) return OwnerDrawResult<string>.NotApplicable();
            if (!OwnerDrawFeatureSwitch.IsEnabled(AdapterId, out string enabledStatus))
                return Fail<string>(OwnerDrawFailureCode.BridgeStopped, enabledStatus);
            if (!OwnerDrawSelectorCodec.TryValidate(selector, out string selectorStatus))
                return Fail<string>(OwnerDrawFailureCode.InvalidSelector, selectorStatus);
            if (!OwnerDrawWindowProbe.TryValidateHost(hostHwnd, IsLogicalVclGridClass,
                out IntPtr gridHwnd, out _, out int processId, out string hostStatus))
                return Fail<string>(OwnerDrawFailureCode.TargetUnavailable, hostStatus);

            OwnerDrawResult<OwnerDrawBridgeSession> ready
                = bridgeManager.EnsureReady(gridHwnd, Descriptor);
            if (!ready.IsSuccess) return Fail<string>(ready.Code, ready.Status);
            if (!TryGetCapabilities(gridHwnd, processId,
                ready.Value.Architecture,
                out VclGridBridgeClient.BridgeCapabilities capabilities,
                out string capabilityStatus))
            {
                return Fail<string>(OwnerDrawFailureCode.ProtocolMismatch,
                    capabilityStatus);
            }

            string text = string.Empty;
            string field = string.Empty;
            string readStatus = string.Empty;
            bool readSucceeded = false;
            if (capabilities.LogicalCellRead && capabilities.OffscreenRead
                && selector.LogicalRow >= 0)
            {
                readSucceeded = bridgeClient.TryReadLogicalCell(processId, gridHwnd,
                    selector.LogicalRow, selector.LogicalColumn, out text,
                    out field, out readStatus);
            }
            if (!readSucceeded && capabilities.SupportsLogicalCellResolve)
            {
                string logicalReadStatus = readStatus;
                readSucceeded = bridgeClient.TryResolveCell(processId, gridHwnd,
                    selector.LogicalRow, selector.LogicalColumn, selector.FieldName,
                    out VclGridBridgeClient.BridgeCell resolved,
                    out string resolveReadStatus);
                readStatus = logicalReadStatus + "; resolveFallback="
                    + resolveReadStatus;
                if (readSucceeded)
                {
                    if (!ValidateSemanticCell(resolved, selector, null,
                        out string identityStatus))
                    {
                        return Fail<string>(OwnerDrawFailureCode.IdentityMismatch,
                            identityStatus + "; " + readStatus);
                    }
                    text = resolved.Text ?? string.Empty;
                    field = resolved.Field ?? string.Empty;
                }
            }
            if (!readSucceeded)
            {
                bridgeManager.Invalidate(AdapterId, processId,
                    ready.Value.Architecture);
                OwnerDrawResult<OwnerDrawBridgeSession> recovered
                    = bridgeManager.EnsureReady(gridHwnd, Descriptor);
                string retryStatus = string.Empty;
                bool retrySucceeded = false;
                if (recovered.IsSuccess
                    && bridgeClient.TryQueryCapabilities(processId,
                        recovered.Value.Architecture,
                        out VclGridBridgeClient.BridgeCapabilities retryCapabilities,
                        out string retryCapabilityStatus))
                {
                    if (retryCapabilities.LogicalCellRead
                        && retryCapabilities.OffscreenRead
                        && selector.LogicalRow >= 0)
                    {
                        retrySucceeded = bridgeClient.TryReadLogicalCell(processId,
                            gridHwnd, selector.LogicalRow, selector.LogicalColumn,
                            out text, out field, out retryStatus);
                    }
                    if (!retrySucceeded
                        && retryCapabilities.SupportsLogicalCellResolve)
                    {
                        retrySucceeded = bridgeClient.TryResolveCell(processId,
                            gridHwnd, selector.LogicalRow, selector.LogicalColumn,
                            selector.FieldName,
                            out VclGridBridgeClient.BridgeCell retryResolved,
                            out retryStatus);
                        if (retrySucceeded)
                        {
                            if (!ValidateSemanticCell(retryResolved, selector, null,
                                out string retryIdentityStatus))
                            {
                                return Fail<string>(
                                    OwnerDrawFailureCode.IdentityMismatch,
                                    retryIdentityStatus + "; " + retryStatus);
                            }
                            text = retryResolved.Text ?? string.Empty;
                            field = retryResolved.Field ?? string.Empty;
                        }
                    }
                    retryStatus = retryCapabilityStatus + "; " + retryStatus;
                }
                if (!retrySucceeded)
                {
                    return Fail<string>(OwnerDrawFailureCode.NotFound,
                        readStatus + "; retry=" + retryStatus
                        + "; recovery=" + recovered.Status);
                }
                readStatus += "; retry=" + retryStatus;
            }
            if (!string.IsNullOrEmpty(selector.FieldName)
                && !string.Equals(selector.FieldName, field, StringComparison.Ordinal))
            {
                return Fail<string>(OwnerDrawFailureCode.IdentityMismatch,
                    "VCL live text field mismatch for logical cell.");
            }
            if (selector.EnforceText && !string.Equals(selector.ExpectedText ?? string.Empty,
                text ?? string.Empty, StringComparison.Ordinal))
            {
                return Fail<string>(OwnerDrawFailureCode.IdentityMismatch,
                    "VCL exact text guard failed for logical cell.");
            }
            return OwnerDrawResult<string>.Success(text ?? string.Empty,
                hostStatus + "; textLength=" + (text == null ? 0 : text.Length)
                + "; " + capabilityStatus + "; " + readStatus);
        }

        internal OwnerDrawResult<VclGridBridgeClient.BridgeGridInfo> GetGridInfo(
            IntPtr hostHwnd)
        {
            if (!OwnerDrawFeatureSwitch.IsEnabled(AdapterId, out string enabledStatus))
                return Fail<VclGridBridgeClient.BridgeGridInfo>(
                    OwnerDrawFailureCode.BridgeStopped, enabledStatus);
            if (!OwnerDrawWindowProbe.TryValidateHost(hostHwnd, IsLogicalVclGridClass,
                out IntPtr gridHwnd, out _, out int processId, out string hostStatus))
                return Fail<VclGridBridgeClient.BridgeGridInfo>(
                    OwnerDrawFailureCode.TargetUnavailable, hostStatus);
            OwnerDrawResult<OwnerDrawBridgeSession> ready
                = bridgeManager.EnsureReady(gridHwnd, Descriptor);
            if (!ready.IsSuccess)
                return Fail<VclGridBridgeClient.BridgeGridInfo>(ready.Code, ready.Status);
            if (!TryGetCapabilities(gridHwnd, processId,
                ready.Value.Architecture,
                out VclGridBridgeClient.BridgeCapabilities capabilities,
                out string capabilityStatus))
            {
                return Fail<VclGridBridgeClient.BridgeGridInfo>(
                    OwnerDrawFailureCode.ProtocolMismatch, capabilityStatus);
            }
            if (!capabilities.LogicalMetadata)
            {
                return Fail<VclGridBridgeClient.BridgeGridInfo>(
                    OwnerDrawFailureCode.ProtocolMismatch,
                    "VCL Bridge does not advertise complete logical metadata for "
                    + "this architecture; " + capabilityStatus);
            }
            if (!bridgeClient.TryQueryGridInfo(processId, gridHwnd,
                out VclGridBridgeClient.BridgeGridInfo info, out string infoStatus))
            {
                bridgeManager.Invalidate(AdapterId, processId,
                    ready.Value.Architecture);
                OwnerDrawResult<OwnerDrawBridgeSession> recovered
                    = bridgeManager.EnsureReady(gridHwnd, Descriptor);
                string retryStatus = string.Empty;
                bool retrySucceeded = recovered.IsSuccess
                    && bridgeClient.TryQueryGridInfo(processId, gridHwnd,
                        out info, out retryStatus);
                infoStatus += "; retry=" + retryStatus;
                if (!retrySucceeded)
                {
                    return Fail<VclGridBridgeClient.BridgeGridInfo>(
                        OwnerDrawFailureCode.ProtocolMismatch,
                        infoStatus + "; recovery=" + recovered.Status);
                }
            }
            return OwnerDrawResult<VclGridBridgeClient.BridgeGridInfo>.Success(info,
                hostStatus + "; " + capabilityStatus + "; " + infoStatus);
        }

        private bool TryGetCapabilities(IntPtr gridHwnd, int processId,
            OwnerDrawArchitecture expectedArchitecture,
            out VclGridBridgeClient.BridgeCapabilities capabilities,
            out string status)
        {
            if (bridgeClient.TryQueryCapabilities(processId, expectedArchitecture,
                out capabilities,
                out status)) return true;

            string firstStatus = status;
            bridgeManager.Invalidate(AdapterId, processId,
                expectedArchitecture);
            OwnerDrawResult<OwnerDrawBridgeSession> recovered
                = bridgeManager.EnsureReady(gridHwnd, Descriptor);
            if (!recovered.IsSuccess)
            {
                capabilities = null;
                status = firstStatus + "; capability recovery failed: "
                    + recovered.Status;
                return false;
            }
            bool succeeded = bridgeClient.TryQueryCapabilities(processId,
                recovered.Value.Architecture,
                out capabilities, out string retryStatus);
            status = firstStatus + "; capability retry=" + retryStatus;
            return succeeded;
        }

        private static bool ValidateSemanticCell(VclGridBridgeClient.BridgeCell cell,
            OwnerDrawSelector selector, Point? pickedPoint, out string status)
        {
            if (cell == null || !cell.HasCellModel || !cell.HasLogicalIdentity
                || cell.LogicalRow < -1 || cell.LogicalColumn < 0
                || cell.ScreenBounds.Width <= 0 || cell.ScreenBounds.Height <= 0)
            {
                status = "VCL Bridge did not return an authoritative logical cell.";
                return false;
            }
            if (pickedPoint.HasValue && !cell.ScreenBounds.Contains(pickedPoint.Value))
            {
                status = "VCL logical cell does not contain the picked point.";
                return false;
            }
            if (selector != null)
            {
                if (cell.LogicalRow != selector.LogicalRow
                    || cell.LogicalColumn != selector.LogicalColumn)
                {
                    status = "VCL logical identity mismatch expected="
                        + selector.LogicalRow + "," + selector.LogicalColumn
                        + " actual=" + cell.LogicalRow + "," + cell.LogicalColumn + ".";
                    return false;
                }
                if (!string.IsNullOrEmpty(selector.FieldName)
                    && !string.Equals(selector.FieldName, cell.Field,
                        StringComparison.Ordinal))
                {
                    status = "VCL logical field mismatch.";
                    return false;
                }
                if (selector.EnforceText
                    && !string.Equals(selector.ExpectedText ?? string.Empty,
                        cell.Text ?? string.Empty, StringComparison.Ordinal))
                {
                    status = "VCL exact text guard failed.";
                    return false;
                }
            }
            status = "VCL authoritative logical cell accepted identity="
                + (cell.IdentitySource ?? string.Empty) + ".";
            return true;
        }

        private static OwnerDrawElementSnapshot BuildSnapshot(string requestId,
            int processId, IntPtr hostHwnd, string hostClass,
            OwnerDrawSelector selector, VclGridBridgeClient.BridgeCell cell)
        {
            return new OwnerDrawElementSnapshot
            {
                RequestId = requestId,
                AdapterId = OwnerDrawSelectorCodec.VclAdapterId,
                ProcessId = processId,
                HostHwnd = hostHwnd,
                HostClassName = hostClass,
                ScreenBounds = cell.ScreenBounds,
                ClientBounds = cell.ClientBounds,
                Text = cell.Text ?? string.Empty,
                TextAvailable = true,
                Selector = selector,
                NativeStatus = cell.NativeStatus ?? string.Empty,
            };
        }

        private static OwnerDrawResult<T> Fail<T>(OwnerDrawFailureCode code,
            string status)
        {
            return OwnerDrawResult<T>.Failed(code, status);
        }
    }
}
