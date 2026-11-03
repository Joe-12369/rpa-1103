using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using II.RPA.Core.Contracts.Talker;
using II.RPA.Core.Contracts.Talker.Engine;
using II.RPA.Core.Contracts.Talker.RuntimeHost;
using II.RPA.Core.Contracts.Talker.Settings;
using II.RPA.Core.Contracts.Talker.ZBrain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rpa.Core.Worker;
using EngineActionMessage = II.RPA.Core.Contracts.Talker.Engine.ActionMessage;
using WorkerEngine = Rpa.Core.Worker.Engine;
using WorkerSettings = Rpa.Core.Worker.Settings;
using WorkerZBrain = Rpa.Core.Worker.Zbrain;
using WorkerZdesktop = Rpa.Core.Worker.Zdesktop;
using ZdesktopActionMessage = II.RPA.Core.Contracts.Talker.Zdesktop.ActionMessage;
using ZdesktopUserInfoMessage = II.RPA.Core.Contracts.Talker.Zdesktop.Message.UserInfoMessage;

namespace II.RPA.Core.WorkerProtocol.Mapping;

/// <summary>
/// Explicit Worker wire codec for the new Talker implementation. Legacy Kernel
/// and share-bus requests are handled on the raw frame by KernelRequestDispatcher
/// before this codec; this codec maps the remaining application contracts.
/// </summary>
public static class WorkerFrameCodec
{
    public static FrameStream Encode(FrameMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var frame = new FrameStream
        {
            Id = message.Id.ToString(),
            ReplyId = message.ReplyId == Guid.Empty ? string.Empty : message.ReplyId.ToString(),
            Type = (FrameStream.Types.Type)Convert.ToInt32(message.Type),
            Code = message.Code,
            Message = message.Message ?? string.Empty,
        };

        if (message.AnonymousPayload is not null)
        {
            var payload = EncodePayload(message.AnonymousPayload);
            frame.Payload = payload is Any any ? any : Any.Pack(payload);
        }

        return frame;
    }

    public static FrameMessage Decode(FrameStream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        FrameMessage message = stream.Type switch
        {
            FrameStream.Types.Type.Ping => Empty(),
            FrameStream.Types.Type.Request => DecodeRequest(stream),
            FrameStream.Types.Type.Command => DecodeCommand(stream),
            FrameStream.Types.Type.Reply => DecodeReply(stream),
            FrameStream.Types.Type.Event => DecodeEvent(stream),
            FrameStream.Types.Type.Proxy => Empty(),
            _ => throw new RpcException(new Status(
                StatusCode.NotFound,
                $"Unsupported Worker frame type: {stream.Type}")),
        };

        MapEnvelope(stream, message);
        return message;
    }

    private static IMessage EncodePayload(IPayload payload)
    {
        return payload switch
        {
            ZBrainCommand command => Encode(command),
            ZBrainProxyRequest request => request.Proxy,
            ZBrainRequest request => Encode(request),
            Reply<ReplyDict> reply => Encode(reply),
            Reply<string> reply => Encode(reply),
            Reply<int> reply => Encode(reply),
            EngineActionMessage action => Encode(action),
            ActionEvent actionEvent => Encode(actionEvent),
            EngineCommand command => Encode(command),
            ZBrainAppEvent appEvent => Encode(appEvent),
            ZdesktopActionMessage action => Encode(action),
            ZdesktopUserInfoMessage userInfo => Encode(userInfo),
            SettingsMessage settings => Encode(settings),
            SharpRuntimeMessageProxy<Any> proxy => proxy.Payload,
            RunPluginFromClientRequest request => Encode(request),
            PayloadRunFromClient request => Encode(request),
            ClientResponse response => Encode(response),
            IRuntimeMessage runtimeMessage => EncodeRuntimePayload(runtimeMessage),
            _ => throw new InvalidOperationException(
                $"Worker payload type '{payload.GetType().FullName}' is not supported by the new Talker codec."),
        };
    }

    private static IMessage EncodeRuntimePayload(IRuntimeMessage message)
    {
        return message.Payload switch
        {
            IMessage protoMessage => protoMessage,
            IPayload payload => EncodePayload(payload),
            _ => throw new InvalidOperationException(
                $"Runtime payload type '{message.Payload?.GetType().FullName ?? "null"}' is not supported."),
        };
    }

    private static WorkerZBrain.Command Encode(ZBrainCommand command)
    {
        var result = new WorkerZBrain.Command
        {
            Type = (WorkerZBrain.Command.Types.CommandType)Convert.ToInt32(command.Type),
        };
        result.Arguments.AddRange(command.Arguments);
        return result;
    }

