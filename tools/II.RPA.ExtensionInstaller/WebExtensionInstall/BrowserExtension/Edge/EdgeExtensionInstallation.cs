using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;

namespace WebExtensionInstall
{
    public static class EdgeExtensionInstallation
    {
        public const string NativeHostName = "ii.rpa.chromenativemsg";
        public const string NativeHostManifestFileName = "manifest.json";
        public const string NativeHostExecutableFileName = "II.RPA.NativeMessagingHost.exe";
        public const string NativeHostConfigFileName = "II.RPA.NativeMessagingHost.exe.config";

        public static bool IsInstalled(
            EdgeExtensionSelection selection,
            string installDirectory,
            string registeredCrxPath,
            string registeredExtensionVersion,
            string registeredNativeHostManifestPath)
        {
            if (selection == null || string.IsNullOrWhiteSpace(installDirectory))
            {
                return false;
            }

            string expectedCrxPath = Path.Combine(installDirectory, selection.CrxFileName);
            string expectedManifestPath = Path.Combine(installDirectory, NativeHostManifestFileName);
            if (!PathEquals(registeredCrxPath, expectedCrxPath)
                || !string.Equals(registeredExtensionVersion, selection.ExtensionVersion, StringComparison.Ordinal)
                || !PathEquals(registeredNativeHostManifestPath, expectedManifestPath)
                || !File.Exists(expectedCrxPath)
                || !File.Exists(Path.Combine(installDirectory, NativeHostExecutableFileName))
                || !File.Exists(Path.Combine(installDirectory, NativeHostConfigFileName))
                || !File.Exists(expectedManifestPath))
            {
                return false;
            }

            try
            {
                JObject manifest = JObject.Parse(File.ReadAllText(expectedManifestPath));
                if (!string.Equals(manifest.Value<string>("name"), NativeHostName, StringComparison.Ordinal)
                    || !string.Equals(manifest.Value<string>("type"), "stdio", StringComparison.Ordinal)
                    || !NativeHostPathMatches(manifest.Value<string>("path"), installDirectory))
                {
                    return false;
                }

                string expectedOrigin = $"chrome-extension://{selection.ExtensionId}/";
                return manifest["allowed_origins"] is JArray origins
                    && origins.Values<string>().Any(origin => string.Equals(origin, expectedOrigin, StringComparison.Ordinal));
            }
            catch
            {
                return false;
            }
        }

        private static bool NativeHostPathMatches(string manifestPath, string installDirectory)
        {
            if (string.IsNullOrWhiteSpace(manifestPath))
            {
                return false;
            }

            string resolvedPath = Path.IsPathRooted(manifestPath)
                ? manifestPath
                : Path.Combine(installDirectory, manifestPath);
            return PathEquals(resolvedPath, Path.Combine(installDirectory, NativeHostExecutableFileName));
        }

        private static bool PathEquals(string left, string right)
        {
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            {
                return false;
            }

            try
            {
                return string.Equals(
                    Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}
