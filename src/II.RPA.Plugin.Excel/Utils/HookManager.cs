using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using static II.RPA.Plugin.Excel.Utils.NativeUtils;

namespace II.RPA.Plugin.Excel.Utils
{
    public class HookManager
    {
        private HookProc _mouseProc;
        private HookProc _keyboardProc;
        private IntPtr _mouseHookID = IntPtr.Zero;
        private IntPtr _keyboardHookID = IntPtr.Zero;
        Action<dynamic> MouseHook, KeyboardHook;

        public HookManager(Action<dynamic> mouseHook, Action<dynamic> keyboardHook)
        {
            MouseHook = mouseHook;
            KeyboardHook = keyboardHook;
            _mouseProc = MouseHookCallback;
            _keyboardProc = KeyboardHookCallback;
        }

        public void SetHook(IntPtr _targetWindowHandle)
        {
            _mouseHookID = SetHook(_targetWindowHandle, _mouseProc, WH_MOUSE_LL);
            _keyboardHookID = SetHook(_targetWindowHandle, _keyboardProc, WH_KEYBOARD_LL);
        }

        public void Unhook()
        {
            KeyboardHook = null;
            MouseHook = null;
            UnhookWindowsHookEx(_mouseHookID);
            UnhookWindowsHookEx(_keyboardHookID);
        }

        private IntPtr SetHook(IntPtr module, HookProc proc, int hookType)
        {
            return SetWindowsHookEx(hookType, proc, IntPtr.Zero, 0);
        }
        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_LBUTTONUP)
            {
                MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                if (MouseHook != null)
                {
                    IntPtr hWnd = WindowFromPoint(hookStruct.Point);
                    if (hWnd != IntPtr.Zero)
                    {
                        GetWindowThreadProcessId((int)hWnd, out int processId);
                        using (var process = Process.GetProcessById(processId))
                        {
                            if (process.ProcessName.Equals("et"))
                            {
                                using (var wps = ProcessUtils.GetParentProcess(processId))
                                {
                                    if (wps.ProcessName.Equals("wps"))
                                    {
                                        MouseHook(wps.Id);
                                    }
                                    else
                                    {
                                        MouseHook(processId);
                                    }
                                }
                            }
                            else
                            {
                                MouseHook(processId);
                            }
                        }
                    }
                }
            }
            return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if (KeyboardHook != null)
                {
                    KeyboardHook(vkCode);
                }
            }
            return CallNextHookEx(_keyboardHookID, nCode, wParam, lParam);
        }

    }

}