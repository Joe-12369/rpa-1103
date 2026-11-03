using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Core.Threading;
using II.RPA.Plugin.Dialog.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace II.RPA.Plugin.Dialog
{
    public sealed class PluginPackage : PluginPackageBase
    {
        public override string PackageName => PluginProtocol.PackageName;

        protected override void Configure(PluginPackageBuilder builder)
        {
            RegisterAbilities(builder);
            builder.Services.AddSingleton<IStaDispatcher, StaDispatcher>();
        }

        private static void RegisterAbilities(PluginPackageBuilder builder)
        {
            builder.AddAbility<ConfigInput>(PluginProtocol.AbilityNames.ConfigInput, AbilityLifetime.Transient, AbilityConcurrencyMode.RejectWhenBusy);
            builder.AddAbility<ConfigInfo>(PluginProtocol.AbilityNames.ConfigInfo, AbilityLifetime.Transient, AbilityConcurrencyMode.RejectWhenBusy);
            builder.AddAbility<ConfigDialog>(PluginProtocol.AbilityNames.ConfigDialog, AbilityLifetime.Transient, AbilityConcurrencyMode.RejectWhenBusy);
        }
    }

    internal static class PluginProtocol
    {
        internal const string PackageName = "Dialog";

        internal static class AbilityNames
        {
            internal const string ConfigDialog = "ConfigDialog";
            internal const string ConfigInfo = "ConfigInfo";
            internal const string ConfigInput = "ConfigInput";
        }
    }
}
