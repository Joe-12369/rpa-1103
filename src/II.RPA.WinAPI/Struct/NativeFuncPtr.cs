using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.WinAPI.Struct
{
    public delegate IntPtr LLProc(Int32 nCode, IntPtr wParam, IntPtr lParam);
    public delegate bool EnumWindowsProc(IntPtr hWnd, int lParam);
}
