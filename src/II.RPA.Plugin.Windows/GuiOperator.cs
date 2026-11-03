using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.Plugin.Windows.Config;
using II.RPA.WinAutomation.CommonUtility;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using II.RPA.WinAutomation.CommonUtility.Utils;
using System.Windows.Media;
using Newtonsoft.Json;
using System.Windows.Forms;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.Plugin.Windows.Core;
using II.RPA.Plugin.Windows.OwnerDraw;
using System.Net.NetworkInformation;
using System.Reflection;
using FlaUI.Core.WindowsAPI;
using static InteropMy.UiaCore;
using II.RPA.Plugin.GUI.Commons.WindowMatch;

namespace II.RPA.Plugin.Windows
{
    public class GuiOperator : IGuiOperator_6_9_0, IGuiWindowsOperator
    {
        public int bt_count = 0;
        private static ILogger logger = Logger.OfContext();

        private string curentProcessName { get; set; } = string.Empty;
        private AutomationElement _lastHoverElement;
        private Rectangle _lastHoverElementRect = Rectangle.Empty;
        private IntPtr _lastHoverHandle = IntPtr.Zero;
        private string _lastHoverProcessName = string.Empty;

        public GuiOperator()
        {
        }

        public eGuiType GuiType => eGuiType.Windows;
        public int Sort { get; set; }

        protected SelectorAdapter SelectorAdapter { get; set; } = new SelectorAdapter();

        private static string SafeClassName(AutomationElement element)
        {
            try
            {
                return element?.Properties.ClassName.ValueOrDefault;
            }
            catch
            {
                return null;
            }
        }

        private static Rectangle SafeBoundingRectangle(AutomationElement element)
        {
            try
            {
                if (element != null && element.Properties.BoundingRectangle.TryGetValue(out var rectangle))
                {
                    return rectangle;
                }
            }
            catch
            {
            }
            return Rectangle.Empty;
        }

        private static bool SafeContainsPoint(AutomationElement element, Point point)
        {
            var rectangle = SafeBoundingRectangle(element);
            return !rectangle.IsEmpty && rectangle.Contains(point);
        }

