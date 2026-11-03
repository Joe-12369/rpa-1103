using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;

namespace II.RPA.Plugin.Windows.OwnerDraw.Vcl
{
    /// <summary>
    /// ABI 8 client extracted from the verified VclGridCellHelper implementation.
    /// It only accepts physical-screen-v2 geometry and explicit semantic identities.
    /// </summary>
    internal sealed class VclGridBridgeClient
    {
        internal const int RequiredAbi = 8;
        internal const string PipePrefix = "II.RPA.VclGridAccBridge.";
        private const int ConnectTimeoutMs = 250;
        private const int ReadTimeoutMs = 8000;
        private const int MaximumResponseBytes = 1024 * 1024;

        private readonly IOwnerDrawBridgeTransport transport;

        internal VclGridBridgeClient(IOwnerDrawBridgeTransport transport = null)
        {
            this.transport = transport ?? new NamedPipeOwnerDrawBridgeTransport();
        }

        internal sealed class BridgeCell
        {
            internal int ViewportRow { get; set; } = -1;
            internal int ViewportColumn { get; set; } = -1;
            internal int LogicalRow { get; set; } = -1;
            internal int LogicalColumn { get; set; } = -1;
            internal Rectangle ScreenBounds { get; set; }
            internal Rectangle ClientBounds { get; set; }
            internal string Text { get; set; } = string.Empty;
            internal string Field { get; set; } = string.Empty;
            internal bool HasCellModel { get; set; }
            internal bool HasLogicalIdentity { get; set; }
            internal string IdentitySource { get; set; } = string.Empty;
            internal string NativeStatus { get; set; } = string.Empty;
        }

        internal sealed class BridgeGridInfo
        {
            internal int RowCount { get; set; }
            internal int ColumnCount { get; set; }
            internal int CurrentRow { get; set; } = -1;
            internal int CurrentColumn { get; set; } = -1;
            internal string[] Headers { get; set; } = new string[0];
            internal string[] Fields { get; set; } = new string[0];
            internal string IdentitySource { get; set; } = string.Empty;
            internal string NativeStatus { get; set; } = string.Empty;
        }

        /// <summary>
        /// Image-level capabilities reported by ABI 8 PING.  These flags only
        /// describe mechanisms present in the loaded Bridge; every target HWND is
        /// still validated by the command that consumes the mechanism.
        /// </summary>
        internal sealed class BridgeCapabilities
        {
            internal int Abi { get; set; }
            internal string AdapterId { get; set; } = string.Empty;
            internal string Architecture { get; set; } = string.Empty;
            internal bool Running { get; set; }
            internal bool GdiVisibleCell { get; set; }
            internal bool GdiPlusText { get; set; }
            internal bool VisibleTable { get; set; }
            internal bool ExactHeaderScan { get; set; }
            internal bool RecordScrollResolve { get; set; }
            internal bool RttiHelper { get; set; }
            internal bool LogicalMetadata { get; set; }
            internal bool LogicalCellRead { get; set; }
            internal bool LogicalTable { get; set; }
            internal bool OffscreenRead { get; set; }
            internal bool FullLogicalModel { get; set; }
            internal bool TargetDependent { get; set; }

            internal bool SupportsLogicalCellResolve
            {
                get
                {
                    return GdiVisibleCell && ExactHeaderScan
                        && RecordScrollResolve;
                }
            }
        }

        internal bool TryQueryCapabilities(int processId,
            OwnerDrawArchitecture expectedArchitecture,
            out BridgeCapabilities capabilities, out string status)
        {
            capabilities = null;
            string request = "PING\tv="
                + RequiredAbi.ToString(CultureInfo.InvariantCulture);
            if (!TrySend(processId, request, 2000, out string response,
                out string transportStatus))
            {
                status = transportStatus;
                return false;
            }
            return TryValidateCapabilitiesResponse(response, expectedArchitecture,
                transportStatus, out capabilities, out status);
        }

