using Grpc.Core;
using II.RPA.Logging;
using II.RPA.Plugin.Core.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Rpa.Core.Worker;
using Rpa.Core.Worker.Sharpruntime;
using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.RuntimeHost.Services
{
    public class WorkerConnection : IDisposable, IRuntimeWorkerService
    {
        private static readonly ILogger _logger = Logger.OfContext();
        private readonly AsyncDuplexStreamingCall<FrameStream, FrameStream> _streamingCall;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
        private readonly TaskCompletionSource<bool> _completion =
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task Completion => _completion.Task;
        public readonly IObservable<SharpRuntimeMessage> _incoming;
        public readonly IObserver<SharpRuntimeMessage> _outgoing;
        public WorkerConnection(AsyncDuplexStreamingCall<FrameStream, FrameStream> streamingCall)
        {
            _streamingCall = streamingCall;
            _incoming = Observable.Create<SharpRuntimeMessage>(observer =>
            {
                _logger.LogDebug("Start reading from gRPC response stream");
                var responseStream = _streamingCall.ResponseStream;

                var readTask = Task.Run(async () =>
                {
                    try
                    {
                        while (await responseStream.MoveNext(_cts.Token).ConfigureAwait(false))
                        {
                            var response = responseStream.Current;

                            if (response.Payload.Is(SharpRuntimeMessage.Descriptor))
                            {
                                var message = response.Payload.Unpack<SharpRuntimeMessage>();
                                observer.OnNext(message);
                            }
                            else
                            {
                                _logger.LogWarning("Unknown payload type");
                            }
                        }
                        observer.OnCompleted();
                    }
                    catch (RpcException rpcEx) when (rpcEx.StatusCode == StatusCode.Unknown)
                    {
                        string debugMsg = rpcEx.InnerException?.ToString();
                        if (!string.IsNullOrEmpty(debugMsg))
                        {
                            try
                            {
                                var msgMatch = Regex.Match(
                                     debugMsg,
                                     @"""grpc_message""\s*:\s*""(.*?)""",
                                     RegexOptions.Singleline,
                                     TimeSpan.FromSeconds(1)
                                 );
                                string grpcMessage = msgMatch.Success ? msgMatch.Groups[1].Value : null;

                                var statusMatch = Regex.Match(
                                    debugMsg,
                                    @"""grpc_status""\s*:\s*(\d+)",
                                    RegexOptions.Singleline,
                                    TimeSpan.FromSeconds(1)
                                );
                                int grpcStatus = statusMatch.Success ? int.Parse(statusMatch.Groups[1].Value) : -1;

                                if (grpcStatus == 2 && grpcMessage == "Stream removed")
                                {
                                    _logger.LogWarning("grpc server exited!");
                                }
                                else
                                {
                                    _logger.LogWarning($"grpc_exception: {grpcMessage};{grpcStatus}");
                                }
                            }
                            catch
                            {
                                _logger.LogWarning(debugMsg,"parse failed!");
                            }

                        }
                        else
                        {
                            Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(_logger, rpcEx, "unknow grpc error");
                        }
                        observer.OnError(rpcEx);
                    }
                    catch (Exception ex)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, "Error reading from gRPC stream");
                        observer.OnError(ex);
                    }
                    finally
                    {
                        _completion.TrySetResult(true);
                    }
                });

                return Disposable.Create(() =>
                {
                    _logger.LogDebug("Disposing gRPC response stream");
                    _cts.Cancel();
                    _streamingCall.Dispose();
                });
            })
            .Publish()
            .RefCount();

            _outgoing = Observer.Create<SharpRuntimeMessage>(
                message => SendWithRetry(message),
                ex => Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, "Outgoing observer error"),
                async () =>
                {
                    try
                    {
                        await _streamingCall.RequestStream.CompleteAsync();
                        _logger.LogInformation("Request stream completed");
                    }
                    catch (Exception ex)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, "Error completing request stream");
                    }
                });
        }


        public IObserver<SharpRuntimeMessage> Outgoing => _outgoing;

        public IObservable<RuntimeMessageModel> MessageStream => _incoming.Select(MapToModel);

        private async void SendWithRetry(SharpRuntimeMessage message)
        {
            const int maxRetries = 5;
            var delay = TimeSpan.FromMilliseconds(200);
            var attempt = 0;

            while (true)
            {
                try
                {
                    await _sendLock.WaitAsync(_cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                try
                {
                    var frame = new FrameStream
                    {
                        Id = Guid.NewGuid().ToString(),
                        Type = FrameStream.Types.Type.Event,
                        Payload = Google.Protobuf.WellKnownTypes.Any.Pack(message),
                    };

                    _logger.LogDebug($"Sending runtimemessage: {message.RequestId};{message.Action}");
                    await _streamingCall.RequestStream.WriteAsync(frame).ConfigureAwait(false);
                    return;
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Connection was canceled, stopping retries.");
                    return;
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    attempt++;
                    Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(_logger, ex, $"Send error (attempt {attempt}), retrying...");
                    try
                    {
                        await Task.Delay(delay, _cts.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogWarning("Connection was canceled, stopping retries.");
                        return;
                    }
                    delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * 2, 1000));
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, "Send failed after retries");
                    return;
                }
                finally
                {
                    _sendLock.Release();
                }
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _streamingCall.Dispose();
            _completion.TrySetResult(true);
        }

        public Task SendMessageAsync(RuntimeMessageModel message)
        {
            var grpcMessage = MapToGrpc(message);
            Outgoing.OnNext(grpcMessage);
            return Task.CompletedTask;
        }
        public void SendMessage(RuntimeMessageModel message)
        {
            var grpcMessage = MapToGrpc(message);
            Outgoing.OnNext(grpcMessage);
        }
        private RuntimeMessageModel MapToModel(SharpRuntimeMessage message)
        {
            return new RuntimeMessageModel
            {
                Action = message.Action,
                Payload = message.Payload,
                RequestId = message.RequestId,
            };
        }

        private SharpRuntimeMessage MapToGrpc(RuntimeMessageModel model)
        {
            return new SharpRuntimeMessage
            {
                Action = model.Action,
                Payload = model.Payload,
                RequestId = model.RequestId,
            };
        }
    }
}

