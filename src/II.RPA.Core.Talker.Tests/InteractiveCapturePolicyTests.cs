using II.RPA.Core.Runtime;
using Xunit;

public class InteractiveCapturePolicyTests
{
    [Theory]
    [InlineData("phoneSelector")]
    [InlineData("phoneSelectorCv")]
    [InlineData("mouseRecord")]
    [InlineData("mouseRecordPhone")]
    [InlineData("uiselector")]
    [InlineData("cvselectUi")]
    [InlineData("captureSimilarElements")]
    [InlineData("makeOcrTableSelector")]
    public void InteractiveClientActionsHaveNoTimeout(string action)
        => Assert.True(InteractiveCapturePolicy.IsClientAction(action));

    [Theory]
    [InlineData("Element.Capture", "Excute")]
    [InlineData("Element.CaptureByRegion", "Excute")]
    [InlineData("Mobile.GetMobileElementInfo", "Excute")]
    [InlineData("Mouse.MouseMove", "Config")]
    public void InteractivePluginActionsHaveNoTimeout(string plugin, string action)
        => Assert.True(InteractiveCapturePolicy.IsPluginAction(plugin, action));

    [Theory]
    [InlineData("Element.IsElementExist", "Excute")]
    [InlineData("Mouse.MouseMove", "Excute")]
    [InlineData("Element.CancelCapture", "Excute")]
    public void ExecutionAndCancellationKeepTheirTimeouts(string plugin, string action)
        => Assert.False(InteractiveCapturePolicy.IsPluginAction(plugin, action));

    [Theory]
    [InlineData("verifySelector")]
    [InlineData("verifySelectorSimilar")]
    [InlineData("cancelComboSelectUi")]
    public void NoninteractiveClientActionsKeepTheirTimeouts(string action)
        => Assert.False(InteractiveCapturePolicy.IsClientAction(action));
}
