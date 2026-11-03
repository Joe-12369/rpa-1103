using System.Threading;
using II.RPA.Plugin.Java.Execute;
using WindowsAccessBridgeInterop;
using Xunit;

namespace II.RPA.Plugin.Java.Tests;

public sealed class AccessBridgeProviderTests
{
    [Fact]
    public void AccessBridge_IsCreatedOnceOnAttachedJavaUiThread()
    {
        var operations = new RecordingAccessBridgeOperations();
        var provider = new AccessBridgeProvider(operations);
        var uiThread = new JavaUiThread();
        provider.AttachUiThread(uiThread);

        var first = provider.AccessBridgeInstance;
        var second = provider.AccessBridgeInstance;

        Assert.Same(first, second);
        Assert.Equal(1, operations.CreateCount);
        Assert.Equal(
            uiThread.Invoke(() => Thread.CurrentThread.ManagedThreadId),
            operations.CreateThreadId);
    }

    [Fact]
    public void ProviderOperations_RunOnAttachedJavaUiThread()
    {
        var operations = new RecordingAccessBridgeOperations();
        var provider = new AccessBridgeProvider(operations);
        var uiThread = new JavaUiThread();
        provider.AttachUiThread(uiThread);

        provider.InitBridge();
        provider.EnumJvms(true);

        var javaThreadId = uiThread.Invoke(() => Thread.CurrentThread.ManagedThreadId);
        Assert.Equal(javaThreadId, operations.InitializeThreadId);
        Assert.Equal(javaThreadId, operations.EnumJvmsThreadId);
    }

    [Fact]
    public void AttachUiThread_IsIdempotentForSameInstance_AndRejectsDifferentInstance()
    {
        var provider = new AccessBridgeProvider(new RecordingAccessBridgeOperations());
        var first = new JavaUiThread();
        var second = new JavaUiThread();

        provider.AttachUiThread(first);
        provider.AttachUiThread(first);

        var exception = Assert.Throws<InvalidOperationException>(
            () => provider.AttachUiThread(second));
        Assert.Contains("Java UI thread", exception.Message);
    }

    [Fact]
    public void ProviderUseBeforeUiThreadAttachment_FailsClearly()
    {
        var provider = new AccessBridgeProvider(new RecordingAccessBridgeOperations());

        var exception = Assert.Throws<InvalidOperationException>(
            () => _ = provider.AccessBridgeInstance);

        Assert.Contains("Java UI thread", exception.Message);
    }

    private sealed class RecordingAccessBridgeOperations : IAccessBridgeOperations
    {
        public int CreateCount { get; private set; }

        public int CreateThreadId { get; private set; }

        public int InitializeThreadId { get; private set; }

        public int EnumJvmsThreadId { get; private set; }

        public AccessBridge Create()
        {
            CreateCount++;
            CreateThreadId = Thread.CurrentThread.ManagedThreadId;
            return new AccessBridge();
        }

        public void Initialize(AccessBridge accessBridge)
        {
            InitializeThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        public List<AccessibleJvm> EnumJvms(AccessBridge accessBridge)
        {
            EnumJvmsThreadId = Thread.CurrentThread.ManagedThreadId;
            return new List<AccessibleJvm>();
        }

        public bool IsJavaWindow(AccessBridge accessBridge, IntPtr windowHandle) => false;

        public void Dispose(AccessBridge accessBridge)
        {
        }
    }
}
