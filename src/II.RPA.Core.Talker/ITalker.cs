using Grpc.Core;
using Rpa.Core.Worker;
using II.RPA.Core.Talker.Models;

namespace II.RPA.Core.Talker
{
    public interface ITalker
    {
        bool IsReady { get; }

        TalkerLifecycleState LifecycleState { get; }

        DateTimeOffset StateChangedAt { get; }

        Exception? LastFault { get; }

        string Name { get; }

        int Pid { get; }

        string Id { get; }

        Task Start();

        Task Stop();

        Task RegisterTalker(IAsyncStreamReader<FrameStream> requestStream, IServerStreamWriter<FrameStream> responseStream, ServerCallContext context);
    }
}
