using II.RPA.Plugin.GUI.Commons;
using II.RPA.WinAutomation.CommonUtility;
using Xunit;

namespace II.RPA.Plugin.GUI.Commons.Tests;

[Collection("Shared UIA")]
public class UiaTimeoutTests
{
    [Fact]
    public void Shared_uia3_uses_bounded_provider_timeouts()
    {
        var previousMode = UiCommon.UIAMode;
        try
        {
            UiCommon.UIAMode = UIAModeType.UIA3;
            var automation = UiCommon.UIAAutomation;

            Assert.Equal(TimeSpan.FromSeconds(2), automation.ConnectionTimeout);
            Assert.Equal(TimeSpan.FromSeconds(5), automation.TransactionTimeout);
        }
        finally
        {
            UiCommon.UIAMode = previousMode;
        }
    }
}

[CollectionDefinition("Shared UIA", DisableParallelization = true)]
public class SharedUiaCollection
{
}
