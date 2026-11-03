using II.RPA.Plugin.Core.Entities;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;


namespace II.RPA.Plugin.Core.Commons
{
    // Token: 0x0200003C RID: 60
    public struct INPUT
    {
        // Token: 0x04000196 RID: 406
        public int type;

        // Token: 0x04000197 RID: 407
        public INPUTUNION ui;
    }

    // Token: 0x0200003D RID: 61
    public struct MOUSEINPUT
    {
        // Token: 0x04000198 RID: 408
        public int dx;

        // Token: 0x04000199 RID: 409
        public int dy;

        // Token: 0x0400019A RID: 410
        public int mouseData;

        // Token: 0x0400019B RID: 411
        public int dwFlags;

        // Token: 0x0400019C RID: 412
        public int time;

        // Token: 0x0400019D RID: 413
        public IntPtr dwExtraInfo;
    }

    // Token: 0x0200003E RID: 62
    public struct KEYBDINPUT
    {
        // Token: 0x0400019E RID: 414
        public short wVk;

        // Token: 0x0400019F RID: 415
        public short wScan;

        // Token: 0x040001A0 RID: 416
        public int dwFlags;

        // Token: 0x040001A1 RID: 417
        public int time;

        // Token: 0x040001A2 RID: 418
        public IntPtr dwExtraInfo;
    }

    // Token: 0x0200003F RID: 63
    public struct HARDWAREINPUT
    {
        // Token: 0x040001A3 RID: 419
        public int uMsg;

        // Token: 0x040001A4 RID: 420
        public short wParamL;

        // Token: 0x040001A5 RID: 421
        public short wParamH;
    }

    // Token: 0x02000040 RID: 64
    [StructLayout(LayoutKind.Explicit)]
    public struct INPUTUNION
    {
        // Token: 0x040001A6 RID: 422
        [FieldOffset(0)]
        public MOUSEINPUT mi;

        // Token: 0x040001A7 RID: 423
        [FieldOffset(0)]
        public KEYBDINPUT ki;

