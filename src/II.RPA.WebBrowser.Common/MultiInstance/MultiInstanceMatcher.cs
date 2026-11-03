using FlaUI.Core.AutomationElements;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.WindowMatch;
using II.RPA.Plugin.GUI.Commons.WindowMatch.Entities;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.NMH;
using II.RPA.WebBrowser.Common.Utils;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.WebBrowser.Common.MultiInstance
{
    internal class MultiInstanceMatcher
    {
        private static ILogger logger = Logger.OfContext();
        public bool AllowMappingViaWindowActivation = false;
        internal void CheckProfileInstance(BrowserWindow browserWindow, out List<int> hostIdList)
        {
            if (browserWindow.WindowType.HasFlag(WindowType.IE))
            {
                hostIdList = new List<int> { -(int)WindowType.IE };
            }
            else if (browserWindow.Handle == IntPtr.Zero ||
                     BrowserSpecRepository.GetBrowserSepc(browserWindow)?.NMHProcessNames?.Length == 0)
            {
                hostIdList = new List<int> { -(int)browserWindow.WindowType };
            }
            else
            {
                hostIdList = GetHostProcessIdsByBrowWnd(browserWindow);
                if (hostIdList.Count == 0)          // 没抓到进程，同样兜底
                    hostIdList = new List<int> { -(int)browserWindow.WindowType };
            }
            BrowserContextInventory.Instance.RemoveInvalidContext();
            BrowserContextInventory.Instance.TryRegisterEntries(hostIdList, browserWindow);
        }

        internal bool CheckProfileNMHProcId(int NMHProcId, out int newNMHProcId)
        {
            newNMHProcId = -1;

            if (!BrowserContextInventory.Instance.RemoveIfHostIdInvalid(NMHProcId, out var browserContext))
            {
                return true;
            }


            if (browserContext == null)
            {
                return false;
            }

            var firstChromeWnd = BrowserWindowProvider.GetFirstBrowserByZOrder(browserContext.Operator.GuiType);
            if (firstChromeWnd == null)
            {
                return false;
            }
            browserContext.BrowserWndHandles.RemoveAll(h => !NativeMethods.IsWindow(h));
            browserContext.BrowserWndHandles.Reverse();
            if (browserContext.BrowserWndHandles.Contains(firstChromeWnd.Handle))
            {
                browserContext.BrowserWndHandles.Remove(firstChromeWnd.Handle);
                browserContext.BrowserWndHandles.Insert(0, firstChromeWnd.Handle);
            }

            foreach (var wndhandle in browserContext.BrowserWndHandles)
            {
                var browserWindow = new BrowserWindow(wndhandle);
                if (!BrowserWindowProvider.BrowserWindowTypeCheck(browserWindow) || !BuildBrowserWindowMap(browserWindow)) continue;
                var newBrowserContext = BrowserContextInventory.Instance.GetBrowserContextByWindowHandle(wndhandle);
                if (newBrowserContext != null)
                {
                    newBrowserContext.BrowserWndHandles.AddRange(browserContext.BrowserWndHandles.Except(newBrowserContext.BrowserWndHandles));
                    newNMHProcId = newBrowserContext.NativeMessagingHostId;
                    break;
                }
            }
            return false;
        }
        internal List<int> GetHostProcessIdsByBrowWnd(BrowserWindow browserWindow)
        {
            if (browserWindow.Handle == IntPtr.Zero)
            {
                logger.LogWarning($"浏览器窗口句柄为零，无法获取对应的 NativeMessagingHost 进程 ID，返回空列表");
                return new List<int>();
            }

            var matchConfig = browserWindow.HitMatchHandler.MatchConfig;
            var browserSpec = BrowserSpecRepository.GetSpec(matchConfig.ConfigKey);

            var hostIds = BrowserProcessMatcher.FindHostProcessIdsFromWindow(
                browserWindow.Handle,
                browserSpec.NMHProcessNames,
                new[] { browserWindow.ProcessName });

            return hostIds;

        }
        public bool BuildBrowserWindowMap(BrowserWindow browserWindow)
        {
            CheckProfileInstance(browserWindow, out var hostIdList);
            if (browserWindow.WindowType.HasFlag(WindowType.IE))
            {
                return browserWindow.WindowType == WindowType.IE ? TryUpdateContextWndMap(-1 * (int)WindowType.IE, browserWindow) : true;
            }
            else if (BrowserSpecRepository.GetBrowserSepc(browserWindow)?.NMHProcessNames?.Length == 0)
            {
                return TryUpdateContextWndMap(-1 * (int)browserWindow.WindowType, browserWindow);
            }
            if (browserWindow.Handle == IntPtr.Zero) return true;
            var allBrowserContexts = BrowserContextInventory.Instance.GetAllBrowserContext()
                                                                     .Where(c => c.BrowserSpec.WindowType == browserWindow.WindowType)
                                                                     .OrderBy(c => c.BrowserWndHandles.Count)
                                                                     .ToList();

            var ret = BuildBrowserWindowMap(browserWindow, allBrowserContexts);
            logger.LogDebugBlock(() =>
            {
                logger.LogDebug($"allProfileEntries:");
                foreach (var browserContext in allBrowserContexts)
                {
                    foreach (var hWnd in browserContext.BrowserWndHandles)
                    {
                        WindowObject wndObj = new WindowObject(hWnd);
                        wndObj.PrintWindowInfo();
                        logger.LogDebug($"NativeMessagingHostId:{browserContext.NativeMessagingHostId}");
                    }
                }
            });
            if (!ret)
            {
                var processIds = GetHostProcessIdsByBrowWnd(browserWindow);
                if (processIds.Count == 1)
                {
                    return TryUpdateContextWndMap(processIds[0], browserWindow);
                }
            }
            return ret;
        }
        public bool BuildBrowserWindowMap(BrowserWindow browserWindow, List<BrowserContext> browserContext)
        {
            foreach (var context in browserContext)
            {
                if (BuildBrowserWindowMap(browserWindow, context))
                {
                    return true;
                }
            }
            return false;
        }


        public bool BuildBrowserWindowMap(BrowserWindow browserWindow, BrowserContext browserContext)
        {
            var windowMap = browserContext.Browser.GetWindowMap();
            if (windowMap.Values.Contains(browserWindow.Handle.ToInt64()))
            {
                if (!browserContext.BrowserWndHandles.Contains(browserWindow.Handle))
                    browserContext.BrowserWndHandles.Add(browserWindow.Handle);
                BrowserContextInventory.Instance.UpdateWindowHandles(browserContext);
                return true;
            }
            if (!AllowMappingViaWindowActivation)
            {
                logger.LogDebug($"未在浏览器窗口映射中找到句柄 {browserWindow.Handle}，且未启用通过窗口激活匹配活动标签页，返回 false");
                return false;
            }
            logger.LogDebug($"未在浏览器窗口映射中找到句柄 {browserWindow.Handle}，尝试匹配活动标签页");
            PredicateFilter<BrowserTabPage> matchFilter = PredicateFilter.NewFilter<BrowserTabPage>((tab) => !tab.Url.EqualsOrIsMatch("chrome-extension://.*/iirpa/ServiceWorkerPage.html"));
            var activeTabs = browserContext.Browser.MatchActiveTabPages(matchFilter, new List<IntPtr>() { browserWindow.Handle }, true).ToList();
            browserWindow.SetForeground();

            var tabPage = RPAUtils.RetryWait(() =>
            {
                var acTab = activeTabs.FirstOrDefault(tab =>
                {
                    tab.GetTabDetail(true);
                    return tab.IsFocused;
                });
                if (acTab == null)
                    browserWindow.SetForeground();
                return acTab;
            }, 2000, 20);
            if (tabPage != null)
            {
                if (!browserContext.BrowserWndHandles.Contains(browserWindow.Handle))
                    browserContext.BrowserWndHandles.Add(browserWindow.Handle);
                BrowserContextInventory.Instance.UpdateWindowHandles(browserContext);
                return true;
            }
            return false;
        }

        private Process[] GetAllNMHProcesses(BrowserSpec browserSpec)
        {
            if (browserSpec.NMHProcessNames.Length == 0)
            {
                logger.LogWarning($"浏览器 {browserSpec.WindowType.ToString()} 的 NMHProcessNames 为空，无法获取对应的 NativeMessagingHost 进程，返回空数组");
                return new Process[0];
            }
            logger.LogDebug($"获取浏览器 {browserSpec.WindowType.ToString()} 的 NMHProcessNames: {browserSpec.NMHProcessNames.ToJson()}");
            List<Process> processes = new List<Process>();
            foreach (var procName in browserSpec.NMHProcessNames)
            {
                var hostProcesses = Process.GetProcessesByName(procName);
                processes.AddRange(hostProcesses);
            }
            logger.LogDebugBlock(() =>
            {
                logger.LogDebug($"获取到II.RPA.NativeMessagingHost 进程：{processes.Select(p => p.Id).ToJson()}");
            });
            return processes.ToArray();
        }

        private bool TryUpdateContextWndMap(int hostId, BrowserWindow browserWindow)
        {
            var browserContext = BrowserContextInventory.Instance.GetBrowserContextByHostId(hostId);
            if (browserContext == null) return false;
            if (!browserContext.BrowserWndHandles.Contains(browserWindow.Handle))
                browserContext.BrowserWndHandles.Add(browserWindow.Handle);
            BrowserContextInventory.Instance.UpdateWindowHandles(browserContext);
            return true;
        }
    }
}
