using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.GoogleSheets.Plugins;

namespace II.RPA.Plugin.GoogleSheets
{
    public sealed class PluginPackage : PluginPackageBase
    {
        public override string PackageName => PluginProtocol.PackageName;

        protected override void Configure(PluginPackageBuilder builder) => RegisterAbilities(builder);

        private static void RegisterAbilities(PluginPackageBuilder builder)
        {
            Add<AddFilterView>(builder, PluginProtocol.AbilityNames.AddFilterView);
            Add<AddNewSheet>(builder, PluginProtocol.AbilityNames.AddNewSheet);
            Add<CopyPasteSheet>(builder, PluginProtocol.AbilityNames.CopyPasteSheet);
            Add<DownloadSheets>(builder, PluginProtocol.AbilityNames.DownloadSheets);
            Add<GetColumnsByTableTitle>(builder, PluginProtocol.AbilityNames.GetColumnsByTableTitle);
            Add<GetSheetRowCount>(builder, PluginProtocol.AbilityNames.GetSheetRowCount);
            Add<GetAllData>(builder, PluginProtocol.AbilityNames.GetAllData);
            Add<GetAllSheetsName>(builder, PluginProtocol.AbilityNames.GetAllSheetsName);
            Add<RenameSheet>(builder, PluginProtocol.AbilityNames.RenameSheet);
            Add<WriteToSheetCell>(builder, PluginProtocol.AbilityNames.WriteToSheetCell);
            Add<WriteToSheetCellFormula>(builder, PluginProtocol.AbilityNames.WriteToSheetCellFormula);
            Add<WriteToSheetRegion>(builder, PluginProtocol.AbilityNames.WriteToSheetRegion);
        }

        private static void Add<TAbility>(PluginPackageBuilder builder, string abilityName)
            where TAbility : class, IPluginAbility
        {
            builder.AddAbility<TAbility>(abilityName, AbilityLifetime.Transient, AbilityConcurrencyMode.RejectWhenBusy);
        }
    }

    internal static class PluginProtocol
    {
        internal const string PackageName = "GoogleTable";

        internal static class AbilityNames
        {
            internal const string AddFilterView = "AddFilterView";
            internal const string AddNewSheet = "AddNewSheet";
            internal const string CopyPasteSheet = "CopyPasteSheet";
            internal const string DownloadSheets = "DownloadSheets";
            internal const string GetAllData = "GetAllData";
            internal const string GetAllSheetsName = "GetAllSheetsName";
            internal const string GetColumnsByTableTitle = "GetColumnsByTableTitle";
            internal const string GetSheetRowCount = "GetSheetRowCount";
            internal const string RenameSheet = "RenameSheet";
            internal const string WriteToSheetCell = "WriteToSheetCell";
            internal const string WriteToSheetCellFormula = "WriteToSheetCellFormula";
            internal const string WriteToSheetRegion = "WriteToSheetRegion";
        }
    }
}
