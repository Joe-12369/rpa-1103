using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;

namespace II.Framework.UI;

public class BaseIIWindow : Window
{
    public const int WM_NCACTIVATE = 0x0086;


    private readonly IntPtr _trueValue = new IntPtr(1);

    public BaseIIWindow()
    {
        DefaultStyleKey = typeof(BaseIIWindow);
        CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, CloseWindow));
        CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand, MaximizeWindow, CanResizeWindow));
        CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand, MinimizeWindow, CanMinimizeWindow));
        CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand, RestoreWindow, CanResizeWindow));
        CommandBindings.Add(new CommandBinding(SystemCommands.ShowSystemMenuCommand, ShowSystemMenu));
    }


    public static readonly DependencyProperty WindowTitleBarProperty =
          DependencyProperty.Register(nameof(WindowTitleBar), typeof(WindowTitleBar), typeof(BaseIIWindow), new PropertyMetadata(default(WindowTitleBar), OnWindowTitleBarChanged));

    private static readonly DependencyPropertyKey IsNonClientActivePropertyKey =
          DependencyProperty.RegisterReadOnly("IsNonClientActive", typeof(bool), typeof(BaseIIWindow), new FrameworkPropertyMetadata(false));

    public static readonly DependencyProperty IsNonClientActiveProperty = IsNonClientActivePropertyKey.DependencyProperty;
    /// <summary>
    /// 获取或设置WindowTitleBar的值
    /// </summary>
    public WindowTitleBar WindowTitleBar
    {
        get => (WindowTitleBar)GetValue(WindowTitleBarProperty);
        set => SetValue(WindowTitleBarProperty, value);
    }

    public bool IsNonClientActive
    {
        get
        {
            return (bool)GetValue(IsNonClientActiveProperty);
        }
    }

    public static readonly DependencyProperty WindowCenterTitleBarProperty =
         DependencyProperty.Register(nameof(WindowCenterTitleBar), typeof(WindowTitleBar), typeof(BaseIIWindow), new PropertyMetadata(default(WindowTitleBar), OnWindowCenterTitleBarChanged));

    public WindowTitleBar WindowCenterTitleBar
    {
        get => (WindowTitleBar)GetValue(WindowCenterTitleBarProperty);
        set => SetValue(WindowCenterTitleBarProperty, value);
    }

    public GridLength ChromeHeight
    {
        get { return (GridLength)GetValue(ChromeHeightProperty); }
        set { SetValue(ChromeHeightProperty, value); }
    }

    // Using a DependencyProperty as the backing store for ChromeHeight.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ChromeHeightProperty =
        DependencyProperty.Register("ChromeHeight", typeof(GridLength), typeof(BaseIIWindow), new PropertyMetadata(GridLength.Auto));



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

    /// <summary>
    /// WindowTitleBar 属性更改时调用此方法。
    /// </summary>
    /// <param name="oldValue">WindowTitleBar 属性的旧值。</param>
    /// <param name="newValue">WindowTitleBar 属性的新值。</param>
    protected virtual void OnWindowTitleBarChanged(WindowTitleBar oldValue, WindowTitleBar newValue)
    {
    }
    protected virtual void OnWindowCenterTitleBarChanged(WindowTitleBar oldValue, WindowTitleBar newValue)
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
        var oldValue = (WindowTitleBar)args.OldValue;
        var newValue = (WindowTitleBar)args.NewValue;
        if (oldValue == newValue)
        {
            return;
        }

        var target = obj as BaseIIWindow;
        target?.OnWindowTitleBarChanged(oldValue, newValue);
    }
    private static void OnWindowCenterTitleBarChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
    {
        var oldValue = (WindowTitleBar)args.OldValue;
        var newValue = (WindowTitleBar)args.NewValue;
        if (oldValue == newValue)
        {
            return;
        }

        var target = obj as BaseIIWindow;
        target?.OnWindowCenterTitleBarChanged(oldValue, newValue);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_NCACTIVATE)
            SetValue(IsNonClientActivePropertyKey, wParam == _trueValue);

        return IntPtr.Zero;
    }

    private void CanResizeWindow(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = this.ResizeMode == ResizeMode.CanResize || this.ResizeMode == ResizeMode.CanResizeWithGrip;
    }

    private void CanMinimizeWindow(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = this.ResizeMode != ResizeMode.NoResize;
    }

    private void CloseWindow(object sender, ExecutedRoutedEventArgs e)
    {
        if (MessageBox.Show($"退出 {AppMeta.Default.AppName} 将导致正在执行的任务强制退出！{Environment.NewLine}确认要退出 {AppMeta.Default.AppName} 吗？", "退出", MessageBoxButton.YesNoCancel, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            Close();
        }
        else
        {
            e.Handled = true;
        }
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

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == WindowStyleProperty)
        {
            if (e.NewValue is WindowStyle newStyle)
            {
                if (newStyle == WindowStyle.None)
                {
                    ChromeHeight = new GridLength(0);
                    this.Background = Brushes.Transparent;
                    this.BorderThickness = new Thickness(0,0,0,0);
                    this.BorderBrush = Brushes.Transparent;
                    WindowChrome.SetWindowChrome(this, new WindowChrome 
                    {
                        GlassFrameThickness = new Thickness(0, 0, 0, 0), 
                    });
                }
                else
                {
                    ChromeHeight = GridLength.Auto;
                }
            }
        }
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
