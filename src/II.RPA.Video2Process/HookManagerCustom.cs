using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace II.RPA.Video2Process
{
    public class HookManagerCustom
    {
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
        static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpFileName);
        [DllImport("user32.dll", CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern int SetWindowsHookEx(
            int idHook,
            HookProc lpfn,
            IntPtr hMod,
            int dwThreadId);
        [DllImport("user32.dll", CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern int UnhookWindowsHookEx(int idHook);
        [DllImport("user32.dll", CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        private static extern int CallNextHookEx(
            int idHook,
            int nCode,
            int wParam,
            IntPtr lParam);
        [DllImport("user32")]
        protected static extern int GetKeyboardState(byte[] pbKeyState);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        protected static extern short GetKeyState(int vKey);
        [DllImport("user32", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern int ToAscii(
            int uVirtKey,
            int uScanCode,
            byte[] lpbKeyState,
            byte[] lpwTransKey,
            int fuState);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)] 
        public static extern IntPtr GetModuleHandle(IntPtr ptr);

        protected const int WH_MOUSE_LL = 14;
        protected const int WH_KEYBOARD_LL = 13;

        protected const int WH_MOUSE = 7;
        protected const int WH_KEYBOARD = 2;
        protected const int WM_MOUSEMOVE = 0x200;
        protected const int WM_LBUTTONDOWN = 0x201;
        protected const int WM_RBUTTONDOWN = 0x204;
        protected const int WM_MBUTTONDOWN = 0x207;
        protected const int WM_LBUTTONUP = 0x202;
        protected const int WM_RBUTTONUP = 0x205;
        protected const int WM_MBUTTONUP = 0x208;
        protected const int WM_LBUTTONDBLCLK = 0x203;
        protected const int WM_RBUTTONDBLCLK = 0x206;
        protected const int WM_MBUTTONDBLCLK = 0x209;
        protected const int WM_MOUSEWHEEL = 0x020A;
        protected const int WM_KEYDOWN = 0x100;
        protected const int WM_KEYUP = 0x101;
        protected const int WM_SYSKEYDOWN = 0x104;
        protected const int WM_SYSKEYUP = 0x105;

        protected const byte VK_SHIFT = 0x10;
        protected const byte VK_CAPITAL = 0x14;
        protected const byte VK_NUMLOCK = 0x90;

        protected const byte VK_LSHIFT = 0xA0;
        protected const byte VK_RSHIFT = 0xA1;
        protected const byte VK_LCONTROL = 0xA2;
        protected const byte VK_RCONTROL = 0x3;
        protected const byte VK_LALT = 0xA4;
        protected const byte VK_RALT = 0xA5;

        protected const byte LLKHF_ALTDOWN = 0x20;

        [StructLayout(LayoutKind.Sequential)]
        protected class POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        protected class MouseHookStruct
        {
            public POINT pt;
            public int hwnd;
            public int wHitTestCode;
            public int dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        protected class MouseLLHookStruct
        {
            public POINT pt;
            public int mouseData;
            public int flags;
            public int time;
            public int dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        protected class KeyboardHookStruct
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public int dwExtraInfo;
        }

        private delegate int HookProc(int nCode, int wParam, IntPtr lParam);
        private static HookProc s_KeyboardDelegate;
        private static int s_KeyboardHookHandle = 0;
        private static HookProc s_MouseDelegate;
        private static int s_MouseHookHandle = 0;

        private static int m_OldX = 0;
        private static int m_OldY = 0;

        public static void InstallMouseHook()
        {
            if (s_MouseHookHandle == 0)
            {
                s_MouseDelegate = MouseHookProc;
                s_MouseHookHandle = SetWindowsHookEx(
                    WH_MOUSE_LL,
                    s_MouseDelegate,
                     GetModuleHandle(IntPtr.Zero),
                    0);
            }
        }

        public static void InstallKeyboardHook()
        {
            if (s_KeyboardHookHandle == 0)
            {
                s_KeyboardDelegate = KeyboardHookProc;
                s_KeyboardHookHandle = SetWindowsHookEx(
                    WH_KEYBOARD_LL,
                    s_KeyboardDelegate,
                    GetModuleHandle(IntPtr.Zero),
                    0);
            }
        }

        public static void UnInstallMouseHook()
        {
            if (s_MouseHookHandle != 0)
            {
                //uninstall hook
                int result = UnhookWindowsHookEx(s_MouseHookHandle);
                //reset invalid handle
                s_MouseHookHandle = 0;
                //Free up for GC
                s_MouseDelegate = null;
            }
        }

        public static void UnInstallKeyboardHook()
        {
            if (s_KeyboardHookHandle != 0)
            {
                //uninstall hook
                int result = UnhookWindowsHookEx(s_KeyboardHookHandle);
                //reset invalid handle
                s_KeyboardHookHandle = 0;
                //Free up for GC
                s_KeyboardDelegate = null;
            }
        }

        public static int MouseHookProc(int nCode, int wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                //Marshall the data from callback.
                MouseLLHookStruct mouseHookStruct = (MouseLLHookStruct)Marshal.PtrToStructure(lParam, typeof(MouseLLHookStruct));

                //detect button clicked
                MouseButtons button = MouseButtons.None;
                short mouseDelta = 0;
                int clickCount = 0;
                bool mouseDown = false;
                bool mouseUp = false;

                switch (wParam)
                {
                    case WM_LBUTTONDOWN:
                        mouseDown = true;
                        button = MouseButtons.Left;
                        clickCount = 1;
                        break;
                    case WM_LBUTTONUP:
                        mouseUp = true;
                        button = MouseButtons.Left;
                        clickCount = 1;
                        break;
                    case WM_LBUTTONDBLCLK:
                        button = MouseButtons.Left;
                        clickCount = 2;
                        break;
                    case WM_RBUTTONDOWN:
                        mouseDown = true;
                        button = MouseButtons.Right;
                        clickCount = 1;
                        break;
                    case WM_RBUTTONUP:
                        mouseUp = true;
                        button = MouseButtons.Right;
                        clickCount = 1;
                        break;
                    case WM_RBUTTONDBLCLK:
                        button = MouseButtons.Right;
                        clickCount = 2;
                        break;
                    case WM_MOUSEWHEEL:
                        //If the message is WM_MOUSEWHEEL, the high-order word of MouseData member is the wheel delta. 
                        //One wheel click is defined as WHEEL_DELTA, which is 120. 
                        //(value >> 16) & 0xffff; retrieves the high-order word from the given 32-bit value
                        mouseDelta = (short)((mouseHookStruct.mouseData >> 16) & 0xffff);

                        //TODO: X BUTTONS (I havent them so was unable to test)
                        //If the message is WM_XBUTTONDOWN, WM_XBUTTONUP, WM_XBUTTONDBLCLK, WM_NCXBUTTONDOWN, WM_NCXBUTTONUP, 
                        //or WM_NCXBUTTONDBLCLK, the high-order word specifies which X button was pressed or released, 
                        //and the low-order word is reserved. This value can be one or more of the following values. 
                        //Otherwise, MouseData is not used. 
                        break;
                }

                //generate event 
                MouseEventArgs e = new MouseEventArgs(
                                                   button,
                                                   clickCount,
                                                   mouseHookStruct.pt.x,
                                                   mouseHookStruct.pt.y,
                                                   mouseDelta);

                //Mouse up
                if (mouseUp)
                {
                    Video2ProcessImp.HookManager_MouseUp(e);
                }

                //Mouse down
                if (mouseDown)
                {
                    Video2ProcessImp.HookManager_MouseDown(e);
                }

                //Wheel was moved
                if (mouseDelta != 0)
                {
                    Video2ProcessImp.HookManager_MouseWheel(e);
                }

                //If someone listens to move and there was a change in coordinates raise move event
                if (m_OldX != mouseHookStruct.pt.y || m_OldY != mouseHookStruct.pt.y)
                {
                    m_OldX = mouseHookStruct.pt.x;
                    m_OldY = mouseHookStruct.pt.y;
                    Video2ProcessImp.HookManager_MouseMove(e);
                }
            }

            NLogger.Default.Trace(string.Format("Mouse code {0} {1}", nCode, wParam));

            //call next hook
            return CallNextHookEx(s_MouseHookHandle, nCode, wParam, lParam);
        }
        private static int KeyboardHookProc(int nCode, Int32 wParam, IntPtr lParam)
        {
            bool handled = false;
            if (nCode >= 0)
            {
                //read structure KeyboardHookStruct at lParam
                KeyboardHookStruct MyKeyboardHookStruct = (KeyboardHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyboardHookStruct));
                //raise KeyDown
                if (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN)
                {
                    Keys keyData = (Keys)MyKeyboardHookStruct.vkCode;
                    KeyEventArgs e = new KeyEventArgs(keyData);
                    Video2ProcessImp.HookManager_KeyDown(e);
                    handled = e.Handled;
                }

                // raise KeyUp
                if (wParam == WM_KEYUP || wParam == WM_SYSKEYUP)
                {
                    Keys keyData = (Keys)MyKeyboardHookStruct.vkCode;
                    KeyEventArgs e = new KeyEventArgs(keyData);
                    Video2ProcessImp.HookManager_KeyUp(e);
                    handled = handled || e.Handled;
                }
            }

            NLogger.Default.Trace(string.Format("Keyboard code {0} {1}", nCode, wParam));

            //forward to other application
            return CallNextHookEx(s_KeyboardHookHandle, nCode, wParam, lParam);
        }
    }
}
