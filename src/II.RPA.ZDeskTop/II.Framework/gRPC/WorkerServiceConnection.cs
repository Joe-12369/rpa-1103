using Grpc.Core;
using II.Framework.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Rpa.Core.Worker;
using Rpa.Core.Worker.Zdesktop;
using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace II.Framework.gRPC
{
    public class WorkerConnection : IDisposable
    {
        private readonly SemaphoreSlim sendLock = new(1, 1);
        private readonly SemaphoreSlim receiveLock = new(1, 1);
        private readonly CancellationTokenSource cts = new();
        private readonly ILogger  _logger;
        private readonly Subject<ZdesktopActionMessage> _streamsSubject;
        private readonly Subject<string> _engineSubject;
        public WorkerConnection(IServiceProvider serviceProvider) 
        {
            _logger = serviceProvider.GetRequiredService<ILogger<WorkerConnection>>();
            _streamsSubject = new Subject<ZdesktopActionMessage>();
            _engineSubject = new Subject<string>();
        }

        public AsyncDuplexStreamingCall<FrameStream, FrameStream> Streamings
        {
            get;
            set;
        }
        public Metadata Meta
        {
            get;
            set;
        }
        public Exception Exception
        {
            get;
            set;
        }
        public CancellationToken Token => cts.Token;
        public async Task Connect()
        {
            var responseStream = Streamings.ResponseStream;
            await Task.Factory.StartNew(async () =>
            {
                while (await responseStream.MoveNext())
                {
                    var response = responseStream.Current;
                    if (response.Payload.Is(ActionMessage.Descriptor))
                    {
                        try
                        {
                            var message = response.Payload.Unpack<ActionMessage>();
                            var zdesktopMessage = JsonConvert.DeserializeObject<ZdesktopActionMessage>(message.Payload);
                            zdesktopMessage.Payload = JsonConvert.SerializeObject(zdesktopMessage.PayloadObj);
                            _logger.LogDebug($"OnNext zdesktopMessage:{JsonConvert.SerializeObject(zdesktopMessage)}");
                            _streamsSubject.OnNext(zdesktopMessage);
                        }
                        catch (Exception ex)
                        {
                            _streamsSubject.OnError(ex);
                        }
                    }
                    else if (response.Payload.Is(Rpa.Core.Worker.Engine.ActionMessage.Descriptor))
                    {
                        try
                        {
                            var message = response.Payload.Unpack<Rpa.Core.Worker.Engine.ActionMessage>();
                            _logger.LogDebug($"Receive engine ActionMessage:{message.Payload}");
                            _engineSubject.OnNext(message.Action);
                        }
                        catch (Exception ex)
                        {
                            _engineSubject.OnError(ex);
                        }
                    }
                }
                _streamsSubject.OnCompleted();
                _engineSubject.OnCompleted();
            },TaskCreationOptions.LongRunning);

        }
        public IObservable<ZdesktopActionMessage> ServerStream
        {
            get => _streamsSubject;
        }
        public IObservable<string> EngineStream
        {
            get => _engineSubject;
        }
        public async Task Send(ZdesktopActionMessage message)
        {
            try
            {
                var frame = new FrameStream
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = FrameStream.Types.Type.Event,
                    ReplyId = string.Empty,
                    Code = 0,
                    Message = string.Empty,
                    Payload = Google.Protobuf.WellKnownTypes.Any.Pack(new ActionMessage
                    {
                        Action = message.Action,
                        Payload = message.Payload,
                    }),
                };
                await sendLock.WaitAsync(Token);
                _logger.LogDebug($"Send ZdesktopActionMessage:{message.Payload}");
                await Streamings!.RequestStream.WriteAsync(frame);
            }
            catch (Exception ex)
            {
                _logger.LogError(new EventId(1), ex, "RequestStream write error");
            }
            finally
            {
                sendLock.Release();
            }
        }
        public async Task SendWithRetry(ZdesktopActionMessage message)
        {
            const int maxRetries = 5;
            var delay = TimeSpan.FromMilliseconds(200);
            var attempt = 0;

            while (true)
            {
                await sendLock.WaitAsync(Token).ConfigureAwait(false);
                try
                {
                    var frame = new FrameStream
                    {
                        Id = Guid.NewGuid().ToString(),
                        Type = FrameStream.Types.Type.Event,
                        ReplyId = string.Empty,
                        Code = 0,
                        Message = string.Empty,
                        Payload = Google.Protobuf.WellKnownTypes.Any.Pack(new ActionMessage
                        {
                            Action = message.Action,
                            Payload = message.Payload,
                        }),
                    };

                    _logger.LogDebug($"Send ZdesktopActionMessage: {message.Payload}");
                    await Streamings!.RequestStream.WriteAsync(frame).ConfigureAwait(false);
                    return;
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    attempt++;
                    Microsoft.Extensions.Logging.LoggerExtensions.LogError(_logger, ex, $"Send error (attempt {attempt})");

                    await Task.Delay(delay, Token).ConfigureAwait(false);
                    delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * 2, 1000));
                }
                finally
                {
                    sendLock.Release();
                }
            }
        }


        public IObservable<ZdesktopActionMessage> GetResponseStreamObservable()
        {
            var responseStream = Streamings.ResponseStream;

            return Observable.Create<ZdesktopActionMessage>(observer =>
            {
                _logger.LogDebug($"FromServer:new obsever");
                var readTask = Task.Run(async () =>
                {
                    try
                    {
                        while (await responseStream.MoveNext())
                        {
                            var response = responseStream.Current;
                            var message = response.Payload.Unpack<ActionMessage>();
                            _logger.LogDebug($"Receive ActionMessage:{JsonConvert.SerializeObject(message)}");
                            var zdesktopMessage = JsonConvert.DeserializeObject<ZdesktopActionMessage>(message.Payload);
                            zdesktopMessage.Payload = JsonConvert.SerializeObject(zdesktopMessage.PayloadObj);
                            _logger.LogDebug($"OnNext zdesktopMessage:{JsonConvert.SerializeObject(zdesktopMessage)}");
                            observer.OnNext(zdesktopMessage);
                        }
                        observer.OnCompleted();
                    }
                    catch (Exception ex)
                    {
                        observer.OnError(ex);
                    }
                });
                return readTask;
            });
        }
        public IObservable<ZdesktopActionMessage> FromServer()
        {
            return CreateServerStream();
        }
        private IObservable<ZdesktopActionMessage> CreateServerStream()
        {
            return Observable.Create<ZdesktopActionMessage>((observer) =>
            {
                var responseStream = Streamings.ResponseStream;
                _logger.LogDebug($"FromServer:new obsever");
                async Task ReadStream()
                {
                    try
                    {
                        async Task<bool> ReadNext()
                        {
                            await receiveLock.WaitAsync();
                            try
                            {
                                return await responseStream.MoveNext(CancellationToken.None);
                            }
                            finally
                            {
                                receiveLock.Release();
                            }
                        }

                        while (await ReadNext())
                        {
                            var response = responseStream.Current;
                            var message = response.Payload.Unpack<ActionMessage>();
                            _logger.LogDebug($"Receive ActionMessage:{JsonConvert.SerializeObject(message)}");
                            var zdesktopMessage = JsonConvert.DeserializeObject<ZdesktopActionMessage>(message.Payload);
                            zdesktopMessage.Payload = JsonConvert.SerializeObject(zdesktopMessage.PayloadObj);
                            _logger.LogDebug($"OnNext zdesktopMessage:{JsonConvert.SerializeObject(zdesktopMessage)}");
                            observer.OnNext(zdesktopMessage);
                        }
                        observer.OnCompleted();
                    }
                    catch (Exception ex)
                    {
                        observer.OnError(ex);
                    }
                }

                Task.Run(ReadStream);

                return Disposable.Create(() =>
                {
                    Streamings.Dispose();
                });
            });
        }

        public IObservable<ZdesktopActionMessage> FromServerOld()
        {
            return Observable.Create<ZdesktopActionMessage>(async (observer, cancellationToken) =>
            {
                try
                {
                    var responseStream = Streamings.ResponseStream;
                    _logger.LogDebug($"FromServer");
                    async Task<bool> ReadNext()
                    {
                        await receiveLock.WaitAsync();
                        try
                        {
                            return await responseStream.MoveNext(cancellationToken);
                        }
                        finally
                        {
                            receiveLock.Release();
                        }
                    }
                    while (await ReadNext())
                    {
                        var response = responseStream.Current;
                        var message = response.Payload.Unpack<ActionMessage>();
                        _logger.LogDebug($"Receive ActionMessage:{JsonConvert.SerializeObject(message)}");
                        var zdesktopMessage = JsonConvert.DeserializeObject<ZdesktopActionMessage>(message.Payload);
                        zdesktopMessage.Payload = JsonConvert.SerializeObject(zdesktopMessage.PayloadObj);
                        _logger.LogDebug($"OnNext zdesktopMessage:{JsonConvert.SerializeObject(zdesktopMessage)}");
                        observer.OnNext(zdesktopMessage);
                    }
                    observer.OnCompleted();
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }

                return Disposable.Create(() =>
                {
                    Streamings.Dispose();
                });
            });
        }
        public void Cancel()
        {
            if (!cts.IsCancellationRequested)
            {
                cts.Cancel();
            }
        }
        public void Dispose()
        {
            cts.Dispose();
            Streamings?.Dispose();
        }
    }
}

