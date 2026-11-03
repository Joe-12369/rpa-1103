using II.RPA.Plugin.Core.PluginBase;

namespace II.RPA.Plugin.WdtErp
{
    public sealed class PluginPackage : PluginPackageBase
    {
        public override string PackageName => PluginProtocol.PackageName;

        protected override void Configure(PluginPackageBuilder builder)
        {
        }
    }

    internal static class PluginProtocol
    {
        internal const string PackageName = "WdtErp";
    }
}
