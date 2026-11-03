using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.Core.Overlay;
using FlaUI.Core.WindowsAPI;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.Core.Entities;
using II.RPA.Plugin.GUI.AiRepair;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.GuiBase;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.Plugin.GUI.Commons.WindowMatch;
using II.RPA.Plugin.GUI.ElementHighlight;
using II.RPA.Plugin.GUI.Params;
using II.RPA.Plugin.GUI.ScreenShot;
using II.RPA.Plugin.GUI.Utils;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.Params;
using II.RPA.WinAutomation.CommonUtility;
using II.RPA.WinAutomation.CommonUtility.Utils;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using VirtualKeyShort = FlaUI.Core.WindowsAPI.VirtualKeyShort;

namespace II.RPA.Plugin.GUI
{
    public class GuiElement
    {
        public delegate string GuiElementSelectedEventHandler(GuiElement element);
        public delegate void GuiElementCancelededEventHandler();
        public delegate void GuiElementSelectoredEventHandler(string selector);

        public static GuiElementSelectedEventHandler OnSelected;
        public static GuiElementCancelededEventHandler OnCanceled;
        public static GuiElementSelectoredEventHandler OnSelectored;

        /// <summary>
        /// web类型selectortype  ,"web"数据采集v2 使用
        /// </summary>
        public static string[] selectorType_Web = new string[] { "web", "chrome", "ie", "firefox", "zexplorer", "web360se", "sogou", "edge", "web360x", "superbrowser", "chromium" };
        public static string[] selectorType_Win = new string[] { "windows", "wdterp", "java" };
        internal static string selectorType_supportSiblings = ",chrome,edge,firefox,web360se,web360x,superbrowser,zexplorer,chromium,sap,";
        public static bool occupyScreen = true;
        internal IGuiNode uiNode;
        public Point RelativeClickPos { get; set; }
        private static GuiElement cacheDesktop;
        private GuiElement cachedParent;
        private GuiElement automationElementParent;
        private GuiElement cachedDirectTopLevelWindow;
        private Lazy<Rectangle> boundingRectangle;
        private static ILogger logger = Logger.OfContext();

        //元素拾取遮罩层
        public static OverlayForm overlayForm;
        private static System.Windows.Threading.Dispatcher overlayDispatcher;
        internal static System.Windows.Threading.Dispatcher OverlayDispatcher =>
            overlayDispatcher ?? throw new InvalidOperationException("Capture overlay UI thread has not been initialized.");

        internal static void InitializeOverlayDispatcher()
        {
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
                throw new InvalidOperationException("Capture overlay requires an STA thread.");
            var dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
            var existing = Interlocked.CompareExchange(ref overlayDispatcher, dispatcher, null);
            if (existing != null && existing != dispatcher)
                throw new InvalidOperationException("Capture overlay UI thread is already initialized.");
            logger.LogDebug($"Capture overlay Dispatcher thread ID:{dispatcher.Thread.ManagedThreadId}");
        }

        internal static OverlayForm PrepareOverlayForm()
        {
            if (!OverlayDispatcher.CheckAccess())
                throw new InvalidOperationException("Capture overlay must be prepared on its UI thread.");

            if (overlayForm == null || overlayForm.IsDisposed || overlayForm.Disposing || overlayForm.CaptureDialogFailed)
            {
                logger.LogWarning("Recreating unavailable capture overlay");
                overlayForm?.CleanupCapture();
                if (overlayForm != null)
                {
                    overlayForm.DisposeEnvironment();
                    overlayForm.Dispose();
                }
                overlayForm = new OverlayForm();
            }
            _ = overlayForm.Handle;
            return overlayForm;
        }
        private static CursorPositionWnd _positionForm = null;
        internal Bitmap currentInformativeScreenshot;
        private Bitmap currentDesktopScreenshot;
        internal string screenShotPath;
        /// <summary>
        /// 定位父元素
        /// </summary>
        public static Action OnSelectedParentElement;
        /// <summary>
        /// 定位子元素
        /// </summary>
        public static Action OnSelectedChildElement;
        /// <summary>
        /// 取消定位元素
        /// </summary>
        public static Action CancelAgentCaptureElement;
        /// <summary>
        /// 确认定位的元素
        /// </summary>
        public static Action ConfirmAgentCaptureElement;

        static GuiElement()
        {
            try
            {
                //TimeTrackHelper timeTrackHelper = new TimeTrackHelper();
                //timeTrackHelper.Start("GuiElementInit");

                //UIAExcuteTask.MoveInspectInfoWndRect += MoveFromLocation;
                UiCommon.UIAAutomation.Equals(null);

                //timeTrackHelper.Pick("GuiElementInit");
                //timeTrackHelper.Stop("GuiElementInit");
            }
            catch (Exception)
            {
                logger.LogWarning("通过调用的方式初始化UIACore失败");
            }
            NativeMethods.SetProcessDPIAware();
        }

        /// <summary>
        /// 初始化，由于JAVA的accessBridge.Initialize()生效有延迟，所以提前使其生效
        /// </summary>
        //public static void Init()
        //{
        //    JavaNode.EnumJvms(true);
        //}
        internal GuiElement(IGuiNode node, GuiElement parent = null)
        {
            this.uiNode = node;
            this.Parent = parent;
            this.boundingRectangle = new Lazy<Rectangle>(() => node.BoundingRectangle);
        }
        internal static bool IsWebSelectorType(string type) => GuiElement.selectorType_Web.Contains(type, StringComparer.OrdinalIgnoreCase);
        internal static bool IsSupportSimilarSelectorType(string selectortype)
        {
            return !"chrome,edge,firefox,web360se,ie,sogou,windows,web360x,superbrowser,zexplorer,chromium,wdterp,java,sap,".Contains(selectortype.ToLower());
        }
        public static CursorPositionWnd PositionForm
        {
            get
            {
                if (_positionForm == null)
                {
                    _positionForm = new CursorPositionWnd();
                }
                return _positionForm;
            }
            set { _positionForm = value; }
        }
        public GuiElement Parent
        {
            get
            {
                if (cachedParent == null)
                {
                    if (uiNode.Parent != null)
                    {
                        cachedParent = new GuiElement(uiNode.Parent);
                    }
                }
                return cachedParent;
            }

            private set
            {
                cachedParent = value;
            }
        }
        public GuiElement AutomationElementParent
        {
            get
            {
                automationElementParent = new GuiElement(uiNode.AutomationElementParent);
                return automationElementParent;
            }
        }
        public static GuiElement Desktop
        {
            get
            {
                /**************/
                //if (cacheDesktop == null)
                //{
                //    var _rootElement = UiCommon.UIAAutomation.GetDesktop();
                //    cacheDesktop = new GuiElement(new UIANode(_rootElement));
                //}

                return cacheDesktop;
            }
        }
        public ElementMatchType MatchSourceType { get; set; } = ElementMatchType.Normal;
        public List<GuiElement> Children
        {
            get
            {
                var list = new List<GuiElement>();
                var children = uiNode.Children;
                foreach (var item in children)
                {
                    list.Add(new GuiElement(item, this));
                }

                return list;
            }
        }
        public string ControlType
        {
            get
            {
                if (string.IsNullOrEmpty(uiNode.ControlType))
                {
                    return "Node";
                }
                else
                {
                    return uiNode.ControlType;
                }

            }
        }
        public string Name
        {
            get
            {
                return uiNode.Name;
            }
        }
        public string AutomationId
        {
            get
            {
                return uiNode.AutomationId;
            }
        }

        public string UserDefinedID
        {
            get
            {
                return uiNode.UserDefinedID;
            }
        }

        public string ClassName
        {
            get
            {
                return uiNode.ClassName;
            }
        }
        public string ProcessName
        {
            get
            {
                return uiNode.ProcessName;
            }
        }

        public string ProcessFullPath
        {
            get
            {
                return uiNode.ProcessFullPath;
            }
        }
        public string Role
        {
            get
            {
                return uiNode.Role;
            }
        }
        public string Description
        {
            get
            {
                return uiNode.Description;
            }
        }
        public string Idx
        {
            get
            {
                return uiNode.Idx;
            }
        }
        public string Id
        {
            get
            {
                return uiNode.AutomationId;
            }
        }

        public bool IsTopLevelWindow
        {
            get
            {
                return uiNode.IsTopLevelWindow;
            }
        }
        /// <summary>
        /// 图像元素解析地址
        /// </summary>
        public string CVServer { get; set; }
        /// <summary>
        /// CV操作（执行）前解析服务地址(元素拾取地址)
        /// </summary>
        public string CVExecutionServiceURL { get; set; }

