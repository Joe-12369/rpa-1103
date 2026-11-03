using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using II.RPA.Plugin.Core.Threading;
using Xunit;

namespace II.RPA.Plugin.GUI.Tests
{
    [CollectionDefinition(Name, DisableParallelization = true)]
    public sealed class OverlayDispatcherCollection
    {
        public const string Name = "Overlay dispatcher";
    }

    internal static class OverlayTestRuntime
    {
        private static readonly IStaDispatcher Dispatcher = new StaDispatcher();
        private static readonly Task Initialization = Dispatcher.InvokeAsync(
            GuiElement.InitializeOverlayDispatcher);

        internal static async Task InvokeAsync(Action action)
        {
            await Initialization;
            await Dispatcher.InvokeAsync(action);
        }
    }

    [Collection(OverlayDispatcherCollection.Name)]
    public class OverlayDispatcherOwnershipTests
    {
        [Fact]
        public async Task OverlayDispatcher_UsesExplicitUiThread_AfterEarlyBackgroundAccess()
        {
            var background = new Thread(() => GuiElement.IsWebSelectorType("chrome"));
            background.Start();
            Assert.True(background.Join(5000));
            await OverlayTestRuntime.InvokeAsync(() => { });
            var dispatched = GuiElement.OverlayDispatcher.InvokeAsync(() =>
            {
                Assert.True(GuiElement.OverlayDispatcher.CheckAccess());
                return Thread.CurrentThread.GetApartmentState();
            }).Task;
            Assert.Same(dispatched, await Task.WhenAny(dispatched, Task.Delay(5000)));
            Assert.Equal(ApartmentState.STA, await dispatched);
        }
    }
}
