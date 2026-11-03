using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using II.RPA.Logging;
using II.RPA.Logging.Formatting;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.Plugin.GUI.Commons.WindowMatch;
using II.RPA.Plugin.Java.Execute;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using WindowsAccessBridgeInterop;
using UiCommon = II.RPA.Plugin.GUI.Commons.UiCommon;

namespace II.RPA.Plugin.Java
{
    public class GuiOperator : IGuiOperator_6_9_0, IGuiOperator
    {    
        private static ILogger logger = Logger.OfContext();
  
        private List<IntPtr> windowHandles = new List<IntPtr>();
        private readonly IJavaUiThread javaUiThread;
        private readonly AccessBridgeProvider accessBridgeProvider;

        internal GuiOperator(IJavaUiThread javaUiThread)
            : this(javaUiThread, AccessBridgeProvider.Singleton)
        {
        }

        internal GuiOperator(
            IJavaUiThread javaUiThread,
            AccessBridgeProvider accessBridgeProvider)
        {
            //！！！此方法要求UI线程！！！
            this.javaUiThread = javaUiThread
                ?? throw new ArgumentNullException(nameof(javaUiThread));
            this.accessBridgeProvider = accessBridgeProvider
                ?? throw new ArgumentNullException(nameof(accessBridgeProvider));
            this.accessBridgeProvider.AttachUiThread(javaUiThread);
            this.accessBridgeProvider.InitBridge();
        }

        public eGuiType GuiType
        {
            get => eGuiType.Java;
        }
        public int Sort
        {
            get;
            set;
        }

        #region 拾取相关方法
        public bool InspectNode(Point screenPoint,
            AutomationElement rootElement,
            AutomationElement pointElement,
            string selector,
            out IGuiNode inspectNode)
        {
            var result = javaUiThread.Invoke(() =>
            {
                IGuiNode node;
                var success = InspectNodeCore(
                    screenPoint, rootElement, pointElement, selector, out node);
                return (success, node);
            });
            inspectNode = result.node;
            return result.success;
        }

        private bool InspectNodeCore(Point screenPoint,
            AutomationElement rootElement,
            AutomationElement pointElement,
            string selector,
            out IGuiNode inspectNode)
        {
            inspectNode = null;
            List<AccessibleJvm> jvms = null;
            var rootWnd = rootElement.Properties.NativeWindowHandle.ValueOrDefault;
            var pointWnd = pointElement.Properties.NativeWindowHandle.ValueOrDefault;
            if (rootWnd != pointWnd)
                jvms = JavaNode.EnumJvms(true); //模态窗口
            else
            {
                bool isNeedForce = !windowHandles.Contains(rootWnd);  //非模态窗口
                logger.LogDebug($"所有句柄 {windowHandles.ToJson()}");
                jvms = JavaNode.EnumJvms(isNeedForce);
            }

            //var jvms = JavaNode.accessBridge.EnumJvms(hwnd => JavaNode.accessBridge.CreateAccessibleWindow(node.WindowHandle));
            logger.LogInformation("jvms.count:" + jvms.Count);
            //是JAVA窗口，内部节点按JAVA方式选择
            Path<AccessibleNode> javaNodePath = null;
            try
            {
                javaNodePath = jvms.Select(jvm => jvm.GetNodePathAt(screenPoint)).Where(x => x != null).FirstOrDefault();
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "GetNodeError");
            }

            var currentJavaNode = javaNodePath == null ? null : javaNodePath.Leaf;
            if (currentJavaNode == null)
            {
                logger.LogInformation(LogBlock.NullCheck(screenPoint, "screenPoint"));
                return false;
            }
            var _javaNode = new JavaNode(currentJavaNode, pointElement);
            _javaNode.JVM = jvms.FirstOrDefault(jvm => jvm.JvmId == currentJavaNode.JvmId);
            _javaNode.CacheParentNode = (AccessibleContextNode)javaNodePath.ElementAt(javaNodePath.Count - 2);  //缓存下倒数第二个节点；
            _javaNode.SnapPoint = screenPoint;
            _javaNode.SimilarElementsSelector = selector;
            inspectNode = _javaNode;
            return true;
        }

