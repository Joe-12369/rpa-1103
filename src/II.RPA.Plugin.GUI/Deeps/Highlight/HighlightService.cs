using II.RPA.Plugin.Core;
using Microsoft.Win32;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace II.RPA.Plugin.GUI.Deeps.Highlight
{
    public class HighlightService : IDisposable
    {
        private HighlightBorder _highlightWindow;
        private Rectangle _currentRect;
        private const string NATIVE_BORDER_COLOR = "#FE5402";
        private const string NATIVE_BORDER_FIIL_COLOR = "#30FE5402";
        private readonly Dispatcher _dispatcher;
        private bool _isVisible = false;
        private bool _ready = false;

        public HighlightService(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        }

        public void Setup()
        {
            if (_ready) return;

            Show(new Rectangle(-100, -100, 0, 0));
            Resume();
            _ready = true;
        }
        private void Show(Rectangle rect)
        {
            _dispatcher.Invoke(() =>
            {
                if (_highlightWindow == null)
                {
                    var borderColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(NATIVE_BORDER_COLOR));
                    _highlightWindow = new HighlightBorder(borderColor);
                    _highlightWindow.SourceInitialized += (s, e) =>
                    {
                        SetWindowTransparent(true);
                        Reposition(rect);
                    };
                    _highlightWindow.Closed += (s, e) => _highlightWindow = null;
                }

                _currentRect = rect;
                _highlightWindow.Show();
                var fillBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(NATIVE_BORDER_FIIL_COLOR));
                _highlightWindow.ResetFillBrush(fillBrush);
                Reposition(rect);
                _isVisible = true;
            });
        }

       
        public void Pause()
        {
            if (!_isVisible)
            {
                return;
            }
            _dispatcher.Invoke(() =>
            {
                _highlightWindow.Hide();
            });
            _isVisible = false;
        }
        public void Reset()
        {
            if (!_isVisible)
            {
                return;
            }
            _dispatcher.Invoke(() =>
            {
                Reposition(new Rectangle(-100, -100, 0, 0));
            });
        }
        public void Resume()
        {
            if (_isVisible)
            {
                return;
            }
            _dispatcher.Invoke(() =>
            {
                if (_highlightWindow.Visibility != Visibility.Visible)
                {
                    _highlightWindow.Visibility = Visibility.Visible;
                }
            });
            _isVisible = true;
        }
        public void UpdateLocation(Rectangle rect)
        {
            _dispatcher.Invoke(() =>
            {
                _currentRect = rect;
                if (_highlightWindow != null)
                {
                    Reposition(rect);
                }
            });
        }
        public Task UpdateLocationAsync(Rectangle rect)
        {
            return
            _dispatcher.InvokeAsync(() =>
            {
                _currentRect = rect;
                if (_highlightWindow != null)
                {
                    //_highlightWindow.ResetFillBrush();
                    Reposition(rect);
                }
            }).Task;
        }
        public void Clear()
        {
            _dispatcher.InvokeAsync(() =>
            {
                if (_highlightWindow != null)
                {
                    _highlightWindow.Close();
                    _highlightWindow = null;
                }
            });
            _ready = false;
        }
        private void Reposition(Rectangle rect)
        {
            if (_highlightWindow == null) return;

            var (X, Y) = GetDpi(_highlightWindow);
            double factor = X / 96.0;

            var scaled = new Rectangle(
                (int)(rect.X / factor),
                (int)(rect.Y / factor),
                (int)(rect.Width / factor),
                (int)(rect.Height / factor)
            );

            _highlightWindow.Left = scaled.Left;
            _highlightWindow.Top = scaled.Top;
            _highlightWindow.Width = scaled.Width;
            _highlightWindow.Height = scaled.Height;
        }

        private void SetWindowTransparent(bool transparent)
        {
            if (_highlightWindow == null) return;

            IntPtr hwnd = new WindowInteropHelper(_highlightWindow).Handle;
            uint style = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
            if (transparent)
            {
                NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, style | NativeMethods.WS_EX_TRANSPARENT);
            }
            else
            {
                NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, style & ~NativeMethods.WS_EX_TRANSPARENT);
            }
        }

        private void OnDisplaySettingsChanged(object sender, EventArgs e)
        {
            _ = _dispatcher.InvokeAsync(() =>
            {
                if (_highlightWindow != null && _highlightWindow.IsVisible)
                {
                    Reposition(_currentRect);
                }
            });
        }

        private (double X, double Y) GetDpi(Visual visual)
        {
            var source = PresentationSource.FromVisual(visual);
            if (source?.CompositionTarget != null)
            {
                var m = source.CompositionTarget.TransformToDevice;
                return (m.M11 * 96.0, m.M22 * 96.0);
            }
            return (96, 96);
        }

        public void Dispose()
        {
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            _dispatcher.Invoke(() => _highlightWindow?.Close());
        }
    }

}
