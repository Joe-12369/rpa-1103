using Grpc.Core;
using II.Framework.Models;
using II.Framework.Utility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Rpa.Core.Childsession;
using System;
using System.Drawing;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;

namespace II.Framework.gRPC
{
    public class ChildSessionClientFacade
    {
        private readonly ILogger _logger;
        private IDisposable _disposed;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ChildSessionService.ChildSessionServiceClient childSessionServiceClient;
        private AsyncClientStreamingCall<Message, Message> _asyncClientStreamingCall;
        private readonly string _rootPath =  $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\Z-Core\\zdesktop";
        public ChildSessionClientFacade(ILogger<ChildSessionClientFacade> logger)
        {
            _logger = logger;
            _cancellationTokenSource = new CancellationTokenSource();
            var endPoint = $"127.0.0.1:{GetPort()}";
            if (Directory.Exists(_rootPath))
            {
                try
                {
                    Directory.Delete(_rootPath, true);
                    Directory.CreateDirectory(_rootPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Fail to operate _rootPath:{ex}");
                }
            }
            _logger.LogInformation($"server stream:{endPoint}");
            var _channel = new Channel(endPoint, ChannelCredentials.Insecure);
            childSessionServiceClient = new ChildSessionService.ChildSessionServiceClient(_channel);
        }
        public void CloseChildSession(bool allowError = false)
        {
            try
            {
                childSessionServiceClient.ChildSessionCloseUnary(new Message { MessageId = new Random().Next(100000, 999999) });
            }
            catch
            {
                if (!allowError) 
                {
                    throw;
                }
            }
        }
        public void StopSendingSnapshot()
        {
            _disposed?.Dispose();
            _disposed = null;
        }
        public void SendingSnapshot(IObservable<SnapshotModel> observablePath)
        {
            if (_asyncClientStreamingCall != null)
            {
                _disposed?.Dispose();
                _asyncClientStreamingCall.RequestStream.CompleteAsync();
            }
            _asyncClientStreamingCall = childSessionServiceClient.SnapshotFromZDesktopStreaming();
            _disposed = observablePath
                .Subscribe(async model =>
                {
                    var path = Path.Combine(_rootPath, $"{DateTime.Now:HH-mm-ss}-{model.Number}.jpg");
                    ImageCompressor.CompressImage(model.Picture, path, 50);
                    await _asyncClientStreamingCall.RequestStream.WriteAsync(new Message
                    {
                        Action = "subscribe.start",
                        Payload = $"{{\"path\":{JsonConvert.ToString(path)},\"base64\":\"\"}}"
                    });
                });
        }
        public IObservable<Message> SubscribeServerStreaming()
        {
            return Observable.Create<Message>(async (observer, cancellationToken) =>
            {
                // Create a new serverStreamCall and handle it inside the observable
                using var serverStreamCall = childSessionServiceClient.ServerStreamingToZdesktop(
                    new Message
                    {
                        MessageId = new Random().Next(100000, 999999)
                    },
                    cancellationToken: cancellationToken);

                try
                {
                    var responseStream = serverStreamCall.ResponseStream;
                    while (await responseStream.MoveNext(cancellationToken))
                    {
                        _logger.LogInformation($"{responseStream.Current.Payload}");
                        var response = responseStream.Current;
                        observer.OnNext(response);
                    }

                    _logger.LogInformation($"ServerStreamingToZdesktop over!");
                    observer.OnCompleted();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.ToString());
                    observer.OnCompleted();
                }

                // Clean-up action: This ensures the stream call is disposed when the subscription ends
                return Disposable.Create(() =>
                {
                    serverStreamCall.Dispose();
                });
            });
        }

        private int GetPort()
        {
            var fileName = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\Z-Core\\Client.txt";
            if (File.Exists(fileName))
            {
                var rootClient = File.ReadAllText(fileName);

                var portFileName = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\Z-Core\\{rootClient}.txt";
                if (File.Exists(portFileName))
                {
                    string idString = File.ReadAllText(portFileName);
                    var port = int.Parse(idString);
                    return port;
                }
                throw new FileNotFoundException(portFileName);
            }
            throw new FileNotFoundException(fileName);
        }
    }
}
