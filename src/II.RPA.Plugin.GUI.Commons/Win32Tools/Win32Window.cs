using AutoIt;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.EnumWindow;
using II.RPA.WinAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Documents;

namespace II.RPA.Plugin.GUI.Commons.Win32Tools
{
    public class Win32Window
    {
        private readonly IntPtr _hWnd;
        public Win32Window(IntPtr hWnd)
        {
            this._hWnd = hWnd;
        }
        public Win32Window Root
        {
            get
            {
                var rootHwnd = NativeMethods.GetAncestor(this._hWnd, GetAncestorFlags.GetRoot);
                return new Win32Window(rootHwnd);
            }
        }
        public string GetText()
        {
            return AutoItHelper.ControlGetText(this.Root.Handle, this._hWnd);
        }
        public IntPtr Handle
        {
            get { return this._hWnd; }
        }
        public bool IsDesktop()
        {
            return this.ClassName == "#32769";
        }
        public bool IsVisible
        {
            get
            {
                return NativeMethods.IsWindowVisible(this._hWnd);
            }
        }
        public bool IsEnabled
        {
            get
            {
                return NativeMethods.IsWindowEnabled(this._hWnd);
            }
        }
        public bool IsActive()
        {
            return AutoItHelper.WinActive(this._hWnd) != 0;
        }
        public void WinActivate()
        {
            AutoItHelper.WinActivate(this._hWnd);
        }
        public string ClassName
        {
            get
            {
                return UiCommon.GetClassName(this._hWnd, 256);
            }
        }
        /// <summary>
        /// 是否模态框
        /// </summary>
        public bool IsModal
        {
            get
            {
                try
                {
                    return UiCommon.UIAAutomation.FromHandle(this._hWnd).AsWindow().IsModal;
                }
                catch
                {
                    return false;
                }
            }
        }
        public int ProcessId
        {
            get
            {
                UiCommon.GetWindowThreadProcessId(this._hWnd, out uint processId);
                return (int)processId;
            }
        }
        public string ProcessName
        {
            get
            {
                using (Process process = Process.GetProcessById(this.ProcessId))
                {
                    return process.ProcessName;
                }
            }
        }
        public static Win32Window GetDesktopWindow()
        {
            return new Win32Window(NativeMethods.GetDesktopWindow());
        }
        public List<Win32Window> FindAllWindow()
        {
            return this.QueryChildWindowAll();
        }
        //public List<Win32Window> ChildrenWindow()
        //{
        //    var wndList = new List<Win32Window>();
        //    var element = UiCommon.UIAAutomation.FromHandle(this._hWnd);
        //    if (element != null)
        //    {
        //        var children = element.FindAllChildren();
        //        foreach (var child in children)
        //        {
        //            if (child.Properties.NativeWindowHandle.IsSupported)
        //            {
        //                var handle = child.Properties.NativeWindowHandle;
        //                if (handle != IntPtr.Zero && NativeMethods.IsWindowVisible(handle))
        //                    wndList.Add(new Win32Window(handle));
        //            }
        //        }
        //    }
        //    return wndList;
        //}

        public List<Win32Window> QueryChildWindowAll(List<string> className)
        {
            List<Win32Window> listWin32 = new List<Win32Window>();
            WindowHandleInfo windowHandleInfo = new WindowHandleInfo(this.Handle, className);
            var listHandle = windowHandleInfo.GetAllChildHandles();
            foreach (var handle in listHandle)
            {
                listWin32.Add(new Win32Window(handle));
            }
            return listWin32;
        }

        //public List<Win32Window> QueryChildWindowAll(Func<Win32Window, bool> predicate)
        //{

        //    List<Win32Window> listWin32 = new List<Win32Window>();
        //    var listHandle = User32Extension.QueryAllChildWindows(this.Handle, (IntPtr m) =>
        //    {
        //        Win32Window win32Window;
        //        win32Window = new Win32Window(m);
        //        if (win32Window != null)
        //        {
        //            listWin32.Add(win32Window);
        //            return true;
        //        }
        //    });
        //    return listWin32;
        //}

        public List<Win32Window> QueryChildWindowAll(bool includeVisible = false)
        {
            List<Win32Window> listWin32 = new List<Win32Window>();
            var listHandle = User32Extension.QueryAllChildWindows(this.Handle, (handle) =>
            {
                if (!User32.IsWindow(handle))
                {
                    return false;
                }
                if (includeVisible || User32.IsWindowVisible(handle))
                {
                    var win32Window = new Win32Window(handle);
                    if (win32Window != null)
                    {
                        listWin32.Add(win32Window);
                        return true;
                    }
                }
                return false;
            });
            foreach (var handle in listHandle)
            {
                listWin32.Add(new Win32Window(handle));
            }
            return listWin32;
        }

        public List<Win32Window> Children()
        {
            var wndList = new List<Win32Window>();
            var element = UiCommon.UIAAutomation.FromHandle(this._hWnd);
            if (element != null)
            {
                var children = element.FindAllChildren();
                foreach (var child in children)
                {
                    if (child.Properties.NativeWindowHandle.IsSupported)
                    {
                        var handle = child.Properties.NativeWindowHandle;
                        if (handle != IntPtr.Zero && NativeMethods.IsWindowVisible(handle))
                            wndList.Add(new Win32Window(handle));
                    }
                }
            }
            return wndList;
        }
        //public List<Win32Window> ChildrenWindow()
        //{
        //    var wndList = new List<Win32Window>();
        //    WindowHandleInfo windowHandleInfo = new WindowHandleInfo(this._hWnd, new List<string>() { "#32770" });
        //    var listHandle = windowHandleInfo.GetAllChildHandles();
        //    foreach (var handle in listHandle)
        //    {
        //        wndList.Add(new Win32Window(handle));
        //    }
        //    return wndList;
        //}
        //private Win32Window MatchDesktopChildPredicate(IntPtr hWnd, IntPtr desktopHWnd, Func<Win32Window, bool> predicate, bool includeInvisible)
        //{
        //    if (!NativeMethods.IsWindow(hWnd))
        //    {
        //        return null;
        //    }
        //    if ((includeInvisible || NativeMethods.IsWindowVisible(hWnd)) && NativeMethods.GetAncestor(hWnd, GetAncestorFlags.GetParent) == desktopHWnd)
        //    {
        //        Win32Window win32Window = new Win32Window(hWnd);

        //        if (predicate(win32Window))
        //        {
        //            return win32Window;
        //        }
        //    }
        //    return null;
        //}
    }
}
