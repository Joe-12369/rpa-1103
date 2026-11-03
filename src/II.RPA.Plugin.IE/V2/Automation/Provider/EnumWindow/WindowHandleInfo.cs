using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace II.RPA.Plugin.IE.V2.Automation.Provider.EnumWindow
{
    internal class WindowHandleInfo
    {
        private delegate bool EnumWindowProc(IntPtr hwnd, IntPtr lParam);

        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumChildWindows(IntPtr window, EnumWindowProc callback, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern int GetClassNameW(IntPtr hWnd, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder lpString, int nMaxCount);

        private IntPtr _MainHandle;

        public WindowHandleInfo(IntPtr handle)
        {
            this._MainHandle = handle;
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
            if (sb.ToString().Contains("Chrome_WidgetWin_")
                || sb.ToString().Contains("Internet Explorer_Server")
                || sb.ToString().Contains("Internet Explorer_TridentDlgFrame")
                || sb.ToString().Contains("Explorer_TridentDlgFrame")
                || sb.ToString().Contains("ThunderDFrame"))
                childHandles.Add(hWnd);

            return true;
        }
    }
}
