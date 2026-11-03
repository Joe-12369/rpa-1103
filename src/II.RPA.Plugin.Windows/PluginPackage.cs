using II.RPA.Plugin.Core.PluginBase;

namespace II.RPA.Plugin.Windows
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
        internal const string PackageName = "GUIWindows";
    }
}
