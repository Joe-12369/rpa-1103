using System;
using System.IO;
using WebExtensionInstall;
using Xunit;

namespace WebExtensionInstall.Tests
{
    public class EdgeExtensionRegistryPlanTests
    {
        [Theory]
        [InlineData(149, "badpelibibaolgkipoeccipldjmmbocd", "IIRPAEdgeExtensionV2.crx", "2.7")]
        [InlineData(150, "kghkafgpmdhgjkeihhhbeikbgdllgdoo", "IIRPAEdgeExtension.crx", "4.0.1.235")]
        public void Create_UsesSelectedExtensionAndUnifiedNativeHost(
            int edgeMajorVersion,
            string expectedId,
            string expectedFile,
            string expectedVersion)
        {
            string installDirectory = Path.Combine("C:\\Users", "test", "RPAEdgeExtension");
            EdgeExtensionRegistryPlan plan = EdgeExtensionRegistryPlan.Create(
                EdgeExtensionPolicy.Select(edgeMajorVersion),
                installDirectory);

            Assert.Equal($@"SOFTWARE\Microsoft\Edge\Extensions\{expectedId}", plan.ExtensionRegistryPath);
            Assert.Equal(Path.Combine(installDirectory, expectedFile), plan.CrxPath);
            Assert.Equal(expectedVersion, plan.ExtensionVersion);
            Assert.Equal(
                @"SOFTWARE\Microsoft\Edge\NativeMessagingHosts\ii.rpa.chromenativemsg",
                plan.NativeHostRegistryPath);
            Assert.Equal(Path.Combine(installDirectory, "manifest.json"), plan.NativeHostManifestPath);
        }
    }
}