        public bool IsCanUse(Point screenPoint,
            AutomationElement rootElement,
            AutomationElement pointElement)
        {
            return javaUiThread.Invoke(
                () => IsCanUseCore(screenPoint, rootElement, pointElement));
        }

        private bool IsCanUseCore(Point screenPoint,
            AutomationElement rootElement,
            AutomationElement pointElement)
        {

            logger.LogDebug($"Start IsCanUse  Java ");
            try
            {
                if (pointElement.Properties.NativeWindowHandle.IsSupported)
                {
                    var windowHandle = pointElement.Properties.NativeWindowHandle.ValueOrDefault;
                    var isJavaWin = false;
                    if (IsJavaWindowInternal(rootElement))
                    {
                        if (accessBridgeProvider.IsInitilized)
                        {
                            isJavaWin = true;
                        }
                        else
                        {
                            throw new JavaException(eErrorMessageType.Error_JavaExtensionUnInstalled);
                        }
                    }
                    else
                    {
                        ///增加兜底判断
                        if (accessBridgeProvider.IsHasJavaEnv)
                        {
                            isJavaWin = accessBridgeProvider.IsJavaWnd(windowHandle);
                        }
                    }
                    if (isJavaWin)
                    {

                        //根据坐标点获取元素，如果元素是TitleBar 或者 Button win32的形式拾取
                        var element = UiCommon.UIAAutomation.FromPoint(screenPoint);
                        if (element != null
                            && (element.ControlType == FlaUI.Core.Definitions.ControlType.TitleBar
                            || element.ControlType == FlaUI.Core.Definitions.ControlType.Button))
                        {
                            return false;
                        }
                        var handle = element.Properties.NativeWindowHandle.ValueOrDefault;
                        if (handle != null
                            && handle != IntPtr.Zero
                            && !windowHandles.Contains(handle))
                        {
                            windowHandles.Add(handle);
                        }
                        return true;
                    }

                }
            }
            finally
            {
                logger.LogDebug($"End IsCanUse  Java ");
            }

            return false;
        }

        public void ReadyToCapture()
        {
            javaUiThread.Invoke(ReadyToCaptureCore);
        }

        private void ReadyToCaptureCore()
        {
            accessBridgeProvider.EnumJvms(true);//重刷JVM列表
            GlobleConfigurations.JavaCaptureMode = Config.Configurations.Info.JavaCaptureMode;
            accessBridgeProvider.IsHasJavaEnv = true;
        }

        public IGuiNode GetElementFromSelector(GetElementFromSelectorParams args)
        {
            return javaUiThread.Invoke(() => GetElementFromSelectorCore(args));
        }

        private IGuiNode GetElementFromSelectorCore(GetElementFromSelectorParams args)
        {
            logger.LogInformation($"Java 进入GetElementFromSelector");
            ISelectorContext selectorContext = SelectorAdapter.GetSelectorJson(args.Selector);

            JavaNode node = new JavaNode(selectorContext.Selector, selectorContext.Version);
            node.TragetWindow = args.WindowInfo;
            node.SetForeground();
            var nodes = node.FindAll(null, 1, 3000, args.FindOnce);
            logger.LogDebug("GetElementFromSelector End");
            var javaNode = nodes.FirstOrDefault(item =>
            {
                if (args.DiscernHidden)
                {
                    return true;
                }
                var rect = item.BoundingRectangle;
                return rect.Width > 0 && rect.Height > 0;
            }) as JavaNode;
            if (javaNode == null)
            {
                throw new JavaException(eErrorMessageType.Error_NotFoundElement);
            }
            return javaNode;
        }
    
        public string GetElementFromSelectorAll(GetElementFromSelectorParams args)
        {
            return javaUiThread.Invoke(() => GetElementFromSelectorAllCore(args));
        }

