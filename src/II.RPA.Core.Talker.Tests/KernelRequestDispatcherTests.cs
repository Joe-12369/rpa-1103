using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using II.RPA.Core.Log;
using II.RPA.Core.Talker;
using II.RPA.Core.Talker.Kernel;
using II.RPA.Core.Talker.Talkers;
using II.RPA.Logging.Configurations;
using Newtonsoft.Json.Linq;
using Rpa.Core.Worker;
using System.Threading.Channels;
using Xunit;

namespace II.RPA.Core.Talker.Tests;

public sealed class KernelRequestDispatcherTests
{
    [Fact]
    public async Task WaitShareReturnsReplyWithOriginalRequestId()
    {
        var manager = new FakeTalkerManager();
        manager.SetShare("SetServiceInfo", new JObject
        {
            ["proxyInfo"] = new JObject { ["useProxy"] = false },
        });
        var writes = CreateWriteChannel(out var writeFrame);
        using var dispatcher = new KernelRequestDispatcher(
            manager,
            NullLogger.Instance,
            writeFrame,
            () => CancellationToken.None);

        var request = CreateRequest(
            KernelRequest.Types.Type.WaitShare,
            new ShareEvent { Key = "SetServiceInfo", Role = "host" });

        Assert.True(dispatcher.TryHandle(request));
        var reply = await writes.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(1));

        Assert.Equal(FrameStream.Types.Type.Reply, reply.Type);
        Assert.Equal(request.Id, reply.ReplyId);

