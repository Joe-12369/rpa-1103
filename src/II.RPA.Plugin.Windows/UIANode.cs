using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.WinAutomation.CommonUtility;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.Patterns;
using FlaUI.Core.Tools;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3.Converters;
using IAccessible2Lib;
using II.RPA.Logging;
using II.RPA.Logging.Formatting;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.Plugin.GUI.Commons.WindowMatch;
using II.RPA.Plugin.Windows.Config;
using II.RPA.Plugin.Windows.Core;
using II.RPA.Plugin.Windows.OwnerDraw;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation.Provider;
using UIAutomationInterop;
using static InteropMy.UiaCore;

namespace II.RPA.Plugin.Windows
{
    public class UIANode : Selector, IGuiNode_6_6_0, IGuiWindowsNode
    {
        private const string ChromeTableCellIndexProperty = "<web>table-cell-index";

        private static ILogger logger = Logger.OfContext();

        internal AutomationElement automationElement;

        private Rectangle _BoundingRectangle = Rectangle.Empty;

        /// <summary>TMS 虚拟项的屏幕边界（拾取时计算，回放时用于精准点击）。</summary>
        internal Rectangle? _tmsVirtualBounds;

        private UIANode automationElementParent;

        private UIANode cachedParent;
        internal AutomationElement automationElement_uia_bak;

        private string cachedProcessFullPath;

        //优化读取速度
        private string cachedProcessName;

        private string cacheSelector;
        public string cacheDataSelector;

        private CancellationTokenSource ctSource = null;

        private Rectangle rect = new Rectangle();

        private Semaphore sema = null;

        private ISelectorContext SelectorContext { get; set; }
        internal string _selector = string.Empty;

        #region ProcessID 元素所在进程的ID
        private static int processID;
        /// <summary>
        /// 元素所在进程的ID
        /// </summary>
        public virtual int ProcessID
        {
            get
            {
                return processID;
            }

            set
            {
                processID = value;
            }
        }
        #endregion

        public UIANode(ISelectorContext context) : this(context.Selector, context.Version)
        {
            SelectorContext = context;
        }
        public UIANode(string json, string version) : base(json, version)
        {
            logger.LogDebug("UIANode-selector" + json);
            cacheSelector = json;
        }