        internal static bool TryValidateCapabilitiesResponse(string response,
            OwnerDrawArchitecture expectedArchitecture, string transportStatus,
            out BridgeCapabilities capabilities, out string status)
        {
            capabilities = null;
            Dictionary<string, string> map = OwnerDrawProtocol.ParseMap(response);
            if (!string.Equals(OwnerDrawProtocol.ResponseKind(response), "OK",
                StringComparison.OrdinalIgnoreCase))
            {
                OwnerDrawProtocol.TryDecodeOptional(map, "status64",
                    out string nativeError, out _);
                status = "VCL PING rejected kind=\""
                    + OwnerDrawProtocol.ResponseKind(response) + "\" nativeStatus=\""
                    + OwnerDrawProtocol.Short(nativeError) + "\"; "
                    + transportStatus;
                return false;
            }

            int abi = OwnerDrawProtocol.GetInt(map, "abi", 0);
            string adapter = OwnerDrawProtocol.Get(map, "adapter");
            string architecture = OwnerDrawProtocol.Get(map, "arch");
            string expectedArchitectureText = expectedArchitecture
                == OwnerDrawArchitecture.X64 ? "x64" : "x86";
            if (abi != RequiredAbi
                || !string.Equals(adapter, OwnerDrawSelectorCodec.VclAdapterId,
                    StringComparison.OrdinalIgnoreCase)
                || !string.Equals(architecture, expectedArchitectureText,
                    StringComparison.OrdinalIgnoreCase))
            {
                status = "VCL PING identity rejected abi=" + abi + " adapter=\""
                    + adapter + "\" arch=\"" + architecture + "\" expectedArch=\""
                    + expectedArchitectureText + "\"; " + transportStatus;
                return false;
            }

            string[] flagNames =
            {
                "running",
                "gdiVisibleCell",
                "gdiPlusText",
                "visibleTable",
                "exactHeaderScan",
                "recordScrollResolve",
                "rttiHelper",
                "logicalMetadata",
                "logicalCellRead",
                "logicalTable",
                "offscreenRead",
                "fullLogicalModel",
                "targetDependent",
            };
            var flags = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (string flagName in flagNames)
            {
                if (!TryReadFlag(map, flagName, out bool flag))
                {
                    status = "VCL PING capability flag is missing or invalid: "
                        + flagName + "; " + transportStatus;
                    return false;
                }
                flags[flagName] = flag;
            }
            if (!flags["running"])
            {
                status = "VCL PING reports that the Bridge is not running; "
                    + transportStatus;
                return false;
            }

            capabilities = new BridgeCapabilities
            {
                Abi = abi,
                AdapterId = adapter,
                Architecture = architecture,
                Running = flags["running"],
                GdiVisibleCell = flags["gdiVisibleCell"],
                GdiPlusText = flags["gdiPlusText"],
                VisibleTable = flags["visibleTable"],
                ExactHeaderScan = flags["exactHeaderScan"],
                RecordScrollResolve = flags["recordScrollResolve"],
                RttiHelper = flags["rttiHelper"],
                LogicalMetadata = flags["logicalMetadata"],
                LogicalCellRead = flags["logicalCellRead"],
                LogicalTable = flags["logicalTable"],
                OffscreenRead = flags["offscreenRead"],
                FullLogicalModel = flags["fullLogicalModel"],
                TargetDependent = flags["targetDependent"],
            };
            status = "VCL PING accepted arch=" + architecture
                + " logicalResolve=" + capabilities.SupportsLogicalCellResolve
                + " gdiPlusText=" + capabilities.GdiPlusText
                + " logicalMetadata=" + capabilities.LogicalMetadata
                + " logicalCellRead=" + capabilities.LogicalCellRead
                + " logicalTable=" + capabilities.LogicalTable
                + " offscreenRead=" + capabilities.OffscreenRead
                + " fullLogicalModel=" + capabilities.FullLogicalModel + "; "
                + transportStatus;
            return true;
        }

        internal bool TryQuerySemanticCell(int processId, IntPtr gridHwnd,
            Point screen, Point client, out BridgeCell cell, out string status)
        {
            Tuple<double, double> scale
                = OwnerDrawWindowProbe.GetBridgeClientToPhysicalScale(gridHwnd);
            string request = string.Join("\t", new[]
            {
                "GET_CELL",
                "v=" + RequiredAbi.ToString(CultureInfo.InvariantCulture),
                "hwnd=" + gridHwnd.ToInt64().ToString(CultureInfo.InvariantCulture),
                "screenX=" + screen.X.ToString(CultureInfo.InvariantCulture),
                "screenY=" + screen.Y.ToString(CultureInfo.InvariantCulture),
                "clientX=" + client.X.ToString(CultureInfo.InvariantCulture),
                "clientY=" + client.Y.ToString(CultureInfo.InvariantCulture),
                "coord=physical-screen-v2",
                "scaleX1000=" + ToProtocolScale(scale.Item1),
                "scaleY1000=" + ToProtocolScale(scale.Item2),
                "semantic=1",
                "depth=LockReadOnly",
            });
            if (!TrySend(processId, request, ReadTimeoutMs, out string response,
                out status))
            {
                cell = null;
                return false;
            }
            return TryParseCell(response, gridHwnd, screen, client,
                out cell, out status);
        }