        private static bool IsExplorerProcess(string processName)
        {
            return string.Equals(processName, "explorer", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsReusableHoverRect(Rectangle rectangle)
        {
            if (rectangle.IsEmpty || rectangle.Width <= 0 || rectangle.Height <= 0)
            {
                return false;
            }

            // 大面积容器（例如桌面 List 或窗口 Pane）不能缓存，否则鼠标在容器内移动时会错过更小的目标元素。
            return (long)rectangle.Width * rectangle.Height <= 300000 && rectangle.Height <= 500;
        }

        private bool TryGetCachedHoverElement(Point point, IntPtr handle, string processName, out AutomationElement element)
        {
            element = null;
            if (_lastHoverElement == null || handle == IntPtr.Zero || handle != _lastHoverHandle)
            {
                return false;
            }

            if (!string.Equals(processName, _lastHoverProcessName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!_lastHoverElementRect.Contains(point))
            {
                return false;
            }

            element = _lastHoverElement;
            return true;
        }

        private void CacheHoverElement(IntPtr handle, string processName, AutomationElement element)
        {
            var rectangle = SafeBoundingRectangle(element);
            if (handle == IntPtr.Zero || element == null || !IsReusableHoverRect(rectangle))
            {
                _lastHoverElement = null;
                _lastHoverElementRect = Rectangle.Empty;
                _lastHoverHandle = IntPtr.Zero;
                _lastHoverProcessName = string.Empty;
                return;
            }

            _lastHoverElement = element;
            _lastHoverElementRect = rectangle;
            _lastHoverHandle = handle;
            _lastHoverProcessName = processName ?? string.Empty;
        }

        public int BinaryGetEndIndex(List<AutomationElement> elements, int start, int end, int x)
        {
            bt_count++;
            int mid = (int)Math.Ceiling((start + end) / 2.0);

            if (start > end)
            {
                return end;
            }
            else
            {
                //Console.WriteLine(elements[mid]);//监测查找了哪些X
                //int mx = elements[mid].BoundingRectangle.X;
                if (SafeBoundingRectangle(elements[mid]).X < x)
                {
                    return BinaryGetEndIndex(elements, mid + 1, end, x);
                }
                else if (SafeBoundingRectangle(elements[mid]).X > x)
                {
                    return BinaryGetEndIndex(elements, start, mid - 1, x);
                }
                else
                {
                    int len = elements.Count();
                    while (mid < len - 1 && SafeBoundingRectangle(elements[mid + 1]).X == x)
                    {
                        mid++;
                    }
                    return mid;
                }
            }
        }

        /// <summary>
        /// 排除集合中的外围元素，即排除那些在集合中 覆盖了 集合中其他元素的元素
        /// </summary>
        /// <param name="elements"></param>
        public void FilterSurround(List<AutomationElement> elements)
        {
            bt_count = 0;
            //针对一些特殊软件 中的列表项，坐标和大小都相同，
            var firstRectangle = SafeBoundingRectangle(elements[0]);
            var lastRectangle = SafeBoundingRectangle(elements[elements.Count - 1]);
            if (firstRectangle.X == lastRectangle.X
                        && firstRectangle.Y == lastRectangle.Y
                        && firstRectangle.Width == lastRectangle.Width
                        && firstRectangle.Height == lastRectangle.Height
                        )
            {
                elements.RemoveRange(1, elements.Count - 1);
                return;
            }
            for (int i = 0; i < elements.Count - 1; i++)
            {

                ////根据X判断 那些元素 x轴方向包含在 i 元素中的,元素数量很多时 才有意义，一般直接使用上边循环即可
                var itemRectangle = SafeBoundingRectangle(elements[i]);
                int end = BinaryGetEndIndex(elements, i, elements.Count - 1, itemRectangle.X + itemRectangle.Width);
                for (int j = i + 1; j <= end; j++)
                {
                    if (itemRectangle.Contains(SafeBoundingRectangle(elements[j])))
                    {
                        elements.RemoveAt(i);
                        i--;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 根据指定选择器参数，转换实际命中元素
        /// </summary>
        /// <param name="param">指定参数</param>
        /// <returns>命中元素</returns>
        public IGuiNode GetElementFromSelector(GetElementFromSelectorParams param)
        {
            IGuiNode foundNode = null;
            //转换json格式，由于存在新老格式，所以存在转换
            ISelectorContext context = this.SelectorAdapter.GetSelectorJson(param);
            // Owner-draw selectors are resolved against live Bridge state on every
            // replay.  A Similar/Sub cache hit would otherwise bypass strict leaf
            // decoding and could return a stale Cell or even its host Pane/Grid.
            if (RequiresLiveOwnerDrawResolution(context?.Selector))
            {
                logger.LogInformation("Owner-draw Selector 跳过 Similar/Sub 缓存并执行实时匹配");
                // The adapter already rejects an unavailable/empty Cell. Avoid the
                // generic hidden-element filter here because it would call the
                // virtual BoundingRectangle a second time after a successful
                // Resolve and turn a transient miss into an exception instead of
                // the required empty result.
                return FindNodes(context, false, false, param.WindowInfo)
                    .FirstOrDefault();
            }
            //循环相似元素
            if (!string.IsNullOrEmpty(context.SubKey))
            {
                bool filterHidden = context.GetFilterHidden(false);
                foundNode = CacheData.GetCacheElement(context.SubKey, context.SubIndex, () =>
                {
                    var parentNode = FetchElementMatchOrCached(context, param);
                    CacheData.GuiNodeCacahe[context.SubKey] = parentNode == null ? new IGuiNode[0] : parentNode.Children.ToArray();
                });
            }
            else
            {
                foundNode = FetchElementMatchOrCached(context, param);
            }
            return foundNode;
        }

        internal static bool RequiresLiveOwnerDrawResolution(string selector)
        {
            return OwnerDrawSelectorEnvelope.TrySplit(selector).State
                != OwnerDrawProviderState.NotApplicable;
        }

        private IGuiNode FetchElementMatchOrCached(ISelectorContext context, GetElementFromSelectorParams param)
        {
            IGuiNode foundNode = null;
            if (!string.IsNullOrEmpty(context.SimilarKey))
            {
                bool filterHidden = context.GetFilterHidden(false);
                foundNode = CacheData.GetCacheElement(context.SimilarKey, context.SimilarIndex, () =>
                {
                    var foundNodes = FindNodes(context, false, filterHidden, param.WindowInfo);
                    CacheData.GuiNodeCacahe[context.SimilarKey] = foundNodes.ToArray();
                });
            }
            else
            {
                bool filterHidden = context.GetFilterHidden(true);
                logger.LogInformation($"Win32 进入GetElementFromSelector");
                foundNode = FindNodes(context, false, filterHidden, param.WindowInfo).FirstOrDefault();
            }
            return foundNode;
        }
        public string GetElementFromSelectorAll(GetElementFromSelectorParams args)
        {
            logger.LogDebug($"start  GetElementFromSelectorAll");
            //转换json格式，由于存在新老格式，所以存在转换
            ISelectorContext context = this.SelectorAdapter.GetSelectorJson(args);
            bool filterHidden = context.GetFilterHidden(false);
            var foundNodes = FindNodes(context, false, filterHidden, args.WindowInfo);
            logger.LogDebug($" GetElementFromSelectorAll foundNodes");

            bool resultBoundings = args.GetExtendParamValue<string>("resultAttrName") == "bounding";
            if (resultBoundings)
            {
                return foundNodes.Select(n => n.BoundingRectangle).ToJsonNoneFormating();
            }
            else
            {
                return CacheData.BulidCacheSimilarSelector(foundNodes, args.Selector).ToJsonNoneFormating();
            }

            #region 【注释】生成所有元素selector  如果在生成时不考虑index规则，可以直接使用，
            //CacheData.NodeSelectorPropsCatch?.Clear();
            //CacheData.IsCanUpdateNodeSelectorPropsCatch = false;
            //List<object> selectorList = new List<object>();
            //foreach (var node in foundNodes)
            //{
            //    if (resultBoundings)
            //    {
            //        selectorList.Add(node.BoundingRectangle);
            //    }
            //    else
            //    {
            //        ((UIANode)node).ContainProcessInfoSelecor = false;
            //        var selector = ExtractViewData.AddCommonProps(
            //                            ((UIANode)node).BuildSelector(),
            //                            this.GuiType.ToString()
            //                        );

            //        selectorList.Add(selector);
            //    }
            //}
            //CacheData.NodeSelectorPropsCatch?.Clear();
            //CacheData.IsCanUpdateNodeSelectorPropsCatch = false;
            //NLogger.Default.Trace($"end  GetElementFromSelectorAll");
            //return selectorList.ToJsonNoneFormating();
            #endregion
        }

        private List<IGuiNode> FindNodes(ISelectorContext context, bool useCache, bool filterHidden, WindowObject tragetWindow)
        {
            return FindNodes(context, automationElement: null, useCache, filterHidden, tragetWindow);
        }
        private List<IGuiNode> FindNodes(ISelectorContext context, AutomationElement automationElement, bool useCache, bool filterHidden, WindowObject tragetWindow)
        {
            logger.LogDebug("strt windwos FindNodes");
            logger.LogDebug("Selector:" + context.Selector);
            UIANode node = new UIANode(context);
            if (automationElement != null)
            {
                node.automationElement = automationElement;
            }
            var rootSelectorItem = node.RootSelectorItem;

            string processName = string.Empty;
            //判断是否是微信程序，需要特殊处理
            var pcSelectItemProp = rootSelectorItem?.FindAccordingName(ConstFields.PROP_NAME_PROCESS_NAME).FirstOrDefault();
            if (pcSelectItemProp != null)
            {
                processName = pcSelectItemProp.Value;
                WeChatDataContext.IsWeChatWorkingProcess = this.PendingWeChatWorking(pcSelectItemProp);
            }

            logger.LogDebug($"{ConstFields.PROP_NAME_PROCESS_NAME}:{processName}");
            //设置要使用的UIA模式
            UiCommon.SetUIAMode(WinConfigurations.Info.Mode2Apps, processName);
            if (tragetWindow != null)
            {
                node.RootWindow = tragetWindow;
            }
            this.DoForegroundPrework(node, rootSelectorItem, context.Flag);
            var findNodes = node.FindAll(useCache: useCache);
            logger.LogDebug($"共匹配到 {findNodes?.Count}个元素");

            // TMS 虚拟面板回放：从 Selector 末级提取 tms-bounds 并注入到已匹配的 UIANode
            try
            {
                var ctxSelectors = context.Selector?.ToObject<List<SelectorProps>>();
                if (ctxSelectors?.Count > 0)
                {
                    var leafProps = ctxSelectors[ctxSelectors.Count - 1];
                    var tmsProp = leafProps.Props?.FirstOrDefault(
                        p => p.Name == TmsPanelHelper.PROP_TMS_BOUNDS);
                    if (tmsProp != null)
                    {
                        var bounds = TmsPanelHelper.ParseTmsBounds(tmsProp.Value);
                        if (bounds.HasValue && findNodes?.Count > 0)
                        {
                            foreach (var n in findNodes)
                            {
                                if (n is UIANode un) un._tmsVirtualBounds = bounds.Value;
                            }
                            logger.LogDebug($"TMS bounds propagated: {bounds.Value}");
                        }
                    }
                }
            }
            catch (Exception tmsEx)
            {
                logger.LogDebug($"TMS bounds propagation skipped: {tmsEx.Message}");
            }

            try
            {
                if (filterHidden)
                {
                    var validNodes = findNodes.Where(x => x.BoundingRectangle.Height > 0 && x.BoundingRectangle.Width > 0).ToList();
                    logger.LogDebug($"未隐藏元素数量： {validNodes.Count()} 个！");
                    if (validNodes != null && validNodes.Any())
                    {
                        return validNodes;
                    }
                    //1.3版本之前的逻辑，如果过滤隐藏后无元素，则最终返回findNodes，此处做逻辑兼容处理，1.3及之后版本，直接返回过滤后的结果
                    else if (double.TryParse(context.Version, out var version) && version >= 1.3)
                    {
                        return validNodes;
                    }
                }
            }
            catch
            {
                logger.LogDebug($"过滤元素时发生异常");
                throw;
            }

            return findNodes;

        }


        const int CHILDID_SELF = 0;
        enum OBJID : uint
        {
            OBJID_WINDOW = 0x00000000,
        }
        [DllImport("oleacc.dll")]
        static extern int AccessibleObjectFromWindow(IntPtr hwnd, uint id, ref Guid iid, [In, Out, MarshalAs(UnmanagedType.IUnknown)] ref object ppvObject);
        [DllImport("Oleacc.dll")]
        static extern int AccessibleObjectFromPoint(Point ptScreen, [MarshalAs(UnmanagedType.Interface)] out object ppacc, out object pvarChild);

        AutomationElement TryToConvertElementFromAccToUiaByRuntimeId(AutomationElement rootElement, IAccessibleInternal acc)
        {
            AutomationElement result = null;
            try
            {
                IServiceProvider sp = (IServiceProvider)acc;
                if (sp != null)
                {
                    IAccessibleEx acc_ex;
                    {
                        object obj_acc_ex;
                        int hr = sp.QueryService(typeof(IAccessibleEx).GUID, typeof(IAccessibleEx).GUID, out obj_acc_ex);
                        acc_ex = obj_acc_ex as IAccessibleEx;
                    }
                    if (acc_ex != null)
                    {
                        var runtimeId = acc_ex.GetRuntimeId();
                        result = rootElement.FindFirst(TreeScope.Descendants, new PropertyCondition(rootElement.Automation.PropertyLibrary.Element.RuntimeId, runtimeId));
                        //System.Diagnostics.Debug.Write($" acc by RuntimeId,");
                    }
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("an error occurs: {0}", e);
            }
            return result;
        }

        /// <summary>
        /// 拾取节点
        /// </summary>
        /// <param name="point"></param>
        /// <param name="rootElement"></param>
        /// <param name="pointElement"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        public bool InspectNode(Point point, AutomationElement rootElement, AutomationElement pointElement, string selector, out IGuiNode node)
        {
            node = null;
            try
            {
                logger.LogDebug($"InspectNode FromPoint");
                var processName = string.Empty;
                IntPtr handle = IntPtr.Zero;
                if (pointElement.Properties.NativeWindowHandle.TryGetValue(out handle))
                {
                    processName = UiCommon.GetProcessNameByCache(handle) ?? string.Empty;
                    UiCommon.SetAutoSeekDepth(WinConfigurations.Info.AutoSeekDepthApps, processName.ToLower());
                }
                AutomationElement element;
                if (IsExplorerProcess(processName) && TryGetCachedHoverElement(point, handle, processName, out element))
                {
                    logger.LogDebug("InspectNode FromPoint skipped by explorer hover cache");
                }
                else
                {
                    element = UiCommon.UIAAutomation.FromPoint(point);
                }
                bool isScanTree = WinConfigurations.Info.ScanElementTreeApps.Contains(processName, StringComparer.OrdinalIgnoreCase);
                // SAP 进程及其 owned 弹窗（#32770）的 UIA 树结构特殊，GroupBox 内控件是 sibling 而非 child，
                // 且 MSAA FromPoint 命中不稳定，必须走全树扫描路径才能准确定位到 CheckBox
                var rootClassName = SafeClassName(rootElement);
                if (!isScanTree && !string.IsNullOrEmpty(rootClassName) && rootClassName.IndexOf("SAP", StringComparison.OrdinalIgnoreCase) >= 0)
                    isScanTree = true;

                /*************** 千牛客户端判断添加，目前只有该场景*************/
                if (element != null)
                {
                    /*规避:在直接根据点拾取元素时，会得到千牛应用下一个会产生干扰的一级元素，这个元素覆盖了所有的元素
                      但是根据句柄获取到的元素（rootNode）是正确的，通过判断是否覆盖来判断 element的准确性
                      */
                    Rectangle pointElementRectangle = SafeBoundingRectangle(pointElement);
                    Rectangle elementRectangle = SafeBoundingRectangle(element);
                    if (!pointElementRectangle.IsEmpty && !elementRectangle.IsEmpty &&
                        pointElementRectangle.Left <= elementRectangle.Left && pointElementRectangle.Right <= elementRectangle.Right &&
                        pointElementRectangle.Top <= elementRectangle.Top && pointElementRectangle.Bottom <= elementRectangle.Bottom)
                    {
                        element = pointElement;
                    }
                }
                /*************** 为防止 上述比较 得到的元素内部还有更符合 鼠标点 的更加精准的子元素或同级元素 *************/
                List<AutomationElement> scanElementList = null;
                //企微等激活acc后 使用UIA拾取的元素，深度匹配从窗口开始匹配，
                if (WinConfigurations.Info.ScanElementTreeApps.Contains(processName, StringComparer.OrdinalIgnoreCase))
                {
                    WindowElement windowElement = new WindowElement(pointElement);
                    scanElementList = windowElement.FromPointByScanElementTree(point);
                }
                else if (IsExplorerProcess(processName))
                {
                    logger.LogDebug("InspectNode skip ScanSmallerElement for explorer");
                }
                else
                {
                    scanElementList = ScanSmallerElement(point, element, 0);
                }
                if (scanElementList?.Count > 0)
                {
                    //Console.WriteLine("疑似元素数量：" + scanElementList.Count());
                    scanElementList = scanElementList.OrderBy(e => SafeBoundingRectangle(e).X).ThenByDescending(e => SafeBoundingRectangle(e).Width).ToList();
                    //为了解决类似QQ登录窗口的拾取问题（需要UIA2）
                    FilterSurround(scanElementList);
                    if (scanElementList.Count > 1)
                    {
                        element = scanElementList.OrderBy(e =>
                        {
                            var rect = SafeBoundingRectangle(e);
                            return rect.Width * rect.Height;
                        }).First();
                    }
                    else
                    {
                        element = scanElementList.First();
                    }
                }
                logger.LogDebug($"InspectNode Scan Element End");
                /**************************************************************/
                if (element == null)
                {
                    element = pointElement ?? rootElement;
                }
                if (element == null)
                {
                    return false;
                }
                if (IsExplorerProcess(processName))
                {
                    CacheHoverElement(handle, processName, element);
                }

                bool can_use_acc_interface = true;
                string[] processNames = { "Kingdee.BOS.XPF.App" };
                bool contains = processNames.Any(s => s.Equals(processName, StringComparison.OrdinalIgnoreCase));
                if (contains || !WinConfigurations.Info.CaptureByAcc || IsExplorerProcess(processName))
                {
                    can_use_acc_interface = false;
                }
                string frameworkId = null;
                bool is_chromium = false;
                if (can_use_acc_interface)
                {
                    if (pointElement != null)
                    {
                        frameworkId = pointElement.Properties.FrameworkId.ValueOrDefault;
                        if (frameworkId == "Chrome")
                        {
                            is_chromium = true;
                        }
                        else
                        {
                            string className = SafeClassName(pointElement);
                            if (className == "Chrome_RenderWidgetHostHWND")
                            {
                                is_chromium = true;
                            }
                            else
                            {
                                //*星号段前的筛选代码有问题，在CEF/Chromium 55时，element会指向类名为"Intermediate D3D Window"的窗口，
                                //且CEF/Chromium 43时，className会出现等于null的情况，因此我们需要再次验证下
                                if (handle != IntPtr.Zero)
                                {
                                    var win32ClassName = new StringBuilder(256);
                                    User32.GetClassName(handle, win32ClassName, 256);
                                    if (win32ClassName.ToString() == "Chrome_RenderWidgetHostHWND")
                                    {
                                        is_chromium = true;
                                        //现在这个逻辑应该不会触发到了吧
                                        System.Diagnostics.Debug.WriteLine("通过窗口句柄来获取窗口类名了！！！");
                                    }
                                }
                            }
                        }
                        if (is_chromium)
                        {
                            if (pointElement.Patterns.LegacyIAccessible.IsSupported)
                            {
                                var acc_test = pointElement.Patterns.LegacyIAccessible.Pattern.GetIAccessible();
                                if (acc_test == null)
                                {
                                    //https://learn.microsoft.com/en-us/windows/win32/api/uiautomationclient/nf-uiautomationclient-iuiautomationlegacyiaccessiblepattern-getiaccessible
                                    //由上面链接的内容可知无法转换成IAccessible的UIA对象要么是OLEACC.dll实现的代理对象,或者来自UIA-to-MSAA桥，
                                    //因此这种情况下代表Chromium渲染窗口的pointElement肯定是来自原生UIA Provider实现的，
                                    //而这时如果强行通过ACC来取元素的话，向上递归时会出现缺一部分节点的情况，因此这个时候我们必须走UIA的方式来拾取
                                    can_use_acc_interface = false;
                                }
                                else
                                {
                                    //System.Diagnostics.Debug.Assert(acc_test == null);
                                }
                            }
                        }
                    }
                }
                UIANode uianode_by_acc = null;

                //System.Diagnostics.Debug.Write($"can_use_acc_interface={can_use_acc_interface}");
                if (can_use_acc_interface)
                {
                    //目前代码有点丑，赶时间先这样了
                    AutomationElement element_by_acc = null;
                    IAccessibleInternal acc = null;
                    object varChild = null;

                    int xLeft = 0, yTop = 0, cxWidth = 0, cyHeight = 0;
                    IAccessibleInternal accPoint_By_accHitTest = null;
                    object varChild_By_accHitTest = null;
                    if (handle != IntPtr.Zero)
                    {
                        object objWnd = null;
                        var guid = typeof(IAccessibleInternal).GUID;
                        AccessibleObjectFromWindow(handle, (uint)OBJID.OBJID_WINDOW, ref guid, ref objWnd);
                        if (objWnd != null)
                        {
                            IAccessibleInternal accWnd = objWnd as IAccessibleInternal;
                            if (accWnd != null)
                            {
                                AccEnabler.CheckForFindBetterHitTestedDescendant(handle, accWnd, point, out accPoint_By_accHitTest, out varChild_By_accHitTest);
                                if (accPoint_By_accHitTest != null)
                                {
                                    accPoint_By_accHitTest.accLocation(out xLeft, out yTop, out cxWidth, out cyHeight, varChild_By_accHitTest);
                                }
                            }
                        }
                    }

                    acc = accPoint_By_accHitTest;
                    varChild = varChild_By_accHitTest;

                    if (acc != null)
                    {
                        if (frameworkId == "WPF")
                        {
                            element_by_acc = TryToConvertElementFromAccToUiaByRuntimeId(rootElement, acc);
                        }
                        else
                        {
                            try
                            {
                                var accSTD = acc as Interop.UIAutomationClient.IAccessible;
                                if (accSTD != null && varChild is int child_id)
                                {
                                    var role = accSTD.accRole[CHILDID_SELF];
                                    bool is_system_client = role is int nRole && nRole == (int)AccessibilityRole.ROLE_SYSTEM_CLIENT;
                                    if (!is_system_client)
                                    {
                                        FlaUI.UIA3.UIA3Automation uia3 = (FlaUI.UIA3.UIA3Automation)rootElement.Automation;
                                        if (uia3 != null)
                                        {
                                            var result = uia3.NativeAutomation.ElementFromIAccessible(accSTD, child_id);
                                            if (result != null)
                                            {
                                                element_by_acc = uia3.WrapNativeElement(result);
                                                //System.Diagnostics.Debug.Write($" acc by ElementFromIAccessible,");
                                            }
                                        }
                                    }
                                }
                            }
                            catch (System.ArgumentException ea)
                            {
                                //UIA-to-MSAA bridge来的ACC元素不支持通过ElementFromIAccessible转换回UIA元素，所以我们需要换其他思路，于是经过奇思妙想我们有了下面的代码，效率也不算差
                                element_by_acc = TryToConvertElementFromAccToUiaByRuntimeId(rootElement, acc);
                            }
                            catch (Exception e)
                            {
                                System.Diagnostics.Debug.WriteLine("an error occurs: {0}", e);
                            }
                        }
                    }

                    if (element_by_acc != null)
                    {
                        Rectangle acc_rect;
                        if (is_chromium && element_by_acc.IsOffscreen)
                        {
                            var f = NativeMethods.DynamicScale;
                            acc_rect = new Rectangle((int)(xLeft * f), (int)(yTop * f), (int)(cxWidth * f), (int)(cyHeight * f));
                        }
                        else
                        {
                            acc_rect = SafeBoundingRectangle(element_by_acc);
                        }
                        var uia_rect = SafeBoundingRectangle(element);
                        if (!acc_rect.IsEmpty)
                        {
                            if (acc_rect.Width * acc_rect.Height < uia_rect.Width * uia_rect.Height)
                            {
                                //element = element_by_acc;
                                //System.Diagnostics.Debug.Write($" replace element by acc");
                                uianode_by_acc = new UIANode(element_by_acc);
                                uianode_by_acc.automationElement_uia_bak = element;
                                logger.LogDebug($"本次拾取优先使用uianode_by_acc");
                            }
                        }
                    }
                }
                logger.LogDebug($"双引擎-acc拾取结束");

                //修正调试时拾取元素的边界框会不断变大的问题
                if (element.Properties.ProcessId.IsSupported)
                {
                    var processId = element.Properties.ProcessId.Value;
                    if (processId == Process.GetCurrentProcess().Id)
                    {
                        if (element.Properties.NativeWindowHandle.IsSupported)
                        {
                            var hwnd = element.Properties.NativeWindowHandle.Value;
                            uint extendedStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
                            if ((extendedStyle & NativeMethods.WS_EX_TRANSPARENT) == 0)
                            {
                                //System.Diagnostics.Debug.Print($"extendedStyle={extendedStyle}");
                                NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, extendedStyle | NativeMethods.WS_EX_LAYERED | NativeMethods.WS_EX_TRANSPARENT);
                            }
                        }
                    }
                }

                node = uianode_by_acc;
                if (node == null)
                {
                    node = new UIANode(element);
                    logger.LogDebug($"本次拾取优先使用uianode_by_uia");
                }

                // Owner-draw enhancement is deliberately fail-open while picking:
                // the normal UIA/MSAA node already exists and remains usable if a
                // Bridge is disabled, unavailable, or cannot identify this point.
                UIANode fallbackNode = (UIANode)node;
                fallbackNode._selector = selector;
                try
                {
                    IntPtr candidateHwnd = fallbackNode.WindowHandle != IntPtr.Zero
                        ? fallbackNode.WindowHandle : handle;
                    OwnerDrawResult<OwnerDrawElementSnapshot> ownerDrawPick
                        = OwnerDrawAdapterRegistry.Shared.TryPick(point, candidateHwnd);
                    if (ownerDrawPick.IsSuccess && ownerDrawPick.Value != null
                        && ownerDrawPick.Value.HostHwnd != IntPtr.Zero)
                    {
                        AutomationElement hostElement = UiCommon.UIAAutomation.FromHandle(
                            ownerDrawPick.Value.HostHwnd);
                        if (hostElement != null)
                        {
                            var hostNode = new UIANode(hostElement)
                            {
                                RootWindow = fallbackNode.RootWindow,
                            };
                            // Preserve edited host properties only when the normal
                            // picker and Bridge identify the exact same UIA element.
                            // A Bridge may drill down from an outer Pane to the real
                            // Grid HWND; reusing that outer Selector would make the
                            // virtual Cell replay against the wrong host.
                            if (IsExactOwnerDrawHost(fallbackNode, hostElement,
                                ownerDrawPick.Value.HostHwnd))
                            {
                                hostNode._selector = selector;
                            }
                            node = new OwnerDrawVirtualElementNode(hostNode,
                                ownerDrawPick.Value);
                            logger.LogDebug("Owner-draw pick enhanced adapter={Adapter} "
                                + "host=0x{Host:X}.", ownerDrawPick.Value.AdapterId,
                                ownerDrawPick.Value.HostHwnd.ToInt64());
                        }
                    }
                    else if (ownerDrawPick.State
                        == OwnerDrawProviderState.RecognizedButFailed)
                    {
                        logger.LogDebug("Owner-draw enhancement failed open: {Status}",
                            ownerDrawPick.Status);
                    }
                }
                catch (Exception ownerDrawEx)
                {
                    node = fallbackNode;
                    Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ownerDrawEx,
                        "Owner-draw enhancement failed open; retaining UIA/MSAA node.");
                }

                // TMS 虚拟面板检测：为 TJdlsPanel 等窗口无关绘制控件计算虚拟菜单项边界
                try
                {
                    if (!IsExplorerProcess(processName)
                        && !(node is OwnerDrawVirtualElementNode))
                    {
                        string clsName = SafeClassName(element) ?? "";
                        if (TmsPanelHelper.IsTmsPanel(clsName)
                            && element.Properties.NativeWindowHandle.TryGetValue(out var tmsHwnd)
                            && tmsHwnd != IntPtr.Zero)
                        {
                            var virtBounds = TmsPanelHelper.ComputeVirtualItem(tmsHwnd, point.X, point.Y);
                            if (virtBounds.HasValue)
                            {
                                ((UIANode)node)._tmsVirtualBounds = virtBounds.Value;
                                logger.LogDebug($"TMS virtual item: row computed, bounds={virtBounds.Value}");
                            }
                        }
                    }
                }
                catch (Exception tmsEx)
                {
                    logger.LogDebug($"TMS detection skipped: {tmsEx.Message}");
                }
                ((UIANode)node)._selector = selector;
                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                //logger.LogDebug($"{ex}");
                throw new WindowsException(eErrorMessageType.Error_CaptureUnauthorizedAccessError, eComponentType.Capture);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"拾取windows元素发生异常");
                return false;
            }
            finally
            {

            }
        }

        private static bool IsExactOwnerDrawHost(UIANode candidate,
            AutomationElement hostElement, IntPtr hostHwnd)
        {
            if (candidate == null || candidate.automationElement == null
                || hostElement == null)
            {
                return false;
            }
            try
            {
                return CanReuseOwnerDrawHostSelector(candidate.WindowHandle,
                    candidate.automationElement.Properties.RuntimeId.ValueOrDefault,
                    hostHwnd,
                    hostElement.Properties.RuntimeId.ValueOrDefault);
            }
            catch
            {
                return false;
            }
        }

        internal static bool CanReuseOwnerDrawHostSelector(IntPtr candidateHwnd,
            int[] candidateRuntimeId, IntPtr hostHwnd, int[] hostRuntimeId)
        {
            return candidateHwnd != IntPtr.Zero && candidateHwnd == hostHwnd
                && candidateRuntimeId != null && candidateRuntimeId.Length > 0
                && hostRuntimeId != null && hostRuntimeId.Length > 0
                && candidateRuntimeId.SequenceEqual(hostRuntimeId);
        }

        public bool InspectNodeFromElement(Point screenPoint, AutomationElement rootNode, out IGuiNode inspectNode)
        {
            //chrome  alert 等元素拾取逻辑
            try
            {
                if (!SafeContainsPoint(rootNode, screenPoint))
                {
                    inspectNode = null;
                    return true;
                }

                var ele = FindElementByPoint(rootNode, screenPoint);
                if (ele != null)
                {
                    inspectNode = new UIANode(ele);
                    return true;
                }
                else
                {
                    var element = UiCommon.UIAAutomation.FromPoint(screenPoint);
                    inspectNode = new UIANode(element);
                    return true;
                }
            }
            catch (Exception)
            {
            }
            inspectNode = null;
            return false;
        }

        public bool IsCanUse(Point screenPoint, AutomationElement rootElement, AutomationElement pointElement)
        {
            logger.LogDebug($"Start IsCanUse  Windows ");
            SetUIAMode(screenPoint, rootElement, out var processName);
            //logger.LogDebug($"IsCanUse processName:{processName},curentProcessName:{curentProcessName}");
            GUI.Commons.AccessibleObject.EnableAccessibility(pointElement);
            AccEnabler.EnableOnce(pointElement);
            if (processName.ToLower() != this.curentProcessName)
            {
                this.curentProcessName = processName.ToLower();
                UiCommon.SetAutoSeekDepth(WinConfigurations.Info.AutoSeekDepthApps, processName.ToLower());
            }
            logger.LogDebug($"End IsCanUse  Windows ");
            //Windows做为最后使用的拾取方式，永远返回 True
            return true;
        }

        public void ReadyToCapture()
        {
            if (CacheData.RootWndNodeProcessPropsCatch != null)
            {
                CacheData.RootWndNodeProcessPropsCatch.Clear();
            }
            this.curentProcessName = string.Empty;
        }

        public void Release()
        {

        }

        public static bool NeedSeekDepth(AutomationElement element)
        {
            if (UiCommon.IsAutoSeekDepth)
            {
                return true;
            }
            if (element != null)
            {
                if (AccEnabler.IsNeedSeekDepthChromiumApp(element))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 扫描鼠标点下存在可能比较小的元素
        /// </summary>
        /// <param name="point"></param>
        /// <param name="parentElement"></param>
        /// <param name="scanLevel"></param>
        /// <returns></returns>
        public List<AutomationElement> ScanSmallerElement(Point point, AutomationElement parentElement, int scanLevel)
        {
            //if (WinConfigurations.Info.SeekDepth <= 0 || scanLevel >= WinConfigurations.Info.SeekDepth)
            //{
            //    return null;
            //}
            if (parentElement == null)
            {
                return null;
            }
            bool IsAutoSeekDepth = NeedSeekDepth(parentElement);
            if (!IsAutoSeekDepth && WinConfigurations.Info.SeekDepth <= 0)
            {
                return null;
            }
            if (!IsAutoSeekDepth && scanLevel > WinConfigurations.Info.SeekDepth)
            {
                return null;
            }

            List<AutomationElement> eleChildrens = new List<AutomationElement>();
            //获取所有符合 坐标点 的 子元素
            var ele = UiCommon.TreeWalker.GetFirstChild(parentElement);
            while (ele != null)
            {
                if (SafeContainsPoint(ele, point))
                {
                    eleChildrens.Add(ele);
                }
                ele = UiCommon.TreeWalker.GetNextSibling(ele);
            }
            List<AutomationElement> eleList = null;

            if (eleChildrens.Count == 0)//没有子元素，判断是否有合适的同级元素
            {
                // 当 parentElement 自身没有子元素时（如 MFC GroupBox 的复选框是 sibling 而非 child），
                // 搜索同级元素，并把找到的同级元素直接作为最终候选结果，不做进一步递归。
                bool hasNoChildren = (UiCommon.TreeWalker.GetFirstChild(parentElement) == null);
                if (hasNoChildren)
                {
                    var parent = UiCommon.TreeWalker.GetParent(parentElement);
                    if (parent != null && parent.Properties.ProcessId.ValueOrDefault == parentElement.Properties.ProcessId.ValueOrDefault)
                    {
                        var siblings = parent.FindAllChildren().Where(e => !e.Equals(parentElement) && SafeContainsPoint(e, point)).ToList();
                        if (siblings.Count > 0)
                        {
                            eleList = siblings;
                            return eleList;
                        }
                    }
                }
            }
            if (eleChildrens.Count > 0)
            {
                var smallerElements = from c in eleChildrens
                                      where SafeContainsPoint(c, point) //&& parentElement.BoundingRectangle.Contains(c.BoundingRectangle)//有可能存在大小小大小小
                                      orderby SafeBoundingRectangle(c).X ascending, SafeBoundingRectangle(c).Y ascending
                                      select c;
                if (smallerElements.Count() > 0)
                {
                    eleList = new List<AutomationElement>();
                    foreach (var item in smallerElements)
                    {
                        item.ScanLevel = scanLevel;
                        var scanChildenList = ScanSmallerElement(point, item, scanLevel + 1);
                        if (scanChildenList?.Count() > 0)
                        {
                            eleList.AddRange(scanChildenList);
                        }
                        else
                        {
                            eleList.Add(item);
                        }
                    }
                }
            }
            return eleList;
        }

        /// <summary>
        /// 根据根窗口 获取应用进程，根据进程名设置UIA 模式
        /// </summary>
        /// <param name="rootElement"></param>
        internal void SetUIAMode(Point screenPoint, AutomationElement rootElement, out string processName)
        {
            processName = "";
            if (rootElement == null)
            {
                return;
            }
            try
            {
                IntPtr hWnd = rootElement.Properties.NativeWindowHandle.Value;
                if (hWnd != IntPtr.Zero)
                {
                    processName = UiCommon.GetProcessNameByCache(hWnd);

                    UiCommon.SetUIAMode(WinConfigurations.Info.Mode2Apps, processName);
                }
            }
            catch
            {

            }
        }

        /// <summary>
        /// 特殊逻辑分支，主要用于处理是否激活并前置窗口
        /// </summary>
        /// <param name="node">当前主节点</param>
        /// <param name="rootSelectorItem">根节点</param>
        private void DoForegroundPrework(UIANode node, WindowsSelectorItem rootSelectorItem, string flagVal)
        {
            bool isNeedSetForeground = true;
            var selectors = node.OfType<WindowsSelectorItem>().ToList();

            logger.LogDebug($"SelectorItemList.Count:{selectors.Count}");

            //如果是桌面元素先进行回到桌面操作
            var rootClassProp = rootSelectorItem?.FindAccordingName(ConstFields.SELECTOR_CLASS_NAME).FirstOrDefault();
            if (rootClassProp != null)
            {
                try
                {
                    string className = rootClassProp.Value;
                    if (className == "Progman|WorkerW")
                    {
                        logger.LogInformation($"桌面元素需先切换到桌面");
                        Type shellType = Type.GetTypeFromProgID("shell.application");
                        object shell = Activator.CreateInstance(shellType);
                        shellType.InvokeMember("ToggleDesktop", BindingFlags.InvokeMethod, null, shell, new object[] { });
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"切换到桌面异常", ex);
                }
            }
            //Step1 主要针对Menus
            //针对一些特殊类型的窗口 设置 “不前置”
            var firstSelector = selectors.FirstOrDefault();
            if (firstSelector != null)
            {
                if (firstSelector.ElementName.Equals(ConstFields.PROP_VALUE_CONTROL_TYPE_MENU))
                {
                    isNeedSetForeground = false;
                }
            }

            if (!string.IsNullOrEmpty(flagVal))
            {
                if (flagVal.Contains("NoForeground"))
                {
                    isNeedSetForeground = false;
                }
                else if (flagVal.Contains("Foreground"))
                {
                    isNeedSetForeground = true;
                }
            }

            logger.LogDebug($"SetForeground:{isNeedSetForeground}");
            if (isNeedSetForeground)
            {
                node.SetForeground();
            }
        }

        private AutomationElement FindElementByPoint(AutomationElement parent, Point point)
        {
            AutomationElement element = null;
            var childs = parent.FindAllChildren();
            //var childs = UIAExcuteTask.Send(() =>
            //{
            //    return parent.FindAllChildren();
            //});
            if (childs.Length > 0)
            {
                var elements = childs.OrderBy(e =>
                {
                    var rect = SafeBoundingRectangle(e);
                    return rect.Width * rect.Height;
                }).ToList();
                foreach (var child in elements)
                {
                    if (SafeContainsPoint(child, point))
                    {
                        var ele = FindElementByPoint(child, point);
                        if (ele == null)
                            element = child;
                        else
                            return ele;
                    }
                }
            }
            return element;
        }

        /// <summary>
        /// 对是否是微信程序进行判断,如果是设置背景条件
        /// </summary>
        /// <param name="rootSelectorItem">根节点</param>
        /// <param name="pcSelectItemProp">进程属性</param>
        private bool PendingWeChatWorking(WinSelectorItemProperty pcSelectItemProp)
        {
            bool isUsingWeChat = false;

            if (pcSelectItemProp != null)
            {
                isUsingWeChat = pcSelectItemProp.ConiditionMatch(ConstFields.PROP_VALUE_PROCESS_WECHAT_NAME);
            }

            return isUsingWeChat;
        }

        private void RecursionChildren(string key, AutomationElement element, int depth, NetChatType type, int processId = 0)
        {
            if (type == NetChatType.WeChat)  //微信
            {
                WeChatElement wce1 = new WeChatElement(element, depth);
                WeChatDataContext.AddWeChatElement(processId, key, wce1);
            }
            else if (type == NetChatType.DingDing)
            {
                DingdingElement wce2 = new DingdingElement(element, depth);
                DingdingDataContext.AddDingdingElement(key, wce2);
            }
            depth++;
            var ele = UiCommon.TreeWalker.GetFirstChild(element);
            while (ele != null)
            {
                this.RecursionChildren(key, ele, depth, type, processId);
                ele = UiCommon.TreeWalker.GetNextSibling(ele);
            }
        }
        #region 数据采集
        public List<List<string>> CollectData_v2(ParamDataCollect_V2Model paramModel)
        {
            List<List<string>> collectDataList = new List<List<string>>();
            SelectorContext context = new SelectorContext();
            context.Version = "1.0";

            if (paramModel.IsTable)
            {
                var tableSelector = paramModel.DataCollectConfig.CollectionConfig.TableSelector;
                tableSelector.RemoveAll(s => s.Accurate.EqualIgnoreCase("false"));
                tableSelector.ForEach(s =>
                {
                    s.Props.RemoveAll(p => p.Accurate.EqualIgnoreCase("false"));
                });
                context.Flag = "NoForeground";
                context.Selector = this.SelectorAdapter.Convert2RunIndexFormat(tableSelector.ToJson());
                var node = FindNodes(context, false, true, paramModel.WindowInfo).FirstOrDefault();
                logger.LogDebug($"CollectData_v2_FindNodes_Node rect:{node.BoundingRectangle}");
                string data = node.GetTableData("");
                collectDataList = data.ToObject<List<List<string>>>();
            }
            else
            {
                //获取相同的父级
                List<SelectorProps> sameParentsProps = paramModel.DataCollectConfig.CollectionConfig.CommonSelector;
                sameParentsProps.RemoveAll(s => s.Accurate.EqualIgnoreCase("false"));
                sameParentsProps.ForEach(s =>
                {
                    s.Props.RemoveAll(p => p.Accurate.EqualIgnoreCase("false"));
                });
                List<IGuiNode> sameParents = null;
                if (sameParentsProps.Count > 0)
                {
                    context.Flag = "Foreground";
                    context.Selector = this.SelectorAdapter.Convert2RunIndexFormat(sameParentsProps.ToJson());
                    sameParents = FindNodes(context, false, false, paramModel.WindowInfo);
                    if (sameParents == null || sameParents.Count == 0)
                    {
                        return collectDataList;
                    }
                }
                else
                {
                    sameParents = new List<IGuiNode>() { null };
                }
                List<UIANode> uiaNodeList = new List<UIANode>();
                var columnSelectorArray = paramModel.DataCollectConfig.CollectionConfig.ColumnSelectorArray;
                for (var j = 0; j < columnSelectorArray.Count; j++)
                {
                    collectDataList.Add(new List<string>());
                    var selectorData = columnSelectorArray[j];
                    if (selectorData.Count == 0)
                    {
                        uiaNodeList.Add(null);
                        continue;
                    }
                    context = new SelectorContext();
                    context.Flag = "NoForeground";
                    selectorData.RemoveAll(s => s.Accurate.EqualIgnoreCase("false"));
                    selectorData.ForEach(s =>
                    {
                        s.Props.RemoveAll(p => p.Accurate.EqualIgnoreCase("false"));
                    });
                    context.Selector = this.SelectorAdapter.Convert2RunIndexFormat(selectorData.ToJson());
                    UIANode node = new UIANode(context.Selector, context.Version);
                    uiaNodeList.Add(node);
                }

                for (int i = 0; i < sameParents.Count; i++)
                {
                    int maxCount = 0;
                    int collectDataCount = collectDataList.First().Count;
                    for (int j = 0; j < columnSelectorArray.Count; j++)
                    {
                        List<string> collectData;
                        try
                        {
                            UIANode node = uiaNodeList[j];
                            var selectorData = columnSelectorArray[j];
                            if (node == null)
                            {
                                collectDataList[j].Add(sameParents[i].GetText());
                                continue;
                            }
                            collectData = new List<string>();
                            var nodes = node.FindAll(sameParents[i], useCache: false);
                            if (nodes != null && nodes.Count > maxCount)
                            {
                                maxCount = nodes.Count;
                            }
                            foreach (var child in nodes)
                            {
                                collectData.Add(child.GetText());
                            }
                        }
                        catch (RPAException ex)
                        {
                            //eErrorMessageType.Error_NotFoundElement
                            if (ex.ErrorMessageType == eErrorMessageType.Error_NotFoundElement)
                            {
                                collectData = new List<string>();
                            }
                            else
                            {
                                throw ex;
                            }
                        }
                        catch (Exception ex)
                        {
                            NLogger.Default.Error($"获取采集数据异常：{ex}");
                            throw ex;
                        }
                        collectDataList[j].AddRange(collectData);
                    }
                    for (int j = 0; j < columnSelectorArray.Count; j++)
                    {
                        int eleCountDiffNum = (collectDataCount + maxCount) - collectDataList[j].Count;
                        if (eleCountDiffNum > 0)
                        {
                            collectDataList[j].AddRange("".PadLeft(eleCountDiffNum - 1, ',').Split(','));
                        }
                    }
                }
            }
            //表格数据以行为单位，非表格数据以列为单位
            if (!paramModel.IsTable)
            {
                return collectDataList.ColumnsDataToRowsData();
            }
            return collectDataList;
        }

        public List<SelectorProps> GetSameParentsProps_v2(ParamDataCollect_V2Model paramModel)
        {
            var columnSelectorArray = paramModel.DataCollectConfig.CollectionConfig.ColumnSelectorArray;
            List<SelectorProps> sameParentsProps = new List<SelectorProps>();
            if (columnSelectorArray.Count <= 1)
            {
                return sameParentsProps;
            }
            var firstSelector = columnSelectorArray.First();
            for (int i = 0; i < firstSelector.Count; i++)
            {
                bool sameProps = true;
                var referenceProp = firstSelector[i];
                for (int j = 1; j < columnSelectorArray.Count; j++)
                {
                    if (i > columnSelectorArray[j].Count - 1)
                    {
                        sameProps = false;
                        break;
                    }
                    if (!CompareSelectorProps(referenceProp, columnSelectorArray[j][i]))
                    {
                        sameProps = false;
                        break;
                    }
                }
                if (sameProps)
                {
                    sameParentsProps.Add(referenceProp);
                }
                else
                {
                    break;
                }
            }
            return sameParentsProps;
        }
        public List<List<string>> CollectDataBySelector(ParamCollectDataBySelectorModel paramModel)
        {
            List<List<string>> collectDataList = new List<List<string>>();
            SelectorContext context = new SelectorContext();
            context.Version = "1.0";

            if (paramModel.IsTable)
            {
                var tableSelector = paramModel.SelectorDataList.FirstOrDefault()?.Selector;
                //GetElementFromSelectorParams @params = new GetElementFromSelectorParams();
                //@params.Selector = tableSelector.ToJson();
                context.Flag = "NoForeground";
                context.Selector = tableSelector.ToJson();
                var node = FindNodes(context, false, true, null).FirstOrDefault();
                string data = node.GetTableData("");
                collectDataList = data.ToObject<List<List<string>>>();
            }
            else
            {

                if (CacheRequest.IsCachingActive)
                {
                    CacheRequest.Pop();
                }
                //获取相同的父级
                List<SelectorProps> sameParentsProps = paramModel.SameParentsProps;
                List<IGuiNode> sameParents = null;
                if (sameParentsProps.Count > 0)
                {
                    context.Flag = "Foreground";
                    context.Selector = this.SelectorAdapter.Convert2RunIndexFormat(sameParentsProps.ToJson());
                    sameParents = FindNodes(context, true, false, null);
                    if (sameParents == null || sameParents.Count == 0)
                    {
                        return collectDataList;
                    }
                }
                else
                {
                    sameParents = new List<IGuiNode>() { null };
                }
                List<UIANode> uiaNodeList = new List<UIANode>();

                for (var j = 0; j < paramModel.SelectorDataList.Count; j++)
                {
                    collectDataList.Add(new List<string>());
                    var selectorData = paramModel.SelectorDataList[j].Selector;
                    if (selectorData.Count == 0)
                    {
                        uiaNodeList.Add(null);
                        continue;
                    }
                    context = new SelectorContext();
                    context.Flag = "NoForeground";
                    context.Selector = this.SelectorAdapter.Convert2RunIndexFormat(selectorData.ToJson());
                    UIANode node = new UIANode(context.Selector, context.Version);
                    uiaNodeList.Add(node);
                }

                for (int i = 0; i < sameParents.Count; i++)
                {
                    int maxCount = 0;
                    int collectDataCount = collectDataList.First().Count;
                    for (int j = 0; j < paramModel.SelectorDataList.Count; j++)
                    {
                        List<string> collectData;
                        try
                        {
                            UIANode node = uiaNodeList[j];
                            var selectorData = paramModel.SelectorDataList[j].Selector;
                            if (node == null)
                            {
                                collectDataList[j].Add(sameParents[i].GetText());
                                continue;
                            }
                            collectData = new List<string>();
                            //if (sameParents[i] != null)
                            //{
                            //    node.automationElement = ((UIANode)sameParents[i]).automationElement;
                            //}
                            var nodes = node.FindAll(sameParents[i], useCache: true);
                            if (nodes != null && nodes.Count > maxCount)
                            {
                                maxCount = nodes.Count;
                            }
                            foreach (var child in nodes)
                            {
                                collectData.Add(child.GetText());
                            }
                        }
                        catch (RPAException ex)
                        {
                            //eErrorMessageType.Error_NotFoundElement
                            if (ex.ErrorMessageType == eErrorMessageType.Error_NotFoundElement)
                            {
                                collectData = new List<string>();
                            }
                            else
                            {
                                throw ex;
                            }
                        }
                        catch (Exception ex)
                        {
                            NLogger.Default.Error($"获取采集数据异常：{ex}");
                            throw ex;
                        }
                        collectDataList[j].AddRange(collectData);
                    }
                    for (int j = 0; j < paramModel.SelectorDataList.Count; j++)
                    {
                        int eleCountDiffNum = (collectDataCount + maxCount) - collectDataList[j].Count;
                        if (eleCountDiffNum > 0)
                        {
                            collectDataList[j].AddRange("".PadLeft(eleCountDiffNum - 1, ',').Split(','));
                        }
                    }
                }
                //collectDataList = ChromeNode.GetCollectDataFromSelector(sameParentsProps, paramModel.SelectorDataList);
            }
            if (CacheRequest.IsCachingActive)
            {
                CacheRequest.Pop();
            }
            return collectDataList;
        }

        public void ScrollIntoView(string srcoll)
        {
            throw new NotImplementedException();
        }

        public string GetBoundings(string jsonParams)
        {
            NLogger.Default.Trace($" start GetBoundings");
            JArray bounding = new JArray();
            JArray selectors = JArray.Parse(jsonParams);
            foreach (var selector in selectors)
            {
                var guiNode = GetElementFromSelector(new GetElementFromSelectorParams()
                {
                    Selector = selector.ToString()
                });
                var rect = guiNode.BoundingRectangle;
                bounding.Add(rect.ToJsonNoneFormating());
            }
            NLogger.Default.Trace($" end GetBoundings");

            return bounding.ToString();
        }
        public string BuildSelectors(string jsonParams)
        {
            throw new NotImplementedException();
        }
        public string BuildSelectors(ParamBuildSelectorsModel paramModel)
        {
            throw new NotImplementedException();
        }
        public string BuildSelectors(BulidSelectorsParams @params)
        {
            var eleIds = JsonConvert.DeserializeObject<string[]>(@params.ElementIds);
            List<string> selectorList = new List<string>();
            for (int i = 0; i < eleIds.Length; i++)
            {
                string[] idItems = eleIds[i].Split('/');
                if (CacheData.GuiNodeCacahe.ContainsKey(idItems[0]) && CacheData.GuiNodeCacahe[idItems[0]].Length > int.Parse(idItems[1]))
                {
                    var node = (UIANode)CacheData.GuiNodeCacahe[idItems[0]][int.Parse(idItems[1])];
                    selectorList.Add(node.BuildSelector().ToString());
                }
            }
            return selectorList.ToJsonNoneFormating();
        }
        #endregion

        public string BuilderSelector(AutomationElement element)
        {
            UIANode node = new UIANode(element);
            return node.BuildSelector();
        }

        public AutomationElement BuilderAutomationElement(string selector, WindowObject tragetWindow)
        {
            SelectorContext context = new SelectorContext();
            context.Version = "1.0";
            context.Flag = "Foreground";
            context.Selector = selector;

            logger.LogInformation($"Win32 BuilderAutomationElement");

            IGuiNode foundNode = null;
            if (foundNode == null)
            {
                foundNode = FindNodes(context, false, true, tragetWindow).FirstOrDefault();
                return ((UIANode)foundNode).automationElement;
            }
            return null;
        }

        public List<SelectorProps> GetSameParentsProps(ParamCollectDataBySelectorModel paramModel)
        {
            List<SelectorProps> sameParentsProps = new List<SelectorProps>();
            if (paramModel.SelectorDataList.Count <= 1)
            {
                return sameParentsProps;
            }
            var firstSelector = paramModel.SelectorDataList.First();
            for (int i = 0; i < firstSelector.Selector.Count; i++)
            {
                bool sameProps = true;
                var referenceProp = firstSelector.Selector[i];
                for (int j = 1; j < paramModel.SelectorDataList.Count; j++)
                {
                    if (i > paramModel.SelectorDataList[j].Selector.Count - 1)
                    {
                        sameProps = false;
                        break;
                    }
                    if (!CompareSelectorProps(referenceProp, paramModel.SelectorDataList[j].Selector[i]))
                    {
                        sameProps = false;
                        break;
                    }
                    //if (!referenceProp.Equals(paramModel.SelectorDataList[j].Selector[i],true))
                    //{
                    //    sameProps = false;
                    //    break;
                    //}
                }
                if (sameProps)
                {
                    sameParentsProps.Add(referenceProp);
                }
                else
                {
                    break;
                }
            }
            return sameParentsProps;
        }
        private bool CompareSelectorProps(SelectorProps sp1, SelectorProps sp2)
        {
            var sp1Props = sp1.Props.Where(p => p.Accurate.EqualIgnoreCase("true"));
            var sp2Props = sp2.Props.Where(p => p.Accurate.EqualIgnoreCase("true"));
            return sp1.Name == sp2.Name &&
             sp1.Type == sp2.Type &&
             sp1.Accurate == sp2.Accurate &&
             sp1Props.Count() == sp2Props.Count() &&
             sp1Props.All(p1 => sp2Props.Any(p2 => p1.EqualsNameAndValue(p2)));
        }

        public string GetElementsAll(GetElementsAllParams @params)
        {
            var tagNames = string.IsNullOrEmpty(@params.TagNames) ? new string[0] : @params.TagNames.Split(',');
            var attrKeywords = string.IsNullOrEmpty(@params.AttrKeywords) ? new string[0] : @params.AttrKeywords.Split(',');
            List<IGuiNode> nodes = new List<IGuiNode>();
            List<JObject> retData = new List<JObject>();
            string cacheKey = $"{@params.WindowHandle.ToString()}";

            AutomationElement rootElement = UiCommon.UIAAutomation.FromHandle(@params.WindowHandle);
            UIANode rootUIANode = new UIANode(rootElement);
            rootUIANode.Properties.BoundingRectangle.TryGetValue(out var rootNodeBoundingRect);
            rootUIANode.IterateNodeChildren((node) =>
            {
                bool isMatch = false;
                if (!node.automationElement.Properties.BoundingRectangle.TryGetValue(out var boundingRect)
                     || !rootNodeBoundingRect.IntersectsWith(boundingRect))
                {
                    return false;//非可视区域的元素，不再继续遍历节点子项
                }
                //无过滤条件
                if (tagNames.Length == 0 && attrKeywords.Length == 0)
                {
                    isMatch = true;
                    nodes.Add(node);
                    return true;
                }
                if (tagNames.Length > 0)
                {
                    isMatch = (node.Properties.ControlType.TryGetValue(out var controlType) && tagNames.Contains(controlType.ToString(), StringComparer.OrdinalIgnoreCase));
                }
                else
                {
                    isMatch = true;
                }

                if (isMatch)
                {
                    if (attrKeywords.Length > 0)
                    {
                        isMatch = (node.Properties.Name.TryGetValue(out var name) && attrKeywords.Contains(name, StringComparer.OrdinalIgnoreCase));
                    }
                    else
                    {
                        isMatch = true;
                    }
                }

                if (isMatch)
                {
                    nodes.Add(node);
                }
                return true;
            });

            CacheData.GuiNodeCacahe[cacheKey] = nodes.ToArray();
            for (int i = 0; i < nodes.Count; i++)
            {
                var dataObj = new JObject()
                    {
                        { "uniqueId",$"{cacheKey}/{i}"},
                        { "controlType",nodes[i].ControlType},
                        { "text",nodes[i].Name},
                    };
                retData.Add(dataObj);
            }
            return new JObject() {
                { "elements",JArray.FromObject( retData)},
                { "img_id",""}
            }.ToJsonNoneFormating();
        }
        public IGuiNode GetElementById(GetElementByIdParams @params)
        {

            string[] idItems = @params.ElementId.Split('/');
            if (CacheData.GuiNodeCacahe.ContainsKey(idItems[0]) && CacheData.GuiNodeCacahe[idItems[0]].Length > int.Parse(idItems[1]))
            {
                return (UIANode)CacheData.GuiNodeCacahe[idItems[0]][int.Parse(idItems[1])];
            }
            return null;
        }

        public List<JObject> GetSimilarOrDataCollectElements(GetElementsAllParams @params)
        {
            throw new NotImplementedException();
        }

        public IGuiNode[] QuerySelectorAll(GetElementFromSelectorParams args)
        {
            logger.LogDebug($"start  QuerySelectorAll");
            //转换json格式，由于存在新老格式，所以存在转换
            ISelectorContext context = this.SelectorAdapter.GetSelectorJson(args);
            bool filterHidden = context.GetFilterHidden(false);
            var foundNodes = FindNodes(context, false, filterHidden, args.WindowInfo);
            logger.LogDebug($"end QuerySelectorAll");

            return foundNodes.ToArray<IGuiNode>();
        }

        public bool VerifyWindowOfNode(GetElementFromSelectorParams args)
        {
            try
            {
                ISelectorContext context = this.SelectorAdapter.GetSelectorJson(args);
                UIANode node = new UIANode(context.Selector, context.Version);
                return node.GetFirstActiveMainWindow() != null;
            }
            catch (Exception ex)
            {
                logger.LogWarning($"校验目标窗口异常:{ex}");
                return true;
            }
        }

        public string GetTargetElementInfo(GetTargetElementInfoModelParams @params)
        {
            throw new NotImplementedException();
        }
    }
}


