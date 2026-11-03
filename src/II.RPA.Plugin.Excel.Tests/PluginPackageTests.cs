using System.Reflection;
using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core.PluginBase;
using Xunit;

namespace II.RPA.Plugin.Excel.Tests;

public sealed class PluginPackageTests
{
    private static readonly string[] AbilityNames = { "GetExcelRule", "GetSystemFont", "Run", "SaveInstance" };

    [Fact]
    public void Fixed_entry_registers_existing_excel_abilities()
    {
        II.RPA.Plugin.Abstractions.IPluginPackage package = new global::II.RPA.Plugin.Excel.PluginPackage();
        Assert.Equal("Excel", package.PackageName);
        var builder = new PluginPackageBuilder();
        package.GetType().GetMethod("RegisterAbilities", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { builder });
        var registrations = (IReadOnlyDictionary<string, AbilityRegistration>)typeof(PluginPackageBuilder)
            .GetProperty("Registrations", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(builder)!;
        Assert.Equal(AbilityNames, registrations.Keys.OrderBy(name => name));
        Assert.All(registrations.Values, registration =>
        {
            Assert.Equal(AbilityLifetime.Transient, registration.Lifetime);
            Assert.Equal(AbilityConcurrencyMode.RejectWhenBusy, registration.ConcurrencyMode);
            Assert.True(IsSubclassOfRawGeneric(typeof(PluginAbilityBase<>), registration.AbilityType));
        });
    }

    [Fact]
    public void Every_excel_entry_parameter_uses_shared_parameter_contract()
    {
        Type[] parameterTypes = { typeof(global::II.RPA.Plugin.Excel.Params.ParamRunModel), typeof(global::II.RPA.Plugin.Excel.Params.ParamSaveInstance), typeof(global::II.RPA.Plugin.Excel.Params.ParamGetSystemFont), typeof(global::II.RPA.Plugin.Excel.Params.ParamRuleModel) };
        Assert.All(parameterTypes, type => Assert.True(typeof(PluginParameterBase).IsAssignableFrom(type), type.FullName));
    }

    [Fact]
    public async Task Missing_excel_method_returns_existing_json_error_contract()
    {
        II.RPA.Plugin.Abstractions.IPluginPackage package = new global::II.RPA.Plugin.Excel.PluginPackage();
        await package.InitializeAsync(new NoOpInvoker());
        var result = await package.RunAsync(new PluginExecutionContext(new PluginRoute("Excel", "Run"), "{}", "excel-invalid"));
        Assert.Contains("\"code\"", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"code\":0", result, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSubclassOfRawGeneric(Type generic, Type candidate)
    {
        for (var current = candidate; current != null && current != typeof(object); current = current.BaseType)
            if ((current.IsGenericType ? current.GetGenericTypeDefinition() : current) == generic) return true;
        return false;
    }

    private sealed class NoOpInvoker : IPluginInvoker
    {
        public Task<string> InvokeAsync(PluginRoute targetRoute, string jsonParam) => Task.FromResult("{\"code\":0}");
    }
}
