using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.IE.V2.Automation.Provider.EnumWindow
{
    /// <summary>
    /// 获取 Win32 窗口的一些基本信息。
    /// </summary>
    public readonly struct WindowInfo
    {
        public WindowInfo(IntPtr hWnd, string className, bool isVisible) : this()// string title,
        {
            Hwnd = hWnd;
            ClassName = className;
            //Title = title;
            IsVisible = isVisible;
        }

        /// <summary>
        /// 获取窗口句柄。
        /// </summary>
        public IntPtr Hwnd { get; }

        /// <summary>
        /// 获取窗口类名。
        /// </summary>
        public string ClassName { get; }

        /// <summary>
        /// 获取窗口标题。
        /// </summary>
        //public string Title { get; }

        /// <summary>
        /// 获取当前窗口是否可见。
        /// </summary>
        public bool IsVisible { get; }



    }
}
