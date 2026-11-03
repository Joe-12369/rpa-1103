using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons.WindowMatch;
using II.RPA.WinAutomation.CommonUtility.Parse;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace II.RPA.WebBrowser.Common.Basic.MatchBrowser
{
    public class EmbeddedIEWindowProvider
    {
        private static ILogger logger;
        private static List<WindowMatcher> embeddedIE_windowMatchers;
        static EmbeddedIEWindowProvider()
        {
            logger = Logger.OfContext();
            embeddedIE_windowMatchers = new List<WindowMatcher>();
        }
        #region 设置“应用内嵌IE”窗口匹配器
        public static void AddAppEmbeddedIEMatcher(WindowMatcher windowMatcher)
        {
            embeddedIE_windowMatchers.Add(windowMatcher);
        }
        #endregion

        #region 判断来源是否是 应用（非浏览器）
        /// <summary>
        /// 是否是支持的浏览器进程
        /// </summary>
        /// <param name="selectorProcessName"></param>
        /// <returns></returns>
        public static bool IsSupportedBrowser(string selectorProcessName)
        {
            var allSupportBrowserPNames = embeddedIE_windowMatchers.SelectMany(wm => wm.MatchConfigs.SelectMany(config => config.ExcludeProcess));
            return allSupportBrowserPNames.Any(pName => pName.EqualsOrIsMatch(selectorProcessName));
        }
        #endregion

        #region 按照Z序窗口  匹配内嵌的窗口
        public static BrowserWindow GetFirstEmbeddedIEWindowByZOrder()
        {
            return GetFirstEmbeddedIEWindowByZOrder(null);
        }
        public static BrowserWindow GetFirstEmbeddedIEWindowByZOrder(Func<BrowserWindow, bool> predicate)
        {
            try
            {
                logger.LogDebug($"开始按照Z序 匹配首个匹配内嵌的窗口");
                List<BrowserWindow> iconicWnds = new List<BrowserWindow>();
                DesktopWindow desktopWindow = new DesktopWindow();
                var matchedWnd = desktopWindow.MatchDesktopWindowInZorder((hWnd) =>
                {
                    BrowserWindow browserWindow = new BrowserWindow(hWnd);
                    if (!browserWindow.IsIconic)
                    {
                        if (MatchBrowserWindow(embeddedIE_windowMatchers, predicate, browserWindow))
                            return browserWindow;
                    }
                    else
                    {
                        iconicWnds.Add(browserWindow);
                    }
                    return null;
                }, FirstWndSourceType.DeskTopFirstWnd, true, true, false).FirstOrDefault();

                var retBrowserWindow = matchedWnd ?? iconicWnds.FirstOrDefault(wnd => MatchBrowserWindow(embeddedIE_windowMatchers, predicate, wnd));

                if (retBrowserWindow != null)
                    retBrowserWindow.SpecifiedWindow = true;
                return retBrowserWindow;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"按z序匹配内嵌的窗口异常");
            }
            finally
            {
                logger.LogDebug($"按照Z序 匹配首个内嵌的窗口类型 结束");
            }
            return null;
        }
        private static bool MatchBrowserWindow(List<WindowMatcher> matchers, Func<BrowserWindow, bool> predicate, BrowserWindow wnd)
        {
            logger.LogDebugBlock(() =>
            {
                logger.LogDebug($"桌面窗口({(wnd.IsIconic ? "最小化" : "")})：{wnd.Handle},{wnd.ClassName},{wnd.Title}");
            });
            var windowMatcher = matchers.FirstOrDefault(matcher => matcher.IsMatch(wnd));
            if (windowMatcher != null && (predicate == null || predicate.Invoke(wnd)))
            {
                return true;
            }
            return false;
        }
        #endregion
        #region 获取指定窗口的浏览器类型
        public static BrowserWindow MatchWindowBrowserByHandle(IntPtr hWnd)
        {
            BrowserWindow windowInfo = null;
            try
            {
                logger.LogDebug($"开始匹配应用内嵌IE浏览器窗口类型");
                BrowserWindow webWindowInfo = new BrowserWindow(hWnd);
                var windowMatcher = embeddedIE_windowMatchers.FirstOrDefault(matcher => matcher.IsMatch(webWindowInfo));
                if (windowMatcher != null)
                {
                    windowInfo = webWindowInfo;
                    return windowInfo;
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"匹配应用内嵌IE浏览器窗口类型 异常");
            }
            finally
            {
                logger.LogDebug($"匹配应用内嵌IE浏览器窗口类型 结束");
            }
            return windowInfo;
        }
        #endregion
    }
}

