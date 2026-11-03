using System.Reflection;
using II.RPA.Core.Runtime;
using II.RPA.Core.Talker.Talkers;
using Xunit;

namespace II.RPA.Core.Talker.Tests;

public sealed class ChildCoreServerRegistrationTests
{
    [Fact]
    public async Task TransportReadyDoesNotCompleteStartBeforeInitialization()
    {
        var manager = DispatchProxy.Create<ITalkerManager, UnusedProxy>();
        var runtime = DispatchProxy.Create<IRpaRuntimeContext, UnusedProxy>();
        var child = new ChildCoreServerTalker(manager, NullLogger.Instance, runtime);
        var initialized = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var reader = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        typeof(BaseTalker).GetProperty(nameof(BaseTalker.IsReady))!.SetValue(child, true);
        typeof(ChildCoreServerTalker).GetField("_readyCompletion", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(child, initialized);
        typeof(ChildCoreServerTalker).GetField("_readTask", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(child, reader.Task);

        Assert.False(child.RegistrationCompleted);
        var start = child.Start();
        Assert.False(start.IsCompleted);
        initialized.SetResult(true);
        await start.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(child.RegistrationCompleted);
        await child.Start().WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task InitializationFailureDoesNotAcknowledgeStartup()
    {
        var child = new ChildCoreServerTalker(
            DispatchProxy.Create<ITalkerManager, UnusedProxy>(), NullLogger.Instance,
            DispatchProxy.Create<IRpaRuntimeContext, UnusedProxy>());
        var initialized = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        typeof(BaseTalker).GetProperty(nameof(BaseTalker.IsReady))!.SetValue(child, true);
        typeof(ChildCoreServerTalker).GetField("_readyCompletion", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(child, initialized);
        typeof(ChildCoreServerTalker).GetField("_readTask", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(child, new TaskCompletionSource().Task);
        var start = child.Start();
        initialized.SetException(new IOException("Disconnected during initialization"));
        await Assert.ThrowsAsync<IOException>(() => start);
        Assert.False(child.RegistrationCompleted);
    }

    public class UnusedProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
            => throw new NotSupportedException(targetMethod?.Name);
    }
}
