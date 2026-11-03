using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.ProcessCreator.Screenshot
{
    internal class MouseHook
    {
        public static event EventHandler<MouseHookEventArgs> StatusChanged;

        private static IntPtr HookId = IntPtr.Zero;

        private static readonly InteropValues.HookProc Proc = HookCallback;

        private static int Count;

        public static void Start()
        {
            if (HookId == IntPtr.Zero)
            {
                HookId = SetHook(Proc);
            }

            if (HookId != IntPtr.Zero)
            {
                Count++;
            }
        }

        public static void Stop()
        {
            Count--;
            if (Count < 1)
            {
                InteropMethods.UnhookWindowsHookEx(HookId);
                HookId = IntPtr.Zero;
            }
        }

        private static IntPtr SetHook(InteropValues.HookProc proc)
        {
            using (var curProcess = Process.GetCurrentProcess())
            {
                using (var curModule = curProcess.MainModule)
                {

                    if (curModule != null)
                    {
                        return InteropMethods.SetWindowsHookEx((int)InteropValues.HookType.WH_MOUSE_LL, proc,
                            InteropMethods.GetModuleHandle(curModule.ModuleName), 0);
                    }
                    return IntPtr.Zero;
                }
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode < 0) return InteropMethods.CallNextHookEx(HookId, nCode, wParam, lParam);
            var hookStruct = (InteropValues.MOUSEHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(InteropValues.MOUSEHOOKSTRUCT));
            StatusChanged?.Invoke(null, new MouseHookEventArgs
            {
                MessageType = (MouseHookMessageType)wParam,
                Point = new InteropValues.POINT(hookStruct.pt.X, hookStruct.pt.Y)
            });

            return InteropMethods.CallNextHookEx(HookId, nCode, wParam, lParam);
        }
        //[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        //internal static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        //[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        //internal static extern IntPtr GetModuleHandle(string lpModuleName);
        //[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        //internal static extern IntPtr SetWindowsHookEx(int idHook, InteropValues.HookProc lpfn, IntPtr hMod, uint dwThreadId);
    }
    internal class MouseHookEventArgs : EventArgs
    {
        public MouseHookMessageType MessageType { get; set; }

        public InteropValues.POINT Point { get; set; }
    }
    public enum MouseHookMessageType
    {
        LeftButtonDown = 0x0201,
        LeftButtonUp = 0x0202,
        MouseMove = 0x0200,
        MouseWheel = 0x020A,
        RightButtonDown = 0x0204,
        RightButtonUp = 0x0205
    }
}
