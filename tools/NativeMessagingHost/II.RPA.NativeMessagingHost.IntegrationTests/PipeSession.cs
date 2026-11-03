using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using II.RPA.IPC.NamedPipe;
using II.RPA.IPC.NamedPipe.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace II.RPA.NativeMessagingHost.IntegrationTests
{
    internal sealed class PipeSession : IDisposable
    {
        private readonly NamedPipeClient client;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<JObject>> pending = new ConcurrentDictionary<string, TaskCompletionSource<JObject>>();
        internal PipeSession(string pipeName)
        {
            client = new NamedPipeClient(pipeName, new NamedPipeClientOptions { ConnectTimeout = TimeSpan.FromSeconds(10) });
            client.MessageReceived += OnMessage;
            client.Disconnected += (s, e) => FailAll(new IOException("Named Pipe disconnected."));
        }
        internal Task ConnectAsync() => client.ConnectAsync(CancellationToken.None);
        internal Task PingAsync() => client.PingAsync(TimeSpan.FromSeconds(5), CancellationToken.None);

        internal async Task<Tuple<JObject, double>> RequestAsync(int clientId, int sequence, int timeout = 30000, int delayMs = -1)
        {
            string id = Guid.NewGuid().ToString("N");
            var completion = new TaskCompletionSource<JObject>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!pending.TryAdd(id, completion)) throw new InvalidOperationException("Duplicate generated MessageId.");
            var sendData = new JObject { ["client"] = clientId, ["sequence"] = sequence };
            if (delayMs >= 0) sendData["delayMs"] = delayMs;
            var message = new JObject { ["MessageId"] = id, ["Action"] = "SendTransferMessage", ["SendData"] = sendData, ["Timeout"] = timeout, ["Timestamp"] = Stopwatch.GetTimestamp(), ["connType"] = "nativemsg_tMsg" };
            long started = Stopwatch.GetTimestamp();
            try
            {
                await client.SendAsync(Encoding.UTF8.GetBytes(message.ToString(Formatting.None)), CancellationToken.None).ConfigureAwait(false);
                if (await Task.WhenAny(completion.Task, Task.Delay(timeout + 5000)).ConfigureAwait(false) != completion.Task) throw new TimeoutException("Response timed out: " + id);
                JObject response = await completion.Task.ConfigureAwait(false);
                if (response["MessageId"]?.ToString() != id) throw new InvalidDataException("Response MessageId mismatch.");
                return Tuple.Create(response, (Stopwatch.GetTimestamp() - started) * 1000d / Stopwatch.Frequency);
            }
            finally { pending.TryRemove(id, out _); }
        }
        private void OnMessage(object sender, NamedPipeMessageReceivedEventArgs args)
        {
            JObject message = JObject.Parse(Encoding.UTF8.GetString(args.Payload));
            string id = message["MessageId"]?.ToString();
            if (id != null && pending.TryGetValue(id, out TaskCompletionSource<JObject> completion)) completion.TrySetResult(message);
        }
        private void FailAll(Exception error) { foreach (TaskCompletionSource<JObject> item in pending.Values) item.TrySetException(error); }
        public void Dispose() { client.Dispose(); FailAll(new ObjectDisposedException(nameof(PipeSession))); }
    }
}