        var body = reply.Payload.Unpack<Reply>().Body.Unpack<StringValue>();
        var value = JObject.Parse(body.Value);
        Assert.False(value["proxyInfo"]?["useProxy"]?.Value<bool>());
    }

    [Fact]
    public async Task SubscribeShareReturnsIdAndForwardsShareEvent()
    {
        var manager = new FakeTalkerManager();
        var writes = CreateWriteChannel(out var writeFrame);
        using var dispatcher = new KernelRequestDispatcher(
            manager,
            NullLogger.Instance,
            writeFrame,
            () => CancellationToken.None);

        var request = CreateRequest(
            KernelRequest.Types.Type.SubscribeBroadcast,
            new ShareEvent { Key = "setLogLevel", Role = "host" });

        Assert.True(dispatcher.TryHandle(request));
        var subscriptionReply = await writes.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(1));
        var subscriptionId = subscriptionReply.Payload.Unpack<Reply>().Body.Unpack<StringValue>().Value;
        Assert.NotEmpty(subscriptionId);

        manager.SetShare("setLogLevel", new JObject { ["logLevel"] = 0 });
        var eventFrame = await writes.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(1));

        Assert.Equal(FrameStream.Types.Type.Event, eventFrame.Type);
        var shareEvent = eventFrame.Payload.Unpack<ShareEvent>();
        Assert.Equal("setLogLevel", shareEvent.Key);
        Assert.Equal("host", shareEvent.Role);
        Assert.Equal(ShareEvent.Types.Type.Set, shareEvent.Type);
    }

    [Fact]
    public async Task WriteTopShareRoundTripsAsJsonStringReply()
    {
        var manager = new FakeTalkerManager();
        var writes = CreateWriteChannel(out var writeFrame);
        using var dispatcher = new KernelRequestDispatcher(
            manager,
            NullLogger.Instance,
            writeFrame,
            () => CancellationToken.None);

        var request = CreateRequest(KernelRequest.Types.Type.WriteTopShare, new ShareEvent { Key = "answer" });
        var kernelRequest = request.Payload.Unpack<KernelRequest>();
        kernelRequest.Kvargs = JsonParser.Default.Parse<Struct>("{\"value\":42}");
        request.Payload = Any.Pack(kernelRequest);

        Assert.True(dispatcher.TryHandle(request));
        var reply = await writes.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(1));
        var body = reply.Payload.Unpack<Reply>().Body.Unpack<StringValue>();

        Assert.Equal(42, JObject.Parse(body.Value)["value"]?.Value<int>());
        Assert.True(manager.ContainsShare("answer"));
    }

    [Fact]
    public async Task UnsupportedKernelRequestReturnsErrorInsteadOfHanging()
    {
        var manager = new FakeTalkerManager();
        var writes = CreateWriteChannel(out var writeFrame);
        using var dispatcher = new KernelRequestDispatcher(
            manager,
            NullLogger.Instance,
            writeFrame,
            () => CancellationToken.None);

        var request = CreateRequest(KernelRequest.Types.Type.StartTalker, null);

        Assert.True(dispatcher.TryHandle(request));
        var reply = await writes.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(1));

        Assert.Equal(FrameStream.Types.Type.Reply, reply.Type);
        Assert.Equal(request.Id, reply.ReplyId);
        Assert.NotEqual(0, reply.Code);
    }

    private static ChannelReader<FrameStream> CreateWriteChannel(
        out Func<FrameStream, Task> writeFrame)
    {
        var channel = System.Threading.Channels.Channel.CreateUnbounded<FrameStream>();
        writeFrame = frame =>
        {
            channel.Writer.TryWrite(frame);
            return Task.CompletedTask;
        };
        return channel.Reader;
    }

    private static FrameStream CreateRequest(KernelRequest.Types.Type type, ShareEvent? shareEvent)
    {
        var request = new KernelRequest { Type = type };
        if (shareEvent != null)
        {
            request.Payload = Any.Pack(shareEvent);
        }

        return new FrameStream
        {
            Id = Guid.NewGuid().ToString(),
            Type = FrameStream.Types.Type.Request,
            Payload = Any.Pack(request),
        };
    }

    private sealed class FakeTalkerManager : ITalkerManager
    {
        private readonly Dictionary<string, JObject> _shares = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<Guid, (string? Key, string? Role, string? Name, Action<ShareChange> Callback)> _subscriptions = new();
        private readonly Dictionary<string, Queue<JObject>> _channels = new(StringComparer.OrdinalIgnoreCase);

        public void SetShare(string key, JObject value)
        {
            _shares[key] = value;
            var change = new ShareChange(key, ShareChangeType.Set);
            foreach (var subscription in _subscriptions.Values.ToArray())
            {
                if ((string.IsNullOrWhiteSpace(subscription.Key) || string.Equals(subscription.Key, key, StringComparison.OrdinalIgnoreCase)) &&
                    (string.IsNullOrWhiteSpace(subscription.Role) || string.Equals(subscription.Role, change.Role, StringComparison.OrdinalIgnoreCase)) &&
                    (string.IsNullOrWhiteSpace(subscription.Name) || string.Equals(subscription.Name, change.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    subscription.Callback(change);
                }
            }
        }

        public bool TryGetShare(string key, out JObject value) => _shares.TryGetValue(key, out value!);

        public Task<JObject> WaitShareAsync(string key, string? role = null, string? name = null, CancellationToken cancellationToken = default)
            => Task.FromResult(_shares[key]);

        public IDisposable SubscribeShare(string? key, string? role, string? name, Action<ShareChange> callback)
        {
            var id = Guid.NewGuid();
            _subscriptions[id] = (key, role, name, callback);
            return new DelegateDisposable(() => _subscriptions.Remove(id));
        }

        public JObject? RemoveShare(string key) => _shares.Remove(key, out var value) ? value : null;

        public IReadOnlyCollection<string> GetShareKeys() => _shares.Keys.ToArray();

        public bool ContainsShare(string key) => _shares.ContainsKey(key);

        public void WriteShareChannel(string key, JObject value)
        {
            if (!_channels.TryGetValue(key, out var channel))
            {
                _channels[key] = channel = new Queue<JObject>();
            }
            channel.Enqueue(value);
        }

        public JObject? ReadShareChannel(string key)
            => _channels.TryGetValue(key, out var channel) && channel.Count > 0 ? channel.Dequeue() : null;

        public int CountShareChannel(string key)
            => _channels.TryGetValue(key, out var channel) ? channel.Count : 0;

        public ValueTask<JObject> ReadShareChannelAsync(string key, CancellationToken cancellationToken = default)
            => new(ReadShareChannel(key)!);

        public void Initialization() { }
        public Task InitializationAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public (bool, int) CheckZDesktopAllow() => (true, 0);
        public Task RegisterTalker(IAsyncStreamReader<FrameStream> requestStream, IServerStreamWriter<FrameStream> responseStream, ServerCallContext context) => Task.CompletedTask;
        public T GetTalker<T>() where T : ITalker => throw new NotSupportedException();
        public bool TryGetTalker<T>(out T talker) where T : ITalker { talker = default!; return false; }
        public IReadOnlyCollection<T> GetTalkers<T>(bool readyOnly = true) where T : ITalker => Array.Empty<T>();
        public Task<T> WaitTalker<T>(TimeSpan timeout, CancellationToken cancellationToken = default) where T : ITalker => throw new NotSupportedException();
        public Task<T> WaitTalker<T>(Predicate<T> predicate, CancellationToken cancellationToken = default) where T : ITalker => throw new NotSupportedException();
        public void StartChildZBrainProxy() { }
        public void StopChildZBrainProxy() { }
        public void AddTalker(ITalker talker) { }
        public bool RemoveTalker(ITalker talker) => false;
        public Task StopAsync() => Task.CompletedTask;

        private sealed class DelegateDisposable : IDisposable
        {
            private readonly Action _dispose;
            private int _disposed;

            public DelegateDisposable(Action dispose) => _dispose = dispose;

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) == 0)
                {
                    _dispose();
                }
            }
        }
    }
}

