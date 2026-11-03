using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.RPASystem.Plugins;

namespace II.RPA.Plugin.RPASystem
{
    public sealed class PluginPackage : PluginPackageBase
    {
        public override string PackageName => PluginProtocol.PackageName;
        protected override void Configure(PluginPackageBuilder builder) => RegisterAbilities(builder);
        private static void RegisterAbilities(PluginPackageBuilder builder)
        {
            Add<AddCredential>(builder, PluginProtocol.AbilityNames.AddCredential); Add<CheckScreenState>(builder, PluginProtocol.AbilityNames.CheckScreenState);
            Add<CpCheckPlug>(builder, PluginProtocol.AbilityNames.CpCheckPlug); Add<DeleteCredential>(builder, PluginProtocol.AbilityNames.DeleteCredential);
            Add<GetApplicationPath>(builder, PluginProtocol.AbilityNames.GetApplicationPath); Add<HIDDriveCheck>(builder, PluginProtocol.AbilityNames.HIDDriveCheck);
            Add<HIDDrivePreCheck>(builder, PluginProtocol.AbilityNames.HIDDrivePreCheck); Add<LocalRemoteConnection>(builder, PluginProtocol.AbilityNames.LocalRemoteConnection);
            Add<LocalRemoteOff>(builder, PluginProtocol.AbilityNames.LocalRemoteOff); Add<RDPLock>(builder, PluginProtocol.AbilityNames.RDPLock);
            Add<RDPUnlock>(builder, PluginProtocol.AbilityNames.RDPUnlock); Add<ReadCredential>(builder, PluginProtocol.AbilityNames.ReadCredential);
            Add<RemoteCancel>(builder, PluginProtocol.AbilityNames.RemoteCancel); Add<RemoteUnLock>(builder, PluginProtocol.AbilityNames.RemoteUnLock);
        }
        private static void Add<TAbility>(PluginPackageBuilder builder, string name) where TAbility : class, IPluginAbility =>
            builder.AddAbility<TAbility>(name, AbilityLifetime.Transient, AbilityConcurrencyMode.RejectWhenBusy);
    }

    internal static class PluginProtocol
    {
        internal const string PackageName = "RPASystem";

        internal static class AbilityNames
        {
            internal const string AddCredential = "AddCredential";
            internal const string CheckScreenState = "CheckScreenState";
            internal const string CpCheckPlug = "CpCheckPlug";
            internal const string DeleteCredential = "DeleteCredential";
            internal const string GetApplicationPath = "GetApplicationPath";
            internal const string HIDDriveCheck = "HIDDriveCheck";
            internal const string HIDDrivePreCheck = "HIDDrivePreCheck";
            internal const string LocalRemoteConnection = "LocalRemoteConnection";
            internal const string LocalRemoteOff = "LocalRemoteOff";
            internal const string RDPLock = "RDPLock";
            internal const string RDPUnlock = "RDPUnlock";
            internal const string ReadCredential = "ReadCredential";
            internal const string RemoteCancel = "RemoteCancel";
            internal const string RemoteUnLock = "RemoteUnLock";
        }
    }
}
