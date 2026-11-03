using WebExtensionInstall;
using Xunit;

namespace WebExtensionInstall.Tests
{
    public class EdgeExtensionPolicyTests
    {
        [Theory]
        [InlineData(149, EdgeExtensionGeneration.V2, "badpelibibaolgkipoeccipldjmmbocd", "IIRPAEdgeExtensionV2.crx", "2.7")]
        [InlineData(150, EdgeExtensionGeneration.V3, "kghkafgpmdhgjkeihhhbeikbgdllgdoo", "IIRPAEdgeExtension.crx", "4.0.1.235")]
        [InlineData(151, EdgeExtensionGeneration.V3, "kghkafgpmdhgjkeihhhbeikbgdllgdoo", "IIRPAEdgeExtension.crx", "4.0.1.235")]
        public void Select_UsesExpectedGenerationAtVersionBoundary(
            int edgeMajorVersion,
            EdgeExtensionGeneration expectedGeneration,
            string expectedId,
            string expectedFileName,
            string expectedVersion)
        {
            EdgeExtensionSelection result = EdgeExtensionPolicy.Select(edgeMajorVersion);

            Assert.Equal(expectedGeneration, result.Generation);
            Assert.Equal(expectedId, result.ExtensionId);
            Assert.Equal(expectedFileName, result.CrxFileName);
            Assert.Equal(expectedVersion, result.ExtensionVersion);
        }
    }
}
