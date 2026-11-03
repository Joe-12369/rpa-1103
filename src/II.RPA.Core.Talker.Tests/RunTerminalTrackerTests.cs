using II.RPA.Core.Talker.Talkers;
using Xunit;
using System.Reflection;
using II.RPA.Core.Contracts.Talker.Engine;
using II.RPA.Core.Runtime;
using II.RPA.Core.Talker.Models;

namespace II.RPA.Core.Talker.Tests;

public sealed class RunTerminalTrackerTests
{
    [Fact]
    public void FinishReleasesExecutionSlotButRetainsPendingTerminalAndLateDone()
    {
        var executor = new EventProbe();
        var tracker = (RunTerminalTracker)typeof(ExecutorTalker)
            .GetField("_terminalTracker", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(executor)!;
        tracker.StartRun("run", "task");
        typeof(ExecutorTalker).GetField("_activeRunRequestId", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(executor, "run");
        executor.Receive(new ActionEvent { Action = "run", RequestId = "run", Type = ActionEventType.Finish });
        Assert.False(executor.IsRunning);
        Assert.Single(tracker.SnapshotPendingRuns());
        executor.Receive(new ActionEvent { Action = "run", RequestId = "run", Type = ActionEventType.Done });
        tracker.MarkBanished("run");
        Assert.True(tracker.TryClaimCompensation("run", 0, out _));
    }

    private sealed class EventProbe : ExecutorTalker
    {
        public EventProbe() : base(null!, NullLogger.Instance,
            DispatchProxy.Create<IRpaRuntimeContext, RuntimeProxy>()) { }
        public void Receive(ActionEvent message) => OnActionEvent(message);

        public void TrackRun(string requestId, string actionId, long generation)
            => Tracker.StartRun(requestId, actionId, generation);

        public void SetActiveRun(string requestId, string actionId)
        {
            typeof(ExecutorTalker)
                .GetField("_activeRunRequestId", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(this, requestId);
            typeof(ExecutorTalker)
                .GetField("_activeRunActionId", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(this, actionId);
        }

        public void CommitTerminal(string requestId)
        {
            Assert.True(Tracker.TryClaimTerminal(requestId));
            Tracker.CommitTerminal(requestId);
        }

        public Task NotifyProcessStoppedAsync()
        {
            var method = typeof(ExecutorTalker).GetMethod(
                "HandleExecutorProcessStatusChangedAsync",
                BindingFlags.NonPublic | BindingFlags.Instance)!;
            return (Task)method.Invoke(this, new object[] { ProcessStatus.Stopped })!;
        }

        private RunTerminalTracker Tracker =>
            (RunTerminalTracker)typeof(ExecutorTalker)
                .GetField("_terminalTracker", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(this)!;
    }

    public class RuntimeProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
            => targetMethod!.ReturnType.IsValueType ? Activator.CreateInstance(targetMethod.ReturnType) : null;
    }

    [Theory]
    [InlineData(false, false, 0, false)]
    [InlineData(true, false, 0, false)]
    [InlineData(false, true, 0, false)]
    [InlineData(true, true, 1, false)]
    [InlineData(true, true, 0, true)]
    public void SuccessRequiresAllRetirementEvidence(bool done, bool banished, int exitCode, bool expected)
    {
        var tracker = new RunTerminalTracker();
        tracker.StartRun("old", "task");
        if (done) tracker.MarkDone("old");
        if (banished) tracker.MarkBanished("old");
        Assert.Equal(expected, tracker.TryClaimCompensation("old", exitCode, out _));
        if (!expected)
            Assert.True(tracker.TryClaimTerminal("old"));
    }

    [Fact]
    public void DoneWithoutTerminalStillRequiresReconciliation()
    {
        var tracker = new RunTerminalTracker();
        tracker.StartRun("old", "task");
        tracker.MarkDone("old");
        Assert.True(tracker.ShouldReportInterruptedRun("old", completed: true));
        Assert.Single(tracker.SnapshotPendingRuns());
    }

    [Fact]
    public void ExitSnapshotCannotIncludeReplacementRun()
    {
        var tracker = new RunTerminalTracker();
        tracker.StartRun("old", "old-task");
        var exiting = tracker.SnapshotPendingRuns();
        tracker.StartRun("new", "new-task");
        Assert.Equal("old", Assert.Single(exiting).RequestId);
        tracker.ClearActiveRun("old");
        Assert.Equal("new", Assert.Single(tracker.SnapshotPendingRuns()).RequestId);
    }

    [Fact]
    public void ExitEvidenceIsRestrictedToItsConnectionGeneration()
    {
        var tracker = new RunTerminalTracker();
        tracker.StartRun("old", "old-task", 1);
        tracker.StartRun("new", "new-task", 2);
        Assert.Equal("old", Assert.Single(tracker.SnapshotPendingRuns(1)).RequestId);
        Assert.Equal("new", Assert.Single(tracker.SnapshotPendingRuns(2)).RequestId);
    }

    [Fact]
    public void ExitAlsoCleansTerminalDeliveredAfterFinish()
    {
        var tracker = new RunTerminalTracker();
        tracker.StartRun("run", "task", 1);
        Assert.True(tracker.TryClaimTerminal("run"));
        tracker.CommitTerminal("run");
        Assert.Empty(tracker.SnapshotPendingRuns(1));
        Assert.Equal("run", Assert.Single(tracker.SnapshotExitingRuns(1)).RequestId);
        tracker.ClearActiveRun("run");
        Assert.Empty(tracker.SnapshotExitingRuns(1));
        Assert.True(tracker.HasTerminalForwarded("run"));
    }

    [Fact]
    public async Task ProcessStopReleasesActiveRunWhenExitSnapshotGenerationDiffers()
    {
        var executor = new EventProbe();
        executor.TrackRun("stale", "task", generation: 1);
        executor.SetActiveRun("stale", "task");
        executor.CommitTerminal("stale");

        await executor.NotifyProcessStoppedAsync();

        Assert.False(executor.IsRunning);
    }

    [Fact]
    public void FailedDeliveryCanRetryAndCommittedTerminalCannotBeReplaced()
    {
        var tracker = new RunTerminalTracker();
        tracker.StartRun("run", "task");
        Assert.True(tracker.TryClaimTerminal("run"));
        Assert.False(tracker.TryClaimTerminal("run"));
        tracker.RollbackTerminal("run");
        Assert.Single(tracker.SnapshotPendingRuns());
        Assert.True(tracker.TryClaimTerminal("run"));
        tracker.CommitTerminal("run");
        Assert.Empty(tracker.SnapshotPendingRuns());
        tracker.ClearActiveRun("run");
        Assert.True(tracker.HasTerminalForwarded("run"));
        Assert.False(tracker.TryClaimTerminal("run"));
    }

    [Fact]
    public void InFlightClaimsSurviveCompletedClaimRetentionLimit()
    {
        var tracker = new RunTerminalTracker();
        tracker.StartRun("pending", "task");
        Assert.True(tracker.TryClaimTerminal("pending"));
        for (var i = 0; i < 100; i++)
        {
            var id = $"completed-{i}";
            tracker.StartRun(id, id);
            Assert.True(tracker.TryClaimTerminal(id));
            tracker.CommitTerminal(id);
            tracker.ClearActiveRun(id);
        }
        Assert.False(tracker.TryClaimTerminal("pending"));
    }
}
