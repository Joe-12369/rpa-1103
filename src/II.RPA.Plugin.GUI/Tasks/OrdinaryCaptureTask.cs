using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA2;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Utils;
using II.RPA.WinAutomation.CommonUtility;
using II.RPA.WinAutomation.CommonUtility.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Interop;

namespace II.RPA.Plugin.GUI
{
    abstract class OrdinaryCaptureTask<T> : CaptureTask<T, IGuiNode> where T : class
    {
        private static ILogger logger = Logger.OfContext();
        protected static readonly object _objLock_GetPointAndWndInfo = new object();
        private static readonly int CurrentProcessId = Process.GetCurrentProcess().Id;
        private static readonly Lazy<AutomationBase> WindowClassUIA2Automation = new Lazy<AutomationBase>(
            () => new UIA2Automation(),
            LazyThreadSafetyMode.ExecutionAndPublication);
        private static string _lastLoggedUIA2WindowClassName;
        //private volatile bool isTaskEnable = true;
        //private MemoryCacheManager CaptureMemoryCacheManager = new MemoryCacheManager();
        internal new int Interval { get; set; } = 1;
        protected override string Name => "普通元素拾取任务";
        /// <summary>
        /// 当前拾取到的 元素的类型（普通元素类型）
        /// </summary>
        protected eGuiType CurrentGuiType { get; set; } = eGuiType.Windows;
        /// <summary>
        /// 查找相似元素需要的Selector
        /// </summary>
        internal string Selector { get; set; }
        internal string CaptureUnauthorizedAccessStr { get; set; } = string.Empty;
        internal Point CaptureScreenPoint { get; set; } = Point.Empty;
        internal AutomationElement CaptureRootWnd { get; set; } = null;
        internal AutomationElement CapturePointWnd { get; set; } = null;
        internal List<IntPtr> CaptureHWndList { get; set; } = null;
        private readonly Dictionary<IntPtr, AutomationElement> _automationElementFromHandleCache = new Dictionary<IntPtr, AutomationElement>();


        internal OrdinaryCaptureTask() : base()
        {
            this.Tick = CaptureTask_Tick;
            this.TickOnceCallback = CaptureTask_TickOnceCallback;
            this.CaptureTaskThreadException = CaptureTask_CaptureTaskThreadException;
            //CaptureMemoryCacheManager.init();
        }

