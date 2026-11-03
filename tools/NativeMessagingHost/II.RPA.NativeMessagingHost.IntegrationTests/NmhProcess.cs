using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace II.RPA.NativeMessagingHost.IntegrationTests
{
    internal sealed class NmhProcess : IDisposable
    {
        internal const string ServiceKey = "U2FsdGVkX19Xagmra6VZ0BB2FitqhBx8";
        private readonly Process process;
        private readonly BinaryReader stdout;
        private readonly BinaryWriter stdin;
        private readonly SemaphoreSlim writeLock = new SemaphoreSlim(1, 1);
        private readonly CancellationTokenSource stop = new CancellationTokenSource();
        private readonly Task crxLoop;
        private readonly Task<string> stderrTask;
        private readonly string serviceKey;
        private readonly string temporaryDirectory;
        private readonly ConcurrentDictionary<int, Task> responses = new ConcurrentDictionary<int, Task>();
        private int nextResponse;
        private int activeResponses;
        private int maxActiveResponses;
        private int disposed;

        private NmhProcess(Process value, string key, string tempDirectory)
        {
            process = value;
            serviceKey = key;
            temporaryDirectory = tempDirectory;
            stdout = new BinaryReader(value.StandardOutput.BaseStream, Encoding.UTF8, true);
            stdin = new BinaryWriter(value.StandardInput.BaseStream, Encoding.UTF8, true);
            stderrTask = value.StandardError.ReadToEndAsync();
            crxLoop = Task.Run(() => RunCrxLoop(stop.Token));
        }

        internal int Id => process.Id;
        internal string PipeName => serviceKey + "_" + Id;
        internal int MaxActiveResponses => Volatile.Read(ref maxActiveResponses);

        internal void EnsureRunning()
        {
            if (!process.HasExited) return;
            string error = stderrTask.IsCompleted ? stderrTask.GetAwaiter().GetResult() : string.Empty;
            throw new InvalidOperationException("NMH exited before Named Pipe connection. ExitCode=" + process.ExitCode + "; stderr=" + error);
        }

        internal static NmhProcess Start(string customServiceKey = null)
        {
            string executable = FindHostExecutable();
            string tempDirectory = null;
            string key = customServiceKey ?? ServiceKey;
            if (customServiceKey != null)
            {
                tempDirectory = Path.Combine(Path.GetTempPath(), "ii-rpa-nmh-" + Guid.NewGuid().ToString("N"));
                CopyDirectory(Path.GetDirectoryName(executable), tempDirectory);
                executable = Path.Combine(tempDirectory, Path.GetFileName(executable));
                string configPath = executable + ".config";
                string config = File.ReadAllText(configPath);
                config = config.Replace("value=\"" + ServiceKey + "\"", "value=\"" + customServiceKey + "\"");
                File.WriteAllText(configPath, config);
            }
            var info = new ProcessStartInfo(executable) { WorkingDirectory = Path.GetDirectoryName(executable), UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
            return new NmhProcess(Process.Start(info) ?? throw new InvalidOperationException("Failed to start NMH."), key, tempDirectory);
        }

        internal async Task SendExitFrameAsync()
        {
            await writeLock.WaitAsync().ConfigureAwait(false);
            try { stdin.Write(0); stdin.Flush(); } finally { writeLock.Release(); }
        }

        internal async Task<int> WaitForExitAsync(TimeSpan timeout)
        {
            Task wait = Task.Run(() => process.WaitForExit());
            if (await Task.WhenAny(wait, Task.Delay(timeout)).ConfigureAwait(false) != wait) throw new TimeoutException("NMH did not exit.");
            return process.ExitCode;
        }

        private void RunCrxLoop(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    int length = stdout.ReadInt32();
                    if (length <= 0 || length > 1024 * 1024) throw new InvalidDataException("Invalid NMH stdout frame: " + length);
                    byte[] bytes = stdout.ReadBytes(length);
                    if (bytes.Length != length) throw new EndOfStreamException();
                    JObject request = JObject.Parse(Encoding.UTF8.GetString(bytes));
                    int responseId = Interlocked.Increment(ref nextResponse);
                    Task responseTask = RespondAsync(request, token);
                    responses.TryAdd(responseId, responseTask);
                    _ = responseTask.ContinueWith(completed => responses.TryRemove(responseId, out Task ignored), TaskScheduler.Default);
                }
            }
            catch (Exception) when (token.IsCancellationRequested || process.HasExited) { }
            catch (EndOfStreamException) { }
        }

        private async Task RespondAsync(JObject request, CancellationToken token)
        {
            int active = Interlocked.Increment(ref activeResponses);
            int observed;
            while (active > (observed = Volatile.Read(ref maxActiveResponses)))
                if (Interlocked.CompareExchange(ref maxActiveResponses, active, observed) == observed) break;
            try
            {
                int delay = request["SendData"]?["delayMs"]?.Value<int>() ?? (request["SendData"]?["sequence"]?.Value<int>() % 7 ?? 0);
                if (delay > 0) await Task.Delay(delay, token).ConfigureAwait(false);
                JObject response = (JObject)request.DeepClone();
                string method = request["SendData"]?["Method"]?.ToString();
                JToken content = method == "getVersion" ? JToken.FromObject("4.0.1.204") : request["SendData"]?.DeepClone() ?? JValue.CreateNull();
                response["CallbackReslut"] = new JObject { ["code"] = 200, ["status"] = "success", ["result"] = new JObject { ["content"] = content, ["error"] = JValue.CreateNull() } };
                Interlocked.Decrement(ref activeResponses);
                active = 0;
                await WriteAsync(response.ToString(Formatting.None)).ConfigureAwait(false);
            }
            finally { if (active != 0) Interlocked.Decrement(ref activeResponses); }
        }

        private async Task WriteAsync(string json)
        {
            byte[] payload = Encoding.UTF8.GetBytes(json);
            await writeLock.WaitAsync().ConfigureAwait(false);
            try { stdin.Write(payload.Length); stdin.Write(payload); stdin.Flush(); } finally { writeLock.Release(); }
        }

        private static string FindHostExecutable()
        {
            DirectoryInfo directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, ".git"))) directory = directory.Parent;
            if (directory == null) throw new DirectoryNotFoundException("Repository root was not found.");
            string path = Path.Combine(directory.FullName, "tools", "NativeMessagingHost", "II.RPA.NativeMessagingHost", "bin", "II.RPA.NativeMessagingHost", "II.RPA.NativeMessagingHost.exe");
            if (!File.Exists(path)) throw new FileNotFoundException("Build NativeMessagingHost.sln first.", path);
            return path;
        }

        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (string file in Directory.GetFiles(source)) File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0) return;
            stop.Cancel();
            try { stdin.Dispose(); } catch { }
            if (!process.HasExited) { try { process.Kill(); process.WaitForExit(5000); } catch { } }
            try { crxLoop.GetAwaiter().GetResult(); } catch { }
            try { Task.WaitAll(responses.Values.ToArray(), 5000); } catch { }
            stdout.Dispose(); writeLock.Dispose(); stop.Dispose(); process.Dispose();
            if (temporaryDirectory != null) { try { Directory.Delete(temporaryDirectory, true); } catch { } }
        }
    }
}
