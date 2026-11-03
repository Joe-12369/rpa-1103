using II.RPA.Plugin.Java.Execute;
using WindowsAccessBridgeInterop;
using Xunit;

namespace II.RPA.Plugin.Java.Tests;

public sealed class GuiOperatorThreadingTests
{
    [Fact]
    public void ImplementedJavaOperationEntries_EnterJavaUiThreadBeforeBusinessCode()
    {
        var uiThread = new RecordingJavaUiThread();
        var provider = new AccessBridgeProvider(new NoOpAccessBridgeOperations());
        var guiOperator = new GuiOperator(uiThread, provider);

        AssertEntersJavaUiThread(uiThread, () => guiOperator.IsCanUse(default, null, null));
        AssertEntersJavaUiThread(uiThread, () =>
        {
            guiOperator.InspectNode(default, null, null, null, out _);
        });
        AssertEntersJavaUiThread(uiThread, () => guiOperator.GetElementFromSelector(null));
        AssertEntersJavaUiThread(uiThread, () => guiOperator.GetElementFromSelectorAll(null));
        AssertEntersJavaUiThread(uiThread, () => guiOperator.CollectDataBySelector(null));
        AssertEntersJavaUiThread(uiThread, () => guiOperator.CollectData_v2(null));
        AssertEntersJavaUiThread(uiThread, () => guiOperator.QuerySelectorAll(null));
    }

    private static void AssertEntersJavaUiThread(
        RecordingJavaUiThread uiThread,
        Action operation)
    {
        uiThread.Reset();
        try
        {
            operation();
        }
        catch
        {
            // Null parameters deliberately stop the existing business body after entry.
        }

        Assert.True(uiThread.InvokeCount > 0);
        Assert.True(uiThread.WasInsideInvocation);
    }

    private sealed class RecordingJavaUiThread : IJavaUiThread
    {
        private int invocationDepth;

        public int InvokeCount { get; private set; }

        public bool WasInsideInvocation { get; private set; }

        public bool CheckAccess() => invocationDepth > 0;

        public void Reset()
        {
            InvokeCount = 0;
            WasInsideInvocation = false;
        }

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
            invocationDepth++;
            WasInsideInvocation = true;
            try
            {
                return operation();
            }
            finally
            {
                invocationDepth--;
            }
        }

        public Task InvokeAsync(Action operation)
        {
            Invoke(operation);
            return Task.CompletedTask;
        }

        public Task<TResult> InvokeAsync<TResult>(Func<TResult> operation)
        {
            return Task.FromResult(Invoke(operation));
        }

        public Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> operation)
        {
            return Invoke(operation);
        }
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
