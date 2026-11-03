using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;

namespace II.RPA.Plugin.Core.Wpf.Controls.Windows
{
    public class FluentWindow : Window
    {
        public const int WM_NCACTIVATE = 0x0086;

        public static readonly DependencyProperty WindowTitleBarProperty =
            DependencyProperty.Register(nameof(WindowTitleBar),
                typeof(FluentWindowTitleBar),
                typeof(FluentWindow),
                new PropertyMetadata(default(FluentWindowTitleBar),
                    OnWindowTitleBarChanged));

        public static readonly DependencyProperty WindowTitleBarLeftProperty =
            DependencyProperty.Register(nameof(WindowTitleBarLeft),
                typeof(FluentWindowTitleBar),
                typeof(FluentWindow),
                new PropertyMetadata(default(FluentWindowTitleBar)));

        public static readonly DependencyProperty WindowTitleBarCloseProperty =
           DependencyProperty.Register(nameof(WindowTitleBarClose),
               typeof(FluentWindowTitleBar),
               typeof(FluentWindow),
               new PropertyMetadata(default(FluentWindowTitleBar)));

        private static readonly DependencyPropertyKey IsNonClientActivePropertyKey =
         DependencyProperty.RegisterReadOnly("IsNonClientActive",
             typeof(bool), typeof(FluentWindow),
             new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty IsNonClientActiveProperty
            = IsNonClientActivePropertyKey.DependencyProperty;

        private readonly IntPtr _trueValue = new IntPtr(1);

        static FluentWindow()
        {
            //DefaultStyleKeyProperty.OverrideMetadata(typeof(FluentWindow), new FrameworkPropertyMetadata(typeof(FluentWindow)));
        }
        public FluentWindow()
        {
            //DefaultStyleKey = typeof(FluentWindow);
            CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, CloseWindow));
            CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand, MaximizeWindow, CanResizeWindow));
            CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand, MinimizeWindow, CanMinimizeWindow));
            CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand, RestoreWindow, CanResizeWindow));
            CommandBindings.Add(new CommandBinding(SystemCommands.ShowSystemMenuCommand, ShowSystemMenu));
        }



        public double WindowChromeHeight
        {
            get { return (double)GetValue(WindowChromeHeightProperty); }
            set { SetValue(WindowChromeHeightProperty, value); }
        }

        // Using a DependencyProperty as the backing store for WindowChromeHeight.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WindowChromeHeightProperty =
            DependencyProperty.Register("WindowChromeHeight", typeof(double), typeof(FluentWindow), new PropertyMetadata(SystemParameters.WindowNonClientFrameThickness.Top));


        public FluentWindowTitleBar WindowTitleBar
        {
            get => (FluentWindowTitleBar)GetValue(WindowTitleBarProperty);
            set => SetValue(WindowTitleBarProperty, value);
        }

        public FluentWindowTitleBar WindowTitleBarLeft
        {
            get => (FluentWindowTitleBar)GetValue(WindowTitleBarLeftProperty);
            set => SetValue(WindowTitleBarLeftProperty, value);
        }

        public FluentWindowTitleBar WindowTitleBarClose
        {
            get => (FluentWindowTitleBar)GetValue(WindowTitleBarCloseProperty);
            set => SetValue(WindowTitleBarCloseProperty, value);
        }

        public bool IsNonClientActive
        {
            get
            {
                return (bool)GetValue(IsNonClientActiveProperty);
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            if (SizeToContent == SizeToContent.WidthAndHeight && WindowChrome.GetWindowChrome(this) != null)
            {
                InvalidateMeasure();
            }
            IntPtr handle = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(handle).AddHook(new HwndSourceHook(WndProc));
        }

        protected virtual void OnWindowTitleBarChanged(FluentWindowTitleBar oldValue, FluentWindowTitleBar newValue)
        {
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            SetValue(IsNonClientActivePropertyKey, true);
        }

        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            SetValue(IsNonClientActivePropertyKey, false);
        }

        private static void OnWindowTitleBarChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var oldValue = (FluentWindowTitleBar)args.OldValue;
            var newValue = (FluentWindowTitleBar)args.NewValue;
            if (oldValue == newValue)
            {
                return;
            }

            var target = obj as FluentWindow;
            target?.OnWindowTitleBarChanged(oldValue, newValue);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_NCACTIVATE)
                SetValue(IsNonClientActivePropertyKey, wParam == _trueValue);

            return IntPtr.Zero;
        }

        private void CanResizeWindow(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.ResizeMode == ResizeMode.CanResize
                || this.ResizeMode == ResizeMode.CanResizeWithGrip;
        }

        private void CanMinimizeWindow(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.ResizeMode != ResizeMode.NoResize;
        }

        private void CloseWindow(object sender, ExecutedRoutedEventArgs e)
        {
            this.Close();
        }

        private void MaximizeWindow(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MaximizeWindow(this);
            e.Handled = true;
        }

        private void MinimizeWindow(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
            e.Handled = true;
        }

        private void RestoreWindow(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.RestoreWindow(this);
            e.Handled = true;
        }

        private void ShowSystemMenu(object sender, ExecutedRoutedEventArgs e)
        {
            Point point = PointToScreen(new Point(0, 0));
            var dipScale = WindowParameters.GetDpi() / 96d;
            if (WindowState == WindowState.Maximized)
            {
                point.X += (SystemParameters.WindowNonClientFrameThickness.Left + WindowParameters.PaddedBorderThickness.Left) * dipScale;
                point.Y += (SystemParameters.WindowNonClientFrameThickness.Top +
                            WindowParameters.PaddedBorderThickness.Top +
                            SystemParameters.WindowResizeBorderThickness.Top -
                            this.BorderThickness.Top)
                            * dipScale;
            }
            else
            {
                point.X += this.BorderThickness.Left * dipScale;
                point.Y += SystemParameters.WindowNonClientFrameThickness.Top * dipScale;
            }

            CompositionTarget compositionTarget = PresentationSource.FromVisual(this).CompositionTarget;
            SystemCommands.ShowSystemMenu(this, compositionTarget.TransformFromDevice.Transform(point));
            e.Handled = true;
        }
    }
}
