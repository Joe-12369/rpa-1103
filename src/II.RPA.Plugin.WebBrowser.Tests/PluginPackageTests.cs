using System.Reflection;
using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Core;
using Xunit;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;

namespace II.RPA.Plugin.WebBrowser.Tests;

public sealed class PluginPackageTests
{
    [Fact]
    public void Package_registers_all_compiled_abilities_with_explicit_concurrency()
    {
        var package = new global::II.RPA.Plugin.WebBrowser.PluginPackage();
        var runtimePackage = Assert.IsAssignableFrom<II.RPA.Plugin.Abstractions.IPluginPackage>(package);
        Assert.Equal("WebBrowser", runtimePackage.PackageName);

        var builder = new PluginPackageBuilder();
        var method = package.GetType().GetMethod("RegisterAbilities", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        method!.Invoke(null, new object[] { builder });
        var registrationsProperty = typeof(PluginPackageBuilder)
            .GetProperty("Registrations", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var registrations = (IReadOnlyDictionary<string, AbilityRegistration>)registrationsProperty.GetValue(builder)!;

        Assert.Equal(41, registrations.Count);
        Assert.Equal(typeof(global::II.RPA.Plugin.WebBrowser.Plugins.Create), registrations["Create"].AbilityType);
        Assert.All(registrations.Values, registration =>
        {
            Assert.Equal(AbilityLifetime.Transient, registration.Lifetime);
            Assert.Equal(AbilityConcurrencyMode.RejectWhenBusy, registration.ConcurrencyMode);
        });
    }

    [Fact]
    public void Registered_abilities_use_typed_validation_and_execute_contracts_only()
    {
        var registrations = GetRegistrations();

        Assert.All(registrations.Values, registration =>
        {
            var methods = registration.AbilityType.GetMethods(
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            Assert.DoesNotContain(methods, method => method.Name == "ParamVerification");
            Assert.DoesNotContain(methods, method => method.Name == "Excute");
        });
        Assert.DoesNotContain(
            typeof(global::II.RPA.Plugin.WebBrowser.Plugins.BrowserComponentBase<>).GetMethods(
                BindingFlags.Instance | BindingFlags.NonPublic),
            method => method.Name == "ParamVerification" || method.Name == "Excute");
    }

    [Fact]
    public void Web_browser_manager_does_not_expose_legacy_action_instance_list()
    {
        Assert.Null(typeof(global::II.RPA.Plugin.WebBrowser.WebBrowserManager)
            .GetProperty("ActionBaseList", BindingFlags.Instance | BindingFlags.Public));
    }

    private static IReadOnlyDictionary<string, AbilityRegistration> GetRegistrations()
    {
        var builder = new PluginPackageBuilder();
        var method = typeof(global::II.RPA.Plugin.WebBrowser.PluginPackage)
            .GetMethod("RegisterAbilities", BindingFlags.NonPublic | BindingFlags.Static)!;
        method.Invoke(null, new object[] { builder });
        var registrationsProperty = typeof(PluginPackageBuilder)
            .GetProperty("Registrations", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (IReadOnlyDictionary<string, AbilityRegistration>)registrationsProperty.GetValue(builder)!;
    }

    [Theory]
    [InlineData("Download", "Element.Download")]
    [InlineData("ElementExecutejs", "Element.ElementExecutejs")]
    public async Task Forwarding_ability_invokes_Element_with_current_context(string abilityName, string expectedRoute)
    {
        var expected = new JMessage
        {
            Code = 2468,
            UnifyCode = "WEB-2468",
            Message = "forwarded",
            Info = "info",
            Extend = "{\"source\":\"element\"}"
        };
        var invoker = new RecordingInvoker(expected.ToJson());
        IPluginAbility ability = abilityName == "Download"
            ? new global::II.RPA.Plugin.WebBrowser.Plugins.Download(invoker)
            : new global::II.RPA.Plugin.WebBrowser.Plugins.ElementExecutejs(invoker);
        var context = new PluginExecutionContext(
            new PluginRoute("WebBrowser", abilityName),
            "{}",
            "invocation-forward");

        var result = JsonConvert.DeserializeObject<JMessage>(await ability.RunAsync(context))!;

        Assert.Equal(expectedRoute, invoker.Route!.ToString());
        Assert.Equal(context.JsonParam, invoker.JsonParameter);
        Assert.Equal(expected.Code, result.Code);
        Assert.Equal(expected.UnifyCode, result.UnifyCode);
        Assert.Equal(expected.Message, result.Message);
        Assert.Equal(expected.Info, result.Info);
        Assert.Equal(expected.Extend, result.Extend);
    }

    private sealed class RecordingInvoker : IPluginInvoker
    {
        private readonly string response;

        public RecordingInvoker(string response)
        {
            this.response = response;
        }

        public PluginRoute? Route { get; private set; }
        public string? JsonParameter { get; private set; }

        public Task<string> InvokeAsync(PluginRoute targetRoute, string jsonParam)
        {
            Route = targetRoute;
            JsonParameter = jsonParam;
            return Task.FromResult(response);
        }
    }
}