    private static WorkerZBrain.Request Encode(ZBrainRequest request)
    {
        var result = new WorkerZBrain.Request
        {
            Method = request.Method ?? string.Empty,
            Path = request.Path ?? string.Empty,
            ContentType = request.ContentType ?? string.Empty,
            Body = request.Body ?? string.Empty,
        };
        result.Headers.Add(request.Headers);
        return result;
    }

    private static Rpa.Core.Worker.Reply Encode(Reply<ReplyDict> reply)
    {
        var body = reply.Body ?? new ReplyDict();
        return WrapReply(new WorkerZBrain.ReplyDict
        {
            Code = body.Code ?? string.Empty,
            Status = body.Status ?? string.Empty,
            Message = body.Message ?? string.Empty,
            Result = body.Result ?? string.Empty,
        });
    }

    private static Rpa.Core.Worker.Reply Encode(Reply<string> reply)
        => WrapReply(new StringValue { Value = reply.Body ?? string.Empty });

    private static Rpa.Core.Worker.Reply Encode(Reply<int> reply)
        => WrapReply(new Int32Value { Value = reply.Body });

    private static Rpa.Core.Worker.Reply WrapReply(IMessage body)
        => new() { Body = Any.Pack(body) };

    private static WorkerEngine.ActionMessage Encode(EngineActionMessage action)
        => new()
        {
            Action = action.Action ?? string.Empty,
            Payload = action.Payload ?? string.Empty,
            WithinZdesktop = action.WithinZdesktop,
            InnerAction = action.InnerAction ?? string.Empty,
        };

    private static WorkerEngine.ActionEvent Encode(ActionEvent actionEvent)
        => new()
        {
            Action = actionEvent.Action ?? string.Empty,
            RequestId = actionEvent.RequestId ?? string.Empty,
            Type = (WorkerEngine.ActionEvent.Types.ActionEventType)Convert.ToInt32(actionEvent.Type),
        };

    private static WorkerEngine.Command Encode(EngineCommand command)
    {
        var result = new WorkerEngine.Command
        {
            Type = (WorkerEngine.Command.Types.CommandType)Convert.ToInt32(command.Type),
        };

        if (command.Argumets is not null)
        {
            result.Args = JsonParser.Default.Parse<ListValue>(
                command.Argumets.ToString(Formatting.None));
        }
        if (command.KeyValueArgumets is not null)
        {
            result.Kvargs = JsonParser.Default.Parse<Struct>(
                command.KeyValueArgumets.ToString(Formatting.None));
        }
        return result;
    }

    private static WorkerZBrain.AppEvent Encode(ZBrainAppEvent appEvent)
        => new()
        {
            Type = (WorkerZBrain.AppEvent.Types.AppEventType)Convert.ToInt32(appEvent.Type),
        };

    private static WorkerZdesktop.ActionMessage Encode(ZdesktopActionMessage action)
        => new()
        {
            Action = action.Action ?? string.Empty,
            Payload = action.Payload ?? string.Empty,
        };

    private static WorkerZdesktop.UserInfoMessage Encode(ZdesktopUserInfoMessage userInfo)
        => new()
        {
            Action = userInfo.Action ?? string.Empty,
            Payload = userInfo.Payload ?? string.Empty,
        };

