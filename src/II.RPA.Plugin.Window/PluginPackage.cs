using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Window.Plugins;

namespace II.RPA.Plugin.Window
{
    public sealed class PluginPackage : PluginPackageBase
    {
        public override string PackageName => PluginProtocol.PackageName;
        protected override void Configure(PluginPackageBuilder builder) => RegisterAbilities(builder);
        private static void RegisterAbilities(PluginPackageBuilder builder)
        {
            Add<ActiveWindow>(builder, PluginProtocol.AbilityNames.ActiveWindow);
            Add<CloseWindow>(builder, PluginProtocol.AbilityNames.CloseWindow);
            Add<GetWindowData>(builder, PluginProtocol.AbilityNames.GetWindowData);
            Add<GetWindowDataList>(builder, PluginProtocol.AbilityNames.GetWindowDataList);
            Add<GetWindowList>(builder, PluginProtocol.AbilityNames.GetWindowList);
            Add<GetWindowRect>(builder, PluginProtocol.AbilityNames.GetWindowRect);
            Add<MoveWindow>(builder, PluginProtocol.AbilityNames.MoveWindow);
            Add<SetWindowShow>(builder, PluginProtocol.AbilityNames.SetWindowShow);
            Add<SetWindowState>(builder, PluginProtocol.AbilityNames.SetWindowState);
            Add<SizeWindow>(builder, PluginProtocol.AbilityNames.SizeWindow);
            Add<WaitWindow>(builder, PluginProtocol.AbilityNames.WaitWindow);
            Add<WindowIsExists>(builder, PluginProtocol.AbilityNames.WindowIsExists);
        }
        private static void Add<TAbility>(PluginPackageBuilder builder, string abilityName) where TAbility : class, IPluginAbility =>
            builder.AddAbility<TAbility>(abilityName, AbilityLifetime.Transient, AbilityConcurrencyMode.RejectWhenBusy);
    }

    internal static class PluginProtocol
    {
        internal const string PackageName = "Window";

        internal static class AbilityNames
        {
            internal const string ActiveWindow = "ActiveWindow";
            internal const string CloseWindow = "CloseWindow";
            internal const string GetWindowData = "GetWindowData";
            internal const string GetWindowDataList = "GetWindowDataList";
            internal const string GetWindowList = "GetWindowList";
            internal const string GetWindowRect = "GetWindowRect";
            internal const string MoveWindow = "MoveWindow";
            internal const string SetWindowShow = "SetWindowShow";
            internal const string SetWindowState = "SetWindowState";
            internal const string SizeWindow = "SizeWindow";
            internal const string WaitWindow = "WaitWindow";
            internal const string WindowIsExists = "WindowIsExists";
        }
    }
}
