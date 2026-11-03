using II.RPA.Plugin.Core.Threading;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Xunit;

namespace II.RPA.Plugin.Core.Tests;

public sealed class StaDispatcherTests
{
    [Fact]
    public async Task InvokeAsync_ReusesOneStaThreadAndDifferentInstancesAreIsolated()
    {
        var first = new StaDispatcher();
        var second = new StaDispatcher();

        var firstCall = await first.InvokeAsync(() =>
            (
                ThreadId: Thread.CurrentThread.ManagedThreadId,
                ApartmentState: Thread.CurrentThread.GetApartmentState()));
        var secondCallOnFirst = await first.InvokeAsync(() => Thread.CurrentThread.ManagedThreadId);
        var callOnSecond = await second.InvokeAsync(() => Thread.CurrentThread.ManagedThreadId);

        Assert.Equal(ApartmentState.STA, firstCall.ApartmentState);
        Assert.Equal(firstCall.ThreadId, secondCallOnFirst);
        Assert.NotEqual(firstCall.ThreadId, callOnSecond);
    }

    [Fact]
    public async Task InvokeAsync_ReturnsBeforeAQueuedSynchronousOperationCompletes()
    {
        var dispatcher = new StaDispatcher();
        using var release = new ManualResetEventSlim(false);
        var stopwatch = Stopwatch.StartNew();

        var invocation = dispatcher.InvokeAsync(() => release.Wait());
        stopwatch.Stop();

        Assert.False(invocation.IsCompleted);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(1));
        release.Set();
        await invocation;
    }

    [Fact]
    public async Task InvokeAsync_SerializesSynchronousOperations()
    {
        var dispatcher = new StaDispatcher();
        var entered = new ConcurrentQueue<int>();
        using var firstEntered = new ManualResetEventSlim(false);
        using var releaseFirst = new ManualResetEventSlim(false);

        var first = dispatcher.InvokeAsync(() =>
        {
            entered.Enqueue(1);
            firstEntered.Set();
            releaseFirst.Wait();
        });
        Assert.True(firstEntered.Wait(TimeSpan.FromSeconds(5)));

        var second = dispatcher.InvokeAsync(() => entered.Enqueue(2));
        await Task.Delay(100);
        Assert.Equal(new[] { 1 }, entered.ToArray());

        releaseFirst.Set();
        await Task.WhenAll(first, second);
        Assert.Equal(new[] { 1, 2 }, entered.ToArray());
    }

    [Fact]
    public async Task InvokeAsync_AwaitableOperationResumesOnTheStaThread()
    {
        var dispatcher = new StaDispatcher();

        var threadIds = await dispatcher.InvokeAsync(async () =>
        {
            var beforeAwait = Thread.CurrentThread.ManagedThreadId;
            await Task.Run(() => Thread.Sleep(20));
            var afterAwait = Thread.CurrentThread.ManagedThreadId;
            return (
                BeforeAwait: beforeAwait,
                AfterAwait: afterAwait,
                ApartmentState: Thread.CurrentThread.GetApartmentState());
        });

        Assert.Equal(threadIds.BeforeAwait, threadIds.AfterAwait);
        Assert.Equal(ApartmentState.STA, threadIds.ApartmentState);
    }

    [Fact]
    public async Task InvokeAsync_PropagatesTheOriginalException()
    {
        var dispatcher = new StaDispatcher();
        var expected = new InvalidOperationException("failure");
        Func<int> operation = () => throw expected;

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => dispatcher.InvokeAsync(operation));

        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task InvokeAsync_CanBeNestedOnTheStaThreadWithoutDeadlock()
    {
        var dispatcher = new StaDispatcher();

        var result = await dispatcher.InvokeAsync(async () =>
        {
            var outerThreadId = Thread.CurrentThread.ManagedThreadId;
            var innerThreadId = await dispatcher.InvokeAsync(() => Thread.CurrentThread.ManagedThreadId);
            return (outerThreadId, innerThreadId);
        });

        Assert.Equal(result.outerThreadId, result.innerThreadId);
    }
}
