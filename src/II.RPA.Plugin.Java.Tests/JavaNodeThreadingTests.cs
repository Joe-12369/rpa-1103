using II.RPA.Plugin.Java.Execute;
using WindowsAccessBridgeInterop;
using Xunit;

namespace II.RPA.Plugin.Java.Tests;

public sealed class JavaNodeThreadingTests
{
    [Fact]
    public void NodeMethod_EntersJavaUiThreadBeforeAccessingJabState()
    {
        var uiThread = new RecordingJavaUiThread();
        var provider = CreateProvider(uiThread);
        var node = new JavaNode(provider);
        uiThread.Reset();

        Assert.ThrowsAny<Exception>(() => node.GetText());

        Assert.Equal(1, uiThread.InvokeCount);
    }

    [Fact]
    public void NodeProperty_EntersJavaUiThreadBeforeAccessingJabState()
    {
        var uiThread = new RecordingJavaUiThread();
        var provider = CreateProvider(uiThread);
        var node = new JavaNode(provider);
        uiThread.Reset();

        Assert.ThrowsAny<Exception>(() => _ = node.BoundingRectangle);

        Assert.Equal(1, uiThread.InvokeCount);
    }

    private static AccessBridgeProvider CreateProvider(RecordingJavaUiThread uiThread)
    {
        var provider = new AccessBridgeProvider(new NoOpAccessBridgeOperations());
        provider.AttachUiThread(uiThread);
        return provider;
    }

    private sealed class RecordingJavaUiThread : IJavaUiThread
    {
        private bool insideInvocation;

        public int InvokeCount { get; private set; }

        public bool CheckAccess() => insideInvocation;

        public void Reset() => InvokeCount = 0;

        public void Invoke(Action operation)
        {
            Invoke<object>(() =>
            {
                operation();
                return null;
            });
        }

        public TResult Invoke<TResult>(Func<TResult> operation)
        {
            InvokeCount++;
            var previous = insideInvocation;
            insideInvocation = true;
            try
            {
                return operation();
            }
            finally
            {
                insideInvocation = previous;
            }
        }

        public Task InvokeAsync(Action operation)
        {
            Invoke(operation);
            return Task.CompletedTask;
        }

        public Task<TResult> InvokeAsync<TResult>(Func<TResult> operation) =>
            Task.FromResult(Invoke(operation));

        public Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> operation) =>
            Invoke(operation);
    }

    private sealed class NoOpAccessBridgeOperations : IAccessBridgeOperations
    {
        public AccessBridge Create() => new AccessBridge();

        public void Initialize(AccessBridge accessBridge)
        {
        }

        public List<AccessibleJvm> EnumJvms(AccessBridge accessBridge) =>
            new List<AccessibleJvm>();

        public bool IsJavaWindow(AccessBridge accessBridge, IntPtr windowHandle) => false;

        public void Dispose(AccessBridge accessBridge)
        {
        }
    }
}
