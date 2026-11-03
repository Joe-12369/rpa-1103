using AutoIt;
using II.RPA.Plugin.Core;
using System;
using System.Drawing;
using System.Threading;

namespace II.RPA.Plugin.GUI.Commons
{
    public static class AutoItHelper
    {
        static AutoItHelper()
        {
            AutoItX.Init();
        }
        public static string ControlGetText(IntPtr winHandle, IntPtr controlHandle)
        {
            return AutoItX.ControlGetText(winHandle, controlHandle, 65535);
        }
        public static int ControlSetText(IntPtr winHandle, IntPtr controlHandle, string value)
        {
            return AutoItX.ControlSetText(winHandle, controlHandle, value);
        }
        public static int WinActivate(IntPtr winHandle)
        {
            return AutoItX.WinActivate(winHandle);
        }
        public static int WinActive(IntPtr winHandle)
        {
            return AutoItX.WinActive(winHandle);
        }
        public static void MouseClick(string botton, Point point, int numClick = 1, int speed = -1)
        {
            MouseClick(botton, point.X, point.Y, numClick, speed);
        }
        public static void MouseClick(string botton, int x, int y, int numClick = 1, int speed = -1)
        {
            AutoItX.MouseClick(botton, x, y, 1, -1);
        }
        public static void Send(string value, int mode = 0)
        {
            Send(value, mode, 50);
        }
        public static void SendKeyDelay(int optionValue)
        {
            AutoItSetOption("SendKeyDelay", optionValue);
        }
        private static void AutoItSetOption(string option, int optionValue)
        {
            AutoItX.AutoItSetOption(option, optionValue);
        }
        public static void Send(string value, bool containsHotkey, int sendKeyDelay = 50)
        {
            AutoItX.AutoItSetOption("SendKeyDelay", sendKeyDelay);
            value = value.Replace("\r\n", "\r");
            AutoItX.Send(value, containsHotkey ? 0 : 1);
        }
        public static void Send(string value, int mode, int sendKeyDelay = 50)
        {
            AutoItX.AutoItSetOption("SendKeyDelay", sendKeyDelay);
            value = value.Replace("\r\n", "\r");
            AutoItX.Send(value, mode);
        }
        public static void ControlInputMessage(string value, IntPtr winHandle, IntPtr controlHandle, bool containsHotkey, int sendKeyDelay = 50)
        {
            AutoItX.AutoItSetOption("SendKeyDelay", sendKeyDelay);
            value = value.Replace("\r\n", "\r");
            AutoItX.ControlSend(winHandle, controlHandle, value, containsHotkey ? 0 : 1);
        }
        [Obsolete]
        public static void ClearWords()
        {
            Send("{CTRLDOWN}{CTRLUP}", 0);
            Send("^{a}", 0);
            Send("{delete}", 0);
        }

        public static void ClearWords(int sendKeyDelay = 50)
        {
            Send("^{a}", 0, sendKeyDelay);
            //Send("{delete}", 0, sendKeyDelay);
        }
        public static void MoveCursorToEnd(int sendKeyDelay = 50)
        {
            Send("^{a}", 0, sendKeyDelay);
            Send("{END}", 0, sendKeyDelay);
        }
        public static void ControlClearWords(IntPtr winHandle, IntPtr controlHandle, int sendKeyDelay = 50)
        {
            ControlInputMessage("^{a}", winHandle, controlHandle, true, sendKeyDelay);
            //ControlInputMessage("{delete}", winHandle, controlHandle, true, sendKeyDelay);
        }
        [Obsolete]
        public static void MoveCursorToEnd()
        {
            Send("{CTRLDOWN}{CTRLUP}", 0);
            Send("^{a}", 0);
            Send("{END}", 0);
        }
        public static void ControlMoveCursorToEnd(IntPtr winHandle, IntPtr controlHandle, int sendKeyDelay = 50)
        {
            ControlInputMessage("^{a}", winHandle, controlHandle, true, sendKeyDelay);
            ControlInputMessage("{END}", winHandle, controlHandle, true, sendKeyDelay);
        }

    }
}
