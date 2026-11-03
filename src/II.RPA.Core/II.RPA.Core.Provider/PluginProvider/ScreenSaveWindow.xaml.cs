using II.RPA.Core.Common;
using II.RPA.Core.Log;
using II.RPA.Core.Services;
using II.RPA.Plugin.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace II.RPA.Core.Provider.PluginProvider
{
    /// <summary>
    /// ScreenSaveWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ScreenSaveWindow : Window
    {
        private readonly bool autoUnlock;
        private readonly string _configFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Z-Factory", "storage.json");
        private string _runKey = "F5";
        private string _stopKey = "Ctrl+F5";
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;
        private readonly int _effect;
        private readonly string _customPwd = null;
        public ScreenSaveWindow(string aTip,
            bool anAutoUnlock,
            ILogger logger,
            int effect = 2,
            string customPwd = null)
        {
            InitializeComponent();
            _customPwd = customPwd;
            _logger = logger;
            _effect = effect;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Accept", "*/*");
            _httpClient.Timeout = TimeSpan.FromMilliseconds(2000);

            autoUnlock = anAutoUnlock;
            if (aTip.Length > 150)
            {
                aTip = $"{aTip.Substring(0, 150)}...";
            }
            tipText.Text = aTip;

            passwordBorder.Visibility = Visibility.Collapsed;
            enter.Visibility = Visibility.Collapsed;
            borderRestart.Visibility = Visibility.Collapsed;
            borderStop.Visibility = Visibility.Visible;

            SourceInitialized += ScreenSaveWindow_SourceInitialized;

            if (Global.Client == ClientID.Factory)
            {
                SetFactoryConfig();
            }
            else if (Global.Client == ClientID.Bot)
            {
                SetBotConfig();
            }



        }
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 0x0085 /* WM_NCPAINT */ || msg == 0x0086 /* WM_NCCALCSIZE */)
            {
                handled = true;
                return IntPtr.Zero;
            }
            return IntPtr.Zero;
        }
        private string GetCurrentUserName()
        {
            WindowsIdentity windowsIdentity = WindowsIdentity.GetCurrent();
            if (windowsIdentity.Name.Contains("\\"))
            {
                string name = windowsIdentity.Name.Split('\\').Last();
                return name;
            }
            return windowsIdentity.Name;
        }
        private string GetCurrentDomainName()
        {
            WindowsIdentity windowsIdentity = WindowsIdentity.GetCurrent();
            if (windowsIdentity.Name.Contains("\\"))
            {
                string name = windowsIdentity.Name.Split('\\').First();
                return name;
            }
            return Environment.MachineName;
        }
        public void SwitchToStop(string aStopTip)
        {
            if (!autoUnlock && !string.IsNullOrEmpty(aStopTip))
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                SetWindowTransparent(hwnd, false);
                tipText.Text = aStopTip;
                //borderRestart.Visibility = Visibility.Visible;
                borderStop.Visibility = Visibility.Collapsed;

                if (IsEmptyWindowPwd())
                {
                    passwordBorder.Visibility = Visibility.Collapsed;
                    enter.Visibility = Visibility.Visible;
                }
                else
                {
                    passwordBorder.Visibility = Visibility.Visible;
                    enter.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                Close();
            }
        }

        private void ScreenSaveWindow_SourceInitialized(object sender, EventArgs e)
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            if (_effect == 2)
            {
                if (Environment.OSVersion.Version < new Version(10, 0))
                {
                    EnableBlur7(hwnd);
                    mainGrid.Background = new SolidColorBrush(Color.FromArgb(225, 0, 0, 0));
                }
                else
                {
                    EnableBlur10(hwnd);
                }
            }
            else
            {
                //WindowStyle = WindowStyle.None;
                //Background = null;
            }
            SetWindowTransparent(hwnd, true);
            SetWindowToolStyle(hwnd);
            //var source = HwndSource.FromHwnd(hwnd);
            //source.AddHook(WndProc);
            Topmost = true;

        }
        private void SetWindowTransparent(IntPtr hwnd, bool aTransparent)
        {
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
        private void SetWindowToolStyle(IntPtr hwnd)
        {
            uint extendedStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
            NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, extendedStyle | NativeMethods.WS_EX_TOOLWINDOW);
        }

        #region blur
        public static void EnableBlur7(IntPtr hwnd)
        {
            DWM_BLURBEHIND blurBehind = new DWM_BLURBEHIND
            {
                dwFlags = 1 | 2, // DWM_BB_ENABLE | DWM_BB_BLURREGION
                fEnable = true,
                hRgnBlur = IntPtr.Zero
            };
            DwmEnableBlurBehindWindow(hwnd, ref blurBehind);
        }
        public static void EnableBlur10(IntPtr hwnd)
        {
            AccentPolicy accent = default(AccentPolicy);
            accent.AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND;
            ChangeOverlayAccentState(hwnd, accent);
        }
        public static void DisableBlur10(IntPtr hwnd)
        {
            AccentPolicy accent = default(AccentPolicy);
            accent.AccentState = AccentState.ACCENT_ENABLE_GRADIENT;
            ChangeOverlayAccentState(hwnd, accent);
        }
        private static void ChangeOverlayAccentState(IntPtr hwnd, AccentPolicy accent)
        {
            int accentStructSize = Marshal.SizeOf(accent);
            IntPtr accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, fDeleteOld: false);
            WindowCompositionAttributeData data = default(WindowCompositionAttributeData);
            data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
            data.SizeOfData = accentStructSize;
            data.Data = accentPtr;
            SetWindowCompositionAttribute(hwnd, ref data);
            Marshal.FreeHGlobal(accentPtr);
        }
        #endregion

        #region interop declare
        const int HWND_TOPMOST = -1;
        const int SWP_NOSIZE = 0x0001;
        const int SWP_NOMOVE = 0x0002;
        const int SWP_SHOWWINDOW = 0x0040;


        [DllImport("dwmapi.dll")]
        public static extern int DwmEnableBlurBehindWindow(IntPtr hWnd, ref DWM_BLURBEHIND blurBehind);

        [StructLayout(LayoutKind.Sequential)]
        public struct DWM_BLURBEHIND
        {
            public uint dwFlags;
            public bool fEnable;
            public IntPtr hRgnBlur;
            public bool fTransitionOnMaximized;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetWindowPos(IntPtr hwnd, int hwndInsertAfter, int x, int y, int cx, int cy, int uFlags);
        [DllImport("user32.dll", EntryPoint = "SendMessageA")]
        public static extern int SendMessage(IntPtr hwnd, int wMsg, int wParam, int lParam);
        [DllImport("user32.dll")]
        internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);
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

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool LogonUser(string lpszUsername,
           string lpszDomain,
           string lpszPassword,
           int dwLogonType,
           int dwLogonProvider,
           out IntPtr phToken
           );

        [DllImport("kernel32.dll")]
        public static extern int FormatMessage(int dwFlags, ref IntPtr lpSource, int dwMessageId, int dwLanguageId, ref String lpBuffer, int nSize, ref IntPtr Arguments);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);


        public static string GetErrorMessage(int errorCode)
        {
            int FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x100;
            int FORMAT_MESSAGE_IGNORE_INSERTS = 0x200;
            int FORMAT_MESSAGE_FROM_SYSTEM = 0x1000;

            int msgSize = 255;
            string lpMsgBuf = null;
            int dwFlags = FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS;

            IntPtr lpSource = IntPtr.Zero;
            IntPtr lpArguments = IntPtr.Zero;
            int returnVal = FormatMessage(dwFlags, ref lpSource, errorCode, 0, ref lpMsgBuf, msgSize, ref lpArguments);

            if (returnVal == 0)
            {
                throw new Exception("Failed to format message for error code " + errorCode.ToString() + ". ");
            }
            return lpMsgBuf;

        }
        #endregion

        private bool WindowsIdentityVerify(string aUserName, string aPassword, string aMachineName)
        {
            IntPtr tokenHandle = new IntPtr(0);
            try
            {
                string UserName = null;
                string MachineName = null;
                string Pwd = null;

                MachineName = aMachineName;
                UserName = aUserName;
                Pwd = aPassword;

                _logger.LogInfo($"WindowsIdentityVerify:{MachineName};{UserName};{Pwd}");

                const int LOGON32_PROVIDER_DEFAULT = 0;
                const int LOGON32_LOGON_INTERACTIVE = 2;
                tokenHandle = IntPtr.Zero;

                bool returnValue = LogonUser(UserName, MachineName, Pwd, LOGON32_LOGON_INTERACTIVE, LOGON32_PROVIDER_DEFAULT, out tokenHandle);

                if (returnValue == false)
                {
                    int ret = Marshal.GetLastWin32Error();
                    if (ret == 1327)
                    {
                        _logger.LogError($"LogonUser:empty password!");
                        return true;
                    }
                    else
                    {
                        string errmsg = GetErrorMessage(ret);
                        _logger.LogError($"LogonUser:{errmsg}:{ret}");
                        return false;
                    }
                }
                else
                {

                    WindowsIdentity newId = new WindowsIdentity(tokenHandle);
                    WindowsPrincipal userperm = new WindowsPrincipal(newId);
                    if (userperm.IsInRole(WindowsBuiltInRole.Administrator))
                    {
                        //"Access Granted. User is admin"
                    }
                    else
                    {
                        //"Access Granted. User is not admin"
                    }
                    return true;
                }
            }
            catch (Exception)
            {
                //
                return false;
            }
            finally
            {
                if (tokenHandle != IntPtr.Zero)
                {
                    CloseHandle(tokenHandle);
                }
            }

        }
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Alt && e.SystemKey == Key.Space)
            {
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Alt && e.SystemKey == Key.F4)
            {
                e.Handled = true;
            }
            else
            {
                base.OnKeyDown(e);
            }
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_customPwd))
            {
                _logger.LogInfo($"Custom password verify:{_customPwd};{passwordBox.Password}");
                if (CustomVerify(passwordBox.Password, _customPwd))
                {
                    Close();
                    return;
                }
                else
                {
                    erroText.Visibility = Visibility.Visible;
                    passwordBox.BorderThickness = new Thickness(0);
                    errorBorder.Visibility = Visibility.Visible;
                    return;
                }
            }
            if (!string.IsNullOrEmpty(passwordBox.Password))
            {
                if (WindowsIdentityVerify(GetCurrentUserName(), passwordBox.Password, GetCurrentDomainName()))
                {
                    this.Close();
                }
                else
                {
                    erroText.Visibility = Visibility.Visible;
                    passwordBox.BorderThickness = new Thickness(0);
                    errorBorder.Visibility = Visibility.Visible;
                }
            }
            else
            {
                if (IsEmptyWindowPwd())
                {
                    this.Close();
                }
            }
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(passwordBox.Password))
            {
                erroText.Visibility = Visibility.Collapsed;
                errorBorder.Visibility = Visibility.Collapsed;
                passwordBox.BorderThickness = new Thickness(1);

                passwordBox.Tag = "请输入密码";
            }
            else
            {
                passwordBox.Tag = string.Empty;
            }
        }

        private void Enter_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private bool ReadJsonConfig(out string runKey, out string stopKey)
        {
            runKey = string.Empty;
            stopKey = string.Empty;

            if (!File.Exists(_configFilePath))
            {
                return false;
            }
            else
            {
                try
                {
                    using (FileStream fileStream = File.Open(_configFilePath, FileMode.Open, FileAccess.Read))
                    {
                        using (StreamReader sw = new StreamReader(fileStream))
                        {
                            using (JsonTextReader jw = new JsonTextReader(sw))
                            {
                                JsonSerializer serializer = new JsonSerializer();
                                var configObject = serializer.Deserialize<JObject>(jw);
                                if (configObject.TryGetValue("CUSTOM_KEYBINDING_RULES_KEY",
                                    StringComparison.OrdinalIgnoreCase,
                                    out JToken valule))
                                {
                                    if (valule is JArray jArray)
                                    {
                                        var runNode = jArray.FirstOrDefault(j => j["commandId"] != null && j["commandId"].ToString().EqualIgnoreCase("run"));
                                        runKey = runNode.Value<string>("keyBinding");

                                        var stopNode = jArray.FirstOrDefault(j => j["commandId"] != null && j["commandId"].ToString().EqualIgnoreCase("stop"));
                                        stopKey = stopNode.Value<string>("keyBinding");
                                    }
                                    return true;
                                }
                                else
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }
        private void SetFactoryConfig()
        {
            if (ReadJsonConfig(out string runKey, out string stopKey))
            {
                _runKey = runKey;
                _stopKey = stopKey;
            }
            runTxt.Text = _runKey.ToUpper();
            stopTxt.Text = _stopKey.ToUpper();
        }
        private async void SetBotConfig()
        {
            var stopKey = await BotService.Singleton.GetConfigOfStopKey();
            _runKey = "";
            _stopKey = stopKey;
            runTxt.Text = _runKey.ToUpper();
            stopTxt.Text = _stopKey.ToUpper();
        }

        private static bool CustomVerify(string password, string customPwd)
        {
            return password == customPwd && !string.IsNullOrEmpty(password);
        }

        private bool IsEmptyWindowPwd()
        {
            return string.IsNullOrEmpty(_customPwd) && WindowsIdentityVerify(GetCurrentUserName(), "", GetCurrentDomainName());
        }
    }
}
