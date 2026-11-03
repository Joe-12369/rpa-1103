using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Database.Plugins;

namespace II.RPA.Plugin.Database
{
    public sealed class PluginPackage : PluginPackageBase
    {
        public override string PackageName => PluginProtocol.PackageName;

        protected override void Configure(PluginPackageBuilder builder) => RegisterAbilities(builder);

        private static void RegisterAbilities(PluginPackageBuilder builder)
        {
            Add<BatchExecuteSql>(builder, PluginProtocol.AbilityNames.BatchExecuteSql);
            Add<BatchInsert>(builder, PluginProtocol.AbilityNames.BatchInsert);
            Add<BeginTransaction>(builder, PluginProtocol.AbilityNames.BeginTransaction);
            Add<CloseConnection>(builder, PluginProtocol.AbilityNames.CloseConnection);
            Add<CommitTransaction>(builder, PluginProtocol.AbilityNames.CommitTransaction);
            Add<CreateDbConnection>(builder, PluginProtocol.AbilityNames.CreateDbConnection);
            Add<ExecuteSql>(builder, PluginProtocol.AbilityNames.ExecuteSql);
            Add<QueryAll>(builder, PluginProtocol.AbilityNames.QueryAll);
            Add<QuerySingle>(builder, PluginProtocol.AbilityNames.QuerySingle);
            Add<RollbackTransaction>(builder, PluginProtocol.AbilityNames.RollbackTransaction);
            Add<TestDbConnection>(builder, PluginProtocol.AbilityNames.TestDbConnection);
        }

        private static void Add<TAbility>(PluginPackageBuilder builder, string abilityName)
            where TAbility : class, IPluginAbility
        {
            builder.AddAbility<TAbility>(abilityName, AbilityLifetime.Transient, AbilityConcurrencyMode.RejectWhenBusy);
        }
    }

    internal static class PluginProtocol
    {
        internal const string PackageName = "Database";

        internal static class AbilityNames
        {
            internal const string BatchExecuteSql = "BatchExecuteSql";
            internal const string BatchInsert = "BatchInsert";
            internal const string BeginTransaction = "BeginTransaction";
            internal const string CloseConnection = "CloseConnection";
            internal const string CommitTransaction = "CommitTransaction";
            internal const string CreateDbConnection = "CreateDbConnection";
            internal const string ExecuteSql = "ExecuteSql";
            internal const string QueryAll = "QueryAll";
            internal const string QuerySingle = "QuerySingle";
            internal const string RollbackTransaction = "RollbackTransaction";
            internal const string TestDbConnection = "TestDbConnection";
        }
    }
}