        private AutomationElement FromHandleCached(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
            {
                return null;
            }

            if (_automationElementFromHandleCache.TryGetValue(hWnd, out var element) && element != null)
            {
                return element;
            }

            AutomationBase captureAutomation;
            if (TryGetUIA2WindowClass(hWnd, out string matchedClassName))
            {
                // 命中的窗口禁止调用 UIA3。UIA2 初始化或访问失败时由外层
                // catch 忽略本次拾取，不能回退到可能终止 RuntimeHost 的 UIA3。
                captureAutomation = WindowClassUIA2Automation.Value;
                if (!string.Equals(_lastLoggedUIA2WindowClassName, matchedClassName, StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogInformation($"窗口类名命中 UIA2WindowClasses，本次拾取使用 UIA2。className={matchedClassName}");
                    _lastLoggedUIA2WindowClassName = matchedClassName;
                }
            }
            else
            {
                captureAutomation = UiCommon.UIAAutomation;
            }


            element = captureAutomation.FromHandle(hWnd);
            if (element != null)
            {
                _automationElementFromHandleCache[hWnd] = element;
            }
            return element;
        }

        internal override void Start()
        {
            try
            {
                if (this.IsStart)
                {
                    return;
                }
#if !DEBUG
                this.StartGuiTypeRefresh();
#endif
                base.Start();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        internal override void Abort()
        {
            try
            {
                logger.LogDebug($"SelectorIssueTrace -- Abort() is called. stacktrace: {new StackTrace()}");
                //this.Tick -= CaptureTask_Tick;
                //this.TickOnceCallback -= CaptureTask_TickOnceCallback;
                base.Abort();
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, "元素拾取线程在终止的时候发生了一个错误，已做拦截");
            }
        }



        /// <summary>
        /// 回到UI线程。主要为拾取过程需要回归到UI线程 提供一种方式 
        /// </summary>
        /// <param name="action"></param>
        internal abstract void BackToUIThread(Action action);

        /// <summary>
        /// 每次拾取元素后的回调
        /// </summary>
        /// <param name="guiNode"></param>
        internal abstract void CaptureTask_TickOnceCallback(IGuiNode guiNode);

        /// <summary>
        /// 拾取任务异常时 调用
        /// </summary>
        /// <param name="ex"></param>
        internal abstract void CaptureTask_CaptureTaskThreadException(Exception ex);

        /// <summary>
        /// 在拾取过程中，GUI类型变化时 执行，虚方法 无具体动作
        /// </summary>
        /// <param name="guiType"></param>
        internal virtual void CaptureTask_GuiTypeChanged(eGuiType guiType)
        {
        }

        /// <summary>
        /// 拾取窗口前动作（每次）
        /// </summary>
        /// <returns>返回false 终止拾取</returns>
        internal virtual bool CaptureTask_GUICapturpWindowBefore()
        {
            //logger.LogDebug("CaptureTask_GUICapturpWindowBefore ordinary");
            return true;
        }

        /// <summary>
        /// 拾取窗口后动作（每次）
        /// </summary>
        /// <returns>返回false 终止拾取</returns>
        internal virtual bool CaptureTask_GUICapturpWindowAfter()
        {
            return true;
        }

        internal void StartGuiTypeRefresh()
        {
            Task.Run(async () =>
            {
                await Task.Delay(5000);
                while (this.IsStart)
                {
                    if ((CaptureScreenPoint != Point.Empty && CaptureRootWnd != null && CapturePointWnd != null) && GuiOperatorProvider.Instance.TryGetSupportedOperator(this.CaptureScreenPoint, this.CaptureRootWnd, this.CapturePointWnd, out var guiOperator))
                    {
                        try
                        {
                            logger.LogInformation($"Ordinary StartGuiTypeRefresh");

                            var guiType = guiOperator.GuiType;

                            if (guiOperator.GuiType != eGuiType.SAP)//SAP内嵌Windows不缓存
                                GuiManager.Info.CacheWndGuiType(this.CaptureHWndList.Last(), guiOperator);
                        }
                        catch (Exception ex)
                        {
                            logger.LogDebug(ex);
                            logger.LogDebug($"判断{guiOperator.GuiType} GUI 类型失败，将继续尝试使用其它GUI类型进行拾取");
                            GuiManager.Info.ClearWndGuiType(this.CaptureHWndList.Last());
                        }

                    }
                    await Task.Delay(5000);
                }
            });
        }

        internal void CaptureTask_Tick(object sender, EventArgs e)
        {
            logger.LogDebug("===============Start Ordinary CaptureTask_Tick==============");
            //System.Collections.Generic.List<JObject> IsCanUseLog = new System.Collections.Generic.List<JObject>();
            if (!IsTaskEnable)
            {
                Task.Delay(200).Wait();
                logger.LogDebug($"===============Ordinary CaptureTask_Tick isTaskEnable:{IsTaskEnable} 取消");
                return;
            }
            try
            {
                System.Drawing.Point screenPoint = System.Drawing.Point.Empty;
                AutomationElement rootElement = null;
                AutomationElement pointElement = null;
                bool inspectResult = false;
                IGuiNode inspectNode = null;
                if (!CaptureTask_GUICapturpWindowBefore() || !IsTaskEnable)
                {
                    Task.Delay(200).Wait();
                    logger.LogDebug($"===============Ordinary CaptureTask_Tick CaptureTask_GUICapturpWindowBefore 取消");
                    return;
                }
                this.ThrowIfCaptureTaskStop();
                this.GetPointAndWndInfo(out screenPoint, out rootElement, out pointElement, out List<IntPtr> hWndList);
                this.ThrowIfCaptureTaskStop();

                if (rootElement == null || pointElement == null)
                {
                    logger.LogDebug($"===============Ordinary CaptureTask_Tick rootElement pointElement 为空 取消");
                    return;
                }

                if (rootElement == null)
                {
                    logger.LogDebug($"SelectorIssueTrace -- rootElement is null");
                    TickOnceResult = null;
                    return;
                }
                this.ThrowIfCaptureTaskStop();
                if (!CaptureTask_GUICapturpWindowAfter() || !IsTaskEnable)
                {
                    logger.LogDebug($"===============Ordinary CaptureTask_Tick CaptureTask_GUICapturpWindowAfter 取消");
                    return;
                }
                this.ThrowIfCaptureTaskStop();
                IntPtr hWndFromPoint = hWndList.Last();
                var guiOperator = this.GetGuiTypeOld(screenPoint, rootElement, pointElement, hWndFromPoint);
                if (guiOperator == null)
                {
                    logger.LogDebug($"===============Ordinary CaptureTask_Tick guiType为空 取消");
                    return;
                }
                this.ThrowIfCaptureTaskStop();
                int CurrentElementCapturedProcessID = rootElement?.Properties?.ProcessId;

                TimeTrackHelper timeTrackHelper = new TimeTrackHelper();
                try
                {
                    //logger.LogDebug($"开始使用插件【{guiOperator.GuiType}】中的拾取方法");
                    //设置拾取信息窗口中的 窗口类型
                    CaptureTask_GuiTypeChanged(guiOperator.GuiType);
                    this.ThrowIfCaptureTaskStop();
                    timeTrackHelper.Start("InspectNode", TimeTrackLogLevel.info);
                    if (guiOperator.GuiType == eGuiType.SAP)
                    {
                        this.BackToUIThread(() =>
                        {
                            inspectResult = guiOperator.InspectNode(screenPoint, rootElement, pointElement, this.Selector, out inspectNode);
                        });
                    }
                    else
                    {
                        inspectResult = guiOperator.InspectNode(screenPoint, rootElement, pointElement, this.Selector, out inspectNode);
                    }
                    this.ThrowIfCaptureTaskStop();
                    //针对web component 特殊场景补充拾取 元素组件tag 为*-app 例如: wijie-app、magic-app
                    if (inspectResult && inspectNode?.UserDefinedID?.ToLower().Contains("-app") == true)
                    {
                        var winOprator = GuiManager.WindowsOperator;
                        inspectResult = winOprator.InspectNode(screenPoint, rootElement, pointElement, "", out inspectNode);
                    }
                    timeTrackHelper.Pick($"InspectNode 结果:{inspectResult}", TimeTrackLogLevel.info);
                    this.ThrowIfCaptureTaskStop();
                    if (!inspectResult)
                    {
                        if (guiOperator.GuiType != eGuiType.Windows)
                        {
                            GuiManager.Info.ClearWndGuiType(hWndFromPoint);
                        }
                        else
                        {
                            logger.LogDebug("Windows 拾取失败时保留 GUI 类型缓存，避免 hover 期间反复重新判型");
                        }
                    }
                    this.CaptureUnauthorizedAccessStr = string.Empty;
                }
                catch (RPAException ex)
                {
                    if (ex.ErrorMessageType == eErrorMessageType.Error_CaptureUnauthorizedAccessError)
                    {
                        this.CaptureUnauthorizedAccessStr = ex.ErrorMessageContent;
                    }
                    else
                    {
                        throw ex;
                    }
                }
                catch (ThreadAbortException ex)
                {
                    //强制终止线程  不处理
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"循环判断拾取程序曾发生了异常：{guiOperator.GuiType}");
                }
                finally
                {
                    timeTrackHelper.Stop("InspectNode", TimeTrackLogLevel.info);
                }
                if (!this.IsStart)
                {
                    logger.LogDebug($"SelectorIssueTrace --IsStart is {IsStart}");
                    return;
                }

                if (!IsTaskEnable)
                {
                    logger.LogDebug($"SelectorIssueTrace --isTaskEnable is {IsTaskEnable}");
                    return;
                }

                this.ThrowIfCaptureTaskStop();
                if (inspectResult)
                {
                    ((IGuiNode_6_6_0)inspectNode).ProcessID = CurrentElementCapturedProcessID;
                    TickOnceResult = (IGuiNode_6_6_0)inspectNode;
                    //CaptureMemoryCacheManager.AddElementToCache(hWndList.Last(), new CapturedElement(screenPoint, (IGuiNode_6_6_0)inspectNode));
                    //logger.LogDebug("查找元素 成功");
                }
                else
                {
                    IGuiNode_6_6_0 CGuiNode = (IGuiNode_6_6_0)(GuiManager.CreateWindowsGuiNode(pointElement));
                    CGuiNode.ProcessID = CurrentElementCapturedProcessID;
                    //实例化一个新的Windows UIANode 作为默认的元素，尽可能少的使用
                    TickOnceResult = CGuiNode;
                    //CaptureMemoryCacheManager.AddElementToCache(hWndList.Last(), new CapturedElement(screenPoint, CGuiNode));
                    logger.LogDebug("查找元素 失败 赋值windows元素");
                }

            }
            catch (COMException ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, "拾取程序d发生底层程序异常");
            }
            catch (Exception ex)
            {
                CaptureTickExceptionLog.Log(logger, ex, "拾取程序发生了异常");

                if (ex is RPAException)
                {
                    //只抛出插件相关的错误
                    RPAException rpaex = (RPAException)ex;
                    //GuiElement.overlayForm.CurrentCaptureMode != Params.CaptureModeType.Intelligence  &&

                    if (rpaex.ErrorMessageType == eErrorMessageType.Error_ChromeExtensionUnInstalled
                          || rpaex.ErrorMessageType == eErrorMessageType.Error_ChromeExtensionUnopen
                          || rpaex.ErrorMessageType == eErrorMessageType.Error_360ExtensionUnInstalled
                          || rpaex.ErrorMessageType == eErrorMessageType.Error_360ExtensionUnopen
                          || rpaex.ErrorMessageType == eErrorMessageType.Error_EdgeExtensionUnInstalled
                          || rpaex.ErrorMessageType == eErrorMessageType.Error_EdgeExtensionUnopen
                          || rpaex.ErrorMessageType == eErrorMessageType.Error_FireFoxExtensionUnInstalled
                          || rpaex.ErrorMessageType == eErrorMessageType.Error_FireFoxExtensionUnopen
                          || rpaex.ErrorMessageType == eErrorMessageType.Error_SogouExtensionUnInstalled
                          || rpaex.ErrorMessageType == eErrorMessageType.Error_SogouExtensionUnopen
                          || rpaex.ErrorMessageType == eErrorMessageType.Error_JavaExtensionUnInstalled
                          || rpaex.ErrorMessageType == eErrorMessageType.Error_ServerHubUnopen
                          || rpaex.ErrorMessageType == eErrorMessageType.Error_360XExtensionUnInstalled
                          || rpaex.ErrorMessageType == eErrorMessageType.Error_360XExtensionUnopen
                          || rpaex.ErrorMessageType == eErrorMessageType.Error_BrowserPluginVersionTooLow)
                    {
                        throw ex;
                    }
                }
            }
            //logger.LogDebug($"SelectorIssueTrace -- {JsonHelper.JsonByNewtonJson(IsCanUseLog)}");
            //logger.LogDebug("===============End Ordinary CaptureTask_Tick ");
        }


