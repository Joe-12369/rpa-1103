using Rpa.Core.Worker;

namespace II.RPA.Core.Talker.Talkers;

internal static class RunTrace
{
    public static bool IsEngineAction(FrameStream frame)
    {
        var typeUrl = frame?.Payload?.TypeUrl;
        return !string.IsNullOrWhiteSpace(typeUrl) &&
            typeUrl.EndsWith("/ActionMessage", StringComparison.Ordinal);
    }

    public static string FormatFrame(FrameStream frame)
    {
        return $"frameId={frame.Id} frameType={frame.Type} payloadType={frame.Payload?.TypeUrl ?? "<none>"}";
    }
}