        internal bool TryResolveCell(int processId, IntPtr gridHwnd,
            int logicalRow, int logicalColumn, string expectedField,
            out BridgeCell cell, out string status)
        {
            cell = null;
            if (logicalRow < -1 || logicalColumn < 0)
            {
                status = "VCL RESOLVE_CELL logical identity is invalid.";
                return false;
            }
            if (!OwnerDrawWindowProbe.TryGetClientOrigin(gridHwnd, out Point origin))
            {
                status = "VCL RESOLVE_CELL cannot obtain the grid client origin.";
                return false;
            }
            Tuple<double, double> scale
                = OwnerDrawWindowProbe.GetBridgeClientToPhysicalScale(gridHwnd);
            string request = string.Join("\t", new[]
            {
                "RESOLVE_CELL",
                "v=" + RequiredAbi.ToString(CultureInfo.InvariantCulture),
                "hwnd=" + gridHwnd.ToInt64().ToString(CultureInfo.InvariantCulture),
                "row=" + logicalRow.ToString(CultureInfo.InvariantCulture),
                "col=" + logicalColumn.ToString(CultureInfo.InvariantCulture),
                "field64=" + OwnerDrawProtocol.Encode(expectedField),
                "screenX=" + origin.X.ToString(CultureInfo.InvariantCulture),
                "screenY=" + origin.Y.ToString(CultureInfo.InvariantCulture),
                "clientX=0",
                "clientY=0",
                "coord=physical-screen-v2",
                "scaleX1000=" + ToProtocolScale(scale.Item1),
                "scaleY1000=" + ToProtocolScale(scale.Item2),
            });
            if (!TrySend(processId, request, ReadTimeoutMs, out string response,
                out status)) return false;
            return TryParseCell(response, gridHwnd, origin, Point.Empty,
                out cell, out status);
        }

        internal bool TryReadLogicalCell(int processId, IntPtr gridHwnd,
            int logicalRow, int logicalColumn, out string text, out string field,
            out string status)
        {
            text = string.Empty;
            field = string.Empty;
            string request = "READ_CELL\tv=" + RequiredAbi
                + "\thwnd=" + gridHwnd.ToInt64().ToString(CultureInfo.InvariantCulture)
                + "\trow=" + logicalRow.ToString(CultureInfo.InvariantCulture)
                + "\tcol=" + logicalColumn.ToString(CultureInfo.InvariantCulture);
            if (!TrySend(processId, request, ReadTimeoutMs, out string response,
                out status)) return false;

            Dictionary<string, string> map = OwnerDrawProtocol.ParseMap(response);
            if (!string.Equals(OwnerDrawProtocol.ResponseKind(response), "OK_VALUE",
                StringComparison.OrdinalIgnoreCase))
            {
                OwnerDrawProtocol.TryDecodeOptional(map, "status64",
                    out string nativeError, out _);
                status = "VCL READ_CELL rejected: "
                    + OwnerDrawProtocol.Short(nativeError);
                return false;
            }
            int abi = OwnerDrawProtocol.GetInt(map, "abi", 0);
            int row = OwnerDrawProtocol.GetInt(map, "row", int.MinValue);
            int column = OwnerDrawProtocol.GetInt(map, "col", int.MinValue);
            if (abi != RequiredAbi || OwnerDrawProtocol.GetInt(map, "semantic", 0) == 0
                || row != logicalRow || column != logicalColumn)
            {
                status = "VCL READ_CELL identity rejected abi=" + abi
                    + " expected=" + logicalRow + "," + logicalColumn
                    + " actual=" + row + "," + column + ".";
                return false;
            }
            bool textDecoded = OwnerDrawProtocol.TryDecodeOptional(map, "text64",
                out text, out string textStatus);
            bool fieldDecoded = OwnerDrawProtocol.TryDecodeOptional(map, "field64",
                out field, out string fieldStatus);
            if (!textDecoded || !fieldDecoded)
            {
                status = "VCL READ_CELL returned malformed data: " + textStatus
                    + "; " + fieldStatus;
                return false;
            }
            status = "VCL READ_CELL succeeded row=" + logicalRow + " col="
                + logicalColumn + " textLength=" + text.Length + ".";
            return true;
        }

