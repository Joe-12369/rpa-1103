using II.RPA.Core.Contracts.Talker;
using Rpa.Core.Worker;

namespace II.RPA.Core.WorkerProtocol.Mapping;

public static class WorkerFrameExtensions
{
    public static FrameStream ToWorkerStream(this FrameMessage message)
        => WorkerFrameCodec.Encode(message);

    public static FrameMessage ToFrameMessage(this FrameStream stream)
        => WorkerFrameCodec.Decode(stream);
}
