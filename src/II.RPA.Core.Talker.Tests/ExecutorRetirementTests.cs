using System.Diagnostics;
using II.RPA.Core.Talker.Models;
using II.RPA.Core.Talker.Processes;
using Xunit;

namespace II.RPA.Core.Talker.Tests;

public sealed class ExecutorRetirementTests
{
    [Theory]
    [InlineData(true, 0, 9)]
    [InlineData(false, 0, 3)]
    [InlineData(true, 1, 3)]
    public async Task OnlyAcknowledgedCleanRetirementBypassesCrashBudget(
        bool retirement, int exitCode, int expectedStarts)
    {
        var worker = new TestWorker(retirement, exitCode, expectedStarts);
        try
        {
            worker.Start();
            await worker.Finished.Task.WaitAsync(TimeSpan.FromSeconds(15));
            Assert.Equal(expectedStarts, worker.Starts);
        }
        finally
        {
            await worker.StopAsync();
        }
    }

    private sealed class TestWorker : BaseProcess
    {
        private readonly bool _retirement;
        private readonly int _exitCode;
        private readonly int _expectedStarts;
        public int Starts { get; private set; }
        public TaskCompletionSource Finished { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TestWorker(bool retirement, int exitCode, int expectedStarts) : base(NullLogger.Instance)
        {
            _retirement = retirement;
            _exitCode = exitCode;
            _expectedStarts = expectedStarts;
            OnProcessStatusChangedEvent += (_, status) =>
            {
                if (status == ProcessStatus.Started && _retirement)
                    ExpectCurrentProcessExit();
            };
        }

        protected override string ProcessName => "RetirementTest";
        protected override int MaximumAutomaticRestartAttempts => 2;
        protected override TimeSpan StableRunThreshold => TimeSpan.FromDays(1);
        protected override TimeSpan GetAutomaticRestartDelay(int attempt) => TimeSpan.FromMilliseconds(1);

        protected override Process GetChildProcess(ref bool addChildProcess)
        {
            addChildProcess = false;
            Starts++;
            return new Process
            {
                StartInfo = new ProcessStartInfo("cmd.exe", $"/d /c exit {_exitCode}")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                },
            };
        }

        protected override void ProcessExitHandle(bool isInitiativeStop, int? exitCode)
        {
            if (Starts == _expectedStarts)
            {
                // Verify crash paths actually exhaust the budget. Stop the clean
                // retirement path at nine generations so this test has a finite lifetime.
                if (!_retirement || _exitCode != 0)
                    base.ProcessExitHandle(isInitiativeStop, exitCode);
                Finished.TrySetResult();
                return;
            }
            base.ProcessExitHandle(isInitiativeStop, exitCode);
        }
    }
}
