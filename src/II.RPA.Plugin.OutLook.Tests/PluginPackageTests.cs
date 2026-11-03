using System.Reflection;
using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core.PluginBase;
using Xunit;

namespace II.RPA.Plugin.OutLook.Tests;

public sealed class PluginPackageTests
{
    private static readonly Type[] AbilityTypes =
    {
        typeof(global::II.RPA.Plugin.OutLook.Plugins.CheckAccount),
        typeof(global::II.RPA.Plugin.OutLook.Plugins.CheckMail),
        typeof(global::II.RPA.Plugin.OutLook.Plugins.OperateMail),
        typeof(global::II.RPA.Plugin.OutLook.Plugins.ReplyMail),
        typeof(global::II.RPA.Plugin.OutLook.Plugins.SendMail)
    };

    [Fact]
    public void Fixed_entry_registers_all_outlook_abilities()
    {
        II.RPA.Plugin.Abstractions.IPluginPackage package = new global::II.RPA.Plugin.OutLook.PluginPackage();
        Assert.Equal("OutLook", package.PackageName);
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
        Assert.IsAssignableFrom<PluginParameterBase>(new global::II.RPA.Plugin.OutLook.Params.ParamCheckAccountModel());
        Assert.IsAssignableFrom<PluginParameterBase>(new global::II.RPA.Plugin.OutLook.Params.ParamCheckMailModel());
        Assert.IsAssignableFrom<PluginParameterBase>(new global::II.RPA.Plugin.OutLook.Params.ParamOperateMailModel());
        Assert.IsAssignableFrom<PluginParameterBase>(new global::II.RPA.Plugin.OutLook.Params.ParamReplyMailModel());
        Assert.IsAssignableFrom<PluginParameterBase>(new global::II.RPA.Plugin.OutLook.Params.ParamSendMailModel());
        Assert.All(AbilityTypes, type =>
        {
            Assert.True(IsSubclassOfRawGeneric(typeof(PluginAbilityBase<>), type));
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            Assert.DoesNotContain(methods, method => method.Name == "ParamVerification" || method.Name == "Excute" || method.Name == "Config");
        });
    }

    [Fact]
    public async Task Invalid_send_mail_parameter_is_returned_as_existing_json_error_contract()
    {
        II.RPA.Plugin.Abstractions.IPluginPackage package = new global::II.RPA.Plugin.OutLook.PluginPackage();
        await package.InitializeAsync(new NullInvoker());

        var result = await package.RunAsync(new PluginExecutionContext(
            new PluginRoute("OutLook", "SendMail"),
            "{}",
            "outlook-invalid-parameter"));

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
        public Task<string> InvokeAsync(PluginRoute targetRoute, string jsonParam) =>
            Task.FromResult("{}");
    }
}
