using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.WindowMatch;
using II.RPA.Plugin.GUI.Commons.WindowMatch.Entities;
using II.RPA.WebBrowser.Common.Basic.BrowserManagers;
using II.RPA.WebBrowser.Common.Basic.Params;
using II.RPA.WebBrowser.Common.MultiInstance;
using II.RPA.WebBrowser.Common.Utils;
using II.RPA.WinAutomation.CommonUtility;
using II.RPA.WinAutomation.CommonUtility.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Permissions;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.WebBrowser.Common.Basic
{
    public abstract class Browser
    {
        protected ILogger logger = Logger.OfContext();

        public int WindowId = -1;

        #region 派生类要实现的抽象属性
        public virtual string CreateProcessArgs { get; set; }

        public virtual List<string> IgnoreUrl { get; } = new List<string>();
        public virtual bool IgorneCreateSetting => false;
        public abstract eGuiType GuiType { get; }
        /// <summary>
        /// 本息消息主机进程名
        /// </summary>
        public virtual string NativeMessageHostProcessName { get; }
        /// <summary>
        /// 本息消息主机进程ID
        /// </summary>
        public int NativeMessageHostProcessID { get; set; }
        /// <summary>
        /// 浏览器进程名 如:chrome
        /// </summary>
        public abstract string BrowserProcessName { get; }

        public virtual string[] BrowserProcessNames { get; } = new string[0];
        /// <summary>
        /// 通信服务的key,作为各种通信管道地址的基础数据
        /// </summary>
        protected virtual string ServiceKey { get; }
        /// <summary>
        /// 浏览器扩展程序的注册表信息地址,例（chrome）:Software\\Google\\Chrome\\Extensions
        /// </summary>
        protected virtual string ExtensionsRegistryKey { get; }
        /// <summary>
        /// 通信服务的key,作为各种通信管道地址的基础数据
        /// </summary>
        protected virtual string[] ExtensionIDs { get; }
        protected virtual eErrorMessageType ExtensionUnInstalledErrorMessageType => eErrorMessageType.Error_None;
        protected virtual eErrorMessageType ExtensionUnopenErrorMessageType => eErrorMessageType.Error_None;
        protected virtual eErrorMessageType ExtensionRefuseAccess => eErrorMessageType.Error_ExtensionRefuseAccess;

        /// <summary>
        /// 扩展程序白名单
        /// </summary>
        public virtual string[] ExtensionsWhitelist { get; set; }
        #endregion

        #region 派生类要实现的抽象行为
        //public virtual BrowserTabPage CreateBrowserTabPage(BrowserWindow WebWindowInfo, int tabId = -1)
        //{
        //    var tabPage = CreateBrowserTabPage(tabId);
        //    tabPage.SetWebWindowInfo(WebWindowInfo);
        //    return tabPage;
        //}
        // public abstract BrowserTabPage CreateBrowserTabPage(int tabId = -1);
        public abstract BrowserTabPage CreateBrowserTabPage(ParamCreateBrowserTabPageModel paramModel);

        /// <summary>
        /// 浏览器程序地址
        /// </summary>
        public virtual string BrowserAppPath { get; set; }
        public abstract string GetBrowserAppPath();
        #endregion
        /// <summary>
        /// 浏览器连接，与浏览器通信
        /// </summary>
        //protected IBrowserConnectionNew Connecttion { get; set; }
        /// <summary>
        /// 浏览器数据提供正
        /// </summary>
        public virtual WebDataProvider WebDataProvider { get; set; }
        public virtual BrowserShutdownManagerBase BrowserShutdownManager { get; set; }

        public virtual bool NeedCareProfileOccupation { get; } = false;


        protected readonly object objConnecttionLock = new object();
        public MatchConfig BrowserMatchConfig { get; set; }

        public Browser()
        {
            WindowHandleAndIdMap = new ConcurrentDictionary<IntPtr, int>();
            ConnectionConfig connConfig = this.GetConnectionConfig();
            this.WebDataProvider = new WebDataProvider(connConfig);
            this.BrowserShutdownManager = new BrowserShutdownManager(this);
            this.SetExtensionsWhitelist();
        }

        #region Connection 相关

        #region 连接类型，可通过类型重写，切换需要的connection
        public virtual Type ServiceConnectionType => typeof(IPCConnection);
        public virtual Type NamePipeConnectionType => typeof(NamePipeConnection);
        public virtual Type SignalRConnectionType => typeof(ServiceHubConnection);
        /// <summary>
        /// 本息消息主机进程名
        /// </summary>
        public virtual string ServiceHubProcessName => "II.RPA.ServiceHub";
        public virtual string SignalRUrl => null;

        #endregion


        public virtual ConnectionConfig GetConnectionConfig()
        {
            return new ConnectionConfig
            {
                GuiType = this.GuiType,
                NativeMessageHostProcessName = this.NativeMessageHostProcessName,
                NativeMessageHostProcessID = this.NativeMessageHostProcessID,
                BrowserProcessName = this.BrowserProcessName,
                ServiceKey = this.ServiceKey,
                ServiceConnectionType = this.ServiceConnectionType,
                NamePipeConnectionType = this.NamePipeConnectionType,
                SignalRConnectionType = this.SignalRConnectionType,
                SignalRUrl = this.SignalRUrl,
                CheckBrowserExtensions = this.CheckBrowserExtensions,
                DetectionMessageProcess = this.DetectionMessageProcess,
                ExtensionUnInstalledErrorMessageType = this.ExtensionUnInstalledErrorMessageType,
                ExtensionUnopenErrorMessageType = this.ExtensionUnopenErrorMessageType
            };
        }

        public virtual void StopConnecttion()
        {
            this.WebDataProvider.Connection?.Stop();
            this.WebDataProvider.Connection = null;
        }
        #endregion

        #region 插件检测、NativeMeaageHost检测相关

        /// <summary>
        /// 判断本地消息主机是否已经启动
        /// </summary>
        public bool CheckNativeMeaageHostIsStarted(int detectionTime = 10)
        {
            if (!this.CheckBrowserExtensions())
            {
                throw new RPAException(this.GuiType, this.ExtensionUnInstalledErrorMessageType);
            }
            try
            {
                if (DetectionMessageProcess(false, 10) == 0)
                {
                    logger.LogDebug($"未检测到NativeMeaageHost进程，可能浏览器插件未启动");
                    throw new RPAException(this.GuiType, this.ExtensionUnopenErrorMessageType);
                }
            }
            catch (Win32Exception win32Ex)
            {
                if (win32Ex.NativeErrorCode == 5)
                {
                    throw new RPAException(this.GuiType, this.ExtensionRefuseAccess);
                }
                throw;
            }
            return true;
        }

        /// <summary>
        /// 判断浏览器插件是否已经安装
        /// </summary>
        public virtual bool CheckBrowserExtensions()
        {
            return true;
        }
        /// <summary>
        /// 检测NativeMeaageHost进程 
        /// </summary>
        /// <param name="detectionTime">检测是时间</param>
        /// <returns>检测到返回true;指定时间段内未检测到，返回false</returns>
        public virtual int DetectionMessageProcess(bool isServiceHub, int detectionTime)
        {
            var hostId = RPAUtils.RetryWait(() =>
            {
                if (this.NativeMessageHostProcessID <= 0)
                {
                    var browserWindow = BrowserWindowProvider.GetFirstBrowserByZOrder(this.GuiType);
                    var nmh_pid = browserWindow == null ? NativeMessageHostProcessID : BrowserContextProvider.Instance.GetBrowserHostId(browserWindow);
                    logger.LogDebugBlock(() =>
                    {
                        logger.LogDebug($"检测NativeMeaageHost进程（大概率为首次启动浏览器时），检测结果 NativeMessageHostProcessID:{nmh_pid}");
                    });
                    return nmh_pid;
                }
                if (BrowserContextProvider.Instance.CheckProfileNMHProcId(NativeMessageHostProcessID, out var newNMHProcId))
                {
                    return NativeMessageHostProcessID;
                }
                else if (newNMHProcId > 0)
                {
                    return newNMHProcId;
                }
                logger.LogDebugBlock(() =>
                {
                    logger.LogDebug($"未检测到NativeMeaageHost进程，可能浏览器插件未启动,将进行检测NMH进程信息");

                });
                return default;
            }, (uint)detectionTime, 50);
            if (this.NativeMessageHostProcessID != hostId)
            {
                this.StopConnecttion();
                this.NativeMessageHostProcessID = hostId;
            }
            return this.NativeMessageHostProcessID;
        }


        #endregion

        #region 设置扩展程序白名单（如IE Tab）
        public virtual void SetExtensionsWhitelist()
        {
            this.ExtensionsWhitelist = new string[0];
        }
        #endregion

        #region 获取浏览器窗口信息
        /// <summary>
        /// 激活状态tab数量，每次获取激活页数据时，触发更新
        /// </summary>
        public virtual int ActiveTabCount { get; set; } = 0;

        public virtual ConcurrentDictionary<IntPtr, int> WindowHandleAndIdMap { get; set; } = null;

        #region 浏览器WindowId与浏览器窗口句柄 关系
        public virtual int GetWindowIdFromWindowHandle(IntPtr hWnd)
        {
            if (WindowHandleAndIdMap.ContainsKey(hWnd))
            {
                return WindowHandleAndIdMap[hWnd];
            }
            logger.LogDebug($"未获取到匹配的WindowId，尝试重新匹配");
            BrowserWindow browserWindow = new BrowserWindow(hWnd);
            var activeTabs = this.MatchActiveTabPages(null, new List<IntPtr>() { hWnd }, true).ToList();
            if (activeTabs.Count == 1)
            {
                this.WindowHandleAndIdMap.TryAdd(browserWindow.Handle, activeTabs[0].WindowId);
            }
            else if (activeTabs.Count > 1)
            {
                BuildWindowIdHandleMap(activeTabs);
            }
            if (WindowHandleAndIdMap.ContainsKey(hWnd))
            {
                return WindowHandleAndIdMap[hWnd];
            }
            logger.LogWarning($"未获取到匹配的WindowId");
            logger.LogDebugBlock(() =>
            {
                activeTabs.ForEach(tab => { logger.LogDebug($"激活页：{tab.Title}"); });
            });
            browserWindow.PrintWindowInfo();
            return -1;
        }
        public virtual void BuildWindowIdHandleMap(List<BrowserTabPage> activeTabs)
        {
            foreach (var tabPage in activeTabs)
            {
                if (this.WindowHandleAndIdMap.Any(pair => pair.Value == tabPage.WindowId))
                {
                    continue;
                }
                int sameTitleTabCount = activeTabs.Count(tab => tab.Title == tabPage.Title);
                if (sameTitleTabCount > 1)
                {
                    tabPage.ActiveTab();
                    if (!RPAUtils.Retry(() => tabPage.IsFocused, 500, 10))
                    {
                        logger.LogDebug($"BuildWindowIdHandleMap：{tabPage.Title},{tabPage.IsFocused}");
                        break;
                    }
                }

                RPAUtils.Retry(() =>
                {
                    var webWindowInfo = this.GetFirstBrowserByZOrder(tabPage.Title);
                    if (webWindowInfo == null)
                    {
                        //考虑标题可能会快速变化的情况，导致无法匹配到窗口，每次重试清理缓存，标题不 变理论上不会执行
                        tabPage.ClearTabDetail();
                    }
                    else
                    {
                        this.WindowHandleAndIdMap.TryAdd(webWindowInfo.Handle, tabPage.WindowId);
                    }
                    return webWindowInfo != null;
                }, 1000, 1);
            }
        }
        #endregion

        #region 按照标题匹配浏览器窗口

        public virtual BrowserWindow GetFirstBrowserByZOrder(string title = "")
        {
            return BrowserWindowProvider.GetFirstBrowserByZOrder((wndInfo) => wndInfo.Title.ContainsIgnoreCase(title), this.GuiType);
        }
        public virtual List<BrowserWindow> GetAllBrowserByZOrder(string title = "")
        {
            return BrowserWindowProvider.GetAllBrowserByZOrder((wndInfo) => wndInfo.Title.ContainsIgnoreCase(title), this.GuiType);
        }
        public virtual BrowserWindow FindWindowInfo(string title = "")
        {
            logger.LogDebug($"FindWindowInfo:{title}");
            int timeout = string.IsNullOrEmpty(title) ? 0 : 1000;
            BrowserWindow webWindowInfo = RPAUtils.RetryWait(() => this.GetFirstBrowserByZOrder(title), (uint)timeout, 5);
            return webWindowInfo;
        }
        public virtual BrowserWindow FindWindowInfo(int hwnd)
        {
            logger.LogDebug($"FindWindowInfo Hwnd:{hwnd}");
            BrowserWindow webWindowInfo = null;
            RPAUtils.Retry(() =>
            {
                webWindowInfo = BrowserWindowProvider.GetFirstBrowserByZOrder((wndInfo) => wndInfo.Handle == (IntPtr)hwnd, this.GuiType);
                return webWindowInfo != null;
            }, 5);
            return webWindowInfo;
        }

        public virtual List<BrowserWindow> FindAllSameTitleWindow(string title = "")
        {
            logger.LogDebug($"FindWindowInfo:{title}");
            List<BrowserWindow> browserWnds = null;
            int timeout = string.IsNullOrEmpty(title) ? 0 : 500;
            RPAUtils.Retry(() =>
            {
                browserWnds = this.GetAllBrowserByZOrder(title);
                return browserWnds.Count > 0;
            }, timeout, 5);
            return browserWnds.Count > 0 ? browserWnds : null;
        }
        #endregion

        #endregion

        #region 获取所有窗口id

        public virtual List<int> GetNormalWindowAll()
        {
            var results = this.WebDataProvider.GetNormalWindowAll();
            if (!results.Any())
            {
                return new List<int>();
            }
            List<int> wndList = new List<int>();
            foreach (var result in results)
            {
                if (result["focused"].ToString().ToLower() == "true")
                {
                    if (wndList.Count > 0)
                        wndList.Clear();
                    var Id = result["id"].ToObject<int>();
                    wndList.Add(Id);
                    return wndList;
                }
                var wndId = result["id"].ToObject<int>();
                wndList.Add(wndId);
            }
            wndList.Sort();
            wndList.Reverse();
            return wndList;
        }

        #endregion
        public virtual void ActiveWindow(int windowId)
        {
            this.WebDataProvider.ActiveWindow(windowId);
        }
        #region tab信息 相关
        public virtual int CreateTabInWindow(ParamCreateTabModel param)
        {
            var result = this.WebDataProvider.CreateTabInWindow(param);
            if (result != null)
            {
                var createtabId = Convert.ToInt32(result.Value<string>("id"));
                return createtabId;
            }
            else
            {
                return -1;
            }
        }
        public virtual int Create(ParamCreateTabModel param)
        {

            var result = this.WebDataProvider.Create(param);
            if (result != null)
            {
                var createtabId = Convert.ToInt32(result.Value<string>("id"));
                return createtabId;
            }
            else
            {
                return -1;
            }
        }

        #region query tabs

        public virtual List<int> NormalTabIds(List<IntPtr> wndHandles = null)
        {
            var ids = this.WebDataProvider.NormalTabIds(new ParamGetTabIdsModel
            {
                WndHandles = wndHandles
            });
            if (!ids.Any())
            {
                return new List<int>();
            }
            var tabIdList = ids.ToString().ToObject<List<int>>();
            return tabIdList;
        }
        public virtual List<int> GetAllTabIds(List<IntPtr> wndHandles = null)
        {
            var ids = this.WebDataProvider.GetAllTabIds(new ParamGetTabIdsModel
            {
                WndHandles = wndHandles
            });
            if (!ids.Any())
            {
                return new List<int>();
            }
            var tabIdList = ids.ToString().ToObject<List<int>>();
            if (this.GuiType != eGuiType.IE)
            {
                tabIdList.Sort();
                tabIdList.Reverse();
            }
            return tabIdList;
        }
        public List<int> TryGetActiveTabIds()
        {
            try
            {
                var ids = this.GetActiveTabIds();
                //if (ids.Count() < 1)
                //{
                //    throw new RPAException(eErrorMessageType.Error_PageNotFound);
                //}
                return ids;
            }
            catch
            {
                return new List<int>();
            }
        }

        /// <summary>
        /// 获取所有的激活tabId
        /// </summary>
        /// <param name="wndHandles"></param>
        /// <param name="original">是否元原始数据，默认会在浏览器插件中进行过滤</param>
        /// <returns>激活的tabId,按ID值降序排列</returns>
        public virtual List<int> GetActiveTabIds(List<IntPtr> wndHandles = null, bool original = false)
        {
            JToken activeIds = this.WebDataProvider.GetActiveTabIds(new ParamGetTabIdsModel
            {
                Original = original,
                WndHandles = wndHandles
            });
            var activetabIdList = new List<int>();
            if (activeIds?.Any() == true)
            {
                activetabIdList = activeIds.ToString().ToObject<List<int>>();
            }
            if (this.GuiType != eGuiType.IE)
            {
                activetabIdList.Sort();
                activetabIdList.Reverse();
            }
            //if (!original)
            this.ActiveTabCount = activetabIdList.Count;
            return activetabIdList;
        }
        #endregion
        /// <summary>
        /// 获取当前激活页面tabId
        /// </summary>
        /// <param name="wndHandles">IE 浏览器使用，如果未指定匹配窗口，则使用所有IE浏览器窗口</param>
        /// <returns></returns>
        public virtual IEnumerable<BrowserTabPage> AcquireAllActiveTab(List<IntPtr> wndHandles = null, PredicateFilter<BrowserTabPage> tabFilter = null)
        {
            var activetabIds = this.GetActiveTabIds(wndHandles);
            return this.MatchTabPages(activetabIds, tabFilter);
        }
        /// <summary>
        /// 获取当前激活页面tabId
        /// </summary>
        /// <param name="wndHandles">IE 浏览器使用，如果未指定匹配窗口，则使用所有IE浏览器窗口</param>
        /// <returns></returns>
        public virtual BrowserTabPage AcquireCurrentActiveTab(List<IntPtr> wndHandles = null, PredicateFilter<BrowserTabPage> tabFilter = null)
        {
            try
            {
                var tabPages = this.AcquireAllActiveTab(wndHandles, tabFilter);
                BrowserTabPage firstTabpage = null;
                foreach (var tabPage in tabPages)
                {
                    if (tabPage.IsFocused)
                    {
                        return tabPage;
                    }
                    firstTabpage = firstTabpage ?? tabPage;
                }
                return firstTabpage;
            }
            catch (RPAException ex)
            {
                throw ex;
            }
            catch (Exception e)
            {
                throw new RPAException(eErrorMessageType.Error_PageNotFound, e);
            }
        }
        /// <summary>
        /// 按照 Active IsFocused IsPopup 优先级 返回首个符合要求的 BrowserTabPage
        /// </summary>
        /// <param name="title"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public virtual BrowserTabPage MatchTabPage(string title = null, string url = null)
        {
            title = title ?? "";
            url = url ?? "";
            string titlePattern = string.IsNullOrEmpty(title) ? "" : II.RPA.Plugin.Core.ConstFields.PATTERN_EQUAL;
            string urlPattern = string.IsNullOrEmpty(url) ? "" : II.RPA.Plugin.Core.ConstFields.PATTERN_EQUAL;
            var cormpareTitle = Plugin.Core.CompareMode.CormpareFactory.GenerateStringMode(titlePattern ?? "");
            var cormpareUrl = Plugin.Core.CompareMode.CormpareFactory.GenerateStringMode(urlPattern ?? "");
            bool supportRegular = true;

            PredicateFilter<BrowserTabPage> matchFilter = PredicateFilter.NewFilter<BrowserTabPage>((tabPage) => tabPage.IsMatch(title, cormpareTitle, url, cormpareUrl, supportRegular));
            return MatchTabPageByPriority(matchFilter);
        }
        /// <summary>
        /// 按照Active IsFocused IsPopup 优先级 返回首个符合要求的 BrowserTabPage
        /// </summary>
        /// <param name="tabFilter"></param>
        /// <param name="wndHandles">指定窗口句柄，主要针对IE页面生效</param>
        /// <returns></returns>
        public BrowserTabPage MatchTabPageByPriority(PredicateFilter<BrowserTabPage> tabFilter, List<IntPtr> wndHandles = null)
        {
            //所有激活tabid
            var tabIds = this.GetActiveTabIds(wndHandles);

            BrowserTabPage matchedTabpage = this.MatchTabPageByPriority(tabIds, tabFilter);
            if (matchedTabpage == null)
            {
                //获取所有tab 再过滤所有激活tab。(未开放直接获取非active接口，暂不增加)
                var allTabIds = this.GetAllTabIds(wndHandles);
                allTabIds.RemoveAll(id => tabIds.IndexOf(id) >= 0);
                matchedTabpage = this.MatchTabPageByPriority(allTabIds, tabFilter);
            }
            return matchedTabpage;
        }
        /// <summary>
        /// 在指定范围内，按照IsFocused IsPopup 优先级 返回首个符合要求的 BrowserTabPage
        /// </summary>
        /// <param name="tabIds">指定tab匹配范围</param>
        /// <param name="tabFilter"></param>
        /// <returns></returns>
        public BrowserTabPage MatchTabPageByPriority(List<int> tabIds, PredicateFilter<BrowserTabPage> tabFilter)
        {
            List<JToken> detailList = new List<JToken>();
            BrowserTabPage firstPopupTab = null;
            BrowserTabPage firstNormalTab = null;
            //优先焦点窗口，其次popup窗口，若无popup匹配，则使用首个匹配到的tab
            var matchedTabs = this.MatchTabPages(tabIds, tabFilter);
            foreach (var tabPage in matchedTabs)
            {
                if (tabPage.IsFocused)
                {
                    return tabPage;
                }
                else if (firstPopupTab == null && tabPage.IsPopup)
                {
                    firstPopupTab = tabPage;
                }
                else if (firstNormalTab == null)
                {
                    firstNormalTab = tabPage;
                }
            }
            return firstPopupTab ?? firstNormalTab;
        }

        #region 匹配所有满足条件tab信息
        /// <summary>
        /// 返回所有符合title、url要求的 BrowserTabPage
        /// </summary>
        /// <param name="title"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public IEnumerable<BrowserTabPage> MatchTabPages(string title = null, string url = null)
        {
            title = title ?? "";
            url = url ?? "";
            string titlePattern = string.IsNullOrEmpty(title) ? "" : II.RPA.Plugin.Core.ConstFields.PATTERN_EQUAL;
            string urlPattern = string.IsNullOrEmpty(url) ? "" : II.RPA.Plugin.Core.ConstFields.PATTERN_EQUAL;
            var cormpareTitle = Plugin.Core.CompareMode.CormpareFactory.GenerateStringMode(titlePattern ?? "");
            var cormpareUrl = Plugin.Core.CompareMode.CormpareFactory.GenerateStringMode(urlPattern ?? "");
            bool supportRegular = true;
            PredicateFilter<BrowserTabPage> matchFilter = new PredicateFilter<BrowserTabPage>();
            matchFilter.AddLast(tabPage => tabPage.IsMatch(title, cormpareTitle, url, cormpareUrl, supportRegular));
            return MatchAllTabPages(matchFilter);
        }
        /// <summary>
        /// 返回所有符合要求的 Active BrowserTabPage
        /// </summary>
        /// <param name="tabFilter"></param>
        /// <param name="wndHandles"></param>
        /// <returns></returns>
        public IEnumerable<BrowserTabPage> MatchActiveTabPages(PredicateFilter<BrowserTabPage> tabFilter, List<IntPtr> wndHandles, bool original)
        {
            var tabIds = this.GetActiveTabIds(wndHandles, original);
            return this.MatchTabPages(tabIds, tabFilter, original);
        }
        /// <summary>
        /// 返回所有符合要求的 BrowserTabPage
        /// </summary>
        /// <param name="tabFilter"></param>
        /// <returns></returns>
        public IEnumerable<BrowserTabPage> MatchAllTabPages(PredicateFilter<BrowserTabPage> tabFilter)
        {
            //获取所有tabid
            var tabIds = this.GetAllTabIds();
            var matchedTabs = this.MatchTabPages(tabIds, tabFilter);
            return matchedTabs;
        }
        /// <summary>
        /// 在指定范围内，返回所有符合要求的 BrowserTabPage
        /// </summary>
        /// <param name="tabIds"></param>
        /// <param name="tabFilter"></param>
        /// <returns></returns>
        private IEnumerable<BrowserTabPage> MatchTabPages(List<int> tabIds, PredicateFilter<BrowserTabPage> tabFilter, bool original = false)
        {
            return this.GetTabPages(tabIds, original).Where(tabPage => tabFilter == null || tabFilter.Run(tabPage));
        }
        /// <summary>
        /// 获取有效的BrowserTabPage
        /// </summary>
        /// <param name="tabIds"></param>
        /// <param name="original">原始数据</param>
        /// <returns></returns>
        private IEnumerable<BrowserTabPage> GetTabPages(List<int> tabIds, bool original)
        {
            foreach (var tabId in tabIds)
            {
                BrowserTabPage tabPage = this.CreateBrowserTabPage(new ParamCreateBrowserTabPageModel() { TabId = tabId });
                if (tabPage.GetTabDetail() == null)
                {
                    continue;
                }
                string pageUrl = tabPage.Url;
                if (!original && pageUrl.Contains("chrome-extension://")
                   && !this.ExtensionsWhitelist.Any(extensionId => pageUrl.Contains(extensionId)))//过滤后台background
                {
                    continue;
                }
                yield return tabPage;
            }
        }
        #endregion 匹配所有满足条件tab信息

        #endregion

        #region 浏览器操作
        public void Minimized()
        {
            this.WebDataProvider.Minimized();
        }
        public void Maximized(int windowId = -1)
        {
            this.WebDataProvider.Maximized(windowId);
        }
        public bool KillMainWindow()
        {
            #region [先关闭主窗口进程]
            var browserWnds = this.GetAllBrowserByZOrder();
            foreach (var browserWnd in browserWnds)
            {
                try
                {
                    using (var process = Process.GetProcessById(browserWnd.ProcessId))
                    {
                        process.Kill();
                        process.WaitForExit(1000);
                    }
                }
                catch
                {

                }
            }
            return browserWnds.Count > 0;
            #endregion
        }
        public virtual void CloseAll(bool killProcess, int timeout = 10000, bool isCloseAlert = false)
        {
            logger.LogDebug($"准备关闭 {this.BrowserProcessName} 浏览器");
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
                int timeOut = killProcess ? 2000 : 5000;
                this.WebDataProvider.CloseAll(timeOut, killProcess, isCloseAlert);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"调用浏览器CloseAll发生异常，仍将尝试通过结束浏览器进程的方式关闭进程");
            }
            timeout -= (int)stopWatch.ElapsedMilliseconds;
            stopWatch.Restart();
            WaitBrowserTabsClosed(timeout + 1000);
            if (killProcess)
            {
                timeout -= (int)stopWatch.ElapsedMilliseconds;
                stopWatch.Restart();
                Task.Delay(2000).Wait();//等待网页关闭后再杀进程，2000这个时间是拍脑袋得阈值，但这个时间修改要慎重
                WaitBrowserProcessExited(timeout);
            }
            stopWatch.Stop();
            if (stopWatch.ElapsedMilliseconds > timeout)
            {
                logger.LogDebug("关闭浏览器超时或异常");
            }
            else if (killProcess)
            {
                RPAUtils.Retry(this.IsNativeMessagingHostExited, timeout - (int)stopWatch.ElapsedMilliseconds);
            }
        }
        public virtual void WaitBrowserTabsClosed(int timeout = 10000)
        {
            logger.LogDebug($"等待浏览器tab页（可视窗口）消失");
            RPAUtils.Retry(() =>
            {
                if (BrowserWindowProvider.GetFirstBrowserByZOrder(null, this.GuiType) == null)
                    return true;
                var activeTabIds = this.TryGetActiveTabIds();
                if (activeTabIds.Count() == 0)
                    return true;
                if (this.IgnoreUrl?.Count > 0)
                {
                    PredicateFilter<BrowserTabPage> tabFilter = new PredicateFilter<BrowserTabPage>();
                    tabFilter.AddLast((tabPage) => !this.IgnoreUrl.Any(e => tabPage.Url.Contains(e)));
                    return this.MatchTabPageByPriority(activeTabIds, tabFilter) == null;
                }
                return false;
            }, timeout > 10000 ? 10000 : timeout, 500, 500);
        }
        public virtual void WaitBrowserProcessExited(int timeout = 10000)
        {
            logger.LogDebug($"开始检测浏览器进程");
            RPAUtils.Retry(() =>
            {
                try
                {
                    var processAll = UiCommon.GetProcessesBySessionId(this.BrowserProcessName);
                    if (processAll.Length == 0)
                    {
                        return true;
                    }
                    string exePath = this.GetBrowserAppPath();
                    var processIds = processAll.Where(p => string.IsNullOrEmpty(exePath) || exePath.EqualIgnoreCase(p.GetExeFilePath()))
                                       .Select(p => p.Id)
                                       .ToArray();
                    processAll.Dispose();
                    if (processIds.Length == 0)
                    {
                        return true;
                    }
                    if (KillMainWindow())
                        Task.Delay(1000).Wait();
                    Utils.ProcessKiller.KillProcessByIds(processIds);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex);
                }
                return false;
            }, timeout, 500);
        }
        public bool IsNativeMessagingHostExited()
        {
            Process[] processArr = null;
            try
            {
                if (string.IsNullOrEmpty(this.NativeMessageHostProcessName))
                {
                    return true;
                }
                processArr = UiCommon.GetProcessesBySessionId(this.NativeMessageHostProcessName);
                foreach (var process in processArr)
                {
                    using (var parentProcess = ParentProcessUtilities.GetParentProcess(process, "cmd"))
                    {
                        var pProcName = parentProcess.ProcessName;
                        if ((pProcName.EqualIgnoreCase(this.BrowserProcessName) || BrowserProcessNames.Any(name => pProcName.EqualIgnoreCase(name)))
                            && this.GetBrowserAppPath().EqualIgnoreCase(parentProcess.GetExeFilePath()))
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                logger.LogDebug($"NativeHostMessaging进程是否退出，发生一次异常，因此无法判断进程是否退出。本次按未退出处理");
                logger.LogDebug(ex);
                return false;
            }
            finally
            {
                if (processArr != null)
                    processArr.Dispose();
            }
        }

        #endregion

        #region 浏览器运行状态
        public virtual bool IsRunBrowser
        {
            get
            {
                var processAll = UiCommon.GetProcessesBySessionId(this.BrowserProcessName);
                bool isRunning = processAll.Length > 0;
                processAll.Dispose();
                return isRunning;
            }
        }

        #endregion
        #region NMH
        public virtual Dictionary<long, long> GetWindowMap()
        {
            var crxVersion = this.WebDataProvider.GetConnCrxVersion();

            if (!string.IsNullOrEmpty(crxVersion)
                && Version.TryParse(crxVersion, out var vCurrent)
                && vCurrent >= new Version("4.0.1.204"))
            {
                var results = this.WebDataProvider.GetWindowMap();
                var wndMap = results.ToObject<Dictionary<long, long>>();
                //每次获取窗口映射关系时，刷新WindowHandleAndIdMap
                foreach (var item in wndMap)
                {
                    var hWnd = new IntPtr(item.Value);
                    if (!WindowHandleAndIdMap.ContainsKey(hWnd))
                    {
                        WindowHandleAndIdMap[hWnd] = (int)item.Key;
                    }
                }
                return wndMap;
            }
            return new Dictionary<long, long>();
        }

        #endregion
    }
}

