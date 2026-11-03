using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons.WindowMatch;
using II.RPA.Plugin.GUI.Commons.WindowMatch.Entities;
using II.RPA.WebBrowser.Common.Basic.MatchBrowser;
using II.RPA.WebBrowser.Common.MultiInstance;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace II.RPA.WebBrowser.Common.Basic
{
    public class BrowserWindowProvider
    {
        private static ILogger logger;
        /// <summary>
        /// 常规的窗口匹配器
        /// </summary>
        private static List<WindowMatcher> windowMatchers;
        private static List<WindowMatcher> embeddedIE_windowMatchers;
        static BrowserWindowProvider()
        {
            logger = Logger.OfContext();
            windowMatchers = new List<WindowMatcher>();
            embeddedIE_windowMatchers = new List<WindowMatcher>();
        }
        #region 添加窗口匹配器
        public static void AddWindowMatcher(WindowMatcher windowMatcher)
        {
            windowMatchers.Add(windowMatcher);
        }

        #endregion

        #region 设置“应用内嵌IE”窗口匹配器
        public static void AddAppEmbeddedIEMatcher(WindowMatcher windowMatcher)
        {
            embeddedIE_windowMatchers.Add(windowMatcher);
        }
        #endregion

        #region 对窗口匹配器按照浏览器处理优先级进行排序

        /// <summary>
        /// 对窗口匹配器按照浏览器处理优先级进行排序
        /// </summary>
        /// <param name="eGuiTypes"></param>
        public static void OrderWindowMatchers(List<string> eGuiTypes)
        {
            List<WindowMatcher> _windowMatchers = new List<WindowMatcher>();
            foreach (var eGuiType in eGuiTypes)
            {
                _windowMatchers.AddRange(windowMatchers.Where(m => m.WindowType.ToString().Equals(eGuiType, StringComparison.OrdinalIgnoreCase)));
            }
            windowMatchers = _windowMatchers;
        }
        #endregion

        #region 遍历Z序窗口 使用匹配器匹配浏览器类型

        #region 获取Z序首个匹配条件的窗口
        public static BrowserWindow GetFirstBrowserByZOrder()
        {
            return GetFirstBrowserByZOrder(null, WindowType.None);
        }
        public static BrowserWindow GetFirstBrowserByZOrder(eGuiType guiType)
        {
            return GetFirstBrowserByZOrder(null, guiType);
        }
        public static BrowserWindow GetFirstBrowserByZOrder(WindowType windowType)
        {
            return GetFirstBrowserByZOrder(null, windowType);
        }
        public static BrowserWindow GetFirstBrowserByZOrder(Func<BrowserWindow, bool> predicate, eGuiType guiType)
        {
            WindowType windowType = (WindowType)Enum.Parse(typeof(WindowType), guiType.ToString(), true);
            return GetFirstBrowserByZOrder(predicate, windowType);
        }
        public static BrowserWindow GetFirstBrowserByZOrder(Func<BrowserWindow, bool> predicate, WindowType windowType)
        {
            try
            {
                logger.LogDebug($"开始按照Z序 匹配首个浏览器窗口类型");
                List<BrowserWindow> iconicWnds = new List<BrowserWindow>();
                var matchers = windowType == WindowType.None ? windowMatchers : windowMatchers.Where(m => (m.WindowType & windowType) > 0).ToList();
                DesktopWindow desktopWindow = new DesktopWindow();
                Func<IntPtr, BrowserWindow> func = (hWnd) =>
                {
                    BrowserWindow browserWindow = new BrowserWindow(hWnd);
                    if (!browserWindow.IsIconic)
                    {
                        if (MatchBrowserWindow(matchers, predicate, browserWindow))
                            return browserWindow;
                    }
                    else
                    {
                        iconicWnds.Add(browserWindow);
                    }
                    return null;
                };
                var matchedWnd = desktopWindow.MatchDesktopWindowInZorder(func).FirstOrDefault();
                //matchedWnd = matchedWnd ?? desktopWindow.MatchDesktopWindowInZorder(func, FirstWndSourceType.ForegroundWnd).FirstOrDefault();
                return matchedWnd ?? iconicWnds.FirstOrDefault(wnd => MatchBrowserWindow(matchers, predicate, wnd));
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"按z序获取浏览器窗口异常");
            }
            finally
            {
                logger.LogDebug($"按照Z序 匹配首个浏览器窗口类型 结束");
            }
            return null;
        }

        #endregion

        #region 获取Z序所有匹配条件的窗口

        public static List<BrowserWindow> GetAllBrowserByZOrder(Func<BrowserWindow, bool> predicate, eGuiType guiType)
        {
            WindowType windowType = (WindowType)Enum.Parse(typeof(WindowType), guiType.ToString(), true);
            return GetAllBrowserByZOrder(predicate, windowType);
        }
        public static List<BrowserWindow> GetAllBrowserByZOrder(Func<BrowserWindow, bool> predicate, WindowType windowType)
        {
            try
            {
                logger.LogDebug($"开始按照Z序 匹配首个浏览器窗口类型");
                List<BrowserWindow> iconicWnds = new List<BrowserWindow>();
                var matchers = windowType == WindowType.None ? windowMatchers : windowMatchers.Where(m => (m.WindowType & windowType) > 0).ToList();
                DesktopWindow desktopWindow = new DesktopWindow();
                Func<IntPtr, BrowserWindow> func = (hWnd) =>
                {
                    BrowserWindow browserWindow = new BrowserWindow(hWnd);
                    if (!browserWindow.IsIconic)
                    {
                        if (MatchBrowserWindow(matchers, predicate, browserWindow))
                            return browserWindow;
                    }
                    else
                    {
                        iconicWnds.Add(browserWindow);
                    }
                    return null;
                };
                var matchedWnd = desktopWindow.MatchDesktopWindowInZorder(func).ToList();
                matchedWnd.AddRange(iconicWnds.Where(wnd => MatchBrowserWindow(matchers, predicate, wnd)));
                return matchedWnd;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"按z序获取浏览器窗口异常");
            }
            finally
            {
                logger.LogDebug($"按照Z序 匹配首个浏览器窗口类型 结束");
            }
            return null;
        }

        #endregion

        private static bool MatchBrowserWindow(List<WindowMatcher> matchers, Func<BrowserWindow, bool> predicate, BrowserWindow wnd)
        {
            logger.LogDebugBlock(() =>
            {
                logger.LogDebug($"桌面窗口({(wnd.IsIconic ? "最小化" : "非最小化")})：{wnd.Handle}({wnd.Handle.ToString("x4")}),{wnd.ClassName},{wnd.Title}");
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="matchIEMode">浏览器IE模式 false忽略IE模式</param>
        /// <returns></returns>
        public static BrowserWindow MatchBrowserWindowByHandle(IntPtr hWnd, bool matchIEMode = true)
        {
            BrowserWindow windowInfo = null;
            try
            {
                logger.LogDebug($"开始匹配浏览器窗口类型");
                BrowserWindow webWindowInfo = new BrowserWindow(hWnd);
                var windowMatcher = windowMatchers.FirstOrDefault(matcher => matcher.IsMatch(webWindowInfo));
                if (windowMatcher != null && matchIEMode)
                {
                    if (webWindowInfo.IsEmbeddedIE())
                    {
                        webWindowInfo.WindowType |= WindowType.IE;
                        webWindowInfo.SpecifiedWindow = true;
                    }
                }
                if (windowMatcher != null)
                {
                    windowInfo = webWindowInfo;
                    return windowInfo;
                }
                else
                {
                    return EmbeddedIEWindowProvider.MatchWindowBrowserByHandle(hWnd);
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"匹配浏览器窗口类型 异常");
            }
            finally
            {
                logger.LogDebug($"匹配浏览器窗口类型 结束");
            }
            return windowInfo;
        }
        public static bool IsBrowserWindow(IntPtr hWnd, WindowType windowType, out BrowserWindow windowInfo)
        {
            windowInfo = MatchBrowserWindowByHandle(hWnd, windowType);
            return windowInfo != null;
        }
        public static BrowserWindow MatchBrowserWindowByHandle(IntPtr hWnd, eGuiType guiType)
        {
            WindowType windowType = (WindowType)Enum.Parse(typeof(WindowType), guiType.ToString(), true);
            return MatchBrowserWindowByHandle(hWnd, windowType);
        }
        public static BrowserWindow MatchBrowserWindowByHandle(IntPtr hWnd, WindowType windowType)
        {
            try
            {
                logger.LogDebug($"判断窗口是否是指定的浏览器类型");
                BrowserWindow windowInfo = new BrowserWindow(hWnd);
                var windowMatcher = windowMatchers.FirstOrDefault(matcher => matcher.WindowType == windowType && matcher.IsMatch(windowInfo));
                if (windowMatcher != null)
                {
                    return windowInfo;
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"判断窗口是否是指定的浏览器类型 异常");
            }
            finally
            {
                logger.LogDebug($"判断窗口是否是指定的浏览器类型 结束");
            }
            return null;
        }
        #endregion

        #region 判断指定窗口 是否是指定的浏览器类型
        public static bool BrowserWindowTypeCheck(BrowserWindow webWindowInfo)
        {
            try
            {
                logger.LogDebug($"开始匹配浏览器窗口类型");
                var windowMatcher = windowMatchers.FirstOrDefault(matcher => matcher.WindowType == webWindowInfo.WindowType && matcher.IsMatch(webWindowInfo));
                //if (windowMatcher != null)
                //{
                //    if (webWindowInfo.IsEmbeddedIE())
                //    {
                //        webWindowInfo.WindowType |= WindowType.IE;
                //        webWindowInfo.SpecifiedWindow = true;
                //    }
                //}
                return windowMatcher != null;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"匹配浏览器窗口类型 异常");
                throw;
            }
            finally
            {
                logger.LogDebug($"匹配浏览器窗口类型 结束");
            }
            return false;
        }
        #endregion

        #region 自动匹配运行组件需要的浏览器窗口信息
        public static BrowserWindow AutoMatchBrowserWindowBeforeExecut(string selectorType, string processName, ExtendedTags extendedTags)
        {
            BrowserWindow windowInfo;
            processName = processName.Replace(".exe", "");
            //规则明确应用内嵌IE只能在应用内嵌IE中匹配，不能在浏览器中匹配
            //ExtendedTags.EmbeddedIE条件子700sp3之后拾取的元素有效， !EmbeddedIEWindowProvider.IsSupportedBrowser(processName)逻辑留做兼容老版本使用
            if (selectorType.EqualIgnoreCase("ie") &&
                (extendedTags.HasFlag(ExtendedTags.EmbeddedIE) || !EmbeddedIEWindowProvider.IsSupportedBrowser(processName)))
            {
                windowInfo = EmbeddedIEWindowProvider.GetFirstEmbeddedIEWindowByZOrder(wnd =>
                {
                    return wnd.ProcessName.EqualsOrIsMatch(processName);
                }) ?? EmbeddedIEWindowProvider.GetFirstEmbeddedIEWindowByZOrder();
            }
            else
            {
                windowInfo = GetFirstBrowserByZOrder();
                if (windowInfo == null)
                {
                    ChromiumBrowserDiscovery.TryDiscoverFromNMH();
                    windowInfo = GetFirstBrowserByZOrder();
                }
            }
            return windowInfo;
        }
        #endregion

        /// <summary>
        /// 是否是支持的浏览器进程
        /// </summary>
        /// <param name="processName"></param>
        /// <returns></returns>
        public static bool IsSupportedBrowser(string processName)
        {
            var allSupportBrowserPNames = windowMatchers.SelectMany(wm => wm.MatchConfigs.SelectMany(config => config.ProcessFeatures));
            return allSupportBrowserPNames.Any(pName => pName.EqualsOrIsMatch(processName));
        }

    }
}

