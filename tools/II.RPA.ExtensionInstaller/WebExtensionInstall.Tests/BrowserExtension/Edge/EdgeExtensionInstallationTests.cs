using System;
using System.IO;
using WebExtensionInstall;
using Xunit;

namespace WebExtensionInstall.Tests
{
    public class EdgeExtensionInstallationTests : IDisposable
    {
        private readonly string _installDirectory = Path.Combine(
            Path.GetTempPath(),
            "WebExtensionInstall.Tests",
            Guid.NewGuid().ToString("N"));

        [Fact]
        public void IsInstalled_ReturnsTrueForCompleteV3Installation()
        {
            EdgeExtensionSelection selection = EdgeExtensionPolicy.Select(150);
            CreateCompleteInstallation(selection);

            bool installed = EdgeExtensionInstallation.IsInstalled(
                selection,
                _installDirectory,
                Path.Combine(_installDirectory, selection.CrxFileName),
                selection.ExtensionVersion,
                Path.Combine(_installDirectory, "manifest.json"));

            Assert.True(installed);
        }

        [Fact]
        public void IsInstalled_ReturnsFalseWhenRegistryPointsToOtherGeneration()
        {
            EdgeExtensionSelection selection = EdgeExtensionPolicy.Select(150);
            CreateCompleteInstallation(selection);

            bool installed = EdgeExtensionInstallation.IsInstalled(
                selection,
                _installDirectory,
                Path.Combine(_installDirectory, EdgeExtensionPolicy.V2CrxFileName),
                selection.ExtensionVersion,
                Path.Combine(_installDirectory, "manifest.json"));

            Assert.False(installed);
        }

        [Fact]
        public void IsInstalled_ReturnsFalseWhenManifestDoesNotAllowTargetId()
        {
            EdgeExtensionSelection selection = EdgeExtensionPolicy.Select(149);
            CreateCompleteInstallation(selection, "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

            bool installed = EdgeExtensionInstallation.IsInstalled(
                selection,
                _installDirectory,
                Path.Combine(_installDirectory, selection.CrxFileName),
                selection.ExtensionVersion,
                Path.Combine(_installDirectory, "manifest.json"));

            Assert.False(installed);
        }

        [Fact]
        public void IsInstalled_ReturnsFalseWhenNativeHostExecutableIsMissing()
        {
            EdgeExtensionSelection selection = EdgeExtensionPolicy.Select(150);
            CreateCompleteInstallation(selection);
            File.Delete(Path.Combine(_installDirectory, "II.RPA.NativeMessagingHost.exe"));

            bool installed = EdgeExtensionInstallation.IsInstalled(
                selection,
                _installDirectory,
                Path.Combine(_installDirectory, selection.CrxFileName),
                selection.ExtensionVersion,
                Path.Combine(_installDirectory, "manifest.json"));

            Assert.False(installed);
        }

        public void Dispose()
        {
            if (Directory.Exists(_installDirectory))
            {
                Directory.Delete(_installDirectory, true);
            }
        }

        private void CreateCompleteInstallation(EdgeExtensionSelection selection, string allowedId = null)
        {
            Directory.CreateDirectory(_installDirectory);
            File.WriteAllText(Path.Combine(_installDirectory, selection.CrxFileName), "crx");
            File.WriteAllText(Path.Combine(_installDirectory, "II.RPA.NativeMessagingHost.exe"), "exe");
            File.WriteAllText(Path.Combine(_installDirectory, "II.RPA.NativeMessagingHost.exe.config"), "config");
            File.WriteAllText(
                Path.Combine(_installDirectory, "manifest.json"),
                $@"{{
                  'name': 'ii.rpa.chromenativemsg',
                  'type': 'stdio',
                  'path': 'II.RPA.NativeMessagingHost.exe',
                  'allowed_origins': ['chrome-extension://{allowedId ?? selection.ExtensionId}/']
                }}");
        }
    }
}
