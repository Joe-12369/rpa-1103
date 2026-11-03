using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.WinAPI
{
    public class Imm32
    {
        [DllImport("imm32")]
        public static extern IntPtr ImmGetDefaultIMEWnd(IntPtr windowHandle);
        [DllImport("imm32")]
        public static extern int ImmGetCompositionString(IntPtr hIMC, uint dwIndex, char[] lpBuf, uint dwBufLen);
    }
}
