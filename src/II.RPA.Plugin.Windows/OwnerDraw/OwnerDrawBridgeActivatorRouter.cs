using System;
using System.Diagnostics;
using System.IO;

namespace II.RPA.Plugin.Windows.OwnerDraw
{
    /// <summary>
    /// Keeps the established in-process activator for matching architectures and
    /// routes only the explicitly supported x64-controller/x86-PB combination to
    /// the packaged x86 Broker. No other cross-bitness combination is widened.
    /// </summary>
    internal sealed class OwnerDrawBridgeActivatorRouter
        : IOwnerDrawBridgeActivator
    {
        private const string PowerBuilderBridgeX86 = "MfcGdiAccBridge.x86.dll";
        private const string PowerBuilderLegacy = "MfcGdiAccBridge.dll";
        private const int PowerBuilderAbi = 6;

        private readonly string bridgeDirectory;
        private readonly IOwnerDrawBridgeActivator nativeActivator;
        private readonly IOwnerDrawInjectorBrokerClient brokerClient;
        private readonly int controllerProcessId;

        internal OwnerDrawBridgeActivatorRouter(string bridgeDirectory,
            IOwnerDrawBridgeActivator nativeActivator = null,
            IOwnerDrawInjectorBrokerClient brokerClient = null)
        {
            if (string.IsNullOrWhiteSpace(bridgeDirectory))
                throw new ArgumentException("Bridge directory is required.",
                    "bridgeDirectory");
            this.bridgeDirectory = Path.GetFullPath(bridgeDirectory);
            this.nativeActivator = nativeActivator
                ?? new NativeOwnerDrawBridgeActivator();
            this.brokerClient = brokerClient
                ?? new OwnerDrawInjectorBrokerClient(this.bridgeDirectory);
            using (Process current = Process.GetCurrentProcess())
                controllerProcessId = current.Id;
        }

        public bool TryCheckLegacyConflict(int processId, string legacyFileName,
            out bool found, out string status)
        {
            found = false;
            if (!TrySelectRoute(processId, null, legacyFileName, 0,
                allowIdentityFromLegacyOnly: true, out bool useBroker,
                out string routeStatus))
            {
                status = routeStatus;
                return false;
            }
            if (!useBroker)
                return nativeActivator.TryCheckLegacyConflict(processId,
                    legacyFileName, out found, out status);

            string bridgePath = Path.Combine(bridgeDirectory,
                PowerBuilderBridgeX86);
            bool result = brokerClient.TryInspect(processId,
                controllerProcessId, bridgePath, legacyFileName,
                PowerBuilderAbi, out found, out status);
            status = routeStatus + " " + status;
            return result;
        }

        public bool TryStart(int processId, string bridgePath,
            string legacyFileName, int requiredAbi, int requestedControllerProcessId,
            out string status)
        {
            if (requestedControllerProcessId != controllerProcessId)
            {
                status = "Bridge controller PID does not identify this RPA process: "
                    + "requested=" + requestedControllerProcessId + " actual="
                    + controllerProcessId + ".";
                return false;
            }
            if (!TrySelectRoute(processId, bridgePath, legacyFileName,
                requiredAbi, allowIdentityFromLegacyOnly: false,
                out bool useBroker, out string routeStatus))
            {
                status = routeStatus;
                return false;
            }
            bool result = useBroker
                ? brokerClient.TryStart(processId, controllerProcessId,
                    bridgePath, legacyFileName, requiredAbi, out status)
                : nativeActivator.TryStart(processId, bridgePath,
                    legacyFileName, requiredAbi, controllerProcessId, out status);
            status = routeStatus + " " + status;
            return result;
        }

        public bool TryStopResident(int processId, string bridgePath,
            string legacyFileName, int requiredAbi,
            out OwnerDrawBridgeStopState stopState, out string status)
        {
            stopState = OwnerDrawBridgeStopState.NotLoaded;
            if (!TrySelectRoute(processId, bridgePath, legacyFileName,
                requiredAbi, allowIdentityFromLegacyOnly: false,
                out bool useBroker, out string routeStatus))
            {
                status = routeStatus;
                return false;
            }
            bool result = useBroker
                ? brokerClient.TryStop(processId, controllerProcessId,
                    bridgePath, legacyFileName, requiredAbi,
                    out stopState, out status)
                : nativeActivator.TryStopResident(processId, bridgePath,
                    legacyFileName, requiredAbi, out stopState, out status);
            status = routeStatus + " " + status;
            return result;
        }

        private bool TrySelectRoute(int processId, string bridgePath,
            string legacyFileName, int requiredAbi,
            bool allowIdentityFromLegacyOnly, out bool useBroker,
            out string status)
        {
            useBroker = false;
            if (!OwnerDrawWindowProbe.TryGetArchitecture(processId,
                out OwnerDrawArchitecture targetArchitecture,
                out string architectureStatus))
            {
                status = architectureStatus;
                return false;
            }

            OwnerDrawArchitecture controllerArchitecture = Environment.Is64BitProcess
                ? OwnerDrawArchitecture.X64 : OwnerDrawArchitecture.X86;
            if (targetArchitecture == controllerArchitecture)
            {
                status = "Same-bitness Bridge activation uses the native path: "
                    + "controller=" + ToText(controllerArchitecture) + " target="
                    + ToText(targetArchitecture) + ".";
                return true;
            }

            if (controllerArchitecture != OwnerDrawArchitecture.X64
                || targetArchitecture != OwnerDrawArchitecture.X86)
            {
                status = "Unsupported cross-bitness Bridge route: controller="
                    + ToText(controllerArchitecture) + " target="
                    + ToText(targetArchitecture) + ".";
                return false;
            }

            bool legacyMatches = string.Equals(legacyFileName,
                PowerBuilderLegacy, StringComparison.Ordinal);
            bool exactIdentity = bridgePath != null
                && string.Equals(Path.GetFileName(bridgePath),
                    PowerBuilderBridgeX86, StringComparison.Ordinal)
                && requiredAbi == PowerBuilderAbi;
            if (!legacyMatches || (!allowIdentityFromLegacyOnly && !exactIdentity))
            {
                status = "The x86 Broker is restricted to exact PB identity "
                    + "MfcGdiAccBridge.x86.dll / MfcGdiAccBridge.dll / ABI 6.";
                return false;
            }

            if (bridgePath != null && !IsDirectChild(bridgeDirectory, bridgePath))
            {
                status = "The x86 PB Bridge must be a direct file in the configured "
                    + "OwnerDraw directory.";
                return false;
            }
            useBroker = true;
            status = "Cross-bitness Bridge activation uses the packaged x86 Broker: "
                + "controller=x64 target=x86.";
            return true;
        }

        internal static bool IsDirectChild(string directory, string filePath)
        {
            try
            {
                string expectedDirectory = Path.GetFullPath(directory)
                    .TrimEnd(Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar);
                string actualDirectory = (Path.GetDirectoryName(
                    Path.GetFullPath(filePath)) ?? string.Empty)
                    .TrimEnd(Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar);
                return string.Equals(expectedDirectory, actualDirectory,
                    StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex) when (ex is ArgumentException
                || ex is NotSupportedException || ex is PathTooLongException)
            {
                return false;
            }
        }

        private static string ToText(OwnerDrawArchitecture architecture)
        {
            return architecture == OwnerDrawArchitecture.X64 ? "x64" : "x86";
        }
    }
}