        public UIANode(AutomationElement element, UIANode parent = null)
        {
            this.automationElement = element;
            this.cachedParent = parent;

            bool isChromeElement = false;
            try
            {
                isChromeElement = element?.Properties.FrameworkId.ValueOrDefault == "Chrome";
            }
            catch
            {
                isChromeElement = false;
            }

            if (isChromeElement && element.IsOffscreen && element.Patterns.LegacyIAccessible.IsSupported)
            {
                var acc_test = element.Patterns.LegacyIAccessible.PatternOrDefault?.GetIAccessible();
                if (acc_test != null)
                {
                    if (acc_test is IAccessibleInternal acc)
                    {
                        int xLeft2 = 0, yTop2 = 0, cxWidth2 = 0, cyHeight2 = 0;
                        int hr = acc.accLocation(out xLeft2, out yTop2, out cxWidth2, out cyHeight2, CHILDID_SELF);
                        if (hr == CommonHresultValues.S_OK)
                        {
                            if (cxWidth2 > 0 && cyHeight2 == 0)
                            {
                                //为了绕过GuiOperator.FindNodes函数中filterHidden为true时的内部逻辑检查，故意将Height改为1，
                                //不然用户取消了对尾部元素的勾选的话，将导致上层的IsOffscreen为true的元素无法被校验到
                                cyHeight2 = 1;
                            }
                            var f = NativeMethods.DynamicScale;
                            var acc_rect = new Rectangle((int)(xLeft2 * f), (int)(yTop2 * f), (int)(cxWidth2 * f), (int)(cyHeight2 * f));
                            _BoundingRectangle = acc_rect;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// /*************** 千牛客户端判断添加，目前只有该场景*************/
        /// </summary>
        public UIANode(AutomationElement element, ref string strRet, UIANode parent = null)
        {
            this.automationElement = element;
            this.cachedParent = parent;
            strRet = BuildSelector();
        }

        public event Action<string> WindowTypeChange;

        public virtual IGuiNode AutomationElementParent
        {
            get
            {
                automationElementParent = new UIANode(automationElement.Parent);
                return automationElementParent;
            }
        }

        public virtual string AutomationId
        {
            get
            {
                try
                {
                    return automationElement.AutomationId;
                }
                catch (Exception)
                {
                    return string.Empty;
                }
            }
        }

        public virtual Rectangle BoundingRectangle
        {
            get
            {
                if (_tmsVirtualBounds.HasValue)
                    return _tmsVirtualBounds.Value;
                if (_BoundingRectangle != Rectangle.Empty)
                {
                    return _BoundingRectangle;
                }
                //logger.LogDebug("UIANode 获取元素区域1");
                if (this.automationElement == null)
                {
                    return Rectangle.Empty;
                }
                if (!this.automationElement.Properties.BoundingRectangle.IsSupported)
                {
                    return Rectangle.Empty;
                }
                //logger.LogDebug("UIANode 获取元素区域");
                _BoundingRectangle = this.automationElement.BoundingRectangle;
                return _BoundingRectangle;
            }
        }

        public virtual List<IGuiNode> Children
        {
            get
            {
                var list = new List<IGuiNode>();
                var children = automationElement.FindAllChildren();
                foreach (var item in children)
                {
                    list.Add(new UIANode(item, this));
                }
                return list;
            }
        }

        public virtual string ClassName
        {
            get
            {
                try
                {
                    return automationElement.ClassName;
                }
                catch (Exception)
                {
                    return string.Empty;
                }
            }
        }
        public FrameworkAutomationElementBase.IProperties Properties
        {
            get
            {
                return automationElement.Properties;
            }
        }
        public virtual string ControlType
        {
            get
            {
                try
                {
                    var controlType = automationElement.Properties.ControlType.IsSupported ? automationElement.Properties.ControlType.ToString() : string.Empty;
                    if (string.IsNullOrEmpty(controlType) || controlType.EqualIgnoreCase(ConstFields.PROP_VALUE_CONTROL_TYPE_UNKNOW))
                    {
                        controlType = ConstFields.SELECTOR_Custom;
                    }
                    return controlType;
                }
                catch (Exception)
                {
                    return string.Empty;
                }
            }
        }

        public virtual string Description
        {
            get
            {
                try
                {
                    if (automationElement.Patterns.LegacyIAccessible.Pattern.Description.IsSupported)
                    {
                        return automationElement.Patterns.LegacyIAccessible.Pattern.Description.ValueOrDefault;
                    }
                    else
                    {
                        return string.Empty;
                    }
                }
                catch (Exception)
                {
                    return string.Empty;
                }
            }
        }

        public bool FindOnce { get; set; }

        public virtual string Idx
        {
            get
            {
                var realParent = GetCorrectParent(automationElement);
                var children = realParent.FindAllChildren();

                int index = 0;

                foreach (var item in children)
                {
                    if (item.Equals(automationElement))
                    {
                        return index.ToString();
                    }

                    index++;
                }

                return "";
            }
        }

        public virtual bool IsTopLevelWindow
        {
            get
            {
                try
                {
                    return automationElement.Parent != null && automationElement.Parent.Parent == null;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public virtual string Name
        {
            get
            {
                try
                {
                    return automationElement.Name;
                }
                catch (Exception)
                {
                    return "";
                }
            }
        }
        internal bool ContainProcessInfoSelecor { get; set; } = true;
        public virtual string NodeSelector
        {
            get
            {
                return GetSelector(false);
            }
        }
        public virtual string GetSelector(bool innerTextFirst)
        {
            if (!string.IsNullOrEmpty(cacheSelector))
            {
                return cacheSelector;
            }
            Stopwatch _w = Stopwatch.StartNew();
            if (this.CheckWithinChromePageTable())
            {
                //获取拾取元素的首个table父节点的selector
                var tableSelector = new UIANode(this.chromePageTableElement).BuildSelector();
                if (this.automationElement.Equals(this.chromePageTableElement))
                {
                    //当前拾取元素就是table，不需要再构建相对于自身的selector
                    logger.LogDebug("Chrome表格根节点直接使用table selector");
                    cacheSelector = tableSelector;
                }
                else
                {
                    string relativeTableSelector = null;
                    try
                    {
                        //获取拾取元素相对于首个table父节点的selector
                        relativeTableSelector = this.BuildRelativeSelector(this.chromePageTableElement);
                    }
                    catch (Exception ex)
                    {
                        //UIA树可能在拾取后发生变化，相对selector失败时回退到普通selector
                        Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "Chrome表格相对selector构建异常，将回退普通selector");
                    }

                    if (relativeTableSelector == null)
                    {
                        logger.LogDebug("Chrome表格相对selector构建失败，回退普通selector");
                        cacheSelector = BuildSelector();
                    }
                    else
                    {
                        //拼接tableselector 和 relativeTableSelector
                        cacheSelector = JArray.Parse(tableSelector).Concat(JArray.Parse(relativeTableSelector)).ToJson();
                    }
                }
            }
            else
            {
                cacheSelector = BuildSelector();
            }

            logger.LogDebug(" get selector and start bulid");
            SelectorAdapter adapter = new SelectorAdapter();
            if (!string.IsNullOrEmpty(_selector))
            {
                try
                {
                    cacheSelector = adapter.Convert2EditIndexFormat(cacheSelector);//替换indexinparent为 @index
                    var selector1 = UiCommon.ExtractSelectors(_selector);
                    var s1 = JsonConvert.DeserializeObject<List<SelectorProps>>(selector1);
                    var s2 = JsonConvert.DeserializeObject<List<SelectorProps>>(cacheSelector);
                    var compareSelectorList = CompareSelectorAndRemoveDiffProps(s1, s2);
                    cacheSelector = compareSelectorList.ToJsonNoneFormating();
                }
                catch (RPAException e)
                {
                    cacheSelector = _selector;
                    _selector = null;
                    throw e;
                }
            }
            string path = string.Empty;
            string processName = string.Empty;
            if (ContainProcessInfoSelecor)
            {
                logger.LogDebug("开始获取进程信息");
                var processId = automationElement.Properties.ProcessId.Value;
                path = ExtractViewData.GetMainModuleFilepath(processId, out processName);
                logger.LogDebug("结束获取进程信息");
            }
            cacheSelector = ExtractViewData.AddCommonProps(cacheSelector, eGuiType.Windows, path, processName: processName);

            cacheSelector = adapter.Convert2EditIndexFormat(cacheSelector);
            _w.Stop();
            logger.LogDebug($"selector bulid end。耗时：{_w.ElapsedMilliseconds}");
            return cacheSelector;
        }
        public virtual IGuiNode Parent
        {
            get
            {
                if (cachedParent == null)
                {
                    var realParent = GetCorrectParent(automationElement);
                    if (realParent != null)
                    {
                        cachedParent = new UIANode(realParent);
                    }
                }
                return cachedParent;
            }
        }

        public virtual string ProcessFullPath
        {
            get
            {
                if (cachedProcessFullPath == null)
                {
                    try
                    {
                        var processId = automationElement.Properties.ProcessId.Value;
                        var path = UiCommon.GetProcessFullPath(processId);

                        cachedProcessFullPath = path;
                    }
                    catch (Exception)
                    {
                        cachedProcessFullPath = string.Empty;
                    }
                }

                return cachedProcessFullPath;
            }
        }

        public virtual string ProcessName
        {
            get
            {
                if (string.IsNullOrEmpty(cachedProcessName))
                {
                    try
                    {
                        var processId = automationElement.Properties.ProcessId.Value;
                        var name = UiCommon.GetProcessNameById(processId);

                        cachedProcessName = name;
                    }
                    catch (Exception ex)
                    {
                        logger.LogDebug($"{ex}");
                        cachedProcessName = string.Empty;
                    }
                }

                return cachedProcessName;
            }
        }

        public virtual string Role
        {
            get { return string.Empty; }
        }

        public WindowObject RootWindow { get; set; }


        public AutomationElement[] _CandidateMainWindows = null;
        public AutomationElement[] CandidateMainWindows
        {
            get
            {
                if (_CandidateMainWindows != null)
                {
                    return _CandidateMainWindows;
                }
                if (RootWindow != null)
                {
                    _CandidateMainWindows = new AutomationElement[] { RootWindow.ToAutomationElement() };
                }
                else
                {
                    _CandidateMainWindows = GetCandidateMainWindows();
                }
                return _CandidateMainWindows;
            }
        }
        public WindowsSelectorItem RootSelectorItem
        {
            get
            {
                var rootSelectorItem = this.OfType<WindowsSelectorItem>().FirstOrDefault(item => item.Enabled);
                return rootSelectorItem;
            }
        }

        public string Title
        {
            get
            {
                try
                {
                    JObject ja = new JObject(this.NodeSelector);
                    JToken propToken = null;
                    if (ja.TryGetValue(ConstFields.SELECTOR_ELEMENT_SELECTOR_TAG, StringComparison.OrdinalIgnoreCase, out propToken))
                    {
                        List<SelectorProps> winPropItems = propToken.ToObject<List<SelectorProps>>();

                        foreach (var selectorItem in winPropItems)
                        {
                            if (selectorItem.Name.Equals(ConstFields.PROP_VALUE_CONTROL_TYPE_WINDOW))
                            {
                                var prop = selectorItem.Props.FirstOrDefault(x => x.Name.Equals(ConstFields.SELECTOR_NAME, StringComparison.OrdinalIgnoreCase));
                                return prop.Value;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex);
                }
                return null;
            }
        }

        public virtual string UserDefinedID
        {
            get
            {
                return "";
            }
        }

        public virtual IntPtr WindowHandle
        {
            get
            {
                try
                {
                    if (automationElement.Properties.NativeWindowHandle.IsSupported)
                    {
                        var windowHandle = automationElement.Properties.NativeWindowHandle.ValueOrDefault;
                        return windowHandle;
                    }
                    else
                    {
                        return IntPtr.Zero;
                    }
                }
                catch (Exception)
                {
                    return IntPtr.Zero;
                }
            }
        }

        private ITreeWalker TreeWalker
        {
            get
            {
                return UiCommon.TreeWalker;
            }
        }

        private AutomationBase UIAAutomation
        {
            get
            {
                return UiCommon.UIAAutomation;
            }
        }

        public virtual string ClickTableCell(string input)
        {
            throw new NotImplementedException();
        }

        public virtual Bitmap ElementToBitmap()
        {
            return automationElement.Capture();
        }



        private void AddIfNotExist(List<AutomationElement> items, AutomationElement addingItem)
        {
            if (items.Any(x => x.Equals(addingItem)))
            {
                return;
            }
            items.Add(addingItem);
        }

        public virtual List<IGuiNode> FindAll(IGuiNode fromElement = null, int maxResult = 1, int milliseconds = 3000)
        {
            return FindAll(fromElement, maxResult, milliseconds, false);
        }
        public virtual List<IGuiNode> FindAll(IGuiNode fromElement = null, int maxResult = 1, int milliseconds = 3000, bool useCache = false)
        {
            OwnerDrawResult<OwnerDrawReplayRequest> ownerDrawReplay
                = OwnerDrawSelectorEnvelope.TrySplit(ToString());
            if (ownerDrawReplay.State == OwnerDrawProviderState.RecognizedButFailed)
            {
                logger.LogWarning("Owner-draw selector rejected before host matching: {Status}",
                    ownerDrawReplay.Status);
                return new List<IGuiNode>();
            }
            if (ownerDrawReplay.IsSuccess)
            {
                return FindOwnerDrawAll(ownerDrawReplay.Value, fromElement,
                    maxResult, useCache);
            }

            TimeSpan timeout = TimeSpan.FromMilliseconds(milliseconds);   //超时时间，这里可以动态配置
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            AutomationElement[] fromElements = null;
            if (fromElement == null)
            {
                fromElements = CandidateMainWindows;
                if (fromElements?.Length > 0)
                    this.RemoveAt(0);
            }
            else
            {
                fromElements = new AutomationElement[] { ((UIANode)fromElement).automationElement };
            }

            var selectors = this.OfType<WindowsSelectorItem>().ToList(); //找出不是顶层元素
            var elements = QuerySelectorAll(selectors, fromElements, useCache);
            logger.LogDebug("查找完成，耗时：" + sw.Elapsed);
            logger.LogDebug("查找完成，找到：" + elements.Count + "个匹配项");
            if (elements.Count > 0)
            {
                List<IGuiNode> nodes = new List<IGuiNode>();
                foreach (var item in elements)
                {
                    nodes.Add(new UIANode(item)
                    {
                        cacheDataSelector = this.SelectorContext?.SourceSelector
                    });
                }
                sw.Stop();
                return nodes;
            }
            else
            {
                return new List<IGuiNode>();
            }
        }

        /// <summary>
        /// 兼容老的桌面元素结构
        /// </summary>
        /// <returns></returns>
        private List<WindowsSelectorItem> CompatibleOldDesktopSelector(List<WindowsSelectorItem> selectors)
        {
            int syslistview32_node_index = selectors[0].Properties.FindIndex(p => p.Name == ConstFields.SELECTOR_CLASS_NAME && p.Value.EqualIgnoreCase("SysListView32"));
            if (syslistview32_node_index >= 0)
            {
                if (selectors[0].Properties.Any(p => p.Name == ConstFields.SELECTOR_ROOT_PROCESSNAME && p.Value.EqualIgnoreCase("explorer")))
                {
                    string progmanSelectorStr_first = ConstFields.SELECTOR_ROOT_TOP_NODESELECTOR;
                    string progmanSelectorStr_second = ConstFields.SELECTOR_ROOT_TOP_SHELLDLL_DefView;
                    var progmanSelector_first = GenerateSelectorItem(JObject.Parse(progmanSelectorStr_first), "1.0");
                    var progmanSelector_second = GenerateSelectorItem(JObject.Parse(progmanSelectorStr_second), "1.0");
                    selectors[0].Properties.RemoveAll(p => p.Name == ConstFields.SELECTOR_ROOT_PROCESSNAME && p.Value.EqualIgnoreCase("explorer"));
                    selectors.Insert(0, (WindowsSelectorItem)progmanSelector_second);
                    selectors.Insert(0, (WindowsSelectorItem)progmanSelector_first);
                }
            }
            return selectors;
        }

        public List<AutomationElement> QuerySelectorAll(List<WindowsSelectorItem> selectors, AutomationElement[] fromElements = null, bool useCache = false, bool useHiddenWindow = false, bool useChildrenWindow = false, bool checkAccEnabler = true)
        {
            List<AutomationElement> elements = new List<AutomationElement>();
            AutomationElement[] startfroms = null;
            try
            {
                startfroms = fromElements;
                logger.LogDebug($"FindAll startfroms length{startfroms?.Length}");
                if (startfroms == null || startfroms.Length == 0)
                {
                    startfroms = new AutomationElement[] { UIAAutomation.GetDesktop() };
                    selectors = CompatibleOldDesktopSelector(selectors);
                }
                elements.AddRange(startfroms);
                AccEnabler.IsMatch_BeginUseCachedResult();
                for (var i = 0; i < selectors.Count; i++)
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    logger.LogDebug($"开始查找第{i}个Selector:{selectors[i].ToString()}");

                    var s = selectors[i];  //子项
                    if (!s.Enabled)
                    {
                        logger.LogDebug($"第{i}节点未启用，执行跳级");
                        WindowsElementMatcher windowsElementMatcher = new WindowsElementMatcher(2 * 60 * 1000);
                        return windowsElementMatcher.JumpQuerySelectorAll(selectors.Skip(i).ToList(), elements, useCache, useHiddenWindow, useChildrenWindow, checkAccEnabler);
                    }
                    var current = new List<AutomationElement>();
                    current.AddRange(elements);
                    logger.LogDebug($"elements.Count:{current.Count}");
                    elements.Clear();
                    bool isDesktop = i == 0 && (fromElements == null || fromElements.Length == 0);
                    foreach (var item in current)
                    {
                        GUI.Commons.AccessibleObject.EnableAccessibility(item);
                        AutomationElement itemCache = null;
                        //开启缓存模式
                        if (useCache && (i == 1 || (i == 0 && !isDesktop)))
                        {
                            if (!CacheRequest.IsCachingActive)
                            {
                                CacheRequestActive(item);
                            }
                            if (CacheRequest.IsCachingActive)
                            {
                                itemCache = item.FindFirst(TreeScope.Element, TrueCondition.Default);
                            }
                        }
                        var matches = s.Matches(itemCache ?? item, isDesktop, useHiddenWindow, useChildrenWindow);//子项selectItem去目标父节点（AutomationElement）中匹配与自身参数匹配的元素，最终得到此子项对应的AutomationElement
                        logger.LogDebug($"找到{(matches == null ? 0 : matches.Count())}个匹配项,本节点耗时：{stopwatch.ElapsedMilliseconds}");
                        if (matches != null && matches.Length > 0)
                        {
                            elements.AddRange(matches);
                        }
                    }
                    logger.LogDebug($"本节点耗时：{stopwatch.ElapsedMilliseconds}");
                    if (elements.Count == 0)
                    {
                        // 自动降级(跳级)兜底只对配置内的应用生效：它放弃失败节点的约束，
                        // 用剩余节点在任意层级上宽松匹配，对所有应用生效时会把"元素已不存在"
                        // 判定成命中(元素校验误判成功)，详见 WinConfigurations.JumpFallbackApps。
                        string fallbackProcessName = GetMatchedProcessName(current);
                        if (!WinConfigurations.Info.IsJumpFallbackApp(fallbackProcessName))
                        {
                            logger.LogDebug($"第{i}层匹配失败，进程[{fallbackProcessName}]未启用自动跳级兜底，匹配终止");
                            break;
                        }
                        // CEF/Chromium 冷启动时 UIA 树结构与拾取时不同（如 ControlType
                        // 从 Group 变为 Text）。逐层匹配已失败，但 s.Matches() 调用中
                        // 的 ControlView 遍历已污染了父元素的 CachedRawViewChildren 缓存，
                        // 导致后续 JumpQuerySelectorAll 的 RecursiveDescendantsByRawView
                        // 返回不完整树（GetChildrenByRawView(false) 优先返回缓存）。
                        //
                        // 清除缓存后执行 JumpQuerySelectorAll，效果等同于用户手动
                        // 勾选掉当前节点——都用 `.*` 跳过结构变化的层。
                        foreach (var item in current)
                        {
                            item.CachedRawViewChildren = null;
                            item.CachedRawViewLeafs?.Clear();
                        }

                        logger.LogDebug($"第{i}层匹配失败，清除缓存+JumpQuerySelectorAll(剩余{selectors.Count - i}层, from={current.Count})");
                        var skipSelectors = new List<WindowsSelectorItem>(selectors.Skip(i));
                        skipSelectors[0].Enabled = false; // 效果等同于用户勾选掉
                        WindowsElementMatcher jumpMatcher = new WindowsElementMatcher(2 * 60 * 1000);
                        var jumpResult = jumpMatcher.JumpQuerySelectorAll(skipSelectors, current, useCache, useHiddenWindow, useChildrenWindow, checkAccEnabler);
                        // 该兜底的语义是"树结构微变但仍是同一个元素"，命中必然唯一；
                        // 命中多个说明失败的节点正是区分该元素的关键约束(例如元素被改名/删除)，
                        // 此时不能当作命中，否则会返回一堆同类元素导致元素校验误判成功。
                        if (jumpResult != null && jumpResult.Count == 1)
                            return jumpResult;
                        if (jumpResult != null && jumpResult.Count > 1)
                            logger.LogWarning($"第{i}层匹配失败，自动跳级兜底命中{jumpResult.Count}个元素，结果不唯一，按未命中处理");
                        break;
                    }
                    if (checkAccEnabler)
                        foreach (var item in elements)
                        {
                            AccEnabler.EnableOnce(item);
                        }
                }

            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, $"查找元素出错");
            }
            finally
            {
                AccEnabler.IsMatch_EndUseCachedResult();
            }
            //没有匹配到任何结果，再次匹配允许使用子窗口
            //没有匹配到任何结果，再次匹配允许使用隐藏子窗口
            if (elements.Count == 0 && !useCache && (!useChildrenWindow || !useHiddenWindow))
            {
                // CEF/Chromium 冷启动时 UIA 树未被完整生成。
                // 在重试之前，先从候选窗口出发用 FindAll(Subtree) 全量枚举，
                // 强制 CEF 服务端生成完整的中间层容器节点。
                foreach (var sf in (fromElements ?? new AutomationElement[0]))
                {
                    try { ExpandUiaSubtree(sf); } catch { }
                }
                logger.LogDebug($"未匹配到结果，再次匹配，使用隐藏窗口、子窗口等");
                elements = QuerySelectorAll(selectors, fromElements, useCache, useHiddenWindow: true, useChildrenWindow: true);
            }
            return elements;
        }
        /// <summary>
        /// 取已匹配元素的进程名，用于判断是否允许"某层匹配失败后自动降级为跳级匹配"。
        /// 取不到进程名时返回空，此时按不允许兜底处理，避免误判。
        /// </summary>
        private string GetMatchedProcessName(List<AutomationElement> matchedElements)
        {
            if (matchedElements == null || matchedElements.Count == 0)
            {
                return string.Empty;
            }
            try
            {
                return UiCommon.GetProcessNameById(matchedElements[0].Properties.ProcessId.Value);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, "获取已匹配元素的进程名失败");
                return string.Empty;
            }
        }
        /// <summary>
        /// 展开 UIA 子树，强制懒加载的无障碍树（如 CEF/Chromium）生成完整节点。
        /// 使用 FindAll(TreeScope.Subtree, TrueCondition.Default) 全量枚举所有子孙节点。
        /// CEF/Chromium 的 UIA 树是按需懒加载的，只有被 UIA 客户端完整枚举过的节点
        /// 才会在树中实体化。手动 TreeWalker 遍历不足以触发所有中间层节点生成，
        /// 必须通过 FindAll 让 UIA 服务端做全量查询才能迫使 CEF 补全缺失的容器节点。
        /// </summary>
        /// <returns>实际展开了多少个节点</returns>
        private static int ExpandUiaSubtree(AutomationElement root)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var allDescendants = root.FindAll(TreeScope.Subtree, TrueCondition.Default);
                int count = allDescendants.Length;
                if (count > 0)
                    logger.LogDebug($"UIA 子树展开完成: {count} 节点, 耗时 {sw.ElapsedMilliseconds}ms");
                return count;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, $"UIA 子树展开异常(非致命)");
                return 0;
            }
        }

        public void CacheRequestActive(AutomationElement startfrom, TreeScope treeScope = TreeScope.Subtree)
        {
            var cr = new CacheRequest
            {
                AutomationElementMode = AutomationElementMode.Full
            };
            // Add the element properties
            cr.Add(startfrom.Automation.PropertyLibrary.Element.ControlType);
            cr.Add(startfrom.Automation.PropertyLibrary.Element.ClassName);
            cr.Add(startfrom.Automation.PropertyLibrary.Element.Name);
            cr.Add(startfrom.Automation.PropertyLibrary.Element.HelpText);
            cr.Add(startfrom.Automation.PropertyLibrary.Element.BoundingRectangle);
            cr.Add(startfrom.Automation.PropertyLibrary.Element.IsOffscreen);
            cr.Add(startfrom.Automation.PropertyLibrary.Element.FrameworkId);
            cr.Add(startfrom.Automation.PropertyLibrary.Element.ProcessId);
            cr.Add(startfrom.Automation.PropertyLibrary.Element.IsPassword);
            //cr.Add(startfrom.Automation.PropertyLibrary.Element.IsEnabled);
            cr.Add(startfrom.Automation.PropertyLibrary.Element.NativeWindowHandle);
            //cr.Add(startfrom.Automation.PropertyLibrary.Element);

            cr.Add(startfrom.Automation.PropertyLibrary.LegacyIAccessible.Name);
            //cr.Add(startfrom.Automation.PropertyLibrary.LegacyIAccessible.State);
            cr.Add(startfrom.Automation.PropertyLibrary.LegacyIAccessible.Value);
            cr.Add(startfrom.Automation.PropertyLibrary.LegacyIAccessible.Role);
            //cr.Add(startfrom.Automation.PropertyLibrary.LegacyIAccessible.Description);
            //cr.Add(startfrom.Automation.PropertyLibrary.LegacyIAccessible.Selection);
            //cr.Add(startfrom.Automation.PropertyLibrary.LegacyIAccessible.ChildId);


            // Add the pattern availability properties
            cr.Add(startfrom.Automation.PatternLibrary.ValuePattern);
            cr.Add(startfrom.Automation.PatternLibrary.Text2Pattern);
            cr.Add(startfrom.Automation.PatternLibrary.SelectionPattern);
            cr.Add(startfrom.Automation.PatternLibrary.SelectionItemPattern);
            cr.Add(startfrom.Automation.PatternLibrary.TextPattern);
            cr.Add(startfrom.Automation.PatternLibrary.TextEditPattern);
            cr.Add(startfrom.Automation.PatternLibrary.LegacyIAccessiblePattern);
            //startfrom.Automation.PropertyLibrary.PatternAvailability.AllForCurrentFramework.ToList().ForEach(x => cr.Add(x));
            cr.TreeScope = treeScope;
            cr.TreeFilter = TrueCondition.Default;
            cr.Activate();
        }
        public virtual string FindElementWithPicPath(string strPicPath)
        {
            return "true";
        }

        public virtual string FindRelativeElementsInfo(string strRelativeSelector, ref JArray context)
        {
            return "";
        }

        public virtual IGuiNode FindRelativeNode(int position, int offsetX, int offsetY)
        {
            IGuiNode relativeNode = null;
            Rectangle rect = automationElement.BoundingRectangle;
            int posX, posY;
            Point point;
            switch (position)
            {
                case 0:     //中心位置偏移
                    posX = rect.Location.X + rect.Width / 2;
                    posY = rect.Location.Y + rect.Height / 2;
                    point = new Point(posX + offsetX, posY + offsetY);
                    break;

                case 1:     //左上角偏移
                    posX = rect.Location.X;
                    posY = rect.Location.Y;
                    point = new Point(posX + offsetX, posY + offsetY);
                    break;

                case 2:     //右上角偏移
                    posX = rect.Location.X + rect.Width;
                    posY = rect.Location.Y;
                    point = new Point(posX + offsetX, posY + offsetY);
                    break;

                case 3:     //左下角偏移
                    posX = rect.Location.X;
                    posY = rect.Location.Y + rect.Height;
                    point = new Point(posX + offsetX, posY + offsetY);
                    break;

                case 4:     //右下角偏移
                    posX = rect.Location.X + rect.Width;
                    posY = rect.Location.Y + rect.Height;
                    point = new Point(posX + offsetX, posY + offsetY);
                    break;

                default:    //默认中心偏移
                    posX = rect.Location.X + rect.Width / 2;
                    posY = rect.Location.Y + rect.Height / 2;
                    point = new Point(posX + offsetX, posY + offsetY);
                    break;
            }

            try
            {
                this.automationElement = this.UIAAutomation.FromPoint(point);
            }
            catch (Exception)
            {
                if (this.automationElement == null)
                {
                    IntPtr hWnd = UiCommon.WindowFromPoint(point);
                    if (hWnd != IntPtr.Zero)
                    {
                        this.automationElement = this.UIAAutomation.FromHandle(hWnd);
                    }
                }
            }
            relativeNode = new UIANode(automationElement);
            return relativeNode;
        }

        public virtual void Focus()
        {
            automationElement.Focus();
            logger.LogDebug("获取焦点成功");
        }

        public virtual IGuiNode GetChildByIdx(int idx)
        {
            var item = automationElement.FindChildAt(idx);
            return new UIANode(item, this);
        }

        public virtual Point GetClickablePoint()
        {
            // TMS 虚拟分段：优先使用虚拟项屏幕边界中心
            if (_tmsVirtualBounds.HasValue)
            {
                var b = _tmsVirtualBounds.Value;
                if (b.Width > 0 && b.Height > 0)
                    return new Point(b.Left + b.Width / 2, b.Top + b.Height / 2);
            }
            //try
            //{
            //    return automationElement.GetClickablePoint();
            //}
            //catch (Exception)
            //{
            //取矩形中心点
            logger.LogDebug("通过获取矩形中心点模拟");
            return new Point(BoundingRectangle.Left + BoundingRectangle.Width / 2,
                                BoundingRectangle.Top + BoundingRectangle.Height / 2);
            //}
        }

        public virtual string GetElementAttribute(string name, string apiVersion)
        {
            try
            {
                var properties = automationElement.GetSupportedPropertiesDirect();
                foreach (var property in properties)
                {
                    if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        if (automationElement.FrameworkAutomationElement.TryGetPropertyValue(property, out object value))
                        {
                            if (value != null)
                                return value.ToString();
                        }
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
            return null;

        }

        public virtual string GetElementCheck()
        {
            string result = string.Empty;
            try
            {
                // automationElement.GetSupportedPatterns();
                var allSupportedPatterns = automationElement.Automation.PatternLibrary.AllForCurrentFramework;//为啥这么写 暂未定位到原因
                if (automationElement.ControlType.ToString().ToLower().Contains("radiobutton"))
                {
                    var rdb = automationElement.AsRadioButton();
                    if (rdb != null)
                    {
                        result = rdb.IsChecked.ToString();
                    }
                }
                else if (automationElement.ControlType.ToString().ToLower().Contains("checkbox"))
                {
                    var chb = automationElement.AsCheckBox();
                    if (chb != null)
                    {
                        result = chb.IsChecked.ToString();
                    }
                }
                else
                {
                    throw new WindowsException(eErrorMessageType.Error_ElementNotOperation);
                }
            }
            catch (Exception e)
            {
                logger.LogInformation($"GetElementCheck实际元素类型为：{automationElement.ControlType}");
                throw new WindowsException(eErrorMessageType.Error_GetSelectError, e);
            }
            return result;
        }

        public int GetElementIndexInParent(AutomationElement parent, AutomationElement element, ref WindowsSelectorItem item)
        {
            int indexInParent = -1;
            int skipCount = 0;
            try
            {
                var childs = parent.CachedChildren;
                int idx = -1;
                if (childs.Length > 0)
                {
                    for (int x = 0; x < childs.Length; x++)
                    {
                        idx++;
                        if (element.Equals(childs[x]))
                        {
                            indexInParent = x;
                            break;
                        }
                        else if (childs[x].Properties.ControlType.IsSupported && childs[x].ControlType == FlaUI.Core.Definitions.ControlType.ToolTip)
                        {
                            skipCount++;
                            continue;
                        }
                        else if (WinConfigurations.Info.FilterHiddenForIndex
                            && childs[x].Properties.IsOffscreen.TryGetValue(out var isOff) && isOff)
                        {
                            skipCount++;
                            continue;
                        }
                    }
                }
                else
                {
                    var treeWorker = UIAAutomation.TreeWalkerFactory.GetRawViewWalker();
                    parent = treeWorker.GetParent(element);
                    var chileEle = treeWorker.GetFirstChild(parent);
                    while (chileEle != null)
                    {
                        idx++;
                        if (element.Equals(chileEle))
                        {
                            indexInParent = idx;
                            break;
                        }
                        else if (chileEle.Properties.ControlType.IsSupported && chileEle.ControlType == FlaUI.Core.Definitions.ControlType.ToolTip)
                        {
                            skipCount++;
                        }
                        else if (WinConfigurations.Info.FilterHiddenForIndex
                            && chileEle.Properties.IsOffscreen.TryGetValue(out var isOff) && isOff)
                        {
                            skipCount++;
                        }
                        chileEle = treeWorker.GetNextSibling(chileEle);
                    }
                }



                //var childs = parent.FindAllChildren();
                //var count = childs.Count();
                //for (int x = 0; x < count; x++)
                //{
                //    if (element.Equals(childs[x]))
                //    {
                //        indexInParent = x;
                //        break;
                //    }
                //    else if (childs[x].Properties.ControlType.IsSupported && childs[x].ControlType == FlaUI.Core.Definitions.ControlType.ToolTip)
                //    {
                //        skipCount++;
                //        continue;
                //    }
                //}
                indexInParent -= skipCount;
                //item.Properties.Add(new SelectorItemProperty(WindowsSelectorItem.INDEX, indexInParent.ToString()));
                if (indexInParent >= 0)
                {
                    bool isAccurate = true;
                    item.Properties.Add(new WinSelectorItemProperty(ConstFields.SELECTOR_INDEX_IN_PARENT, indexInParent.ToString()) { Accurate = isAccurate });
                }
                return indexInParent;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public virtual string GetElementSelect(int selectType = 1)
        {
            string result = string.Empty;
            if (!automationElement.ControlType.ToString().ToLower().Contains("combobox"))
            {
                logger.LogInformation($"GetElementSelect实际元素类型为：{automationElement.ControlType}");
                throw new WindowsException(eErrorMessageType.Error_ElementNotOperation);
            }
            try
            {
                var cbo = this.automationElement.AsComboBox();

                if (cbo != null)
                {
                    result = cbo.EditableText.ToString();
                }
            }
            catch (Exception ex)
            {
                throw new WindowsException(eErrorMessageType.Error_OperationFail, ex);
            }
            return result;
        }

        public virtual string GetElementSelectEx(string strJson)
        {
            logger.LogDebug($"Windows GetElementSelectEx param:{strJson}");
            //chromeTab.IsActive();
            int selectMode = 0, selectType = 1;

            GetElementSelectExParamModel _GetElementSelectExParam = null;
            try
            {
                _GetElementSelectExParam = JsonConvert.DeserializeObject<GetElementSelectExParamModel>(strJson);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"Windows GetElementSelectEx param Err. Json:{strJson}");
                throw new WindowsException(eErrorMessageType.Error_ParamsError);
            }
            finally
            {
                if (_GetElementSelectExParam == null || _GetElementSelectExParam.selectMode == -1 || _GetElementSelectExParam.selectType == -1)
                {
                    logger.LogError($"Windows GetElementSelectEx param Err. Json:{strJson}");
                    throw new WindowsException(eErrorMessageType.Error_ParamsError);
                }
                selectMode = _GetElementSelectExParam.selectMode;
                selectType = _GetElementSelectExParam.selectType;
            }

            string strContent = string.Empty;

            logger.LogDebug($"GetElementSelectEx:selectMode--{selectMode} selectType{selectType}");
            try
            {
                if (!automationElement.ControlType.ToString().ToLower().Contains("combobox"))
                {
                    logger.LogInformation($"GetElementSelect实际元素类型为：{automationElement.ControlType}");
                    throw new WindowsException(eErrorMessageType.Error_ElementNotOperation);
                }

                var cbo = this.automationElement.AsComboBox();

                if (cbo == null)
                {
                    //result = cbo.EditableText.ToString();
                    throw new WindowsException(eErrorMessageType.Error_NotFoundElement);
                }

                if (selectMode == 0)//获取当前下拉选项
                {
                    string strCurrentText = string.Empty;
                    switch (selectType)
                    {
                        case 0:
                            int _index = -1;
                            try
                            {
                                strCurrentText = cbo.EditableText;
                            }
                            catch (Exception ex)
                            {
                                logger.LogError("EditableText", ex);
                                try
                                {
                                    strCurrentText = cbo.Value;
                                }
                                catch (Exception valueex)
                                {
                                    logger.LogError("vaule", valueex);
                                    try
                                    {
                                        if (cbo.FrameworkType == FrameworkType.Wpf)
                                        {
                                            strCurrentText = cbo.SelectedItem.Text;
                                        }
                                    }
                                    catch (Exception wpfex)
                                    {
                                        logger.LogError("wpfex", wpfex);
                                    }
                                }
                            }
                            if (!string.IsNullOrEmpty(strCurrentText))
                            {
                                for (int i = 0; i < cbo.Items.Length; i++)
                                {
                                    if (strCurrentText.ToString() == cbo.Items[i].Text)
                                    {
                                        _index = i;
                                        break;
                                    }
                                }
                            }
                            var IndexItem = new JsonArrayClass(_index);
                            strContent = IndexItem.ToJson();
                            break;
                        case 1:
                        case 2:
                            try
                            {
                                strCurrentText = cbo.EditableText.ToString();
                            }
                            catch (Exception ex)
                            {
                                logger.LogError("EditableText", ex);
                                try
                                {
                                    strCurrentText = cbo.Value;
                                }
                                catch (Exception valueex)
                                {
                                    logger.LogError("vaule", valueex);
                                    try
                                    {
                                        if (cbo.FrameworkType == FrameworkType.Wpf)
                                        {
                                            strCurrentText = cbo.SelectedItem.Text;
                                        }
                                    }
                                    catch (Exception wpfex)
                                    {
                                        logger.LogError("wpfex", wpfex);
                                    }
                                }
                            }

                            var TextItem = new JsonArrayClass(strCurrentText);
                            strContent = TextItem.ToJson();
                            break;
                    }
                }
                else //获取全部下拉选项
                {
                    switch (selectType)
                    {
                        case 0:
                            int? _length = cbo?.Items?.Length;
                            if ((int)_length < 1)
                            {
                                var IndexItem = new JsonArrayClass(new List<int>());
                                strContent = IndexItem.ToJson();
                            }
                            else
                            {
                                var IndexItem = new JsonArrayClass(Enumerable.Range(0, (int)_length).ToList());
                                strContent = IndexItem.ToJson();
                            }
                            break;
                        case 1:
                        case 2:
                            int? iLength = cbo?.Items?.Length;
                            if ((int)iLength < 1)
                            {
                                var IndexItem = new JsonArrayClass(new List<string>());
                                strContent = IndexItem.ToJson();
                            }
                            else
                            {
                                List<string> TextList = new List<string>();
                                for (int i = 0; i < cbo.Items.Length; i++)
                                {
                                    ComboBoxItem element = cbo.Items[i] as ComboBoxItem;
                                    TextList.Add(element.Text);
                                }
                                var TextItem = new JsonArrayClass(TextList);
                                strContent = TextItem.ToJson();
                            }

                            break;
                    }
                }
                cbo?.Collapse();
            }
            catch (RPAException rpaEx)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, rpaEx, $"WindowsException GetElementSelectEx Exception");
                throw rpaEx;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"Windows GetElementSelectEx Exception");
                throw new WindowsException(eErrorMessageType.Error_GetSelectError);
            }

            return strContent;
        }

        public virtual string GetEnableClick()
        {
            try
            {
                return automationElement.IsEnabled.ToString();
            }
            catch (Exception e)
            {
                throw new WindowsException(eErrorMessageType.Error_ElementIsEnable, e);
            }
        }

        public string GetHTML()
        {
            return "";
        }

        public virtual Rectangle GetRegion(int positionType)
        {
            if (positionType == 1)
            {
                return new Rectangle();
            }
            else if (positionType == 2)
            {
                RelativePosition(this);
                return new Rectangle(this.BoundingRectangle.X - rect.X, this.BoundingRectangle.Y - rect.Y, this.BoundingRectangle.Width, this.BoundingRectangle.Height);
            }
            else if (positionType == 3)
            {
                return this.BoundingRectangle;
            }
            else
            {
                return this.BoundingRectangle;
            }
        }

        enum OBJID : uint
        {
            OBJID_CLIENT = 0xFFFFFFFC,
        }
        [DllImport("oleacc.dll")]
        static extern int AccessibleObjectFromWindow(IntPtr hwnd, uint id, ref Guid iid, [In, Out, MarshalAs(UnmanagedType.IUnknown)] ref object ppvObject);

        //https://chromium.googlesource.com/chromium/src/+/refs/heads/main/docs/accessibility/browser/ia2_to_uia.md
        static IUIAutomationRegistrar registrar;
        static int uia_unique_id_property_id;
        static int GetUniqueIdPropertyId()
        {
            int uia_unique_id_property_id = 0;

            try
            {
                Guid UiaPropertyUniqueIdGuid = new Guid("cc7eeb32-4b62-4f4c-aff6-1c2e5752ad8e");
                registrar = (IUIAutomationRegistrar)new CUIAutomationRegistrar();

                UIAutomationPropertyInfo unique_id_property_info = new UIAutomationPropertyInfo
                {
                    guid = UiaPropertyUniqueIdGuid,
                    pProgrammaticName = "UniqueId",
                    type = UIAutomationType.UIAutomationType_String
                };

                int hr = registrar.RegisterProperty(ref unique_id_property_info, out uia_unique_id_property_id);
                if (hr != 0)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to register property. HRESULT: " + hr);
                    return 0;
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("an error occurs: {0}", e);
            }
            return uia_unique_id_property_id;
        }
        static IItemContainerProvider ContainerFromWndEleClient(AutomationElement testElement)
        {
            IItemContainerProvider itemContainer = null;

            try
            {
                var controlType = testElement.Properties.ControlType.ValueOrDefault;
                if (controlType == FlaUI.Core.Definitions.ControlType.Window || controlType == FlaUI.Core.Definitions.ControlType.Pane)
                {
                    IntPtr handle = testElement.GetWindowHandle();
                    if (handle != IntPtr.Zero)
                    {
                        object objWnd = null;
                        var guid = typeof(IAccessibleInternal).GUID;
                        AccessibleObjectFromWindow(handle, (uint)OBJID.OBJID_CLIENT, ref guid, ref objWnd);
                        if (objWnd != null)
                        {
                            IServiceProvider sp = (IServiceProvider)objWnd;
                            if (sp != null)
                            {
                                IAccessible2 acc2;
                                {
                                    object obj;
                                    sp.QueryService(typeof(IAccessible2).GUID, typeof(IAccessible2).GUID, out obj);
                                    acc2 = obj as IAccessible2;
                                }
                                if (acc2 != null)
                                {
                                    if (uia_unique_id_property_id == 0)
                                    {
                                        uia_unique_id_property_id = GetUniqueIdPropertyId();
                                        System.Diagnostics.Debug.Assert(uia_unique_id_property_id != 0);
                                    }

                                    IRawElementProviderFragment pf = acc2 as IRawElementProviderFragment;
                                    if (pf != null)
                                    {
                                        IRawElementProviderFragmentRoot pfr = pf.FragmentRoot;//ACC实现时这句会抛异常
                                        if (pfr != null)
                                        {
                                            //.NET把IRawElementProviderFragmentRoot的基类搞错了，实际上是IUnknown，所以下面这个转换不能省略
                                            IRawElementProviderSimple ps = pfr as IRawElementProviderSimple;
                                            if (ps != null)
                                            {
                                                const int UIA_ItemContainerPatternId = 10019;
                                                object cp_Unk = ps.GetPatternProvider(UIA_ItemContainerPatternId);
                                                if (cp_Unk != null)
                                                {
                                                    itemContainer = cp_Unk as IItemContainerProvider;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (COMException ea)
            {
                System.Diagnostics.Debug.WriteLine("an error occurs: {0}", ea);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("an error occurs: {0}", e);
            }

            return itemContainer;
        }
        public static Accessibility.IAccessible ConvertToAccByItemContainer(IItemContainerProvider itemContainer, AutomationElement element)
        {
            if (itemContainer == null)
            {
                var cur = element;//FindItemByProperty是支持自己查找自己的，而不单单是查找子孙节点，因此可以我们可以从当前节点开始遍历
                while (cur != null)
                {
                    itemContainer = ContainerFromWndEleClient(cur);
                    if (itemContainer != null)
                    {
                        break;
                    }
                    cur = cur.Parent;
                }
            }
            if (itemContainer == null)
            {
                return null;
            }

            Accessibility.IAccessible acc = null;

            var nativeEle = element.ToNative();
            if (nativeEle != null)
            {
                object uniqueIdValue = nativeEle.GetCurrentPropertyValue(uia_unique_id_property_id);
                string strUniqueIdValue = uniqueIdValue as string;
                if (string.IsNullOrEmpty(strUniqueIdValue))
                {
                    IRawElementProviderSimple psFound = itemContainer.FindItemByProperty(null, uia_unique_id_property_id, uniqueIdValue);
                    uniqueIdValue = nativeEle.GetCurrentPropertyValue(uia_unique_id_property_id);
                    strUniqueIdValue = uniqueIdValue as string;
                    System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(strUniqueIdValue) || element.Properties.NativeWindowHandle.ValueOrDefault != IntPtr.Zero);
                }
                if (!string.IsNullOrEmpty(strUniqueIdValue))
                {
                    IRawElementProviderSimple psFound = itemContainer.FindItemByProperty(null, uia_unique_id_property_id, uniqueIdValue);
                    if (psFound != null)
                    {
                        acc = psFound as Accessibility.IAccessible;
                        System.Diagnostics.Debug.Assert(acc != null);
                    }
                }
            }

            return acc;
        }
        //闲得蛋疼的同学也可以尝试把上面两个函数用UIA客户端API实现出来，不过感觉大概率可能性会查不出来

        public string BuildRelativeSelector(AutomationElement rootElement, bool isEnableParentRebuild = true)
        {
            Stopwatch sw1 = Stopwatch.StartNew();
            logger.LogDebug("start BuildRelativeSelector");

            if (rootElement == null || this.automationElement == null)
            {
                logger.LogWarning("Chrome表格相对selector构建失败：根元素或目标元素为空");
                return null;
            }

            if (this.automationElement.Equals(rootElement))
            {
                logger.LogDebug("Chrome表格相对selector为空：目标元素就是根元素");
                return "[]";
            }

            List<WindowsSelectorItem> validSelectorItems = new List<WindowsSelectorItem>();
            var ancestorSet = new List<AutomationElement>();
            var currentElement = this.automationElement;
            while (currentElement != null && !rootElement.Equals(currentElement))
            {
                ancestorSet.Add(currentElement);
                currentElement = currentElement.Parent;
            }

            if (currentElement == null)
            {
                sw1.Stop();
                logger.LogWarning($"Chrome表格相对selector构建失败：根元素不是目标元素的祖先，耗时：{sw1.ElapsedMilliseconds}");
                return null;
            }

            ancestorSet.Reverse();
            var rootInclusivePath = new List<AutomationElement>(ancestorSet.Count + 1) { rootElement };
            rootInclusivePath.AddRange(ancestorSet);

            logger.LogDebug("正向获取获取元素链路");
            var parentChainDic = GetElementChain(rootInclusivePath, rootElement);
            if (parentChainDic == null)
            {
                logger.LogDebug("Chrome表格相对selector正向链路获取失败，尝试逆向获取元素链路");
                parentChainDic = ReverseGetElementChain(rootInclusivePath);

                //逆向链路包含rootElement，而tableSelector已经包含该节点，需要移除避免重复
                if (parentChainDic.Count == 0 || !rootElement.Equals(parentChainDic[0].Value))
                {
                    sw1.Stop();
                    logger.LogWarning($"Chrome表格相对selector逆向链路无效，耗时：{sw1.ElapsedMilliseconds}");
                    return null;
                }
                parentChainDic.RemoveAt(0);
            }

            if (parentChainDic.Count != ancestorSet.Count
                || parentChainDic.Count == 0
                || !this.automationElement.Equals(parentChainDic[parentChainDic.Count - 1].Value))
            {
                sw1.Stop();
                logger.LogWarning($"Chrome表格相对selector元素链不完整，耗时：{sw1.ElapsedMilliseconds}");
                return null;
            }

            IItemContainerProvider itemContainer = null;
            for (int i = 0; i < parentChainDic.Count; i++)
            {
                var pathItem = parentChainDic[i].Value;

                var nearest_wnd_container = ContainerFromWndEleClient(pathItem);
                if (nearest_wnd_container != null)
                {
                    itemContainer = nearest_wnd_container;
                }
                var item = new WindowsSelectorItem(pathItem, itemContainer, i, parentChainDic[i].Key);
                validSelectorItems.Add(item);
            }
            //不启用网页元素的class属性
            DisabledDocuemntElementsClassProp(validSelectorItems);
            if (isEnableParentRebuild)
            {
                var newSelectorItems = EnableIndexProp(validSelectorItems, new List<IntPtr>(), new List<AutomationElement> { rootElement }, true);
                if (newSelectorItems == null && this.automationElement_uia_bak != null)
                {
                    logger.LogDebug($"uiaUIANode_by_acc buildSelector失败");
                    if ((this.ControlType ?? "").ToLower() == "text" && isEnableParentRebuild)
                    {
                        return new UIANode(this.automationElement.Parent).BuildRelativeSelector(rootElement, false);
                    }
                }
            }
            else
            {
                validSelectorItems.ForEach(item =>
                {
                    var idxProp = item.GetProperty(ConstFields.SELECTOR_INDEX_IN_PARENT);
                    if (idxProp != null)
                    {
                        idxProp.Accurate = true;
                    }
                });
            }
            EnableChromeTableCellIndexProp(validSelectorItems, rootElement);
            this.AddRange(validSelectorItems);
            string selecorStr = this.ToString();
            sw1.Stop();
            logger.LogDebug($"end BuildRelativeSelector,耗时：{sw1.ElapsedMilliseconds}");
            return selecorStr;
        }

        /// <summary>
        /// Chrome/CEF页面表格的相对selector中，自动启用末级单元格的列索引属性。
        /// </summary>
        private void EnableChromeTableCellIndexProp(List<WindowsSelectorItem> selectorItems, AutomationElement rootElement)
        {
            if (selectorItems == null
                || selectorItems.Count == 0
                || rootElement == null
                || this.automationElement == null)
            {
                return;
            }

            try
            {
                if (!rootElement.IsPageTable() || !this.automationElement.IsPageTableCell())
                {
                    return;
                }

                var targetItem = selectorItems[selectorItems.Count - 1];
                var cellIndexProp = targetItem.GetProperty(ChromeTableCellIndexProperty);
                if (cellIndexProp == null
                    || !int.TryParse(cellIndexProp.Value?.ToString(), out var cellIndex)
                    || cellIndex < 0)
                {
                    return;
                }

                cellIndexProp.Accurate = true;
                logger.LogDebug($"Chrome表格单元格自动启用table-cell-index:{cellIndex}");
            }
            catch (Exception ex)
            {
                //UIA树可能在拾取后失效，该增强失败时保持属性默认不启用
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "Chrome表格单元格启用table-cell-index失败，保持原selector属性状态");
            }
        }

        public string BuildSelector(bool isEnableParentRebuild = true)
        {
            Stopwatch sw1 = Stopwatch.StartNew();
            var element = automationElement;
            AutomationElement elementParent = element;
            logger.LogDebug("start GetSelector");

            var process = element.GetProcess();
            bool isScanElementTreeApps = WinConfigurations.Info.ScanElementTreeApps.Contains(process.ProcessName, StringComparer.OrdinalIgnoreCase);
            //bool isAccEnablerApps = AccEnabler.IsAccEnablerApps(element);
            string pVersion = process.GetProductVersion();
            bool isUseScanCacheApps = WinConfigurations.Info.SelectorUseScanCacheAppsInfo.Values.Any(info =>
                info.processName.EqualIgnoreCase(process.ProcessName) && RPAUtils.IsVersionInRange(pVersion, info.MinVersion, info.MaxVersion)
            );
            var pathToRoot = GetPathToRoot(element, isUseScanCacheApps);

            (List<WindowsSelectorItem> validSelectorItems, List<IntPtr> hwndList, List<AutomationElement> newPathToRoot) = SelectorBuilderHelper.BuildWindowSelector(pathToRoot);

            logger.LogDebug("GetCorrectParent");
            List<KeyValuePair<int, AutomationElement>> parentChainDic = null;
            if (CacheRequest.IsCachingActive)
            {
                logger.LogDebug("通过缓存的方式获取元素链路");
                parentChainDic = GetElementChainByCache(newPathToRoot);
            }
            else if (!isScanElementTreeApps)
            {
                logger.LogDebug("正向获取获取元素链路");
                parentChainDic = GetElementChain(newPathToRoot);
            }
            if (parentChainDic == null)
            {
                logger.LogDebug("逆向获取获取元素链路");
                parentChainDic = ReverseGetElementChain(newPathToRoot);
            }
            logger.LogDebug("元素链获取完成");
            Clear();

            IItemContainerProvider itemContainer = null;
            var itemCount = parentChainDic.Count();
            var startIdx = validSelectorItems.Count > 0 ? 1 : 0;
            for (int i = startIdx; i < itemCount; i++)
            {
                var pathItem = parentChainDic[i].Value;
                WindowsSelectorItem item;
                var keyVal = CacheData.NodeSelectorPropsCatch.FirstOrDefault(ele => ele.Key.Equals(pathItem));
                if (keyVal.Value != null)
                {
                    item = keyVal.Value;
                    validSelectorItems.Add(keyVal.Value);
                    continue;
                }

                var nearest_wnd_container = ContainerFromWndEleClient(pathItem);
                if (nearest_wnd_container != null)
                {
                    itemContainer = nearest_wnd_container;
                }
                item = new WindowsSelectorItem(pathItem, itemContainer, i, parentChainDic[i].Key);

                foreach (var p in item.Properties)
                {
                    int idx1 = p.Value.IndexOf(".");
                    if (p.Name.Equals(ConstFields.SELECTOR_CLASS_NAME, StringComparison.OrdinalIgnoreCase) && idx1 > -1)
                    {
                        int idx2 = p.Value.IndexOf(".", idx1 + 1);
                        if (idx2 > idx1)
                        {
                            ((WinSelectorItemProperty)p).Accurate = false;
                            //p.Enabled = false; //对于此类的className会动态变化，不能用来做标识；
                        }
                    }
                }
                if (CacheData.IsCanUpdateNodeSelectorPropsCatch)
                {
                    CacheData.NodeSelectorPropsCatch[pathItem] = item;
                }
                validSelectorItems.Add(item);
            }


            //TODO 一旦完整支持正则表达式将会移除
            //this.ProcessSetAccurate(validSelectorItems);
            //通过扫描元素树得到的元素，不需要使用QuerySelectorAll校验同结构元素
            //实际上 都不需要，只是为了与老版本场景效果保持一致  避免出现版本差异
            DisabledDocuemntElementsClassProp(validSelectorItems);

            // 检测是否应启用 BoundingRect 坐标辅助定位。
            // 只有同一父节点下存在大量“ControlType 相同，且 Selector 中有效身份属性也无法区分”的
            // 兄弟元素时才启用。仅有大量同 ControlType 元素、或父容器子元素很多，并不能证明坐标
            // 是稳定的元素身份（例如钉钉等可滚动列表中的 ListItem）。
            bool shouldUseBoundingRect = false;
            if (parentChainDic.Count >= 2)
            {
                try
                {
                    var targetParent = parentChainDic[parentChainDic.Count - 2].Value;
                    var targetElement = parentChainDic[parentChainDic.Count - 1].Value;
                    var targetItem = validSelectorItems.LastOrDefault();
                    var identityProperties = GetBoundingRectIdentityProperties(targetItem);
                    int totalChildCount = 0;
                    int sameControlTypeCount = 0;
                    int indistinguishableSiblingCount = 0;
                    var targetControlType = default(FlaUI.Core.Definitions.ControlType);

                    try { targetElement.Properties.ControlType.TryGetValue(out targetControlType); } catch { }

                    if (CacheRequest.IsCachingActive)
                    {
                        totalChildCount = targetParent.CachedChildren.Length;
                        try
                        {
                            foreach (var cached in targetParent.CachedChildren)
                            {
                                if (cached.Properties.ControlType.TryGetValue(out var ct) && ct == targetControlType)
                                {
                                    sameControlTypeCount++;
                                    if (identityProperties.Count > 0
                                        && MatchBoundingRectIdentityProperties(cached, identityProperties))
                                    {
                                        indistinguishableSiblingCount++;
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                    else
                    {
                        var rawWalker = UIAAutomation.TreeWalkerFactory.GetRawViewWalker();
                        var rawChild = rawWalker.GetFirstChild(targetParent);
                        while (rawChild != null)
                        {
                            totalChildCount++;
                            try
                            {
                                if (rawChild.Properties.ControlType.TryGetValue(out var ct) && ct == targetControlType)
                                {
                                    sameControlTypeCount++;
                                    if (identityProperties.Count > 0
                                        && MatchBoundingRectIdentityProperties(rawChild, identityProperties))
                                    {
                                        indistinguishableSiblingCount++;
                                    }
                                }
                            }
                            catch { }
                            rawChild = rawWalker.GetNextSibling(rawChild);
                        }
                    }

                    shouldUseBoundingRect = identityProperties.Count > 0
                        && indistinguishableSiblingCount > 10;

                    if (shouldUseBoundingRect)
                    {
                        logger.LogDebug($"目标父节点: 总子元素={totalChildCount}, 同ControlType={sameControlTypeCount}, 有效身份属性={identityProperties.Count}, 同结构候选={indistinguishableSiblingCount} → 启用坐标辅助定位");
                    }
                    else
                    {
                        logger.LogDebug($"目标父节点: 总子元素={totalChildCount}, 同ControlType={sameControlTypeCount}, 有效身份属性={identityProperties.Count}, 同结构候选={indistinguishableSiblingCount} → 不启用坐标辅助定位");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogDebug($"BoundingRect触发条件检测异常: {ex.Message}");
                }
            }

            if (isEnableParentRebuild)
            {
                if (shouldUseBoundingRect)
                {
                    // 为最深层的目标元素添加坐标辅助定位属性
                    // 当同一父节点下有大量同属性元素时（如WPF数据绑定的Edit/Text），
                    // Index可能因视图重建而漂移，BoundingRect提供稳定的坐标参照
                    if (parentChainDic.Count >= 1 && validSelectorItems.Count >= 1)
                    {
                        var targetElement = parentChainDic[parentChainDic.Count - 1].Value;
                        var targetItem = validSelectorItems[validSelectorItems.Count - 1];
                        try
                        {
                            if (targetElement.Properties.BoundingRectangle.TryGetValue(out var rect) && !rect.IsEmpty)
                            {
                                // 使用窗口相对坐标。
                                // 窗口相对坐标在窗口未改变大小时保持稳定，不受窗口拖动影响，
                                // 也与"获取元素位置"组件返回的坐标一致，便于手动填写boundingrect。
                                int relX = rect.X, relY = rect.Y;
                                if (parentChainDic.Count >= 1)
                                {
                                    var windowElement = parentChainDic[0].Value;
                                    if (windowElement.Properties.BoundingRectangle.TryGetValue(out var windowRect) && !windowRect.IsEmpty)
                                    {
                                        relX = rect.X - windowRect.X;
                                        relY = rect.Y - windowRect.Y;
                                    }
                                }
                                targetItem.Properties.Add(new WinSelectorItemProperty(
                                    ConstFields.SELECTOR_BOUNDING_RECT,
                                    $"{relX},{relY},{rect.Width},{rect.Height}")
                                { Accurate = true, Enabled = true });
                                logger.LogDebug($"添加坐标辅助定位: boundingrect={relX},{relY},{rect.Width},{rect.Height} (窗口相对坐标)");
                            }
                        }
                        catch { }
                    }
                    validSelectorItems = EnableAllIndexProp(validSelectorItems);
                }
                else
                {
                    var newSelectorItems = EnableIndexProp(validSelectorItems, hwndList, pathToRoot, useQuerySelectorAll: !isScanElementTreeApps);
                    if (newSelectorItems == null && this.automationElement_uia_bak != null)
                    {
                        sw1.Stop();
                        logger.LogDebug($"uiaUIANode_by_acc buildSelector失败,耗时：{sw1.ElapsedMilliseconds}");
                        if ((this.ControlType ?? "").ToLower() == "text" && isEnableParentRebuild)
                        {
                            return new UIANode(this.automationElement.Parent).BuildSelector(false);
                        }
                    }
                }
            }
            else
            {
                validSelectorItems.ForEach(item =>
                {
                    var idxProp = item.GetProperty(ConstFields.SELECTOR_INDEX_IN_PARENT);
                    if (idxProp != null)
                    {
                        idxProp.Accurate = true;
                    }
                });
            }
            // TMS 虚拟面板：将虚拟项屏幕边界注入最末级 SelectorProps
            if (_tmsVirtualBounds.HasValue && validSelectorItems.Count > 0)
            {
                var b = _tmsVirtualBounds.Value;
                var leafItem = validSelectorItems[validSelectorItems.Count - 1];
                leafItem.Properties.Add(new WinSelectorItemProperty(
                    TmsPanelHelper.PROP_TMS_BOUNDS,
                    $"Screen:{b.X},{b.Y},{b.Width},{b.Height};tolerance=12")
                { Accurate = true, Enabled = true });
            }
            this.AddRange(validSelectorItems);
            if (CacheData.IsCanUpdateNodeSelectorPropsCatch)//节点缓存更新只跟新一次
            {
                CacheData.IsCanUpdateNodeSelectorPropsCatch = false;
            }
            string selecorStr = this.ToString();
            sw1.Stop();
            logger.LogDebug($"end GetSelector,耗时：{sw1.ElapsedMilliseconds}");
            return selecorStr;
        }

        /// <summary>
        /// 获取可用于判断兄弟元素是否真正无法区分的身份属性。
        /// Index、BoundingRect 等位置属性不属于元素身份；没有任何有效身份属性时不自动启用坐标。
        /// </summary>
        private static List<WinSelectorItemProperty> GetBoundingRectIdentityProperties(WindowsSelectorItem targetItem)
        {
            if (targetItem == null)
            {
                return new List<WinSelectorItemProperty>();
            }

            return targetItem.Properties
                .OfType<WinSelectorItemProperty>()
                .Where(property => property.Enabled
                    && property.Accurate
                    && !string.IsNullOrEmpty(property.Value)
                    && (property.Name.EqualIgnoreCase(ConstFields.SELECTOR_NAME)
                        || property.Name.EqualIgnoreCase(ConstFields.SELECTOR_CLASS_NAME)
                        || property.Name.EqualIgnoreCase(ConstFields.SELECTOR_AUTOMATION_ID)))
                .ToList();
        }

        /// <summary>
        /// 使用与运行时 Selector 一致的属性匹配规则，判断候选元素是否与目标结构属性相同。
        /// </summary>
        private static bool MatchBoundingRectIdentityProperties(
            AutomationElement candidate,
            List<WinSelectorItemProperty> identityProperties)
        {
            if (candidate == null || identityProperties == null || identityProperties.Count == 0)
            {
                return false;
            }

            try
            {
                foreach (var property in identityProperties)
                {
                    if (property.Name.EqualIgnoreCase(ConstFields.SELECTOR_NAME))
                    {
                        if (!candidate.Properties.Name.TryGetValue(out var name)
                            || !property.ConiditionMatch(name))
                        {
                            return false;
                        }
                    }
                    else if (property.Name.EqualIgnoreCase(ConstFields.SELECTOR_CLASS_NAME))
                    {
                        if (!candidate.Properties.ClassName.TryGetValue(out var className)
                            || !property.ConiditionMatch(className))
                        {
                            return false;
                        }
                    }
                    else if (property.Name.EqualIgnoreCase(ConstFields.SELECTOR_AUTOMATION_ID))
                    {
                        if (!candidate.Properties.AutomationId.TryGetValue(out var automationId)
                            || !property.ConiditionMatch(automationId))
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private List<IGuiNode> FindOwnerDrawAll(OwnerDrawReplayRequest replay,
            IGuiNode fromElement, int maxResult, bool useCache)
        {
            if (replay == null || replay.Selector == null)
                return new List<IGuiNode>();

            AutomationElement[] fromElements;
            var selectors = this.OfType<WindowsSelectorItem>().ToList();
            if (selectors.Count == 0) return new List<IGuiNode>();

            // Remove only the synthetic leaf from the local copy.  Keeping this
            // Selector instance intact makes retries deterministic.
            selectors.RemoveAt(selectors.Count - 1);
            if (fromElement == null)
            {
                fromElements = CandidateMainWindows;
                if (fromElements != null && fromElements.Length > 0
                    && selectors.Count > 0)
                {
                    selectors.RemoveAt(0);
                }
            }
            else if (fromElement is UIANode fromNode
                && fromNode.automationElement != null)
            {
                fromElements = new[] { fromNode.automationElement };
            }
            else
            {
                return new List<IGuiNode>();
            }

            List<AutomationElement> hosts = QuerySelectorAll(selectors,
                fromElements, useCache);
            // Resolve may scroll a VCL grid.  Never invoke it against several
            // ambiguous host candidates and then pick whichever happens to
            // resolve successfully.
            if (hosts.Count != 1)
            {
                logger.LogWarning("Owner-draw selector matched host count={Count}; "
                    + "exactly one host is required before Bridge resolve.",
                    hosts.Count);
                return new List<IGuiNode>();
            }

            AutomationElement element = hosts[0];
            if (!element.Properties.NativeWindowHandle.TryGetValue(
                    out IntPtr hostHwnd) || hostHwnd == IntPtr.Zero)
            {
                logger.LogWarning("Owner-draw selector matched one host but its HWND "
                    + "is unavailable; adapter={Adapter} matchedHostCount=1.",
                    replay.Selector.AdapterId);
                return new List<IGuiNode>();
            }

            string hostHwndText = "0x" + hostHwnd.ToInt64().ToString("X");
            Stopwatch resolveWatch = Stopwatch.StartNew();
            OwnerDrawResult<OwnerDrawElementSnapshot> result
                = OwnerDrawAdapterRegistry.Shared.Resolve(hostHwnd,
                    replay.Selector);
            resolveWatch.Stop();
            if (!result.IsSuccess || result.Value == null)
            {
                logger.LogWarning("Owner-draw virtual element resolve failed; "
                    + "hwnd={Hwnd} adapter={Adapter} state={State} code={Code} "
                    + "status={Status} elapsedMs={ElapsedMs}.",
                    hostHwndText, replay.Selector.AdapterId, result.State,
                    result.Code, result.Status, resolveWatch.ElapsedMilliseconds);
                return new List<IGuiNode>();
            }

            if (result.Value.HostHwnd != hostHwnd)
            {
                logger.LogWarning("Owner-draw virtual element resolve returned a "
                    + "different host; hwnd={Hwnd} resolvedHwnd={ResolvedHwnd} "
                    + "adapter={Adapter} state={State} code={Code} status={Status} "
                    + "elapsedMs={ElapsedMs}.",
                    hostHwndText,
                    "0x" + result.Value.HostHwnd.ToInt64().ToString("X"),
                    replay.Selector.AdapterId, result.State, result.Code,
                    result.Status, resolveWatch.ElapsedMilliseconds);
                return new List<IGuiNode>();
            }

            logger.LogDebug("Owner-draw virtual element resolve succeeded; "
                + "hwnd={Hwnd} adapter={Adapter} state={State} code={Code} "
                + "status={Status} elapsedMs={ElapsedMs}.",
                hostHwndText, replay.Selector.AdapterId, result.State,
                result.Code, result.Status, resolveWatch.ElapsedMilliseconds);

            var host = new UIANode(element)
            {
                cacheDataSelector = SelectorContext?.SourceSelector,
                RootWindow = RootWindow,
            };
            var resolved = new List<IGuiNode>
            {
                new OwnerDrawVirtualElementNode(host, result.Value)
                {
                    cacheDataSelector = SelectorContext?.SourceSelector,
                },
            };
            return maxResult == 0 ? new List<IGuiNode>()
                : resolved.Take(maxResult > 0 ? maxResult : int.MaxValue).ToList();
        }

        private void JumpVerticalLayoutUINode(List<WindowsSelectorItem> validSelectorItems)
        {
            for (int i = validSelectorItems.Count - 1; i > 0; i--)
            {
                var windowsSelectorItem = (WindowsSelectorItem)validSelectorItems[i];
                var nameProp = windowsSelectorItem.GetProperty(ConstFields.SELECTOR_NAME);
                if (nameProp != null && nameProp.Value == ConstFields.SELECTOR_NAME_WXWORK_VERTICALLAYOUTUI)
                {
                    if (i - 1 < 0 || i + 1 > validSelectorItems.Count)
                    {
                        return;
                    }
                    var windowsSelectorItemPrev = (WindowsSelectorItem)validSelectorItems[i - 1];
                    var namePropPrev = windowsSelectorItemPrev.GetProperty(ConstFields.SELECTOR_NAME);
                    if (namePropPrev != null && namePropPrev.Value == ConstFields.SELECTOR_NAME_WXWORK_ROOTMENU)
                    {
                        validSelectorItems.Remove(windowsSelectorItem);
                    }
                    var windowsSelectorItemNext = (WindowsSelectorItem)validSelectorItems[i + 1];
                    var namePropNext = windowsSelectorItemNext.GetProperty(ConstFields.SELECTOR_NAME);
                    if (namePropPrev != null && namePropNext != null &&
                        ((namePropPrev.Value == ConstFields.SELECTOR_NAME_WXWORK_CHATLIST
                            && namePropNext.Value == ConstFields.SELECTOR_NAME_WXWORK_CHATITEM)
                        || (namePropPrev.Value == ConstFields.SELECTOR_NAME_WXWORK_SEARCH_MESSAGE_CHATLIST
                            && namePropNext.Value == ConstFields.SELECTOR_NAME_WXWORK_CHATITEM)
                        || (namePropPrev.Value == ConstFields.SELECTOR_NAME_WXWORK_EXTERNALS
                            && namePropNext.Value == ConstFields.SELECTOR_NAME_WXWORK_ITEM)
                        || (namePropPrev.Value == ConstFields.SELECTOR_NAME_WXWORK_COLLECTIONLIST
                            && namePropNext.Value == ConstFields.SELECTOR_NAME_WXWORK_LISTCONTAINERELEMENTUI)
                            )
                        )
                    {
                        validSelectorItems.Remove(windowsSelectorItem);
                    }
                }
            }
        }

        #region 不勾选Document节点下，所有的Class
        private void DisabledDocuemntElementsClassProp(List<WindowsSelectorItem> validSelectorItems)
        {
            if (!validSelectorItems.Any(item => item.ElementName.EqualIgnoreCase(ConstFields.SELECTOR_NAME_DOCUMENT)))
            {
                return;
            }
            for (int i = validSelectorItems.FindIndex(item => item.ElementName.EqualIgnoreCase(ConstFields.SELECTOR_NAME_DOCUMENT)); i < validSelectorItems.Count; i++)
            {
                var classProp = validSelectorItems[i].GetProperty(ConstFields.SELECTOR_CLASS_NAME);
                if (classProp != null)
                {
                    classProp.Accurate = false;
                }
            }
        }
        #endregion
        #region index属性启用
        private List<WindowsSelectorItem> EnableIndexProp(List<WindowsSelectorItem> validSelectorItems, List<IntPtr> hwndList, List<AutomationElement> pathToRoot, bool useQuerySelectorAll)
        {
            if (!WinConfigurations.Info.ValidateElementBeforeBuild)
            {
                return EnableAllIndexProp(validSelectorItems);
            }

            Stopwatch sw1 = Stopwatch.StartNew();
            logger.LogDebug($"启用index逻辑");
            try
            {
                if (useQuerySelectorAll)
                {
                    return EnableIndexProp(validSelectorItems, hwndList, pathToRoot);
                }
                else
                {
                    return EnableIndexProp_ScanElement(validSelectorItems, hwndList, pathToRoot);
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"启用index逻辑发生异常，如果是索引范围类型，大概率是正反两个方向获取的链路不匹配，将启用所有index属性");
                return EnableAllIndexProp(validSelectorItems);
            }
            finally
            {
                logger.LogDebug($"启用index逻辑结束，耗时：{sw1.ElapsedMilliseconds}");
                sw1.Stop();
            }
        }

        private static List<WindowsSelectorItem> EnableAllIndexProp(List<WindowsSelectorItem> validSelectorItems)
        {
            validSelectorItems.ForEach(item =>
            {
                var idxProp = item.GetProperty(ConstFields.SELECTOR_INDEX_IN_PARENT);
                if (idxProp != null)
                {
                    idxProp.Accurate = true;
                }
            });
            return validSelectorItems;
        }

        private List<WindowsSelectorItem> EnableIndexProp_ScanElement(List<WindowsSelectorItem> validSelectorItems, List<IntPtr> hwndList, List<AutomationElement> pathToRoot)
        {

            for (int i = 1; i < hwndList.Count; i++)
            {
                var fromWnd = UiCommon.UIAAutomation.FromHandle(hwndList[i - 1]);
                var matchingElements = validSelectorItems[i].Matches(fromWnd, false);
                if (matchingElements?.Length > 1)
                {
                    var idxProp = validSelectorItems[i].GetProperty(ConstFields.SELECTOR_INDEX_IN_PARENT);
                    if (idxProp != null)
                    {
                        idxProp.Accurate = true;
                    }
                }
            }

            for (int i = hwndList.Count; i < pathToRoot.Count; i++)
            {
                //var itemIndex = hwndList.Count + i;
                //if (hwndList.Count > 0) itemIndex--;
                var matchingElements = validSelectorItems[i].Matches(pathToRoot[i - 1], false);
                if (matchingElements?.Length > 1)
                {
                    var idxProp = validSelectorItems[i].GetProperty(ConstFields.SELECTOR_INDEX_IN_PARENT);
                    if (idxProp != null)
                    {
                        idxProp.Accurate = true;
                    }
                }
            }
            return validSelectorItems;
        }
        private List<WindowsSelectorItem> EnableIndexProp(List<WindowsSelectorItem> validSelectorItems, List<IntPtr> hwndList, List<AutomationElement> pathToRoot)
        {
            //using (CacheRequest.ForceNoCache())
            {
                var rootWnd = hwndList.Count > 0 ? UiCommon.UIAAutomation.FromHandle(hwndList[0]) : pathToRoot[0];
                var rootWnds= new AutomationElement[] { rootWnd };
                var elements = QuerySelectorAll(validSelectorItems.Skip(1).ToList(), rootWnds, useCache: CacheRequest.IsCachingActive, checkAccEnabler: false);
                if (elements?.Count == 0)
                {
                    logger.LogInformation($"拾取后selector检验 未获取到元素");
                    return null;
                }
                if (elements?.Count == 1 && elements[0].Equals(this.automationElement))
                {
                    return validSelectorItems;
                }
                logger.LogDebug($"获取到{elements?.Count}个同结构元素");
                List<AutomationElement> matchingElements = elements;
                AutomationElement pathElement = this.automationElement;
                for (int i = validSelectorItems.Count - 1; i > validSelectorItems.Count - pathToRoot.Count; i--)
                {
                    //逻辑上可以直接使用pathToRoot[i]，GetCorrectParent耗时不到1毫秒，两者皆可
                    var pathElementParent = GetCorrectParent(pathElement);
                    List<AutomationElement> matchingEleParents = new List<AutomationElement>();
                    foreach (var matchEle in matchingElements)
                    {
                        if (i == validSelectorItems.Count - 1 && matchEle.Equals(pathElement))//排除拾取链路中的元素
                        {
                            continue;
                        }
                        var matchEleParent = GetCorrectParent(matchEle);
                        if (pathElementParent.Equals(matchEleParent))
                        {
                            var idxProp = validSelectorItems[i].GetProperty(ConstFields.SELECTOR_INDEX_IN_PARENT);
                            if (idxProp != null)
                            {
                                idxProp.Accurate = true;
                            }
                        }
                        else if (matchingEleParents.IndexOf(matchEleParent) < 0)
                        {
                            matchingEleParents.Add(matchEleParent);
                        }
                    }
                    matchingElements = matchingEleParents;
                    pathElement = pathElementParent;
                }
                if (matchingElements.Count < 1)
                {
                    return validSelectorItems;
                }
                var matchingHwnds = matchingElements.Select(mEle => mEle.Properties.NativeWindowHandle.TryGetValue(out var eleHandle) ? eleHandle : IntPtr.Zero).ToList();
                matchingHwnds.RemoveAll(hwnd => hwnd == IntPtr.Zero);

                for (int i = hwndList.Count - 1; i >= 0; i--)
                {
                    var pathHwndParent = NativeMethods.GetParent(hwndList[i]);
                    List<IntPtr> matchingHwndParents = new List<IntPtr>();
                    foreach (var matchHwnd in matchingHwnds)
                    {
                        if (i == hwndList.Count - 1 && matchHwnd.Equals(pathHwndParent))//排除拾取链路中的元素
                        {
                            continue;
                        }
                        var matchHwndParent = NativeMethods.GetParent(matchHwnd);
                        if (pathHwndParent.Equals(matchHwndParent))
                        {
                            var idxProp = validSelectorItems[i].GetProperty(ConstFields.SELECTOR_INDEX_IN_PARENT);
                            if (idxProp != null)
                            {
                                idxProp.Accurate = true;
                            }
                        }
                        else if (matchingHwndParents.IndexOf(matchHwndParent) < 0)
                        {
                            matchingHwndParents.Add(matchHwndParent);
                        }
                    }
                }
                return validSelectorItems;
            }
        }

        #endregion
        private List<AutomationElement> GetPathToRoot(AutomationElement element, bool isUseScanCacheApps)
        {
            var pathToRoot = new List<AutomationElement>();
            AutomationElement elementParent = element;
            while (element != null)
            {
                try
                {
                    //遍历父元素获得整个控件链
                    elementParent = GetCorrectParent(element, isUseScanCacheApps);
                    if (elementParent == null
                        && element.ScanCachedParent != null
                        && pathToRoot.Count == 0
                        //此方案仅限button  
                        //&& element.ScanCachedParent.Properties.ControlType.TryGetValue(out var controlType)
                        //&& controlType == FlaUI.Core.Definitions.ControlType.Button
                        )
                    {
                        element = element.ScanCachedParent;
                        automationElement = element;
                        //pathToRoot.Clear();
                        continue;
                    }
                    if (elementParent != null || CacheRequest.IsCachingActive)
                    {
                        pathToRoot.Add(element);
                    }
                }
                catch (Exception)
                {
                    elementParent = null;//如果这段代码真的有异常抛出，不置为null则无法退出循环，如果期望抛出异常此处也未throw
                    logger.LogInformation("GetCorrectParent异常");
                }
                element = elementParent;
            }
            pathToRoot.Reverse();
            return pathToRoot;
        }

        #region 根据拾取元素的原始元素链 或者 最终生成selector的元素链（含idx）
        /// <summary>
        /// 通过缓存或者元素链
        /// </summary>
        /// <param name="originalChain"></param>
        /// <returns></returns>
        private List<KeyValuePair<int, AutomationElement>> GetElementChainByCache(List<AutomationElement> originalChain)
        {
            List<KeyValuePair<int, AutomationElement>> retElementChainList = new List<KeyValuePair<int, AutomationElement>>();
            retElementChainList.Add(new KeyValuePair<int, AutomationElement>(-1, originalChain[0]));
            for (int i = 1; i < originalChain.Count; i++)
            {
                int skipCount = 0;
                bool isMatched = false;
                var childs = originalChain[i - 1].CachedChildren;
                if (childs.Length > 0)
                {
                    for (int x = 0; x < childs.Length; x++)
                    {
                        if (originalChain[i].Equals(childs[x]))
                        {
                            retElementChainList.Add(new KeyValuePair<int, AutomationElement>(x - skipCount, childs[x]));
                            isMatched = true;
                            break;
                        }
                        else if (childs[x].Properties.ControlType.IsSupported && childs[x].ControlType == FlaUI.Core.Definitions.ControlType.ToolTip)
                        {
                            skipCount++;
                        }
                        else if (WinConfigurations.Info.FilterHiddenForIndex
                            && childs[x].Properties.IsOffscreen.TryGetValue(out var isOff) && isOff)
                        {
                            skipCount++;
                        }
                    }
                }
                if (!isMatched)
                {
                    return null;
                }
            }
            return retElementChainList;
        }
        /// <summary>
        /// 逆向获取元素链  ，即根据根高亮元素 一层一层向上查找 直到 找到根窗口元素的“方向”
        /// </summary>
        /// <param name="originalChain"></param>
        /// <returns></returns>
        private List<KeyValuePair<int, AutomationElement>> ReverseGetElementChain(List<AutomationElement> originalChain)
        {
            List<KeyValuePair<int, AutomationElement>> retElementChainList = new List<KeyValuePair<int, AutomationElement>>();
            if (originalChain == null || originalChain.Count == 0)
            {
                return retElementChainList;
            }
            retElementChainList.Add(new KeyValuePair<int, AutomationElement>(-1, originalChain[0]));
            var walker = UIAAutomation.TreeWalkerFactory.GetRawViewWalker();
            for (int i = 1; i < originalChain.Count; i++)
            {
                int idx = -1;
                int skipCount = 0;
                int indexParent = -1;
                bool isMatched = false;
                var child = walker.GetFirstChild(originalChain[i - 1]);

                List<AutomationElement> children = null;
                int children_curIndex = 0;
                if (child == null)
                {
                    children = originalChain[i - 1].FindAllChildrenByAcc();
                    if (children != null)
                    {
                        if (children.Count > 0)
                        {
                            child = children[children_curIndex++];
                        }
                    }
                }

                while (child != null)
                {
                    idx++;
                    if (originalChain[i].Equals(child))
                    {
                        if (isMatched)//Automation Equals 有重复元素 不使用使用索引
                        {
                            isMatched = false;
                            break;
                        }
                        indexParent = idx - skipCount;
                        isMatched = true;
                        // retElementChainList.Add(new KeyValuePair<int, AutomationElement>(idx - skipCount, originalChain[i]));
                        //break;
                    }
                    else if (child.Properties.ControlType.IsSupported && child.ControlType == FlaUI.Core.Definitions.ControlType.ToolTip)
                    {
                        skipCount++;
                    }
                    else if (WinConfigurations.Info.FilterHiddenForIndex
                        && child.Properties.IsOffscreen.TryGetValue(out var isOff) && isOff)
                    {
                        skipCount++;
                    }
                    if (children == null)
                    {
                        child = walker.GetNextSibling(child);
                    }
                    else
                    {
                        if (children_curIndex < children.Count)
                        {
                            child = children[children_curIndex++];
                        }
                        else
                        {
                            child = null;
                        }
                    }

                }
                if (!isMatched)
                {
                    //没匹配到 不使用idx 只记录元素   且未发现有此场景，只是程序逻辑上的兜底
                    retElementChainList.Add(new KeyValuePair<int, AutomationElement>(-1, originalChain[i]));
                }
                else
                {
                    retElementChainList.Add(new KeyValuePair<int, AutomationElement>(indexParent, originalChain[i]));
                }
            }
            return retElementChainList;
        }
        /// <summary>
        /// 获取元素链（正向） ，即根据根窗口 一层一层向下查找 直到 找到高亮元素的“方向”
        /// </summary>
        /// <param name="originalChain"></param>
        /// <returns></returns>
        private List<KeyValuePair<int, AutomationElement>> GetElementChain(List<AutomationElement> originalChain, AutomationElement rootEle = null)
        {
            if (!WinConfigurations.Info.ForwardElementChain || originalChain.Count == 0)
            {
                return null;
            }
            List<KeyValuePair<int, AutomationElement>> retElementChainList = new List<KeyValuePair<int, AutomationElement>>();
            if (rootEle == null)
            {
                if (!originalChain[0].Properties.NativeWindowHandle.TryGetValue(out var rootHandle))
                {
                    if (!originalChain[0].TryGetWindowHandleFromAccessible(out rootHandle))
                    {
                        return null;
                    }
                }
                rootEle = UIAAutomation.FromHandle(rootHandle);
                retElementChainList.Add(new KeyValuePair<int, AutomationElement>(-1, rootEle));
            }
            if (rootEle == null)
            {
                return null;
            }
            logger.LogDebug("获取元素链，得到根窗口");

            AutomationElement parent = rootEle;
            var walker = UIAAutomation.TreeWalkerFactory.GetRawViewWalker();
            List<AutomationElement> childrenInParent = null;
            for (int i = 1; i < originalChain.Count; i++)
            {
                childrenInParent = new List<AutomationElement>();
                var child = walker.GetFirstChild(parent);

                List<AutomationElement> children = null;
                int children_curIndex = 0;
                if (child == null)
                {
                    children = parent.FindAllChildrenByAcc();
                    if (children != null)
                    {
                        if (children.Count > 0)
                        {
                            child = children[children_curIndex++];
                        }
                    }
                }


                int idx = -1;
                int skipCount = 0;
                bool isMatched = false;
                while (child != null)
                {
                    logger.LogDebug($"获取元素链，while {idx}");
                    idx++;
                    if (child.Equals(originalChain[i]))
                    {
                        retElementChainList.Add(new KeyValuePair<int, AutomationElement>(idx - skipCount, child));
                        isMatched = true;
                        parent = child;
                        break;
                    }
                    else if (child.Properties.ControlType.TryGetValue(out var childControlType) && childControlType == FlaUI.Core.Definitions.ControlType.ToolTip)
                    {
                        skipCount++;
                    }
                    else if (WinConfigurations.Info.FilterHiddenForIndex
                        && child.Properties.IsOffscreen.TryGetValue(out var isOff) && isOff)
                    {
                        skipCount++;
                    }

                    childrenInParent.Add(child);
                    if (children == null)
                    {
                        child = walker.GetNextSibling(child);
                    }
                    else
                    {
                        if (children_curIndex < children.Count)
                        {
                            child = children[children_curIndex++];
                        }
                        else
                        {
                            child = null;
                        }
                    }
                }
                #region 向下搜索  在逆向对比父级
                //if (!isMatched)
                //{
                //    var cond = new List<ConditionBase>();
                //    if (originalChain[i].Properties.ControlType.TryGetValue(out var controlType))
                //    {
                //        cond.Add(new PropertyCondition(UIAAutomation.PropertyLibrary.Element.ControlType, controlType));
                //    }
                //    if (originalChain[i].Properties.Name.TryGetValue(out var name))
                //    {
                //        cond.Add(new PropertyCondition(UIAAutomation.PropertyLibrary.Element.Name, name));
                //    }
                //    if (originalChain[i].Properties.ClassName.TryGetValue(out var className))
                //    {
                //        cond.Add(new PropertyCondition(UIAAutomation.PropertyLibrary.Element.ClassName, className));
                //    }
                //    AndCondition andCond = new AndCondition(cond);
                //    var children = parent.FindAllDescendants(andCond);
                //    foreach (var item in children)
                //    {
                //        if (item.Equals(originalChain[i]))
                //        {
                //            retElementChainList.Add(new KeyValuePair<int, AutomationElement>(idx, child));
                //            isMatched = true;
                //            parent = child;
                //            break;
                //        }
                //    }
                //    return null;
                //}
                #endregion
                if (!isMatched)
                {
                    var eleAndIdxList = GetElementChainUntilTargetElement(parent, originalChain[i]);
                    if (eleAndIdxList.Count > 0)
                    {
                        eleAndIdxList.Reverse();
                        retElementChainList.AddRange(eleAndIdxList);
                        isMatched = true;
                        parent = eleAndIdxList.Last().Value;
                    }
                }

                if (!isMatched
                    && originalChain[i].Properties.BoundingRectangle.TryGetValue(out var boundingRect)
                    && !boundingRect.IsEmpty)
                {
                    var allboundingRects = childrenInParent.Select(ele =>
                    {
                        if (ele.Properties.BoundingRectangle.TryGetValue(out var eleBoundingRect))
                        {
                            return eleBoundingRect;
                        }
                        return Rectangle.Empty;
                    }).ToList();
                    var rectMatchedCount = allboundingRects.Count(rect => rect.Equals(boundingRect));
                    if (rectMatchedCount == 1)
                    {
                        var index = allboundingRects.FindIndex(rect => rect.Equals(boundingRect));
                        retElementChainList.Add(new KeyValuePair<int, AutomationElement>(index, childrenInParent[index]));
                        isMatched = true;
                    }
                }
                if (!isMatched)
                {
                    return null;
                }
            }
            return retElementChainList;
        }
        /// <summary>
        /// 获取元素链，至到找到指定的目标元素
        /// </summary>
        /// <param name="startElement"></param>
        /// <param name="targetElement"></param>
        /// <param name="eleAndIdxList"></param>
        /// <returns>不包含起始元素，但包含目标元素</returns>
        private List<KeyValuePair<int, AutomationElement>> GetElementChainUntilTargetElement(AutomationElement startElement, AutomationElement targetElement, List<KeyValuePair<int, AutomationElement>> eleAndIdxList = null)
        {
            logger.LogDebug("获取元素链，至到找到指定的目标元素");
            if (eleAndIdxList == null)
            {
                eleAndIdxList = new List<KeyValuePair<int, AutomationElement>>();
            }
            List<AutomationElement> tempNodeList = new List<AutomationElement>();
            var walker = UIAAutomation.TreeWalkerFactory.GetRawViewWalker();
            var child = walker.GetFirstChild(startElement);
            int idx = -1;
            int skipCount = 0;
            while (child != null)
            {
                idx++;
                if (child.Equals(targetElement))
                {
                    eleAndIdxList.Add(new KeyValuePair<int, AutomationElement>(idx - skipCount, child));
                    return eleAndIdxList;
                }
                else if (child.Properties.ControlType.TryGetValue(out var childControlType) && childControlType == FlaUI.Core.Definitions.ControlType.ToolTip)
                {
                    skipCount++;
                }
                else if (WinConfigurations.Info.FilterHiddenForIndex
                    && child.Properties.IsOffscreen.TryGetValue(out var isOff) && isOff)
                {
                    skipCount++;
                }
                tempNodeList.Add(child);
                child = walker.GetNextSibling(child);
            }
            for (int i = 0; i < tempNodeList.Count; i++)
            {
                GetElementChainUntilTargetElement(tempNodeList[i], targetElement, eleAndIdxList);
                if (eleAndIdxList.Count > 0)
                {
                    eleAndIdxList.Add(new KeyValuePair<int, AutomationElement>(i, tempNodeList[i]));
                    break;
                }
            }
            return eleAndIdxList;
        }

        #endregion


        private void ProcessSetAccurate(IEnumerable<WindowsSelectorItem> validSelectorItems)
        {
            foreach (var selectorItem in validSelectorItems)
            {
                foreach (WinSelectorItemProperty prop in selectorItem.Properties)
                {
                    prop.Accurate = true;
                }
            }
        }

        private void ProcessSetAccurate(IEnumerable<SelectorProps> validSelectorItems)
        {
            foreach (var selectorItem in validSelectorItems)
            {
                foreach (var prop in selectorItem.Props)
                {
                    prop.Accurate = ConstFields.PROP_ACCURATE_TURE;
                }
            }
        }

        public virtual string GetTableData(string input)
        {
            if (this.CheckWithinChromePageTable())
            {
                logger.LogDebug($"chromePageTableElement rect:{chromePageTableElement.BoundingRectangle}");
                var data = chromePageTableElement.AsChrominumHtmlTable().TableData.ToJson();
                return data;
            }


            List<IList<string>> table = new List<IList<string>>();
            if (automationElement.Properties.ControlType.IsSupported && (automationElement.ControlType == FlaUI.Core.Definitions.ControlType.Table || automationElement.ControlType == FlaUI.Core.Definitions.ControlType.DataGrid))
            {
                var rows = automationElement.FindAllChildren();
                foreach (var row in rows)
                {
                    row.Properties.ControlType.TryGetValue(out var rowControlType);
                    if (rowControlType == FlaUI.Core.Definitions.ControlType.ScrollBar)
                        continue;
                    List<string> listRows = new List<string>();
                    var cells = row.FindAllChildren();
                    foreach (var cell in cells)
                    {
                        cell.Properties.ControlType.TryGetValue(out var cellControlType);
                        if (cellControlType == FlaUI.Core.Definitions.ControlType.Header || cellControlType == FlaUI.Core.Definitions.ControlType.HeaderItem)
                        {
                            continue;
                        }
                        var value = cell.Patterns.LegacyIAccessible.Pattern.Value;
                        listRows.Add(value);
                    }
                    if (listRows.Count != 0)
                        table.Add(listRows);
                }
            }
            logger.LogDebug($"采集到内容行数量：{table.Count}");
            List<string> rowFirst = new List<string>();
            if (table.Count > 0)
            {
                var rowCount = table.First().Count;
                for (int i = 0; i < rowCount; i++)
                {
                    rowFirst.Add("");
                }
            }
            table.Insert(0, rowFirst);
            return JsonConvert.SerializeObject(table);
        }

        private const int CHILDID_SELF = 0;
        public virtual string GetText()
        {
            string text = string.Empty;

            try
            {
                switch (automationElement.ControlType)
                {
                    case FlaUI.Core.Definitions.ControlType.Button:
                        var btn = this.automationElement.AsButton();
                        text = btn?.Name;
                        break;

                    case FlaUI.Core.Definitions.ControlType.CheckBox:
                        var chk = this.automationElement.AsCheckBox();
                        text = chk?.Text;
                        break;

                    case FlaUI.Core.Definitions.ControlType.ComboBox:
                        var cbo = this.automationElement.AsComboBox();
                        text = cbo?.SelectedItem?.ToString();
                        break;

                    case FlaUI.Core.Definitions.ControlType.RadioButton:
                        var rb = this.automationElement.AsRadioButton();
                        text = rb?.Name;
                        break;

                    case FlaUI.Core.Definitions.ControlType.TreeItem:
                        if (AccEnabler.IsAccEnablerApps(this.automationElement))
                        {
                            if (this.automationElement.Patterns.LegacyIAccessible.IsSupported)
                            {
                                var acc = this.automationElement.Patterns.LegacyIAccessible.Pattern.GetIAccessible();
                                text = acc.accValue[CHILDID_SELF];
                            }
                        }
                        else
                        {
                            var ti = this.automationElement.AsTreeItem();
                            text = ti?.Text;
                        }
                        break;

                    case FlaUI.Core.Definitions.ControlType.Window:
                        var wd = this.automationElement.AsWindow();
                        text = wd?.Title;
                        break;

                    case FlaUI.Core.Definitions.ControlType.Text:
                        {
                            var tb = this.automationElement.AsTextBox();

                            if (tb != null)
                            {
                                // 优先使用DocumentRange模式
                                if (tb.GetDocRangeText(out var docRange))
                                {
                                    text = docRange;
                                }
                                else if (tb.Patterns.Value.IsSupported && tb.Patterns.Text.IsSupported)
                                {
                                    text = tb.Text;
                                }
                                else if (!string.IsNullOrEmpty(tb.Name))
                                {
                                    text = tb.Name;
                                }
                                else
                                {
                                    text = string.Empty;
                                }
                            }
                            break;
                        }

                    case FlaUI.Core.Definitions.ControlType.ListItem:
                        {
                            var li = this.automationElement.AsListBoxItem();

                            if (li != null)
                            {
                                if (li.Patterns.Value.IsSupported && li.Patterns.Text.IsSupported)
                                    text = li.Text;
                                else if (!string.IsNullOrEmpty(li.Name))
                                    text = li.Name;
                                else
                                    text = string.Empty;
                            }

                            break;
                        }


                    case FlaUI.Core.Definitions.ControlType.MenuItem:
                        var mi = this.automationElement.AsMenuItem();
                        text = mi?.Text;
                        break;

                    case FlaUI.Core.Definitions.ControlType.TabItem:
                        var tbm = this.automationElement.AsTabItem();
                        text = tbm?.Name;
                        break;

                    case FlaUI.Core.Definitions.ControlType.HeaderItem:

                        var ghi = this.automationElement.AsGridHeaderItem();
                        text = ghi?.Text;
                        break;

                    case FlaUI.Core.Definitions.ControlType.DataItem:
                        var gc = this.automationElement.AsGridCell();
                        if (gc != null && gc.Patterns.Value.IsSupported)
                            text = gc?.Patterns.Value.Pattern.Value.Value;
                        if (string.IsNullOrEmpty(text))
                            text = gc?.Name;

                        break;

                    case FlaUI.Core.Definitions.ControlType.Edit:
                        var tbe = this.automationElement.AsTextBox();
                        text = tbe?.Text;
                        break;
                    case FlaUI.Core.Definitions.ControlType.Group:
                        var children = this.automationElement.FindAllChildren();
                        //var textElements = children.Where(e => e.Patterns.LegacyIAccessible.Pattern.Role == AccessibilityRole.ROLE_SYSTEM_TEXT);
                        var textElements = children.Where(e => e.ControlType == FlaUI.Core.Definitions.ControlType.Text);
                        foreach (var element in textElements)
                        {
                            text = element.Name;
                            break;
                        }
                        break;

                    default:
                        {
                            var df = this.automationElement.AsTextBox();

                            if (df != null)
                            {
                                if (df.Patterns.Value.IsSupported && df.Patterns.Text.IsSupported)
                                {
                                    text = df.Text;
                                }
                                else if (!string.IsNullOrEmpty(df.Name))
                                {
                                    text = df.Name;
                                }
                                else
                                {
                                    text = string.Empty;
                                }
                            }

                            break;
                        }
                }

                try
                {
                    if (string.IsNullOrEmpty(text)
                        && automationElement.Patterns.LegacyIAccessible.IsSupported
                        && automationElement.Patterns.LegacyIAccessible.Pattern.Value.IsSupported
                        )
                    {
                        var value = automationElement.Patterns.LegacyIAccessible.Pattern.Value.ToString();
                        if (!string.IsNullOrEmpty(value))
                        {
                            text = value;
                        }
                    }
                    if (string.IsNullOrEmpty(text)
                        && automationElement.Patterns.Value.IsSupported
                        && automationElement.Patterns.Value.Pattern.Value.IsSupported)
                    {
                        text = automationElement.Patterns.Value.Pattern.Value.Value;
                    }
                }
                catch
                {
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "get text Exception");

                try
                {
                    if (automationElement.Properties.HelpText.IsSupported && !string.IsNullOrEmpty(automationElement.HelpText))
                    {
                        if (automationElement.HelpText.Contains("DataGrid"))
                        {
                            if (automationElement.Patterns.LegacyIAccessible.IsSupported && automationElement.Patterns.LegacyIAccessible.Pattern.Value.IsSupported)
                            {
                                text = automationElement.Patterns.LegacyIAccessible.Pattern.Value.ValueOrDefault;
                            }
                        }
                    }
                    else if (automationElement.Patterns.LegacyIAccessible.IsSupported
                        && automationElement.Patterns.LegacyIAccessible.Pattern.Role.IsSupported
                        && automationElement.Patterns.LegacyIAccessible.Pattern.Role == AccessibilityRole.ROLE_SYSTEM_CELL)
                    {
                        if (automationElement.Patterns.LegacyIAccessible.Pattern.Value.IsSupported)
                        {
                            text = automationElement.Patterns.LegacyIAccessible.Pattern.Value.ValueOrDefault;
                        }
                    }
                    else
                    {
                        text = this.automationElement.Name;
                    }
                }
                catch (Exception innerEx)
                {
                    throw new WindowsException(eErrorMessageType.Error_GetTextError, innerEx);
                }
            }

            return text;
        }

        public virtual void IntoView()
        {
        }

        public virtual void MouseClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            if (clickParams == null)
            {
                clickParams = new UiElementClickParams();
            }
            try
            {
                logger.LogDebug($"operateType:{clickParams.operateType}offsetX:{offsetX}offsetY:{offsetY}cursorPos:{clickParams.cursorPos}");
                if (clickParams.operateType == 0 && offsetX == 0 && offsetY == 0)
                {
                    GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => automationElement.Click(clickParams.moveMouse));
                }
                else //如果有偏移量的情况下，只能用模拟点击了；
                {
                    SetForeground();
                    MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
                    Thread.Sleep(200);
                    GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => GUIUtils.MouseAction(clickParams.RPAMousePresses, RPAMouseButtons.Left));
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"首次点击异常");
                try
                {
                    MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
                    Thread.Sleep(200);
                    GUIUtils.MouseAction(clickParams.RPAMousePresses, RPAMouseButtons.Left);
                }
                catch (Exception anex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, anex, $"再次点击异常");
                    throw ex;
                }
            }
        }

        public virtual void MouseConterClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            SetForeground();
            MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
            GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => GUIUtils.MouseAction(clickParams.RPAMousePresses, RPAMouseButtons.Middle));
        }

        public virtual void MouseConterDoubleClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            SetForeground();
            MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
            GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => Mouse.DoubleClick(MouseButton.Middle));
        }

        public virtual void MouseDoubleClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            if (clickParams == null)
            {
                clickParams = new UiElementClickParams();
            }

            try
            {
                logger.LogDebug($"operateType:{clickParams.operateType}offsetX:{offsetX}offsetY:{offsetY}cursorPos:{clickParams.cursorPos}");
                if (clickParams.operateType == 0 && offsetX == 0 && offsetY == 0)
                {
                    GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => automationElement.DoubleClick(clickParams.moveMouse));
                }
                else
                {
                    SetForeground();
                    MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
                    GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => Mouse.LeftDoubleClick());
                }
            }
            catch (Exception)
            {
                logger.LogDebug($"首次点击异常");
                try
                {
                    MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
                    Mouse.LeftDoubleClick();
                }
                catch (Exception ex)
                {
                    logger.LogDebug($"再次点击异常");
                    throw ex;
                }
            }
        }

        public virtual void MouseHover(UiElementHoverParams hoverParams = null)
        {
            if (hoverParams == null)
            {
                hoverParams = new UiElementHoverParams();
            }

            try
            {
                MouseMove(hoverParams.cursorPos, hoverParams.offsetX, hoverParams.offsetY, hoverParams.isUsePerson);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /*

        /// <summary>
        /// 鼠标移动到 由设置鼠标绝对位置改为移动鼠标（有移动轨迹）
        /// </summary>
        /// <param name="cursorPos"></param>
        /// <param name="offsetX"></param>
        /// <param name="offsetY"></param>
        public virtual void MouseMove(int cursorPos, int offsetX = 0, int offsetY = 0)
        {
            //NLogger.Default.Trace($"鼠标移动到: offsetX:{offsetX} offsetY:{offsetY} cursorPos:{cursorPos}");
            var rect = GetRegion(3);
            //NLogger.Default.Trace($"当前Rect: offsetX:{offsetX} offsetY:{offsetY} cursorPos:{cursorPos}");
            if (cursorPos == 0)
            {
                var point = GetClickablePoint();
                //Mouse.Position = new Point(point.X + offsetX, point.Y + offsetY);
                Mouse.MoveTo(point.X + offsetX, point.Y + offsetY);
            }
            else if (cursorPos == 1)//左上
            {
                //Mouse.Position = new Point(rect.X + offsetX, rect.Y + offsetY);
                Mouse.MoveTo(rect.X + offsetX, rect.Y + offsetY);
            }
            else if (cursorPos == 2)//右上
            {
                //Mouse.Position = new Point(rect.X + rect.Width + offsetX, rect.Y + offsetY);
                Mouse.MoveTo(rect.X + rect.Width + offsetX, rect.Y + offsetY);
            }
            else if (cursorPos == 3)//左下
            {
                //Mouse.Position = new Point(rect.X + offsetX, rect.Y + rect.Height + offsetY);
                Mouse.MoveTo(rect.X + offsetX, rect.Y + rect.Height + offsetY);
            }
            else if (cursorPos == 4)//右下
            {
                //Mouse.Position = new Point(rect.X + rect.Width + offsetX, rect.Y + rect.Height + offsetY);
                Mouse.MoveTo(rect.X + rect.Width + offsetX, rect.Y + rect.Height + offsetY);
            }
            else if (cursorPos == 5)//随机
            {
                //Mouse.Position = rect.GetRandomPoint();
                Mouse.MoveTo(rect.GetRandomPoint().X, rect.GetRandomPoint().Y);
            }
        }

        */


        /// <summary>
        /// 鼠标移动到 由设置鼠标绝对位置改为移动鼠标（有移动轨迹）
        /// </summary>
        /// <param name="cursorPos"></param>
        /// <param name="offsetX"></param>
        /// <param name="offsetY"></param>
        public virtual void MouseMove(int cursorPos, int offsetX = 0, int offsetY = 0, bool isUsePerson = false)
        {
            var rect = GetRegion(3);
            Point point = new Point(0, 0);
            if (cursorPos == MouseParamDef.CUR_POS_CENTER)
            {
                point = GetClickablePoint();
                point = new Point(point.X + offsetX, point.Y + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_LEFT_TOP)//左上
            {
                point = new Point(rect.X + offsetX, rect.Y + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_RIGHT_TOP)//右上
            {
                point = new Point(rect.X + rect.Width + offsetX, rect.Y + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_LEFT_BOTTOM)//左下
            {
                point = new Point(rect.X + offsetX, rect.Y + rect.Height + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_RIGHT_BOTTOM)//右下
            {
                point = new Point(rect.X + rect.Width + offsetX, rect.Y + rect.Height + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_RANDOM)//随机
            {
                point = rect.GetRandomPoint();
            }
            else if (cursorPos == MouseParamDef.CUR_POS_CENTER_TOP)//上中
            {
                point = new Point(rect.X + rect.Width / 2 + offsetX, rect.Y + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_CENTER_LEFT)//左中
            {
                point = new Point(rect.X + offsetX, rect.Y + rect.Height / 2 + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_CENTER_RIGHT)//右中
            {
                point = new Point(rect.X + rect.Width + offsetX, rect.Y + rect.Height / 2 + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_CENTER_BOTTOM)//下中
            {
                point = new Point(rect.X + rect.Width / 2 + offsetX, rect.Y + rect.Height + offsetY);
            }
            II.RPA.Plugin.GUI.Commons.MouseMotions.MouseMotionHelper.PersonMoveMouse(isUsePerson, point);
        }

        public virtual void MouseRightClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            if (clickParams == null)
            {
                clickParams = new UiElementClickParams();
            }
            try
            {
                logger.LogDebug($"operateType:{clickParams.operateType}offsetX:{offsetX}offsetY:{offsetY}cursorPos:{clickParams.cursorPos}");
                if (clickParams.operateType == 0 && offsetX == 0 && offsetY == 0)
                {
                    GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => automationElement.RightClick(clickParams.moveMouse));
                }
                else
                {
                    SetForeground();
                    MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
                    GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => GUIUtils.MouseAction(clickParams.RPAMousePresses, RPAMouseButtons.Right));
                }
            }
            catch (Exception)
            {
                logger.LogDebug($"首次点击异常");
                try
                {
                    MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
                    Mouse.RightClick();
                }
                catch (Exception ex)
                {
                    logger.LogDebug($"再次点击异常");
                    throw ex;
                }
            }
        }

        public virtual void MouseRightDoubleClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            if (clickParams == null)
            {
                clickParams = new UiElementClickParams();
            }
            try
            {
                logger.LogDebug($"operateType:{clickParams.operateType}offsetX:{offsetX}offsetY:{offsetY}cursorPos:{clickParams.cursorPos}");
                if (clickParams.operateType == 0 && offsetX == 0 && offsetY == 0)
                {
                    GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => automationElement.RightDoubleClick(clickParams.moveMouse));
                }
                else
                {
                    SetForeground();
                    MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
                    GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => Mouse.RightDoubleClick());
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug($"点击异常");
                throw ex;
            }
        }

        public virtual void SetElementAttribute(string name, string value)
        {
        }

        /// <summary>
        /// 勾选
        /// </summary>
        /// <param name="check"></param>
        /// <returns></returns>
        public virtual void SetElementCheck(int check)
        {
            try
            {
                if (automationElement.ControlType.ToString().ToLower().Contains("radiobutton"))
                {
                    var rdb = automationElement.AsRadioButton();
                    if (rdb != null)
                    {
                        rdb.IsChecked = check > 0 ? true : false;
                    }
                }
                else if (automationElement.ControlType.ToString().ToLower().Contains("checkbox"))
                {
                    var chb = automationElement.AsCheckBox();
                    if (chb != null)
                    {
                        chb.IsChecked = check > 0 ? true : false;
                    }
                }
                else
                {
                    logger.LogInformation($"SetElementCheck实际元素类型为：{automationElement.ControlType}");
                    throw new WindowsException(eErrorMessageType.Error_ElementNotOperation);
                }
            }
            catch (Exception e)
            {
                throw new WindowsException(eErrorMessageType.Error_SetSelectError, e);
            }
        }

        public virtual void SetElementMultipleSelect(int selectType, string setValue)
        {
        }

        /// <summary>
        /// 设置Combox选择
        /// </summary>
        /// <param name="selectType">0：顺序，1：按文本 2：选项值(一般为网页元素拥有 win元素不支持该属性)</param>
        /// <param name="setValue"></param>
        /// <param name="matchMode">0 精确匹配 1 模糊 2 正则</param>
        /// <exception cref="WindowsException"></exception>
        public virtual void SetElementSelect(int selectType, string setValue, int matchMode = 0)
        {
            logger.LogInformation($"用户请求选中下拉框的选项: {setValue},模式: {selectType}");

            if (selectType != 0 && selectType != 1)//选项设置类型为选项值
            {
                logger.LogInformation($"SetElementSelect 当前为Win元素 不支持按选项值设置");
                throw new WindowsException(eErrorMessageType.Error_SetElementSelectWinNotSupportValue);
            }

            if (!automationElement.ControlType.ToString().ToLower().Contains("combobox"))
            {
                logger.LogInformation($"SetElementSelect实际元素类型为：{automationElement.ControlType}");
                throw new WindowsException(eErrorMessageType.Error_ElementNotOperation);
            }
            try
            {
                var comboBox = this.automationElement.AsComboBox();

                if (comboBox != null)
                {
                    comboBox.Expand();
                    Thread.Sleep(1000);

                    if (selectType == 0)
                    {
                        bool validInt = int.TryParse(setValue, out var index);

                        if (validInt == false)
                        {
                            throw new WindowsException(eErrorMessageType.Error_SelectValueIsNotValid);
                        }

                        if (index >= comboBox.Items.Count())
                        {
                            throw new WindowsException(eErrorMessageType.Error_IndexOverflow);
                        }

                        var matchedItem = comboBox.Items[index];
                        SelectComboBoxItem(comboBox, matchedItem, index);
                    }
                    else if (selectType == 1)
                    {
                        var index = 0;
                        ComboBoxItem matchedItem = null;

                        foreach (var item in comboBox.Items)
                        {
                            if (matchMode == 0)
                            {
                                if (item.Text == setValue)
                                {
                                    matchedItem = item;
                                    break;
                                }
                            }
                            if (matchMode == 1)
                            {
                                if (item.Text.Contains(setValue))
                                {
                                    matchedItem = item;
                                    break;
                                }
                            }
                            if (matchMode == 2)
                            {
                                if (Regex.IsMatch(item.Text, setValue))
                                {
                                    matchedItem = item;
                                    break;
                                }
                            }
                            index++;
                        }

                        if (matchedItem == null)
                        {
                            throw new WindowsException(eErrorMessageType.Error_SetSelectError);
                        }
                        else
                        {
                            SelectComboBoxItem(comboBox, matchedItem, index);
                        }
                    }
                }
                else
                {
                    throw new Exception(Localize($"仅可以对下拉框元素执行此操作", "controlType_UIANode_DF9BEDC8"));
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "设置选择元素发生了错误");
                throw new WindowsException(eErrorMessageType.Error_SetSelectError, ex);
            }
        }
        public virtual void SetElementSelect(int selectType, string setValue)
        {
            SetElementSelect(selectType, setValue, 0);
        }
        private void SelectComboBoxItem(ComboBox comboBox, ComboBoxItem comboBoxItem, int index)
        {
            logger.LogInformation($"对下拉框项{comboBoxItem?.Text} / {index}执行选中操作");
            //这两个方法主要目的是光标移动到目标选项上，不会触发选择动作，留着会影响个别场景后续的选择点击动作，先去除
            //comboBox.Select(index);
            //comboBoxItem?.Select();

            try
            {
                //comboBoxItem?.Click();
                //if (comboBoxItem.IsSelected == false)
                //{
                var point = comboBoxItem.BoundingRectangle.Center();
                Mouse.Position = point;
                //comboBox.Expand();
                Thread.Sleep(500);
                Mouse.Click();
                //}
            }
            catch
            {
                var point = comboBoxItem.BoundingRectangle.Center();
                Mouse.Position = point;
                comboBox.Expand();
                Thread.Sleep(500);
                Mouse.Click();
            }
        }

        public virtual void SetForeground()
        {
            logger.LogDebug($"SetForeground");
            if (this.RootWindow != null)
            {
                this.RootWindow.Activate(); ;
            }
            else
            {
                GetFirstActiveMainWindow();
            }
        }
        /// <summary>
        /// 获取候选主窗口元素列表
        /// </summary>
        public AutomationElement[] GetCandidateMainWindows()
        {
            var firstSelector = this.OfType<WindowsSelectorItem>()
                                    .FirstOrDefault(x => x.Enabled);

            if (firstSelector == null)
            {
                return Array.Empty<AutomationElement>();
            }

            var elements = firstSelector.Matches(UIAAutomation.GetDesktop(), true)
                          ?? Array.Empty<AutomationElement>();

            logger.LogDebug("Matched {Count} candidate windows", elements.Length);
            //如果元素类型不包含Windows，则说明元素结构是老版本元素结构，需要额外AccEnabler一次篡改元素结构，以处理首次拾取时元素类型不匹配的问题；反之说明元素结构是新的元素结构，在此处已经AccEnabler篡改过
            if (!firstSelector.ElementType.HasFlag(SelectorPropsType.Windows))
            {
                foreach (var ele in elements)
                {
                    AccEnabler.EnableOnce(ele);
                }
            }

            return elements;
        }


        public AutomationElement GetFirstActiveMainWindow()
        {
            var candidates = CandidateMainWindows;
            if (candidates.Length == 0)
            {
                return null;
            }

            var topElement = candidates.FirstOrDefault();
            logger.LogDebug(LogBlock.NullCheck(topElement, "topElement"));
            this.SetForeground(topElement);

            return topElement;
        }

        public virtual void SetText(string text, int isReplace, bool isSimulate)
        {
            TextBox tb = null;
            string value = string.Empty;
            try
            {
                tb = this.automationElement.AsTextBox();
                tb.Focus();
                if (isSimulate)
                {
                    SetForeground();
                    MouseMove(0, 0, 0);
                    Mouse.LeftClick();
                    Thread.Sleep(50);

                    //如果类型是Pane，当输入框中没有文本时，tb.Text会抛异常
                    if (tb.ControlType == FlaUI.Core.Definitions.ControlType.Pane)
                    {
                        value = tb.Name;
                    }
                    else
                    {
                        value = tb.Text;
                    }
                    if (!string.IsNullOrEmpty(value))
                    {
                        ClearElementValue();
                    }
                    if (isReplace == 0)
                    {
                        KeyboardSimulate.KeyboardSimulateInput(text);
                    }
                    else
                    {
                        KeyboardSimulate.KeyboardSimulateInput(value + text);
                    }
                }
                else
                {
                    if (isReplace == 0)
                    {
                        value = text;
                    }
                    else if (isReplace == 1)
                    {
                        //如果类型是Pane，当输入框中没有文本时，tb.Text会抛异常
                        if (tb.ControlType == FlaUI.Core.Definitions.ControlType.Pane)
                        {
                            value = tb.Name + text;
                        }
                        else
                        {
                            value = tb.Text + text;
                        }
                    }
                    tb.Text = value;
                }
            }
            catch (Exception e)
            {
                throw new WindowsException(eErrorMessageType.Error_SetTextError, e);
            }
        }

        protected override SelectorItem GenerateSelectorItem(JObject json, string version)
        {
            return WindowsSelectorItem.FromJObject(json, version);
        }

        //此处可能会出现从鼠标点获取的元素和从Desktop往下获取的元素不一致的情况，根据实际情况有可能需要修正
        private AutomationElement GetCorrectParent(AutomationElement element, bool isUseScanCacheApps = false)
        {
            if (CacheRequest.IsCachingActive)
            {
                return element.CachedParent;
            }
            else if (element.ScanCachedParent != null && isUseScanCacheApps)
            {
                return element.ScanCachedParent;
            }
            else
            {
                return UiCommon.UIAAutomation.TreeWalkerFactory.GetRawViewWalker().GetParent(element);
            }
        }

        private void RelativePosition(IGuiNode guiNode)
        {
            if (guiNode.Name == this.Title)
            {
                rect = guiNode.BoundingRectangle;
            }
            else
            {
                if (guiNode.Parent != null)
                    RelativePosition(guiNode.Parent);
                else
                    rect = ((UIANode)((List<SelectorItem>)this)[0].Element).BoundingRectangle;
            }
        }

        public void SetForeground(AutomationElement topElement)
        {
            if (topElement == null)
            {
                return;
            }
            if (topElement.Properties.ClassName.TryGetValue(out var className))
            {
                if (className == "#32769" || (className == "WorkerW" || className == "Progman"))
                {
                    AutoItHelper.Send("#m", 0);
                    Task.Delay(100).Wait();
                    return;
                }
            }
            if (topElement.Properties.NativeWindowHandle.IsSupported)
            {
                var wndHandle = topElement.Properties.NativeWindowHandle;
                UiCommon.GetWindowThreadProcessId(wndHandle, out uint processId);
                Process process = Process.GetProcessById((int)processId);
                if (WinConfigurations.Info.IgnoreVisible != null
                    && WinConfigurations.Info.IgnoreVisible.Any()
                    && WinConfigurations.Info.IgnoreVisible.Contains(process.ProcessName))
                {
                    if (NativeMethods.IsWindow(wndHandle) && NativeMethods.IsWindowVisible(wndHandle))
                        UiCommon.ForceShow(topElement.Properties.NativeWindowHandle);
                }
                else
                {
                    UiCommon.ForceShow(topElement.Properties.NativeWindowHandle);
                }
            }
        }

        public virtual void MouseThreeClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            if (clickParams == null)
            {
                clickParams = new UiElementClickParams();
            }
            try
            {
                logger.LogDebug($"operateType:{clickParams.operateType}offsetX:{offsetX}offsetY:{offsetY}cursorPos:{clickParams.cursorPos}");
                if (clickParams.operateType == 0 && offsetX == 0 && offsetY == 0)
                {
                    GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () =>
                    {
                        automationElement.DoubleClick(clickParams.moveMouse);
                        automationElement.Click(clickParams.moveMouse);
                    });
                }
                else  //如果有偏移量的情况下，只能用模拟点击了；
                {
                    SetForeground();
                    MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
                    GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () =>
                    {
                        Mouse.LeftDoubleClick();
                        Mouse.LeftClick();
                    });
                }
            }
            catch (Exception)
            {
                logger.LogDebug($"首次点击异常");
                try
                {
                    MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
                    Mouse.LeftDoubleClick();
                    Mouse.LeftClick();
                }
                catch (Exception ex)
                {
                    logger.LogDebug($"再次点击异常");
                    throw ex;
                }
            }
        }

        public virtual void MouseRightThreeClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            if (clickParams == null)
            {
                clickParams = new UiElementClickParams();
            }
            try
            {
                logger.LogDebug($"operateType:{clickParams.operateType}offsetX:{offsetX}offsetY:{offsetY}cursorPos:{clickParams.cursorPos}");
                if (clickParams.operateType == 0 && offsetX == 0 && offsetY == 0)
                {
                    GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () =>
                    {
                        automationElement.RightDoubleClick(clickParams.moveMouse);
                        automationElement.RightClick(clickParams.moveMouse);
                    });
                }
                else
                {
                    SetForeground();
                    MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
                    GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () =>
                    {
                        Mouse.RightDoubleClick();
                        Mouse.RightClick();
                    });
                }

            }
            catch (Exception)
            {
                logger.LogDebug($"首次点击异常");
                try
                {
                    MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
                    Mouse.RightDoubleClick();
                    Mouse.RightClick();
                }
                catch (Exception ex)
                {
                    logger.LogDebug($"再次点击异常");
                    throw ex;
                }
            }
        }

        public virtual void MouseConterThreeClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            SetForeground();
            MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
            GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () =>
            {
                Mouse.DoubleClick(FlaUI.Core.Input.MouseButton.Middle);
                Mouse.Click(FlaUI.Core.Input.MouseButton.Middle);
            });
        }
        public List<string> BreakString(string _str)
        {
            //char[] strCharArr = _str.ToCharArray();
            List<string> RetList = new List<string>();
            foreach (var item in _str.ToCharArray())
            {
                RetList.Add(item.ToString());
            }
            return RetList;
        }
        public JObject GetInputMethod()
        {
            string result = GuiRuntime.Services.InvokePlugin(new II.RPA.Plugin.Abstractions.PluginRoute("DesktopInput", "GetState"), "{}");
            var info = JObject.Parse(result)["info"];
            var state = JObject.Parse(info.ToString());
            return state;
        }
        public void SetInputMethod(string model = "en_us")
        {
            JObject j = new JObject();
            j.Add("state", model);
            var result = GuiRuntime.Services.InvokePlugin(new II.RPA.Plugin.Abstractions.PluginRoute("DesktopInput", "SetState"), j.ToString());
        }
        public virtual bool SetText(ParamSetTextModel param)
        {
            ///元素无有效可视区域、认为输入失败
            if (automationElement.BoundingRectangle.Height <= 0 && automationElement.BoundingRectangle.Width <= 0)
            {
                return false;
            }
            UiElementClickParams uiElementClickParams = new UiElementClickParams
            {
                operateType = 1,
                cursorPos = param.ElementPosition
            };
            MouseClick(param.OffsetX, param.OffsetY, uiElementClickParams);
            Thread.Sleep(param.FocusTimeout);
            string oldValue = string.Empty;

            //if ((opItem.Properties.IsEnabled.IsSupported && !opItem.IsEnabled)
            //    || (opItem.Patterns.Value.Pattern.IsReadOnly.IsSupported && opItem.Patterns.Value.Pattern.IsReadOnly))
            //    throw new WindowsException(eErrorMessageType.Error_ElementNotOperation);
            var replace = param.isReplace == 0 ? true : false;
            var fallback = false;

            if (param.inputType == 0)
            {
                IAutomationPattern<IValuePattern> opValuePattern = null;
                var opItem = UIAAutomation.FromPoint(System.Windows.Forms.Control.MousePosition);
                //很多控件在获取到焦点的时候会改变DOM结构
                //这里强制在获取焦点之后重新获取一次元素
                //无法解决不支持UIAAutomation.FromPoint获取元素的场景（钉钉、微信、千牛等）
                opValuePattern = opItem.Patterns.Value;

                if (opValuePattern.IsSupported)
                {
                    oldValue = opValuePattern.Pattern.Value;

                    if (replace)
                    {
                        opValuePattern.Pattern.SetValue(param.text);
                    }
                    else
                    {
                        opValuePattern.Pattern.SetValue($"{oldValue}{param.text}");
                    }
                }
                else
                {
                    // 对于钉钉、微信、千牛等平台，不会支持value模式，使用旧模式进行补偿
                    var tb = this.automationElement.AsTextBox();

                    oldValue = tb.ControlType == FlaUI.Core.Definitions.ControlType.Pane ?
                            tb.Name :
                            tb.Text;

                    var value = replace ? param.text : $"{oldValue}{param.text}";
                    tb.Text = value;

                    if (tb.Text != value)
                    {
                        // 旧模式仍然会有失败的情况，一个UIA节点被检测为支持Vlaue，但并不保证SetValue能成功
                        fallback = true;
                    }
                }
            }

            if (param.inputType != 0 || fallback)
            {
                if (replace)
                {
                    //AutoItHelper.ControlClearWords(NativeMethods.GetForegroundWindow(), this.WindowHandle, param.Interval);
                    AutoItHelper.ClearWords(param.Interval);
                }
                else
                {
                    //AutoItHelper.ControlMoveCursorToEnd(NativeMethods.GetForegroundWindow(), this.WindowHandle, param.Interval);
                    AutoItHelper.MoveCursorToEnd(param.Interval);
                }

                Thread.Sleep(500);

                if (param.inputType == 1 || fallback)
                {
                    // 是否用fallback补偿的时候需要自动切换输入法，否则会导致输入问题
                    if (param.Trigger == 1 || fallback)
                    {
                        SetInputMethod();
                    }
                    //AutoItHelper.ControlInputMessage(param.text, NativeMethods.GetForegroundWindow(), this.WindowHandle, false, param.Interval);

                    if (param.ForceSwitchENG == 1)
                    {
                        using (var keyboard = new KeyboardLayoutManager(NativeMethods.GetForegroundWindow()))
                        {
                            keyboard.SwitchToEnglish();
                            Thread.Sleep(100);
                            AutoItHelper.Send(param.text, false, param.Interval);
                            keyboard.Restore();
                        }
                    }
                    else
                    {
                        AutoItHelper.Send(param.text, false, param.Interval);
                    }
                }
                else
                {
                    logger.LogInformation($"剪切输入内容：{param.text}");

                    //System.Windows.Clipboard.SetText(param.text);
                    GUI.Commons.UiCommon.SetClipBoard(param.text);

                    //Ctrl + V
                    TypeCombine(20, VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_V);

                    GUI.Commons.UiCommon.ClearClipBoard();
                    logger.LogInformation($"清除剪切板内容");
                }
            }

            var verify = true;

            if (param.isVerify == 1)
            {
                if (replace)
                    verify = GetText().Equals(param.text);
                else
                    verify = GetText().EndsWith(param.text);
            }
            //try
            //{
            //    if (replace && param.isVerify == 1 && !verify && param.inputType == 1)
            //    {
            //        param.inputType = 0;
            //        return SetText(param);
            //    }
            //}
            //catch (Exception)
            //{
            //}

            return verify;
        }
        /// <summary>
        /// 设置文本，支持输入间隔
        /// </summary>
        /// <param name="text"></param>
        /// <param name="isReplace"></param>
        /// <param name="inputType"></param>
        /// <param name="interval"></param>
        /// <exception cref="WindowsException"></exception>
        public virtual void SetText(string text, int isReplace, int inputType, int interval = 0)
        {
            bool doReplace = isReplace == 0;
            var fallback = false;

            try
            {
                SetForeground();
                MouseMove(0, 0, 0);
                Mouse.LeftClick();
                Thread.Sleep(50);

                if (inputType == 0)
                {
                    //很多控件在获取到焦点的时候会改变DOM结构
                    //这里强制在获取焦点之后重新获取一次元素
                    //无法解决不支持UIAAutomation.FromPoint获取元素的场景（钉钉、微信、千牛等）
                    var opItem = UIAAutomation.FromPoint(System.Windows.Forms.Control.MousePosition);
                    var opValuePattern = opItem.Patterns.Value;
                    if (opValuePattern.IsSupported)
                    {
                        var oldValue = opValuePattern.Pattern.Value;

                        if (doReplace)
                        {
                            opValuePattern.Pattern.SetValue(text);
                        }
                        else
                        {
                            opValuePattern.Pattern.SetValue($"{oldValue}{text}");
                        }
                    }
                    else
                    {
                        // 对于钉钉、微信、千牛等平台，不会支持value模式，使用旧模式进行补偿
                        var tb = this.automationElement.AsTextBox();

                        var oldValue = tb.ControlType == FlaUI.Core.Definitions.ControlType.Pane ?
                                tb.Name :
                                tb.Text;

                        var value = doReplace ? text : $"{oldValue}{text}";
                        tb.Text = value;

                        // 旧模式仍然会有失败的情况，一个UIA节点被检测为支持Vlaue，但并不保证SetValue能成功
                        if (tb.Text != value)
                        {
                            fallback = true;
                        }
                    }
                }

                if (inputType != 0 || fallback)
                {
                    //基于操作系统指令
                    if (doReplace)
                    {
                        AutoItHelper.ClearWords();
                    }
                    else
                    {
                        AutoItHelper.MoveCursorToEnd();
                    }

                    Thread.Sleep(200);

                    if (inputType == 1 || fallback)
                    {
                        string result = GuiRuntime.Services.InvokePlugin(new II.RPA.Plugin.Abstractions.PluginRoute("DesktopInput", "GetState"), "{}");
                        var ddd = JObject.Parse(result)["info"];
                        var state = JObject.Parse(ddd.ToString());
                        if (state.Value<string>("state").Equals("中文"))
                        {
                            JObject j = new JObject();
                            j.Add("state", "en_us");
                            result = GuiRuntime.Services.InvokePlugin(new II.RPA.Plugin.Abstractions.PluginRoute("DesktopInput", "SetState"), j.ToString());
                        }
                        if (interval > 0)
                        {
                            AutoItHelper.Send(text, 1, sendKeyDelay: interval);
                        }
                        else
                        {
                            AutoItHelper.Send(text, 1, 50);
                        }
                    }
                    else if (inputType == 2)
                    {
                        //System.Windows.Clipboard.SetText(text);
                        GUI.Commons.UiCommon.SetClipBoard(text);

                        //Ctrl + V
                        TypeCombine(20, VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_V);
                        logger.LogDebug("Ctrl + V");
                        GUI.Commons.UiCommon.ClearClipBoard();
                        logger.LogDebug("ClearClipBoard");
                    }
                }
            }
            catch (Exception e)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, e, "SetText发生了异常");
                throw new WindowsException(eErrorMessageType.Error_SetTextError, e);
            }
        }

        /// <summary>
        /// 清除元素的输入值。主要思路是Ctrl+A，然后Delete。
        /// 但是有例外，某些输入控件不支持Ctrl+A，通常是简单文本框（例如钉钉的登录账号输入框）
        /// 所以加一个补偿逻辑：
        /// 使用Home然后Shift+End指令。新的UIA Com支持，ValuePattern，暂时不可用。
        /// </summary>
        private void ClearElementValue()
        {
            //基于ValuePattern的方案，但是当前版本的Interop.UIA COM并不支持
            /*bool? hasValue = null;
            IValuePattern valuePattern = null;

            if (automationElement.Patterns.Value.IsSupported)
            {
                valuePattern = automationElement.Patterns.Value.Pattern;
                hasValue = !string.IsNullOrEmpty(valuePattern.Value?.Value);
            }

            if (hasValue == false)
            {
                //如果一个UI元素是支持Value模板，但是并没有值，那么无需清理，避免额外的输入操作
                return;
            }
            else if (hasValue == true && valuePattern != null && !valuePattern.IsReadOnly)
            {
                //如果一个UI元素有值且可编辑
                valuePattern.SetValue(null);
            }
            else
            {
                TypeCombine(50, VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
                Thread.Sleep(100);
                Keyboard.Type(VirtualKeyShort.DELETE);
            }*/


            //Home , Shift+End
            Keyboard.Type(VirtualKeyShort.HOME);
            TypeCombine(20, VirtualKeyShort.SHIFT, VirtualKeyShort.END);

            //Ctrl+A
            TypeCombine(20, VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);

            Thread.Sleep(100);
            Keyboard.Type(VirtualKeyShort.DELETE);
        }

        /// <summary>
        /// 将鼠标的光标移动到元素的结尾。主要思路是Ctrl+A，然后End。
        /// End键比→键有优势，在不支持Ctrl+A的时候也能生效。
        /// </summary>
        private void MoveToElementEnd()
        {
            TypeCombine(20, VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
            Thread.Sleep(100);
            Keyboard.Type(VirtualKeyShort.END);
        }

        /// <summary>
        /// 输入组合按键，被组合的按键在输入的时候指定一个延迟，避免组合不生效
        /// </summary>
        /// <param name="interval"></param>
        /// <param name="keys"></param>
        private void TypeCombine(int interval, params VirtualKeyShort[] keys)
        {
            foreach (var key in keys)
            {
                Keyboard.Press(key);
                Thread.Sleep(interval);
            }

            foreach (var key in keys.Reverse())
            {
                Keyboard.Release(key);
            }
        }

        #region 数据采集
        public virtual bool ChromePageTable { get; private set; }
        private AutomationElement chromePageTableElement = null;

        public virtual bool IsTable
        {
            get
            {
                try
                {
                    if (IsDataGrid())
                    {
                        return true;
                    }
                    else if (CheckWithinChromePageTable())
                    {
                        return true;
                    }
                    return false;
                }
                catch (Exception)
                {
                    return false;
                }
                throw new RPAException(eErrorMessageType.Error_DataCollection_GUINoSupport);

            }
        }
        public bool CheckWithinChromePageTable()
        {
            if (ChromePageTable)
                return ChromePageTable;

            var ele = automationElement;
            if (ele.FrameworkType != FrameworkType.Chrome)
            {
                return false;
            }
            while (ele != null)
            {
                try
                {
                    if (ele.IsPageTable())
                    {
                        var trChild = ele.FindFirstChild();
                        if (trChild != null && trChild.IsPageTableRow())
                        {
                            var firstCell = trChild.FindFirstChild();
                            if (firstCell != null && (firstCell.IsPageTableColumnHeader() || firstCell.IsPageTableCell()))
                            {
                                ChromePageTable = true;
                                chromePageTableElement = ele;
                                break;
                            }
                            else
                            {
                                break;//不是 列标题单元格 || 单元格 
                            }
                        }
                        else
                        {
                            break;//不是行
                        }
                    }
                    if (ele.Properties.ControlType.ValueOrDefault == FlaUI.Core.Definitions.ControlType.Table)
                    {
                        break;//只判断冒泡的第一个table
                    }
                }
                catch (Exception)
                {
                }
                ele = ele.Parent;
            }
            return ChromePageTable;

        }
        private bool IsDataGrid()
        {
            var ele = automationElement;
            while (ele != null)
            {
                try
                {
                    if (ele.ControlType == FlaUI.Core.Definitions.ControlType.Table || ele.ControlType == FlaUI.Core.Definitions.ControlType.DataGrid)
                    {
                        foreach (var children in ele.FindAllChildren())
                        {
                            if (children.Properties.ControlType.IsSupported && children.ControlType == FlaUI.Core.Definitions.ControlType.ScrollBar)
                                continue;
                            foreach (var child in children.FindAllChildren())
                            {
                                if (child.Properties.ControlType.IsSupported)
                                {
                                    if (child.ControlType != FlaUI.Core.Definitions.ControlType.Header && child.ControlType != FlaUI.Core.Definitions.ControlType.HeaderItem)
                                    {
                                        throw new RPAException(eErrorMessageType.Error_DataCollection_GUINoSupport);
                                    }
                                    break;
                                }
                                else
                                {
                                    throw new RPAException(eErrorMessageType.Error_DataCollection_GUINoSupport);
                                }
                            }
                        }

                        automationElement = ele;
                        return true;
                    }
                }
                catch (Exception)
                {
                    //throw new RPAException(eErrorMessageType.Error_DataCollection_GUINoSupport);
                }
                ele = ele.Parent;
            }
            return false;
        }
        private string tableNodeSelector;
        public virtual string TableNodeSelector
        {
            get
            {
                if (string.IsNullOrEmpty(cacheSelector))
                {
                    if (this.ChromePageTable)
                    {
                        this.automationElement = this.chromePageTableElement;
                    }
                    cacheSelector = BuildSelector();

                    var processId = automationElement.Properties.ProcessId.Value;
                    var path = ExtractViewData.GetMainModuleFilepath(processId, out var processName);
                    cacheSelector = ExtractViewData.AddCommonProps(cacheSelector, eGuiType.Windows, path, processName: processName);
                    SelectorAdapter adapter = new SelectorAdapter();
                    cacheSelector = adapter.Convert2EditIndexFormat(cacheSelector);
                }
                return cacheSelector;
            }
        }

        public virtual eGuiType GuiType => eGuiType.Windows;

        public virtual bool IsShow
        {
            get
            {
                if (this.automationElement != null)
                {
                    if (automationElement.Patterns.LegacyIAccessible.IsSupported)
                    {
                        try
                        {
                            return !this.automationElement.Patterns.LegacyIAccessible.Pattern.State.ToString().Contains("STATE_SYSTEM_INVISIBLE");
                        }
                        catch
                        {
                            return true;
                        }
                    }
                    else
                        return true;
                }
                else
                    return true;
            }
        }

        public virtual List<SelectorProps> MatchNextButtonSelector(ParamDoElementEditorModel paramModel)
        {
            return new List<SelectorProps>();
        }

        public virtual List<SelectorProps> AutoBulidCollectDataSelector(ParamBulidCollectDataSelectorModel paramModel)
        {
            //string selectors = GetElementFromSelectorAll(paramModel.OnlyContainsTagsSelector.ToJson());
            //List<string> similarSelectors = new List<string>();
            //JArray elementIds = JArray.Parse(selectors);
            //if (elementIds.Count == 1)
            //{
            //    return paramModel.OriginalSelector;
            //}
            ////JArray selectorArray = JArray.Parse(selector);
            //foreach (var elementId in elementIds)
            //{
            //    if (elementId.ToString() == this.UserDefinedID)
            //    {
            //        continue;
            //    }
            //    var element = chromeTab.BuildSelector(elementId.ToString());
            //    JArray elementArray = JArray.Parse(element);
            //    elementArray.Remove(elementArray[0]);
            //    //elementArray[0] = selectorArray[0];
            //    //var ele = ExtractViewData.AddCommonProps(elementArray.ToString(), eGuiType.Chrome.ToString());
            //    var ele = elementArray.ToString(Newtonsoft.Json.Formatting.None);
            //    similarSelectors.Add(ele);
            //}
            ////return elements.ToString(Newtonsoft.Json.Formatting.None);
            //return this.BuilidCollectDataSelector(paramModel.OriginalSelector, similarSelectors);
            return new List<SelectorProps>();
        }

        public virtual List<SelectorProps> CompareBulidCollectDataSelector(ParamBulidCollectDataSelectorModel paramModel)
        {
            List<SelectorProps> originalSelectorPropsList = paramModel.OriginalSelector.ToObject<List<SelectorProps>>();
            List<SelectorProps> similarSelectorPropsList = paramModel.CompareOriginalSelector.ToObject<List<SelectorProps>>();
            ////移除原串中 多余的节点
            //if (originalSelectorPropsList.Count > similarSelectorPropsList.Count)
            //{
            //    originalSelectorPropsList.RemoveRange(similarSelectorPropsList.Count, originalSelectorPropsList.Count - similarSelectorPropsList.Count);
            //}
            ////移除相似串中 多余的节点
            //if (originalSelectorPropsList.Count < similarSelectorPropsList.Count)
            //{
            //    similarSelectorPropsList.RemoveRange(originalSelectorPropsList.Count, similarSelectorPropsList.Count - originalSelectorPropsList.Count);
            //}
            ////判断节点类型是否都相同
            //for (int i = 0; i < originalSelectorPropsList.Count; i++)
            //{
            //    if (originalSelectorPropsList[i].Type != similarSelectorPropsList[i].Type || originalSelectorPropsList[i].Name != similarSelectorPropsList[i].Name)
            //    {
            //        return new List<SelectorProps>();
            //    }
            //}
            return CompareSelectorAndRemoveDiffProps(originalSelectorPropsList, similarSelectorPropsList);
            //return new List<SelectorProps>();
        }
        private List<SelectorProps> CompareSelectorAndRemoveDiffProps(List<SelectorProps> originalSelectorPropsList, List<SelectorProps> similarSelectorPropsList)
        {
            //移除原串中 多余的节点
            if (originalSelectorPropsList.Count > similarSelectorPropsList.Count)
            {
                originalSelectorPropsList.RemoveRange(similarSelectorPropsList.Count, originalSelectorPropsList.Count - similarSelectorPropsList.Count);
            }
            //移除相似串中 多余的节点
            if (originalSelectorPropsList.Count < similarSelectorPropsList.Count)
            {
                similarSelectorPropsList.RemoveRange(originalSelectorPropsList.Count, similarSelectorPropsList.Count - originalSelectorPropsList.Count);
            }
            //判断节点类型是否都相同
            for (int i = 0; i < originalSelectorPropsList.Count; i++)
            {
                if (originalSelectorPropsList[i].Name != similarSelectorPropsList[i].Name)
                {
                    throw new WindowsException(eErrorMessageType.Error_ElementNode_Error);
                    //return new List<SelectorProps>();
                }
            }
            for (int i = 0; i < originalSelectorPropsList.Count; i++)
            {
                foreach (var prop in originalSelectorPropsList[i].Props)
                {
                    var ssProp = similarSelectorPropsList[i].Props.FirstOrDefault(ssp => ssp.Name == prop.Name);
                    if (ssProp == null)
                    {
                        prop.Accurate = "false";
                        continue;
                    }
                    if (prop.Value == ssProp.Value)
                    {
                        //prop.Accurate = "true";
                    }
                    else
                    {
                        prop.Accurate = "false";
                    }
                }

                //去异留同
                //originalSelectorPropsList[i].Props = originalSelectorPropsList[i].Props.Where(osp => similarSelectorPropsList[i].Props.Any(ssp => ssp.Name == osp.Name && ssp.Value == osp.Value)).ToList();
            }
            return originalSelectorPropsList;
        }

        private List<SelectorProps> BuilidCollectDataSelector(List<SelectorProps> originalSelector, List<string> structureSimilarSelectors)
        {
            //int targetIndex = -1;
            ////基础selector的副本，即原串副本
            //List<SelectorProps> originalSelectorPropsList = originalSelector.ToObject<List<SelectorProps>>();
            ////所有相似串的集合，
            //var similarSelectorPropsLists = structureSimilarSelectors.Select(sss => sss.ToObject<List<SelectorProps>>()).ToList();
            ////移除所有节点层数不同的selector ，理论上是不存在的，只是个保险
            //similarSelectorPropsLists.RemoveAll(sps => sps.Count() != originalSelectorPropsList.Count());
            ////用来存储每一个selector节点与基础selector对应节点的差异值
            //Dictionary<int, List<int>> selectorDiffCountList = new Dictionary<int, List<int>>();
            ////int propTotalCount = originalSelectorPropsList.Select(sp => sp.Props.Count).Sum();
            //string[] efectivePropNameArr = { "class", "id", "@index" };//有效属性
            //for (int i = 0; i < originalSelectorPropsList.Count; i++)//拾取原串所有的props
            //{
            //    //移除 基础selector的副本 中的 无效元素，避免影响差异值准确性
            //    originalSelectorPropsList[i].Props.RemoveAll(osp => !efectivePropNameArr.Contains(osp.Name));
            //    for (int j = 0; j < similarSelectorPropsLists.Count; j++)//遍历每一个疑似元素串
            //    {
            //        if (!selectorDiffCountList.ContainsKey(j))
            //        {
            //            selectorDiffCountList[j] = new List<int>();
            //        }
            //        //相同属性数量
            //        int samePropCount = originalSelectorPropsList[i].Props.Count(osp => similarSelectorPropsLists[j][i].Props.Any(ssp => ssp.Name == osp.Name && ssp.Value == osp.Value));
            //        //总数量 - 相同数量 = 差异值
            //        int diffCount = originalSelectorPropsList[i].Props.Count - samePropCount;
            //        selectorDiffCountList[j].Add(diffCount);
            //    }
            //}
            ////生成整体的差异数据，便于后边排除逻辑使用
            //var selectorListDiffData = selectorDiffCountList.Select((diffCountList) => new
            //{
            //    Index = diffCountList.Key,
            //    DiffSum = diffCountList.Value.Sum(),
            //    FirstDiffIndex = diffCountList.Value.FindIndex(v => v > 0),
            //    LastDiffIndex = diffCountList.Value.FindLastIndex(v => v > 0)
            //}).ToList();

            //selectorListDiffData.RemoveAll(data => data.DiffSum == 0);//排除自己
            //int mindiffSum = selectorListDiffData.Min(data => data.DiffSum);//最小差异值
            ////优先使用差异值最小的元素
            //var selectorListDiffData_diffMin = selectorListDiffData.Where(data => data.DiffSum == mindiffSum);
            //if (selectorListDiffData_diffMin.Count() > 0)
            //{
            //    //targetIndex = selectorListDiffData_diffMin.OrderBy(data => data.firstDiffIndex).First();//豆腐块优先
            //    targetIndex = selectorListDiffData_diffMin.OrderBy(data => data.LastDiffIndex).Last().Index;//表格样式优先（tag非table）
            //}
            //if (targetIndex == -1)
            //{
            //    return originalSelectorPropsList;
            //}
            //else
            //{
            //    //一定是使用完整的原串 与 最相似元素 去异求同，尽可能多的保留相似属性
            //    //originalSelectorPropsList = originalSelector.ToObject<List<SelectorProps>>();
            //    List<SelectorProps> similarSelectorPropsList = similarSelectorPropsLists[targetIndex];
            //    return ChromeTab.CompareBulidSimileSelector(originalSelector, similarSelectorPropsList);
            //}
            return new List<SelectorProps>();
        }
        public virtual string GetNextElementBuildSelector(List<string> cssSelectorList)
        {
            try
            {
                return string.Empty;
            }
            catch (Exception e)
            {
                throw new WindowsException(eErrorMessageType.Error_NotFoundElement);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="srcoll"></param>
        public static void ScrollIntoView(string srcoll)
        {
            ////var scrollleft = chromeTab.GetScroll("left");
            //var scrollObject = JsonConvert.DeserializeObject<JObject>(srcoll);
            //var top = scrollObject["scrollTop"].ToString() == "" ? 0 : Convert.ToInt32(scrollObject["scrollTop"].ToString());
            //int record = -1;
            //chrome = chrome ?? new Chrome();
            //for (int i = 0; i < 10; i++)//找10 次
            //{
            //    chromeTab = chrome.Acquire("", "", "", 1); //初始化chromeTab
            //    if (chromeTab != null)
            //    {
            //        while (true)
            //        {
            //            var scrolltopStr = chromeTab.GetScroll("top");
            //            //logger.LogDebug($"scrolltopStr:{scrolltopStr}");
            //            float.TryParse(scrolltopStr, out var scrolltopF);
            //            int scrolltop = (int)scrolltopF;
            //            if (record != scrolltop)
            //                record = scrolltop;
            //            else
            //                return;
            //            if (scrolltop >= top)
            //            {
            //                return;
            //            }
            //            else
            //            {
            //                chromeTab.Scroll("top", top);
            //            }
            //            Thread.Sleep(200);
            //        }
            //    }
            //    else
            //    {
            //        Thread.Sleep(100);
            //        NLogger.Default.Info("未获取到ChromeTab信息");
            //    }
            //}
        }

        public virtual string GetHtml()
        {
            throw new NotImplementedException();
        }

        #endregion

        public virtual string GetSubElements(ParamGetSubElements param)
        {
            var children = this.Children;
            if (children.Count <= 0)
            {
                throw new Exception(string.Format("不存在子元素".I18n("controlType_UIANode_C81BE4C3")));
            }
            var subElementSelectors = CacheData.BulidCacheSubElementSelector(children, this.cacheDataSelector);
            if (!string.IsNullOrEmpty(param.IndexStr))
            {
                if (int.TryParse(param.IndexStr, out int index) && index <= subElementSelectors.Count)
                {
                    return new[] { subElementSelectors[index - 1] }.ToJsonNoneFormating();
                }
                else
                {
                    return "[]";
                }
            }
            return subElementSelectors.ToJsonNoneFormating();
        }

        public virtual string GetParent(ParamGetParent param)
        {
            throw new NotImplementedException();
        }

        public virtual string GetSiblings(ParamGetSiblings param)
        {
            throw new NotImplementedException();
        }

        public UIANode[] GetChildren()
        {
            List<UIANode> children = new List<UIANode>();
            var treeWalker = UiCommon.UIAAutomation.TreeWalkerFactory.GetRawViewWalker();
            var chileEle = treeWalker.GetFirstChild(this.automationElement);
            while (chileEle != null)
            {
                children.Add(new UIANode(chileEle));
                chileEle = treeWalker.GetNextSibling(chileEle);
            }
            return children.ToArray();
        }
        public void IterateNodeChildren(Func<UIANode, bool> func)
        {
            this.GetChildren().ForEach((node) =>
            {
                if (func(node))
                {
                    node.IterateNodeChildren(func);
                }
            });
        }

        public virtual IGuiNode[] QueryTargetSelectorAll(ParamQueryTargetSelectorAllModel param)
        {
            throw new NotImplementedException();
        }

        public virtual string GetElementAttribute_V1(string name, bool isIntact, string apiVersion)
        {
            return GetElementAttribute(name, apiVersion);
        }
        public virtual List<string> GetSelectorAll(List<IGuiNode> guiNodes, string sourceSelector)
        {
            return CacheData.BulidCacheSimilarSelector(guiNodes, sourceSelector);
        }
        public virtual List<Rectangle> GetRegionAll(List<IGuiNode> guiNodes)
        {
            return guiNodes.Select(node => { return node.GetRegion(3); }).ToList();
        }
        public virtual List<string> GetTextAll(List<IGuiNode> guiNodes)
        {
            return guiNodes.Select(node => { return node.GetText(); }).ToList();
        }

        public virtual List<string> GetHtmlAll(List<IGuiNode> guiNodes)
        {
            throw new NotImplementedException();
        }

        public virtual List<string> GetAttributeAll(List<IGuiNode> guiNodes, string attributeName, bool isIntact, string apiVersion)
        {
            return guiNodes.Select(node => { return node.GetElementAttribute(attributeName, apiVersion); }).ToList();
        }

        public virtual void SetElementSelectWithMatchModel(int selectType, int matchModel, string setValue)
        {
            setValue = setValue.ToObject<List<string>>((ex) =>
            {
                throw new RPAException(this.GuiType, eErrorMessageType.Error_SelectValueIsNotValid, ex);
            }).First();
            SetElementSelect(selectType, setValue, matchModel);
        }

        public virtual string GetCleanHtml()
        {
            throw new NotImplementedException();
        }
    }
}



