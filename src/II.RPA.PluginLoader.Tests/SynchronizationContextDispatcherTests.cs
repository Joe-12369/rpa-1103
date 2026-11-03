using II.RPA.RuntimeHost.Services;
using Xunit;

namespace II.RPA.PluginLoader.Tests;

public sealed class SynchronizationContextDispatcherTests
{
    [Fact]
    public async Task InvokeAsync_posts_to_context_and_completes_after_async_operation()
    {
        var context = new ImmediateSynchronizationContext();
        var operation = new TaskCompletionSource<string>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        var invocation = SynchronizationContextDispatcher.InvokeAsync(
            context,
            () => operation.Task);

        Assert.Equal(1, context.PostCount);
        Assert.False(invocation.IsCompleted);

        operation.SetResult("completed");

        Assert.Equal("completed", await invocation);
    }

    private sealed class ImmediateSynchronizationContext : SynchronizationContext
    {
        public int PostCount { get; private set; }

        public override void Post(SendOrPostCallback callback, object? state)
        {
            PostCount++;
            callback(state);
        }
    }
}
