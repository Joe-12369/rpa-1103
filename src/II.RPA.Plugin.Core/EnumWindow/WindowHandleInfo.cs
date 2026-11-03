using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace II.RPA.Plugin.Core.EnumWindow
{
    public class WindowHandleInfo
    {
        private delegate bool EnumWindowProc(IntPtr hwnd, IntPtr lParam);

        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumChildWindows(IntPtr window, EnumWindowProc callback, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern int GetClassNameW(IntPtr hWnd, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder lpString, int nMaxCount);

        private IntPtr _MainHandle;
        private List<string> _ListClassName;
        public WindowHandleInfo(IntPtr handle, List<string> classNames)
        {
            this._MainHandle = handle;
            this._ListClassName = classNames;
        }

        public List<IntPtr> GetAllChildHandles()
        {
            List<IntPtr> childHandles = new List<IntPtr>();

            GCHandle gcChildhandlesList = GCHandle.Alloc(childHandles);
            IntPtr pointerChildHandlesList = GCHandle.ToIntPtr(gcChildhandlesList);

            try
            {
                EnumWindowProc childProc = new EnumWindowProc(EnumWindow);
                EnumChildWindows(this._MainHandle, childProc, pointerChildHandlesList);
            }
            finally
            {
                gcChildhandlesList.Free();
            }

            return childHandles;
        }

        private bool EnumWindow(IntPtr hWnd, IntPtr lParam)
        {
            GCHandle gcChildhandlesList = GCHandle.FromIntPtr(lParam);

            if (gcChildhandlesList == null || gcChildhandlesList.Target == null)
            {
                return false;
            }

            List<IntPtr> childHandles = gcChildhandlesList.Target as List<IntPtr>;
            StringBuilder sb = new StringBuilder(256);
            GetClassNameW(hWnd, sb, sb.Capacity);
            if (this._ListClassName.Contains(sb.ToString())&& NativeMethods.IsWindowVisible(hWnd))
            { 
                childHandles.Add(hWnd);
            } 
            return true;
        }
    }
}
