using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.RPASystem.Helper
{
    public class User32Helper
    {
        public static bool IsConnection(string className, string title)
        {
            return NativeMethods.FindWindow(className, title) != IntPtr.Zero;
        }

        public static bool Close(string className, string title)
        {
            IntPtr ptr = NativeMethods.FindWindow(className, title);
            if (ptr != IntPtr.Zero)
            {
                //发送关闭指令
                return SetTextByClipboard.PostMessage(ptr, 0x0010, 0, 0);
            }
            else
            {
                return true;
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool LockWorkStation();
    }
}
