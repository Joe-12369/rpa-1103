using System.Reflection;
using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core.PluginBase;
using Xunit;

namespace II.RPA.Plugin.GoogleSheets.Tests;

public sealed class PluginPackageTests
{
    private static readonly Type[] AbilityTypes =
    {
        typeof(global::II.RPA.Plugin.GoogleSheets.Plugins.AddFilterView),
        typeof(global::II.RPA.Plugin.GoogleSheets.Plugins.AddNewSheet),
        typeof(global::II.RPA.Plugin.GoogleSheets.Plugins.CopyPasteSheet),
        typeof(global::II.RPA.Plugin.GoogleSheets.Plugins.DownloadSheets),
        typeof(global::II.RPA.Plugin.GoogleSheets.Plugins.GetColumnsByTableTitle),
        typeof(global::II.RPA.Plugin.GoogleSheets.Plugins.GetSheetRowCount),
        typeof(global::II.RPA.Plugin.GoogleSheets.Plugins.GetAllData),
        typeof(global::II.RPA.Plugin.GoogleSheets.Plugins.GetAllSheetsName),
        typeof(global::II.RPA.Plugin.GoogleSheets.Plugins.RenameSheet),
        typeof(global::II.RPA.Plugin.GoogleSheets.Plugins.WriteToSheetCell),
        typeof(global::II.RPA.Plugin.GoogleSheets.Plugins.WriteToSheetCellFormula),
        typeof(global::II.RPA.Plugin.GoogleSheets.Plugins.WriteToSheetRegion)
    };

    private static readonly Type[] ParameterTypes =
    {
        typeof(global::II.RPA.Plugin.GoogleSheets.Params.ParamAddFilterViewModel),
        typeof(global::II.RPA.Plugin.GoogleSheets.Params.ParamAddNewSheetModel),
        typeof(global::II.RPA.Plugin.GoogleSheets.Params.ParamCopyPasteSheetModel),
        typeof(global::II.RPA.Plugin.GoogleSheets.Params.ParamDownloadSheetsModel),
        typeof(global::II.RPA.Plugin.GoogleSheets.Params.ParamGetAllDataModel),
        typeof(global::II.RPA.Plugin.GoogleSheets.Params.ParamGetAllSheetsNameModel),
        typeof(global::II.RPA.Plugin.GoogleSheets.Params.ParamGetColumnsByTableTitleModel),
        typeof(global::II.RPA.Plugin.GoogleSheets.Params.ParamRenameSheetModel),
        typeof(global::II.RPA.Plugin.GoogleSheets.Params.ParamWriteToSheetCellModel),
        typeof(global::II.RPA.Plugin.GoogleSheets.Params.ParamWriteToSheetRegionModel)
    };

    [Fact]
    public void Fixed_entry_registers_all_google_table_abilities()
    {
        II.RPA.Plugin.Abstractions.IPluginPackage package = new global::II.RPA.Plugin.GoogleSheets.PluginPackage();
        Assert.Equal("GoogleTable", package.PackageName);
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
    public void Abilities_and_parameters_use_typed_contract_without_legacy_hooks()
    {
        Assert.All(ParameterTypes, type => Assert.True(typeof(PluginParameterBase).IsAssignableFrom(type)));
        Assert.All(AbilityTypes, type =>
        {
            Assert.True(IsSubclassOfRawGeneric(typeof(PluginAbilityBase<>), type));
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            Assert.DoesNotContain(methods, method => method.Name == "ParamVerification" || method.Name == "Excute" || method.Name == "Config");
        });
    }

    [Fact]
    public async Task Invalid_google_sheet_url_is_returned_as_existing_json_error_contract()
    {
        II.RPA.Plugin.Abstractions.IPluginPackage package = new global::II.RPA.Plugin.GoogleSheets.PluginPackage();
        await package.InitializeAsync(new NullInvoker());
        var result = await package.RunAsync(new PluginExecutionContext(
            new PluginRoute("GoogleTable", "AddNewSheet"),
            "{\"url\":\"invalid\"}",
            "google-invalid-url"));
        Assert.Contains("\"code\"", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"code\":0", result, StringComparison.OrdinalIgnoreCase);
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
        public Task<string> InvokeAsync(PluginRoute targetRoute, string jsonParam) => Task.FromResult("{}");
    }
}