        private object lockGuiType = new object();

        protected IGuiOperator GetGuiTypeOld(Point screenPoint, AutomationElement rootElement, AutomationElement pointElement, IntPtr hWndFromPoint)
        {
            TimeTrackHelper timeTrackHelper = new TimeTrackHelper();
            timeTrackHelper.Start("GetGuiType函数耗时检测", TimeTrackLogLevel.info);
            try
            {
                var guiOperator = GuiManager.Info.GetGuiType(hWndFromPoint);
                if (guiOperator == null)
                {
                    try
                    {
                        if (GuiOperatorProvider.Instance.TryGetSupportedOperator(this.CaptureScreenPoint, this.CaptureRootWnd, this.CapturePointWnd, out guiOperator))
                        {
                            if (guiOperator.GuiType != eGuiType.SAP)//SAP内嵌Windows不缓存
                                GuiManager.Info.CacheWndGuiType(hWndFromPoint, guiOperator);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogDebug($"判断 GUI 类型失败");
                        TickOnceResult = GuiManager.CreateWindowsGuiNode(pointElement);
                        throw ex;
                    }

                }
                return guiOperator;
            }
            finally
            {
                timeTrackHelper.Pick("GetGuiType函数耗时检测", TimeTrackLogLevel.info);
                timeTrackHelper.Stop("GetGuiType函数耗时检测", TimeTrackLogLevel.info);
                //logger.LogDebug($"当前GuiType {guiType}");
            }
        }

        internal override void StopAfterExecute()
        {
            try
            {
                base.StopAfterExecute();
            }
            catch (Exception)
            {
                logger.LogError("停止拾取普通拾取异常StopAfterExecute");
            }
        }
        internal override void ReStartExecute()
        {
            try
            {
                base.ReStartExecute();
            }
            catch (Exception)
            {
                logger.LogError("停止拾取普通拾取异常StopAfterExecute");
            }
        }
        internal virtual void GetPointAndWndInfo(out Point screenPoint, out AutomationElement rootElement, out AutomationElement pointElement, out List<IntPtr> hWndList)
        {
            lock (_objLock_GetPointAndWndInfo)
            {

                rootElement = null;
                pointElement = null;
                hWndList = new List<IntPtr>();
                try
                {
                    screenPoint = System.Windows.Forms.Cursor.Position;
                    //得到自根窗口开始到点窗口（通过点直接获取到）的句柄集合
                    hWndList = UiCommon.GetRootWindowList(screenPoint);

                    if (!IsTaskEnable)
                    {
                        logger.LogDebug($"GetPointAndWndInfo pause");
                        return;
                    }
                    if (hWndList.Count > 0)
                    {
                        foreach (IntPtr hWnd in hWndList)
                        {
                            if (hWnd != IntPtr.Zero)
                            {
                                UiCommon.GetWindowThreadProcessId(hWnd, out var processId1);
                                if (processId1 != GlobleConfigurations.CurrentProcessId)
                                {
                                    rootElement = FromHandleCached(hWnd);   //获取根窗体
                                }
                            }
                            if (rootElement == null)
                            {
                                //this.CaptureRootWnd = null;
                                break;
                            }
                            if (rootElement.Properties.BoundingRectangle.IsSupported && rootElement.BoundingRectangle.Contains(screenPoint))
                            {
                                this.CaptureRootWnd = rootElement;
                                break;
                            }
                        }
                        pointElement = FromHandleCached(hWndList.Last());
                        this.CapturePointWnd = pointElement;
                        this.CaptureScreenPoint = screenPoint;
                        this.CaptureHWndList = hWndList;
                    }
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, $"获取屏幕元素时发生一次异常，将忽略本次异常继续拾取");
                    screenPoint = Point.Empty;
                }
            }
        }

        private static bool TryGetUIA2WindowClass(IntPtr hWnd, out string matchedClassName)
        {
            matchedClassName = null;
            List<string> configuredPatterns = Configurations.Info?.UIA2WindowClasses;
            if (hWnd == null || hWnd == IntPtr.Zero || configuredPatterns == null || configuredPatterns.Count == 0)
            {
                return false;
            }

            string className = UiCommon.GetClassName(hWnd);
            foreach (string configuredPattern in configuredPatterns)
            {
                if (IsUIA2WindowClassMatch(className, configuredPattern))
                {
                    matchedClassName = className;
                    return true;
                }
            }

            return false;
        }

        private static bool IsUIA2WindowClassMatch(string className, string configuredPattern)
        {
            if (string.IsNullOrWhiteSpace(className) || string.IsNullOrWhiteSpace(configuredPattern))
            {
                return false;
            }

            string pattern = configuredPattern.Trim();
            if (pattern.EndsWith("*", StringComparison.Ordinal))
            {
                string prefix = pattern.Substring(0, pattern.Length - 1);
                return prefix.Length > 0
                    && className.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
            }

            return string.Equals(className, pattern, StringComparison.OrdinalIgnoreCase);
        }
    }
}