        // Token: 0x040001A8 RID: 424
        [FieldOffset(0)]
        public HARDWAREINPUT hi;
    }
    public class SetTextByClipboard
    {
        [DllImport("user32.dll", EntryPoint = "SendMessageA", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hwnd, int wMsg, int wParam, int lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr WindowFromPoint(int xPoint, int yPoint);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetCursorPos(out POINT lpPoint);
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern bool ScreenToClient(IntPtr hWnd, ref System.Drawing.Point rect);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SetFocus(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern uint MapVirtualKey(int uCode, int uMapType);
        [DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
        [DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int SendMessageTimeout(IntPtr hWnd, [MarshalAs(UnmanagedType.U4)] uint Msg, IntPtr wParam, IntPtr lParam,
            [MarshalAs(UnmanagedType.U4)] int fuFlags, [MarshalAs(UnmanagedType.U4)] int uTimeout, [MarshalAs(UnmanagedType.U4)] ref int lpdwResult);
        [DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern short VkKeyScan(char ch);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern short GetKeyState(int nVirtKey);
        public const int WM_MOUSEOVER = 0x0200;
        public const int WM_LBUTTONDOWN = 0x0201;
        public const int WM_LBUTTONUP = 0x0202;
        public const int WM_PASTE = 0x0302;
        public const int WM_CHAR = 0x0102;
        private static ILogger logger = Logger.OfContext();

        public static void SetText(Rectangle rect, string strText)
        {
            Point pt = new Point(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);
            Clipboard.SetText(strText);//这函数有问题 可能执行成功但是异常报错 可以加异常处理
            IntPtr hWnd = WindowFromPoint(pt.X, pt.Y);
            ScreenToClient(hWnd, ref pt);
            int cx = pt.X;
            int cy = pt.Y;

            SetFocus(hWnd);

            SendMessage(hWnd, WM_LBUTTONDOWN, 0, (cx + (cy << 16)));
            SendMessage(hWnd, WM_LBUTTONUP, 0, (cx + (cy << 16)));

            //htmlDoc2.execCommand("paste");

            //SendKeys.Send(strText);

            for (int i = 0; i < strText.Length; i++)
            {
                string strTemp = strText.Substring(i, 1);
                byte[] sarr = Encoding.GetEncoding("gb2312").GetBytes(strTemp);
                if (sarr.Length == 1)
                //if (false == Regex.IsMatch(strText[i].ToString(), @"[\u4e00-\u9fff]"))
                {
                    SendMessage(hWnd, WM_CHAR, (strText[i] & 0xFF), 0);
                    Thread.Sleep(20);
                }
                else
                {
                    string str = string.Format("{0}", strText[i]);
                    SendKeys.SendWait(str);
                    Thread.Sleep(20);
                }
            }

            //keybd_event((byte)(162), 0, 0, 0);  //按下ctrl，
            //keybd_event((byte)86, 0, 0, 0);  //按下 V 键
            //keybd_event((byte)86, 0, 2, 0);  //释放 V 键
            //keybd_event((byte)(162), 0, 0x02, 0);　　//释放 ctrl 键
            //Thread.Sleep(20);  
        }
        public void SimulateInputString(Rectangle rect, string sText)
        {
            logger.LogDebug($"SimulateInputString {sText}");

            Point pt = new Point(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);
            IntPtr hWnd = WindowFromPoint(pt.X, pt.Y);
            ScreenToClient(hWnd, ref pt);
            int cx = pt.X;
            int cy = pt.Y;

            SetFocus(hWnd);

            SendMessage(hWnd, WM_LBUTTONDOWN, 0, (cx + (cy << 16)));
            SendMessage(hWnd, WM_LBUTTONUP, 0, (cx + (cy << 16)));

            char[] array = sText.ToCharArray();
            char[] array2 = array;
            int i = 0;
            Random random = new Random();
            while (i < array2.Length)
            {
                char c = array2[i];
                logger.LogDebug($"SimulateInputString {c}");
                INPUT[] array3 = new INPUT[2];
                if (c < '\0' || c >= 'Ā')
                {
                    goto IL_2DE;
                }
                short num2 = VkKeyScan(c);
                if (num2 == -1)
                {
                    goto IL_2DE;
                }
                bool flag2 = (num2 >> 8 & 1) != 0;
                if ((GetKeyState(20) & 1) != 0 && ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')))
                {
                    flag2 = !flag2;
                }
                array3[0].type = (array3[1].type = 1);
                array3[0].ui.ki.dwFlags = 0;
                if (flag2)
                {
                    array3[0].ui.ki.wVk = 16;
                    array3[0].ui.ki.wScan = (short)MapVirtualKey(16, 0);
                    SendInput_1(1U, array3, Marshal.SizeOf(default(INPUT)));
                    Thread.Sleep(50);
                }
                array3[0].ui.ki.wVk = (array3[1].ui.ki.wVk = (short)(num2 & 255));
                array3[0].ui.ki.wScan = (array3[1].ui.ki.wScan = (short)MapVirtualKey((int)(num2 & 255), 0));
                array3[1].ui.ki.dwFlags = 2;
                SendInput_1(2U, array3, Marshal.SizeOf(default(INPUT)));
                if (flag2)
                {
                    Thread.Sleep(50);
                    array3[0].ui.ki.wVk = 16;
                    array3[0].ui.ki.wScan = (short)MapVirtualKey(16, 0);
                    array3[0].ui.ki.dwFlags = 2;
                    SendInput_1(1U, array3, Marshal.SizeOf(default(INPUT)));
                }
                i++;
                Thread.Sleep(50);
                continue;
IL_2DE:
                array3[0].type = 1;
                array3[0].ui.ki.wVk = 0;
                array3[0].ui.ki.wScan = (short)c;
                array3[0].ui.ki.dwFlags = 4;
                array3[0].ui.ki.time = 0;
                array3[0].ui.ki.dwExtraInfo = IntPtr.Zero;
                array3[1].type = 1;
                array3[1].ui.ki.wVk = 0;
                array3[1].ui.ki.wScan = (short)c;
                array3[1].ui.ki.dwFlags = 6;
                array3[1].ui.ki.time = 0;
                array3[1].ui.ki.dwExtraInfo = IntPtr.Zero;
                SendInput_1(2U, array3, Marshal.SizeOf(default(INPUT)));
                i++;
                InterruptibleDelay(120 + random.Next(5));
            }
        }
        public bool InterruptibleDelay(int milliSeconds)
        {
            int num = milliSeconds / 1000;
            int num2 = milliSeconds % 1000;
            if (num > 0)
            {
                for (int i = 0; i < num; i++)
                {
                    Thread.Sleep(1000);
                }
            }
            if (num2 > 0)
            {
                Thread.Sleep(num2);
            }
            return false;
        }
        public void SendInput_1(uint nInputs, INPUT[] pInputs, int cbSize)
        {
            int tickCount = Environment.TickCount;
            for (; ; )
            {
                uint nRet = SendInput(nInputs, pInputs, cbSize);
                if (nRet == cbSize)
                {
                    break;
                }
                else
                {
                    int lastWin32Error = Marshal.GetLastWin32Error();
                    if (lastWin32Error == 5)
                    {

                    }
                    else
                    {
                        break;
                    }
                }

                if (Environment.TickCount - tickCount > 1000)
                {
                    break;
                }

                logger.LogDebug($"SendInput_1 {Environment.TickCount}");

                Thread.Sleep(1000);
            }
        }
        public void SystemMessageSetText(Rectangle rect, string text)
        {
            logger.LogDebug($"SystemMessageText {text}");

            Point pt = new Point(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);
            IntPtr hWnd = WindowFromPoint(pt.X, pt.Y);
            ScreenToClient(hWnd, ref pt);
            int cx = pt.X;
            int cy = pt.Y;

            SetFocus(hWnd);

            SendMessage(hWnd, WM_LBUTTONDOWN, 0, (cx + (cy << 16)));
            SendMessage(hWnd, WM_LBUTTONUP, 0, (cx + (cy << 16)));

            Random random = new Random();
            char[] array = text.ToCharArray();
            foreach (char wParam in array)
            {
                IsWinHung(hWnd, 2000);
                EnsurePostMessage(hWnd, 646U, (int)wParam, 0);
                IsWinHung(hWnd, 2000);
                InterruptibleDelay(120 + random.Next(5));
            }
        }
        private void EnsurePostMessage(IntPtr hWnd, uint Msg, int wParam, int lParam)
        {
            if (!PostMessage(hWnd, Msg, wParam, lParam))
            {

            }
        }
        private bool IsWinHung(IntPtr hWnd, int nTimeOut = 5000)
        {
            int num = 0;
            return SendMessageTimeout(hWnd, 0U, IntPtr.Zero, IntPtr.Zero, 2, nTimeOut, ref num) == 0;
        }
    }
}
