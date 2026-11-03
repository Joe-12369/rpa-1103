using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.DesktopInput.InputMethod;
using II.RPA.Plugin.DesktopInput.Mouse;
using II.RPA.Plugin.DesktopInput.Mouse.Plugins;
using System.Collections.Generic;

namespace II.RPA.Plugin.DesktopInput
{
    public sealed class PluginPackage : PluginPackageBase
    {
        public override string PackageName => PluginProtocol.PackageName;

        public override IReadOnlyCollection<string> RoutePackageNames => PluginProtocol.RoutePackageNames;

        protected override void Configure(PluginPackageBuilder builder)
        {
            /*这里做一个同Abiltity 多路由注册的例子 DesktopInput.MouseMove与 Mouse.MouseMove*/
            Add<MouseMove>(builder, PluginProtocol.RoutePackageName.DesktopInput, PluginProtocol.AbilityNames.MouseMove);


            Add<MouseMove>(builder, PluginProtocol.RoutePackageName.Mouse, PluginProtocol.AbilityNames.MouseMove);
            Add<MouseDrag>(builder, PluginProtocol.RoutePackageName.Mouse, PluginProtocol.AbilityNames.MouseDrag);
            Add<MouseMovementTrack>(builder, PluginProtocol.RoutePackageName.Mouse, PluginProtocol.AbilityNames.MouseMovementTrack);
            Add<MouseRelease>(builder, PluginProtocol.RoutePackageName.Mouse, PluginProtocol.AbilityNames.MouseRelease);
            Add<GetState>(builder, PluginProtocol.RoutePackageName.DesktopInput, PluginProtocol.AbilityNames.GetState);
            Add<SetState>(builder, PluginProtocol.RoutePackageName.DesktopInput, PluginProtocol.AbilityNames.SetState);
            // Preserve routes used by older upstream clients.
            Add<GetState>(builder, PluginProtocol.RoutePackageName.InputMethod, PluginProtocol.AbilityNames.GetState);
            Add<SetState>(builder, PluginProtocol.RoutePackageName.InputMethod, PluginProtocol.AbilityNames.SetState);
        }

        private static void Add<TAbility>(PluginPackageBuilder builder, string routePackageName, string abilityName)
            where TAbility : class, IPluginAbility
        {
            builder.AddAbility<TAbility>(
                routePackageName,
                abilityName,
                AbilityLifetime.Transient,
                AbilityConcurrencyMode.RejectWhenBusy);
        }
    }

    internal static class PluginProtocol
    {
        internal const string PackageName = "DesktopInput";

        internal static readonly IReadOnlyCollection<string> RoutePackageNames = new[]
        {
            RoutePackageName.Mouse,
            RoutePackageName.InputMethod,
            RoutePackageName.DesktopInput
        };

        internal static class RoutePackageName
        {
            internal const string Mouse = "Mouse";
            internal const string InputMethod = "InputMethod";
            internal const string DesktopInput = "DesktopInput";
        }

        internal static class AbilityNames
        {
            internal const string MouseDrag = "MouseDrag";
            internal const string MouseMove = "MouseMove";
            internal const string MouseMovementTrack = "MouseMovementTrack";
            internal const string MouseRelease = "MouseRelease";
            internal const string GetState = "GetState";
            internal const string SetState = "SetState";
        }
    }
}
