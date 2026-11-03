using Gma.UserActivityMonitor;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Imaging;
using II.RPA.Plugin.Core;
using II.RPA.WinAutomation.CommonUtility;
using II.RPA.Plugin.GUI.Params;
using WpfAnimatedGif;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using II.RPA.Logging;
using System.Windows.Interop;
using System.Windows.Media;

namespace II.RPA.Plugin.GUI
{
    public partial class MousePosition : Window
    {
        #region 变量
        DateTime _lastKeyDownTime;
        List<Keys> _hookKeys;
        private bool isPause = false;
        public bool IsPause
        {
            get { return isPause; }
            set { isPause = value; }
        }
        internal bool IsClosed { get; set; }
        #endregion
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int GWL_EXSTYLE = (-20);
        private static ILogger logger = Logger.OfContext();

        [DllImport("user32", EntryPoint = "SetWindowLong")]
        private static extern uint SetWindowLong(IntPtr hwnd, int nIndex, uint dwNewLong);

        private string SceneName { get; set; } = null;
        public MousePosition()
        {
            InitializeComponent();

            this.SourceInitialized += MousePosition_SourceInitialized;
            WinAutomation.CommonUtility.XamlResource.LanguageResourceHelper.GetInstance().SetAppInstance(this);
            var screenPoint = System.Windows.Forms.Cursor.Position;
            tb_PositionX.Text = screenPoint.X.ToString();
            tb_PositionY.Text = screenPoint.Y.ToString();
            _lastKeyDownTime = DateTime.Now;
        }