        internal static Bitmap CaptureDesktop()
        {
            return UiCommon.UIAAutomation.GetDesktop().Capture();
        }
        internal static Bitmap CaptureDesktopByRectangle(Rectangle r)
        {
            Bitmap bitmap = null;
            try
            {
                var img = UiCommon.UIAAutomation.GetDesktop().Capture();
                bitmap = new Bitmap(r.Width, r.Height);

                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.DrawImage(img, new Rectangle(0, 0, bitmap.Width, bitmap.Height), r, GraphicsUnit.Pixel);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
            return bitmap;
        }
        /// <summary>
        /// 当前元素对应的Handle句柄
        /// </summary>
        public IntPtr WindowHandle
        {
            get
            {
                return uiNode.WindowHandle;
            }
        }

        public string Selector
        {
            get
            {
                return this.GetSelector(false);
            }
        }
        public string PinSelector
        {
            get
            {
                if (uiNode is IGuiWebNode)
                {
                    return (uiNode as IGuiWebNode_690).PinSelector;
                }
                return string.Empty;
            }
        }
        public string GetColletionColumnSelector()
        {
            if (uiNode is IGuiWebNode_690)
            {
                return (uiNode as IGuiWebNode_690).GetColletionColumnSelector();
            }
            return string.Empty;
        }
        public string GetSelector(bool innerTextFirst)
        {
            eGuiType guiType = eGuiType.None;
            if (uiNode is IGuiNode_6_1_0)
                guiType = ((IGuiNode_6_1_0)uiNode).GuiType;
            if (guiType == eGuiType.IE || guiType == eGuiType.SAP)
            {
                string ret = "";
                try
                {
                    if (DataCollectingManager.dataCollectOverlayWnd == null)
                    {
                        GuiElement.overlayForm.Invoke(() =>
                        {
                            ret = ((IGuiNode_6_6_0)uiNode).GetSelector(innerTextFirst).ToString();
                        });
                    }
                    //数据采集模式
                    else
                    {
                        DataCollectingManager.dataCollectOverlayWnd.Dispatcher.Invoke(() =>
                        {
                            ret = ((IGuiNode_6_6_0)uiNode).GetSelector(innerTextFirst).ToString();
                        });
                    }
                }
                catch (RPAException rpaEx)
                {
                    if (rpaEx.ErrorMessageType == eErrorMessageType.Error_ElementLevel_Error
                        || rpaEx.ErrorMessageType == eErrorMessageType.Error_ElementNonsupport_Error
                        || rpaEx.ErrorMessageType == eErrorMessageType.Error_ElementNode_Error)
                    {
                        throw rpaEx;
                    }
                }
                catch
                {

                }
                return ret;
            }
            else
            {
                return ((IGuiNode_6_6_0)uiNode).GetSelector(innerTextFirst).ToString();
            }
        }
        public string PureSelector
        {
            get
            {
                eGuiType guiType = eGuiType.None;
                if (uiNode is IGuiNode_6_1_0)
                    guiType = ((IGuiNode_6_1_0)uiNode).GuiType;
                if (guiType == eGuiType.IE || uiNode is IGuiSAPNode)
                {
                    string ret = "";
                    //ExecutionContext.Run(ElementCaptureTask.Instance.ExecutionContext, delegate
                    //{
                    //    ret = uiNode.NodeSelector.ToString();
                    //}, null);
                    ret = uiNode.NodeSelector.ToString();
                    return ret;
                }
                else
                {
                    return uiNode.NodeSelector.ToString();
                }
            }
        }
        public string TableSelector
        {
            get
            {
                eGuiType guiType = eGuiType.None;
                if (uiNode is IGuiNode_6_1_0)
                    guiType = ((IGuiNode_6_1_0)uiNode).GuiType;
                if (guiType == eGuiType.IE || guiType == eGuiType.SAP)
                {
                    string ret = "";
                    //ExecutionContext.Run(ElementCaptureTask.Instance.ExecutionContext, delegate
                    //{
                    //    ret = uiNode.NodeSelector.ToString();
                    //}, null);
                    if (DataCollectingManager.dataCollectOverlayWnd == null)
                    {
                        GuiElement.overlayForm.Invoke(() =>
                        {
                            ret = ((IGuiNode_6_1_0)uiNode).TableNodeSelector.ToString();
                        });
                    }
                    //数据采集模式
                    else
                    {
                        DataCollectingManager.dataCollectOverlayWnd.Dispatcher.Invoke(() =>
                        {
                            ret = ((IGuiNode_6_1_0)uiNode).TableNodeSelector.ToString();
                        });
                    }
                    return ret;
                }
                else
                {
                    return ((IGuiNode_6_1_0)uiNode).TableNodeSelector.ToString();
                }
            }
        }
        public bool IsWebType()
        {
            eGuiType guiType = eGuiType.None;
            if (uiNode is IGuiNode_6_1_0)
                guiType = ((IGuiNode_6_1_0)uiNode).GuiType;
            return GuiElement.selectorType_Web.Contains(guiType.ToString(), StringComparer.OrdinalIgnoreCase);
        }
        public bool IsGuiType(string selectorType)
        {
            eGuiType guiType = eGuiType.None;
            if (uiNode is IGuiNode_6_1_0)
                guiType = ((IGuiNode_6_1_0)uiNode).GuiType;
            return guiType.ToString().EqualIgnoreCase(selectorType);
        }
        public Rectangle BoundingRectangle
        {
            get
            {
                return this.boundingRectangle.Value;
            }
            set
            {
                this.boundingRectangle = new Lazy<Rectangle>(() => value);
            }
        }

        internal static void ReadyToCapture()
        {
            TimeTrackHelper timeTrackHelper = new TimeTrackHelper();
            timeTrackHelper.Start("ReadyToCapture", TimeTrackLogLevel.info);
            //GlobleConfigurations.ElementAPPDic.Clear();//清除缓存的句柄和进程之间的映射关系
            GuiManager.Info.ClearWndGuiType();//清除已缓存的 window guiType映射关系
            var ReadyToCaptureTasks = new List<Task>();
            CancellationTokenSource ReadyToCaptureCts = new CancellationTokenSource();
            CancellationToken ReadyToCaptureToken = ReadyToCaptureCts.Token;

            foreach (IGuiOperator guiOperator in GuiOperatorProvider.Instance.GuiOperatorEnumerable)
            {
                ReadyToCaptureTasks.Add(

                   Task.Run(() =>
                   {
                       if (ReadyToCaptureToken.IsCancellationRequested) return;
                       TimeTrackHelper timeTrackHelperVar = new TimeTrackHelper();
                       try
                       {
                           timeTrackHelperVar.Start($"ReadyToCapture (1) {guiOperator.GuiType.ToString()}", TimeTrackLogLevel.degbug);
                           guiOperator.ReadyToCapture();

                       }
                       catch (Exception ex)
                       {
                           logger.LogError($"ReadyToCapture Err:{ex.Message}");
                       }
                       finally
                       {
                           timeTrackHelperVar.Pick($"ReadyToCapture (2) {guiOperator.GuiType.ToString()}", TimeTrackLogLevel.degbug);
                           timeTrackHelperVar.Stop($"ReadyToCapture (1) {guiOperator.GuiType.ToString()}", TimeTrackLogLevel.degbug);
                       }
                   }, ReadyToCaptureToken));
            }

            if (!ReadyToCaptureToken.IsCancellationRequested)
            {
                Task.WaitAll(ReadyToCaptureTasks.ToArray());
            }

            timeTrackHelper.Pick("ReadyToCapture", TimeTrackLogLevel.info);
            timeTrackHelper.Stop("ReadyToCapture", TimeTrackLogLevel.info);
        }
        public static GuiElement FromElementId(string selectorType, GetElementByIdParams @params)
        {
            IGuiOperator_6_9_0 guiOperator = (IGuiOperator_6_9_0)GuiOperatorProvider.Instance.GetGuiOperator(selectorType, @params.WindowInfo);
            var guiNode = guiOperator.GetElementById(@params);
            return guiNode == null ? null : new GuiElement(guiNode);
        }
        #region 7.0.0 版本多元素匹配逻辑
        public static List<GuiElement> QuerySelectorAll(GetElementFromSelectorParams param)
        {
            List<GuiElement> elements = new List<GuiElement>();
            IGuiNode[] nodes = null;
            //var matchType = ElementMatchType.Normal;
            var byIndex = ExtractViewData.SelectIsFindbyIndex(param.Selector, out CommonProps commonProps);
            var isSmartControl = commonProps.isSmartControl.As<bool>();
            bool isSimilarElementItem = commonProps.ExtendedTags.HasFlag(ExtendedTags.SimilarElementItem);
            bool enableAiEnhanced = !isSimilarElementItem && (isSmartControl || GlobleConfigurations.EnableAiEnhancedScreenshot);
            param.DiscernHidden = commonProps.DiscernHidden.As<bool>();
            IGuiOperator_6_9_0 guiOperator = (IGuiOperator_6_9_0)GuiOperatorProvider.Instance.GetGuiOperator(param.SelectorType, param.WindowInfo);

            bool skipStandard = "aiEnhanced".Equals(param.VerifyMode, StringComparison.OrdinalIgnoreCase);
            bool skipAiRepair = "selector".Equals(param.VerifyMode, StringComparison.OrdinalIgnoreCase);

            // 标准元素结构查找（aiEnhanced 模式跳过）
            if (!skipStandard)
            {
                try
                {
                    nodes = guiOperator.QuerySelectorAll(param);
                }
                catch (RPAException rpaex)
                {
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
                        throw rpaex;
                    }
                    else if (rpaex.IsRPAException(eErrorMessageType.Error_EmbeddedIEMode, out _))
                    {
                        logger.LogDebug($"内嵌IE模式 将按照IE模式重新执行");
                        param.SelectorType = eGuiType.IE.ToString();
                        param.ActionIEModeExcetpion(rpaex);
                        return QuerySelectorAll(param);
                    }
                    else if (!rpaex.IsRPAException(eErrorMessageType.Error_NotFoundElement, out _))
                    {
                        throw rpaex;
                    }
                }
            }
            //按照元素位置匹配
            if ((nodes == null || nodes.Length == 0) && byIndex && !skipStandard)
            {
                nodes = QuerySelectorAll_ByIndex(param, guiOperator);
            }
            //智能修复：selector 模式跳过，aiEnhanced 模式直接走，默认（空）为补位逻辑
            bool shouldAiRepair = (nodes == null || nodes.Length == 0) && enableAiEnhanced
                && (skipStandard || param.isActive);
            if (shouldAiRepair && !skipAiRepair)
            {
                var isTargetWin = (guiOperator as IGuiOperator_6_9_0).VerifyWindowOfNode(param);
                if (isTargetWin)
                {
                    nodes = QuerySelectorAll_AiRepair(param, commonProps);
                    param.MatchType = ElementMatchType.AiRepair;
                }
            }
            if (nodes != null)
                nodes.ForEach(node => { elements.Add(new GuiElement(node)); });
            return elements;
        }

