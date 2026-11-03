using Gma.UserActivityMonitor;
using II.RPA.Logging;
using II.RPA.Plugin.GUI.ProcessCreator.ViewModel;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.Devices;
using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace II.RPA.Plugin.GUI.ProcessCreator
{
    /// <summary>
    /// Interaction logic for ActionWindow.xaml
    /// </summary>
    public partial class ActionWindow : Window
    {
        #region interop declare
        private readonly uint _blurOpacity = 0;
        private readonly uint _blurBackgroundColor = 0x990000;
        [DllImport("user32.dll")]
        internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);
        #endregion
        private static readonly ILogger logger = Logger.OfContext();
        private readonly ActionViewModelBase m_ViewModel;
        private bool m_IsActionOver = false;
        private Point m_PressedPosition;
        private bool m_IsDragMoved = false;

        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="aViewModel">viewmodel</param>
        /// <param name="aPrepareCost">构建窗口参数的时间花费、调试用</param>
        public ActionWindow(ActionViewModelBase aViewModel,string aPrepareCost = null)
        {
            InitializeComponent();
            SourceInitialized += ActionWindow_SourceInitialized;
            //Deactivated += ActionWindow_Deactivated;
            Loaded += ActionWindow_Loaded;
            Activated += ActionWindow_Activated;
            InstallHook();
            m_ViewModel = aViewModel;
            m_ViewModel.CloseWindowAction = WindowEventHandler;
            DataContext = m_ViewModel;
            //KeyDown += ActionWindow_KeyDown;
        }

        private void ActionWindow_MouseMove(object sender, MouseEventArgs e)
        {
            if (System.Windows.Input.Mouse.LeftButton  == MouseButtonState.Pressed 
                && m_PressedPosition != e.GetPosition(this))
            {
                m_IsDragMoved = true;
                DragMove();
            }
        }
        private void ActionWindow_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (m_IsDragMoved)
            {
                m_IsDragMoved = false;
                e.Handled = true;
            }
        }

        private void ActionWindow_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            m_PressedPosition = e.GetPosition(this);
        }


        private void ActionWindow_Activated(object sender, EventArgs e)
        {
            logger.LogDebug("ActionWindow.Activated");
        }

        private void ActionWindow_Loaded(object sender, RoutedEventArgs e)
        {
            PreviewMouseLeftButtonDown += ActionWindow_PreviewMouseLeftButtonDown;
            MouseMove += ActionWindow_MouseMove;
            PreviewMouseLeftButtonUp += ActionWindow_PreviewMouseLeftButtonUp;
            m_IsDragMoved = false;
        }

        private void ActionWindow_Deactivated(object sender, EventArgs e)
        {
            WindowEventHandler("ActionWindow_Deactivated");
        }

        private void ActionWindow_SourceInitialized(object sender, EventArgs e)
        {
            logger.LogDebug("ActionWindow.SourceInitialized");
            SetWindowLocation(this);
            Activate();
            //this.ActivateCenteredToMouse();
            //searchbox.Focus();
        }

        #region hook
        private void InstallHook()
        {
            UninstallHook();
            HookManager.KeyUp += HookManager_KeyUp;
            HookManager.KeyDown += HookManager_KeyDown;
        }

        private void UninstallHook()
        {
            try
            {
                HookManager.KeyDown -= HookManager_KeyDown;
                HookManager.KeyUp -= HookManager_KeyUp;
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
        }
        private void HookManager_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {

        }
        private void HookManager_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == System.Windows.Forms.Keys.Escape)  
            {
                logger.LogDebug($"ActionWindow.HookManager_KeyUp:Escape");
                WindowEventHandler("Escape");
            }
        }
        #endregion
        /// <summary>
        /// 设置窗口位置
        /// </summary>
        /// <param name="window"></param>
        private void SetWindowLocation(ActionWindow window)
        {
            double offset_X = 42;
            double offset_Y = 42;
            try
            {
                var screen = System.Windows.Forms.Screen.PrimaryScreen;
                var workingAreaRect = screen.WorkingArea;
                logger.LogInformation($"workingAreaRect:{workingAreaRect.Width},{workingAreaRect.Height}");
                var cursorPoint = System.Windows.Forms.Cursor.Position;
                PresentationSource source = PresentationSource.FromVisual(this);
                double dpiX = source.CompositionTarget.TransformToDevice.M11;
                double dpiY = source.CompositionTarget.TransformToDevice.M22;
                logger.LogInformation($"dpi:{dpiX},{dpiY}");
                //NLogger.Default.Debug($"window_size:{window.Width},{window.Height}");
                //NLogger.Default.Debug($"window_actual_ize:{window.ActualWidth},{window.ActualHeight}"); 

                if ((workingAreaRect.Width / dpiX) - (cursorPoint.X / dpiX) <= window.Width / dpiX)
                {
                    window.Left = workingAreaRect.Width / dpiX - window.Width / dpiX - offset_X / dpiX;
                }
                else
                {
                    window.Left = cursorPoint.X / dpiX;
                }
                //double height = window.Height;
                double height = 360.0;
                //double height = 40 + m_ViewModel.ItemsCount * 28;
                if ((workingAreaRect.Height / dpiY) - (cursorPoint.Y / dpiY) <= height / dpiY)
                {
                    window.Top = workingAreaRect.Height / dpiY - height / dpiY - offset_Y / dpiX;
                }
                else
                {
                    window.Top = cursorPoint.Y;
                }
                logger.LogInformation($"SetWindowLocation:{this.Left},{this.Top}");
            }
            catch (Exception ex)
            {
                logger.LogError($"SetWindowLocation Error:", ex);
                window.Top = offset_Y;
                window.Left = offset_X;
            }
        }

        private void WindowEventHandler(string aParam)
        {
            if (!m_IsActionOver)
            {
                m_IsActionOver = true;
                logger.LogInformation($"close reason:{aParam}");
                UninstallHook();
                Dispatcher.Invoke(() => 
                {
                    Close();
                });
            }
        }
        /// <summary>
        /// 模糊效果、备用
        /// </summary>
        private void EnableBlur()
        {
            var windowHelper = new WindowInteropHelper(this);
            var accent = new AccentPolicy
            {
                AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
                GradientColor = (_blurOpacity << 24) | (_blurBackgroundColor & 0xFFFFFF)
            };
            var accentStructSize = Marshal.SizeOf(accent);
            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);
            var data = new WindowCompositionAttributeData
            {
                Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                SizeOfData = accentStructSize,
                Data = accentPtr
            };

            SetWindowCompositionAttribute(windowHelper.Handle, ref data);
            Marshal.FreeHGlobal(accentPtr);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            WindowEventHandler("close button");
        }
    }
    #region interop declare
    internal enum AccentState
    {
        ACCENT_DISABLED = 0,
        ACCENT_ENABLE_GRADIENT = 1,
        ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
        ACCENT_ENABLE_BLURBEHIND = 3,
        ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
        ACCENT_INVALID_STATE = 5
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct AccentPolicy
    {
        public AccentState AccentState;
        public uint AccentFlags;
        public uint GradientColor;
        public uint AnimationId;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WindowCompositionAttributeData
    {
        public WindowCompositionAttribute Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    internal enum WindowCompositionAttribute
    {
        // ...
        WCA_ACCENT_POLICY = 19
        // ...
    }
    #endregion
}
