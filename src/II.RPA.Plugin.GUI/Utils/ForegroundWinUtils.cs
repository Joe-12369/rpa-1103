using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.WindowMatch;
using II.RPA.Plugin.GUI.Windows;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Utils
{
    public class ForegroundWinUtils
    {
        private static ILogger logger = Logger.OfContext();
        public static IntPtr GetForegroundWindow(bool reStart = false)
        {
            IntPtr hWnd;
            if (reStart)
            {
                logger.LogDebug($"通过【GetForegroundWindow】方式获取窗口句柄");
                hWnd = NativeMethods.GetForegroundWindow();
            }
            else
            {
                logger.LogDebug($"通过【GetDesktopWindow】方式获取窗口句柄");
                var desktop = NativeMethods.GetDesktopWindow();
                hWnd = NativeMethods.GetWindow(desktop, (uint)GET_WINDOW_CMD.GW_CHILD);
            }
            while (hWnd != IntPtr.Zero)
            {
                if (NativeMethods.IsWindow(hWnd)
                  && NativeMethods.IsWindowVisible(hWnd)
                  && (NativeMethods.GetWindowLong(hWnd, NativeMethods.GWL_EXSTYLE) & 128) == 0
                  )
                {
                    var curPName = UiCommon.GetProcessNameByCache(hWnd);
                    logger.LogDebug($"窗口进程:{curPName}");
                    if (!Configurations.Info.ClientProcessNames.Any(pn => curPName.Contains(pn)))
                    {
                        logger.LogDebug($"将使用窗口：{hWnd}，进程：{curPName}");
                        try
                        {
                            WindowObject windowObject = new WindowObject(hWnd);
                            windowObject.PrintWindowInfo();
                            UiCommon.UIAAutomation.FromHandle(hWnd);
                            break;
                        }
                        catch (Exception ex)
                        {
                            Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"遍历查找置顶窗口异常");
                        }
                    }
                }
                else
                {
                    //var curPName = UiCommon.GetProcessName(hWnd);
                    logger.LogDebug($"忽略窗口：{hWnd}，进程：");
                }
                hWnd = NativeMethods.GetWindow(hWnd, 2);
            }
            if (hWnd == IntPtr.Zero && !reStart)
            {
                return GetForegroundWindow(true);
            }
            return hWnd;
        }
    }
}