        private static IGuiNode[] QuerySelectorAll_AiRepair(GetElementFromSelectorParams param, CommonProps commonProps)
        {
            if (!commonProps.GUIType.Equals(eGuiType.CV.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                var rumModel = "validation";//此处应该都是校验元素查找才会走到吧
                string originalElementId = JObject.Parse(param.Selector)
                    ?.GetValue("elementDetail")
                    ?.Value<string>("elementId") ?? "";
                var node = AiRepairUtils.ExecAiRepair_V2(commonProps, rumModel, originalElementId);
                if (node != null)
                {
                    //matchType = ElementMatchType.AiRepair;
                    return new[] { node.uiNode };
                }
            }
            return null;
        }

        private static IGuiNode[] QuerySelectorAll_ByIndex(GetElementFromSelectorParams param, IGuiOperator_6_9_0 guiOperator)
        {
            try
            {
                IGuiNode[] nodes;
                var selectorByIndex = ExtractPropsToIndex(param.Selector);
                var commonSelector = ExtractViewData.AddCommonProps(selectorByIndex, param.SelectorType);
                param.Selector = commonSelector;
                nodes = guiOperator.QuerySelectorAll(param);
                return nodes;
            }
            catch (RPAException rpaex)
            {
                if (!rpaex.IsRPAException(eErrorMessageType.Error_NotFoundElement, out _))
                {
                    throw rpaex;
                }
            }
            return null;
        }
        #endregion
        public static string FromSelectorAll(GetElementFromSelectorParams fromeSelectorParams,
             out ElementMatchType matchType
            )
        {
            matchType = ElementMatchType.Normal;
            var byIndex = ExtractViewData.SelectIsFindbyIndex(fromeSelectorParams.Selector, out CommonProps commonProps);
            var isSmartControl = commonProps.isSmartControl.As<bool>();
            bool isSimilarElementItem = commonProps.ExtendedTags.HasFlag(ExtendedTags.SimilarElementItem);
            bool enableAiEnhanced = !isSimilarElementItem && (isSmartControl || GlobleConfigurations.EnableAiEnhancedScreenshot);
            fromeSelectorParams.DiscernHidden = commonProps.DiscernHidden.As<bool>();
            string eleSelectors = string.Empty;
            JArray eleIdArray = null;


            bool skipStandard = "aiEnhanced".Equals(fromeSelectorParams.VerifyMode, StringComparison.OrdinalIgnoreCase);
            bool skipAiRepair = "selector".Equals(fromeSelectorParams.VerifyMode, StringComparison.OrdinalIgnoreCase);

            // 标准元素结构查找（aiEnhanced 模式跳过）
            if (!skipStandard)
            {
                try
                {
                    eleSelectors = OriginalFromSelectorAll(fromeSelectorParams);
                    if (eleSelectors != null)
                        eleIdArray = JArray.Parse(eleSelectors);
                    if (eleIdArray == null || eleIdArray.Count == 0)
                    {
                        eleSelectors = string.Empty;
                    }
                }
                catch (Exception ex)
                {
                    if (ex is RPAException)
                    {
                        var rpaex = (RPAException)ex;
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
                            throw rpaex;
                        }
                        else if (rpaex.IsRPAException(eErrorMessageType.Error_EmbeddedIEMode, out _))
                        {
                            logger.LogDebug($"内嵌IE模式 将按照IE模式重新执行");
                            fromeSelectorParams.SelectorType = eGuiType.IE.ToString();
                            return FromSelectorAll(fromeSelectorParams, out matchType);
                        }
                    }

                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "OriginalFromSelectorAll Find Fail ,NotFoundElement,Ignore");
                    if (!enableAiEnhanced && !byIndex)
                        throw ex;
                }
            }

            if (string.IsNullOrEmpty(eleSelectors))
            {
                if (byIndex && !skipStandard)
                {
                    var selectorByIndex = ExtractPropsToIndex(fromeSelectorParams.Selector);
                    var commonSelector = ExtractViewData.AddCommonProps(selectorByIndex, fromeSelectorParams.SelectorType);
                    fromeSelectorParams.Selector = commonSelector;
                    eleSelectors = OriginalFromSelectorAll(fromeSelectorParams);
                    if (eleSelectors != null)
                        eleIdArray = JArray.Parse(eleSelectors);
                    if (eleIdArray == null || eleIdArray.Count == 0)
                    {
                        eleSelectors = string.Empty;
                    }
                }
            }
            // 智能修复：selector 模式跳过，aiEnhanced 模式直接走，默认（空）为补位逻辑
            bool shouldAiRepair = string.IsNullOrEmpty(eleSelectors) && enableAiEnhanced
                && (skipStandard || !commonProps.GUIType.Equals(eGuiType.CV.ToString(), StringComparison.OrdinalIgnoreCase));
            if (shouldAiRepair && !skipAiRepair)
            {
                //只有在校验时，可能存在单元素组件场景 执行多元素检验逻辑，所以根据resultAttrName 再进行一次过滤
                var dicExtendParam = fromeSelectorParams.ExtendParam.ToObject<Dictionary<string, object>>();
                if (dicExtendParam.TryGetValue("resultAttrName", out var resultAttrName) && resultAttrName.ToString() == "bounding")
                {
                    commonProps.GUIType = fromeSelectorParams.SelectorType;
                    string runModel = "validation";
                    string originalElementId = JObject.Parse(fromeSelectorParams.Selector)
                        ?.GetValue("elementDetail")
                        ?.Value<string>("elementId") ?? "";
                    var node = AiRepairUtils.ExecAiRepair_V2(commonProps, runModel, originalElementId);
                    if (node != null)
                    {
                        matchType = ElementMatchType.AiRepair;
                        eleSelectors = new[] { node.BoundingRectangle }.ToJson();
                    }
                }
            }
            return eleSelectors;
        }
        public static string OriginalFromSelectorAll(GetElementFromSelectorParams param)
        {
            string elements = null;

            IGuiOperator_6_9_0 guiOperator = (IGuiOperator_6_9_0)GuiOperatorProvider.Instance.GetGuiOperator(param.SelectorType, param.WindowInfo);
            if (guiOperator != null)
            {
                logger.LogDebug($"找到GuiOprator:{guiOperator.GuiType}");
                elements = guiOperator.GetElementFromSelectorAll(param);
            }

            return elements;
        }


