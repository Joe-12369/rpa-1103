using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;

namespace II.RPA.Plugin.Windows.OwnerDraw.PowerBuilder
{
    /// <summary>
    /// Strict client for the customer-validated MfcGdiAccBridge protocol used by
    /// PowerBuilder pbdwNN owner-drawn DataWindows.
    /// </summary>
    internal sealed class PowerBuilderGdiBridgeClient
    {
        internal const int RequiredAbi = 6;
        internal const string PipePrefix = "II.RPA.MfcGdiAccBridge.";
        private const int ConnectTimeoutMs = 250;
        private const int ReadTimeoutMs = 3000;
        private const int MaximumResponseBytes = 256 * 1024;

        private readonly IOwnerDrawBridgeTransport transport;

        internal PowerBuilderGdiBridgeClient(IOwnerDrawBridgeTransport transport = null)
        {
            this.transport = transport ?? new NamedPipeOwnerDrawBridgeTransport();
        }

        internal sealed class BridgeCell
        {
            internal int Index { get; set; } = -1;   // diagnostic only
            internal string Kind { get; set; } = string.Empty; // grid-cell | label | input
            internal int Row { get; set; } = -1;
            internal int Col { get; set; } = -1;
            internal string Field { get; set; } = string.Empty;
            internal string Text { get; set; } = string.Empty;
            internal bool TextAvailable { get; set; }
            internal Rectangle ScreenBounds { get; set; }
            internal Rectangle ClientBounds { get; set; }
            internal string Method { get; set; }
            internal string NativeStatus { get; set; }
        }

        internal bool TryPing(int processId,
            OwnerDrawArchitecture expectedArchitecture, out string status)
        {
            if (!TrySend(processId, "PING\tv=" + RequiredAbi,
                out string response,
                out string transportStatus))
            {
                status = transportStatus;
                return false;
            }
            return TryValidatePingResponse(response, expectedArchitecture,
                transportStatus, out status);
        }

        internal static bool TryValidatePingResponse(string response,
            OwnerDrawArchitecture expectedArchitecture, string transportStatus,
            out string status)
        {
            Dictionary<string, string> map = OwnerDrawProtocol.ParseMap(response);
            bool decoded = OwnerDrawProtocol.TryDecodeOptional(map, "status64",
                out string nativeStatus, out string decodeStatus);
            int abi = OwnerDrawProtocol.GetInt(map, "abi", 0);
            int buildId = OwnerDrawProtocol.GetInt(map, "buildId", 0);
            string adapter = OwnerDrawProtocol.Get(map, "adapter");
            string architecture = OwnerDrawProtocol.Get(map, "arch");
            int running = OwnerDrawProtocol.GetInt(map, "running", -1);
            string expectedArchitectureText = expectedArchitecture
                == OwnerDrawArchitecture.X64 ? "x64" : "x86";
            bool success = string.Equals(OwnerDrawProtocol.ResponseKind(response), "OK",
                StringComparison.OrdinalIgnoreCase) && decoded
                && string.Equals(nativeStatus, "pong", StringComparison.OrdinalIgnoreCase)
                && abi == RequiredAbi
                && unchecked((uint)buildId)
                    == NativeOwnerDrawBridgeActivator.RequiredPowerBuilderBridgeBuildId
                && string.Equals(adapter, OwnerDrawSelectorCodec.PowerBuilderAdapterId,
                    StringComparison.OrdinalIgnoreCase)
                && string.Equals(architecture, expectedArchitectureText,
                    StringComparison.OrdinalIgnoreCase)
                && running == 1;
            status = "PING success=" + success + " abi=" + abi
                + " buildId=0x" + unchecked((uint)buildId).ToString("X8")
                + " requiredBuildId=0x"
                + NativeOwnerDrawBridgeActivator.RequiredPowerBuilderBridgeBuildId
                    .ToString("X8")
                + " adapter=\"" + adapter + "\" arch=\"" + architecture
                + "\" expectedArch=\"" + expectedArchitectureText + "\" running="
                + running + " nativeStatus=\""
                + OwnerDrawProtocol.Preview(nativeStatus) + "\" " + decodeStatus
                + "; " + transportStatus;
            return success;
        }

