using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Core.Threading;
using II.RPA.Plugin.Excel.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace II.RPA.Plugin.Excel
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
            Add<GetExcelRule>(builder, PluginProtocol.AbilityNames.GetExcelRule);
            Add<GetSystemFont>(builder, PluginProtocol.AbilityNames.GetSystemFont);
            Add<Run>(builder, PluginProtocol.AbilityNames.Run);
            Add<SaveInstance>(builder, PluginProtocol.AbilityNames.SaveInstance);
        }

        private static void Add<TAbility>(PluginPackageBuilder builder, string name) where TAbility : class, IPluginAbility =>
            builder.AddAbility<TAbility>(name, AbilityLifetime.Transient, AbilityConcurrencyMode.RejectWhenBusy);
    }

    internal static class PluginProtocol
    {
        internal const string PackageName = "Excel";

        internal static class AbilityNames
        {
            internal const string GetExcelRule = "GetExcelRule";
            internal const string GetSystemFont = "GetSystemFont";
            internal const string Run = "Run";
            internal const string SaveInstance = "SaveInstance";
        }
    }
}