        public static string ExtractPropsToIndex(string selector)
        {
            var nativeSelector = UiCommon.ExtractSelectors(selector);
            var listSelector = JsonConvert.DeserializeObject<List<SelectorProps>>(nativeSelector);
            foreach (var props in listSelector)
            {
                foreach (var prop in props.Props)
                {
                    if (prop.Name == "@index")
                    {
                        if (prop.Accurate != "true")
                            prop.Accurate = "true";
                    }
                    else
                    {
                        prop.Accurate = "false";
                    }
                }
            }
            return JsonConvert.SerializeObject(listSelector);
        }
        public static GuiElement FromSelector(string selector, string selectorType, object extendParam)
        {
            return FromSelector(selector, selectorType, false, "", extendParam);
        }
        public static GuiElement FromSelector(string selector, string selectorType, bool findOnce = false, string CVExecutionServiceURL = "", object extendParam = null, bool isMove = true, bool isHighlighting = true, bool isFromOcrTable = false)
        {
            var param = new GetElementFromSelectorParams
            {
                Selector = selector,
                SelectorType = selectorType,
                FindOnce = findOnce,
                CVExecutionServiceURL = CVExecutionServiceURL,
                ExtendParam = extendParam,
                isMove = isMove,
                isActive = isHighlighting,
                isFromOCRTable = isFromOcrTable
            };
            return FromSelector(param);
        }
        public static GuiElement FromSelector(GetElementFromSelectorParams fromeSelectorParams)
        {
            AiBuriedDataModel aiBuriedDataModel = new AiBuriedDataModel();
            var byIndex = ExtractViewData.SelectIsFindbyIndex(fromeSelectorParams.Selector, out CommonProps commonProps);
            var isSmartControl = commonProps.isSmartControl.As<bool>();
            var targetFlags = ExtendedTags.SimilarElementItem | ExtendedTags.WindowSelectorType;
            //排除的标识位类型元素（目前是相似元素、窗口、含变量元素）
            bool isExcludedFlagsType = (commonProps.ExtendedTags & targetFlags) != 0;
            //排除的几个不触发AI增强的元素类型
            bool isExcludedGuiType = commonProps.GUIType.Equals(eGuiType.CV.ToString(), StringComparison.OrdinalIgnoreCase) ||
                commonProps.GUIType.Equals(eGuiType.Java.ToString(), StringComparison.OrdinalIgnoreCase) ||
                commonProps.GUIType.Equals(eGuiType.Swing.ToString(), StringComparison.OrdinalIgnoreCase);
            bool enableAiEnhanced = !isExcludedFlagsType && !isExcludedGuiType && (isSmartControl || GlobleConfigurations.EnableAiEnhancedScreenshot);
            bool isBeforeVersion16 = RPAUtils.CompareVersionNumber(commonProps.Version, "1.6") < 0;
            fromeSelectorParams.DiscernHidden = commonProps.DiscernHidden.As<bool>();

            GuiElement node = null;
            bool isCV = commonProps.GUIType.Equals(eGuiType.CV.ToString(), StringComparison.OrdinalIgnoreCase);
            bool matchType_Normal = fromeSelectorParams.MatchType == ElementMatchType.None || (fromeSelectorParams.MatchType & ElementMatchType.Normal) != 0;
            bool matchType_AiRepair = (fromeSelectorParams.MatchType & ElementMatchType.AiRepair) != 0;
            bool isUseAiCache = false;//是否缓存修复
            bool isUseAiRepair = false;//是否全新修复
            bool useFlowGuardLocate = false;//是否使用全局开关触发的元素寻回
            //未指定匹配方式，或指定了普通拾取 两种情况下，使用普通拾取
            if (matchType_Normal)
            {
                Stopwatch matchElement_sp = Stopwatch.StartNew();
                if (!matchType_AiRepair && isSmartControl && !isExcludedFlagsType && !isExcludedGuiType && !isCV)
                {
                    logger.LogDebug($"元素设置已启用智能修复，将尝试使用修复缓存数据");
                    node = OriginalFromSelector_aiRepair(fromeSelectorParams, byIndex);
                    if (node != null)
                    {
                        aiBuriedDataModel.AiRepaireStatus = AiRepaireStatus.RepaireSuccess;
                        isUseAiCache = true;
                    }
                }
                //使用智能修复缓存数据匹配之后，再使用组件selector匹配
                var commponentSelectorNode = OriginalFromSelector(fromeSelectorParams, enableAiEnhanced, byIndex);
                if (commponentSelectorNode != null)
                {
                    logger.LogDebug($"组件关联selector正确匹配到元素，将被使用");
                    AiRepairUtils.ClearAiRepairData(commonProps.GetRunUniqueID());
                    node = commponentSelectorNode;
                    isUseAiCache = false;
                    //全局开关已开，同时需满足（1.6版本之前&非java、cv、窗口类元素&不含变量元素），才触发截图寻回
                    if (GlobleConfigurations.EnableAiEnhancedScreenshot && isBeforeVersion16 && !isExcludedFlagsType && !isExcludedGuiType)
                    {
                        _ = ElementSceneRecoveryService.TryRecoverAsync(fromeSelectorParams.Selector, node);
                    }
                }
                else
                {
                    logger.LogDebug($"使用组件关联selector未匹配到元素");
                }
                matchElement_sp.Stop();
                logger.LogDebug($"普通元素匹配元素耗时：{matchElement_sp.ElapsedMilliseconds} ms");
            }
            if (node == null && enableAiEnhanced && matchType_AiRepair)
            {
                isUseAiRepair = true;
                IGuiOperator_6_9_0 guiOperator = (IGuiOperator_6_9_0)GuiOperatorProvider.Instance.GetGuiOperator(fromeSelectorParams.SelectorType, fromeSelectorParams.WindowInfo);

                var isTargetWin = (guiOperator as IGuiOperator_6_9_0)?.VerifyWindowOfNode(fromeSelectorParams);
                if (isTargetWin == true)
                {
                    AiRepairUtils.ClearAiRepairData(commonProps.GetRunUniqueID());
                    string runMode = "runtime";//此处应该是只有执行时查找元素会走到这里，校验不会
                    string originalElementId = JObject.Parse(fromeSelectorParams.Selector)
                        ?.GetValue("elementDetail")
                        ?.Value<string>("elementId") ?? "";
                    if (isBeforeVersion16)
                    {
                        useFlowGuardLocate = GlobleConfigurations.EnableAiEnhancedScreenshot;
                    }
                    else
                    {
                        useFlowGuardLocate = false;
                    }
                    node = AiRepairUtils.ExecAiRepair_V2(commonProps, runMode, originalElementId, useFlowGuardLocate);
                }
                aiBuriedDataModel.AiRepaireStatus = node != null ? AiRepaireStatus.RepaireSuccess : AiRepaireStatus.RepaireFail;
            }

            #region 智能修复埋点数据
            RPACommon rPACommon = JsonConvert.DeserializeObject<RPACommon>(fromeSelectorParams.Selector);
            //只在使用缓存修复成功或全新修复时才上传埋点数据
            if ((isUseAiCache || isUseAiRepair) && enableAiEnhanced)
            {
                aiBuriedDataModel.IsEnableAiRepaire = enableAiEnhanced;
                aiBuriedDataModel.ElementId = rPACommon.ElementDetail.ElementId;
                aiBuriedDataModel.ElementName = rPACommon.ElementDetail.ElementName;
                aiBuriedDataModel.TargetApp = rPACommon.ElementDetail.CategoryName;
                aiBuriedDataModel.IsAiRepaireOfCache = isUseAiCache;
                aiBuriedDataModel.AiRepaireType = !isBeforeVersion16 ? AiRepaireType.AiPlus : AiRepaireType.Intelligent;
                aiBuriedDataModel.AiRepairePoint = AiRepairUtils.LastRepaireNodePoint;
                aiBuriedDataModel.AiRepaireTriggerType = !(GlobleConfigurations.EnableAiEnhancedScreenshot && (isBeforeVersion16 || !isSmartControl)) ? AiRepaireTriggerType.ElementSelf : AiRepaireTriggerType.ExecutionEntrance;
                aiBuriedDataModel.AiRepaireCode = AiRepairUtils.LastRepaireStatusCode;
                aiBuriedDataModel.AiRepaireErrorMsg = AiRepairUtils.LastRepaireErrorMsg;
                var aiBuriedKey = $"{fromeSelectorParams.RunParam.StartTimeStamp}{fromeSelectorParams.RunParam.CurrentAttempt}";
                GlobleConfigurations.AiBuriedDataCacheDic.Add(aiBuriedKey, aiBuriedDataModel.ToJsonNoneFormating());
            }
            //UiCommon.AiBuriedDataCacheDic.Add(aiBuriedKey, aiBuriedDataModel.ToJsonNoneFormating());
            #endregion

            if (node == null)
            {
                string repairFailMsg = !isBeforeVersion16 ? "AI增强定位失败" : "智能修复失败";
                string erroCustomMsg = ((matchType_AiRepair || GlobleConfigurations.EnableAiEnhancedScreenshot) && enableAiEnhanced && !isCV) ?
                    $"元素查找失败，{repairFailMsg}，元素名：{rPACommon.ElementDetail.ElementName}；请确保操作界面已打开或可见" :
                    Localize($"元素查找失败，元素名：{rPACommon.ElementDetail.ElementName}；请确保操作界面已打开或可见", "GuiElement_30F5B2D2");
                switch (fromeSelectorParams.SelectorType.ToLower())
                {
                    case "windows":
                        throw new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.None, erroCustomMsg, eGuiType.Windows);
                    case "ie":
                        throw new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.None, erroCustomMsg, eGuiType.IE);
                    case "sap":
                        throw new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.None, erroCustomMsg, eGuiType.SAP);
                    case "cv":
                        throw new RPAException(eGuiType.CV, eErrorMessageType.Error_CVElementMatchingError, eComponentType.None);
                    case "chrome":
                        throw new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.None, erroCustomMsg, eGuiType.Chrome);
                    case "firefox":
                        throw new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.None, erroCustomMsg, eGuiType.FireFox);
                    case "java":
                        throw new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.None, erroCustomMsg, eGuiType.Java);
                    default:
                        throw new RPAException(eErrorMessageType.Error_NotFoundElement, erroCustomMsg);
                }
            }
            return node;
        }
        public static GuiElement OriginalFromSelector_aiRepair(GetElementFromSelectorParams fromeSelectorParams, bool byIndex)
        {
            IGuiNode node = null;
            IGuiOperator guiOperator = null;
            var fromeSelectorParams_copy = fromeSelectorParams.ToObject<GetElementFromSelectorParams>();
            fromeSelectorParams_copy.WindowInfo = fromeSelectorParams.WindowInfo;
            fromeSelectorParams_copy.ActionIEModeExcetpion = fromeSelectorParams.ActionIEModeExcetpion;
            try
            {
                RPACommon rpaCommon = fromeSelectorParams_copy.Selector.ToObject<RPACommon>();
                string runUniqueId = rpaCommon.CommonProps.GetRunUniqueID();
                var aiRepairSelector = AiRepairUtils.GetAiRepairSelector(runUniqueId, out long airepairTime);
                if (aiRepairSelector == null)
                {
                    logger.LogDebug($"无智能修复缓存数据，不进行修复缓存数据匹配，返回 null");
                    return null;
                }
                if (rpaCommon.CommonProps.GetModifyTime() <= airepairTime)
                {
                    logger.LogDebug($"判断存在智能修复缓存selector，并将使用此修复缓存selector");
                    rpaCommon.Selector = aiRepairSelector;
                    fromeSelectorParams_copy.Selector = rpaCommon.ToJson();
                }
                else
                {
                    logger.LogDebug($"缓存数据保存时间早于元素最后一次编辑时间，缓存数据失效，将清除");
                    AiRepairUtils.ClearAiRepairData(runUniqueId);
                    return null;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"处理智能修复缓存数据异常，本次不执行执行能修。异常信息：{ex}");
                return null;
            }
            return OriginalFromSelector(fromeSelectorParams_copy, true, byIndex);

        }
        public static GuiElement OriginalFromSelector(GetElementFromSelectorParams fromeSelectorParams, bool isSmartControl, bool byIndex)
        {
            IGuiNode node = null;
            IGuiOperator guiOperator = null;
            try
            {
                logger.LogDebug($"开始寻找{fromeSelectorParams.SelectorType} GuiOprator");

                guiOperator = (IGuiOperator_6_9_0)GuiOperatorProvider.Instance.GetGuiOperator(fromeSelectorParams.SelectorType, fromeSelectorParams.WindowInfo);
                logger.LogDebug($"找到GuiOprator:{guiOperator?.GuiType}");
                if (guiOperator == null) return null;
                node = guiOperator.GetElementFromSelector(fromeSelectorParams);
                logger.LogDebug($"匹配到结果：{node != null}");
            }
            catch (RPAException rpaex)
            {
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
                    throw rpaex;
                }
                else if (rpaex.IsRPAException(eErrorMessageType.Error_EmbeddedIEMode, out _))
                {
                    logger.LogDebug($"内嵌IE模式 将按照IE模式重新执行");
                    fromeSelectorParams.SelectorType = eGuiType.IE.ToString();
                    fromeSelectorParams.ActionIEModeExcetpion(rpaex);
                    return OriginalFromSelector(fromeSelectorParams, isSmartControl, byIndex);
                }

                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, rpaex, "OriginalFromSelector Find Fail ,NotFoundElement,Ignore，");
                if (!isSmartControl && !byIndex)
                {
                    throw rpaex;
                }
            }
            if (node == null)
            {
                try
                {
                    if (byIndex && !fromeSelectorParams.SelectorType.Equals(eGuiType.CV.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        var selectorByIndex = ExtractPropsToIndex(fromeSelectorParams.Selector);
                        var commonSelector = ExtractViewData.AddCommonProps(selectorByIndex, fromeSelectorParams.SelectorType);
                        fromeSelectorParams.Selector = commonSelector;
                        node = guiOperator.GetElementFromSelector(fromeSelectorParams);
                    }
                }
                catch (Exception ex)
                {
                    if (!isSmartControl)
                    {
                        throw ex;
                    }
                }
            }
            return node != null ? new GuiElement(node) : null;
        }
        internal static bool Highlightings(List<Rectangle> rects, bool useTightBorder = false)
        {
            return HighlightingByRegin(rects, useTightBorder);
        }
        internal static bool HighlightingByRegin(List<Rectangle> recs, bool useTightBorder = false)
        {
            return OverlayDispatcher.Invoke(() =>
            {
                // Highlighting must not replace or hide an active capture's overlay.
                if (OverlayCaptureGate.IsOccupied)
                    return false;
                if (recs.Count > 0)
                {
                    PrepareOverlayForm();
                    HighlightRectsFlash(recs, useTightBorder);
                }
                return true;
            });
        }

        public static void HighlightRectsFlash(List<Rectangle> recs, bool useTightBorder = false)
        {
            logger.LogDebug("GuiElement.overlayForm.BeginInvoke");
            GuiElement.Highlighting(recs, useTightBorder);
        }



        /// <summary>
        /// 高亮元素
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        internal static bool Highlighting(GuiElement element)
        {
            var eleType = ElementType.Ordinary;
            if (element.uiNode is IGuiCVNode590)
            {
                eleType = ElementType.Intelligence;
            }
            return HighlightingByRegin(element.GetRegion(3), eleType);
        }

        /// <summary>
        /// 高亮元素
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        internal static bool ElementHighLighterFlash(GuiElement element)
        {
            return ElementHighLighterFlash(element.GetRegion(3), IsTightHighlightNode(element.uiNode));
        }

        private static bool IsTightHighlightNode(IGuiNode node)
        {
            return node is IGuiNode_6_1_0 guiNode && guiNode.GuiType == eGuiType.SAP;
        }

        /// <summary>
        /// 高亮元素
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        internal static bool HighlightingByRegin(Rectangle rec)
        {
            return HighlightingByRegin(rec, ElementType.Ordinary);
        }

        #region 高亮旧实现
        /*
        public static bool ElementHighLighterFlash(Rectangle rec)
        {
            try
            {
                logger.LogDebug($"进入高亮闪烁 {rec.ToJson()}");
                if (Thread.CurrentThread.ManagedThreadId != 1)
                {
                    logger.LogInformation("async call HighlightingByRegin，renew form");
                    GuiElement.overlayForm = new OverlayForm();
                    var handle = GuiElement.overlayForm.Handle;
                }

                ElementHighLighterParam _ElementHighLighterParam = new ElementHighLighterParam();
                _ElementHighLighterParam.CHighLighterFlashParam.IsFlashEnable = true;
                _ElementHighLighterParam.CRectangle = rec;
                GuiElement.overlayForm.Invoke(new Action(() =>
                {
                    GuiElement.overlayForm.CurrentElementHighlighterManager.ShowElementFlash(_ElementHighLighterParam);
                }));
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        */

        #endregion

        public static bool ElementHighLighterFlash(Rectangle rec, bool useTightBorder = false)
        {
            try
            {
                //GuiElement.overlayForm.CurrentElementHighlighterManager.Flash(rec).Wait();
                GuiElement.overlayForm.CurrentElementHighlighterManager.Flash(rec, useTightBorder);
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 高亮元素
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        internal static bool HighlightingByRegin(Rectangle rec, ElementType eleType)
        {
            return OverlayDispatcher.Invoke(() =>
            {
                if (OverlayCaptureGate.IsOccupied)
                    return false;
                var form = PrepareOverlayForm();
                GuiElement.Highlighting(rec, eleType);
                form.Opacity = 0.99;
                form.TopMost = true;
                try
                {
                    for (int i = 0; i < 3; i++)
                    {
                        if (i != 0)
                            Thread.Sleep(150);
                        form.Show();
                        Thread.Sleep(150);
                        form.Hide();
                    }
                }
                finally
                {
                    form.TopMost = false;
                    form.Hide();
                    form.SetWndHide();
                }
                return true;
            });
        }

        public static string GetSelectorType(string selector)
        {
            string selectorType;
            CommonParseSelector.ParseSelectorV1(selector, out selectorType, out string version);
            return selectorType;
        }



        public static List<JObject> ParseSelector(string param, ref string selector, out string value)
        {
            selector = RecoverViewData.RestoreViewData(selector);
            List<JObject> listTemp = new List<JObject>();
            value = string.Empty;
            try
            {
                JArray ja = JArray.Parse(selector);
                foreach (JObject obj in ja)
                {
                    listTemp.Add(obj);
                    if (obj.ContainsKey(param))
                    {
                        value = obj.Value<string>(param).ToString();
                    }

                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
            return listTemp;
        }
        /************************/

        ////Bitmap cacheContainerImg;
        //public Bitmap ElementToBitmap()
        //{
        //    Bitmap tmp = null;
        //    if (uiNode is CVNode)
        //    {
        //        CVNode node = uiNode as CVNode;
        //        tmp = node.ElementToBitmap();
        //        return tmp;
        //    }
        //    return null;
        //}
        public Bitmap CaptureInformativeScreenshot(string filepath, Bitmap bitmap)
        {
            Bitmap target = null;
            try
            {
                string dirName = Path.GetDirectoryName(filepath);

                if (!Directory.Exists(dirName))
                {
                    Directory.CreateDirectory(dirName);
                }
                currentDesktopScreenshot = bitmap;

                if (uiNode.BoundingRectangle.IsEmpty)
                {
                    //QQ截取时会出现为空的情况
                    return null;
                }
                target = DrawInformativeOnUiNode(currentDesktopScreenshot);
                try
                {
                    target.Save(filepath);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex);
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "snap screen shot fail");
            }
            return target;
        }
        private Bitmap DrawInformativeOnUiNode(Bitmap currentDesktopScreenshot)
        {
            var ratio = NativeMethods.ScaleX;
            Rectangle cropRect = new Rectangle(
                Math.Max((int)(uiNode.BoundingRectangle.X * ratio), 0),
                Math.Max((int)(uiNode.BoundingRectangle.Y * ratio), 0),
                (int)(uiNode.BoundingRectangle.Width * ratio),
                (int)(uiNode.BoundingRectangle.Height * ratio));
            using (Graphics g = Graphics.FromImage(currentDesktopScreenshot))
            {
                //最终截图的红色标记边框
                Pen pen = new Pen(Color.Red, 2);
                g.DrawRectangle(pen, cropRect);
            }
            if ((cropRect.Width + cropRect.X) > currentDesktopScreenshot.Width)
            {
                cropRect.Width = currentDesktopScreenshot.Width - cropRect.X;
            }
            if ((cropRect.Height + cropRect.Y) > currentDesktopScreenshot.Height)
            {
                cropRect.Height = currentDesktopScreenshot.Height - cropRect.Y;
            }

            BoundingRectangle = uiNode.BoundingRectangle;
            cropRect.Inflate(40, 40);
            Bitmap target = new Bitmap(cropRect.Width, cropRect.Height);

            using (Graphics g = Graphics.FromImage(target))
            {
                logger.LogDebug($"cropRect:{cropRect.Width}+{cropRect.Height}");

                g.DrawImage(currentDesktopScreenshot, new Rectangle(0, 0, target.Width, target.Height),
                                 cropRect,
                                 GraphicsUnit.Pixel);
            }

            return ImageHelper.CompressImage(target, 900, 900);
        }

        public string CaptureInformativeScreenshotToFile(string filePath = null)
        {
            var ret = "";
            if (filePath == null)
            {
                var guid = Guid.NewGuid().ToString("N");

                var screenshotsPath = Application.StartupPath + @"\.screenshots";
                if (!System.IO.Directory.Exists(screenshotsPath))
                {
                    System.IO.Directory.CreateDirectory(screenshotsPath);
                }

                var fileName = guid + @".png";
                ret = fileName;
                filePath = screenshotsPath + @"\" + fileName;
            }
            var deskTop = UiCommon.UIAAutomation.GetDesktop().Capture();
            if (currentInformativeScreenshot == null)
            {
                //之前生成框选元素图时出错，需要重新生成
                currentInformativeScreenshot = DrawInformativeOnUiNode(deskTop);
            }
            try
            {
                currentInformativeScreenshot.Save(filePath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }

            return filePath;
        }

        public static void Highlighting(List<Rectangle> rectangles, bool useTightBorder = false)
        {
            overlayForm.Highlighting(rectangles, useTightBorder);
        }
        public static void Highlighting(Rectangle rectangle)
        {
            Highlighting(rectangle, ElementType.Ordinary);
        }
        public static void Highlighting(Rectangle rectangle, ElementType eleType)
        {
            overlayForm.Highlighting(rectangle, eleType);
        }
        /// <summary>
        /// 元素方式高亮
        /// </summary>
        public static void StartElementHighlight(string filepath, bool isCVCapture = false, string cvServer = null, int InspectStatus = 0, string selector = null, CaptureModeType captureModeType = CaptureModeType.Intelligence, int isCvSupport = 0)
        {
            overlayForm.StartHighlight(filepath, isCVCapture, cvServer, InspectStatus, selector, captureModeType, isCvSupport);
        }
        public static void StartCVRegionHighlight(string filepath)
        {
            overlayForm.StartRegionHighlight(filepath);
        }
        #region 元素操作事件
        public List<string> GetHtmlAll(List<GuiElement> guiNodes)
        {
            var list = guiNodes.Select(node => { return (IGuiNode)node.uiNode; }).ToList();
            return ((IGuiNode_6_6_0)list.FirstOrDefault()).GetHtmlAll(list);
        }
        public string GetHtml()
        {
            var uiwebNode = uiNode as IGuiWebNode;
            return uiwebNode.GetHtml();
        }
        public string GetCleanHtml()
        {
            var uiwebNode = uiNode as IGuiWebNode;
            return uiwebNode.GetCleanHtml();
        }
        public void IntoView()
        {
            uiNode.IntoView();
        }
        public string GetSubElements(ParamGetSubElements param)
        {
            return ((IGuiNode_6_3_0)uiNode).GetSubElements(param);
        }
        public string GetParent(ParamGetParent param)
        {
            if (uiNode is IGuiNode_6_3_0)
            {
                return ((IGuiNode_6_3_0)uiNode).GetParent(param);
            }
            throw new NotImplementedException();
        }
        public string GetSiblings(ParamGetSiblings param)
        {
            if (uiNode is IGuiNode_6_3_0)
                return ((IGuiNode_6_3_0)uiNode).GetSiblings(param);
            throw new NotImplementedException();
        }
        private Rectangle GetRectangle()
        {
            var element = uiNode as IGuiWebNode_690;
            if (element != null)
            {
                if (!element.IsIntoView)
                {
                    uiNode.IntoView();
                }
            }
            return uiNode.GetRegion(3);
        }
        /// <summary>
        /// 无需接口(基于chromium内核的直接走网页api截图)
        /// </summary>
        /// <param name="rectangle"></param>
        /// <param name="picPath"></param>
        public void Screenshot(string picPath, string selectorType = "", bool isUseVerifyCode = true)
        {

            if (uiNode is ElementGuiNode && !selectorType.EqualIgnoreCase("ie") && !isUseVerifyCode)
            {
                //基于chromium内核的直接走网页api截图
                this.GetElementshot(picPath);
            }
            else
            {
                var rectangle = GetRectangle();
                rectangle.Intersect(Screen.FromRectangle(rectangle).WorkingArea);
                using (Bitmap b = new Bitmap(rectangle.Width, rectangle.Height))
                {
                    using (Graphics g = Graphics.FromImage(b))
                    {
                        g.CopyFromScreen(rectangle.X, rectangle.Y, 0, 0, rectangle.Size);
                    }
                    b.Save(picPath);//保存图片
                }
            }
        }
        public string GetElementshot(string picPath)
        {
            try
            {
                string base64Img = string.Empty;
                var elementData = ((ElementGuiNode)uiNode).GetElementShot();
                bool isNeedScroll = bool.Parse(elementData[1]);
                var image = ((ElementGuiNode)uiNode).GetTabPageShot(isNeedScroll);
                using (var ms = new MemoryStream())
                {
                    image.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    base64Img = Convert.ToBase64String(ms.ToArray());
                }
                if (isNeedScroll)
                {
                    //如果需要滚动网页，则重新获取元素在上下文中的区域信息
                    elementData = ((ElementGuiNode)uiNode).GetElementShot();
                }
                var arryShotData = JsonConvert.DeserializeObject<List<object>>(elementData[0]);
                var browserScale = float.Parse(arryShotData[0].ToString());
                var rect = JsonConvert.DeserializeObject<ElementRect>(arryShotData[1].ToString());
                ElementShoter elementShoter = new ElementShoter();
                bool captureShotResult = elementShoter.ProcessScreenshot(new BrowserScreenshotData
                {
                    ScreenshotDataUrl = base64Img,
                    ElementRect = new ElementRect
                    {
                        Left = rect.Left,
                        Top = rect.Top,
                        Width = rect.Width,
                        Height = rect.Height
                    },
                    BrowserScaleFactor = browserScale
                }, picPath);
                if (!captureShotResult)
                {
                    throw new Exception("网页元素截图异常".I18n("controlType_GuiElement_9DA7ABF0"));
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                throw new Exception("网页元素截图异常".I18n("controlType_GuiElement_9DA7ABF0"), ex);
            }
            return picPath;
        }
        /// <summary>
        /// 高亮一个元素
        /// </summary>
        /// <param name="color">高亮的颜色</param>
        /// <param name="duration">延迟时间</param>
        /// <param name="blocking">是否堵塞</param>
        public void DrawHighlight(Color? color = null, TimeSpan? duration = null, bool blocking = false)
        {
            var colorName = color ?? Color.Red;
            var rectangle = uiNode.BoundingRectangle;
            this.BoundingRectangle = uiNode.BoundingRectangle;
            if (!rectangle.IsEmpty)
            {
                var durationInMs = (int)(duration ?? TimeSpan.FromSeconds(2)).TotalMilliseconds;

                var overlayManager = new WinFormsOverlayManager();
                if (blocking)
                {
                    overlayManager.ShowBlocking(rectangle, colorName, durationInMs);
                }
                else
                {
                    overlayManager.Show(rectangle, colorName, durationInMs);
                }
            }
        }

        //public void SetForeground()
        //{
        //    var directWindow = DirectTopLevelWindow;
        //    if (directWindow != null)
        //    {
        //        UiCommon.ForceShow(directWindow.WindowHandle);
        //        DirectTopLevelWindow.uiNode.SetForeground();
        //    }
        //}


        //根据相对坐标偏移量获取元素
        public GuiElement FindRelativeElement(int position, int offsetX, int offsetY)
        {
            IGuiNode relativeNode = uiNode.FindRelativeNode(position, offsetX, offsetY);
            GuiElement relativeElement = new GuiElement(relativeNode);
            return relativeElement;
        }
        public Point GetClickablePoint()
        {
            return uiNode.GetClickablePoint();
        }

        public void Focus()
        {
            uiNode.Focus();
        }
        public void SetText(string text, int isReplace, bool isSimulate)
        {
            uiNode.SetText(text, isReplace, isSimulate);
        }
        public List<string> GetTextAll(List<GuiElement> guiNodes)
        {
            var list = guiNodes.Select(node => { return (IGuiNode)node.uiNode; }).ToList();
            return ((IGuiNode_6_6_0)list.FirstOrDefault()).GetTextAll(list);
        }
        public string GetText()
        {
            return uiNode.GetText();
        }
        public void SetAttribute(string name, string value)
        {
            uiNode.SetElementAttribute(name, value);
        }
        public string GetAttribute_V1(string name, bool isIntact, string apiVersion)
        {
            return ((IGuiNode_6_6_0)uiNode).GetElementAttribute_V1(name, isIntact, apiVersion);
        }
        public List<string> GetAttributeAll(List<GuiElement> guiNodes, string attributeName, bool isIntact, string apiVersion)
        {
            var list = guiNodes.Select(node => { return (IGuiNode)node.uiNode; }).ToList();
            return ((IGuiNode_6_6_0)list.FirstOrDefault()).GetAttributeAll(list, attributeName, isIntact, apiVersion);
        }
        public string GetAttribute(string name, string apiVersion)
        {
            return uiNode.GetElementAttribute(name, apiVersion);
        }
        public string GetElementSelect(int selectType = 1)
        {
            return uiNode.GetElementSelect(selectType);
        }
        public string GetElementSelectEx(string strParamJson)
        {
            return ((IGuiNode_GetElementSelectExtension)uiNode).GetElementSelectEx(strParamJson);
        }
        public void SetElementSelect(int selectType, string setValue)
        {
            uiNode.SetElementSelect(selectType, setValue);
        }
        public void SetElementMultipleSelect(int selectType, string setValue)
        {
            uiNode.SetElementMultipleSelect(selectType, setValue);
        }
        public void SetElementSelectWithMatchModel(int selectType, int matchModel, string setValue)
        {
            uiNode.SetElementSelectWithMatchModel(selectType, matchModel, setValue);
        }
        public string GetEnableClick()
        {
            return uiNode.GetEnableClick();
        }
        public string GetElementCheck()
        {
            return uiNode.GetElementCheck();
        }

        public string FindElementWithPicPath(string strPicPath)
        {
            return uiNode.FindElementWithPicPath(strPicPath);
        }
        public string GetTableData(string input)
        {
            return uiNode.GetTableData(input);
        }
        public string ClickTableCell(string input)
        {
            return uiNode.ClickTableCell(input);
        }
        /// <summary>
        /// 勾选
        /// </summary>
        /// <param name="check"></param>
        /// <returns></returns>
        public void SetElementCheck(int check)
        {

            uiNode.SetElementCheck(check);
        }
        public List<string> GetSelectorAll(List<GuiElement> guiNodes, string sourceSelector)
        {
            var list = guiNodes.Select(node => { return (IGuiNode)node.uiNode; }).ToList();
            return ((IGuiNode_6_6_0)list.FirstOrDefault()).GetSelectorAll(list, sourceSelector);
        }
        public List<Rectangle> GetRegionAll(List<GuiElement> guiNodes, int positionType)
        {
            var list = guiNodes.Select(node => { return (IGuiNode)node.uiNode; }).ToList();
            return ((IGuiNode_6_6_0)list.FirstOrDefault()).GetRegionAll(list);
        }
        public Rectangle GetRegion(int positionType)
        {
            return uiNode.GetRegion(positionType);
        }
        /// <summary>
        /// CV获取文本，只能CVNode使用功能
        /// </summary>
        /// <param name="cvServer"></param>
        /// <returns></returns>
        public string GetTextByCV(string cvServer)
        {
            return ((IGuiCVNode)uiNode).GetText(cvServer);
        }
        public string FindRelativeElementsInfo(string strRelativeSelector, ref JArray context)
        {
            return uiNode.FindRelativeElementsInfo(strRelativeSelector, ref context);
        }

        #endregion`

        #region 键盘操作事件
        public static void KeyboardPress(VirtualKey virtualKey)
        {
            Keyboard.Press((VirtualKeyShort)virtualKey);
        }

        public static void KeyboardRelease(VirtualKey virtualKey)
        {
            Keyboard.Release((VirtualKeyShort)virtualKey);
        }

        public static void KeyboardType(string text)
        {
            Keyboard.Type(text);
        }
        #endregion

        #region 鼠标操作事件
        public void MouseConterClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            uiNode.MouseConterClick(offsetX, offsetY, clickParams);
        }

        public void MouseConterDoubleClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            uiNode.MouseConterDoubleClick(offsetX, offsetY, clickParams);
        }

        public void MouseHover(UiElementHoverParams hoverParams = null)
        {
            uiNode.MouseHover(hoverParams);
        }
        public void MouseClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            uiNode.MouseClick(offsetX, offsetY, clickParams);
        }

        public void MouseDoubleClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            uiNode.MouseDoubleClick(offsetX, offsetY, clickParams);
        }

        public void MouseRightClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            uiNode.MouseRightClick(offsetX, offsetY, clickParams);
        }

        public void MouseRightDoubleClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            uiNode.MouseRightDoubleClick(offsetX, offsetY, clickParams);
        }

        public static void MouseDrag(MouseButton mouseButton, Point startingPoint, Point endingPoint)
        {
            Mouse.Drag(startingPoint, endingPoint.X - startingPoint.X, endingPoint.Y - startingPoint.Y);
        }

        public static void MouseUp(MouseButton mouseButton)
        {
            Mouse.Up((FlaUI.Core.Input.MouseButton)mouseButton);
        }

        public static void MouseDown(MouseButton mouseButton)
        {
            Mouse.Down((FlaUI.Core.Input.MouseButton)mouseButton);
        }

        public static void MouseMoveTo(Point newPosition)
        {
            Mouse.MoveTo(newPosition);
        }

        public static void MouseMoveTo(int newX, int newY)
        {
            Mouse.MoveTo(newX, newY);
        }

        public static void MouseSetPostion(Point newPosition)
        {
            Mouse.Position = newPosition;
        }
        public static void MouseSetPostion(int newX, int newY)
        {
            Mouse.Position = new Point(newX, newY);
        }


        public static void MouseAction(ClickType clickType, MouseButton mouseButton)
        {
            switch (clickType)
            {
                case ClickType.Single:
                    MouseClick(mouseButton);
                    break;
                case ClickType.Double:
                    MouseDoubleClick(mouseButton);
                    break;
                case ClickType.Down:
                    MouseDown(mouseButton);
                    break;
                case ClickType.Up:
                    MouseUp(mouseButton);
                    break;
                default:
                    break;
            }
        }

        public static void MouseVerticalScroll(double lines)
        {
            Mouse.Scroll(lines);
        }

        public static void MouseHorizontalScroll(double lines)
        {
            Mouse.HorizontalScroll(lines);
        }

        public static void MouseClick(MouseButton mouseButton)
        {
            Mouse.Click((FlaUI.Core.Input.MouseButton)mouseButton);
        }

        public static void MouseDoubleClick(MouseButton mouseButton)
        {
            Mouse.DoubleClick((FlaUI.Core.Input.MouseButton)mouseButton);
        }

        public static void MouseLeftClick()
        {
            Mouse.LeftClick();
        }

        public static void MouseRightClick()
        {
            Mouse.RightClick();
        }

        public static void MouseLeftDoubleClick()
        {
            Mouse.LeftDoubleClick();
        }

        public static void MouseRightDoubleClick()
        {
            Mouse.RightDoubleClick();
        }
        #endregion

        internal static void CVRelease()
        {
            try
            {
                var oprator = GuiManager.CreateGuiCVOperator();
                oprator.Release();
            }
            catch (Exception e)
            {
                logger.LogError(e);
            }
        }
        internal static void ReleaseGuiResources()
        {
            try
            {
                //GlobleConfigurations.ElementAPPDic.Clear();//清除缓存的句柄和进程之间的映射关系
                var releaseTasks = GuiOperatorProvider.Instance.GuiOperatorEnumerable
                    .Select(op => Task.Run(() =>
                    {
                        try { op.Release(); }
                        catch (Exception ex)
                        {
                            Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "ReleaseCapture error on {Operator}", op.GetType().Name);
                        }
                    }))
                    .ToArray();
                Task.WaitAll(releaseTasks, TimeSpan.FromSeconds(2));
            }
            catch (Exception e)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "ReleaseCapture aggregate error");
            }
        }
        internal static void MoveFromLocation(Point screenPoint)
        {
            try
            {
                logger.LogDebug("移动状态窗口");
                GuiElement.overlayForm?.inspectorStatus?.MoveFromLocation(screenPoint);
            }
            catch (Exception)
            {
                logger.LogDebug("移动状态窗口 本次异常，将继续运行");
            }
        }
        #region 鼠标三击
        public void MouseThreeClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            IGuiNode_MouseExtension mnode = (IGuiNode_MouseExtension)uiNode;
            mnode.MouseThreeClick(offsetX, offsetY, clickParams);
        }
        public void MouseRightThreeClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            IGuiNode_MouseExtension mnode = (IGuiNode_MouseExtension)uiNode;
            mnode.MouseRightThreeClick(offsetX, offsetY, clickParams);
        }
        public void MouseConterThreeClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            IGuiNode_MouseExtension mnode = (IGuiNode_MouseExtension)uiNode;
            mnode.MouseConterThreeClick(offsetX, offsetY, clickParams);
        }
        #endregion

        public List<GuiElement> QueryTargetSelectorAll(Commons.Params.ParamQueryTargetSelectorAllModel param)
        {
            List<GuiElement> elements = new List<GuiElement>();
            IGuiNode_6_6_0 target = (IGuiNode_6_6_0)uiNode;
            var nodes = target.QueryTargetSelectorAll(param);
            nodes.ForEach(node => { elements.Add(new GuiElement(node)); });
            return elements;
        }
        public bool SetText(Commons.Params.ParamSetTextModel param)
        {
            IGuiNode_6_5_0 input = (IGuiNode_6_5_0)uiNode;
            return input.SetText(param);
        }
        public void SetText(string text, int isReplace, int inputType, int interval)
        {
            IGuiNode_InputExtension inode = (IGuiNode_InputExtension)uiNode;
            inode.SetText(text, isReplace, inputType, interval);//修改位置
        }
        public string GetCVOriginImage(string cvServer)
        {
            return ((IGuiCVNode604)uiNode).GetOriginImage(cvServer);
        }
        public bool IsShow()
        {
            //CV部分继承接口方法结构有差异,提前过滤
            return uiNode is IGuiCVNode ? true : ((IGuiNode_6_4_0)uiNode).IsShow;
        }
        public string ElementExecutejs(string scriptCode, string @params, string executionEnvironment = "", bool isAsyncCode = false, float waitResultTimeout = 0, bool catchException = false)
        {
            return ((IGuiWebNode_690)uiNode).Executejs(scriptCode, @params, executionEnvironment, isAsyncCode, waitResultTimeout, catchException);
        }
        public string ElementDownload(string saveFile, string fileName, int timeout)
        {
            return ((IGuiWebNode_690)uiNode).ElementDownload(saveFile, fileName, timeout);
        }
        public string ElementDownloader(II.RPA.Plugin.GUI.Commons.Params.ParamDownloaderModel param)
        {
            return ((IGuiWebNode_690)uiNode).ElementDownloader(param);
        }
        public void ElementFileUploader(II.RPA.Plugin.GUI.Commons.Params.ParamFileUploaderModel param)
        {
            ((IGuiWebNode_690)uiNode).ElementFileUploader(param);
        }

        public string GetLineAndBarChartData(Commons.Params.ParamGetChartDataModel param)
        {
            return ((IGuiWebNode_690)uiNode).GetLineAndBarChartData(param);
        }
        public string GetPieChartData(Commons.Params.ParamGetChartDataModel param)
        {
            return ((IGuiWebNode_690)uiNode).GetPieChartData(param);
        }

        #region 数据采集
        public List<SelectorProps> AutoBulidCollectDataSelector(ParamBulidCollectDataSelectorModel paramModel)
        {
            if (this.uiNode is IGuiNode_6_1_0)
            {
                IGuiNode_6_1_0 uiNode_dataCollecting = (IGuiNode_6_1_0)this.uiNode;
                return uiNode_dataCollecting.AutoBulidCollectDataSelector(paramModel);
            }
            else
            {
                throw new RPAException(eErrorMessageType.Error_DataCollection_GUINoSupport);
            }
        }
        public List<SelectorProps> CompareBulidCollectDataSelector(ParamBulidCollectDataSelectorModel paramModel)
        {
            if (this.uiNode is IGuiNode_6_1_0)
            {
                IGuiNode_6_1_0 uiNode_dataCollecting = (IGuiNode_6_1_0)this.uiNode;
                var selector = uiNode_dataCollecting.CompareBulidCollectDataSelector(paramModel);
                if (selector == null || selector.Count == 0)
                {
                    throw new RPAException(eErrorMessageType.Error_DataCollection_SimileCaptureDifferentElementTypes);
                }
                return selector;
            }
            else
            {
                throw new RPAException(eErrorMessageType.Error_DataCollection_GUINoSupport);
            }
        }
        public List<SelectorProps> CompareBulidCollectDataSelectorV2(ParamBulidCollectDataSelectorModel paramModel)
        {
            List<SelectorProps> selector = null;
            if (this.uiNode is IGuiWebNode_690)
            {
                IGuiWebNode_690 uiNode_dataCollecting = (IGuiWebNode_690)this.uiNode;
                selector = uiNode_dataCollecting.CompareBuildCollectDataSelectorV2(paramModel);
            }
            else if (this.uiNode is IGuiNode_6_1_0)
            {
                IGuiNode_6_1_0 uiNode_dataCollecting = (IGuiNode_6_1_0)this.uiNode;
                selector = uiNode_dataCollecting.CompareBulidCollectDataSelector(paramModel);
            }
            else
            {
                throw new RPAException(eErrorMessageType.Error_DataCollection_GUINoSupport);
            }
            if (selector == null || selector.Count == 0)
            {
                throw new RPAException(eErrorMessageType.Error_DataCollection_SimileCaptureDifferentElementTypes);
            }
            return selector;
        }
        public bool ElementIsTable()
        {
            if (uiNode is IGuiNode_6_1_0)
            {
                return ((IGuiNode_6_1_0)uiNode).IsTable;
            }
            return false;
        }
        public bool TryGetTableElement(out IGuiWebNode_690 tableNode)
        {
            if (uiNode is IGuiWebNode_690)
            {
                return ((IGuiWebNode_690)uiNode).TryGetTableElement(out tableNode);
            }
            tableNode = null;
            return false;
        }
        public string BuildSmartSelector(List<Dictionary<string, object>> validNodeAttributes)
        {
            if (uiNode is IGuiWebNode_690)
            {
                return ((IGuiWebNode_690)uiNode).BuildSmartSelector(validNodeAttributes);
            }
            return string.Empty;
        }
        public string GetNextElementBuildSelector()
        {
            if (uiNode is IGuiNode_6_1_0)
            {
                return ((IGuiNode_6_1_0)uiNode).GetNextElementBuildSelector(Configurations.Info.DataCollectionTurnPageButtonCssSelectorList);
            }
            return string.Empty;
        }
        public IGuiNode GetTurningPageElement()
        {
            if (uiNode is IGuiWebNode_690)
            {
                return ((IGuiWebNode_690)uiNode).GetTurningPageElement(new List<string>());
            }
            return null;
        }
        public List<SelectorProps> GetSimilarElementSelector(List<SelectorProps> originalSelector)
        {
            if (uiNode is IGuiWebNode_690)
            {
                return ((IGuiWebNode_690)uiNode).GetSimilarElementSelector(originalSelector);
            }
            return new List<SelectorProps>();
        }
        #endregion

        #region 
        //自动获取循环相似元素结构
        public string GetSimilarSelectorByAuto()
        {
            if (uiNode is IGuiWebNode_690)
            {
                return (uiNode as IGuiWebNode_690).GetSimilarSelectorByAuto();
            }
            else
            {
                //非网页类元素返回当前元素自身结构
                return Selector;
            }
        }
        #endregion

    }
}


