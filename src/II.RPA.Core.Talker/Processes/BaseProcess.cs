using II.RPA.Core.Log;
using II.RPA.Core.Talker.Models;
using System.Diagnostics;

namespace II.RPA.Core.Talker.Processes
{
    public abstract class BaseProcess
    {
        private readonly object _lifecycleGate = new();
        private readonly HashSet<long> _intentionalStops = new();
        private ProcessStatus _status;
        private bool _isInitiativeStop;
        private long _processGeneration;
        private Task? _activeExitTask;
        private DateTimeOffset? _processStartedAt;
        private int _consecutiveUnexpectedExits;
        private bool _expectedExit;

        protected BaseProcess(ILogger logger)
        {
            Logger = logger;
        }

        public ProcessStatus Status
        {
            get => _status;
            private set
            {
                if (_status != value)
                {
                    _status = value;
                    OnProcessStatusChangedEvent?.Invoke(this, value);
                }
            }
        }

        public event EventHandler<ProcessStatus>? OnProcessStatusChangedEvent;

        public ILogger Logger { get; }

        public int? ProcessId { get; private set; }

        public int? LastExitCode { get; private set; }

        public Process? Process { get; private set; }

        public bool IsInitiativeStop => _isInitiativeStop;

        protected bool IsRestart { get; private set; }

        protected virtual int MaximumAutomaticRestartAttempts => 5;

        protected virtual TimeSpan StableRunThreshold => TimeSpan.FromMinutes(1);

        protected abstract string ProcessName { get; }

        /// <summary>
        /// Start/exit lines are useful for long-lived workers. High-frequency utility processes
        /// (see <see cref="ADBProcess"/>) downgrade them to trace to keep the log readable.
        /// </summary>
        protected virtual bool LogLifecycleAsDebug => true;

        protected virtual bool LogExitAsDebug(int? exitCode) => LogLifecycleAsDebug;

        protected abstract Process GetChildProcess(ref bool addChildProcess);

        protected virtual void HandleOutputDataReceived(string data) { }
        protected virtual void HandleErrorDataReceived(string data) { }

        public async Task RestartAsync(bool isRestart = false)
        {
            IsRestart = isRestart;
            try
            {
                await StopAsync().ConfigureAwait(false);
                Start();
            }
            finally
            {
                IsRestart = false;
            }
        }

        public void Suspend()
        {
            Process? process;
            lock (_lifecycleGate)
            {
                process = Process;
            }

            if (ProcessIsStarted(process))
            {
                process!.Suspend();
            }
        }

        public void Resume()
        {
            Process? process;
            lock (_lifecycleGate)
            {
                process = Process;
            }

            if (ProcessIsStarted(process))
            {
                process!.Resume();
            }
        }

        public void Start() => StartCore(isAutomaticRestart: false);

        private void StartCore(bool isAutomaticRestart)
        {
            lock (_lifecycleGate)
            {
                if (ProcessIsStarted(Process))
                {
                    Logger.LogDebug($"进程{ProcessName}已经运行【{ProcessId}】，忽略重复启动");
                    return;
                }

                if (!isAutomaticRestart)
                {
                    _consecutiveUnexpectedExits = 0;
                }
            }

            Status = ProcessStatus.Starting;
            LastExitCode = null;
            Process? process = null;
            try
            {
                var addChildProcess = true;
                process = GetChildProcess(ref addChildProcess);
                if (!ProcessIsStarted(process) && !process.Start())
                {
                    throw new InvalidOperationException("启动子进程失败。");
                }

                if (process.StartInfo.RedirectStandardOutput)
                {
                    process.OutputDataReceived += Process_OutputDataReceived;
                    process.BeginOutputReadLine();
                }

                if (process.StartInfo.RedirectStandardError)
                {
                    process.ErrorDataReceived += Process_ErrorDataReceived;
                    process.BeginErrorReadLine();
                }

                if (addChildProcess)
                {
                    ChildProcessJob.AddChildProcess(process);
                }

                long generation;
                lock (_lifecycleGate)
                {
                    generation = ++_processGeneration;
                    _isInitiativeStop = false;
                    _expectedExit = false;
                    Process = process;
                    ProcessId = process.Id;
                    _processStartedAt = DateTimeOffset.UtcNow;
                    Status = ProcessStatus.Started;
                    _activeExitTask = Task.Run(() => ObserveProcessExitAsync(process, generation));
                }

                LogLifecycle($"启动{ProcessName}进程成功【{ProcessId}】");
            }
            catch (Exception ex)
            {
                if (process != null)
                {
                    process.OutputDataReceived -= Process_OutputDataReceived;
                    process.ErrorDataReceived -= Process_ErrorDataReceived;
                    process.Dispose();
                }

                Logger.LogWarning($"启动{ProcessName}进程失败", ex);
                Status = ProcessStatus.Stopped;
                throw new InvalidOperationException($"启动{ProcessName}进程失败。", ex);
            }
        }

        public async Task StopAsync()
        {
            Process? process;
            Task? exitTask;
            long generation;
            lock (_lifecycleGate)
            {
                _isInitiativeStop = true;
                _consecutiveUnexpectedExits = 0;
                process = Process;
                exitTask = _activeExitTask;
                generation = _processGeneration;
                if (process != null)
                {
                    _intentionalStops.Add(generation);
                }
            }

            if (process == null)
            {
                Status = ProcessStatus.Stopped;
                return;
            }

            try
            {
                if (ProcessIsStarted(process))
                {
                    process.Kill(entireProcessTree: true);
                }

                if (exitTask != null)
                {
                    await exitTask.ConfigureAwait(false);
                }
                else
                {
                    await process.WaitForExitAsync().ConfigureAwait(false);
                    process.Dispose();
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"主动关闭{ProcessName}[{ProcessId}]进程失败", ex);
            }
        }