internal sealed class NullLogger : ILogger
{
    public static NullLogger Instance { get; } = new();

    public void LogTrace(string aMessage, string callerMemberName = "", string aCallerFilePath = "") { }
    public void LogTrace(string aMessage, Exception anException, string callerMemberName = "", string aCallerFilePath = "") { }
    public void LogDebug(string aMessage, string callerMemberName = "", string aCallerFilePath = "") { }
    public void LogDebug(string aMessage, Exception anException, string callerMemberName = "", string aCallerFilePath = "") { }
    public void LogInfo(string aMessage, string callerMemberName = "", string aCallerFilePath = "") { }
    public void LogInfo(string aMessage, Exception anException, string callerMemberName = "", string aCallerFilePath = "") { }
    public void LogWarning(string aMessage, string callerMemberName = "", string aCallerFilePath = "") { }
    public void LogWarning(string aMessage, Exception anException, string callerMemberName = "", string aCallerFilePath = "") { }
    public void LogError(string aMessage, string callerMemberName = "", string aCallerFilePath = "") { }
    public void LogError(string aMessage, Exception anException, string callerMemberName = "", string aCallerFilePath = "") { }
    public void LogError(Exception anException, string callerMemberName = "", string aCallerFilePath = "") { }
    public void LogFatal(string aMessage, string callerMemberName = "", string aCallerFilePath = "") { }
    public void LogFatal(string aMessage, Exception anException, string callerMemberName = "", string aCallerFilePath = "") { }
    public void LogFatal(Exception anException, string callerMemberName = "", string aCallerFilePath = "") { }
    public void LogCommunication(string aMessage, Exception anException, StreamScope aStreamScope, string aCallerFilePath = "") { }
    public void LogCommunication(string aMessage, StreamScope aStreamScope, string aCallerFilePath = "") { }
    public void LogFFmpeg(string aMessage, string aCallerFilePath = "") { }
    public void LogZBrain(string aMessage, string aCallerFilePath = "") { }
    public void LogExecutor(string aMessage, string aCallerFilePath = "") { }
    public void LogInterpreter(string aMessage, string aCallerFilePath = "") { }
    public void LogZdesktop(string aMessage, string aCallerFilePath = "") { }
    public void LogMCPAgent(string aMessage, string aCallerFilePath = "") { }
}
