using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using II.RPA.Core.Log;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Rpa.Core.Worker;
using System.Collections.Concurrent;

namespace II.RPA.Core.Talker.Kernel;

/// <summary>
/// Handles the legacy KernelRequest messages still emitted by rpa-engine.
///
/// KernelRequest is intentionally handled on the raw protobuf frame, before the
/// new WorkerFrameCodec maps the frame to the application contracts. This keeps
/// the new protocol contracts independent from the legacy share-bus wire format
/// while preserving the request/reply correlation expected by the engine.
/// </summary>
internal sealed class KernelRequestDispatcher : IDisposable
{
    private readonly ITalkerManager _talkerManager;
    private readonly ILogger _logger;
    private readonly Func<FrameStream, Task> _writeFrame;
    private readonly Func<CancellationToken> _getCancellationToken;
    private readonly ConcurrentDictionary<string, IDisposable> _subscriptions = new(StringComparer.OrdinalIgnoreCase);
    private int _disposed;

    public KernelRequestDispatcher(
        ITalkerManager talkerManager,
        ILogger logger,
        Func<FrameStream, Task> writeFrame,
        Func<CancellationToken> getCancellationToken)
    {
        _talkerManager = talkerManager ?? throw new ArgumentNullException(nameof(talkerManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _writeFrame = writeFrame ?? throw new ArgumentNullException(nameof(writeFrame));
        _getCancellationToken = getCancellationToken ?? throw new ArgumentNullException(nameof(getCancellationToken));
    }

    public bool TryHandle(FrameStream frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        if (Volatile.Read(ref _disposed) != 0 ||
            frame.Type != FrameStream.Types.Type.Request ||
            frame.Payload is null ||
            !frame.Payload.Is(KernelRequest.Descriptor))
        {
            return false;
        }

        var request = frame.Payload.Unpack<KernelRequest>();
        _logger.LogDebug($"收到 KernelRequest:{request.Type};frameId={frame.Id}");
        _ = ProcessAsync(frame, request);
        return true;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        ClearSubscriptions();
    }

    public void ClearSubscriptions()
    {
        foreach (var subscription in _subscriptions.Values)
        {
            subscription.Dispose();
        }
        _subscriptions.Clear();
    }

    private async Task ProcessAsync(FrameStream frame, KernelRequest request)
    {
        var cancellationToken = _getCancellationToken();
        try
        {
            var reply = await HandleAsync(frame, request, cancellationToken).ConfigureAwait(false);
            await _writeFrame(reply).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The worker connection is already gone; there is no reply stream to write to.
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"处理 KernelRequest 失败: type={request.Type}, frameId={frame.Id}", ex);
            try
            {
                await _writeFrame(CreateReply(
                    frame,
                    JsonReply(new
                    {
                        code = "kernel_request_error",
                        message = ex.Message,
                    }),
                    code: 1,
                    message: ex.Message)).ConfigureAwait(false);
            }
            catch (Exception replyException)
            {
                _logger.LogDebug("KernelRequest 错误 Reply 发送失败", replyException);
            }
        }
    }

    private async Task<FrameStream> HandleAsync(
        FrameStream frame,
        KernelRequest request,
        CancellationToken cancellationToken)
    {
        var shareEvent = UnpackShareEvent(request);
        switch (request.Type)
        {
            case KernelRequest.Types.Type.WaitShare:
            {
                var value = await _talkerManager.WaitShareAsync(
                    RequireKey(shareEvent),
                    shareEvent?.Role,
                    shareEvent?.Name,
                    cancellationToken).ConfigureAwait(false);
                return CreateReply(frame, JsonReply(value));
            }

            case KernelRequest.Types.Type.SubscribeBroadcast:
            {
                var subscriptionId = Guid.NewGuid().ToString();
                var subscription = _talkerManager.SubscribeShare(
                    shareEvent?.Key,
                    shareEvent?.Role,
                    shareEvent?.Name,
                    change => _ = SendShareEventAsync(subscriptionId, change));
                if (!_subscriptions.TryAdd(subscriptionId, subscription))
                {
                    subscription.Dispose();
                    throw new InvalidOperationException($"Share subscription [{subscriptionId}] registration failed.");
                }

                // The historical protocol returns the subscription id as a plain string.
                return CreateReply(frame, new StringValue { Value = subscriptionId });
            }

            case KernelRequest.Types.Type.WriteTopShareChannel:
            {
                var value = ParseKeyValueArguments(request);
                _talkerManager.WriteShareChannel(RequireKey(shareEvent), value);
                return CreateReply(frame, JsonReply(value));
            }

            case KernelRequest.Types.Type.ReadTopShareChannel:
                return CreateReply(frame, JsonReply(_talkerManager.ReadShareChannel(RequireKey(shareEvent))));

            case KernelRequest.Types.Type.CountTopShareChannel:
                return CreateReply(frame, new Int32Value
                {
                    Value = _talkerManager.CountShareChannel(RequireKey(shareEvent)),
                });

            case KernelRequest.Types.Type.ReadTopShare:
            {
                _talkerManager.TryGetShare(RequireKey(shareEvent), out var value);
                return CreateReply(frame, JsonReply(value));
            }

            case KernelRequest.Types.Type.WriteTopShare:
            {
                var value = ParseKeyValueArguments(request);
                _talkerManager.SetShare(RequireKey(shareEvent), value);
                return CreateReply(frame, JsonReply(value));
            }

            case KernelRequest.Types.Type.RemoveTopShare:
                return CreateReply(frame, JsonReply(_talkerManager.RemoveShare(RequireKey(shareEvent))));

            case KernelRequest.Types.Type.TopShareKeys:
                return CreateReply(frame, JsonReply(_talkerManager.GetShareKeys()));

            case KernelRequest.Types.Type.ContainTopShare:
                return CreateReply(frame, JsonReply(_talkerManager.ContainsShare(RequireKey(shareEvent))));

            case KernelRequest.Types.Type.StartTalker:
            case KernelRequest.Types.Type.GetTalker:
                throw new NotSupportedException(
                    $"KernelRequest {request.Type} is not available in the new Talker manager.");

            default:
                throw new NotSupportedException($"Unsupported KernelRequest type: {request.Type}");
        }
    }

    private async Task SendShareEventAsync(string subscriptionId, ShareChange change)
    {
        var cancellationToken = _getCancellationToken();
        try
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                return;
            }

            var payload = new ShareEvent
            {
                Id = subscriptionId,
                Key = change.Key,
                Role = change.Role ?? string.Empty,
                Name = change.Name ?? string.Empty,
                Type = change.Type switch
                {
                    ShareChangeType.Set => ShareEvent.Types.Type.Set,
                    ShareChangeType.Unset => ShareEvent.Types.Type.Unset,
                    ShareChangeType.Write => ShareEvent.Types.Type.Write,
                    ShareChangeType.Read => ShareEvent.Types.Type.Read,
                    _ => ShareEvent.Types.Type.Unknown,
                },
            };

            await _writeFrame(new FrameStream
            {
                Id = Guid.NewGuid().ToString(),
                Type = FrameStream.Types.Type.Event,
                Payload = Any.Pack(payload),
            }).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"Share event 发送失败: subscriptionId={subscriptionId}, key={change.Key}", ex);
        }
    }

    private static ShareEvent? UnpackShareEvent(KernelRequest request)
    {
        return request.Payload is not null && request.Payload.Is(ShareEvent.Descriptor)
            ? request.Payload.Unpack<ShareEvent>()
            : null;
    }

    private static string RequireKey(ShareEvent? shareEvent)
    {
        if (string.IsNullOrWhiteSpace(shareEvent?.Key))
        {
            throw new InvalidOperationException("KernelRequest 缺少 ShareEvent.key");
        }

        return shareEvent.Key;
    }

    private static JObject ParseKeyValueArguments(KernelRequest request)
    {
        var value = request.Kvargs?.ToString();
        return string.IsNullOrWhiteSpace(value)
            ? new JObject()
            : JObject.Parse(value);
    }

    private static StringValue JsonReply(object? value)
    {
        var settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
        };
        return new StringValue
        {
            Value = JsonConvert.SerializeObject(value, settings),
        };
    }

    private static FrameStream CreateReply(
        FrameStream request,
        IMessage body,
        int code = 0,
        string message = "")
    {
        return new FrameStream
        {
            Id = Guid.NewGuid().ToString(),
            ReplyId = request.Id,
            Type = FrameStream.Types.Type.Reply,
            Code = code,
            Message = message ?? string.Empty,
            Payload = Any.Pack(new Reply
            {
                Body = Any.Pack(body),
            }),
        };
    }
}
