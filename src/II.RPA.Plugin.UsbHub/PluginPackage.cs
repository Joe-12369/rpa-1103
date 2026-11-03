using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.UsbHub.Plugins;

namespace II.RPA.Plugin.UsbHub
{
    public sealed class PluginPackage : PluginPackageBase
    {
        public override string PackageName => PluginProtocol.PackageName;
        protected override void Configure(PluginPackageBuilder builder) => RegisterAbilities(builder);

        private static void RegisterAbilities(PluginPackageBuilder builder)
        {
            Add<LocalClose>(builder, PluginProtocol.AbilityNames.LocalClose);
            Add<LocalGetComs>(builder, PluginProtocol.AbilityNames.LocalGetComs);
            Add<LocalOpen>(builder, PluginProtocol.AbilityNames.LocalOpen);
            Add<NetworkClose>(builder, PluginProtocol.AbilityNames.NetworkClose);
            Add<NetworkOpen>(builder, PluginProtocol.AbilityNames.NetworkOpen);
        }

        private static void Add<TAbility>(PluginPackageBuilder builder, string abilityName) where TAbility : class, IPluginAbility
        {
            builder.AddAbility<TAbility>(abilityName, AbilityLifetime.Transient, AbilityConcurrencyMode.RejectWhenBusy);
        }
    }

    internal static class PluginProtocol
    {
        internal const string PackageName = "UsbHub";

        internal static class AbilityNames
        {
            internal const string LocalClose = "LocalClose";
            internal const string LocalGetComs = "LocalGetComs";
            internal const string LocalOpen = "LocalOpen";
            internal const string NetworkClose = "NetworkClose";
            internal const string NetworkOpen = "NetworkOpen";
        }
    }
}