        private string GetElementFromSelectorAllCore(GetElementFromSelectorParams args)
        {
            logger.LogInformation($"Java 进入GetElementFromSelectorAll");
            ISelectorContext selectorContext= SelectorAdapter.GetSelectorJson(args.Selector);

            JavaNode node = new JavaNode(selectorContext.Selector, selectorContext.Version);
            node.TragetWindow = args.WindowInfo;
            node.SetForeground();
            var nodes = node.FindAll(null, 1, 3000, args.FindOnce).Where(item =>
            {
                if (args.DiscernHidden)
                {
                    return true;
                }
                var rect = item.BoundingRectangle;
                return rect.Width > 0 && rect.Height > 0;
            }).ToList();
            logger.LogDebug("GetElementFromSelectorAll End");


            bool resultBoundings = args.GetExtendParamValue<string>("resultAttrName") == "bounding";
            if (resultBoundings)
            {
                return nodes.Select(n => n.BoundingRectangle).ToJsonNoneFormating();
            }
            else
            {
                //return CacheData.BulidCacheSelector(nodes, args.Selector).ToJsonNoneFormating();
                return string.Empty;
            }
        }
        public void Release()
        {
            //do nothing
        }
        private bool IsJavaWindowInternal(AutomationElement aRootElement)
        {
            string className = aRootElement.ClassName;
            logger.LogDebug($"IsJavaWindowInternal:{className}");
            switch (className)
            {
                case "SunAwtCanvas":
                case "SunAwtFrame":
                case "SunAwtDialog":
                case "SunAwtWindow":
                    return true;
                default:
                    return false;
            }
        }

        #endregion

        #region 数据采集
        private List<List<string>> CollectTableData(List<Core.Commons.SelectorProps> tableSelector, string selectorVersion = null, WindowObject targetWindow = null)
        {
            GetElementFromSelectorParams @params = new GetElementFromSelectorParams();
            @params.Selector = tableSelector.ToJson();
            //SelectorContext context = new SelectorContext();
            //context.Selector = tableSelector.ToJson();
            //context.Version = "1.0";
            //context.Flag = "NoForeground";
            //var node = JavaNode.GetElementFromSelector(tableSelector.ToJson());

            JavaNode node = new JavaNode(tableSelector.ToJson(), selectorVersion);
            node.TragetWindow = targetWindow;
            node.SetForeground();
            var nodes = node.FindAll(null, 1, 3000, false);
            logger.LogDebug("GetElementFromSelector End");
            var javaNode = nodes.FirstOrDefault() as JavaNode;

            string data = javaNode.GetTableData("");
            return data.ToObject<List<List<string>>>();
        }
        public List<List<string>> CollectDataBySelector(ParamCollectDataBySelectorModel paramModel)
        {
            return javaUiThread.Invoke(() => CollectDataBySelectorCore(paramModel));
        }

