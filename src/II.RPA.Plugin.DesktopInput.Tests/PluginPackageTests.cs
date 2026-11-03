using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.DesktopInput.InputMethod.Params;
using II.RPA.Plugin.DesktopInput.Mouse.Params;
using System.Text.Json;
using Xunit;

namespace II.RPA.Plugin.DesktopInput.Tests;

public sealed class PluginPackageTests
{
    [Fact]
    public async Task Package_exposes_desktop_input_and_preserves_legacy_routes()
    {
        IPluginPackage package = new PluginPackage();
        Assert.Equal("DesktopInput", package.PackageName);
        Assert.Equal(new[] { "Mouse", "InputMethod", "DesktopInput" }, package.RoutePackageNames);
        await package.InitializeAsync(new NullInvoker());
        Assert.Contains("\"Code\"", await package.RunAsync(Context("DesktopInput", "GetState", "{}")), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"Code\"", await package.RunAsync(Context("InputMethod", "GetState", "{}")), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Mouse_move_person_mode_keeps_the_existing_cross_ability_route()
    {
        var invoker = new RecordingInvoker();
        IPluginPackage package = new PluginPackage();
        await package.InitializeAsync(invoker);
        var result = await package.RunAsync(Context("Mouse", "MouseMove", "{\"x\":7,\"y\":9,\"speedValue\":40,\"isUsePerson\":1}"));
        Assert.Equal("Mouse.MouseMovementTrack", invoker.TargetRoute?.ToString());
        using var parameter = JsonDocument.Parse(invoker.JsonParam!);
        Assert.Equal(7, parameter.RootElement.GetProperty("x").GetInt32());
        Assert.Equal(9, parameter.RootElement.GetProperty("y").GetInt32());
        Assert.Contains("\"code\":0", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parameters_keep_the_typed_contract()
    {
        Assert.IsAssignableFrom<PluginParameterBase>(new ParamMouseMoveModel());
        Assert.IsAssignableFrom<PluginParameterBase>(new ParamMouseDragModel());
        Assert.IsAssignableFrom<PluginParameterBase>(new ParamMouseMovementTrackModel());
        Assert.IsAssignableFrom<PluginParameterBase>(new ParamMouseReleaseModel());
        Assert.IsAssignableFrom<PluginParameterBase>(new ParamGetState());
        Assert.IsAssignableFrom<PluginParameterBase>(new ParamSetState());
    }

    private static PluginExecutionContext Context(string packageName, string abilityName, string json) => new(new PluginRoute(packageName, abilityName), json, Guid.NewGuid().ToString("N"));

    private sealed class NullInvoker : IPluginInvoker
    {
        public Task<string> InvokeAsync(PluginRoute targetRoute, string jsonParam) => Task.FromResult("{}");
    }

    private sealed class RecordingInvoker : IPluginInvoker
    {
        public PluginRoute? TargetRoute { get; private set; }
        public string? JsonParam { get; private set; }
        public Task<string> InvokeAsync(PluginRoute targetRoute, string jsonParam)
        {
            TargetRoute = targetRoute;
            JsonParam = jsonParam;
            return Task.FromResult("{\"code\":0,\"unifyCode\":\"0\",\"message\":\"操作成功\",\"info\":\"\"}");
        }
    }
}
