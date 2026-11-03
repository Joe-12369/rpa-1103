using II.RPA.WinAPI;
using II.RPA.WinAPI.Struct;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace II.RPA.Plugin.DesktopInput.InputMethod.Commons
{
    internal class ImmHelper
    { 
        internal static WinAPI.Struct.GUITHREADINFO GetFocusImState(IntPtr hwnd)
        {
            uint windowThreadProcessId = User32.GetWindowThreadProcessId(hwnd, IntPtr.Zero);
            WinAPI.Struct.GUITHREADINFO guiThreadInfo = default(WinAPI.Struct.GUITHREADINFO);
            guiThreadInfo.cbSize = Marshal.SizeOf<WinAPI.Struct.GUITHREADINFO>(guiThreadInfo);
            User32.GetGUIThreadInfo(windowThreadProcessId, ref guiThreadInfo);
            return guiThreadInfo;
        }

        public static void SetWindowIme(IntPtr hWnd = default(IntPtr), ImmHelper.IMELang targetLang = ImmHelper.IMELang.English)
        {
            if (hWnd == IntPtr.Zero)
            {
                hWnd = User32.GetForegroundWindow();
            }
            if (ImmHelper.IsWindowImeUserDefined(hWnd))
            {
                ImmHelper.TryLoadKLIDForWindow(hWnd, targetLang);
                return;
            }
            IntPtr imeHwnd = ImmHelper.GetWindowImeWnd(hWnd);
            ImmHelper.IMELang currentLang = ImmHelper.GetWindowIme((IntPtr)0);
            if (currentLang == ImmHelper.IMELang.Chinese && targetLang == ImmHelper.IMELang.English)
            {
                User32.SendMessage(imeHwnd, 643U, ((IntPtr)6), (IntPtr)0);
                return;
            }
            if (currentLang == ImmHelper.IMELang.English && targetLang == ImmHelper.IMELang.Chinese)
            {
                User32.SendMessage(imeHwnd, 643U, ((IntPtr)6), (IntPtr)1);
            }
        }

        public static ImmHelper.IMELang GetWindowIme(IntPtr hWnd = default(IntPtr))
        {
            if (hWnd == IntPtr.Zero)
            {
                hWnd = User32.GetForegroundWindow();
            }
            if (ImmHelper.IsWindowImeUserDefined(hWnd))
            {
                return ImmHelper.IMELang.Unknow;
            }
            if (User32.SendMessage(ImmHelper.GetWindowImeWnd(hWnd), 643U, ((IntPtr)5), IntPtr.Zero) == 1)
            {
                return ImmHelper.IMELang.Chinese;
            }
            return ImmHelper.IMELang.English;
        }

        public static void TryLoadKLIDForWindow(IntPtr hWnd = default(IntPtr), ImmHelper.IMELang imeMode = ImmHelper.IMELang.English)
        {
            if (hWnd == IntPtr.Zero)
            {
                hWnd = User32.GetForegroundWindow();
            }
            IntPtr pwszKLID = User32.LoadKeyboardLayout(((imeMode == ImmHelper.IMELang.English) ? new StringBuilder("08040804") : new StringBuilder("00000804")).ToString(), 1);
            User32.SendMessage(hWnd, 80U, IntPtr.Zero, pwszKLID);
        }

        private static IntPtr GetWinKeyboardLayout(IntPtr hWnd)
        {
            IntPtr result;
            try
            {
                result = User32.GetKeyboardLayout(User32.GetWindowThreadProcessId(hWnd, IntPtr.Zero));
            }
            catch
            {
                result = new IntPtr(1033);
            }
            return result;
        }

        public static IntPtr GetWindowImeWnd(IntPtr hWnd)
        {
            uint windowThreadProcessId = User32.GetWindowThreadProcessId(hWnd, IntPtr.Zero);
            GUITHREADINFO guiThreadInfo = default(GUITHREADINFO);
            guiThreadInfo.cbSize = Marshal.SizeOf<GUITHREADINFO>(guiThreadInfo);
            User32.GetGUIThreadInfo(windowThreadProcessId, ref guiThreadInfo);
            return Imm32.ImmGetDefaultIMEWnd(guiThreadInfo.hwndFocus);
        }

        internal static bool IsWindowImeUserDefined(IntPtr hWnd)
        {
            IntPtr hkl = GetWinKeyboardLayout(hWnd);
            long primaryLanguageId = (long)(hkl.ToInt32() & 16711680);
            bool flag = (primaryLanguageId == 2097152L || primaryLanguageId > 33554432L) && (primaryLanguageId < 67043328L || primaryLanguageId == 67043328L);
            long subLanguageId = (long)hkl.ToInt32() & (long)(-16777216);
            bool isSubUserDefined = (subLanguageId == 2097152L || subLanguageId > 2097152L) && (subLanguageId < 4128768L || subLanguageId == 4128768L);
            return flag || isSubUserDefined;
        }

        public enum IMELang
        {
            English,
            Chinese,
            Unknow
        }
    }
}
