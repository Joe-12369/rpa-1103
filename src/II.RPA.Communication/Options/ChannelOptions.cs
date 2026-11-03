using Grpc.Core;

namespace II.RPA.Communication.Options;

public static class ChannelOptions
{
    public static IReadOnlyList<ChannelOption> DefaultServerOptions { get; } =
    [
        new(Grpc.Core.ChannelOptions.MaxReceiveMessageLength, int.MaxValue),
        new(Grpc.Core.ChannelOptions.MaxSendMessageLength, int.MaxValue),
        new("grpc.http2.write_buffer_size", 67_108_864),
        new("grpc.minimal_stack", 1),
        new("grpc.per_message_compression", 0)
    ];

    public static IReadOnlyList<ChannelOption> DefaultClientOptions { get; } =
    [
        new(Grpc.Core.ChannelOptions.MaxReceiveMessageLength, int.MaxValue),
        new(Grpc.Core.ChannelOptions.MaxSendMessageLength, int.MaxValue),
        new("grpc.keepalive_permit_without_calls", 1),
        new("grpc.http2.max_pings_without_data", 0),
        new("grpc.max_reconnect_backoff_ms", 1_000),
        new("grpc.min_reconnect_backoff_ms", 300),
        new("grpc.initial_reconnect_backoff_ms", 300),
        new("grpc.enable_http_proxy", 0),
        new("grpc.max_concurrent_streams", 300),
        new("grpc.http2.write_buffer_size", 67_108_864),
        new("grpc.minimal_stack", 1)
    ];
}