        public void SetWindowTransparent(bool aTransparent)
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            uint extendedStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
            if (aTransparent)
            {
                NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, extendedStyle | NativeMethods.WS_EX_TRANSPARENT);
            }
            else
            {
                NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, extendedStyle & ~NativeMethods.WS_EX_TRANSPARENT);
            }
        }
        public void MousePosition_SourceInitialized(object sender, EventArgs e)
        {
            SetWindowTransparent(true);
            IntPtr wptr = new WindowInteropHelper(this).Handle;
            HwndSource hs = HwndSource.FromHwnd(wptr);
            hs.AddHook(new HwndSourceHook(WndProc));
        }
        protected IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            //if (msg == NativeMethods.WM_NCHITTEST)
            //{
            //    return (IntPtr)NativeMethods.HTTRANSPARENT;
            //}
            return (IntPtr.Zero);
            //return (IntPtr)NativeMethods.HTTRANSPARENT;
        }
        public MousePosition(string sceneName)
        {
            InitializeComponent();
            WinAutomation.CommonUtility.XamlResource.LanguageResourceHelper.GetInstance().SetAppInstance(this);
            var screenPoint = System.Windows.Forms.Cursor.Position;
            tb_PositionX.Text = screenPoint.X.ToString();
            tb_PositionY.Text = screenPoint.Y.ToString();

            ChangeCaptureStatusStyle(sceneName);
            _hookKeys = new List<Keys>() { Keys.Escape, Keys.F2, Keys.F3 };
        }
        internal void SetWndHide()
        {
            try
            {
                if (!this.CheckAccess())
                {
                    this.Dispatcher.Invoke((System.Windows.Forms.MethodInvoker)delegate
                    {
                        SetWndHide();
                    });
                    return;
                }
                this.Hide();
                UninstallHook();
            }
            catch (Exception ex)
            {
                logger.LogDebug("设置状态窗口隐藏异常，忽略");
            }
        }
        internal void SetWndShow()
        {

            try
            {
                if (!this.CheckAccess())
                {
                    this.Dispatcher.Invoke((System.Windows.Forms.MethodInvoker)delegate
                    {
                        SetWndShow();
                    });
                    return;
                }
                this.Show();
                InstallHook();
            }
            catch (Exception ex)
            {
                logger.LogDebug("设置状态窗口显示异常，忽略");
            }

        }
        public void ChangeCaptureStatusStyle(string sceneName = null)
        {
            try
            {

                if (!txt_Status.CheckAccess())
                {
                    txt_Status.Dispatcher.Invoke((System.Windows.Forms.MethodInvoker)delegate
                    {
                        ChangeCaptureStatusStyle(sceneName = null);
                    });
                    return;
                }


            }
            catch (Exception ex)
            {
                //trace
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"改变拾取状态 窗口效果时发生异常，一般情况下忽略此问题。");
            }
        }
        internal void InstallHook()
        {
            UninstallHook();
            HookManager.KeyDown += HookManager_KeyUp;//HookManager_KeyUp;
            HookManager.MouseDown += HookManager_MouseDown;
        }

        internal void UninstallHook()
        {
            HookManager.KeyDown -= HookManager_KeyUp;
            HookManager.MouseDown -= HookManager_MouseDown;
        }
        private void HookManager_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:
                    if ((System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
                    {
                        if (!this.CheckAccess())
                        {
                            this.Dispatcher.Invoke((System.Windows.Forms.MethodInvoker)delegate
                            {
                                GuiElement.PositionForm.DialogResult = true;
                                GuiElement.PositionForm.StopHighlight();
                            });
                        }
                        else
                        {
                            GuiElement.PositionForm.DialogResult = true;
                            GuiElement.PositionForm.StopHighlight();
                        }
                    }
                    break;
                case MouseButtons.Right:
                    if (!this.CheckAccess())
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            this.Close();
                            GuiElement.PositionForm.Close();
                        });
                    }
                    else
                    {
                        this.Close();
                        GuiElement.PositionForm.Close();
                    }
                    break;
                default:
                    break;
            }
        }

        private void HookManager_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            try
            {
                //var span = (DateTime.Now - _lastKeyDownTime).TotalMilliseconds;
                //_lastKeyDownTime = DateTime.Now;
                //if (span < 200)
                //{
                //    e.Handled = _hookKeys.Contains(e.KeyCode);
                //    return;
                //}
                switch (e.KeyCode)
                {
                    case Keys.Escape:
                        e.Handled = true;
                        if (!this.CheckAccess())
                        {
                            this.Dispatcher.Invoke(() =>
                            {
                                GuiElement.PositionForm.Close();
                            });
                        }
                        else
                        {
                            GuiElement.PositionForm.Close();
                        }
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, "按键事件处理发生了一个异常：MousePosition（注册在 KeyDown 上的键盘回调）");
            }
        }

        private void SetPanelVisibility(FrameworkElement panel, bool isVisible = false)
        {
            try
            {
                if (!panel.CheckAccess())
                {
                    this.Dispatcher.Invoke((System.Windows.Forms.MethodInvoker)delegate
                    {
                        SetPanelVisibility(panel, isVisible);
                    });
                    return;
                }
                if (!isVisible)
                {
                    if (panel.Visibility == Visibility.Visible)
                    {
                        var height = panel.Height;
                        this.Height -= height;
                    }
                    panel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    if (panel.Visibility == Visibility.Collapsed)
                    {
                        var height = panel.Height;
                        this.Height += height;
                    }
                    panel.Visibility = Visibility.Visible;
                }
            }
            catch (ObjectDisposedException ex)
            {
            }
            catch (Exception ex)
            {
            }
        }
        public void SetCVCaptureMsg(string msg = "")
        {
            try
            {
                if (!this.CheckAccess())
                {
                    this.Dispatcher.Invoke((System.Windows.Forms.MethodInvoker)delegate
                    {
                        SetCVCaptureMsg(msg);
                    });
                    return;
                }
                if (string.IsNullOrEmpty(msg))
                {
                    SetPanelVisibility(rect_loadingLine);
                    SetPanelVisibility(sp_loading);
                    SetCursorBusy(false);
                }
                else
                {
                    SetPanelVisibility(rect_loadingLine, true);
                    SetPanelVisibility(sp_loading, true);
                    SetCursorBusy(true);
                }
            }
            catch (ObjectDisposedException ex)
            {

            }
            //l_captureMsg.Text = msg;
        }
        private void SetCursorBusy(bool isBusy)
        {
            try
            {
                if (isBusy)
                {
                    var cursorFilePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(this.GetType().Assembly.Location), "aero_working.ani");
                    var curHanble = NativeMethods.LoadCursorFromFile(cursorFilePath);
                    NativeMethods.SetSystemCursor(NativeMethods.CopyIcon(curHanble), NativeMethods.OCR_NORMAL);
                    NativeMethods.SetSystemCursor(NativeMethods.CopyIcon(curHanble), NativeMethods.OCR_IBEAM);
                }
                else
                {
                    NativeMethods.SystemParametersInfo(0x0057, 0, null, 0);
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, $"{(isBusy ? "设置" : "取消")}鼠标后台运行效果异常");
            }
        }

        public void HideSwitchPanle()
        {
            try
            {
                if (!this.CheckAccess())
                {
                    this.Dispatcher.Invoke((System.Windows.Forms.MethodInvoker)delegate
                    {
                        HideSwitchPanle();
                    });
                    return;
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, "隐藏拾取模式提示区域异常");
            }
        }


        internal System.Drawing.Rectangle GetWndRectangle()
        {
            return new System.Drawing.Rectangle();
        }



        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            IsClosed = true;
            UninstallHook();
            SetCursorBusy(false);
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }
    }
}