        internal bool TryQueryGridInfo(int processId, IntPtr gridHwnd,
            out BridgeGridInfo info, out string status)
        {
            info = null;
            string request = "GET_GRID_INFO\tv=" + RequiredAbi
                + "\thwnd=" + gridHwnd.ToInt64().ToString(CultureInfo.InvariantCulture);
            if (!TrySend(processId, request, 5000, out string response,
                out status)) return false;

            Dictionary<string, string> map = OwnerDrawProtocol.ParseMap(response);
            if (!string.Equals(OwnerDrawProtocol.ResponseKind(response), "OK_INFO",
                StringComparison.OrdinalIgnoreCase))
            {
                OwnerDrawProtocol.TryDecodeOptional(map, "status64",
                    out string nativeError, out _);
                status = "VCL GET_GRID_INFO rejected kind=\""
                    + OwnerDrawProtocol.ResponseKind(response) + "\" nativeStatus=\""
                    + OwnerDrawProtocol.Short(nativeError) + "\".";
                return false;
            }
            int abi = OwnerDrawProtocol.GetInt(map, "abi", 0);
            int rows = OwnerDrawProtocol.GetInt(map, "rowCount", -1);
            int columns = OwnerDrawProtocol.GetInt(map, "colCount", -1);
            if (abi != RequiredAbi || OwnerDrawProtocol.GetInt(map, "semantic", 0) == 0
                || rows < 0 || columns <= 0)
            {
                status = "VCL logical grid metadata unavailable abi=" + abi
                    + " rows=" + rows + " columns=" + columns + ".";
                return false;
            }
            bool headersDecoded = OwnerDrawProtocol.TryDecodeOptional(map, "headers64",
                out string headers, out string headerStatus);
            bool fieldsDecoded = OwnerDrawProtocol.TryDecodeOptional(map, "fields64",
                out string fields, out string fieldStatus);
            bool statusDecoded = OwnerDrawProtocol.TryDecodeOptional(map, "status64",
                out string nativeStatus, out string nativeStatusDecode);
            if (!headersDecoded || !fieldsDecoded || !statusDecoded)
            {
                status = "VCL GET_GRID_INFO returned malformed data: " + headerStatus
                    + "; " + fieldStatus + "; " + nativeStatusDecode;
                return false;
            }
            info = new BridgeGridInfo
            {
                RowCount = rows,
                ColumnCount = columns,
                CurrentRow = OwnerDrawProtocol.GetInt(map, "currentRow", -1),
                CurrentColumn = OwnerDrawProtocol.GetInt(map, "currentCol", -1),
                Headers = SplitTsv(headers),
                Fields = SplitTsv(fields),
                IdentitySource = OwnerDrawProtocol.Get(map, "identity"),
                NativeStatus = nativeStatus,
            };
            status = "VCL GET_GRID_INFO succeeded rows=" + rows + " columns="
                + columns + ".";
            return true;
        }

        private bool TrySend(int processId, string request, int readTimeout,
            out string response, out string status)
        {
            return transport.TryRoundTrip(PipePrefix, processId, request,
                ConnectTimeoutMs, readTimeout, MaximumResponseBytes,
                out response, out status);
        }

