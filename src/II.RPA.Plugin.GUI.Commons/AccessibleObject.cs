using Accessibility;
using FlaUI.Core.AutomationElements;
using System;
using System.Runtime.InteropServices;
using FlaUI.Core.WindowsAPI;

namespace II.RPA.Plugin.GUI.Commons
{
    [Flags]
    public enum SEND_MESSAGE_TIMEOUT_FLAGS : uint
    {
        SMTO_ABORTIFHUNG = 2U,
        SMTO_BLOCK = 1U,
        SMTO_NORMAL = 0U,
        SMTO_NOTIMEOUTIFNOTHUNG = 8U,
        SMTO_ERRORONEXIT = 32U
    }

    public class AccessibleObject
    {
        const int kIdScreenReaderHoneyPot = 1;

        public static void EnableAccessibility(AutomationElement win)
        {
            if (!IsChromiumRenderWidgetHostHWND(win))
            {
                return;
            }
            try
            {
                //Chromium渲染窗口的自动化接口激活条件：
                //【前提】命令行参数中不含"--disable-renderer-accessibility"
                //【激活选项1】IAccessible2或者其他IAccessibleXXX(非IAccessible)拓展接口被使用了
                //【激活选项2】Windows屏幕阅读器被启用了，且调用过了根IAccessible对象的get_accName函数
                string name = win.Name;
                if (SendMessageTimeout(win.Properties.NativeWindowHandle, WindowsMessages.WM_GETOBJECT, (IntPtr)0, new IntPtr(kIdScreenReaderHoneyPot), SEND_MESSAGE_TIMEOUT_FLAGS.SMTO_ABORTIFHUNG | SEND_MESSAGE_TIMEOUT_FLAGS.SMTO_BLOCK, 1000U, IntPtr.Zero) == IntPtr.Zero)
                {
                    //WindowsException exc = new WindowsException(Marshal.GetLastWin32Error());
                }

            }
            catch (Exception e)
            {
                //Logging.Warn("激活窗口失败：" + win.Handle.ToString("x2") + " : " + e.Message);
            }
        }
        private static bool IsChromiumRenderWidgetHostHWND(AutomationElement wnd)
        {
            if (wnd.Properties.ClassName.IsSupported)
            {
                string className = wnd.ClassName;
                return className.Contains("RenderWidgetHostHWND");
            }
            return false;
        }


        [DllImport("User32", EntryPoint = "SendMessageTimeoutW", ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, SEND_MESSAGE_TIMEOUT_FLAGS fuFlags, uint uTimeout, IntPtr lpdwResult);
    }
}
