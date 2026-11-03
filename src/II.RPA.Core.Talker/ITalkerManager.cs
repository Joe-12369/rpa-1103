using Grpc.Core;
using II.RPA.Core.Talker.Talkers;
using Newtonsoft.Json.Linq;
using Rpa.Core.Worker;

namespace II.RPA.Core.Talker
{
    public sealed record ShareChange(
        string Key,
        ShareChangeType Type,
        string Role = "host",
        string Name = "host");

    public enum ShareChangeType
    {
        Unknown = 0,
        Set = 1,
        Unset = 2,
        Write = 3,
        Read = 4,
    }

    public interface ITalkerManager
    {
        void SetShare(string key, JObject value);

        bool TryGetShare(string key, out JObject value);

        Task<JObject> WaitShareAsync(
            string key,
            string? role = null,
            string? name = null,
            CancellationToken cancellationToken = default);

        IDisposable SubscribeShare(
            string? key,
            string? role,
            string? name,
            Action<ShareChange> callback);

        JObject? RemoveShare(string key);

        IReadOnlyCollection<string> GetShareKeys();

        bool ContainsShare(string key);

        void WriteShareChannel(string key, JObject value);

        JObject? ReadShareChannel(string key);

        int CountShareChannel(string key);

        ValueTask<JObject> ReadShareChannelAsync(string key, CancellationToken cancellationToken = default);

        void Initialization();

        Task InitializationAsync(CancellationToken cancellationToken = default);

        (bool, int) CheckZDesktopAllow();

        Task RegisterTalker(IAsyncStreamReader<FrameStream> requestStream, IServerStreamWriter<FrameStream> responseStream, ServerCallContext context);

        T GetTalker<T>() where T : ITalker;

        bool TryGetTalker<T>(out T talker) where T : ITalker;

        IReadOnlyCollection<T> GetTalkers<T>(bool readyOnly = true) where T : ITalker;

        Task<T> WaitTalker<T>(TimeSpan timeout, CancellationToken cancellationToken = default) where T : ITalker;

        Task<T> WaitTalker<T>(Predicate<T> predicate, CancellationToken cancellationToken = default) where T : ITalker;

        void StartChildZBrainProxy();

        void StopChildZBrainProxy();

        void AddTalker(ITalker talker);

        bool RemoveTalker(ITalker talker);

        Task StopAsync();
    }
}