        private static bool TryParseCell(string response, IntPtr gridHwnd,
            Point screen, Point client, out BridgeCell cell, out string status)
        {
            cell = null;
            Dictionary<string, string> map = OwnerDrawProtocol.ParseMap(response);
            if (!string.Equals(OwnerDrawProtocol.ResponseKind(response), "OK",
                StringComparison.OrdinalIgnoreCase))
            {
                OwnerDrawProtocol.TryDecodeOptional(map, "status64",
                    out string nativeError, out _);
                status = "VCL cell request rejected: "
                    + OwnerDrawProtocol.Short(nativeError);
                return false;
            }

            int abi = OwnerDrawProtocol.GetInt(map, "abi", 0);
            string coordinateSystem = OwnerDrawProtocol.Get(map, "coord");
            if (abi != RequiredAbi || !string.Equals(coordinateSystem,
                "physical-screen-v2", StringComparison.OrdinalIgnoreCase))
            {
                status = "VCL protocol rejected abi=" + abi + " coord=\""
                    + coordinateSystem + "\"; ABI 8 physical-screen-v2 is required.";
                return false;
            }
            if (!TryReadScreenRect(map, out Rectangle screenBounds))
            {
                status = "VCL ABI 8 response has no valid physical screen bounds.";
                return false;
            }
            bool hasLogicalRow = OwnerDrawProtocol.TryGetInt(map, "logicalRow",
                out int logicalRow);
            bool hasLogicalColumn = OwnerDrawProtocol.TryGetInt(map, "logicalCol",
                out int logicalColumn);
            bool semantic = OwnerDrawProtocol.GetInt(map, "semantic", 0) != 0;
            if (semantic && (!hasLogicalRow || !hasLogicalColumn))
            {
                status = "VCL semantic response omitted logicalRow/logicalCol.";
                return false;
            }
            bool textDecoded = OwnerDrawProtocol.TryDecodeOptional(map, "text64",
                out string text, out string textStatus);
            bool fieldDecoded = OwnerDrawProtocol.TryDecodeOptional(map, "field64",
                out string field, out string fieldStatus);
            bool statusDecoded = OwnerDrawProtocol.TryDecodeOptional(map, "status64",
                out string nativeStatus, out string nativeStatusDecode);
            if (!textDecoded || !fieldDecoded || !statusDecoded)
            {
                status = "VCL cell response has malformed base64/UTF-8: " + textStatus
                    + "; " + fieldStatus + "; " + nativeStatusDecode;
                return false;
            }

            Rectangle clientBounds = TryReadClientRect(map, "cell", out Rectangle parsed)
                ? parsed : (TryReadClientRect(map, "text", out parsed)
                    ? parsed : Rectangle.Empty);
            cell = new BridgeCell
            {
                ViewportRow = OwnerDrawProtocol.GetInt(map, "viewportRow",
                    OwnerDrawProtocol.GetInt(map, "row", -1)),
                ViewportColumn = OwnerDrawProtocol.GetInt(map, "viewportCol",
                    OwnerDrawProtocol.GetInt(map, "col", -1)),
                LogicalRow = hasLogicalRow ? logicalRow : -1,
                LogicalColumn = hasLogicalColumn ? logicalColumn : -1,
                ScreenBounds = screenBounds,
                ClientBounds = clientBounds,
                Text = text ?? string.Empty,
                Field = field ?? string.Empty,
                HasCellModel = OwnerDrawProtocol.GetInt(map, "model", 0) != 0,
                HasLogicalIdentity = semantic,
                IdentitySource = OwnerDrawProtocol.Get(map, "identity"),
                NativeStatus = nativeStatus ?? string.Empty,
            };
            status = "VCL cell response accepted viewport=" + cell.ViewportRow + ","
                + cell.ViewportColumn + " logical=" + cell.LogicalRow + ","
                + cell.LogicalColumn + " semantic=" + cell.HasLogicalIdentity
                + " textLength=" + cell.Text.Length + ".";
            return true;
        }

        private static bool TryReadScreenRect(IDictionary<string, string> map,
            out Rectangle rectangle)
        {
            rectangle = Rectangle.Empty;
            int left = OwnerDrawProtocol.GetInt(map, "left", int.MinValue);
            int top = OwnerDrawProtocol.GetInt(map, "top", int.MinValue);
            int width = OwnerDrawProtocol.GetInt(map, "width", 0);
            int height = OwnerDrawProtocol.GetInt(map, "height", 0);
            if (left == int.MinValue || top == int.MinValue || width <= 0 || height <= 0)
                return false;
            rectangle = new Rectangle(left, top, width, height);
            return true;
        }

        private static bool TryReadClientRect(IDictionary<string, string> map,
            string prefix, out Rectangle rectangle)
        {
            rectangle = Rectangle.Empty;
            int left = OwnerDrawProtocol.GetInt(map, prefix + "Left", int.MinValue);
            int top = OwnerDrawProtocol.GetInt(map, prefix + "Top", int.MinValue);
            int width = OwnerDrawProtocol.GetInt(map, prefix + "Width", 0);
            int height = OwnerDrawProtocol.GetInt(map, prefix + "Height", 0);
            if (left == int.MinValue || top == int.MinValue || width <= 0 || height <= 0)
                return false;
            rectangle = new Rectangle(left, top, width, height);
            return true;
        }

        private static int ToProtocolScale(double scale)
        {
            return Math.Max(100, Math.Min(8000,
                (int)Math.Round(scale * 1000.0, MidpointRounding.AwayFromZero)));
        }

        private static string[] SplitTsv(string value)
        {
            return string.IsNullOrEmpty(value) ? new string[0] : value.Split('\t');
        }

        private static bool TryReadFlag(IDictionary<string, string> map,
            string name, out bool value)
        {
            value = false;
            if (!OwnerDrawProtocol.TryGetInt(map, name, out int numeric)
                || (numeric != 0 && numeric != 1)) return false;
            value = numeric == 1;
            return true;
        }
    }
}
