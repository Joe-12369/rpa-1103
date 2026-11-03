using II.Framework.Utility;
using II.RPA.ZDeskTop.Views;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace II.RPA.ZDeskTop.Services;

public class OverlayService : ReactiveObject
{
    private readonly ILogger _logger;
    private OverlayWindow _overlayWindow;
    private ZDeskTopWindow _window;
    private bool _isShown = false;
    private readonly ControlService _controlService;
    private bool _enable = false;
    public OverlayService(ILogger<OverlayService> logger, ControlService controlService)
    {
        _logger = logger;
        _controlService = controlService;
        _logger.LogInformation($"{nameof(OverlayService)} .ctor");
        _controlService.RunStream
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(running => 
            {
                if (running)
                {
                    PauseButtonVisibility = Visibility.Visible;
                }
                else
                {
                    PauseButtonVisibility = Visibility.Collapsed;
                }
            });
        RestoreCommand = ReactiveCommand.Create(() =>
        {
            _logger.LogDebug("RestoreCommand start");
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    _window?.CancelMiniMode();
                    Disable();
                    Hide();
                }
                catch (Exception ex)
                {
                    _logger.LogError(new EventId(1), ex, "RestoreCommand");
                }
            });

            _logger.LogDebug("RestoreCommand end");
        }, outputScheduler: RxApp.MainThreadScheduler);
        PauseCommand = ReactiveCommand.CreateFromTask<bool, (bool, bool)>(async pause =>
        {
            if (pause)
            {
                try
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _overlayWindow.IsEnabled = false;
                        PauseButtonVisibility = Visibility.Collapsed;
                        //PausingVisibility = Visibility.Visible;
                    });
                    _logger.LogDebug($"PasueCommand：start {pause}");

                    await _controlService.Pause();

                    await Task.Delay(100).ConfigureAwait(false);
                    _logger.LogDebug($"PasueCommand：end");
                    return (true, true);
                }
                catch(Exception ex)
                {
                    _logger.LogError(new EventId(0), ex, "Pause failed!");
                    return (true, false);
                }
            }
            else
            {
                return (false, true);
            }
        });
        PauseCommand
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(result =>
            {
                if (result.Item1)
                {
                    PausingVisibility = Visibility.Collapsed;
                    //PauseButtonVisibility = Visibility.Visible;
                    _logger.LogDebug($"Pause result: {result.Item2}");
                    IsPaused = false;
                    _overlayWindow.IsEnabled = true;
                }
                else
                {
                    _logger.LogDebug($"Resume result: {result.Item2}");
                }
            });
        if (OSVersionHelper.IsWindows11())
        {
            OverlayCornerRadius = new CornerRadius(8);
        }
    }
    public ReactiveCommand<Unit, Unit> RestoreCommand
    {
        get;
        set;
    }
    public ReactiveCommand<bool, (bool, bool)> PauseCommand
    {
        get;
        set;
    }
    private Visibility _visibility = Visibility.Collapsed;
    public Visibility PauseButtonVisibility
    {
        get
        {
            return _visibility;
        }
        set
        {
            this.RaiseAndSetIfChanged(ref _visibility, value);
        }
    }
    private Visibility _pausingVisibility = Visibility.Collapsed;
    public Visibility PausingVisibility
    {
        get
        {
            return _pausingVisibility;
        }
        set
        {
            this.RaiseAndSetIfChanged(ref _pausingVisibility, value);
        }
    }
    private CornerRadius _overlayCornerRadius = new(0);
    public CornerRadius OverlayCornerRadius
    {
        get
        {
            return _overlayCornerRadius;
        }
        set
        {
            this.RaiseAndSetIfChanged(ref _overlayCornerRadius, value);
        }
    }
    private bool _isPaused = false;
    public bool IsPaused
    {
        get
        {
            return _isPaused;
        }
        set
        {
            this.RaiseAndSetIfChanged(ref _isPaused, value);
        }
    }
    public void InitOwner(ZDeskTopWindow window)
    {
        if (_window == null)
        {
            _overlayWindow = new OverlayWindow
            {
                DataContext = this
            };
            _window = window;
            _window.SizeChanged += OwnerSizeChanged;
            _window.LocationChanged += OwnerLocationChanged;
            _overlayWindow.Owner = _window;
            Task.Factory.StartNew(async () =>
            {
                while (Application.Current != null)
                {
                    try
                    {
                        await Task.Delay(200);
                        if (!_enable)
                        {
                            if (Application.Current == null)
                            {
                                break;
                            }
                            await Application.Current?.Dispatcher?.InvokeAsync(() =>
                            {
                                Hide();
                            });
                            continue;
                        }
                        await Application.Current?.Dispatcher?.InvokeAsync(() =>
                        {
                            if (_window != null)
                            {
                                if (IsMouseCapture())
                                {
                                    Show();
                                }
                                else
                                {
                                    Hide();
                                }
                            }
                        });
                    }
                    catch (NullReferenceException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(new EventId(1), ex, "");
                        continue;
                    }
                }
            }, TaskCreationOptions.LongRunning);
        }
    }


    private void OwnerLocationChanged(object sender, EventArgs e)
    {
        _overlayWindow.Left = _window.Left;
        _overlayWindow.Top = _window.Top;
    }

    private void OwnerSizeChanged(object sender, EventArgs e)
    {
        _overlayWindow.Width = _window.Width;
        _overlayWindow.Height = _window.Height;
    }

    public void Enable()
    {
        _enable = true;
    }
    public void Disable()
    {
        _enable = false;
    }
    public void Show()
    {
        if (_enable)
        {
            Application.Current.Dispatcher.Invoke(() => 
            {
                if (!_isShown)
                {
                    ShowCore();
                }
                _overlayWindow.Visibility = Visibility.Visible;
                _overlayWindow.IsHitTestVisible = true;
            });
        }
    }
    public void Hide()
    {
        if (_isShown)
        {
            Application.Current?.Dispatcher.Invoke(() =>
                {
                    if (_overlayWindow.Visibility == Visibility.Visible)
                    {
                        _overlayWindow.Visibility = Visibility.Hidden;
                        _overlayWindow.IsHitTestVisible = false;
                    }
                });
        }
    }
    private void ShowCore()
    {
        _overlayWindow.Left = _window.Left;
        _overlayWindow.Top = _window.Top;
        _overlayWindow.Width = _window.Width;
        _overlayWindow.Height = _window.Height;
        _overlayWindow.Show();
        //WindowBrushHelper.SetAcrylicBrush(_overlayWindow);
        _isShown = true;
    }
    public bool IsMouseCapture()
    {
        if (_window != null)
        {
            var point = System.Windows.Forms.Control.MousePosition;
            if (point.X / (TransformToPixels(_window).Item1 / 96.0) < _window.Left
                || point.Y / (TransformToPixels(_window).Item1 / 96.0) < _window.Top)
            {
                return false;
            }
            else if (point.X / (TransformToPixels(_window).Item1 / 96.0) > _window.Left + _window.ActualWidth
                || point.Y / (TransformToPixels(_window).Item1 / 96.0) > _window.Top + _window.ActualHeight)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        return false;
    }
    public (double, double) TransformToPixels(Window aVisual)
    {
        Matrix matrix;
        var helper = new WindowInteropHelper(aVisual);
        var handle = helper.EnsureHandle();
        var source = PresentationSource.FromVisual(aVisual);
        if (source != null)
        {
            matrix = source.CompositionTarget.TransformToDevice;
        }
        else
        {
            using var src = HwndSource.FromHwnd(handle);
            matrix = src.CompositionTarget.TransformToDevice;
        }

        double pixelX = (int)(matrix.M11 * 96.0);
        double pixelY = (int)(matrix.M22 * 96.0);
        return (pixelX, pixelY);
    }
}
