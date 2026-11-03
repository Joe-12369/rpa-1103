#nullable disable

using II.RPA.Core.Log;
using II.RPA.Core.Runtime;
using II.RPA.Core.Talker.Talkers;
using II.RPA.Logging.Configurations;
using Xunit;

namespace II.RPA.Core.Talker.Tests;

public sealed class ExecutorRestartCancellationTests
{
    [Fact]
    public async Task VersionIsAppliedBeforeRestart()
    {
        var version = "3.6.8";
        var versionSeenByRestart = string.Empty;

        await PythonEnvironmentVersionTransaction.ApplyAsync(
            "3.11.9",
            () => version,
            value => version = string.IsNullOrEmpty(value) ? "3.6.8" : value,
            () =>
            {
                versionSeenByRestart = version;
                return Task.CompletedTask;
            });

        Assert.Equal("3.11.9", versionSeenByRestart);
    }

    [Fact]
    public async Task VersionRollsBackWhenRestartFails()
    {
        var version = "3.11.9";

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            PythonEnvironmentVersionTransaction.ApplyAsync(
                string.Empty,
                () => version,
                value => version = string.IsNullOrEmpty(value) ? "3.6.8" : value,
                () => Task.FromException(new InvalidOperationException("start failed"))));

        Assert.Equal("3.11.9", version);
    }

    [Fact]
    public async Task RestartDoesNotStopWorkerWhenCancellationPrecedesGateAcquisition()
    {
        var executor = new RestartProbeExecutor(new NoOpLogger(), new TestRuntimeContext());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => executor.RestartAsync(cancellation.Token));

        Assert.Equal(0, executor.StopCalls);
        Assert.Equal(0, executor.StartCalls);
    }

    [Fact]
    public async Task RestartStartsWorkerWhenCallerCancelsAfterStopBegins()
    {
        var executor = new RestartProbeExecutor(new NoOpLogger(), new TestRuntimeContext());
        using var cancellation = new CancellationTokenSource();

        var restartTask = executor.RestartAsync(cancellation.Token);
        await executor.StopStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        cancellation.Cancel();
        executor.AllowStopToFinish();

        await restartTask.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Equal(1, executor.StopCalls);
        Assert.Equal(1, executor.StartCalls);
    }

    private sealed class RestartProbeExecutor : ExecutorTalker
    {
        private readonly TaskCompletionSource _allowStopToFinish =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public RestartProbeExecutor(ILogger logger, IRpaRuntimeContext runtimeContext)
            : base(null, logger, runtimeContext)
        {
        }

        public TaskCompletionSource StopStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int StartCalls { get; private set; }

        public int StopCalls { get; private set; }

        public override Task Start()
        {
            StartCalls++;
            return Task.CompletedTask;
        }

        public override async Task Stop()
        {
            StopCalls++;
            StopStarted.TrySetResult();
            await _allowStopToFinish.Task.ConfigureAwait(false);
        }

        public void AllowStopToFinish() => _allowStopToFinish.TrySetResult();
    }

    private sealed class TestRuntimeContext : IRpaRuntimeContext
    {
        public RpaClientKind ClientKind => RpaClientKind.Bot;
        public string ClientId => "test-client";
        public string ProducedClientId => "test-client";
        public string RootClientId => "test-client";
        public int CurrentProcessId => 1;
        public int SessionId => 1;
        public int ServerPort => 1;
        public int RootServerPort => 1;
        public bool LoopAdb => false;
        public bool IsMiniModeActive => false;
        public bool ForceRestartHost => false;
        public string EngineExecutablePath => "rpa-engine.exe";
        public string PythonEnvironmentVersion => "3.6.8";
        public string UserInfoParameter { get; set; }

        public string BuildUserInfoParameter() => string.Empty;
        public string FixZBrainUriForZDesktop(string parameter) => parameter;
        public void SetSetting(string key, object value) { }
        public string GetConfiguration(string commandLineName, string configName) => null;
        public bool IsMobileProject(string packagePath) => false;
    }

    private sealed class NoOpLogger : ILogger
    {
        public void LogTrace(string aMessage, string callerMemberName = null, string aCallerFilePath = null) { }
        public void LogTrace(string aMessage, Exception anException, string callerMemberName = null, string aCallerFilePath = null) { }
        public void LogDebug(string aMessage, string callerMemberName = null, string aCallerFilePath = null) { }
        public void LogDebug(string aMessage, Exception anException, string callerMemberName = null, string aCallerFilePath = null) { }
        public void LogInfo(string aMessage, string callerMemberName = null, string aCallerFilePath = null) { }
        public void LogInfo(string aMessage, Exception anException, string callerMemberName = null, string aCallerFilePath = null) { }
        public void LogWarning(string aMessage, string callerMemberName = null, string aCallerFilePath = null) { }
        public void LogWarning(string aMessage, Exception anException, string callerMemberName = null, string aCallerFilePath = null) { }
        public void LogError(string aMessage, string callerMemberName = null, string aCallerFilePath = null) { }
        public void LogError(string aMessage, Exception anException, string callerMemberName = null, string aCallerFilePath = null) { }
        public void LogError(Exception anException, string callerMemberName = null, string aCallerFilePath = null) { }
        public void LogFatal(string aMessage, string callerMemberName = null, string aCallerFilePath = null) { }
        public void LogFatal(string aMessage, Exception anException, string callerMemberName = null, string aCallerFilePath = null) { }
        public void LogFatal(Exception anException, string callerMemberName = null, string aCallerFilePath = null) { }
        public void LogCommunication(string aMessage, Exception anException, StreamScope aStreamScope, string aCallerFilePath = null) { }
        public void LogCommunication(string aMessage, StreamScope aStreamScope, string aCallerFilePath = null) { }
        public void LogFFmpeg(string aMessage, string aCallerFilePath = null) { }
        public void LogZBrain(string aMessage, string aCallerFilePath = null) { }
        public void LogExecutor(string aMessage, string aCallerFilePath = null) { }
        public void LogInterpreter(string aMessage, string aCallerFilePath = null) { }
        public void LogZdesktop(string aMessage, string aCallerFilePath = null) { }
        public void LogMCPAgent(string aMessage, string aCallerFilePath = null) { }
    }
}