        public Task WaitForExitAsync(CancellationToken cancellationToken = default)
        {
            Task? exitTask;
            lock (_lifecycleGate)
            {
                exitTask = _activeExitTask;
            }

            if (exitTask == null)
            {
                return Task.CompletedTask;
            }

            return cancellationToken.CanBeCanceled
                ? exitTask.WaitAsync(cancellationToken)
                : exitTask;
        }

        protected virtual void ProcessExitHandle(bool isInitiativeStop, int? exitCode)
        {
            if (!isInitiativeStop)
            {
                ScheduleAutomaticRestart();
            }
        }

        // A worker can retire itself after completing a request. Keep this distinct
        // from StopAsync: retirement must replenish capacity, while StopAsync must not.
        protected void ExpectCurrentProcessExit()
        {
            lock (_lifecycleGate)
            {
                if (Process != null)
                {
                    _expectedExit = true;
                }
            }
        }

        protected virtual TimeSpan GetAutomaticRestartDelay(int attempt)
        {
            var milliseconds = Math.Min(500 * Math.Pow(2, attempt - 1), 30000);
            return TimeSpan.FromMilliseconds(milliseconds);
        }

        private void ScheduleAutomaticRestart()
        {
            int attempt;
            long sourceGeneration;
            TimeSpan delay;
            lock (_lifecycleGate)
            {
                if (_isInitiativeStop || Process != null)
                {
                    return;
                }

                var expected = _expectedExit && LastExitCode == 0;
                if (expected || (_processStartedAt.HasValue &&
                    DateTimeOffset.UtcNow - _processStartedAt.Value >= StableRunThreshold))
                {
                    _consecutiveUnexpectedExits = 0;
                }

                attempt = expected ? 0 : ++_consecutiveUnexpectedExits;
                if (attempt > MaximumAutomaticRestartAttempts)
                {
                    Logger.LogError(
                        $"进程{ProcessName}连续异常退出{attempt}次，已触发自动重启熔断，请检查部署文件和子进程日志");
                    return;
                }

                sourceGeneration = _processGeneration;
                delay = expected ? TimeSpan.Zero : GetAutomaticRestartDelay(attempt);
            }

            if (attempt == 0)
            {
                Logger.LogDebug($"Worker {ProcessName} retired normally; replenishing capacity.");
            }
            else
            {
                Logger.LogWarning(
                    $"进程{ProcessName}异常退出，将在{delay.TotalMilliseconds:0}ms后进行第{attempt}次自动重启");
            }
            _ = RestartAfterDelayAsync(sourceGeneration, delay);
        }

        private async Task RestartAfterDelayAsync(long sourceGeneration, TimeSpan delay)
        {
            try
            {
                await Task.Delay(delay).ConfigureAwait(false);
                lock (_lifecycleGate)
                {
                    // StopAsync, an explicit Start/Restart, or a newer process generation invalidates
                    // this delayed restart. Holding the gate across StartCore closes the stop/restart race.
                    if (_isInitiativeStop ||
                        Process != null ||
                        _processGeneration != sourceGeneration)
                    {
                        return;
                    }

                    StartCore(isAutomaticRestart: true);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"自动重启{ProcessName}进程失败", ex);
            }
        }

        private async Task ObserveProcessExitAsync(Process process, long generation)
        {
            int? exitCode = null;
            try
            {
                await process.WaitForExitAsync().ConfigureAwait(false);
                try
                {
                    exitCode = process.ExitCode;
                    LastExitCode = exitCode;
                }
                catch
                {
                }

                process.OutputDataReceived -= Process_OutputDataReceived;
                process.ErrorDataReceived -= Process_ErrorDataReceived;

                bool intentional;
                bool isCurrent;
                lock (_lifecycleGate)
                {
                    intentional = _intentionalStops.Remove(generation);
                    isCurrent = generation == _processGeneration && ReferenceEquals(Process, process);
                    if (isCurrent)
                    {
                        Process = null;
                        ProcessId = null;
                        _activeExitTask = null;
                    }
                }

                LogLifecycle($"进程{ProcessName}已退出，exitCode:{exitCode}，本次是否为主动退出：{intentional}", LogExitAsDebug(exitCode));

                // A previous process generation must never mutate or restart the current process.
                if (isCurrent)
                {
                    Status = ProcessStatus.Stopped;
                    ProcessExitHandle(intentional, exitCode);
                }
            }
            finally
            {
                process.Dispose();
            }
        }

        private static bool ProcessIsStarted(Process? process)
        {
            try
            {
                return process != null && !process.HasExited;
            }
            catch
            {
                return false;
            }
        }

        private void LogLifecycle(string message, bool? asDebug = null)
        {
            if (asDebug ?? LogLifecycleAsDebug)
            {
                Logger.LogDebug(message);
            }
            else
            {
                Logger.LogTrace(message);
            }
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            HandleErrorDataReceived(e.Data ?? string.Empty);
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            HandleOutputDataReceived(e.Data ?? string.Empty);
        }
    }
}
