using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Abstractions;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json.Linq;
using Xunit;

namespace II.RPA.Plugin.GUI.Tests
{
    [Collection(OverlayDispatcherCollection.Name)]
    public class OverlayCaptureLifecycleTests
    {
        [Fact]
        public async Task Gate_RejectsNestedAndConcurrentCapture_AndCanBeReused()
        {
            Assert.True(OverlayCaptureGate.TryEnter());
            try
            {
                Assert.False(OverlayCaptureGate.TryEnter());
                Assert.False(await Task.Run(() => OverlayCaptureGate.TryEnter()));
            }
            finally
            {
                OverlayCaptureGate.Exit();
            }

            Assert.True(OverlayCaptureGate.TryEnter());
            OverlayCaptureGate.Exit();
        }

        [Fact]
        public async Task Prepare_RecreatesDisposedAndFailedModalOverlay_BeforeNextCapture()
        {
            Exception failure = null;
            await OverlayTestRuntime.InvokeAsync(() =>
            {
                try
                {
                    var initial = GuiElement.PrepareOverlayForm();
                    initial.Dispose();
                    var recovered = GuiElement.PrepareOverlayForm();
                    Assert.NotSame(initial, recovered);
                    Assert.False(recovered.IsDisposed);
                    Assert.True(recovered.IsHandleCreated);

                    recovered.DisposeEnvironment();
                    recovered.Dispose();
                    var failed = new FailingOverlayForm();
                    GuiElement.overlayForm = failed;
                    Assert.Throws<InvalidOperationException>(() => failed.ShowCaptureDialog());
                    Assert.True(failed.CaptureDialogFailed);
                    Assert.False(failed.IsDisposed);

                    var next = GuiElement.PrepareOverlayForm();
                    Assert.NotSame(failed, next);
                    Assert.True(failed.IsDisposed);
                    Assert.False(next.CaptureDialogFailed);
                    Assert.True(next.IsHandleCreated);

                    var nested = new TestCaptureAbility<TestParams>();
                    var outer = new TestCaptureAbility<TestParams>();
                    int selectedCalls = 0;
                    outer.ExecuteAction = () =>
                    {
                        GuiElement.OnCanceled = () => selectedCalls++;
                        var rejected = JObject.Parse(nested.RunAsync(CreateContext()).GetAwaiter().GetResult());
                        Assert.NotEqual(0, rejected.Value<int>("code"));
                        Assert.Equal(0, nested.ValidationCalls);
                        Assert.Equal(0, nested.ResultCalls);
                        Assert.Same(next, GuiElement.overlayForm);
                        Assert.False(next.CancellationForCapture.IsCancellationRequested);
                        GuiElement.OnCanceled();
                    };
                    Assert.Equal(0, JObject.Parse(outer.RunAsync(CreateContext()).GetAwaiter().GetResult()).Value<int>("code"));
                    Assert.Equal(1, selectedCalls);
                    Assert.Null(GuiElement.OnCanceled);
                    Assert.False(OverlayCaptureGate.IsOccupied);

                    // Exceptions must also release the session for the next caller.
                    outer.ExecuteAction = () => throw new InvalidOperationException("Capture failed.");
                    Assert.NotEqual(0, JObject.Parse(outer.RunAsync(CreateContext()).GetAwaiter().GetResult()).Value<int>("code"));
                    Assert.False(OverlayCaptureGate.IsOccupied);
                    outer.ExecuteAction = null;
                    Assert.Equal(0, JObject.Parse(outer.RunAsync(CreateContext()).GetAwaiter().GetResult()).Value<int>("code"));
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
                finally
                {
                    GuiElement.overlayForm?.CleanupCapture();
                    GuiElement.overlayForm?.DisposeEnvironment();
                    GuiElement.overlayForm?.Dispose();
                }
            });
            if (failure != null)
                ExceptionDispatchInfo.Capture(failure).Throw();
        }

        private static PluginExecutionContext CreateContext()
        {
            return new PluginExecutionContext(
                new PluginRoute("Element", "TestCapture"),
                "{}",
                Guid.NewGuid().ToString("N"));
        }

        private sealed class FailingOverlayForm : OverlayForm
        {
            protected override void CreateHandle()
            {
                throw new InvalidOperationException("Simulated modal handle creation failure.");
            }
        }

        public sealed class TestParams : PluginParameterBase { }

        private sealed class TestCaptureAbility<T> : OverlayCaptureAbilityBase<T>
            where T : PluginParameterBase, new()
        {
            public Action ExecuteAction;
            public int ValidationCalls;
            public int ResultCalls;
            protected override eComponentType ComponentType => eComponentType.Capture;

            protected override void ValidateParameter(T parameter)
            {
                ValidationCalls++;
                GuiElement.overlayForm.InitForStartCapture();
            }

            protected override JMessage Execute(T parameter, PluginExecutionContext executionContext)
            {
                ExecuteAction?.Invoke();
                return new JMessage();
            }

            protected override string HandlingResultMessage(T parameter, JMessage resultMsg)
            {
                ResultCalls++;
                return base.HandlingErrorMessage(resultMsg);
            }
        }
    }
}
