namespace WebExtensionInstall
{
    public enum EdgeExtensionGeneration
    {
        V2,
        V3
    }

    public sealed class EdgeExtensionSelection
    {
        public EdgeExtensionSelection(
            EdgeExtensionGeneration generation,
            string extensionId,
            string crxFileName,
            string extensionVersion)
        {
            Generation = generation;
            ExtensionId = extensionId;
            CrxFileName = crxFileName;
            ExtensionVersion = extensionVersion;
        }

        public EdgeExtensionGeneration Generation { get; }

        public string ExtensionId { get; }

        public string CrxFileName { get; }

        public string ExtensionVersion { get; }
    }

    public static class EdgeExtensionPolicy
    {
        public const int V3MinBrowserVersion = 150;
        public const string V2ExtensionId = "badpelibibaolgkipoeccipldjmmbocd";
        public const string V3ExtensionId = "kghkafgpmdhgjkeihhhbeikbgdllgdoo";
        public const string LegacyTemporaryV3ExtensionId = "pcopnibgkbdnlaeagepigbboebdfejmb";
        public const string V2CrxFileName = "IIRPAEdgeExtensionV2.crx";
        public const string V3CrxFileName = "IIRPAEdgeExtension.crx";
        public const string V2ExtensionVersion = "2.7";
        public const string V3ExtensionVersion = "4.0.1.235";

        public static EdgeExtensionSelection Select(int edgeMajorVersion)
        {
            return edgeMajorVersion < V3MinBrowserVersion
                ? new EdgeExtensionSelection(
                    EdgeExtensionGeneration.V2,
                    V2ExtensionId,
                    V2CrxFileName,
                    V2ExtensionVersion)
                : new EdgeExtensionSelection(
                    EdgeExtensionGeneration.V3,
                    V3ExtensionId,
                    V3CrxFileName,
                    V3ExtensionVersion);
        }
    }
}
