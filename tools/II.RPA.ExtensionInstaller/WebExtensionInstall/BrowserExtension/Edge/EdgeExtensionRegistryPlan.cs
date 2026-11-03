using System.IO;

namespace WebExtensionInstall
{
    public sealed class EdgeExtensionRegistryPlan
    {
        private EdgeExtensionRegistryPlan()
        {
        }

        public string ExtensionRegistryPath { get; private set; }

        public string CrxPath { get; private set; }

        public string ExtensionVersion { get; private set; }

        public string NativeHostRegistryPath { get; private set; }

        public string NativeHostManifestPath { get; private set; }

        public static EdgeExtensionRegistryPlan Create(
            EdgeExtensionSelection selection,
            string installDirectory)
        {
            return new EdgeExtensionRegistryPlan
            {
                ExtensionRegistryPath = $@"SOFTWARE\Microsoft\Edge\Extensions\{selection.ExtensionId}",
                CrxPath = Path.Combine(installDirectory, selection.CrxFileName),
                ExtensionVersion = selection.ExtensionVersion,
                NativeHostRegistryPath = $@"SOFTWARE\Microsoft\Edge\NativeMessagingHosts\{EdgeExtensionInstallation.NativeHostName}",
                NativeHostManifestPath = Path.Combine(installDirectory, EdgeExtensionInstallation.NativeHostManifestFileName)
            };
        }
    }
}