        internal bool TryRefresh(int processId, IntPtr viewHwnd,
            out string status)
        {
            if (viewHwnd == IntPtr.Zero)
            {
                status = "REFRESH requires a non-zero PowerBuilder DataWindow hwnd.";
                return false;
            }
            string request = "REFRESH\tv=" + RequiredAbi + "\thwnd="
                + viewHwnd.ToInt64().ToString(CultureInfo.InvariantCulture);
            if (!TrySend(processId, request,
                out string response,
                out string transportStatus))
            {
                status = transportStatus;
                return false;
            }
            Dictionary<string, string> map = OwnerDrawProtocol.ParseMap(response);
            string kind = OwnerDrawProtocol.ResponseKind(response);
            int abi = OwnerDrawProtocol.GetInt(map, "abi", 0);
            int views = OwnerDrawProtocol.GetInt(map, "views", -1);
            OwnerDrawProtocol.TryDecodeOptional(map, "status64",
                out string nativeStatus, out string decodeStatus);
            bool success = string.Equals(kind, "OK_REFRESH",
                StringComparison.OrdinalIgnoreCase)
                && abi == RequiredAbi
                && views == 1;
            status = "REFRESH success=" + success + " abi=" + abi
                + " views=" + views + " kind=\"" + kind + "\" hwnd=0x"
                + viewHwnd.ToInt64().ToString("X") + " nativeStatus=\""
                + OwnerDrawProtocol.Short(nativeStatus) + "\" " + decodeStatus
                + "; " + transportStatus;
            return success;
        }

        internal bool TryGetCellAt(int processId, IntPtr viewHwnd,
            Point screenPoint, out BridgeCell cell, out string status)
        {
            string request = "GET_TEXT_AT\tv=" + RequiredAbi + "\thwnd="
                + viewHwnd.ToInt64().ToString(CultureInfo.InvariantCulture)
                + "\tscreenX=" + screenPoint.X.ToString(CultureInfo.InvariantCulture)
                + "\tscreenY=" + screenPoint.Y.ToString(CultureInfo.InvariantCulture);
            return TryQueryCell("GET_TEXT_AT", processId, request, null,
                out cell, out _, out status);
        }

        internal bool TryResolveCell(int processId, IntPtr viewHwnd,
            OwnerDrawSelector selector, out BridgeCell cell,
            out OwnerDrawFailureCode failureCode, out string status)
        {
            if (selector == null)
            {
                cell = null;
                failureCode = OwnerDrawFailureCode.InvalidSelector;
                status = "RESOLVE_CELL selector is null.";
                return false;
            }
            var guard = new ResolveGuard
            {
                Kind = selector.PbKind,
                Row = selector.PbRow,
                Col = selector.PbCol,
                ClientBounds = selector.CellClientBounds,
                EnforceText = selector.EnforceText,
                ExpectedText = selector.ExpectedText ?? string.Empty,
            };
            return TryResolveCellCore(processId, viewHwnd, guard,
                refreshBeforeResolve: true, out cell, out failureCode, out status);
        }

        private bool TryResolveCellCore(int processId, IntPtr viewHwnd,
            ResolveGuard guard, bool refreshBeforeResolve, out BridgeCell cell,
            out OwnerDrawFailureCode failureCode, out string status)
        {
            cell = null;
            failureCode = OwnerDrawFailureCode.None;
            if (guard == null)
            {
                failureCode = OwnerDrawFailureCode.InvalidRequest;
                status = "RESOLVE_CELL received a null guard.";
                return false;
            }
            bool hasGrid = guard.Kind == PowerBuilderCellKind.GridCell
                && guard.Row >= -1 && guard.Col >= 0;
            bool hasRect = guard.Kind == PowerBuilderCellKind.Input
                && guard.ClientBounds.Width > 0 && guard.ClientBounds.Height > 0;
            bool hasLabelText = guard.Kind == PowerBuilderCellKind.Label
                && !string.IsNullOrEmpty(guard.ExpectedText);
            if (!hasGrid && !hasRect && !hasLabelText)
            {
                failureCode = OwnerDrawFailureCode.InvalidSelector;
                status = "RESOLVE_CELL requires kind-specific identity.";
                return false;
            }
            string refreshStatus = "REFRESH reused the current capture.";
            if (refreshBeforeResolve
                && !TryRefresh(processId, viewHwnd, out refreshStatus))
            {
                failureCode = OwnerDrawFailureCode.TransportFailure;
                status = "RESOLVE_CELL aborted because refresh failed: " + refreshStatus;
                return false;
            }
            string request = "RESOLVE_CELL\tv=" + RequiredAbi
                + "\thwnd=" + viewHwnd.ToInt64().ToString(CultureInfo.InvariantCulture)
                + "\tkind=" + ToKindText(guard.Kind);
            if (hasGrid)
            {
                request += "\trow=" + guard.Row.ToString(CultureInfo.InvariantCulture)
                    + "\tcol=" + guard.Col.ToString(CultureInfo.InvariantCulture);
            }
            else if (hasRect)
            {
                request += "\texpectedLeft=" + guard.ClientBounds.Left
                    + "\texpectedTop=" + guard.ClientBounds.Top
                    + "\texpectedWidth=" + guard.ClientBounds.Width
                    + "\texpectedHeight=" + guard.ClientBounds.Height;
            }
            else
            {
                request += "\texpectedText64=" + Convert.ToBase64String(
                    Encoding.UTF8.GetBytes(guard.ExpectedText ?? string.Empty));
            }
            bool success = TryQueryCell("RESOLVE_CELL", processId, request,
                guard, out cell, out failureCode, out string resolveStatus);
            status = refreshStatus + "; " + resolveStatus;
            return success;
        }

