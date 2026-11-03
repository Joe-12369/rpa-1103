using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace II.RPA.Plugin.Windows.OwnerDraw
{
    internal delegate bool OwnerDrawBridgeHealthProbe(int processId,
        OwnerDrawArchitecture expectedArchitecture, out string status);

    internal enum OwnerDrawProviderState
    {
        NotApplicable,
        Success,
        RecognizedButFailed,
    }

    internal enum OwnerDrawFailureCode
    {
        None,
        NotApplicable,
        InvalidRequest,
        InvalidSelector,
        TargetUnavailable,
        ArchitectureUnknown,
        ArchitectureMismatch,
        BridgeNotFound,
        BridgeStartFailed,
        BridgeStopped,
        TransportFailure,
        ProtocolMismatch,
        NotFound,
        IdentityMismatch,
        Ambiguous,
        LegacyConflict,
    }

    internal enum OwnerDrawArchitecture
    {
        X86,
        X64,
    }

    internal enum OwnerDrawBridgeActivationRoute
    {
        Direct,
        X86Broker,
    }

    /// <summary>
    /// Pure support policy shared by pickup and product lifecycle status. Native
    /// activation details remain behind IOwnerDrawBridgeActivator.
    /// </summary>
    internal static class OwnerDrawBridgeArchitecturePolicy
    {
        internal static bool TryResolve(OwnerDrawArchitecture controllerArchitecture,
            OwnerDrawArchitecture targetArchitecture,
            OwnerDrawBridgeDescriptor descriptor,
            out OwnerDrawBridgeActivationRoute route, out string status)
        {
            route = OwnerDrawBridgeActivationRoute.Direct;
            string controller = ToText(controllerArchitecture);
            string target = ToText(targetArchitecture);
            if (descriptor == null)
            {
                status = "Owner-draw architecture policy requires a Bridge descriptor; "
                    + "controller=" + controller + " target=" + target + ".";
                return false;
            }

            if (controllerArchitecture == targetArchitecture)
            {
                status = "Owner-draw architecture route=direct controller="
                    + controller + " target=" + target + " adapter="
                    + descriptor.AdapterId + ".";
                return true;
            }

            if (controllerArchitecture == OwnerDrawArchitecture.X64
                && targetArchitecture == OwnerDrawArchitecture.X86
                && descriptor.AllowX64ControllerToX86Target)
            {
                route = OwnerDrawBridgeActivationRoute.X86Broker;
                status = "Owner-draw architecture route=x86-broker controller=x64 "
                    + "target=x86 adapter=" + descriptor.AdapterId + ".";
                return true;
            }

            status = "Unsupported owner-draw architecture route controller="
                + controller + " target=" + target + " adapter="
                + descriptor.AdapterId + "; "
                + (controllerArchitecture == OwnerDrawArchitecture.X86
                    && targetArchitecture == OwnerDrawArchitecture.X64
                    ? "an x86 RPA cannot activate a Bridge in an x64 target."
                    : "this adapter does not allow x64-to-x86 Broker activation.");
            return false;
        }

        private static string ToText(OwnerDrawArchitecture architecture)
        {
            return architecture == OwnerDrawArchitecture.X64 ? "x64" : "x86";
        }
    }

    internal enum OwnerDrawIdentityStrength
    {
        VisualIndex,
        LogicalCell,
    }

    internal enum PowerBuilderCellKind
    {
        GridCell,
        Label,
        Input,
    }

    /// <summary>
    /// Three-state result shared by all owner-draw adapters. Interactive picking
    /// treats RecognizedButFailed as fail-open; explicit owner-draw selector replay
    /// treats it as fail-closed.
    /// </summary>
    internal sealed class OwnerDrawResult<T>
    {
        private OwnerDrawResult(OwnerDrawProviderState state, OwnerDrawFailureCode code,
            T value, string status)
        {
            State = state;
            Code = code;
            Value = value;
            Status = status ?? string.Empty;
        }

        internal OwnerDrawProviderState State { get; private set; }

        internal OwnerDrawFailureCode Code { get; private set; }

        internal T Value { get; private set; }

        internal string Status { get; private set; }

        internal bool IsSuccess
        {
            get { return State == OwnerDrawProviderState.Success; }
        }

        internal static OwnerDrawResult<T> NotApplicable(string status = null)
        {
            return new OwnerDrawResult<T>(OwnerDrawProviderState.NotApplicable,
                OwnerDrawFailureCode.NotApplicable, default(T), status);
        }

        internal static OwnerDrawResult<T> Success(T value, string status = null)
        {
            return new OwnerDrawResult<T>(OwnerDrawProviderState.Success,
                OwnerDrawFailureCode.None, value, status);
        }

        internal static OwnerDrawResult<T> Failed(OwnerDrawFailureCode code, string status)
        {
            if (code == OwnerDrawFailureCode.None || code == OwnerDrawFailureCode.NotApplicable)
                code = OwnerDrawFailureCode.InvalidRequest;
            return new OwnerDrawResult<T>(OwnerDrawProviderState.RecognizedButFailed,
                code, default(T), status);
        }
    }

    internal sealed class OwnerDrawPickRequest
    {
        internal OwnerDrawPickRequest(Point screenPoint, IntPtr candidateHwnd)
        {
            RequestId = Guid.NewGuid().ToString("N");
            ScreenPoint = screenPoint;
            CandidateHwnd = candidateHwnd;
        }

        internal string RequestId { get; private set; }

        internal Point ScreenPoint { get; private set; }

        /// <summary>
        /// Optional HWND already obtained by the existing picker. An adapter still
        /// validates/normalizes it to the actual owner-draw host before injection.
        /// </summary>
        internal IntPtr CandidateHwnd { get; private set; }
    }

    internal sealed class OwnerDrawSelector
    {
        internal int SchemaVersion { get; set; }

        internal string AdapterId { get; set; }

        internal string HostClassName { get; set; }

        internal OwnerDrawIdentityStrength IdentityStrength { get; set; }

        // PowerBuilder/MFC-GDI visual identity (deprecated: superseded by the
        // logical PbKind/PbRow/PbCol/PbField identity; kept only for reference).
        internal int CellIndex { get; set; } = -1;

        internal int LineIndex { get; set; } = -1;

        internal int SegmentIndex { get; set; } = -1;

        /// <summary>
        /// Deprecated: superseded by <see cref="PbKind"/>. Kept for reference only.
        /// </summary>
        internal string CellSource { get; set; }

        // PowerBuilder geometric/logical identity (replaces visual ordinal identity).
        internal PowerBuilderCellKind PbKind { get; set; } = PowerBuilderCellKind.GridCell;

        internal int PbRow { get; set; } = -1;

        internal int PbCol { get; set; } = -1;

        /// <summary>Diagnostic header field name for a grid-cell; not an identity guard.</summary>
        internal string PbField { get; set; }

        /// <summary>
        /// Client rectangle that is the identity for a free-form PB input cell.
        /// Unused for grid-cell (row/col) and label (exact text) kinds.
        /// </summary>
        internal Rectangle CellClientBounds { get; set; }

        // VCL semantic identity. Row -1 denotes a column header.
        internal int LogicalRow { get; set; } = int.MinValue;

        internal int LogicalColumn { get; set; } = int.MinValue;

        internal string FieldName { get; set; }

        internal string IdentitySource { get; set; }

        // Diagnostic by default. The selector UI may explicitly promote it to
        // an exact guard, but changing business text must not silently do so.
        internal string ExpectedText { get; set; }

        internal bool EnforceText { get; set; }
    }

    internal sealed class OwnerDrawElementSnapshot
    {
        internal string RequestId { get; set; }

        internal string AdapterId { get; set; }

        internal int ProcessId { get; set; }

        internal IntPtr HostHwnd { get; set; }

        internal string HostClassName { get; set; }

        internal Rectangle ScreenBounds { get; set; }

        internal Rectangle ClientBounds { get; set; }

        internal string Text { get; set; }

        /// <summary>Distinguishes a real blank cell from a failed text read.</summary>
        internal bool TextAvailable { get; set; }

        internal OwnerDrawSelector Selector { get; set; }

        internal string NativeStatus { get; set; }
    }

    internal sealed class OwnerDrawBridgeDescriptor
    {
        internal OwnerDrawBridgeDescriptor(string adapterId, string x86FileName,
            string x64FileName, string legacyFileName, int requiredAbi,
            OwnerDrawBridgeHealthProbe healthProbe,
            bool allowX64ControllerToX86Target = false)
        {
            if (string.IsNullOrWhiteSpace(adapterId))
                throw new ArgumentException("Adapter id is required.", "adapterId");
            if (string.IsNullOrWhiteSpace(legacyFileName)
                || !string.Equals(Path.GetFileName(legacyFileName), legacyFileName,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Legacy bridge file name must be a file name without a path.",
                    "legacyFileName");
            }
            if (healthProbe == null)
                throw new ArgumentNullException("healthProbe");
            AdapterId = adapterId;
            X86FileName = x86FileName ?? string.Empty;
            X64FileName = x64FileName ?? string.Empty;
            LegacyFileName = legacyFileName;
            RequiredAbi = requiredAbi;
            HealthProbe = healthProbe;
            AllowX64ControllerToX86Target = allowX64ControllerToX86Target;
        }

        internal string AdapterId { get; private set; }

        internal string X86FileName { get; private set; }

        internal string X64FileName { get; private set; }

        /// <summary>
        /// An earlier validation build used an architecture-neutral module name while
        /// exposing the same per-process pipe. Its mere residency is therefore a
        /// conflict, even when that older bridge has already been stopped.
        /// </summary>
        internal string LegacyFileName { get; private set; }

        /// <summary>
        /// Exact ABI understood by the managed client. A larger number is a
        /// different contract, not an implicitly compatible upgrade.
        /// </summary>
        internal int RequiredAbi { get; private set; }

        internal OwnerDrawBridgeHealthProbe HealthProbe { get; private set; }

        /// <summary>
        /// Allows a 64-bit controller to activate this adapter's x86 Bridge through
        /// the packaged x86 Broker. Same-bitness activation never uses this flag.
        /// </summary>
        internal bool AllowX64ControllerToX86Target { get; private set; }

        internal string GetFileName(OwnerDrawArchitecture architecture)
        {
            return architecture == OwnerDrawArchitecture.X64 ? X64FileName : X86FileName;
        }
    }

    internal sealed class OwnerDrawBridgeSession
    {
        internal string AdapterId { get; set; }

        internal int ProcessId { get; set; }

        internal IntPtr HostHwnd { get; set; }

        internal OwnerDrawArchitecture Architecture { get; set; }

        internal string BridgePath { get; set; }

        internal DateTime StartedUtc { get; set; }

        internal DateTime ProcessStartTimeUtc { get; set; }
    }

    internal sealed class OwnerDrawSelectorProperty
    {
        internal OwnerDrawSelectorProperty(string name, string value, bool accurate)
        {
            Name = name ?? string.Empty;
            Value = value ?? string.Empty;
            Accurate = accurate;
        }

        internal string Name { get; private set; }

        internal string Value { get; private set; }

        internal bool Accurate { get; private set; }
    }
}
