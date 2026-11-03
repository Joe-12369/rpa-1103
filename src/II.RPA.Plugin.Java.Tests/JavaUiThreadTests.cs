using System.Collections.Concurrent;
using System.Threading;
using II.RPA.Plugin.GUI.Commons;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace II.RPA.Plugin.Java.Tests;

public sealed class JavaUiThreadTests
{
    [Fact]
    public void Invoke_ReusesOneStaThread_AndSupportsNestedCalls()
    {
        var uiThread = new JavaUiThread();

        var first = uiThread.Invoke(() =>
        {
            Assert.True(uiThread.CheckAccess());
            var outerThreadId = Thread.CurrentThread.ManagedThreadId;
            var innerThreadId = uiThread.Invoke(() => Thread.CurrentThread.ManagedThreadId);
            return (outerThreadId, innerThreadId, Thread.CurrentThread.GetApartmentState());
        });
        var secondThreadId = uiThread.Invoke(() => Thread.CurrentThread.ManagedThreadId);

        Assert.Equal(first.outerThreadId, first.innerThreadId);
        Assert.Equal(first.outerThreadId, secondThreadId);
        Assert.Equal(ApartmentState.STA, first.Item3);
    }

    [Fact]
    public async Task InvokeAsync_SerializesCalls_AndKeepsAwaitContinuationOnStaThread()
    {
        var uiThread = new JavaUiThread();
        var entered = new ConcurrentQueue<int>();
        using var firstEntered = new ManualResetEventSlim(false);
        using var releaseFirst = new ManualResetEventSlim(false);

        var first = uiThread.InvokeAsync(() =>
        {
            entered.Enqueue(1);
            firstEntered.Set();
            releaseFirst.Wait();
        });
        Assert.True(firstEntered.Wait(TimeSpan.FromSeconds(5)));

        var second = uiThread.InvokeAsync(() => entered.Enqueue(2));
        await Task.Delay(100);
        Assert.Equal(new[] { 1 }, entered.ToArray());

        releaseFirst.Set();
        await Task.WhenAll(first, second);

        var threadIds = await uiThread.InvokeAsync(async () =>
        {
            var beforeAwait = Thread.CurrentThread.ManagedThreadId;
            await Task.Run(() => Thread.Sleep(20));
            var afterAwait = Thread.CurrentThread.ManagedThreadId;
            return (beforeAwait, afterAwait);
        });

        Assert.Equal(new[] { 1, 2 }, entered.ToArray());
        Assert.Equal(threadIds.beforeAwait, threadIds.afterAwait);
    }

    [Fact]
    public async Task InvokeAndInvokeAsync_PropagateOriginalExceptions()
    {
        var uiThread = new JavaUiThread();
        var synchronous = new InvalidOperationException("sync failure");
        var asynchronous = new InvalidOperationException("async failure");
        Func<int> synchronousOperation = () => throw synchronous;
        Func<int> asynchronousOperation = () => throw asynchronous;

        var syncActual = Assert.Throws<InvalidOperationException>(
            () => uiThread.Invoke(synchronousOperation));
        var asyncActual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => uiThread.InvokeAsync(asynchronousOperation));

        Assert.Same(synchronous, syncActual);
        Assert.Same(asynchronous, asyncActual);
    }

    [Fact]
    public void GuiModule_RegistersOneJavaUiThreadPerContainer()
    {
        var services = new ServiceCollection();
        new GuiModule().Configure(new GuiModuleBuilder(services));
        using var provider = services.BuildServiceProvider();

        var first = provider.GetRequiredService<IJavaUiThread>();
        var second = provider.GetRequiredService<IJavaUiThread>();

        Assert.Same(first, second);
    }
}
