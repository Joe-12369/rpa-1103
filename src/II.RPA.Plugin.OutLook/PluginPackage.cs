using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.OutLook.Plugins;

namespace II.RPA.Plugin.OutLook
{
    public sealed class PluginPackage : PluginPackageBase
    {
        public override string PackageName => PluginProtocol.PackageName;

        protected override void Configure(PluginPackageBuilder builder) => RegisterAbilities(builder);

        private static void RegisterAbilities(PluginPackageBuilder builder)
        {
            Add<CheckAccount>(builder, PluginProtocol.AbilityNames.CheckAccount);
            Add<CheckMail>(builder, PluginProtocol.AbilityNames.CheckMail);
            Add<OperateMail>(builder, PluginProtocol.AbilityNames.OperateMail);
            Add<ReplyMail>(builder, PluginProtocol.AbilityNames.ReplyMail);
            Add<SendMail>(builder, PluginProtocol.AbilityNames.SendMail);
        }

        private static void Add<TAbility>(PluginPackageBuilder builder, string abilityName)
            where TAbility : class, IPluginAbility
        {
            builder.AddAbility<TAbility>(abilityName, AbilityLifetime.Transient, AbilityConcurrencyMode.RejectWhenBusy);
        }
    }

    internal static class PluginProtocol
    {
        internal const string PackageName = "OutLook";

        internal static class AbilityNames
        {
            internal const string CheckAccount = "CheckAccount";
            internal const string CheckMail = "CheckMail";
            internal const string OperateMail = "OperateMail";
            internal const string ReplyMail = "ReplyMail";
            internal const string SendMail = "SendMail";
        }
    }
}