    private static WorkerSettings.Settings Encode(SettingsMessage message)
    {
        var settings = new WorkerSettings.Settings();
        foreach (var pair in message.Settings)
        {
            if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value is null)
            {
                continue;
            }

            var item = new WorkerSettings.SettingItem { Key = pair.Key };
            switch (pair.Value)
            {
                case string value:
                    item.StringValue = value;
                    break;
                case int value:
                    item.IntValue = value;
                    break;
                case bool value:
                    item.BoolValue = value;
                    break;
                case double value:
                    item.DoubleValue = value;
                    break;
                case float value:
                    item.DoubleValue = value;
                    break;
                case byte[] value:
                    item.BytesValue = ByteString.CopyFrom(value);
                    break;
                case ByteString value:
                    item.BytesValue = value;
                    break;
                default:
                    continue;
            }
            settings.Items.Add(item);
        }
        return settings;
    }

    private static Rpa.Core.Plugin.RunPluginFromClientRequest Encode(RunPluginFromClientRequest request)
        => new()
        {
            ClientId = request.ClientId ?? string.Empty,
            WithinZdesktop = request.WithinZdesktop,
            Payload = Encode(request.PayloadRunFromClient),
        };

    private static Rpa.Core.Plugin.PayloadRunFromClient Encode(PayloadRunFromClient request)
        => new()
        {
            PackagePath = request.PackagePath ?? string.Empty,
            PluginAction = request.PluginAction ?? string.Empty,
            PluginParam = request.PluginParam ?? string.Empty,
            PluginVersion = request.PluginVersion ?? string.Empty,
        };

    private static Rpa.Core.Plugin.ClientResponse Encode(ClientResponse response)
        => new()
        {
            Code = response.Code,
            Message = response.Message ?? string.Empty,
            Result = response.Result ?? string.Empty,
            PluginAction = response.PluginAction ?? string.Empty,
        };

    private static FrameMessage DecodeEvent(FrameStream stream)
    {
        if (stream.Payload is null)
        {
            return Empty();
        }

        if (stream.Payload.Is(WorkerEngine.ActionMessage.Descriptor))
        {
            var action = stream.Payload.Unpack<WorkerEngine.ActionMessage>();
            return Frame(new EngineActionMessage
            {
                Action = action.Action,
                Payload = action.Payload,
                WithinZdesktop = action.WithinZdesktop,
                InnerAction = action.InnerAction,
            });
        }
        if (stream.Payload.Is(WorkerEngine.ActionEvent.Descriptor))
        {
            var actionEvent = stream.Payload.Unpack<WorkerEngine.ActionEvent>();
            return Frame(new ActionEvent
            {
                Type = (ActionEventType)Convert.ToInt32(actionEvent.Type),
                RequestId = actionEvent.RequestId,
                Action = actionEvent.Action,
            });
        }
        if (stream.Payload.Is(WorkerZBrain.AppEvent.Descriptor))
        {
            var appEvent = stream.Payload.Unpack<WorkerZBrain.AppEvent>();
            return Frame(new ZBrainAppEvent
            {
                Type = (ZBrainAppEventType)Convert.ToInt32(appEvent.Type),
            });
        }
        if (stream.Payload.Is(WorkerZdesktop.ActionMessage.Descriptor))
        {
            var action = stream.Payload.Unpack<WorkerZdesktop.ActionMessage>();
            return Frame(new ZdesktopActionMessage
            {
                Action = action.Action,
                Payload = action.Payload,
            });
        }
        if (stream.Payload.Is(WorkerZdesktop.UserInfoMessage.Descriptor))
        {
            var userInfo = stream.Payload.Unpack<WorkerZdesktop.UserInfoMessage>();
            return Frame(new ZdesktopUserInfoMessage
            {
                Action = userInfo.Action,
                Payload = userInfo.Payload,
            });
        }
        if (stream.Payload.Is(WorkerZBrain.Request.Descriptor))
        {
            return Frame(new ZBrainProxyRequest(stream.Payload));
        }
        if (stream.Payload.Is(Rpa.Core.Worker.Reply.Descriptor))
        {
            return DecodeReply(stream);
        }
        if (stream.Payload.Is(Rpa.Core.Worker.Sharpruntime.SharpRuntimeMessage.Descriptor))
        {
            return Frame(new SharpRuntimeMessageProxy<Any>(stream.Payload));
        }
        if (stream.Payload.Is(Rpa.Core.Plugin.RunPluginFromClientRequest.Descriptor))
        {
            return DecodePluginRequest(stream.Payload.Unpack<Rpa.Core.Plugin.RunPluginFromClientRequest>());
        }
        if (stream.Payload.Is(Rpa.Core.Plugin.ClientResponse.Descriptor))
        {
            return DecodePluginResponse(stream.Payload.Unpack<Rpa.Core.Plugin.ClientResponse>());
        }
        if (stream.Payload.Is(WorkerSettings.Settings.Descriptor))
        {
            return Frame(DecodeSettings(stream.Payload.Unpack<WorkerSettings.Settings>()));
        }

        return Empty();
    }

    private static FrameMessage DecodeRequest(FrameStream stream)
    {
        if (stream.Payload is null)
        {
            return Empty();
        }

        if (stream.Payload.Is(WorkerZBrain.Request.Descriptor))
        {
            var request = stream.Payload.Unpack<WorkerZBrain.Request>();
            return Frame(new ZBrainRequest
            {
                Method = request.Method,
                Path = request.Path,
                ContentType = request.ContentType,
                Headers = request.Headers.ToDictionary(pair => pair.Key, pair => pair.Value),
                Body = request.Body,
            });
        }
        if (stream.Payload.Is(Rpa.Core.Plugin.RunPluginFromClientRequest.Descriptor))
        {
            return DecodePluginRequest(stream.Payload.Unpack<Rpa.Core.Plugin.RunPluginFromClientRequest>());
        }

        return Empty();
    }

    private static FrameMessage DecodeCommand(FrameStream stream)
    {
        if (stream.Payload is null)
        {
            return Empty();
        }

        if (stream.Payload.Is(WorkerEngine.Command.Descriptor))
        {
            var command = stream.Payload.Unpack<WorkerEngine.Command>();
            var args = command.Args?.ToString();
            var kvargs = command.Kvargs?.ToString();
            return Frame(new EngineCommand
            {
                Type = (EngineCommandType)Convert.ToInt32(command.Type),
                Argumets = string.IsNullOrWhiteSpace(args) ? null : JArray.Parse(args),
                KeyValueArgumets = string.IsNullOrWhiteSpace(kvargs) ? null : JObject.Parse(kvargs),
            });
        }

        return Empty();
    }

    private static FrameMessage DecodeReply(FrameStream stream)
    {
        if (stream.Payload is null)
        {
            return Empty();
        }
        if (stream.Payload.Is(Rpa.Core.Plugin.ClientResponse.Descriptor))
        {
            return DecodePluginResponse(stream.Payload.Unpack<Rpa.Core.Plugin.ClientResponse>());
        }
        if (!stream.Payload.Is(Rpa.Core.Worker.Reply.Descriptor))
        {
            return Empty();
        }

        var reply = stream.Payload.Unpack<Rpa.Core.Worker.Reply>();
        if (reply.Body is null)
        {
            return Empty();
        }
        if (reply.Body.Is(WorkerZBrain.ReplyDict.Descriptor))
        {
            var body = reply.Body.Unpack<WorkerZBrain.ReplyDict>();
            return Frame(new Reply<ReplyDict>
            {
                Body = new ReplyDict
                {
                    Code = body.Code,
                    Status = body.Status,
                    Message = body.Message,
                    Result = body.Result,
                },
            });
        }
        if (reply.Body.Is(StringValue.Descriptor))
        {
            return Frame(new Reply<string>
            {
                Body = reply.Body.Unpack<StringValue>().Value,
            });
        }
        if (reply.Body.Is(Int32Value.Descriptor))
        {
            return Frame(new Reply<int>
            {
                Body = reply.Body.Unpack<Int32Value>().Value,
            });
        }

        return Empty();
    }

    private static FrameMessage DecodePluginRequest(Rpa.Core.Plugin.RunPluginFromClientRequest request)
    {
        var payload = request.Payload;
        return Frame(new RuntimeMessage<RunPluginFromClientRequest>(
            new RunPluginFromClientRequest
            {
                ClientId = request.ClientId,
                WithinZdesktop = request.WithinZdesktop,
                PayloadRunFromClient = payload is null
                    ? new PayloadRunFromClient()
                    : new PayloadRunFromClient
                    {
                        PackagePath = payload.PackagePath,
                        PluginAction = payload.PluginAction,
                        PluginParam = payload.PluginParam,
                        PluginVersion = payload.PluginVersion,
                    },
            }));
    }

    private static FrameMessage DecodePluginResponse(Rpa.Core.Plugin.ClientResponse response)
        => Frame(new RuntimeMessage<ClientResponse>(new ClientResponse
        {
            Code = response.Code,
            Message = response.Message,
            Result = response.Result,
            PluginAction = response.PluginAction,
        }));

    private static SettingsMessage DecodeSettings(WorkerSettings.Settings settings)
    {
        var message = new SettingsMessage();
        foreach (var item in settings.Items)
        {
            object? value = item.ValueCase switch
            {
                WorkerSettings.SettingItem.ValueOneofCase.StringValue => item.StringValue,
                WorkerSettings.SettingItem.ValueOneofCase.IntValue => item.IntValue,
                WorkerSettings.SettingItem.ValueOneofCase.BoolValue => item.BoolValue,
                WorkerSettings.SettingItem.ValueOneofCase.DoubleValue => item.DoubleValue,
                WorkerSettings.SettingItem.ValueOneofCase.BytesValue => item.BytesValue.ToByteArray(),
                _ => null,
            };
            if (!string.IsNullOrWhiteSpace(item.Key) && value is not null)
            {
                message.Settings[item.Key] = value;
            }
        }
        return message;
    }

    private static FrameMessage<TPayload> Frame<TPayload>(TPayload payload)
        where TPayload : class, IPayload
        => new() { Payload = payload };

    private static FrameMessage Empty() => new();

    private static void MapEnvelope(FrameStream stream, FrameMessage message)
    {
        if (!Guid.TryParse(stream.Id, out var id))
        {
            throw new RpcException(new Status(
                StatusCode.InvalidArgument,
                $"Worker frame id is invalid: '{stream.Id}'."));
        }

        Guid replyId = Guid.Empty;
        if (!string.IsNullOrWhiteSpace(stream.ReplyId) &&
            !Guid.TryParse(stream.ReplyId, out replyId))
        {
            throw new RpcException(new Status(
                StatusCode.InvalidArgument,
                $"Worker frame reply_id is invalid: '{stream.ReplyId}'."));
        }

        message.Id = id;
        message.ReplyId = replyId;
        message.Type = (FrameType)Convert.ToInt32(stream.Type);
        message.Code = stream.Code;
        message.Message = stream.Message ?? string.Empty;
    }
}
