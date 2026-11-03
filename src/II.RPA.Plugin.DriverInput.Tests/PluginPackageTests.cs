using System.Reflection;
using System.Text.Json;
using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.DriverInput.Params;
using II.RPA.Plugin.DriverInput.Plugins.PluginBase;
using II.RPA.WinAutomation.CommonUtility;
using Xunit;

namespace II.RPA.Plugin.DriverInput.Tests;

public sealed class PluginPackageTests
{
    private static readonly Type[] AbilityTypes =
    {
        typeof(global::II.RPA.Plugin.DriverInput.Plugins.DriverInput),
        typeof(global::II.RPA.Plugin.DriverInput.Plugins.DriverSearch)
    };

    [Fact]
    public void Fixed_entry_registers_both_driver_abilities()
    {
        II.RPA.Plugin.Abstractions.IPluginPackage package = new global::II.RPA.Plugin.DriverInput.PluginPackage();
        Assert.Equal("DriverInput", package.PackageName);
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
    public void Abilities_use_synchronous_typed_contract_without_legacy_hooks()
    {
        Assert.IsAssignableFrom<PluginParameterBase>(new ParamDriverInputModel());
        Assert.IsAssignableFrom<PluginParameterBase>(new ParamDriverSearchModel());
        Assert.All(AbilityTypes, type =>
        {
            Assert.True(IsSubclassOfRawGeneric(typeof(PluginAbilityBase<>), type));
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            Assert.DoesNotContain(methods, method => method.Name == "ParamVerification" || method.Name == "Excute" || method.Name == "Config");
        });
    }

    [Theory]
    [InlineData(1, "PS2")]
    [InlineData(2, "HID")]
    public async Task Driver_input_base_preserves_mode_component_label(int inputMode, string expectedLabel)
    {
        var ability = new LabelProbeAbility();
        var result = await ability.RunAsync(new PluginExecutionContext(
            new PluginRoute("DriverInput", "DriverInput"),
            $"{{\"InputMode\":{inputMode}}}",
            $"driver-label-{inputMode}"));

        using var document = JsonDocument.Parse(result);
        Assert.Equal(expectedLabel, document.RootElement.GetProperty("info").GetString());
    }

    [Fact]
    public async Task Invalid_driver_input_parameter_is_returned_as_existing_json_error_contract()
    {
        II.RPA.Plugin.Abstractions.IPluginPackage package = new global::II.RPA.Plugin.DriverInput.PluginPackage();
        await package.InitializeAsync(new NullInvoker());
        var result = await package.RunAsync(new PluginExecutionContext(
            new PluginRoute("DriverInput", "DriverInput"),
            "{\"InputMode\":0}",
            "driver-invalid-parameter"));
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

    private sealed class LabelProbeAbility : DriverInputComponentBase<ParamDriverInputModel>
    {
        protected override eComponentType ComponentType => eComponentType.DriverInput;
        protected override void ValidateParameter(ParamDriverInputModel parameter) { }
        protected override JMessage Execute(ParamDriverInputModel parameter, PluginExecutionContext context) =>
            new JMessage { Info = ComponentLabel };
    }

    private sealed class NullInvoker : IPluginInvoker
    {
        public Task<string> InvokeAsync(PluginRoute targetRoute, string jsonParam) => Task.FromResult("{}");
    }
}
