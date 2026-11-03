using II.RPA.Core.Common;
using II.RPA.Core.Log;
using II.RPA.Plugin.Core;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace II.RPA.Core.Provider.PluginProvider
{
    public class ScreenSaver
    {
        private readonly UIThreadProvider uIThreadProvider;
        private ScreenSaveWindow screenSaveWindow;
        private readonly ILogger m_Logger;
        private volatile string _customPwd = null;
        public ScreenSaver(ILogger logger)
        {
            uIThreadProvider = new UIThreadProvider();
            IsShowing = false;
            m_Logger = logger;
        }
        public bool IsShowing { get; private set; }
        public bool UseCustomPwd => !string.IsNullOrEmpty(_customPwd);
        public async Task ShowSaveWindow(string aTip,
            bool anAutoUnlock,
            string customPwd = null,
            int effect = 2)
        {
            if (IsShowing)
            {
                await uIThreadProvider.UIDispatcher.InvokeAsync(() =>
                {
                    screenSaveWindow.Close();
                    screenSaveWindow = null;
                });
            }
            _customPwd = customPwd;
            screenSaveWindow = BuildSreenSaveWindow(aTip, anAutoUnlock, effect, _customPwd);
            await uIThreadProvider.UIDispatcher.InvokeAsync(() => { screenSaveWindow.Show(); });
            IsShowing = true;
        }
        public async Task StopSaveWindow(string aStopTip)
        {
            await uIThreadProvider.UIDispatcher.InvokeAsync(() => { screenSaveWindow.SwitchToStop(aStopTip); });
            IsShowing = false;
        }
        private ScreenSaveWindow BuildSreenSaveWindow(string aTip, bool anAutoUnlock, int effect = 2, string customPwd = null)
        {
            return uIThreadProvider.UIDispatcher.Invoke(() => 
            { 
                return new ScreenSaveWindow(aTip, anAutoUnlock, m_Logger, effect, customPwd); 
            });
        }
        public void SetWindowTransparent(Window window, bool aTransparent)
        {
            IntPtr hwnd = new WindowInteropHelper(window).Handle;
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
    }
}
