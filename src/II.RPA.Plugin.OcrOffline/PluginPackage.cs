using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.OcrOffline.Plugins;

namespace II.RPA.Plugin.OcrOffline
{
    public sealed class PluginPackage : PluginPackageBase
    {
        public override string PackageName => PluginProtocol.PackageName;
        protected override void Configure(PluginPackageBuilder builder) => RegisterAbilities(builder);
        private static void RegisterAbilities(PluginPackageBuilder builder)
        {
            Add<ExecuteCV>(builder, PluginProtocol.AbilityNames.ExecuteCV);
            Add<GetOcrTableDataByImg>(builder, PluginProtocol.AbilityNames.GetOcrTableDataByImg);
            Add<GetOcrTableDataBySelectorAILimit>(builder, PluginProtocol.AbilityNames.GetOcrTableDataBySelectorAILimit);
            Add<GetOcrTableDataBySelector>(builder, PluginProtocol.AbilityNames.GetOcrTableDataBySelector);
            Add<GetTextByCV_V2>(builder, PluginProtocol.AbilityNames.GetTextByCV_V2);
            Add<GetTextByCV>(builder, PluginProtocol.AbilityNames.GetTextByCV);
            Add<MakeOcrTableSelector>(builder, PluginProtocol.AbilityNames.MakeOcrTableSelector);
        }
        private static void Add<TAbility>(PluginPackageBuilder builder, string abilityName) where TAbility : class, IPluginAbility =>
            builder.AddAbility<TAbility>(abilityName, AbilityLifetime.Transient, AbilityConcurrencyMode.RejectWhenBusy);
    }

    internal static class PluginProtocol
    {
        internal const string PackageName = "OcrOffline";

        internal static class AbilityNames
        {
            internal const string ExecuteCV = "ExecuteCV";
            internal const string GetOcrTableDataByImg = "GetOcrTableDataByImg";
            internal const string GetOcrTableDataBySelector = "GetOcrTableDataBySelector";
            internal const string GetOcrTableDataBySelectorAILimit = "GetOcrTableDataBySelectorAILimit";
            internal const string GetTextByCV = "GetTextByCV";
            internal const string GetTextByCV_V2 = "GetTextByCV_V2";
            internal const string MakeOcrTableSelector = "MakeOcrTableSelector";
        }
    }
}
