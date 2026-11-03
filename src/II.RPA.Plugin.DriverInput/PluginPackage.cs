using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.DriverInput.Plugins;

namespace II.RPA.Plugin.DriverInput
{
    public sealed class PluginPackage : PluginPackageBase
    {
        public override string PackageName => PluginProtocol.PackageName;

        protected override void Configure(PluginPackageBuilder builder) => RegisterAbilities(builder);

        private static void RegisterAbilities(PluginPackageBuilder builder)
        {
            Add<Plugins.DriverInput>(builder, PluginProtocol.AbilityNames.DriverInput);
            Add<DriverSearch>(builder, PluginProtocol.AbilityNames.DriverSearch);
        }

        private static void Add<TAbility>(PluginPackageBuilder builder, string abilityName)
            where TAbility : class, IPluginAbility
        {
            builder.AddAbility<TAbility>(abilityName, AbilityLifetime.Transient, AbilityConcurrencyMode.RejectWhenBusy);
        }
    }

    internal static class PluginProtocol
    {
        internal const string PackageName = "DriverInput";

        internal static class AbilityNames
        {
            internal const string DriverInput = "DriverInput";
            internal const string DriverSearch = "DriverSearch";
        }
    }
}
