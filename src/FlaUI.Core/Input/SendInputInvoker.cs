using Microsoft.Extensions.Logging;
using System;
using System.Runtime.InteropServices;

namespace FlaUI.Core.Input
{
    public static class SendInputInvoker
    {
        private static readonly ILogger logger = II.RPA.Logging.Logger.OfContext();

        [DllImport("user32.dll")]
        internal static extern uint SendInput(uint nInputs, [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs, int cbSize);

        public static void MouseLeftButtonClick()
        {
            INPUT[] i = new INPUT[1];

            i[0].type = 0;
            i[0].U.mi.time = 0;
            i[0].U.mi.dwFlags = MOUSEEVENTF.LEFTDOWN | MOUSEEVENTF.ABSOLUTE;
            i[0].U.mi.dwExtraInfo = UIntPtr.Zero;
            i[0].U.mi.dx = 1;
            i[0].U.mi.dy = 1;

            var result = SendInput(1, i, INPUT.Size);
            if (result == 0)
            {
                var errorCode = Marshal.GetLastWin32Error();
                logger.LogError("SendInputInvoker: Could not send mouse input. ErrorCode: {0}", errorCode);
            }
            else
            {
                logger.LogDebug("SendInputInvoker: Send mouse input successfully. Result: {0}", result);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            internal uint type;
            internal InputUnion U;
            internal static int Size
            {
                get { return Marshal.SizeOf(typeof(INPUT)); }
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct InputUnion
        {
            [FieldOffset(0)]
            internal MOUSEINPUT mi;
            //[FieldOffset(0)]
            //internal KEYBDINPUT ki;
            //[FieldOffset(0)]
            //internal HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MOUSEINPUT
        {
            internal int dx;
            internal int dy;
            internal int mouseData;
            internal MOUSEEVENTF dwFlags;
            internal uint time;
            internal UIntPtr dwExtraInfo;
        }

        [Flags]
        internal enum MOUSEEVENTF : uint
        {
            ABSOLUTE = 0x8000,
            HWHEEL = 0x01000,
            MOVE = 0x0001,
            MOVE_NOCOALESCE = 0x2000,
            LEFTDOWN = 0x0002,
            LEFTUP = 0x0004,
            RIGHTDOWN = 0x0008,
            RIGHTUP = 0x0010,
            MIDDLEDOWN = 0x0020,
            MIDDLEUP = 0x0040,
            VIRTUALDESK = 0x4000,
            WHEEL = 0x0800,
            XDOWN = 0x0080,
            XUP = 0x0100
        }
    }
}
