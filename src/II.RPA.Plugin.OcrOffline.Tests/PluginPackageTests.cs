using System.Reflection;
using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core.PluginBase;
using Xunit;

namespace II.RPA.Plugin.OcrOffline.Tests;

public sealed class PluginPackageTests
{
    private static readonly Type[] AbilityTypes =
    {
        typeof(global::II.RPA.Plugin.OcrOffline.ExecuteCV),
        typeof(global::II.RPA.Plugin.OcrOffline.Plugins.GetOcrTableDataByImg),
        typeof(global::II.RPA.Plugin.OcrOffline.Plugins.GetOcrTableDataBySelectorAILimit),
        typeof(global::II.RPA.Plugin.OcrOffline.Plugins.GetOcrTableDataBySelector),
        typeof(global::II.RPA.Plugin.OcrOffline.GetTextByCV_V2),
        typeof(global::II.RPA.Plugin.OcrOffline.GetTextByCV),
        typeof(global::II.RPA.Plugin.OcrOffline.Plugins.MakeOcrTableSelector)
    };

    [Fact]
    public void Fixed_entry_registers_all_ocr_offline_abilities()
    {
        II.RPA.Plugin.Abstractions.IPluginPackage package = new global::II.RPA.Plugin.OcrOffline.PluginPackage();
        Assert.Equal("OcrOffline", package.PackageName);
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
    public void Abilities_and_parameters_use_typed_async_contract()
    {
        Assert.IsAssignableFrom<PluginParameterBase>(new global::II.RPA.Plugin.OcrOffline.Params.ParamExecuteCVModel());
        Assert.IsAssignableFrom<PluginParameterBase>(new global::II.RPA.Plugin.OcrOffline.Params.ParamGetOcrTableDataByImg());
        Assert.IsAssignableFrom<PluginParameterBase>(new global::II.RPA.Plugin.OcrOffline.Params.ParamGetOcrTableDataBySelector());
        Assert.IsAssignableFrom<PluginParameterBase>(new global::II.RPA.Plugin.OcrOffline.Params.ParamGetTextByCVModel());
        Assert.IsAssignableFrom<PluginParameterBase>(new global::II.RPA.Plugin.OcrOffline.Params.ParamGetTextByCV_V2Model());
        Assert.IsAssignableFrom<PluginParameterBase>(new global::II.RPA.Plugin.OcrOffline.Params.ParamMakeOcrTableSelector());
        Assert.All(AbilityTypes, type => Assert.True(IsSubclassOfRawGeneric(typeof(AsyncPluginAbilityBase<>), type)));
    }

    [Fact]
    public async Task Invalid_execute_cv_parameter_is_returned_as_existing_json_error_contract()
    {
        II.RPA.Plugin.Abstractions.IPluginPackage package = new global::II.RPA.Plugin.OcrOffline.PluginPackage();
        await package.InitializeAsync(new RecordingInvoker());
        var result = await package.RunAsync(new PluginExecutionContext(new PluginRoute("OcrOffline", "ExecuteCV"), "{}", "ocr-invalid-parameter"));
        Assert.Contains("\"code\"", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"code\":0", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Make_table_selector_routes_capture_through_invoker_with_same_invocation()
    {
        var invoker = new RecordingInvoker
        {
            Result = "{\"code\":1,\"message\":\"capture stopped\",\"info\":\"\"}"
        };
        II.RPA.Plugin.Abstractions.IPluginPackage package = new global::II.RPA.Plugin.OcrOffline.PluginPackage();
        await package.InitializeAsync(invoker);
        var context = new PluginExecutionContext(
            new PluginRoute("OcrOffline", "MakeOcrTableSelector"),
            "{\"imgPath\":\"capture.png\"}",
            "ocr-table-capture");

        await package.RunAsync(context);

        Assert.Equal("Element.CaptureCVForOcrTable", invoker.TargetRoute?.ToString());
        Assert.Contains("\"filePath\"", invoker.JsonParam, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSubclassOfRawGeneric(Type generic, Type candidate)
    {
        for (var current = candidate; current != null && current != typeof(object); current = current.BaseType)
            if ((current.IsGenericType ? current.GetGenericTypeDefinition() : current) == generic) return true;
        return false;
    }

    private sealed class RecordingInvoker : IPluginInvoker
    {
        public string Result { get; set; } = "{\"code\":0,\"unifyCode\":\"0\",\"message\":\"操作成功\",\"info\":\"\"}";
        public PluginRoute? TargetRoute { get; private set; }
        public string? JsonParam { get; private set; }

        public Task<string> InvokeAsync(PluginRoute targetRoute, string jsonParam)
        {
            TargetRoute = targetRoute;
            JsonParam = jsonParam;
            return Task.FromResult(Result);
        }
    }
}
