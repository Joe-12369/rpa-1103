using System.Reflection;
using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core.PluginBase;
using Xunit;

namespace II.RPA.Plugin.Database.Tests;

public sealed class PluginPackageTests
{
    private static readonly Type[] AbilityTypes =
    {
        typeof(global::II.RPA.Plugin.Database.Plugins.BatchExecuteSql),
        typeof(global::II.RPA.Plugin.Database.Plugins.BatchInsert),
        typeof(global::II.RPA.Plugin.Database.Plugins.BeginTransaction),
        typeof(global::II.RPA.Plugin.Database.Plugins.CloseConnection),
        typeof(global::II.RPA.Plugin.Database.Plugins.CommitTransaction),
        typeof(global::II.RPA.Plugin.Database.Plugins.CreateDbConnection),
        typeof(global::II.RPA.Plugin.Database.Plugins.ExecuteSql),
        typeof(global::II.RPA.Plugin.Database.Plugins.QueryAll),
        typeof(global::II.RPA.Plugin.Database.Plugins.QuerySingle),
        typeof(global::II.RPA.Plugin.Database.Plugins.RollbackTransaction),
        typeof(global::II.RPA.Plugin.Database.Plugins.TestDbConnection)
    };

    [Fact]
    public void Fixed_entry_registers_all_database_abilities()
    {
        II.RPA.Plugin.Abstractions.IPluginPackage package = new global::II.RPA.Plugin.Database.PluginPackage();
        Assert.Equal("Database", package.PackageName);
        var builder = new PluginPackageBuilder();
        var register = package.GetType().GetMethod("RegisterAbilities", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(register);
        register!.Invoke(null, new object[] { builder });
        var property = typeof(PluginPackageBuilder).GetProperty("Registrations", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var registrations = (IReadOnlyDictionary<string, AbilityRegistration>)property.GetValue(builder)!;
        Assert.Equal(AbilityTypes.OrderBy(type => type.Name), registrations.Values.Select(value => value.AbilityType).OrderBy(type => type.Name));
        Assert.All(registrations.Values, registration =>
        {
            Assert.Equal(AbilityLifetime.Transient, registration.Lifetime);
            Assert.Equal(AbilityConcurrencyMode.RejectWhenBusy, registration.ConcurrencyMode);
        });
    }

    [Fact]
    public void Abilities_use_async_typed_contract_without_legacy_hooks()
    {
        Assert.IsAssignableFrom<PluginParameterBase>(new global::II.RPA.Plugin.Database.Params.ParamCreateDbConnectionModel());
        Assert.All(AbilityTypes, type =>
        {
            Assert.True(IsSubclassOfRawGeneric(typeof(AsyncPluginAbilityBase<>), type));
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            Assert.DoesNotContain(methods, method => method.Name == "ParamVerification" || method.Name == "Excute" || method.Name == "Config");
        });
    }

    [Fact]
    public async Task Invalid_parameter_is_returned_as_the_existing_json_error_contract()
    {
        II.RPA.Plugin.Abstractions.IPluginPackage package =
            new global::II.RPA.Plugin.Database.PluginPackage();
        await package.InitializeAsync(new NullInvoker());

        var result = await package.RunAsync(new PluginExecutionContext(
            new PluginRoute("Database", "TestDbConnection"),
            "{}",
            "database-invalid-parameter"));

        Assert.Contains("\"Code\"", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"Code\":0", result, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSubclassOfRawGeneric(Type generic, Type candidate)
    {
        for (var current = candidate; current != null && current != typeof(object); current = current.BaseType)
        {
            if ((current.IsGenericType ? current.GetGenericTypeDefinition() : current) == generic) return true;
        }
        return false;
    }

    private sealed class NullInvoker : IPluginInvoker
    {
        public Task<string> InvokeAsync(
            PluginRoute targetRoute,
            string jsonParam) => Task.FromResult("{}");
    }
}