        private bool TryQueryCell(string command, int processId, string request,
            ResolveGuard expected, out BridgeCell cell,
            out OwnerDrawFailureCode failureCode, out string status)
        {
            cell = null;
            failureCode = OwnerDrawFailureCode.None;
            if (!TrySend(processId, request, out string response,
                out string transportStatus))
            {
                failureCode = OwnerDrawFailureCode.TransportFailure;
                status = transportStatus;
                return false;
            }
            if (!TryParseCell(response, out cell, out failureCode,
                out string parseStatus))
            {
                status = command + " parse failed: " + parseStatus + "; "
                    + transportStatus;
                return false;
            }
            if (expected != null)
            {
                bool kindMatch = string.Equals(cell.Kind,
                    ToKindText(expected.Kind), StringComparison.OrdinalIgnoreCase);
                if (!kindMatch)
                {
                    failureCode = OwnerDrawFailureCode.IdentityMismatch;
                    status = command + " cell kind guard failed.";
                    cell = null;
                    return false;
                }
                if (expected.Kind == PowerBuilderCellKind.GridCell
                    && (cell.Row != expected.Row || cell.Col != expected.Col))
                {
                    failureCode = OwnerDrawFailureCode.IdentityMismatch;
                    status = command + " row/col guard failed.";
                    cell = null;
                    return false;
                }
                if (expected.Kind == PowerBuilderCellKind.Input
                    && cell.ClientBounds != expected.ClientBounds)
                {
                    failureCode = OwnerDrawFailureCode.IdentityMismatch;
                    status = command + " input geometry guard failed.";
                    cell = null;
                    return false;
                }
                if (expected.EnforceText
                    && !string.Equals(expected.ExpectedText ?? string.Empty,
                        cell.Text ?? string.Empty, StringComparison.Ordinal))
                {
                    failureCode = OwnerDrawFailureCode.IdentityMismatch;
                    status = command + " exact text guard failed.";
                    cell = null;
                    return false;
                }
            }
            status = command + " succeeded index=" + cell.Index + " method="
                + cell.Method + " kind=" + cell.Kind + " row=" + cell.Row
                + " col=" + cell.Col + " bounds=" + cell.ScreenBounds
                + " textLength=" + cell.Text.Length + "; " + transportStatus;
            return true;
        }

