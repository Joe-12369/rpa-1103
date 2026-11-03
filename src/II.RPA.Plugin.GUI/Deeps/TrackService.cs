using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using Gma.UserActivityMonitor;
using II.RPA.Logging;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Deeps.Highlight;
using II.RPA.Plugin.GUI.Deeps.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Drawing;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Threading;

namespace II.RPA.Plugin.GUI.Deeps
{
    public class TrackService : IDisposable
    {
        private static readonly ILogger _logger = Logger.OfContext();
        private readonly IDisposable _mouseHoverSubscription;
        private readonly IDisposable _mouseClickSubscription;
        private readonly BehaviorSubject<InspectMode> _inspectModeSubject = new BehaviorSubject<InspectMode>(InspectMode.None);
        private readonly Dispatcher _dispatcher;
        private readonly Subject<AutomationElement> _nextElement = new Subject<AutomationElement>();
        private readonly HighlightService _highlightService;
        private readonly AutomationBase _automationBase;
        private readonly CompositeDisposable _subscriptions = new CompositeDisposable();
        private readonly WndService _wndService;
        public TrackService(Dispatcher dispatcher, 
            HighlightService highlightService, 
            AutomationBase automationBase,
            WndService wndService)
        {
            _highlightService = highlightService;
            _automationBase = automationBase;
            _dispatcher = dispatcher;
            CurrentElement = _nextElement;
            _wndService = wndService;
        }
        public IObservable<AutomationElement> CurrentElement { get; }

        public bool IsTracking => _inspectModeSubject.Value != InspectMode.None;

        public void StartTrack(InspectMode mode)
        {
            //_highlightService.Resume();
            SetupTrackStreams();
            _inspectModeSubject.OnNext(mode);
            _logger.LogInformation($"Inspect mode set to {mode}");
        }

        public void StopTrack()
        {
            //_highlightService.Pause();
            _inspectModeSubject.OnNext(InspectMode.None);
            _logger.LogInformation("Inspect mode stopped");
        }

        public void Dispose()
        {
            DisposeMergeStream();
            _mouseHoverSubscription?.Dispose();
            _mouseClickSubscription?.Dispose();
            _inspectModeSubject?.Dispose();
        }
        private void UninstallHooks()
        {
            try
            {
                _mouseHoverSubscription?.Dispose();
                _mouseClickSubscription?.Dispose();
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, "UninstallHooks Error");
            }
        }

        private void SetupTrackStreams()
        {
            var mouseMoveStream = CreateHoverStream(10);
            var mouseDownStream = CreateMouseLeftButtonDownStream(10);

            var elementStream = mouseMoveStream
                .Where(_ => _inspectModeSubject.Value != InspectMode.None)
                .Select(point => SafeGetAutomationElement(point))
                .Where(e=> e != null)
                .Publish()
                .RefCount();

            var mouseHoverSubscription = elementStream
                .WithLatestFrom(_inspectModeSubject, (element, mode) => new { element, mode })
                .Where(x => x.mode == InspectMode.MouseHover && x.element != null)
                .Select(x => x.element)
                .Where(e => e != null)
                .Throttle(TimeSpan.FromMilliseconds(100))
                .Subscribe(
                    onNext: element => PublishElement(element, "hover"),
                    onError: ex => Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, "Error in hover stream")
                );

            var mouseClickSubscription = mouseDownStream
                .Throttle(TimeSpan.FromMilliseconds(100))
                .WithLatestFrom(_inspectModeSubject, (e, mode) => (e, mode))
                .Where(x => x.mode == InspectMode.MouseDown)
                .Select(_=> Cursor.Position)
                .Select(point => SafeGetAutomationElement(point))
                .Where(e => e != null)
                .Subscribe(
                    onNext: element => PublishElement(element, "click"),
                    onError: ex => Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, "Error in click stream")
                );

            //_subscriptions.Add(highlightSubscription);
            _subscriptions.Add(mouseHoverSubscription);
            _subscriptions.Add(mouseClickSubscription);
        }
        private IObservable<Point> CreateHoverStream(int throttle)
        {
            return Observable.Interval(TimeSpan.FromMilliseconds(throttle/2))
                .Throttle(TimeSpan.FromMilliseconds(throttle))
                .ObserveOn(TaskPoolScheduler.Default)
                .Select(_ => Cursor.Position)
                .DistinctUntilChanged(args => (args.X, args.Y))
                .Publish()
                .RefCount();
        }
        private IObservable<MouseEventArgs> CreateMouseLeftButtonDownStream(int throttle)
        {
            return
            _dispatcher.Invoke(() =>
            {
                return Observable.FromEventPattern<MouseEventHandler, MouseEventArgs>(
                    handler => HookManager.MouseDown += handler,
                    handler => HookManager.MouseDown -= handler
                )
                .Throttle(TimeSpan.FromMilliseconds(throttle))
                .Select(evt => evt.EventArgs)
                .ObserveOn(TaskPoolScheduler.Default)
                .Publish()
                .RefCount();
            });
        }
        private AutomationElement SafeGetAutomationElement(Point point)
        {
            try
            {
                var hwnd = UiCommon.WindowFromPoint(point);

                if (IsSafeHwnd(hwnd) && !IsExcludedWindow(hwnd))
                {
                    return _automationBase.FromPoint(point);
                }
                return null;
            }
            catch (COMException)
            {
                return null;
            }
            catch (TimeoutException)
            {
                return null;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(_logger, ex, $"Failed to get automation element from point {point}");
                return null;
            }
        }

        private void PublishElement(AutomationElement element, string source)
        {
            try
            {
                _nextElement.OnNext(element);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, "Error publishing element from {Source}", source);
            }
        }

        private static bool IsValidRectangle(Rectangle rect)
        {
            return rect != Rectangle.Empty &&
                   rect != default &&
                   rect.Width > 0 &&
                   rect.Height > 0;
        }

        private static bool IsSafeHwnd(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
            {
                return false;
            }

            var (IsHigher, _, _) = Utils.CompareTargetProcessIntegrity(hwnd);
            if (IsHigher)
            {
                return false;
            }
            var visible = Utils.IsWindowVisible(hwnd);
            if (!visible)
            {
                return false;
            }
            var hang = Utils.IsHungAppWindow(hwnd);
            if (hang)
            {
                return false;
            }
            return true;
        }

        private bool IsExcludedWindow(IntPtr hwnd)
        {
            return _wndService.IsWindowExcluded(hwnd);
        }
        private void DisposeMergeStream()
        {
            _subscriptions?.Dispose();
        }
    }
}

