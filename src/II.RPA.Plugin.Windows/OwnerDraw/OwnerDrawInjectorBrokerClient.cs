using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Windows.OwnerDraw
{
    internal interface IOwnerDrawInjectorBrokerClient
    {
        bool TryInspect(int targetProcessId, int controllerProcessId,
            string bridgePath, string legacyFileName, int requiredAbi,
            out bool found, out string status);

        bool TryStart(int targetProcessId, int controllerProcessId,
            string bridgePath, string legacyFileName, int requiredAbi,
            out string status);

        bool TryStop(int targetProcessId, int controllerProcessId,
            string bridgePath, string legacyFileName, int requiredAbi,
            out OwnerDrawBridgeStopState stopState, out string status);
    }

    internal sealed class OwnerDrawBrokerProcessResult
    {
        internal bool Started { get; set; }
        internal bool TimedOut { get; set; }
        internal bool OutputLimitExceeded { get; set; }
        internal int ExitCode { get; set; }
        internal string StandardOutput { get; set; }
        internal string StandardError { get; set; }
        internal string Error { get; set; }
    }

    internal interface IOwnerDrawBrokerProcessRunner
    {
        OwnerDrawBrokerProcessResult Run(ProcessStartInfo startInfo, int timeoutMs);
    }

    internal sealed class OwnerDrawBrokerProcessRunner
        : IOwnerDrawBrokerProcessRunner
    {
        private const int MaximumStandardOutputCharacters = 262144;
        private const int MaximumStandardErrorCharacters = 8192;

        public OwnerDrawBrokerProcessResult Run(ProcessStartInfo startInfo,
            int timeoutMs)
        {
            var result = new OwnerDrawBrokerProcessResult
            {
                StandardOutput = string.Empty,
                StandardError = string.Empty,
                Error = string.Empty,
            };
            try
            {
                using (Process process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        result.Error = "Process.Start returned null.";
                        return result;
                    }
                    result.Started = true;
                    Task<BoundedTextResult> stdout = ReadBoundedAsync(
                        process.StandardOutput, MaximumStandardOutputCharacters);
                    Task<BoundedTextResult> stderr = ReadBoundedAsync(
                        process.StandardError, MaximumStandardErrorCharacters);
                    var watch = Stopwatch.StartNew();
                    bool exited = false;
                    while (watch.ElapsedMilliseconds < timeoutMs)
                    {
                        if ((stdout.IsCompleted && stdout.Result.Exceeded)
                            || (stderr.IsCompleted && stderr.Result.Exceeded))
                        {
                            result.OutputLimitExceeded = true;
                            break;
                        }
                        if (process.WaitForExit(50))
                        {
                            exited = true;
                            break;
                        }
                    }
                    if (!exited)
                    {
                        result.TimedOut = !result.OutputLimitExceeded;
                        try { process.Kill(); } catch { }
                        process.WaitForExit(2000);
                    }
                    else
                    {
                        // Complete asynchronous output delivery after the process
                        // handle is signalled.
                        process.WaitForExit();
                    }
                    if (stdout.Wait(2000))
                    {
                        result.StandardOutput = stdout.Result.Text;
                        result.OutputLimitExceeded |= stdout.Result.Exceeded;
                    }
                    if (stderr.Wait(2000))
                    {
                        result.StandardError = stderr.Result.Text;
                        result.OutputLimitExceeded |= stderr.Result.Exceeded;
                    }
                    if (!result.TimedOut && !result.OutputLimitExceeded
                        && process.HasExited)
                        result.ExitCode = process.ExitCode;
                    return result;
                }
            }
            catch (Exception ex)
            {
                result.Error = ex.GetType().Name + ": " + ex.Message;
                return result;
            }
        }

        private sealed class BoundedTextResult
        {
            internal string Text { get; set; }
            internal bool Exceeded { get; set; }
        }

        private static async Task<BoundedTextResult> ReadBoundedAsync(
            StreamReader reader, int maximumCharacters)
        {
            var text = new StringBuilder(Math.Min(maximumCharacters, 4096));
            var buffer = new char[4096];
            try
            {
                while (text.Length <= maximumCharacters)
                {
                    int count = await reader.ReadAsync(buffer, 0,
                        Math.Min(buffer.Length,
                            maximumCharacters + 1 - text.Length));
                    if (count == 0)
                        return new BoundedTextResult { Text = text.ToString() };
                    text.Append(buffer, 0, count);
                }
                return new BoundedTextResult
                {
                    Text = text.ToString(0, maximumCharacters),
                    Exceeded = true,
                };
            }
            catch (Exception ex) when (ex is IOException
                || ex is ObjectDisposedException)
            {
                return new BoundedTextResult { Text = text.ToString() };
            }
        }
    }

    internal sealed class OwnerDrawInjectorBrokerClient
        : IOwnerDrawInjectorBrokerClient
    {
        internal const string BrokerFileName = "OwnerDrawInjectorBroker.x86.exe";
        internal const int InspectTimeoutMs = 10000;
        internal const int StartTimeoutMs = 40000;
        internal const int StopTimeoutMs = 25000;

        private readonly string brokerPath;
        private readonly IOwnerDrawBrokerProcessRunner runner;

        internal OwnerDrawInjectorBrokerClient(string bridgeDirectory,
            IOwnerDrawBrokerProcessRunner runner = null)
        {
            string directory = string.IsNullOrWhiteSpace(bridgeDirectory)
                ? AppDomain.CurrentDomain.BaseDirectory : bridgeDirectory;
            brokerPath = Path.Combine(Path.GetFullPath(directory), BrokerFileName);
            this.runner = runner ?? new OwnerDrawBrokerProcessRunner();
        }

        public bool TryInspect(int targetProcessId, int controllerProcessId,
            string bridgePath, string legacyFileName, int requiredAbi,
            out bool found, out string status)
        {
            found = false;
            if (!TryInvoke(OwnerDrawInjectorBrokerOperation.Inspect,
                targetProcessId, controllerProcessId, bridgePath, legacyFileName,
                requiredAbi, InspectTimeoutMs, out OwnerDrawInjectorBrokerResponse response,
                out status))
                return false;
            if (response.Value == "Found") found = true;
            else if (response.Value != "NotFound")
            {
                status = "Broker Inspect returned an unknown value: "
                    + response.Value + ".";
                return false;
            }
            return true;
        }

        public bool TryStart(int targetProcessId, int controllerProcessId,
            string bridgePath, string legacyFileName, int requiredAbi,
            out string status)
        {
            return TryInvoke(OwnerDrawInjectorBrokerOperation.Start,
                targetProcessId, controllerProcessId, bridgePath, legacyFileName,
                requiredAbi, StartTimeoutMs, out _, out status);
        }

        public bool TryStop(int targetProcessId, int controllerProcessId,
            string bridgePath, string legacyFileName, int requiredAbi,
            out OwnerDrawBridgeStopState stopState, out string status)
        {
            stopState = OwnerDrawBridgeStopState.NotLoaded;
            if (!TryInvoke(OwnerDrawInjectorBrokerOperation.Stop,
                targetProcessId, controllerProcessId, bridgePath, legacyFileName,
                requiredAbi, StopTimeoutMs, out OwnerDrawInjectorBrokerResponse response,
                out status))
            {
                if (response != null && response.Value == "LegacyConflict")
                    stopState = OwnerDrawBridgeStopState.LegacyConflict;
                return false;
            }
            if (!Enum.TryParse(response.Value, false, out stopState)
                || !Enum.IsDefined(typeof(OwnerDrawBridgeStopState), stopState))
            {
                status = "Broker Stop returned an unknown state: "
                    + response.Value + ".";
                return false;
            }
            return true;
        }

        internal ProcessStartInfo BuildStartInfo(
            OwnerDrawInjectorBrokerOperation operation, int targetProcessId,
            int controllerProcessId, string bridgePath, string legacyFileName,
            int requiredAbi)
        {
            return new ProcessStartInfo
            {
                FileName = brokerPath,
                WorkingDirectory = Path.GetDirectoryName(brokerPath),
                Arguments = OwnerDrawInjectorBrokerProtocol.BuildArguments(operation,
                    targetProcessId, controllerProcessId, bridgePath,
                    legacyFileName, requiredAbi),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                WindowStyle = ProcessWindowStyle.Hidden,
            };
        }

        private bool TryInvoke(OwnerDrawInjectorBrokerOperation operation,
            int targetProcessId, int controllerProcessId, string bridgePath,
            string legacyFileName, int requiredAbi, int timeoutMs,
            out OwnerDrawInjectorBrokerResponse response, out string status)
        {
            response = null;
            if (!File.Exists(brokerPath))
            {
                status = "The packaged x86 owner-draw injector Broker is missing: \""
                    + brokerPath + "\".";
                return false;
            }

            ProcessStartInfo startInfo;
            try
            {
                startInfo = BuildStartInfo(operation, targetProcessId,
                    controllerProcessId, bridgePath, legacyFileName, requiredAbi);
            }
            catch (Exception ex)
            {
                status = "Cannot construct the fixed Broker request: "
                    + ex.GetType().Name + ": " + ex.Message;
                return false;
            }

            OwnerDrawBrokerProcessResult execution = runner.Run(startInfo, timeoutMs);
            if (!execution.Started)
            {
                status = "Cannot start the packaged x86 Broker. " + execution.Error;
                return false;
            }
            if (execution.TimedOut)
            {
                status = "The x86 Broker exceeded its " + timeoutMs
                    + " ms bounded lifetime and was terminated. The PB Bridge DLL "
                    + "is never unloaded; inspect the target before retrying.";
                return false;
            }
            if (execution.OutputLimitExceeded)
            {
                status = "The x86 Broker exceeded its bounded stdout/stderr "
                    + "allowance and was terminated; refusing the result.";
                return false;
            }
            if (!string.IsNullOrEmpty(execution.StandardError))
            {
                status = "The x86 Broker emitted unexpected stderr output; refusing "
                    + "an unstructured result.";
                return false;
            }
            if (!OwnerDrawInjectorBrokerProtocol.TryParseResponse(
                execution.StandardOutput, operation, out response,
                out string parseStatus))
            {
                status = parseStatus + " Broker exit=" + execution.ExitCode + ".";
                return false;
            }

            status = "x86 Broker operation=" + operation + " code="
                + response.Code + " value=" + response.Value + " exit="
                + execution.ExitCode + "; " + response.Status;
            if (execution.ExitCode != 0 || !response.Success)
                return false;
            return true;
        }
    }
}
