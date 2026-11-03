using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace II.RPA.Plugin.Windows.OwnerDraw
{
    /// <summary>
    /// Converts the virtual leaf to/from neutral property records. GuiOperator's
    /// existing WindowsSelectorItem remains the owner of the surrounding host path.
    /// </summary>
    internal static class OwnerDrawSelectorCodec
    {
        internal const int CurrentSchemaVersion = 2;
        internal const string NativeProviderName = "owner-draw";
        internal const string PowerBuilderAdapterId = "powerbuilder-datawindow";
        internal const string VclAdapterId = "vcl-grid";

        internal const string NativeProvider = "native-provider";
        internal const string Marker = "owner-draw";
        internal const string Adapter = "owner-draw-adapter";
        internal const string SchemaVersion = "owner-draw-version";
        internal const string IdentityStrength = "owner-draw-identity";
        internal const string HostClass = "owner-draw-host-class";
        internal const string CellText = "owner-draw-text";

        internal const string PbCellIndex = "pb-cell-index";
        internal const string PbLineIndex = "pb-line-index";
        internal const string PbSegmentIndex = "pb-segment-index";
        internal const string PbCellSource = "pb-cell-source";
        internal const string PbCellRect = "pb-cell-rect";

        internal const string PbKind = "pb-kind";
        internal const string PbRow = "pb-row";
        internal const string PbCol = "pb-col";
        internal const string PbField = "pb-field";

        internal const string VclGridCell = "vcl-grid-cell";
        internal const string VclGridClass = "vcl-grid-class";
        internal const string VclGridRow = "vcl-grid-row";
        internal const string VclGridColumn = "vcl-grid-col";
        internal const string VclGridField = "vcl-grid-field";
        internal const string VclIdentitySource = "vcl-grid-identity";
        internal const string VclGridText = "vcl-grid-text";

        /// <summary>
        /// Reserved field names are also the fail-closed discriminator.  Values
        /// are deliberately ignored here: a damaged marker must still be routed
        /// to the strict decoder and must never fall back to its host selector.
        /// </summary>
        internal static bool IsReservedSelectorPropertyName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            return string.Equals(name, NativeProvider,
                       StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, Marker, StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, Adapter, StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, SchemaVersion,
                       StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, IdentityStrength,
                       StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, HostClass,
                       StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, CellText,
                       StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, PbCellIndex,
                       StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, PbLineIndex,
                       StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, PbSegmentIndex,
                       StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, PbCellSource,
                       StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, PbCellRect,
                       StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, PbKind, StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, PbRow, StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, PbCol, StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, PbField, StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, VclGridCell,
                       StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, VclGridClass,
                       StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, VclGridRow,
                       StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, VclGridColumn,
                       StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, VclGridField,
                       StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, VclIdentitySource,
                       StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, VclGridText,
                       StringComparison.OrdinalIgnoreCase);
        }

        internal static IReadOnlyList<OwnerDrawSelectorProperty> Encode(
            OwnerDrawSelector selector)
        {
            if (!TryValidate(selector, out string error))
                throw new ArgumentException(error, "selector");

            var result = new List<OwnerDrawSelectorProperty>
            {
                Property(NativeProvider, NativeProviderName, true),
                Property(Marker, "true", true),
                Property(Adapter, selector.AdapterId, true),
                Property(SchemaVersion, CurrentSchemaVersion.ToString(
                    CultureInfo.InvariantCulture), true),
                Property(IdentityStrength, ToIdentityText(selector.IdentityStrength), true),
                Property(HostClass, selector.HostClassName, true),
            };

            if (string.Equals(selector.AdapterId, PowerBuilderAdapterId,
                StringComparison.OrdinalIgnoreCase))
            {
                result.Add(Property(PbKind, ToPbKindText(selector.PbKind), true));
                if (selector.PbKind == PowerBuilderCellKind.GridCell)
                {
                    result.Add(Property(PbRow, selector.PbRow.ToString(
                        CultureInfo.InvariantCulture), true));
                    result.Add(Property(PbCol, selector.PbCol.ToString(
                        CultureInfo.InvariantCulture), true));
                    if (!string.IsNullOrEmpty(selector.PbField))
                        result.Add(Property(PbField, selector.PbField, false));
                    result.Add(Property(CellText, selector.ExpectedText, selector.EnforceText));
                }
                else if (selector.PbKind == PowerBuilderCellKind.Input)
                {
                    result.Add(Property(PbCellRect, FormatRect(selector.CellClientBounds), true));
                    result.Add(Property(CellText, selector.ExpectedText, selector.EnforceText));
                }
                else
                {
                    // Label identity is the exact text itself.
                    result.Add(Property(CellText, selector.ExpectedText, true));
                }
                return result;
            }

            result.Add(Property(VclGridCell, "true", true));
            result.Add(Property(VclGridClass, selector.HostClassName, true));
            result.Add(Property(VclGridRow, selector.LogicalRow.ToString(
                CultureInfo.InvariantCulture), true));
            result.Add(Property(VclGridColumn, selector.LogicalColumn.ToString(
                CultureInfo.InvariantCulture), true));
            if (!string.IsNullOrEmpty(selector.FieldName))
                result.Add(Property(VclGridField, selector.FieldName, true));
            if (!string.IsNullOrEmpty(selector.IdentitySource))
                result.Add(Property(VclIdentitySource, selector.IdentitySource, false));
            result.Add(Property(VclGridText, selector.ExpectedText, selector.EnforceText));
            return result;
        }

        internal static bool TryDecode(IEnumerable<OwnerDrawSelectorProperty> properties,
            out OwnerDrawSelector selector, out string status)
        {
            selector = null;
            if (properties == null)
            {
                status = "Owner-draw selector leaf has no properties.";
                return false;
            }

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var accurate = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var reservedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (OwnerDrawSelectorProperty property in properties)
            {
                if (property == null || string.IsNullOrWhiteSpace(property.Name)) continue;
                if (IsReservedSelectorPropertyName(property.Name)
                    && !reservedNames.Add(property.Name))
                {
                    status = "Owner-draw selector contains duplicate reserved property '"
                        + property.Name + "'.";
                    return false;
                }
                values[property.Name] = property.Value ?? string.Empty;
                if (property.Accurate) accurate.Add(property.Name);
            }
            return TryDecode(values, accurate, true, out selector, out status);
        }

        internal static bool TryDecode(IReadOnlyDictionary<string, string> properties,
            out OwnerDrawSelector selector, out string status)
        {
            return TryDecode(properties, new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                false, out selector, out status);
        }

        private static bool TryDecode(IReadOnlyDictionary<string, string> properties,
            ISet<string> accurate, bool requireAccuracyMetadata,
            out OwnerDrawSelector selector, out string status)
        {
            selector = null;
            if (properties == null)
            {
                status = "Owner-draw selector leaf has no properties.";
                return false;
            }

            bool marker = IsTrue(Get(properties, Marker));
            bool legacyVclSelector = false;
            string nativeProvider = Get(properties, NativeProvider);
            string adapterId = Get(properties, Adapter);
            string encodedIdentity = Get(properties, IdentityStrength);

            // Compatibility with selectors emitted by the earlier VCL integration.
            if (!marker && IsTrue(Get(properties, VclGridCell)))
            {
                marker = true;
                adapterId = VclAdapterId;
                legacyVclSelector = true;
            }

            if (!marker
                || (!string.IsNullOrEmpty(nativeProvider)
                    && !string.Equals(nativeProvider, NativeProviderName,
                        StringComparison.OrdinalIgnoreCase)))
            {
                status = "Selector leaf is not an owner-draw selector.";
                return false;
            }

            int version = ParseInt(Get(properties, SchemaVersion),
                string.Equals(adapterId, VclAdapterId, StringComparison.OrdinalIgnoreCase)
                    ? CurrentSchemaVersion : -1);
            if (version != CurrentSchemaVersion)
            {
                status = "Unsupported owner-draw selector version '"
                    + Get(properties, SchemaVersion) + "'.";
                return false;
            }

            var decoded = new OwnerDrawSelector
            {
                SchemaVersion = version,
                AdapterId = adapterId,
                HostClassName = FirstNonEmpty(Get(properties, HostClass),
                    Get(properties, VclGridClass)),
                IdentitySource = Get(properties, VclIdentitySource),
            };

            if (string.Equals(adapterId, PowerBuilderAdapterId,
                StringComparison.OrdinalIgnoreCase))
            {
                if (!TryParsePbKind(Get(properties, PbKind),
                    out PowerBuilderCellKind kind))
                {
                    status = "PowerBuilder selector pb-kind is invalid or missing.";
                    return false;
                }
                decoded.IdentityStrength = OwnerDrawIdentityStrength.LogicalCell;
                decoded.PbKind = kind;
                if (kind == PowerBuilderCellKind.GridCell)
                {
                    int row = ParseInt(Get(properties, PbRow), -1);
                    int col = ParseInt(Get(properties, PbCol), -1);
                    if (row < -1 || col < 0)
                    {
                        status = "PowerBuilder grid-cell selector row/col is invalid.";
                        return false;
                    }
                    decoded.PbRow = row;
                    decoded.PbCol = col;
                    decoded.PbField = Get(properties, PbField);
                    decoded.ExpectedText = Get(properties, CellText);
                    decoded.EnforceText = accurate.Contains(CellText);
                }
                else if (kind == PowerBuilderCellKind.Input)
                {
                    if (!TryParseRect(Get(properties, PbCellRect),
                        out System.Drawing.Rectangle rect) || rect.Width <= 0)
                    {
                        status = "PowerBuilder input selector rectangle is invalid.";
                        return false;
                    }
                    decoded.CellClientBounds = rect;
                    decoded.ExpectedText = Get(properties, CellText);
                    decoded.EnforceText = accurate.Contains(CellText);
                }
                else
                {
                    decoded.ExpectedText = Get(properties, CellText);
                    decoded.EnforceText = accurate.Contains(CellText);
                }
            }
            else if (string.Equals(adapterId, VclAdapterId,
                StringComparison.OrdinalIgnoreCase))
            {
                decoded.IdentityStrength = OwnerDrawIdentityStrength.LogicalCell;
                decoded.LogicalRow = ParseInt(Get(properties, VclGridRow), int.MinValue);
                decoded.LogicalColumn = ParseInt(Get(properties, VclGridColumn), int.MinValue);
                decoded.FieldName = Get(properties, VclGridField);
                decoded.ExpectedText = FirstNonEmpty(Get(properties, VclGridText),
                    Get(properties, CellText));
                decoded.EnforceText = accurate.Contains(VclGridText)
                    || accurate.Contains(CellText);
            }
            else
            {
                status = "Unknown owner-draw adapter '" + adapterId + "'.";
                return false;
            }

            if (requireAccuracyMetadata)
            {
                List<string> requiredExact;
                if (legacyVclSelector)
                {
                    // Legacy VCL leaves did not contain the generic owner-draw
                    // envelope fields, but their marker and logical row/column are
                    // still identity.  If an editor disables any of them, reject the
                    // selector instead of silently continuing with hidden identity.
                    requiredExact = new List<string>
                    {
                        VclGridCell,
                        VclGridRow,
                        VclGridColumn,
                    };
                    if (properties.ContainsKey(VclGridClass))
                        requiredExact.Add(VclGridClass);
                    if (properties.ContainsKey(VclGridField))
                        requiredExact.Add(VclGridField);
                }
                else
                {
                    requiredExact = new List<string>
                    {
                        NativeProvider,
                        Marker,
                        Adapter,
                        SchemaVersion,
                        IdentityStrength,
                        HostClass,
                    };
                    if (string.Equals(adapterId, PowerBuilderAdapterId,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        requiredExact.Add(PbKind);
                        if (decoded.PbKind == PowerBuilderCellKind.GridCell)
                        {
                            requiredExact.Add(PbRow);
                            requiredExact.Add(PbCol);
                        }
                        else if (decoded.PbKind == PowerBuilderCellKind.Input)
                        {
                            requiredExact.Add(PbCellRect);
                        }
                        else
                        {
                            requiredExact.Add(CellText);
                        }
                    }
                    else
                    {
                        requiredExact.Add(VclGridRow);
                        requiredExact.Add(VclGridColumn);
                        if (properties.ContainsKey(VclGridField))
                            requiredExact.Add(VclGridField);
                    }
                }
                string invalidExact = requiredExact.FirstOrDefault(name =>
                    !properties.ContainsKey(name) || !accurate.Contains(name));
                if (!string.IsNullOrEmpty(invalidExact))
                {
                    status = "Owner-draw identity property must be present and exact: '"
                        + invalidExact + "'.";
                    return false;
                }
            }

            if (!TryValidate(decoded, out status)) return false;
            string expectedIdentity = ToIdentityText(decoded.IdentityStrength);
            if (!string.IsNullOrEmpty(encodedIdentity)
                && !string.Equals(encodedIdentity, expectedIdentity,
                    StringComparison.OrdinalIgnoreCase))
            {
                status = "Owner-draw identity kind '" + encodedIdentity
                    + "' conflicts with adapter '" + decoded.AdapterId + "'.";
                return false;
            }
            selector = decoded;
            status = "Owner-draw selector decoded adapter=" + decoded.AdapterId
                + " identity=" + ToIdentityText(decoded.IdentityStrength) + ".";
            return true;
        }

        internal static bool TryValidate(OwnerDrawSelector selector, out string status)
        {
            if (selector == null)
            {
                status = "Owner-draw selector is null.";
                return false;
            }
            if (selector.SchemaVersion != CurrentSchemaVersion)
            {
                status = "Owner-draw selector schema must be "
                    + CurrentSchemaVersion.ToString(CultureInfo.InvariantCulture) + ".";
                return false;
            }
            if (string.IsNullOrWhiteSpace(selector.HostClassName))
            {
                status = "Owner-draw selector host class is missing.";
                return false;
            }
            if (string.Equals(selector.AdapterId, PowerBuilderAdapterId,
                StringComparison.OrdinalIgnoreCase))
            {
                if (selector.PbKind == PowerBuilderCellKind.GridCell)
                {
                    if (selector.PbRow < -1 || selector.PbCol < 0)
                    {
                        status = "PowerBuilder grid-cell selector row/col is invalid.";
                        return false;
                    }
                }
                else if (selector.PbKind == PowerBuilderCellKind.Input)
                {
                    if (selector.CellClientBounds.Width <= 0
                        || selector.CellClientBounds.Height <= 0)
                    {
                        status = "PowerBuilder input selector has no client geometry.";
                        return false;
                    }
                }
                else if (!selector.EnforceText
                    || string.IsNullOrEmpty(selector.ExpectedText))
                {
                    status = "PowerBuilder label selector requires exact text.";
                    return false;
                }
                status = "PowerBuilder logical-cell selector is valid.";
                return true;
            }
            if (string.Equals(selector.AdapterId, VclAdapterId,
                StringComparison.OrdinalIgnoreCase))
            {
                if (selector.LogicalRow < -1 || selector.LogicalColumn < 0)
                {
                    status = "VCL logical row/column selector is incomplete or invalid.";
                    return false;
                }
                status = "VCL logical-cell selector is valid.";
                return true;
            }
            status = "Unknown owner-draw adapter '" + selector.AdapterId + "'.";
            return false;
        }

        private static OwnerDrawSelectorProperty Property(string name, string value,
            bool accurate)
        {
            return new OwnerDrawSelectorProperty(name, value ?? string.Empty, accurate);
        }

        private static string ToIdentityText(OwnerDrawIdentityStrength strength)
        {
            return strength == OwnerDrawIdentityStrength.LogicalCell
                ? "logical-cell" : "visual-index";
        }

        private static string Get(IReadOnlyDictionary<string, string> properties,
            string name)
        {
            return properties.TryGetValue(name, out string value)
                ? value ?? string.Empty : string.Empty;
        }

        private static int ParseInt(string value, int fallback)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                out int parsed) ? parsed : fallback;
        }

        private static string FormatRect(System.Drawing.Rectangle rectangle)
        {
            return rectangle.Left.ToString(CultureInfo.InvariantCulture) + ","
                + rectangle.Top.ToString(CultureInfo.InvariantCulture) + ","
                + rectangle.Width.ToString(CultureInfo.InvariantCulture) + ","
                + rectangle.Height.ToString(CultureInfo.InvariantCulture);
        }

        private static bool TryParseRect(string value,
            out System.Drawing.Rectangle rectangle)
        {
            rectangle = System.Drawing.Rectangle.Empty;
            if (string.IsNullOrEmpty(value)) return false;
            string[] fields = value.Split(',');
            if (fields.Length != 4
                || !int.TryParse(fields[0], NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out int left)
                || !int.TryParse(fields[1], NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out int top)
                || !int.TryParse(fields[2], NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out int width)
                || !int.TryParse(fields[3], NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out int height)
                || width <= 0 || height <= 0)
            {
                return false;
            }
            try
            {
                rectangle = new System.Drawing.Rectangle(left, top, width, height);
                return rectangle.Right >= rectangle.Left
                    && rectangle.Bottom >= rectangle.Top;
            }
            catch (ArgumentException)
            {
                rectangle = System.Drawing.Rectangle.Empty;
                return false;
            }
        }

        private static bool IsTrue(string value)
        {
            return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase);
        }

        private static string ToPbKindText(PowerBuilderCellKind kind)
        {
            switch (kind)
            {
                case PowerBuilderCellKind.GridCell: return "grid-cell";
                case PowerBuilderCellKind.Label: return "label";
                default: return "input";
            }
        }

        private static bool TryParsePbKind(string value, out PowerBuilderCellKind kind)
        {
            if (string.Equals(value, "grid-cell", StringComparison.OrdinalIgnoreCase))
            { kind = PowerBuilderCellKind.GridCell; return true; }
            if (string.Equals(value, "label", StringComparison.OrdinalIgnoreCase))
            { kind = PowerBuilderCellKind.Label; return true; }
            if (string.Equals(value, "input", StringComparison.OrdinalIgnoreCase))
            { kind = PowerBuilderCellKind.Input; return true; }
            kind = PowerBuilderCellKind.GridCell;
            return false;
        }

        private static string FirstNonEmpty(params string[] values)
        {
            return values.FirstOrDefault(value => !string.IsNullOrEmpty(value))
                ?? string.Empty;
        }
    }
}
