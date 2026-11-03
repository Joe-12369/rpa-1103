using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using II.RPA.IPC.NamedPipe;
using II.RPA.IPC.NamedPipe.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace II.RPA.NativeMessagingHost.PerformanceTests
{
    internal static class Program
    {
        private const string PipeKey = "U2FsdGVkX19Xagmra6VZ0BB2FitqhBx8";
        private const string NmhProcessName = "II.RPA.NativeMessagingHost";
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        private static int Main(string[] args)
        {
            try
            {
                int iterations = ReadInt(args, "--iterations", 1);
                string output = ReadString(args, "--output", Path.Combine(Environment.CurrentDirectory, "chrome-nmh-performance"));
                string probeCommand = ReadString(args, "--probe-command", null);
                string browserExecutable = ReadString(args, "--browser-exe", @"C:\Program Files\Google\Chrome\Application\chrome.exe");
                string browserProcessName = ReadString(args, "--browser-process", "chrome");
                string targetClientFlag = ReadString(args, "--target-client-flag", "Chrome");
                RunAsync(iterations, output, probeCommand, browserExecutable, browserProcessName, targetClientFlag).GetAwaiter().GetResult();
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static async Task RunAsync(int iterations, string output, string probeCommand, string browserExecutable, string browserProcessName, string targetClientFlag)
        {
            if (iterations <= 0) throw new ArgumentOutOfRangeException(nameof(iterations));
            var samples = new List<Sample>();
            for (int i = 1; i <= iterations; i++)
            {
                Sample sample = await MeasureAsync(i, probeCommand, browserExecutable, browserProcessName, targetClientFlag).ConfigureAwait(false);
                samples.Add(sample);
                Console.WriteLine(JsonConvert.SerializeObject(sample, Formatting.None));
                await Task.Delay(1000).ConfigureAwait(false);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
            File.WriteAllText(output + ".json", JsonConvert.SerializeObject(new
            {
                generatedAt = DateTimeOffset.Now,
                samples,
                summary = BuildSummary(samples)
            }, Formatting.Indented), new UTF8Encoding(false));
            File.WriteAllText(output + ".csv", BuildCsv(samples), new UTF8Encoding(true));
            Console.WriteLine(JsonConvert.SerializeObject(BuildSummary(samples), Formatting.Indented));
        }

        private static async Task<Sample> MeasureAsync(int iteration, string probeCommand, string browserExecutable, string browserProcessName, string targetClientFlag)
        {
            var sample = new Sample { Iteration = iteration, Timestamp = DateTimeOffset.Now };
            NamedPipeClient client = null;
            try
            {
                sample.FailureStage = "StopExistingProcesses";
                StopAll(browserProcessName);
                StopAll(NmhProcessName);
                await WaitForNoProcessesAsync(browserProcessName, TimeSpan.FromSeconds(15)).ConfigureAwait(false);

                sample.FailureStage = "StartChrome";
                var clock = Stopwatch.StartNew();
                Process chrome = Process.Start(new ProcessStartInfo
                {
                    FileName = browserExecutable,
                    Arguments = "https://www.baidu.com/",
                    UseShellExecute = true
                });
                sample.ChromePid = chrome == null ? 0 : chrome.Id;

                sample.FailureStage = "ObserveNmh";
                Process nmh = await WaitForProcessAsync(NmhProcessName, TimeSpan.FromSeconds(20)).ConfigureAwait(false);
                sample.BrowserToNmhMs = clock.Elapsed.TotalMilliseconds;
                NmhObservation observation = await WaitForStableNmhAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                nmh = observation.Process;
                sample.NmhPid = nmh.Id;
                sample.NmhFileVersion = FileVersionInfo.GetVersionInfo(observation.ExecutablePath).FileVersion;
                sample.PipeName = PipeKey + "_" + nmh.Id.ToString(CultureInfo.InvariantCulture);

                sample.FailureStage = "ConnectPipe";
                client = new NamedPipeClient(sample.PipeName, new NamedPipeClientOptions { ConnectTimeout = TimeSpan.FromSeconds(10) });
                await client.ConnectAsync(CancellationToken.None).ConfigureAwait(false);
                var frameworkPing = Stopwatch.StartNew();
                await client.PingAsync(TimeSpan.FromSeconds(2), CancellationToken.None).ConfigureAwait(false);
                frameworkPing.Stop();
                sample.FrameworkPingRttMs = frameworkPing.Elapsed.TotalMilliseconds;
                sample.BrowserToReadyMs = clock.Elapsed.TotalMilliseconds;
                sample.NmhToReadyMs = sample.BrowserToReadyMs - sample.BrowserToNmhMs;

                if (!string.IsNullOrWhiteSpace(probeCommand))
                {
                    sample.FailureStage = "Probe:" + probeCommand;
                    ResponseMeasurement probe = await SendAsync(client, probeCommand, 5000, targetClientFlag).ConfigureAwait(false);
                    sample.ProbeCommand = probeCommand;
                    sample.ProbeRttMs = probe.ElapsedMs;
                    sample.ProbeResponse = probe.Response.ToString(Formatting.None);
                    EnsureSuccessfulBusinessResponse(probeCommand, probe.Response);
                    sample.Success = true;
                    sample.FailureStage = null;
                    return sample;
                }

                sample.FailureStage = "GetVersion";
                ResponseMeasurement getVersion = await WaitForSuccessfulResponseAsync(client, "getVersion", clock, sample, targetClientFlag).ConfigureAwait(false);
                sample.GetVersionRttMs = getVersion.ElapsedMs;
                sample.GetVersionResponse = getVersion.Response.ToString(Formatting.None);
                sample.CrxVersion = getVersion.Response.SelectToken("CallbackReslut.result.content")?.Value<string>();
                sample.BrowserToCrxReadyMs = clock.Elapsed.TotalMilliseconds;

                sample.FailureStage = "Ping";
                ResponseMeasurement ping = await SendAsync(client, "ping", 5000, targetClientFlag).ConfigureAwait(false);
                EnsureSuccessfulBusinessResponse("ping", ping.Response);
                sample.PingRttMs = ping.ElapsedMs;
                sample.PingResponse = ping.Response.ToString(Formatting.None);
                sample.Success = true;
                sample.FailureStage = null;
                return sample;
            }
            catch (Exception exception)
            {
                sample.Exception = exception.ToString();
                return sample;
            }
            finally
            {
                if (client != null) client.Dispose();
            }
        }

        private static void EnsureSuccessfulBusinessResponse(string command, JObject response)
        {
            JToken callback = response?["CallbackReslut"];
            int? code = callback?["code"]?.Value<int?>();
            JToken error = callback?["result"]?["error"];
            if (code != 200 || (error != null && error.Type != JTokenType.Null))
                throw new InvalidOperationException(command + " returned an unsuccessful CRX response: " + response?.ToString(Formatting.None));
        }

        private static async Task<ResponseMeasurement> WaitForSuccessfulResponseAsync(NamedPipeClient client, string command, Stopwatch browserClock, Sample sample, string targetClientFlag)
        {
            Stopwatch readiness = Stopwatch.StartNew();
            JObject lastResponse = null;
            while (readiness.Elapsed < TimeSpan.FromSeconds(15))
            {
                ResponseMeasurement measurement = await SendAsync(client, command, 5000, targetClientFlag).ConfigureAwait(false);
                lastResponse = measurement.Response;
                try
                {
                    EnsureSuccessfulBusinessResponse(command, measurement.Response);
                    return measurement;
                }
                catch (InvalidOperationException)
                {
                    sample.CrxReadinessAttempts++;
                    await Task.Delay(250).ConfigureAwait(false);
                }
            }
            throw new TimeoutException(command + " did not return a successful CRX response within 15 seconds. Last response: " + lastResponse?.ToString(Formatting.None));
        }

        private static async Task<ResponseMeasurement> SendAsync(NamedPipeClient client, string command, int timeoutMs, string targetClientFlag)
        {
            string messageId = Guid.NewGuid().ToString("N");
            var completion = new TaskCompletionSource<JObject>(TaskCreationOptions.RunContinuationsAsynchronously);
            EventHandler<NamedPipeMessageReceivedEventArgs> handler = (sender, eventArgs) =>
            {
                try
                {
                    JObject response = JObject.Parse(StrictUtf8.GetString(eventArgs.Payload));
                    if (string.Equals(response.Value<string>("MessageId"), messageId, StringComparison.OrdinalIgnoreCase))
                        completion.TrySetResult(response);
                }
                catch (Exception exception) { completion.TrySetException(exception); }
            };
            client.MessageReceived += handler;
            try
            {
                var request = new JObject
                {
                    ["MessageId"] = messageId,
                    ["Action"] = "SendTransferMessage",
                    ["TargetClientType"] = 4,
                    ["TargetClientFlag"] = targetClientFlag,
                    ["SendData"] = new JObject { ["method"] = command, ["params"] = new JObject(), ["codeId"] = "" },
                    ["MessageType"] = 1,
                    ["Timeout"] = timeoutMs,
                    ["Timestamp"] = Stopwatch.GetTimestamp()
                };
                var stopwatch = Stopwatch.StartNew();
                await client.SendAsync(StrictUtf8.GetBytes(request.ToString(Formatting.None)), CancellationToken.None).ConfigureAwait(false);
                Task winner = await Task.WhenAny(completion.Task, Task.Delay(timeoutMs)).ConfigureAwait(false);
                if (!ReferenceEquals(winner, completion.Task)) throw new TimeoutException(command + " did not return within " + timeoutMs + " ms.");
                JObject response = await completion.Task.ConfigureAwait(false);
                stopwatch.Stop();
                return new ResponseMeasurement { ElapsedMs = stopwatch.Elapsed.TotalMilliseconds, Response = response };
            }
            finally { client.MessageReceived -= handler; }
        }

        private static async Task<Process> WaitForProcessAsync(string name, TimeSpan timeout)
        {
            Stopwatch clock = Stopwatch.StartNew();
            while (clock.Elapsed < timeout)
            {
                Process process = Process.GetProcessesByName(name).OrderByDescending(p => SafeStartTime(p)).FirstOrDefault();
                if (process != null) return process;
                await Task.Delay(10).ConfigureAwait(false);
            }
            throw new TimeoutException(name + " was not observed within " + timeout.TotalSeconds + " seconds.");
        }

        private static async Task<NmhObservation> WaitForStableNmhAsync(TimeSpan timeout)
        {
            Stopwatch clock = Stopwatch.StartNew();
            while (clock.Elapsed < timeout)
            {
                foreach (Process process in Process.GetProcessesByName(NmhProcessName).OrderByDescending(p => SafeStartTime(p)))
                {
                    try
                    {
                        if (!process.HasExited && process.MainModule != null && !string.IsNullOrWhiteSpace(process.MainModule.FileName))
                            return new NmhObservation { Process = process, ExecutablePath = process.MainModule.FileName };
                    }
                    catch (InvalidOperationException) { }
                    catch (System.ComponentModel.Win32Exception) { }
                }
                await Task.Delay(10).ConfigureAwait(false);
            }
            throw new TimeoutException("No stable NMH process with a readable executable path was observed.");
        }

        private static async Task WaitForNoProcessesAsync(string browserProcessName, TimeSpan timeout)
        {
            Stopwatch clock = Stopwatch.StartNew();
            while (clock.Elapsed < timeout)
            {
                if (Process.GetProcessesByName(browserProcessName).Length == 0 && Process.GetProcessesByName(NmhProcessName).Length == 0) return;
                await Task.Delay(50).ConfigureAwait(false);
            }
            throw new TimeoutException("Chrome/NMH did not fully exit.");
        }

        private static void StopAll(string name)
        {
            foreach (Process process in Process.GetProcessesByName(name))
            {
                try { process.Kill(); } catch (InvalidOperationException) { } catch (System.ComponentModel.Win32Exception) { }
            }
        }

        private static DateTime SafeStartTime(Process process) { try { return process.StartTime; } catch { return DateTime.MinValue; } }

        private static object BuildSummary(IReadOnlyCollection<Sample> samples)
        {
            Sample[] successful = samples.Where(s => s.Success).ToArray();
            return new
            {
                count = samples.Count,
                success = successful.Length,
                failure = samples.Count - successful.Length,
                BrowserToNmhMs = Stats(successful.Select(s => s.BrowserToNmhMs)),
                NmhToReadyMs = Stats(successful.Select(s => s.NmhToReadyMs)),
                BrowserToReadyMs = Stats(successful.Select(s => s.BrowserToReadyMs)),
                BrowserToCrxReadyMs = Stats(successful.Select(s => s.BrowserToCrxReadyMs)),
                FrameworkPingRttMs = Stats(successful.Select(s => s.FrameworkPingRttMs)),
                GetVersionRttMs = Stats(successful.Select(s => s.GetVersionRttMs)),
                PingRttMs = Stats(successful.Select(s => s.PingRttMs))
            };
        }

        private static object Stats(IEnumerable<double> values)
        {
            double[] sorted = values.OrderBy(v => v).ToArray();
            if (sorted.Length == 0) return null;
            return new { min = R(sorted[0]), mean = R(sorted.Average()), p50 = R(P(sorted, .50)), p95 = R(P(sorted, .95)), p99 = R(P(sorted, .99)), max = R(sorted[sorted.Length - 1]) };
        }

        private static double P(double[] values, double percentile)
        {
            double index = (values.Length - 1) * percentile; int lower = (int)Math.Floor(index); int upper = (int)Math.Ceiling(index);
            return lower == upper ? values[lower] : values[lower] + ((values[upper] - values[lower]) * (index - lower));
        }

        private static double R(double value) => Math.Round(value, 3);

        private static string BuildCsv(IEnumerable<Sample> samples)
        {
            var text = new StringBuilder("Iteration,Success,Timestamp,ChromePid,NmhPid,NmhFileVersion,CrxVersion,PipeName,BrowserToNmhMs,NmhToReadyMs,BrowserToReadyMs,BrowserToCrxReadyMs,CrxReadinessAttempts,FrameworkPingRttMs,GetVersionRttMs,PingRttMs,FailureStage,Exception\r\n");
            foreach (Sample s in samples) text.AppendLine(string.Join(",", new[] { s.Iteration.ToString(), s.Success.ToString(), Csv(s.Timestamp.ToString("o")), s.ChromePid.ToString(), s.NmhPid.ToString(), Csv(s.NmhFileVersion), Csv(s.CrxVersion), Csv(s.PipeName), R(s.BrowserToNmhMs).ToString(CultureInfo.InvariantCulture), R(s.NmhToReadyMs).ToString(CultureInfo.InvariantCulture), R(s.BrowserToReadyMs).ToString(CultureInfo.InvariantCulture), R(s.BrowserToCrxReadyMs).ToString(CultureInfo.InvariantCulture), s.CrxReadinessAttempts.ToString(), R(s.FrameworkPingRttMs).ToString(CultureInfo.InvariantCulture), R(s.GetVersionRttMs).ToString(CultureInfo.InvariantCulture), R(s.PingRttMs).ToString(CultureInfo.InvariantCulture), Csv(s.FailureStage), Csv(s.Exception) }));
            return text.ToString();
        }

        private static string Csv(string value) => "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";
        private static int ReadInt(string[] args, string name, int fallback) { string value = ReadString(args, name, null); return value == null ? fallback : int.Parse(value, CultureInfo.InvariantCulture); }
        private static string ReadString(string[] args, string name, string fallback) { int i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback; }

        private sealed class ResponseMeasurement { public double ElapsedMs; public JObject Response; }
        private sealed class NmhObservation { public Process Process; public string ExecutablePath; }
        private sealed class Sample
        {
            public int Iteration { get; set; } public bool Success { get; set; } public DateTimeOffset Timestamp { get; set; }
            public int ChromePid { get; set; } public int NmhPid { get; set; } public string NmhFileVersion { get; set; } public string CrxVersion { get; set; } public string PipeName { get; set; }
            public double BrowserToNmhMs { get; set; } public double NmhToReadyMs { get; set; } public double BrowserToReadyMs { get; set; } public double BrowserToCrxReadyMs { get; set; } public int CrxReadinessAttempts { get; set; } public double FrameworkPingRttMs { get; set; } public double GetVersionRttMs { get; set; } public double PingRttMs { get; set; }
            public string GetVersionResponse { get; set; } public string PingResponse { get; set; } public string FailureStage { get; set; } public string Exception { get; set; }
            public string ProbeCommand { get; set; } public double ProbeRttMs { get; set; } public string ProbeResponse { get; set; }
        }
    }
}
