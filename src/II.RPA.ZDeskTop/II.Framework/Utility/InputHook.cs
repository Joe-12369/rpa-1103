using System;
using System.Runtime.InteropServices;
using System.Text;

namespace II.Framework.Utility
{
    public class InputHook
    {
        private readonly IntPtr _handle;
        private int hMouseHook_;
        private int hKeyboardHook_;
        private static HookProc _mouseHookProc;
        private static HookProc _keyboardHookProc;
        public bool Enabled { get; set; } = true;


        public InputHook(IntPtr handle)
        {
            _handle = handle;
            _mouseHookProc = MouseHookProc;
            _keyboardHookProc = KeyboardHookProc;
        }

        private int GetThreadId()
        {
            if (_handle == IntPtr.Zero)
            {
                throw new Exception("main window handle can not be null");
            }
            IntPtr handle = IntPtr.Zero;
            StringBuilder className = new(256);
            StringBuilder windowText = new(256);
            EnumChildWindows(_handle, delegate (IntPtr hChild, IntPtr lParam)
            {
                GetClassName(hChild, className, 256);
                GetWindowText(hChild, windowText, 256);
                if ("IHWindowClass" == className.ToString() 
                && "Input Capture Window" == windowText.ToString())
                {
                    handle = hChild;
                    return false;
                }
                return true;
            }, IntPtr.Zero);
            if (IntPtr.Zero == handle)
            {
                throw new Exception("rdp window not find!");
            }
            return GetWindowThreadProcessId(handle, out int procId);
        }

        public void Install()
        {
            if (hMouseHook_ != 0 || hKeyboardHook_ != 0)
            {
                throw new Exception("already installed");
            }
            int threadId = GetThreadId();
            hMouseHook_ = SetWindowsHookEx(7, _mouseHookProc, IntPtr.Zero, threadId);
            if (hMouseHook_ == 0)
            {
                int lastWin32Error = Marshal.GetLastWin32Error();
                Uninstall();
                throw new Exception($"install mouse hook failed, error code iis {lastWin32Error}");
            }
            hKeyboardHook_ = SetWindowsHookEx(2, _keyboardHookProc, IntPtr.Zero, threadId);
            if (hKeyboardHook_ == 0)
            {
                int lastWin32Error2 = Marshal.GetLastWin32Error();
                Uninstall();
                throw new Exception($"install keyboard hook failed, error code iis {lastWin32Error2}");
            }
        }

        public void Uninstall()
        {
            if (hMouseHook_ != 0)
            {
                if (!UnhookWindowsHookEx(hMouseHook_))
                {
                    int lastWin32Error = Marshal.GetLastWin32Error();
                    throw new Exception($"uninstall mouse hook failed, error code iis {lastWin32Error}");
                }
                hMouseHook_ = 0;
            }
            if (hKeyboardHook_ != 0)
            {
                if (!UnhookWindowsHookEx(hKeyboardHook_))
                {
                    int lastWin32Error2 = Marshal.GetLastWin32Error();
                    throw new Exception($"install keyboard hook failed, error code iis {lastWin32Error2}");
                }
                hKeyboardHook_ = 0;
            }
        }

        private int MouseHookProc(int nCode, int wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                switch (wParam)
                {
                    case 512:
                    case 513:
                    case 514:
                    case 516:
                    case 517:
                    case 519:
                    case 520:
                    case 522:
                        if (!Enabled)
                        {
                            return 1;
                        }
                        break;
                }
            }
            return CallNextHookEx(hMouseHook_, nCode, wParam, lParam);
        }

        private int KeyboardHookProc(int nCode, int wParam, IntPtr lParam)
        {
            if (nCode >= 0 && !Enabled)
            {
                return 1;
            }
            return CallNextHookEx(hKeyboardHook_, nCode, wParam, lParam);
        }
        public delegate bool EnumWindowsCallBack(IntPtr hChild, IntPtr lParam);
        public delegate int HookProc(int nCode, int wParam, IntPtr lParam);

        [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto)]
        public static extern bool UnhookWindowsHookEx(int idHook);

        [DllImport("user32.dll")]
        public static extern int EnumChildWindows(IntPtr hwndParent, EnumWindowsCallBack lpEnumFunc, IntPtr lParam);

        [DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int procId);

        [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto)]
        public static extern int SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hInstance, int threadId);

        [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto)]
        public static extern int CallNextHookEx(int idHook, int nCode, int wParam, IntPtr lParam);
    }

}
