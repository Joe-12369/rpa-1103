using Gma.UserActivityMonitor;
using II.RPA.Logging;
using II.RPA.Plugin.GUI.ProcessCreator.ActionProvider;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace II.RPA.Plugin.GUI.ProcessCreator
{
    /// <summary>
    /// Interaction logic for ActionPopupWindow.xaml
    /// </summary>
    public partial class ActionPopupWindow : Window
    {
        private static readonly ILogger logger = Logger.OfContext();
        public ActionViewModelBase m_ViewModel;
        private bool _inSearching;
        private readonly IntPtr m_RootHandle;
        #region interop declare
        private readonly uint _blurOpacity = 0;
        private readonly uint _blurBackgroundColor = 0xFFFFFF;
        public const int WM_CLOSE = 0x10;
        [DllImport("user32.dll", EntryPoint = "SendMessageA")]
        public static extern int SendMessage(IntPtr hwnd, int wMsg, int wParam, int lParam);
        [DllImport("user32.dll")]
        internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);
        #endregion
        private bool m_IsActionOver = false;

        private readonly bool m_IsWindows11Higher = false;
        public ActionPopupWindow(ActionViewModelBase aViewModel,IntPtr aRootHandle)
        {
            InitializeComponent();
            WinAutomation.CommonUtility.XamlResource.LanguageResourceHelper.GetInstance().SetAppInstance(this);
            m_RootHandle = aRootHandle;
            SourceInitialized += ActionPopupWindow_SourceInitialized;
            Loaded += ActionPopupWindow_Loaded;
            Closed += ActionPopupWindow_Closed;
            m_ViewModel = aViewModel;
            m_ViewModel.CloseWindowActionEvent += WindowEventHandler;
            DataContext = m_ViewModel;
            //resultList.Visibility = Visibility.Collapsed;

            logger.LogInformation($"OSVersion:{Environment.OSVersion.Platform};{Environment.OSVersion.VersionString};{Environment.OSVersion.Version.Build}");
            if ((Environment.OSVersion.Version.Build >= 22000)
                || Environment.OSVersion.Version.Major > 10)
            {
                m_IsWindows11Higher = true;
            }
            else
            {
                ChromeBorder.Background = new SolidColorBrush(Color.FromArgb(210, 250, 250, 250));
            }
        }

        /// <summary>
        /// Explorer 右键菜单场景下注册的扩展鼠标钩子需在 <see cref="Closed"/>、<see cref="WindowEventHandler"/> 等路径卸掉；
        /// 不在 <c>Closing</c> 中卸除，避免 <c>e.Cancel == true</c> 时窗口仍打开但钩子已卸。
        /// </summary>
        private void DetachExplorerMouseClickHook()
        {
            HookManager.MouseClickExt -= HookManager_MouseClickExt;
        }

        private void ActionPopupWindow_Closed(object sender, EventArgs e)
        {
            logger.LogDebug("ActionPopupWindow_Closed");
            SourceInitialized -= ActionPopupWindow_SourceInitialized;
            DetachExplorerMouseClickHook();
            Loaded -= ActionPopupWindow_Loaded;
            Closed -= ActionPopupWindow_Closed;
            Resources.Clear();
            if (m_ViewModel != null)
            {
                m_ViewModel.CloseWindowActionEvent -= WindowEventHandler;
                m_ViewModel = null;
            }
        }

        private void ActionPopupWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (m_IsWindows11Higher)
            {
                //EnableBlur();
                EnableBlur(this);
            }
        }

        private void ActionPopupWindow_SourceInitialized(object sender, EventArgs e)
        {
            if (m_IsWindows11Higher)
            {
                IntPtr hWnd = new WindowInteropHelper(GetWindow(this)).EnsureHandle();
                var attribute = DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE;
                var preference = DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
                DwmSetWindowAttribute(hWnd, attribute, ref preference, sizeof(uint));
            }
            this.MoverCenteredToMouse();
            try
            {
                DetachExplorerMouseClickHook();
                if (!string.IsNullOrEmpty(m_ViewModel.ActionParam.ProcessName)
                    && m_ViewModel.ActionParam.ProcessName.ToLower().StartsWith("explorer"))
                {
                    HookManager.MouseClickExt += HookManager_MouseClickExt;
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "ActionPopupWindow_SourceInitialized");
            }
        }

        private void HookManager_MouseClickExt(object sender, MouseEventExtArgs e)
        {
            try
            {
                if (SomeUtils.IsWindowsContextMenu(JObject.Parse(m_ViewModel.DoSelectorResult.Selector)))
                {
                    logger.LogDebug($"ExplorerContextMenu:{m_RootHandle}");
                    //SendMessage(m_RootHandle, WM_CLOSE, 0, 0);
                    SendKeys.SendWait("{ESC}");


                }
                else
                {
                    logger.LogDebug($"Not ExplorerContextMenu:{m_RootHandle}");
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "ActionPopupWindow.HookManager_MouseClickExt");
            }
        }

        public void WindowEventHandler(string aParam)
        {
            DetachExplorerMouseClickHook();
            SourceInitialized -= ActionPopupWindow_SourceInitialized;
            Loaded -= ActionPopupWindow_Loaded;
            m_ViewModel.CloseWindowActionEvent -= WindowEventHandler;
            m_ViewModel = null;
            if (!m_IsActionOver)
            {
                m_IsActionOver = true;
                logger.LogInformation($"close reason:{aParam}");
                Dispatcher.Invoke(() =>
                {
                    Close();
                });
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            recommendList.Visibility = Visibility.Collapsed;
            CancelBtn.Visibility = Visibility.Visible;
            DummyButton.Visibility = Visibility.Collapsed;
            //resultList.Visibility = Visibility.Collapsed;
            SearchKey.Clear();
            SearchKey.Focus();
            _inSearching = true;
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            CancelBtn.Visibility=Visibility.Collapsed;
            DummyButton.Visibility = Visibility.Visible;
            recommendList.Visibility = Visibility.Visible;
            //if (m_ViewModel is ActionPopupViewModel viewModel)
            //{
            //    viewModel.Filter = string.Empty;
            //}
            if (m_ViewModel is ActionPopupViewModel viewModel)
            {
                viewModel.RaiseNeedHoldEvent(false);
            }
            _inSearching = false;
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
        public enum DWMWINDOWATTRIBUTE
        {
            DWMWA_WINDOW_CORNER_PREFERENCE = 33
        }

        public enum DWM_WINDOW_CORNER_PREFERENCE
        {
            DWMWCP_DEFAULT = 0,
            DWMWCP_DONOTROUND = 1,
            DWMWCP_ROUND = 2,
            DWMWCP_ROUNDSMALL = 3
        }

        // Import dwmapi.dll and define DwmSetWindowAttribute in C# corresponding to the native function.
        [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern long DwmSetWindowAttribute(IntPtr hwnd,
                                                         DWMWINDOWATTRIBUTE attribute,
                                                         ref DWM_WINDOW_CORNER_PREFERENCE pvAttribute,
                                                         uint cbAttribute);
        #endregion
        private static void EnableBlur(Window window)
        {
            var windowHelper = new WindowInteropHelper(window);

            var accent = new AccentPolicy
            {
                AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND
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

        private void SearchKey_GotFocus(object sender, RoutedEventArgs e)
        {
            if (m_ViewModel is ActionPopupViewModel viewModel)
            {
                viewModel.RaiseNeedHoldEvent(true);
            }

        }

        private void SearchKey_LostFocus(object sender, RoutedEventArgs e)
        {
            //if (m_ViewModel is ActionPopupViewModel viewModel)
            //{
            //    viewModel.RaiseNeedHoldEvent(false);
            //}
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            if (m_ViewModel is ActionPopupViewModel viewModel)
            {
                if (_inSearching)
                {
                    viewModel.RaiseNeedHoldEvent(false);
                }
            }
        }
    }
}