        private List<List<string>> CollectDataBySelectorCore(ParamCollectDataBySelectorModel paramModel)
        {
            if (paramModel.IsTable)
            {
                var tableSelector = paramModel.SelectorDataList.FirstOrDefault()?.Selector;
                return CollectTableData(tableSelector);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public List<Core.Commons.SelectorProps> GetSameParentsProps(ParamCollectDataBySelectorModel paramModel)
        {
            throw new NotImplementedException();
        }
        public List<List<string>> CollectData_v2(ParamDataCollect_V2Model paramModel)
        {
            return javaUiThread.Invoke(() => CollectDataV2Core(paramModel));
        }

        private List<List<string>> CollectDataV2Core(ParamDataCollect_V2Model paramModel)
        {
            if (paramModel.IsTable)
            {
                var tableSelector = paramModel.DataCollectConfig.CollectionConfig.TableSelector;
                return CollectTableData(tableSelector);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public List<Core.Commons.SelectorProps> GetSameParentsProps_v2(ParamDataCollect_V2Model paramModel)
        {
            throw new NotImplementedException();
        }
        #endregion
 

        public void ScrollIntoView(string srcoll)
        {
            try
            {
                var scrollParams = srcoll.ToObject<Dictionary<string, object>>();
                if (scrollParams == null) return;
                int scrollTop = scrollParams.ContainsKey("scrollTop") ? Convert.ToInt32(scrollParams["scrollTop"]) : 0;
                int scrollLeft = scrollParams.ContainsKey("scrollLeft") ? Convert.ToInt32(scrollParams["scrollLeft"]) : 0;
                int delay = scrollParams.ContainsKey("ScrollVerticalDelay") ? Convert.ToInt32(scrollParams["ScrollVerticalDelay"]) : 0;

                var handle = NativeMethods.GetForegroundWindow();
                var windowElement = GUI.Commons.UiCommon.UIAAutomation.FromHandle(handle);
                if (windowElement == null) return;
                var windowRect = windowElement.BoundingRectangle;

                var centerX = windowRect.Left + windowRect.Width / 2;
                var centerY = windowRect.Top + windowRect.Height / 2;
                Mouse.Position = new Point(centerX, centerY);
                Thread.Sleep(50);

                if (scrollTop != 0)
                {
                    int steps = Math.Abs(scrollTop);
                    int direction = Math.Sign(scrollTop);
                    for (int i = 0; i < steps; i++)
                    {
                        Mouse.Scroll(direction);
                        Thread.Sleep(delay > 0 ? delay : 30);
                    }
                }

                if (scrollLeft != 0)
                {
                    int steps = Math.Abs(scrollLeft);
                    int direction = Math.Sign(scrollLeft);
                    for (int i = 0; i < steps; i++)
                    {
                        Mouse.HorizontalScroll(direction);
                        Thread.Sleep(delay > 0 ? delay : 30);
                    }
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "Java ScrollIntoView failed");
            }
        }

        public string GetBoundings(string jsonParams)
        {
            throw new NotImplementedException();
        }
        public string BuildSelectors(string jsonParams)
        {
            throw new NotImplementedException();
        }
        public string BuildSelectors(BulidSelectorsParams @params)
        {
            throw new NotImplementedException();
        }
        public string BuildSelectors(ParamBuildSelectorsModel paramModel)
        {
            throw new NotImplementedException();
        }


        #region chatRpa
        public string GetElementsAll(GetElementsAllParams @params)
        {
            throw new NotImplementedException();
        }

        public IGuiNode GetElementById(GetElementByIdParams args)
        {
            throw new NotImplementedException();
        }

        public IGuiNode[] QuerySelectorAll(GetElementFromSelectorParams args)
        {
            return javaUiThread.Invoke(() => QuerySelectorAllCore(args));
        }

        private IGuiNode[] QuerySelectorAllCore(GetElementFromSelectorParams args)
        {
            logger.LogInformation($"Java 进入 QuerySelectorAll");
            ISelectorContext selectorContext = SelectorAdapter.GetSelectorJson(args.Selector);

            JavaNode node = new JavaNode(selectorContext.Selector, selectorContext.Version);           
            node.TragetWindow = args.WindowInfo;
            node.SetForeground();

            var nodes = node.FindAll(null, 1, 10000, args.FindOnce);
           
            var filtered = nodes
                .Where(item =>
                {
                    if (args.DiscernHidden)
                    {
                        return true;
                    }
                    var rect = item.BoundingRectangle;                  
                    return rect.Width > 0 && rect.Height > 0;
                })
                .ToList();

            logger.LogDebug("QuerySelectorAll End");         

            if (filtered.Count == 0)
            {
                throw new JavaException(eErrorMessageType.Error_NotFoundElement);
            }   
            return filtered.ToArray();
        }
        public List<JObject> GetSimilarOrDataCollectElements(GetElementsAllParams @params)
        {
            throw new NotImplementedException();
        }
        #endregion
        public bool VerifyWindowOfNode(GetElementFromSelectorParams args)
        {
            return true;
        }

        public string GetTargetElementInfo(GetTargetElementInfoModelParams @params)
        {
            throw new NotImplementedException();
        }
    }
}
