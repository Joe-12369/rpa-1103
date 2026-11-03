using II.RPA.Plugin.Abstractions;
using Xunit;

namespace II.RPA.Plugin.Abstractions.Tests;

public sealed class ContractShapeTests
{
    [Fact]
    public void PluginAbility_ExposesOnlyRunAsync()
    {
        var method = Assert.Single(typeof(IPluginAbility).GetMethods());
        Assert.Equal("RunAsync", method.Name);
        Assert.Equal(typeof(Task<string>), method.ReturnType);
        Assert.Equal(new[] { typeof(PluginExecutionContext) }, method.GetParameters().Select(parameter => parameter.ParameterType));
    }

    [Fact]
    public void PluginPackage_ExposesPackageNameInitializationAndRun()
    {
        var property = Assert.Single(typeof(IPluginPackage).GetProperties());
        Assert.Equal("PackageName", property.Name);
        Assert.Equal(typeof(string), property.PropertyType);
        Assert.False(property.CanWrite);

        var methods = typeof(IPluginPackage)
            .GetMethods()
            .Where(method => !method.IsSpecialName)
            .OrderBy(method => method.Name)
            .ToArray();
        Assert.Equal(2, methods.Length);
        Assert.Equal("InitializeAsync", methods[0].Name);
        Assert.Equal(typeof(Task), methods[0].ReturnType);
        Assert.Equal(new[] { typeof(IPluginInvoker) }, methods[0].GetParameters().Select(parameter => parameter.ParameterType));
        Assert.Equal("RunAsync", methods[1].Name);
        Assert.Equal(typeof(Task<string>), methods[1].ReturnType);
        Assert.Equal(new[] { typeof(PluginExecutionContext) }, methods[1].GetParameters().Select(parameter => parameter.ParameterType));
    }

    [Fact]
    public void PluginInvoker_ExposesOnlyInvokeAsync()
    {
        var method = Assert.Single(typeof(IPluginInvoker).GetMethods());
        Assert.Equal("InvokeAsync", method.Name);
        Assert.Equal(typeof(Task<string>), method.ReturnType);
        Assert.Equal(
            new[] { typeof(PluginRoute), typeof(string) },
            method.GetParameters().Select(parameter => parameter.ParameterType));
    }
}
