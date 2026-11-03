using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using II.RPA.Logging;
using II.RPA.Plugin.Core.Services;
using II.RPA.Plugin.Core.UI;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Deeps.Actions;
using II.RPA.Plugin.GUI.Deeps.Highlight;
using II.RPA.Plugin.GUI.Deeps.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Deeps
{
    public class DeepInspectService
    {
        private static readonly ILogger _logger = Logger.OfContext();
        private IDisposable _incomingStreamDisposable;
        private IDisposable _trackStreamDisposable;
        private IDisposable _peerStreamDisposable;
        private volatile bool _started = false;
        private readonly ActionRouter _router;
        private readonly UIProvider _provider;
        private readonly TrackService _trackService;
        private readonly IElementService _elementService;
        private readonly HighlightService _highlightService;
        private readonly AutomationBase _automationBase;
        private readonly PushMessageService _pushMessageService;
        private readonly WndService _wndService;
        private readonly int _maxConcurrency = 3;
        private int _currentConcurrency = 0;
        public DeepInspectService()
        {
            _automationBase = UiCommon.UIAAutomation;
            _wndService = new WndService();
            _pushMessageService = new PushMessageService();
            _provider = new UIProvider();
            _highlightService = new HighlightService(_provider.UIDispatcher);
            _trackService = new TrackService(_provider.UIDispatcher, _highlightService, _automationBase, _wndService);
            _elementService = new ElementService(_highlightService, _automationBase, _wndService);
            _router = new ActionRouter(_trackService, _elementService);
            try
            {
                _maxConcurrency = Environment.ProcessorCount;
                _logger.LogInformation($"ProcessorCount:{_maxConcurrency}");
            }
            catch
            {
                
            }
        }
        public static HashSet<string> ProductNames
        {
            get;
            private set;
        }
        public bool IsStarted => _started;

        public void Start(string[] names)
        {
            if (names != null && names.Length > 0)
            {
                foreach (var name in names)
                {
                    _wndService.AddToExcludedProcessNames(name);
                }
            }
            _wndService.WarmupCache();
            if (!_started)
            {
                _started = true;
                _highlightService.Setup();

                _trackStreamDisposable = _trackService
                    .CurrentElement
                    .Where(x => x != null)
                    .Where(_ => _started)
                    .Throttle(TimeSpan.FromMilliseconds(150))
                    .Subscribe(onNext: HandleTrackMessage,
                    onError: e => { Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, e, "CurrentElement.OnError"); },
                    onCompleted:() => { _logger.LogInformation("CurrentElement.onCompleted"); });

                _peerStreamDisposable = _elementService
                    .PeerStream
                    .Where(x => x != null && !x.Deprecated)
                    .Where(_ => _started)
                    .Where(_ => _trackService.IsTracking)
                    .Subscribe(result =>
                    {
                        _ = _pushMessageService.NotifyPeerElementAsync(result);
                    },
                    onError: e => { Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, e, "PeerStream.OnError"); },
                    onCompleted: () => { _logger.LogInformation("PeerStream.onCompleted"); });

                _incomingStreamDisposable = RuntimeMessageService
                    .IncomingStream
                    .Where(_ => _started)
                    .Where(messageModel => !_pushMessageService.TryHandleResponse(messageModel))
                    .Select(messageModel =>
                        Observable.FromAsync(() =>
                        {
                            return ProcessMessageAsync(messageModel);
                        })
                    )
                    .Merge(_maxConcurrency)
                    .Subscribe();
            }
        }

        public void Stop()
        {
            _started = false;
            _trackService.StopTrack();
            _highlightService.Clear();
            _incomingStreamDisposable?.Dispose();
            _peerStreamDisposable?.Dispose();
            _trackStreamDisposable?.Dispose();
        }
        private void HandleTrackMessage(AutomationElement automationElement)
        {
            _ = _elementService.StartPeerElementAsync(automationElement);
        }

        private void Speak(string action, string payload, string requestId)
        {
            if (!_started)
                return;
            RuntimeMessageService.SendMessage(new RuntimeMessageModel
            {
                Action = action,
                Payload = payload,
                RequestId = requestId
            });
        }
        private Task ProcessMessageAsync(RuntimeMessageModel messageModel)
        {
            Interlocked.Increment(ref _currentConcurrency);
            _logger.LogInformation($"concurrency:{_currentConcurrency}");
            try
            {
                _logger.LogDebug(
                    $"Incoming message [{messageModel.RequestId}] " +
                    $"Action={messageModel.Action}: " +
                    Environment.NewLine +
                    $"Payload={messageModel.Payload}");

                var result = _router.RouteAction(messageModel.Action, messageModel.Payload);

                if (string.IsNullOrEmpty(result))
                {
                    result = PayloadDto.Failed().ToString();
                }
                Speak(messageModel.Action, result, messageModel.RequestId);

                _logger.LogDebug($"Outgoing message [{messageModel.RequestId}]" +
                    $"Action={messageModel.Action}:" +
                    Environment.NewLine + 
                    $"{messageModel.Payload}");
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, $"Error handling message RequestId={messageModel.RequestId}");
                Speak(messageModel.Action,
                    PayloadDto.Failed(ex.Message).ToString(),
                    messageModel.RequestId);
            }
            finally
            {
                Interlocked.Decrement(ref _currentConcurrency);
            }
            return Task.CompletedTask;
        }
    }
}


