using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.Core
{
    public class CursorHelper
    {
        private static string cursorFilePath = string.Empty;
        private static string CursorFilePath
        {
            get
            {
                if (string.IsNullOrEmpty(cursorFilePath))
                {
                    cursorFilePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(CursorHelper).Assembly.Location), "aero_working.ani");
                }
                return cursorFilePath;
            }
            set
            {
                cursorFilePath = value;
            }
        }

        private static IntPtr curHanble = IntPtr.Zero;
        private static IntPtr CurHanble
        {
            get
            {
                if (curHanble == IntPtr.Zero)
                {
                    curHanble = NativeMethods.LoadCursorFromFile(CursorFilePath);
                }
                return curHanble;
            }
            set
            {
                curHanble = value;
            }
        }

        private static ILogger logger = Logger.OfContext();

        public static void SetCursorBusy(bool isBusy)
        {
            try
            {
                if (isBusy)
                {
                    NativeMethods.SetSystemCursor(NativeMethods.CopyIcon(CurHanble), NativeMethods.OCR_NORMAL);
                    NativeMethods.SetSystemCursor(NativeMethods.CopyIcon(CurHanble), NativeMethods.OCR_IBEAM);
                }
                else
                {
                    NativeMethods.SystemParametersInfo(0x0057, 0, null, 0);
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, $"{(isBusy ? "设置" : "取消")}鼠标后台运行效果异常");
            }
        }
    }
}