        private static bool TryParseCell(string response, out BridgeCell cell,
            out OwnerDrawFailureCode failureCode, out string status)
        {
            cell = null;
            failureCode = OwnerDrawFailureCode.None;
            string kind = OwnerDrawProtocol.ResponseKind(response);
            if (!string.Equals(kind, "OK", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(kind, "OK_CELL", StringComparison.OrdinalIgnoreCase))
            {
                Dictionary<string, string> errorMap
                    = OwnerDrawProtocol.ParseMap(response);
                OwnerDrawProtocol.TryDecodeOptional(errorMap, "status64",
                    out string nativeError, out _);
                failureCode = MapNativeFailure(OwnerDrawProtocol.Get(errorMap, "code"));
                status = "Bridge rejected cell request kind=\"" + kind
                    + "\" code=\"" + OwnerDrawProtocol.Get(errorMap, "code")
                    + "\" nativeStatus=\"" + OwnerDrawProtocol.Short(nativeError) + "\".";
                return false;
            }

            Dictionary<string, string> map = OwnerDrawProtocol.ParseMap(response);
            int abi = OwnerDrawProtocol.GetInt(map, "abi", 0);
            if (abi != RequiredAbi)
            {
                failureCode = OwnerDrawFailureCode.ProtocolMismatch;
                status = "Bridge cell response ABI mismatch actual=" + abi
                    + " required=" + RequiredAbi + ".";
                return false;
            }
            if (!OwnerDrawProtocol.TryGetInt(map, "index", out int index) || index < 0)
            {
                failureCode = OwnerDrawFailureCode.ProtocolMismatch;
                status = "Bridge response has no valid cell index.";
                return false;
            }
            if (!TryReadRect(map, "screen", out Rectangle screenBounds)
                || screenBounds.Width <= 0 || screenBounds.Height <= 0)
            {
                failureCode = OwnerDrawFailureCode.ProtocolMismatch;
                status = "Bridge response has no valid screen geometry for index="
                    + index + ".";
                return false;
            }
            bool textDecoded = OwnerDrawProtocol.TryDecodeOptional(map, "text64",
                out string text, out string textStatus);
            bool statusDecoded = OwnerDrawProtocol.TryDecodeOptional(map, "status64",
                out string nativeStatus, out string nativeStatusDecode);
            if (!textDecoded || !statusDecoded)
            {
                failureCode = OwnerDrawFailureCode.ProtocolMismatch;
                status = "Bridge returned malformed UTF-8/base64 for index=" + index
                    + ": " + textStatus + "; " + nativeStatusDecode;
                return false;
            }

            if (!TryReadRect(map, "client", out Rectangle clientBounds))
            {
                failureCode = OwnerDrawFailureCode.ProtocolMismatch;
                status = "Bridge response has no valid client geometry for index="
                    + index + ".";
                return false;
            }
            string cellKind = OwnerDrawProtocol.Get(map, "kind");
            if (!IsKnownCellKind(cellKind))
            {
                failureCode = OwnerDrawFailureCode.ProtocolMismatch;
                status = "Bridge response has an invalid cell kind for index="
                    + index + ".";
                return false;
            }
            bool fieldDecoded = OwnerDrawProtocol.TryDecodeOptional(map, "field64",
                out string field, out string fieldStatus);
            if (!fieldDecoded)
            {
                failureCode = OwnerDrawFailureCode.ProtocolMismatch;
                status = "Bridge returned malformed field64 for index=" + index
                    + ": " + fieldStatus;
                return false;
            }
            cell = new BridgeCell
            {
                Index = index,
                Kind = cellKind,
                Row = OwnerDrawProtocol.GetInt(map, "row", -1),
                Col = OwnerDrawProtocol.GetInt(map, "col", -1),
                Field = field ?? string.Empty,
                Text = text ?? string.Empty,
                TextAvailable = true,
                ScreenBounds = screenBounds,
                ClientBounds = clientBounds,
                Method = OwnerDrawProtocol.Get(map, "method", "cell"),
                NativeStatus = nativeStatus ?? string.Empty,
            };
            status = "Parsed PB GDI cell index=" + index + "; " + textStatus + ".";
            return true;
        }

        private static OwnerDrawFailureCode MapNativeFailure(string code)
        {
            if (string.Equals(code, "invalid_request", StringComparison.OrdinalIgnoreCase))
                return OwnerDrawFailureCode.InvalidRequest;
            if (string.Equals(code, "not_found", StringComparison.OrdinalIgnoreCase))
                return OwnerDrawFailureCode.NotFound;
            if (string.Equals(code, "identity_mismatch", StringComparison.OrdinalIgnoreCase))
                return OwnerDrawFailureCode.IdentityMismatch;
            if (string.Equals(code, "ambiguous", StringComparison.OrdinalIgnoreCase))
                return OwnerDrawFailureCode.Ambiguous;
            return OwnerDrawFailureCode.ProtocolMismatch;
        }

        private static bool IsKnownCellKind(string kind)
        {
            return string.Equals(kind, "grid-cell", StringComparison.OrdinalIgnoreCase)
                || string.Equals(kind, "label", StringComparison.OrdinalIgnoreCase)
                || string.Equals(kind, "input", StringComparison.OrdinalIgnoreCase);
        }

        private static string ToKindText(PowerBuilderCellKind kind)
        {
            switch (kind)
            {
                case PowerBuilderCellKind.GridCell: return "grid-cell";
                case PowerBuilderCellKind.Label: return "label";
                default: return "input";
            }
        }

        private sealed class ResolveGuard
        {
            internal PowerBuilderCellKind Kind { get; set; } = PowerBuilderCellKind.GridCell;
            internal int Row { get; set; } = -1;
            internal int Col { get; set; } = -1;
            internal Rectangle ClientBounds { get; set; }
            internal bool EnforceText { get; set; }
            internal string ExpectedText { get; set; } = string.Empty;
        }

        private bool TrySend(int processId, string request, out string response,
            out string status)
        {
            return transport.TryRoundTrip(PipePrefix, processId, request,
                ConnectTimeoutMs, ReadTimeoutMs, MaximumResponseBytes,
                out response, out status);
        }

        private static bool TryReadRect(IDictionary<string, string> map,
            string prefix, out Rectangle rectangle)
        {
            rectangle = Rectangle.Empty;
            if (!OwnerDrawProtocol.TryGetInt(map, prefix + "Left", out int left)
                || !OwnerDrawProtocol.TryGetInt(map, prefix + "Top", out int top)
                || !OwnerDrawProtocol.TryGetInt(map, prefix + "Width", out int width)
                || !OwnerDrawProtocol.TryGetInt(map, prefix + "Height", out int height)
                || width <= 0 || height <= 0)
                return false;
            try
            {
                rectangle = new Rectangle(left, top, width, height);
                return rectangle.Right >= rectangle.Left
                    && rectangle.Bottom >= rectangle.Top;
            }
            catch (ArgumentException)
            {
                rectangle = Rectangle.Empty;
                return false;
            }
        }
    }
}
