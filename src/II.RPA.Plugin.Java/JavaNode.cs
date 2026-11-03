using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;
using II.RPA.Logging;
using II.RPA.Logging.Formatting;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.Plugin.GUI.Commons.WindowMatch;
using II.RPA.Plugin.Java.Execute;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using WindowsAccessBridgeInterop;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;

namespace II.RPA.Plugin.Java
{

    public class JavaNode : GUI.Commons.Selector, IGuiNode_6_6_0
    {
        #region [variable]
        private string cachedProcessFullPath;
        private static ILogger logger = Logger.OfContext();
        private readonly AccessBridgeProvider accessBridgeProvider =
            AccessBridgeProvider.Singleton;
        public static AccessBridge accessBridge =>
            AccessBridgeProvider.Singleton.AccessBridgeInstance;
        public Rectangle BoundingRectangleForCell;
        public AccessibleContextInfo info = null;
        public AccessibleJvm JVM = null;
        internal static AutomationBase uia3Automation = new UIA3Automation();
        internal AccessibleNode accessibleNode;
        internal int windowX = 0;
        internal int windowY = 0;

        internal List<string> PathSegments = new List<string>();


        //update:wenyi，暂时没遇到情况，先保留这里逻辑

        /// <summary>
        /// 可能发现在什么情况下拾取时的父节点在后面校验时发现元素的父节点发生变化了，会用 CacheParentNode来替换父节点信息
        /// </summary>        
        private bool parentChanged = false;
        /// <summary>
        /// 这里定义了一个CacheAccessNode来缓存拾取时父节点信息
        /// </summary>
        public AccessibleContextNode CacheParentNode;
        private bool needRemeberPoint = false;
        private const PropertyOptions DefaultPropertyOptions = PropertyOptions.AccessibleContextInfo |
             PropertyOptions.AccessibleIcons |
             PropertyOptions.AccessibleKeyBindings |
             PropertyOptions.AccessibleRelationSet |
             PropertyOptions.ParentContext |
             PropertyOptions.Children |
             PropertyOptions.ObjectDepth |
             PropertyOptions.TopLevelWindowInfo |
             PropertyOptions.ActiveDescendent |
             PropertyOptions.AccessibleText |
             PropertyOptions.AccessibleHyperText |
             PropertyOptions.AccessibleValue |
             PropertyOptions.AccessibleSelection |
             PropertyOptions.AccessibleTable |
             PropertyOptions.AccessibleTableCells |
             PropertyOptions.AccessibleTableCellsSelect |
             PropertyOptions.AccessibleActions;

        private AccessibleContextNode accessibleContextNode;
        private IGuiNode cachedParent;
        private List<JavaControlElement> cacheList = new List<JavaControlElement>();
        private string cacheSelector;
        private Dictionary<string, string> propDict = new Dictionary<string, string>();
        private Rectangle rect = new Rectangle();
        private AutomationElement uiAutomation;
        public Point SnapPoint;
        private static int processID;
        private string cachedProcessName;

        public string SimilarElementsSelector { get; set; }
        #endregion

        public JavaNode(string json, string version) : base(json, version)
        {
            RefreshJvms();
            cacheSelector = json;
        }

        public JavaNode(AccessibleNode accessibleNode, AutomationElement element)
        {
            this.accessibleNode = accessibleNode;
            this.uiAutomation = element;
            Reload();
        }

        internal JavaNode(AccessBridgeProvider accessBridgeProvider)
        {
            this.accessBridgeProvider = accessBridgeProvider
                ?? throw new ArgumentNullException(nameof(accessBridgeProvider));
        }
        internal WindowObject TragetWindow { get; set; }
        public string Role
        {
            get
            {
                if (propDict.ContainsKey(ConstFields.SELECTOR_ROLE))
                {
                    return propDict[ConstFields.SELECTOR_ROLE];
                }

                return string.Empty;
            }
        }
        /// <summary>
        /// 滚动后JAB的GetScreenRectangle可能不反映新位置(JViewport限制)。
        /// 此处存储IntoView计算的Y偏移修正值，每次use后清零。
        /// </summary>
        private int _scrollYCorrection;

        //对于表格内部的cell类型，
        public Rectangle BoundingRectangle
        {
            get
            {
                if (!accessBridgeProvider.CheckAccess())
                    return accessBridgeProvider.Invoke(() => BoundingRectangle);

                var rect = accessibleNode.GetScreenRectangle();
                if (!rect.HasValue)
                {
                    var r = BoundingRectangleForCell;
                    if (_scrollYCorrection != 0)
                        r.Y -= _scrollYCorrection;
                    return r;
                }
                if (_scrollYCorrection != 0)
                {
                    rect = new Rectangle(rect.Value.X, rect.Value.Y - _scrollYCorrection, rect.Value.Width, rect.Value.Height);
                }
                return rect.Value;
            }
        }

        public List<IGuiNode> Children
        {
            get
            {
                if (!accessBridgeProvider.CheckAccess())
                    return accessBridgeProvider.Invoke(() => Children);

                var list = new List<IGuiNode>();

                var children = accessibleNode.GetChildren();
                foreach (var child in children)
                {
                    if (child == null)
                    {
                        continue;
                    }
                    list.Add(new JavaNode(child, uiAutomation));
                }

                return list;
            }
        }

        public string Idx
        {
            get;
            set;
        }

        public string NodeSelector
        {
            get
            {
                return GetSelector(false);
            }
        }
        public string GetSelector(bool innerTextFirst)
        {
            if (!accessBridgeProvider.CheckAccess())
                return accessBridgeProvider.Invoke(() => GetSelector(innerTextFirst));

            if (string.IsNullOrEmpty(cacheSelector))
            {
                cacheSelector = GetSelector();
                if (uiAutomation == null)
                {
                    IntPtr handle = NativeMethods.GetForegroundWindow();
                    uiAutomation = GUI.Commons.UiCommon.UIAAutomation.FromHandle(handle);
                }
                var processId = uiAutomation.Properties.ProcessId.Value;
                var path = ExtractViewData.GetMainModuleFilepath(processId, out var processName);

                cacheSelector = ExtractViewData.AddCommonProps(cacheSelector, eGuiType.Java, path, processName: processName);
                cacheSelector = SelectorAdapter.Convert2EditIndexFormat(cacheSelector);
            }
            if (string.IsNullOrEmpty(SimilarElementsSelector))
                return cacheSelector;
            return SelectorAdapter.BuildSimilarSelector(cacheSelector, SimilarElementsSelector);
        }
        private string GetSelector()
        {
            List<JavaNode> pathToRoot = new List<JavaNode>();
            JavaNode root = null;
            JavaNode element = this;

            bool insideTable = false;
            //构建元素路径
            while (element != null)
            {
                if (pathToRoot.Contains(element)) { break; }

                // 判断是否进入 table
                if (!insideTable && element.Role == ConstFields.ROLE_VALUE_TABLE)
                {
                    insideTable = true;
                }

                if (element.Parent != null)
                {
                    //处理Java Table数据
                    if (element.Parent.Role == ConstFields.ROLE_VALUE_TABLE)
                    {
                        int index = element.accessibleContextNode.GetIndexInParent();
                        JavaNode parentNode = (element.Parent) as JavaNode;
                        AccessibleContextNode acn = parentNode.accessibleNode as AccessibleContextNode;

                        AccessibleTableInfo tableInfo = null;
                        bool result = accessBridge.Functions.GetAccessibleTableInfo(acn.JvmId, acn.AccessibleContextHandle, out tableInfo);
                        if (result)
                        {
                            element.CellIndex = index.ToString();
                            element.Row = (index / tableInfo.columnCount).ToString();
                            element.Column = (index % tableInfo.columnCount).ToString();
                        }
                    }

                    //解决有tool tip层问题
                    if (element.Parent.Role == ConstFields.ROLE_VALUE_LAYERED_PANE)
                    {
                        bool hasTooltip = false;
                        int tooltipIndex = 0;
                        var elements = element.Parent.Children.ToArray();
                        foreach (var elementNode in elements)
                        {
                            var node = elementNode as JavaNode;
                            var child = node.Children == null ? null : node.Children.ToArray();
                            if (child != null)
                            {
                                foreach (var childNode in child)
                                {
                                    if (childNode.Role == ConstFields.ROLE_VALUE_TOOL_TIP)
                                    {
                                        hasTooltip = true;
                                        tooltipIndex = int.Parse(elementNode.Idx);
                                        break;
                                    }
                                }

                                if (hasTooltip) break;
                            }
                        }
                        if (hasTooltip && int.TryParse(element.Idx, out int idx))
                        {
                            if (tooltipIndex < idx)
                            {
                                element.Id = element.Id.Replace(element.Idx, (idx - 1).ToString());
                                element.Idx = (idx - 1).ToString();
                            }
                        }
                    }

                    if (_isTable != true || insideTable)
                    {
                        pathToRoot.Add(element);
                    }

                }
                else
                {
                    //根节点设置
                    root = element;
                    if (!parentChanged)
                    {
                        pathToRoot.Add(element);
                    }
                }

                try
                {
                    element = (JavaNode)element.Parent;
                    //首节点需要判断；
                    if (!parentChanged)
                    {
                        if (element == null || (CacheParentNode != null && (element?.Role != CacheParentNode.GetInfo().role_en_US)))
                        {
                            element = new JavaNode(CacheParentNode, this.uiAutomation);
                            needRemeberPoint = true;
                        }
                        parentChanged = true;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex);
                }
            }

            //反转路径为从根节点到当前节点的顺序
            pathToRoot.Reverse();

            this.Clear();

            //构建每一层的节点selector
            for (var i = 0; i < pathToRoot.Count; i++)
            {
                JavaSelectorItem item = new JavaSelectorItem(pathToRoot[i], i != 0 && i != pathToRoot.Count - 1);

                if (i == 0)
                {
                    //首层节点属性默认选中
                    item.Properties.ForEach(p => p.Accurate = true);

                }
                //Todo: 这里为元素节点添加更多的属性，或在构造函数中处理

                //父元素节点改变了，记录元素坐标
                if (pathToRoot[i].needRemeberPoint)
                {
                    item.Properties.Add(new JavaSelectorItemProperty(ConstFields.SELECTOR_POSITION_X, (SnapPoint.X - pathToRoot[0].BoundingRectangle.X).ToString()));
                    item.Properties.Add(new JavaSelectorItemProperty(ConstFields.SELECTOR_POSITION_Y, (SnapPoint.Y - pathToRoot[0].BoundingRectangle.Y).ToString()));
                }

                this.Add(item);
            }

            //从末尾节点开始，逐个启用节点的更多属性
            for (var i = this.Count() - 1; i >= 0; i--)
            {
                var childs = GetElementsWithuiSelector(JVM, null, 1);
                if (childs.Count == 1)
                {
                    return ToString();
                }
                else
                {
                    var props = (this.ElementAt(i) as JavaSelectorItem)?.Properties;
                    JavaSelectorItemProperty _indexProp = null;
                    foreach (var prop in props)
                    {
                        if (prop.Name == ConstFields.SELECTOR_INDEX_IN_PARENT)
                        {
                            _indexProp = prop;
                            continue;
                        }
                        else
                        {
                            prop.Accurate = true;
                            var _childs = GetElementsWithuiSelector(JVM, null, 1);
                            logger.LogDebug($"GetSelector i:{i}, prop:{prop.Name}, childs:{_childs?.Count()}");
                            if (_childs.Count == 1)
                            {
                                return ToString();
                            }

                        }
                    }
                    if (_indexProp != null)
                    {
                        _indexProp.Accurate = true;
                    }
                }

            }
            return this.ToString();
        }

        public string WindowTitle
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
                            var prop = selectorItem.Props.FirstOrDefault(x => x.Name.Equals(ConstFields.SELECTOR_NAME, StringComparison.OrdinalIgnoreCase));
                            return prop.Value;
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
        public string CellIndex { get; set; } = null;
        public string Row { get; set; } = null;
        //对于是表格里的单元格，需要填充
        public string Column { get; set; } = null;
        public string Title { get; set; }

        public string UserDefinedID
        {
            get
            {
                try
                {
                    return propDict["Object Depth"] + "-" + propDict["Index in parent"] + "-" + propDict["Children count"];
                }
                catch (Exception e)
                {
                    return "";
                }
            }
        }

        public IntPtr WindowHandle
        {
            get
            {
                if (propDict.ContainsKey("WindowHandle"))
                {
                    return (IntPtr)Convert.ToInt32(propDict["WindowHandle"]);
                }
                return IntPtr.Zero;
            }
        }
        public int ProcessID
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
        public string ProcessName
        {
            get
            {
                if (cachedProcessName == null)
                {
                    try
                    {
                        var processId = uiAutomation.Properties.ProcessId.Value;
                        var name = GUI.Commons.UiCommon.GetProcessName(processId);

                        cachedProcessName = name;
                    }
                    catch (Exception)
                    {
                        cachedProcessName = "";
                    }
                }

                return cachedProcessName;
            }
        }

        public string TableNodeSelector
        {
            get
            {
                return NodeSelector;
            }
        }
        public string ProcessFullPath
        {
            get
            {
                if (cachedProcessFullPath == null)
                {
                    try
                    {
                        var processId = uiAutomation.Properties.ProcessId.Value;
                        var path = GUI.Commons.UiCommon.GetProcessFullPath(processId);

                        cachedProcessFullPath = path;
                    }
                    catch (Exception)
                    {
                        cachedProcessFullPath = "";
                    }
                }

                return cachedProcessFullPath;
            }
        }
        public bool IsShow => true;

        public eGuiType GuiType => eGuiType.Java;

        private bool? _isTable;
        public bool IsTable
        {
            get
            {
                if (!accessBridgeProvider.CheckAccess())
                    return accessBridgeProvider.Invoke(() => IsTable);

                if (_isTable.HasValue)
                    return _isTable.Value;
                for (JavaNode current = this; current != null; current = current.Parent as JavaNode)
                {
                    if (current.Role == ConstFields.ROLE_VALUE_TABLE)
                    {
                        _isTable = true;
                        return true;
                    }
                }
                _isTable = false;
                return false;
            }
        }


        public string AutomationId
        {
            get
            {
                return "";
            }
        }
        public string ClassName
        {
            get
            {
                return "";
            }
        }

        public string ControlType
        {
            get
            {
                return propDict[ConstFields.SELECTOR_ROLE];
            }
        }
        public string Description
        {
            get
            {
                if (propDict.ContainsKey("Description"))
                {
                    return propDict["Description"];
                }

                return "";
            }
        }
        public string Id { get; set; }


        public bool IsTopLevelWindow
        {
            get
            {
                return WindowHandle != IntPtr.Zero;
            }
        }

        public string Name
        {
            get
            {
                if (propDict.ContainsKey(ConstFields.SELECTOR_NAME))
                {
                    return propDict[ConstFields.SELECTOR_NAME];
                }

                return "";
            }
        }

        [JsonIgnore]
        public IGuiNode Parent
        {
            get
            {
                if (!accessBridgeProvider.CheckAccess())
                    return accessBridgeProvider.Invoke(() => Parent);

                if (cachedParent == null)
                {
                    var parent = accessibleNode.GetParent();
                    if (parent != null)
                    {
                        //如果parent含有WindowHandle属性，则说明到了顶层窗口，改用UIA窗口结点
                        IGuiNode parentUiNode = new JavaNode(parent, this.uiAutomation);
                        cachedParent = parentUiNode;
                    }
                }
                return cachedParent;
            }
        }

        public IGuiNode AutomationElementParent => throw new NotImplementedException();

        public static void RefreshJvms()
        {
            AccessBridgeProvider.Singleton.RefreshJvms();
        }

        public bool ButtonClickEnable()
        {
            if (!accessBridgeProvider.CheckAccess())
                return accessBridgeProvider.Invoke(ButtonClickEnable);

            try
            {
                AccessibleContextNode cn = accessibleNode as AccessibleContextNode;
                var info = cn.GetInfo();
                if (info.states_en_US.ToLower().Contains("enabled"))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (JavaException ex)
            {
                throw new JavaException(eErrorMessageType.Error_ElementIsEnable, eComponentType.ElementEnable, ex);
            }
            catch (Exception e)
            {
                throw new RPAException(eGuiType.Java, eErrorMessageType.Error_ElementIsEnable, eComponentType.ElementEnable, e);
            }
        }

        public string CaptureInformativeScreenshotToFile(string filePath)
        {
            if (!accessBridgeProvider.CheckAccess())
                return accessBridgeProvider.Invoke(
                    () => CaptureInformativeScreenshotToFile(filePath));

            string result = string.Empty;

            try
            {
                var deskTop = uia3Automation.GetDesktop().Capture();
                string dir = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                //之前生成框选元素图时出错，需要重新生成
                var currentInformativeScreenshot = DrawInformativeOnUiNode(deskTop);

                currentInformativeScreenshot.Save(filePath);

                filePath = result;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "CaptureInformativeScreenshotToFile error");
            }
            return result;
        }

        public List<IGuiNode> FindAll(JavaNode fromElement = null, int maxResult = 1, int milliseconds = 3000, bool findOnce = false, bool buildPath = false)
        {
            if (!accessBridgeProvider.CheckAccess())
                return accessBridgeProvider.Invoke(
                    () => FindAll(fromElement, maxResult, milliseconds, findOnce, buildPath));

            List<IGuiNode> result = new List<IGuiNode>();
            var timeout = TimeSpan.FromMilliseconds(milliseconds);
            var sw = new Stopwatch();
            sw.Start();
            int index = 0;
            do
            {
                logger.LogDebug($"第{index}次查找：{result.Count}, Elapsed{sw.Elapsed}, timeout{timeout}");

                if (index > 0)
                {
                    AccessBridgeProvider.Singleton.RefreshJvms();
                }
                foreach (var jvm in EnumJvms())
                {
                    result = GetElementsWithuiSelector(jvm, fromElement, maxResult, buildPath);
                    if (result != null && result.Count() > 0)
                    {
                        return result.ToList();
                    }
                }
                index++;
                if (result.Count == 0)
                {
                    Thread.Sleep(600);
                }
                if (findOnce == true)
                {
                    if (result.Count == 0)
                    {
                        return new List<IGuiNode>();
                    }
                }
            }
            while (result.Count == 0 && sw.Elapsed < timeout);
            logger.LogDebug("FindAll End:");
            return new List<IGuiNode>();
        }

        //TODO 实际是有问题的
        public string GetChild(string selelctor)
        {
            if (!accessBridgeProvider.CheckAccess())
                return accessBridgeProvider.Invoke(() => GetChild(selelctor));

            if (this.Children.Count <= 0)
            {
                throw new Exception(string.Format("不存在子元素".I18n("controlType_JavaNode_C81BE4C3")));
            }

            try
            {
                JArray arrayAllSelector = new JArray();
                JArray selectorProps = JArray.Parse(selelctor);
                for (int i = 0; i < this.Children.Count; i++)
                {
                    var item = this.Children[i];

                    SelectorProps selectorItem = new SelectorProps()
                    {
                        Name = item.Role,
                        Props = new List<Props>()
                    };

                    if (item.Name != null & item.Name != string.Empty)
                    {
                        Props prop = new Props()
                        {
                            Name = ConstFields.SELECTOR_NAME,
                            Value = item.Name
                        };
                        selectorItem.Props.Add(prop);
                    }
                    else
                    {
                        Props prop = new Props()
                        {
                            Name = ConstFields.SELECTOR_INDEX_IN_PARENT,
                            Value = item.Idx,
                            Accurate = ConstFields.PROP_ACCURATE_TURE,
                            Pattern = ConstFields.PATTERN_EQUAL
                        };
                        selectorItem.Props.Add(prop);
                    }
                    var subObj = JObject.FromObject(selectorItem);
                    selectorProps.Add(subObj);
                    string subSelector = selectorProps.ToString();
                    selectorProps.Remove(subObj);
                    subSelector = SelectorAdapter.Convert2EditIndexFormat(subSelector);
                    arrayAllSelector.Add(JObject.Parse(subSelector));
                }
                return JsonConvert.SerializeObject(arrayAllSelector);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public Point GetClickablePoint()
        {
            var point = new Point(BoundingRectangle.Left + BoundingRectangle.Width / 2, BoundingRectangle.Top + BoundingRectangle.Height / 2);
            return point;
        }

        public string GetElementCheck()
        {
            if (!accessBridgeProvider.CheckAccess())
                return accessBridgeProvider.Invoke(GetElementCheck);

            string result = string.Empty;
            try
            {
                if (Role.Contains(ConstFields.ROLE_VALUE_CHECK) || Role.Contains(ConstFields.ROLE_VALUE_RADIO))
                {
                    var sb = new StringBuilder(accessBridge.TextBufferLengthLimit);

                    var rst = accessBridge.Functions.GetCurrentAccessibleValueFromContext(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, sb, (short)sb.Capacity);
                    if (rst == true)
                    {
                        int tmp = -1;
                        if (int.TryParse(sb.ToString(), out tmp))
                        {
                            result = tmp > 0 ? "true" : "false";
                            return result;
                        }
                    }
                }
                else
                {
                    throw new JavaException(eErrorMessageType.Error_ElementNotOperation);
                }
            }
            catch (JavaException e)
            {
                throw new JavaException(e.ErrorMessageType, eComponentType.GetElementCheck, e);
            }
            catch (Exception ex)
            {
                throw new RPAException(eGuiType.Java, eErrorMessageType.Error_GetElementCheckError, eComponentType.GetElementCheck, ex);
            }
            return result;
        }

        public string GetElementSelect()
        {
            if (!accessBridgeProvider.CheckAccess())
                return accessBridgeProvider.Invoke(GetElementSelect);

            try
            {
                if (Role.Contains(ConstFields.ROLE_VALUE_COMBO))
                {
                    var tmp = accessBridge.Functions.GetAccessibleSelectionFromContext(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, 0);
                    AccessibleContextNode acn = new AccessibleContextNode(accessBridge, tmp);
                    var info = acn.GetInfo();
                    return info.name;
                }
                else
                {
                    throw new JavaException(eErrorMessageType.Error_ElementNotOperation);
                }
            }
            catch (JavaException e)
            {
                throw new JavaException(e.ErrorMessageType, eComponentType.GetElementSelect, e);
            }
            catch (Exception e)
            {
                throw new RPAException(eGuiType.Java, eErrorMessageType.Error_GetSelectError, eComponentType.GetElementSelect, e);
            }
        }

        public string GetElementSelectEx(string strJson)
        {
            if (!accessBridgeProvider.CheckAccess())
                return accessBridgeProvider.Invoke(() => GetElementSelectEx(strJson));

            logger.LogDebug($"JavaNode GetElementSelectEx param:{strJson}");
            //chromeTab.IsActive();
            int selectMode = 0, selectType = 1;

            GetElementSelectExParamModel _GetElementSelectExParam = null;
            try
            {
                _GetElementSelectExParam = JsonConvert.DeserializeObject<GetElementSelectExParamModel>(strJson);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"JavaNode GetElementSelectEx param Err. Json:{strJson}");
                throw new JavaException(eErrorMessageType.Error_ParamsError);
            }
            finally
            {
                if (_GetElementSelectExParam == null || _GetElementSelectExParam.selectMode == -1 || _GetElementSelectExParam.selectType == -1)
                {
                    logger.LogError($"JavaNode GetElementSelectEx param Err. Json:{strJson}");
                    throw new JavaException(eErrorMessageType.Error_ParamsError);
                }
                selectMode = _GetElementSelectExParam.selectMode;
                selectType = _GetElementSelectExParam.selectType;
            }


            try
            {
                if (Role.Contains(ConstFields.ROLE_VALUE_COMBO))
                {
                    if (selectMode == 0)//获取当前选项
                    {
                        var tmp = accessBridge.Functions.GetAccessibleSelectionFromContext(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, 0);
                        AccessibleContextNode acn = new AccessibleContextNode(accessBridge, tmp);
                        var info = acn.GetInfo();
                        if (selectType == 0)//索引
                        {
                            var retJson = new JObject
                            {{"ret", info.indexInParent}}.ToString();
                            return retJson;
                        }
                        else //1 文本
                        {
                            var retJson = new JObject
                            {{"ret", info.name}}.ToString();
                            return retJson;
                        }
                    }
                    else //获取全部
                    {
                        if (selectType == 0)//索引
                        {
                            var selectedContext = accessBridge.Functions.GetAccessibleSelectionFromContext(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, 0);
                            var contextNode = new AccessibleContextNode(accessBridge, selectedContext);
                            var list = contextNode.GetParent().GetChildren().ToList();
                            var retJson = new JsonArrayClass();
                            retJson.ret = Enumerable.Range(0, list.Count).ToList();
                            return retJson.ToJson();
                        }
                        else //1 文本
                        {
                            var selectedContext = accessBridge.Functions.GetAccessibleSelectionFromContext(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, 0);
                            var contextNode = new AccessibleContextNode(accessBridge, selectedContext);
                            var list = contextNode.GetParent().GetChildren().ToList();
                            var nameList = new List<string>();

                            for (int i = 0; i < list.Count; i++)
                            {
                                var cNode = list[i] as AccessibleContextNode;
                                if (cNode != null)
                                {
                                    nameList.Add(cNode.GetInfo().name);
                                }
                            }
                            var retJson = new JsonArrayClass();
                            retJson.ret = nameList;
                            return retJson.ToJson();
                        }
                    }
                }
                else
                {
                    throw new JavaException(eErrorMessageType.Error_ElementNotOperation);
                }
            }
            catch (JavaException e)
            {
                throw new JavaException(e.ErrorMessageType, eComponentType.GetElementSelect, e);
            }
            catch (Exception e)
            {
                throw new RPAException(eGuiType.Java, eErrorMessageType.Error_GetSelectError, eComponentType.GetElementSelect, e);
            }
            return string.Empty;
        }

        public JavaNode GetNodeByPoint(int x, int y)
        {
            if (!accessBridgeProvider.CheckAccess())
                return accessBridgeProvider.Invoke(() => GetNodeByPoint(x, y));

            JavaNode newNode = null;
            AccessibleContextNode accessibleNode = this.accessibleNode as AccessibleContextNode;
            AccessibleContextNode contextNode = accessibleNode.GetNodeAt(accessibleNode.JvmId, accessibleNode.AccessibleContextHandle, x, y);
            if (contextNode != null)
            {
                newNode = new JavaNode(contextNode, this.uiAutomation);
            }
            return newNode;
        }

        public Rectangle GetRegion(int positionType)
        {
            if (!accessBridgeProvider.CheckAccess())
                return accessBridgeProvider.Invoke(() => GetRegion(positionType));

            if (positionType == 1)
            {
                return new Rectangle();
            }
            else if (positionType == 2)
            {
                IntPtr handle = NativeMethods.GetForegroundWindow();
                var element = GUI.Commons.UiCommon.UIAAutomation.FromHandle(handle);
                if (element.Properties.BoundingRectangle.IsSupported)
                {
                    rect = element.BoundingRectangle;
                    return new Rectangle(this.BoundingRectangle.X - rect.X, this.BoundingRectangle.Y - rect.Y, this.BoundingRectangle.Width, this.BoundingRectangle.Height);
                }
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

        public string GetTableData()
        {
            if (!accessBridgeProvider.CheckAccess())
                return accessBridgeProvider.Invoke(GetTableData);

            string reuslt = string.Empty;
            try
            {
                List<List<string>> sub = new List<List<string>>();
                AccessibleTableInfo tableInfo;
                bool brt = JavaNode.accessBridge.Functions.GetAccessibleTableInfo(this.accessibleNode.JvmId, this.accessibleContextNode.AccessibleContextHandle, out tableInfo);
                if (brt == true)
                {
                    int column = tableInfo.columnCount;
                    int row = tableInfo.rowCount;

                    for (int i = 0; i < row; i++)
                    {
                        List<string> items = new List<string>();
                        for (int j = 0; j < column; j++)
                        {
                            AccessibleTableCellInfo cellInfo;
                            JavaNode.accessBridge.Functions.GetAccessibleTableCellInfo(this.accessibleNode.JvmId, this.accessibleContextNode.AccessibleContextHandle, i, j, out cellInfo);
                            AccessibleContextNode cn = new AccessibleContextNode(JavaNode.accessBridge, cellInfo.accessibleContext);

                            var info = cn.GetInfo();
                            if (info.accessibleText != 0)
                            {
                                AccessibleTextItemsInfo textItems;
                                if (accessBridge.Functions.GetAccessibleTextItems(cn.JvmId, cn.AccessibleContextHandle, out textItems, 0))
                                {
                                    var result = textItems.sentence;
                                    if (result.EndsWith("\n")) result = result.Substring(0, result.Length - 1);
                                    if (string.IsNullOrEmpty(result))
                                    {
                                        result = info.description;
                                    }
                                    items.Add(result);
                                    continue;
                                }
                            }
                            items.Add(info.name);
                        }
                        sub.Add(items);
                    }

                    var rowCount = sub.FirstOrDefault().Count;
                    List<string> rowFirst = new List<string>();
                    for (int i = 0; i < rowCount; i++)
                    {
                        rowFirst.Add("");
                    }
                    sub.Insert(0, rowFirst);
                    reuslt = Newtonsoft.Json.JsonConvert.SerializeObject(sub);
                }
            }
            catch (RPAException e)
            {
                throw e;
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                throw new RPAException(eErrorMessageType.Error_GetTableDataError);
            }
            return reuslt;
        }

        /// <summary>
        /// 因为node对象在查找过程被重置 ，缓存selector为空 现在查找表格需要用到selector 特此透传 请勿效仿
        /// </summary>
        /// <param name="selector"></param>
        /// <returns></returns>
        /// <exception cref="JavaException"></exception>
        /// <exception cref="RPAException"></exception>
        public string GetText(string selector = null)
        {
            if (!accessBridgeProvider.CheckAccess())
                return accessBridgeProvider.Invoke(() => GetText(selector));

            try
            {
                if (info.accessibleText != 0)
                {
                    AccessibleTextItemsInfo textItems;
                    if (accessBridge.Functions.GetAccessibleTextItems(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, out textItems, 0))
                    {
                        var result = textItems.sentence;
                        if (result.EndsWith("\n")) result = result.Substring(0, result.Length - 1);
                        if (string.IsNullOrEmpty(result))
                        {
                            result = accessibleContextNode.GetInfo().description;
                        }
                        return result;
                    }
                }
                if (info.accessibleSelection != 0)
                {
                    if (Role.Contains("combo"))
                    {
                        var tmp = accessBridge.Functions.GetAccessibleSelectionFromContext(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, 0);
                        AccessibleContextNode acn = new AccessibleContextNode(accessBridge, tmp);
                        var info = acn.GetInfo();
                        return info.name;
                    }
                    else
                    {
                        AccessibleTextSelectionInfo textItems;
                        if (accessBridge.Functions.GetAccessibleTextSelectionInfo(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, out textItems))
                        {
                            var result = textItems.selectedText;
                            if (result.EndsWith("\n")) result = result.Substring(0, result.Length - 1);
                            return result;
                        }
                    }
                }
                if (!string.IsNullOrEmpty(selector))
                {
                    try
                    {
                        var selectorList = JsonConvert.DeserializeObject<List<SelectorProps>>(selector);

                        int rowIndex = int.Parse(selectorList[selectorList.Count - 1].Props.Where(x => x.Name.EqualIgnoreCase(ConstFields.ROLE_VALUE_TABLE_ROW)).First().Value);
                        int columnIndex = int.Parse(selectorList[selectorList.Count - 1].Props.Where(x => x.Name.EqualIgnoreCase(ConstFields.ROLE_VALUE_TABLE_COLUMN)).First().Value);
                        for (int i = selectorList.Count - 1; i > 0; i--)
                        {
                            if (!selectorList[i].Name.Equals("table", StringComparison.OrdinalIgnoreCase))
                                selectorList.Remove(selectorList[i]);
                            else
                                break;
                        }
                        if (selectorList.Count > 0)
                        {
                            JavaNode node = new JavaNode(JsonConvert.SerializeObject(selectorList), "1.0");
                            var nodes = node.FindAll(null, 1, 3000, false);
                            var tableData = nodes.FirstOrDefault().GetTableData(string.Empty);
                            var tableDataList = JsonConvert.DeserializeObject<List<List<string>>>(tableData);
                            info.name = tableDataList[rowIndex + 1][columnIndex];
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
                return info.name;
            }
            catch (JavaException e)
            {
                throw new JavaException(eErrorMessageType.Error_GetTextError, eComponentType.GetText, e);
            }
            catch (Exception e)
            {
                throw new RPAException(eGuiType.Java, eErrorMessageType.Error_GetTextError, eComponentType.GetText, e);
            }
        }

        public void MouseClick(int offsetX, int offsetY, bool issimulate, int cursorPos = 0)
        {
            if (!accessBridgeProvider.CheckAccess())
            {
                accessBridgeProvider.Invoke(
                    () => MouseClick(offsetX, offsetY, issimulate, cursorPos));
                return;
            }

            try
            {
                IntoView();
                if (issimulate)
                {
                    MouseMove(cursorPos, offsetX, offsetY);
                    Mouse.LeftClick();
                }
                else
                {
                    AccessibleActions _actions;
                    var actions = new Dictionary<string, object>();
                    if (accessBridge.Functions.GetAccessibleActions(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, out _actions))
                    {
                        if (_actions.actionsCount > 0)
                        {
                            foreach (var a in _actions.actionInfo)
                            {
                                if (actions.Count >= _actions.actionsCount) continue;
                                if (!string.IsNullOrEmpty(a.name))
                                {
                                    if (!actions.ContainsKey(a.name))
                                    {
                                        actions.Add(a.name, a);
                                    }
                                }
                            }
                        }
                    }
                    if (actions.ContainsKey("单击") || actions.ContainsKey("click"))
                    {
                        int failure = 0;
                        AccessibleActionInfo info = new AccessibleActionInfo();
                        if (actions.ContainsKey("单击"))
                        {
                            info = (AccessibleActionInfo)actions["单击"];
                        }
                        else
                        {
                            info = (AccessibleActionInfo)actions["click"];
                        }
                        AccessibleActionsToDo actiontodo = new AccessibleActionsToDo();
                        actiontodo.actions = new AccessibleActionInfo[32];
                        actiontodo.actions[0] = info;
                        actiontodo.actionsCount = 1;
                        if (accessBridge.Functions.DoAccessibleActions(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, ref actiontodo, out failure))
                        {
                            logger.LogDebug("Success, click!");
                        }
                        else
                        {
                            throw new JavaException(eErrorMessageType.Error_ClickError, eComponentType.ElementClick);
                        }
                    }
                    else
                    {
                        // throw new RPAExecuteException("元素不支持点击操作");
                        MouseMove(cursorPos, offsetX, offsetY);
                        Mouse.LeftClick();
                    }
                }
            }
            catch (JavaException e)
            {
                throw new JavaException(eErrorMessageType.Error_ClickError, eComponentType.ElementClick, e);
            }
            catch (Exception e)
            {
                throw new RPAException(eGuiType.Java, eErrorMessageType.Error_ClickError, eComponentType.ElementClick, e);
            }
        }

        public void MouseConterClick(int offsetX, int offsetY, int cursorPos)
        {
            MouseMove(cursorPos, offsetX, offsetY);
            Mouse.Click(FlaUI.Core.Input.MouseButton.Middle);
        }

        public void MouseConterDoubleClick(int offsetX, int offsetY, int cursorPos)
        {
            MouseMove(cursorPos, offsetX, offsetY);
            Mouse.DoubleClick(FlaUI.Core.Input.MouseButton.Middle);
        }

        public void MouseDoubleClick(int offsetX, int offsetY, int cursorPos = 0)
        {
            IntoView();
            MouseMove(cursorPos, offsetX, offsetY);
            Mouse.LeftDoubleClick();
        }

        public void MouseHover(int offsetX, int offsetY, int cursorPos = 0)
        {
            MouseMove(cursorPos, offsetX, offsetY);
        }

        public void MouseMove(int cursorPos, int offsetX = 0, int offsetY = 0)
        {
            var rect = GetRegion(3);
            if (cursorPos == MouseParamDef.CUR_POS_CENTER)
            {
                var point = GetClickablePoint();
                Mouse.Position = new Point(point.X + offsetX, point.Y + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_LEFT_TOP)//左上
            {
                Mouse.Position = new Point(rect.X + offsetX, rect.Y + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_RIGHT_TOP)//右上
            {
                Mouse.Position = new Point(rect.X + rect.Width + offsetX, rect.Y + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_LEFT_BOTTOM)//左下
            {
                Mouse.Position = new Point(rect.X + offsetX, rect.Y + rect.Height + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_RIGHT_BOTTOM)//右下
            {
                Mouse.Position = new Point(rect.X + rect.Width + offsetX, rect.Y + rect.Height + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_RANDOM)//随机
            {
                Mouse.Position = GetRandomPoint(rect);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_CENTER_TOP)//上中
            {
                Mouse.Position = new Point(rect.X + rect.Width / 2 + offsetX, rect.Y + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_CENTER_LEFT)//左中
            {
                Mouse.Position = new Point(rect.X + offsetX, rect.Y + rect.Height / 2 + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_CENTER_RIGHT)//右中
            {
                Mouse.Position = new Point(rect.X + rect.Width + offsetX, rect.Y + rect.Height / 2 + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_CENTER_BOTTOM)//下中
            {
                Mouse.Position = new Point(rect.X + rect.Width / 2 + offsetX, rect.Y + rect.Height + offsetY);
            }
        }
        private Point GetRandomPoint(Rectangle rect)
        {
            try
            {
                System.Random rand = new System.Random(Guid.NewGuid().GetHashCode());
                // 生成随机X坐标
                int x = rand.Next(rect.X, rect.X + rect.Width);
                // 生成随机Y坐标
                int y = rand.Next(rect.Y, rect.Y + rect.Height);
                // 随机坐标
                return new System.Drawing.Point(x, y);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, "获取随机坐标异常，使用(x,y)");
                return new System.Drawing.Point(rect.X, rect.Y);
            }
        }
        public void MouseRightClick(int offsetX, int offsetY, int cursorPos = 0)
        {
            IntoView();
            MouseMove(cursorPos, offsetX, offsetY);
            Mouse.RightClick();
        }

        public void MouseRightDoubleClick(int offsetX, int offsetY, int cursorPos = 0)
        {
            IntoView();
            MouseMove(cursorPos, offsetX, offsetY);
            Mouse.RightDoubleClick();
        }

        public void Reload()
        {
            if (!accessBridgeProvider.CheckAccess())
            {
                accessBridgeProvider.Invoke(Reload);
                return;
            }

            try
            {
                propDict = new Dictionary<string, string>();
                var props = accessibleNode.GetProperties(DefaultPropertyOptions);
                EnumProperties(props, null, 0, true);
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }

            try
            {
                this.Idx = accessibleNode.GetIndexInParent().ToString();
                accessibleNode.Refresh();
                accessibleContextNode = accessibleNode as AccessibleContextNode;
                if (accessibleContextNode == null)
                {
                    return;
                }
                info = accessibleContextNode.GetInfo();
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
        }

        public void SetElementCheck(int check)
        {
            if (!accessBridgeProvider.CheckAccess())
            {
                accessBridgeProvider.Invoke(() => SetElementCheck(check));
                return;
            }

            try
            {
                if (Role.Contains(ConstFields.ROLE_VALUE_CHECK) || Role.Contains(ConstFields.ROLE_VALUE_RADIO))
                {
                    var sb = new StringBuilder(accessBridge.TextBufferLengthLimit);

                    var rst = accessBridge.Functions.GetCurrentAccessibleValueFromContext(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, sb, (short)sb.Capacity);
                    if (rst == true)
                    {
                        int tmp = -1;
                        if (int.TryParse(sb.ToString(), out tmp))
                        {
                            if (tmp == 0 && check > 0)
                            {
                                MouseClick(0, 0, true);
                            }
                            if (tmp == 1 && check < 1)
                            {
                                MouseClick(0, 0, true);
                            }
                        }
                    }
                }
                else
                {
                    throw new JavaException(eErrorMessageType.Error_ElementNotOperation);
                }
            }
            catch (JavaException e)
            {
                throw new JavaException(e.ErrorMessageType, eComponentType.SetElementCheck, e);
            }
            catch (Exception ex)
            {
                throw new RPAException(eGuiType.Java, eErrorMessageType.Error_SetElementCheckError, eComponentType.SetElementCheck, ex);
            }
        }

        public void SetElementSelect(int selectType, string setValue)
        {
            if (!accessBridgeProvider.CheckAccess())
            {
                accessBridgeProvider.Invoke(() => SetElementSelect(selectType, setValue));
                return;
            }

            try
            {
                if (this.Role.Contains(ConstFields.ROLE_VALUE_COMBO))
                {
                    cacheList.Clear();

                    RecursionChildren(accessibleContextNode, 0);
                    if (cacheList.Count > 1)
                    {
                        var temp = cacheList.OrderBy(x => x.Depth).LastOrDefault();
                        if (temp != null)
                        {
                            int depth = temp.Depth;
                            var selections = cacheList.Where(x => x.Depth == depth).ToList();
                            if (selectType == 0)   //按顺序设置
                            {
                                int idx;
                                bool p = int.TryParse(setValue, out idx);
                                if (p == false)
                                {
                                    throw new JavaException(eErrorMessageType.Error_SelectValueIsNotValid);
                                }
                                if (idx >= selections.Count)
                                {
                                    throw new JavaException(eErrorMessageType.Error_IndexOverflow);
                                }
                                accessBridge.Functions.AddAccessibleSelectionFromContext(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, idx);
                            }
                            else if (selectType == 1)
                            {
                                int matchIndex = -1;
                                for (int i = 0; i < selections.Count; i++)
                                {
                                    var info = selections[i].AccInfo;
                                    if (setValue == info.name)
                                    {
                                        matchIndex = i;
                                        break;
                                    }
                                }
                                if (matchIndex == -1)
                                {
                                    throw new JavaException(eErrorMessageType.Error_SetSelectError);
                                }
                                else
                                {
                                    accessBridge.Functions.AddAccessibleSelectionFromContext(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, matchIndex);
                                }
                            }
                        }
                    }
                    else
                    {
                        var selectedContext = accessBridge.Functions.GetAccessibleSelectionFromContext(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, 0);
                        var contextNode = new AccessibleContextNode(accessBridge, selectedContext);
                        var list = contextNode.GetParent().GetChildren().ToList();
                        Dictionary<int, string> pairs = new Dictionary<int, string>();
                        for (int i = 0; i < list.Count; i++)
                        {
                            var cNode = list[i] as AccessibleContextNode;
                            if (cNode != null)
                            {
                                pairs.Add(i, cNode.GetInfo().name);
                            }
                        }
                        if (selectType == 0)   //按顺序设置
                        {
                            int idx;
                            bool p = int.TryParse(setValue, out idx);
                            if (p == false)
                            {
                                throw new JavaException(eErrorMessageType.Error_SelectValueIsNotValid);
                            }
                            if (idx >= pairs.Count)
                            {
                                throw new JavaException(eErrorMessageType.Error_IndexOverflow);
                            }
                            accessBridge.Functions.AddAccessibleSelectionFromContext(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, idx);
                        }
                        else if (selectType == 1)
                        {
                            int matchIndex = -1;
                            var matchNode = pairs.Where(x => x.Value == setValue);
                            if (matchNode.Count() > 0)
                            {
                                matchIndex = matchNode.First().Key;
                            }

                            if (matchIndex == -1)
                            {
                                throw new JavaException(eErrorMessageType.Error_SetSelectError);
                            }
                            else
                            {
                                accessBridge.Functions.AddAccessibleSelectionFromContext(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, matchIndex);
                            }
                        }
                    }
                }
                else
                {
                    throw new JavaException(eErrorMessageType.Error_ElementNotOperation);
                }
            }
            catch (JavaException e)
            {
                throw new JavaException(e.ErrorMessageType, eComponentType.SetElementSelect, e);
            }
            catch (Exception e)
            {
                throw new RPAException(eGuiType.Java, eErrorMessageType.Error_SetSelectError, eComponentType.SetElementSelect, e);
            }
        }

        public void SetForeground()
        {
            if (!accessBridgeProvider.CheckAccess())
            {
                accessBridgeProvider.Invoke(SetForeground);
                return;
            }

            // 如果this有selector信息（通过selector查找到的），走原逻辑
            var selectorItem = this.OfType<JavaSelectorItem>().FirstOrDefault(x => x.Enabled);
            if (selectorItem != null)
            {
                foreach (var jvm in EnumJvms())
                {
                    JavaNode node = new JavaNode(jvm, uia3Automation.GetDesktop());
                    JavaNode matchNode = selectorItem.Matches(node).FirstOrDefault();
                    if (matchNode != null)
                    {
                        try
                        {
                            windowX = matchNode.BoundingRectangle.X;
                            windowY = matchNode.BoundingRectangle.Y;
                            IntPtr ptr = matchNode.accessibleNode.AccessBridge.Functions.GetHWNDFromAccessibleContext(matchNode.accessibleNode.JvmId, matchNode.accessibleContextNode.AccessibleContextHandle);
                            if (this.TragetWindow == null)
                            {
                                UiCommon.ForceShow(ptr);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex);
                        }
                    }
                }
            }
            else
            {
                // 无selector信息，尝试多种方式获取窗口句柄并激活
                IntPtr foundHwnd = IntPtr.Zero;

                // 方式1: accessibleNode -> GetHWNDFromAccessibleContext
                try
                {
                    foundHwnd = accessibleNode.AccessBridge.Functions.GetHWNDFromAccessibleContext(
                        accessibleNode.JvmId, accessibleContextNode.AccessibleContextHandle);
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, "SetForeground GetHWNDFromAccessibleContext failed");
                }

                // 方式2: 通过Process.MainWindowHandle获取
                if (foundHwnd == IntPtr.Zero)
                {
                    try
                    {
                        int pid = ProcessID;
                        if (pid > 0)
                            foundHwnd = Process.GetProcessById(pid).MainWindowHandle;
                    }
                    catch (Exception ex)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, "SetForeground Process.MainWindowHandle failed");
                    }
                }

                // 方式3: 最后fallback到当前前台窗口
                if (foundHwnd == IntPtr.Zero)
                    foundHwnd = NativeMethods.GetForegroundWindow();

                if (foundHwnd != IntPtr.Zero)
                {
                    UiCommon.ForceShow(foundHwnd);
                    logger.LogInformation($"[SetForeground] ForceShow hwnd=0x{foundHwnd.ToInt64():X}");
                }
                else
                {
                    logger.LogInformation("[SetForeground] No window handle found via any method");
                }
            }

            if (this.TragetWindow != null)
            {
                UiCommon.ForceShow(TragetWindow.Handle);
            }
        }
        private AccessibleContextInfo GetInfo()
        {
            //this.ThrowIfDisposed();
            AccessibleContextInfo info;
            if (!accessBridge.Functions.GetAccessibleContextInfo(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, out info))
            {
                return null;
            }
            return info;
        }
        private string GetAccessibleText()
        {
            int len = 32767;
            AccessibleTextInfo textInfo;
            if (accessBridge.Functions.GetAccessibleTextInfo(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, out textInfo, 0, 0))
            {
                len = Math.Min(textInfo.charCount, len);
            }
            StringBuilder sb = new StringBuilder(len);
            string value = "";
            AccessibleContextInfo info = this.GetInfo();
            if ((info.accessibleInterfaces & AccessibleInterfaces.cAccessibleValueInterface) != (AccessibleInterfaces)0 && accessBridge.Functions.GetCurrentAccessibleValueFromContext(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, sb, (short)len))
            {
                value = sb.ToString();
            }
            string text = "";
            if ((info.accessibleInterfaces & AccessibleInterfaces.cAccessibleTextInterface) != (AccessibleInterfaces)0)
            {
                for (int i = 0; i < len; i++)
                {
                    AccessibleTextItemsInfo itemsInfo;
                    if (accessBridge.Functions.GetAccessibleTextItems(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, out itemsInfo, i))
                    {
                        sb.Append(itemsInfo.letter);
                    }
                }
                text = sb.ToString();
            }
            if (!string.IsNullOrEmpty(value))
            {
                return "";
            }
            return text;
        }
        public void SetText(string text, int isReplace, bool isSimulate)
        {
            if (!accessBridgeProvider.CheckAccess())
            {
                accessBridgeProvider.Invoke(() => SetText(text, isReplace, isSimulate));
                return;
            }

            try
            {
                if (info.accessibleText == 0)
                {
                    logger.LogError(("元素不支持文本操作,进行模拟输入"));
                    if (Role.Contains("combo"))
                    {
                        int retry = 0;

                        do
                        {
                            var tmp = accessBridge.Functions.GetAccessibleSelectionFromContext(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, 0);
                            AccessibleContextNode acn = new AccessibleContextNode(accessBridge, tmp);

                            var info = acn.GetInfo();
                            var selectName = info.name;  //获取当前选中值

                            var block = LogBlock.Rows(
                                default,
                                retry.ToString().Tag("次数"),
                                selectName.Tag("selectName"),
                                text.Tag("SetText"));

                            logger.LogInformation(block);

                            if (selectName.Trim() != text.Trim())
                            {
                                cacheList.Clear();
                                RecursionChildren(accessibleContextNode, 0);
                                int nowIndex = 0, targetIndex = 0;
                                var temp = cacheList.OrderBy(x => x.Depth).LastOrDefault();
                                if (temp != null)
                                {
                                    int depth = temp.Depth;
                                    var selections = cacheList.Where(x => x.Depth == depth).ToList();
                                    for (int i = 0; i < selections.Count; i++)
                                    {
                                        if (selections[i].Name == selectName)
                                        {
                                            nowIndex = i;
                                        }
                                        if (selections[i].Name == text)
                                        {
                                            targetIndex = i;
                                        }
                                    }
                                    if (nowIndex > targetIndex)
                                    {
                                        //向上按键
                                        Point p = GetClickablePoint();
                                        Mouse.Position = p;
                                        Mouse.Click();
                                        logger.LogInformation("NowIndex:" + nowIndex + "targetIndex:" + targetIndex);
                                        for (int i = targetIndex; i < nowIndex; i++)
                                        {
                                            Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.UP);
                                            Thread.Sleep(20);
                                        }
                                        Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.ENTER);
                                    }
                                    else
                                    {
                                        Point p = GetClickablePoint();
                                        Mouse.Position = p;
                                        Mouse.Click();
                                        logger.LogInformation("NowIndex:" + nowIndex + "targetIndex:" + targetIndex);
                                        for (int i = nowIndex; i < targetIndex; i++)
                                        {
                                            Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.DOWN);
                                            Thread.Sleep(20);
                                        }
                                        Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.ENTER);
                                    }
                                }
                            }
                            else
                            {
                                return;
                            }
                            retry++;
                        }
                        while (retry < 3);
                    }
                    else
                    {
                        Point p = GetClickablePoint();
                        Mouse.Position = p;
                        Mouse.Click();
                        Thread.Sleep(200);
                        if (isReplace == 0)
                        {
                            Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL);
                            Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_A);
                            Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.DELETE);

                            Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.DELETE);
                            Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_A);
                            Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL);
                        }
                        Keyboard.Type(text);
                        Mouse.Click();
                    }
                }
                else
                {
                    AccessibleTextItemsInfo textItems;
                    if (accessBridge.Functions.GetAccessibleTextItems(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, out textItems, 0))
                    {
                        if (isSimulate)
                        {
                            var value = textItems.word;
                            if (!string.IsNullOrEmpty(textItems.word))
                            {
                                Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL);
                                Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_A);
                                Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.DELETE);

                                Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.DELETE);
                                Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_A);
                                Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL);
                                Thread.Sleep(200);
                            }
                            if (isReplace == 0)
                            {
                                Keyboard.SendInput(text);
                            }
                            else
                            {
                                Keyboard.SendInput(value + text);
                            }
                        }
                        else
                        {
                            string value = string.Empty;
                            if (isReplace == 0)
                                value = text;
                            else
                                value = GetAccessibleText() + text;
                            if (!accessBridge.Functions.SetTextContents(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, value))
                            {
                                accessBridge.Functions.GetAccessibleTextItems(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, out textItems, 0);
                                if (textItems.word != value)
                                    throw new JavaException(eErrorMessageType.Error_SetTextError);
                            }
                        }
                    }
                }
            }
            catch (JavaException e)
            {
                throw new JavaException(eErrorMessageType.Error_SetTextError, eComponentType.SetText, e);
            }
            catch (Exception e)
            {
                throw new RPAException(eGuiType.Java, eErrorMessageType.Error_SetTextError, eComponentType.SetText, e);
            }
        }

        internal static List<AccessibleJvm> EnumJvms(bool bRefresh = false)
        {
            return AccessBridgeProvider.Singleton.EnumJvms(bRefresh);
        }

        protected override GUI.Commons.SelectorItem GenerateSelectorItem(JObject json, string version)
        {
            return JavaSelectorItem.FromJObject(json, version);
        }

        private Bitmap DrawInformativeOnUiNode(Bitmap currentDesktopScreenshot)
        {
            using (Graphics g = Graphics.FromImage(currentDesktopScreenshot))
            {
                //最终截图的红色标记边框
                Pen pen = new Pen(Color.Red, 2);
                g.DrawRectangle(pen, this.BoundingRectangle);
            }

            Rectangle cropRect = this.BoundingRectangle;
            cropRect.Inflate(5, 5);
            Bitmap target = new Bitmap(cropRect.Width, cropRect.Height);

            using (Graphics g = Graphics.FromImage(target))
            {
                g.DrawImage(currentDesktopScreenshot, new Rectangle(0, 0, target.Width, target.Height),
                                 cropRect,
                                 GraphicsUnit.Pixel);
            }

            return target;
        }

        private void EnumProperties(PropertyList list, string prefix, int ident, bool ignoreGroups)
        {
            foreach (var prop in list)
            {
                var name = prop.Name.ToLower();

                if (name == "parent")
                {
                    continue;
                }

                if (name == "children" && !ignoreGroups)
                {
                    var g = prop as PropertyGroup;
                    if (g == null)
                    {
                        continue;
                    }
                    g.LoadChildren();
                    foreach (var c in g.Children)
                    {
                        var v = c.Value;
                    }
                    continue;
                }
                if (name == "top level window")
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(prefix))
                {
                    name = prefix + "." + name;
                }

                if (prop is PropertyGroup)
                {
                    if (ignoreGroups) continue;
                    try
                    {
                        var g = prop as PropertyGroup;
                        if (g == null)
                        {
                            continue;
                        }
                        g.LoadChildren();
                        if (ident <= 2)
                        {
                            EnumProperties(g.Children, name, (ident + 1), false);
                        }
                    }
                    catch (Exception ex)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"EnumProperties Error for {name}");
                    }
                }
                else
                {
                    if (prop.Value == null)
                    {
                        continue;
                    }
                    var value = prop.Value.ToString();
                    if (string.IsNullOrEmpty(value))
                    {
                        continue;
                    }

                    if (!propDict.ContainsKey(name))
                    {
                        propDict.Add(name, prop.Value.ToString());
                    }
                }
            }
        }

        private List<IGuiNode> GetElementsWithuiSelector(WindowsAccessBridgeInterop.AccessibleJvm jvm, JavaNode fromElement, int maxresults, bool buildPathInfo = false)
        {
            var currents = new List<JavaNode>();
            var selectors = this.OfType<JavaSelectorItem>().Where(x => x.Enabled).ToList(); //找出不是顶层元素
            JavaNode startfrom = null;
            if (fromElement != null)
            {
                startfrom = fromElement;
            }
            if (startfrom == null)
            {
                startfrom = new JavaNode(jvm, null);
            }
            currents.Add(startfrom);

            JavaNode[] result = null;
            for (int i = 0; i < selectors.Count; i++)
            {
                JavaSelectorItem selectorItem = selectors[i];
                List<JavaNode> elements = new List<JavaNode>(currents);
                currents.Clear();
                foreach (var eleItem in elements)
                {
                    result = selectorItem.Matches(eleItem, windowX, windowY);
                    if (result.Count() > 0)
                    {
                        result.ForEach(n =>
                        {
                            n.PathSegments.AddRange(eleItem.PathSegments);
                            if (string.IsNullOrEmpty(CellIndex))
                            {
                                n.PathSegments.Add(n.Idx);
                            }
                            else
                            {
                                n.PathSegments.Add(n.CellIndex);
                            }


                        });
                        currents.AddRange(result);
                    }
                    else
                    {
                        //如果没有在子元素中获取到，再去后代里面找元素
                        if (selectorItem.Properties.Where(x => x.Name.EqualIgnoreCase(ConstFields.SELECTOR_POSITION_X)).Count() > 0) //需要根据坐标来获取
                        {
                            int x = int.Parse(selectorItem.Properties.Where(m => m.Name.EqualIgnoreCase(ConstFields.SELECTOR_POSITION_X)).FirstOrDefault().Value) + windowX;
                            int y = int.Parse(selectorItem.Properties.Where(n => n.Name.EqualIgnoreCase(ConstFields.SELECTOR_POSITION_Y)).FirstOrDefault().Value) + windowY;
                            var node = this.accessibleContextNode.GetNodeAt(this.accessibleContextNode.JvmId, this.accessibleContextNode.AccessibleContextHandle, x, y);
                            if (node != null)
                            {
                                JavaNode javaNode = new JavaNode(node, this.uiAutomation);

                                javaNode.PathSegments.AddRange(eleItem.PathSegments);
                                javaNode.PathSegments.Add(javaNode.Idx);
                                currents.Add(javaNode);
                            }
                        }
                        else
                        {
                            result = GetFromSubList(selectorItem, eleItem.Children);
                            result.ForEach(n =>
                            {
                                n.PathSegments.AddRange(eleItem.PathSegments);
                                if (string.IsNullOrEmpty(CellIndex))
                                {
                                    n.PathSegments.Add(n.Idx);
                                }
                                else
                                {
                                    n.PathSegments.Add(n.CellIndex);
                                }
                            });
                            currents.AddRange(result);
                        }
                    }
                }

                if (i == (selectors.Count - 1))
                {
                    result = currents.ToArray();
                }
            }
            if (result == null)
            {
                return new List<IGuiNode>();
            }
            return new List<IGuiNode>(result);
        }

        private JavaNode[] GetFromSubList(JavaSelectorItem selectItem, List<IGuiNode> parent)
        {
            JavaNode[] list = new JavaNode[0];
            foreach (var ele in parent)
            {
                list = selectItem.Matches(ele as JavaNode);
                if (list.Count() == 0)
                {
                    list = GetFromSubList(selectItem, ele.Children);
                }
            }
            return list;
        }
        private void RecursionChildren(AccessibleContextNode node, int depth)
        {
            JavaControlElement jce = new JavaControlElement(node, depth);
            cacheList.Add(jce);
            depth++;
            var nodes = node.GetChildren();
            foreach (var item in nodes)
            {
                AccessibleContextNode acn = item as AccessibleContextNode;
                RecursionChildren(acn, depth);
            }
        }

        private void RelativePosition(AccessibleNode acc)
        {
            if (WindowTitle == acc.GetTitle())
            {
                rect = acc.GetScreenRectangle().Value;
            }
            else
            {
                RelativePosition(acc.GetParent());
            }
        }
        public void MouseThreeClick(int offsetX, int offsetY, int cursorPos)
        {
            MouseMove(cursorPos, offsetX, offsetY);
            Mouse.LeftDoubleClick();
            Mouse.LeftClick();
        }

        public void MouseRightThreeClick(int offsetX, int offsetY, int cursorPos)
        {
            MouseMove(cursorPos, offsetX, offsetY);
            Mouse.RightDoubleClick();
            Mouse.RightClick();
        }

        public void MouseConterThreeClick(int offsetX, int offsetY, int cursorPos)
        {
            MouseMove(cursorPos, offsetX, offsetY);
            Mouse.DoubleClick(FlaUI.Core.Input.MouseButton.Middle);
            Mouse.Click(FlaUI.Core.Input.MouseButton.Middle);
        }



        public bool SetText(ParamSetTextModel param)
        {
            if (!accessBridgeProvider.CheckAccess())
                return accessBridgeProvider.Invoke(() => SetText(param));

            if (param.inputType == 0)
            {
                string value = string.Empty;
                if (param.isReplace == 0)
                    value = param.text;
                else
                    value = GetAccessibleText() + param.text;


                AccessibleTextItemsInfo textItems;
                accessBridge.Functions.GetAccessibleTextItems(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, out textItems, 0);
                if (!accessBridge.Functions.SetTextContents(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, value))
                {
                    accessBridge.Functions.GetAccessibleTextItems(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, out textItems, 0);
                    if (textItems.word != value)
                        throw new JavaException(eErrorMessageType.Error_SetTextError);
                }
            }
            else
            {
                /**这一段在其他元素操作是 包括(param.inputType == 0) 也执行，
                 * 但老的java插件在(param.inputType == 0)时没有执行这一段
                 * 保留原写法
                */
                var replace = param.isReplace == 0 ? true : false;
                UiElementClickParams clickParams = new UiElementClickParams();
                clickParams.operateType = 0;
                clickParams.cursorPos = param.ElementPosition;
                MouseClick(param.OffsetX, param.OffsetY, clickParams);
                Thread.Sleep(param.FocusTimeout);

                if (replace)
                {
                    AutoItHelper.ClearWords(param.Interval);
                }
                else
                {
                    AutoItHelper.MoveCursorToEnd(param.Interval);
                }
                Thread.Sleep(200);
                if (param.inputType == 1)
                {
                    if (param.Trigger == 1)
                    {
                        SetInputMethod();
                    }
                    AutoItHelper.Send(param.text, false, param.Interval);
                }
                else
                {
                    ClipboardSetText(param.text);
                    //Ctrl + V
                    TypeCombine(20, VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_V);
                }
            }

            var verify = true;
            if (param.isVerify == 1)
            {

                if (param.isReplace == 0)
                    verify = GetText().Equals(param.text);
                else
                    verify = GetText().EndsWith(param.text);
            }
            return verify;
        }
        #region 新方式处理模拟输入
        public void ClipboardSetText(string text)
        {
            if (!NativeMethods.OpenClipboard(IntPtr.Zero))
            {
                ClipboardSetText(text);
                return;
            }
            NativeMethods.EmptyClipboard();
            NativeMethods.SetClipboardData(13, Marshal.StringToHGlobalUni(text));
            NativeMethods.CloseClipboard();
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
        public void SetInputMethod(string model = "en_us")
        {
            JObject j = new JObject();
            j.Add("state", model);
            var result = GuiRuntime.Services.InvokePlugin(new II.RPA.Plugin.Abstractions.PluginRoute("DesktopInput", "SetState"), j.ToString());
        }
        #endregion
        public string GetSubElements()
        {
            if (!accessBridgeProvider.CheckAccess())
                return accessBridgeProvider.Invoke(GetSubElements);

            if (this.Children.Count <= 0)
            {
                throw new Exception(string.Format("不存在子元素".I18n("controlType_JavaNode_C81BE4C3")));
            }

            try
            {
                JArray arrayAllSelector = new JArray();
                JArray selectorProps = JArray.Parse("");
                SelectorAdapter adapter = new SelectorAdapter();
                for (int i = 0; i < this.Children.Count; i++)
                {
                    var item = this.Children[i];

                    Core.Commons.SelectorProps selectorItem = new Core.Commons.SelectorProps()
                    {
                        Name = item.Role,
                        Props = new List<Core.Commons.Props>()
                    };

                    if (item.Name != null & item.Name != string.Empty)
                    {
                        Core.Commons.Props prop = new Core.Commons.Props()
                        {
                            Name = ConstFields.SELECTOR_NAME,
                            Value = item.Name
                        };
                        selectorItem.Props.Add(prop);
                    }
                    else
                    {
                        Core.Commons.Props prop = new Core.Commons.Props()
                        {
                            Name = ConstFields.SELECTOR_INDEX_IN_PARENT,
                            Value = item.Idx,
                            Accurate = ConstFields.PROP_ACCURATE_TURE,
                            Pattern = ConstFields.PATTERN_EQUAL
                        };
                        selectorItem.Props.Add(prop);
                    }
                    var subObj = JObject.FromObject(selectorItem);
                    selectorProps.Add(subObj);
                    string subSelector = selectorProps.ToString();
                    selectorProps.Remove(subObj);
                    subSelector = SelectorAdapter.Convert2EditIndexFormat(subSelector);
                    arrayAllSelector.Add(JObject.Parse(subSelector));
                }
                return JsonConvert.SerializeObject(arrayAllSelector);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public string GetParent(ParamGetParent param)
        {
            throw new NotImplementedException();
        }

        public string GetSiblings(ParamGetSiblings param)
        {
            throw new NotImplementedException();
        }

        public List<Core.Commons.SelectorProps> MatchNextButtonSelector(ParamDoElementEditorModel paramModel)
        {
            throw new NotImplementedException();
        }

        public List<Core.Commons.SelectorProps> AutoBulidCollectDataSelector(ParamBulidCollectDataSelectorModel paramModel)
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
            throw new NotImplementedException();
        }

        public List<Core.Commons.SelectorProps> CompareBulidCollectDataSelector(ParamBulidCollectDataSelectorModel paramModel)
        {
            //List<SelectorProps> originalSelectorPropsList = paramModel.OriginalSelector.ToObject<List<SelectorProps>>();
            //List<SelectorProps> similarSelectorPropsList = paramModel.CompareOriginalSelector.ToObject<List<SelectorProps>>();
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
            //return ChromeTab.CompareBulidSimileSelector(originalSelectorPropsList, similarSelectorPropsList);
            throw new NotImplementedException();
        }

        public string GetNextElementBuildSelector(List<string> cssSelectorList)
        {
            throw new NotImplementedException();
            //try
            //{
            //    chrome = chrome ?? new Chrome();
            //    for (int i = 0; i < 10; i++)//找10 次
            //    {
            //        chromeTab = chromeTab ?? chrome.Acquire("", "", "", 1);
            //        if (chromeTab != null)
            //        {
            //            if (cssSelectorList.Count == 0)
            //            {
            //                cssSelectorList.Add("");//
            //            }
            //            foreach (var cssSelector in cssSelectorList)
            //            {
            //                string btnSelector = chromeTab.GetNextElementBuildSelector(cssSelector);
            //                if (!string.IsNullOrEmpty(btnSelector))
            //                {
            //                    return btnSelector;
            //                }
            //            }
            //        }
            //        else
            //        {
            //            Thread.Sleep(100);
            //            NLogger.Default.Info("未获取到ChromeTab信息");
            //        }
            //    }
            //    return null;
            //}
            //catch (Exception e)
            //{
            //    throw new ChromeException(eErrorMessageType.Error_NotFoundElement);
            //}
        }

        public string GetHtml()
        {
            throw new NotImplementedException();
        }

        public void MouseThreeClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            throw new NotImplementedException();
        }

        public void MouseRightThreeClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            throw new NotImplementedException();
        }

        public void MouseConterThreeClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            throw new NotImplementedException();
        }

        public void SetText(string text, int isReplace, int inputType, int interval)
        {
            throw new NotImplementedException();
        }

        public void IntoView()
        {
            if (!accessBridgeProvider.CheckAccess())
            {
                accessBridgeProvider.Invoke(IntoView);
                return;
            }

            try
            {
                SetForeground();
                Thread.Sleep(100);

                var elemRect = BoundingRectangle;
                logger.LogInformation($"[Java IntoView] elemRect={elemRect}");

                if (elemRect.Width <= 0 || elemRect.Height <= 0)
                {
                    logger.LogInformation("[Java IntoView] SKIP — element rect invalid");
                    return;
                }

                IntPtr frameHwnd = NativeMethods.GetForegroundWindow();
                if (frameHwnd == IntPtr.Zero) return;

                var frameElement = GUI.Commons.UiCommon.UIAAutomation.FromHandle(frameHwnd);
                if (frameElement == null) return;
                var windowRect = frameElement.BoundingRectangle;

                int centerX = elemRect.X + elemRect.Width / 2;
                int centerY = elemRect.Y + elemRect.Height / 2;

                logger.LogInformation($"[Java IntoView] elem=({centerX},{centerY}), win=({windowRect.Left},{windowRect.Top})-({windowRect.Right},{windowRect.Bottom})");

                if (centerX >= windowRect.Left && centerX <= windowRect.Right &&
                    centerY >= windowRect.Top && centerY <= windowRect.Bottom)
                {
                    logger.LogInformation("[Java IntoView] SKIP — already visible");
                    return;
                }

                int direction = centerY > windowRect.Bottom ? 1 : -1;

                // --- 记录滚动前的scrollbar值 ---
                int scrollBefore = GetScrollPaneOffset();
                logger.LogInformation($"[Java IntoView] scrollBefore={scrollBefore}");

                // 步骤1: JAB原生滚动
                TryScrollViaAccessBridge(direction);

                // 步骤2: PostMessage WM_MOUSEWHEEL
                IntPtr canvasHwnd = FindSunAwtCanvasChild(frameHwnd);
                logger.LogInformation($"[Java IntoView] canvasHwnd=0x{canvasHwnd.ToInt64():X}");

                int safeX = centerX;
                int safeY = direction > 0 ? windowRect.Bottom - 40 : windowRect.Top + 40;
                safeX = Math.Max(safeX, windowRect.Left + 10);
                safeX = Math.Min(safeX, windowRect.Right - 10);
                safeY = Math.Max(safeY, windowRect.Top + 10);
                safeY = Math.Min(safeY, windowRect.Bottom - 10);
                Mouse.Position = new Point(safeX, safeY);
                Thread.Sleep(30);

                // 每页 = 1200 wheel 单位。元素距窗口底部距离 / 30px每行 ≈ 需要的滚动量
                int scrollAmount = direction > 0
                    ? Math.Max((centerY - windowRect.Bottom) / 30 + 2, 2)   // 元素在窗口下方
                    : Math.Max((windowRect.Top - centerY) / 30 + 2, 2);    // 元素在窗口上方

                for (int i = 0; i < scrollAmount; i++)
                {
                    int wParam = (direction * -1200) << 16;
                    int lParam = (safeY << 16) | (safeX & 0xFFFF);
                    NativeMethods.PostMessage(canvasHwnd != IntPtr.Zero ? canvasHwnd : frameHwnd,
                        0x020A, new IntPtr(wParam), new IntPtr(lParam));
                    Thread.Sleep(30);
                }

                // 等待渲染后读scrollbar计算修正值
                Thread.Sleep(200);
                int scrollAfter = GetScrollPaneOffset();
                logger.LogInformation($"[Java IntoView] scrollAfter={scrollAfter}");

                // --- 计算Y修正值，让BoundingRectangle返回正确坐标 ---
                _scrollYCorrection = scrollAfter - scrollBefore;
                logger.LogInformation($"[Java IntoView] _scrollYCorrection={_scrollYCorrection}, correctedY={elemRect.Y - _scrollYCorrection}");
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "Java IntoView failed, continuing with click");
            }
        }

        /// <summary>
        /// 从AccessBridge树中查找scroll pane的垂直scrollbar，返回当前value(滚动偏移量)
        /// </summary>
        private int GetScrollPaneOffset()
        {
            try
            {
                var node = accessibleContextNode;
                for (int d = 0; d < 30; d++)
                {
                    if (node == null) break;
                    var info = node.GetInfo();
                    if (info == null) break;
                    string role = (info.role_en_US ?? "").ToLower();

                    if (role == "scroll pane")
                    {
                        var children = node.GetChildren();
                        foreach (var child in children)
                        {
                            var ctx = child as AccessibleContextNode;
                            if (ctx == null) continue;
                            var cinfo = ctx.GetInfo();
                            if ((cinfo?.role_en_US ?? "").ToLower() == "scroll bar")
                            {
                                var sb = new StringBuilder(64);
                                if (accessBridge.Functions.GetCurrentAccessibleValueFromContext(
                                    ctx.JvmId, ctx.AccessibleContextHandle, sb, (short)sb.Capacity))
                                {
                                    if (int.TryParse(sb.ToString(), out int val))
                                        return val;
                                }
                            }
                        }
                        break;
                    }
                    var parent = node.GetParent();
                    node = parent as AccessibleContextNode;
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, "GetScrollPaneOffset error");
            }
            return 0;
        }

        /// <summary>
        /// 在Java frame窗口下查找SunAwtCanvas子窗口——这是真正处理鼠标滚轮消息的窗口
        /// </summary>

        private static IntPtr FindSunAwtCanvasChild(IntPtr frameHwnd)
        {
            try
            {
                var frame = GUI.Commons.UiCommon.UIAAutomation.FromHandle(frameHwnd);
                if (frame == null) return IntPtr.Zero;
                var children = frame.FindAllChildren();
                foreach (var child in children)
                {
                    if (child.Properties.ClassName.IsSupported)
                    {
                        string cn = child.Properties.ClassName.ValueOrDefault ?? "";
                        // SunAwtCanvas 处理 Java AWT 鼠标输入
                        if (cn == "SunAwtCanvas")
                        {
                            if (child.Properties.NativeWindowHandle.IsSupported)
                                return child.Properties.NativeWindowHandle.ValueOrDefault;
                        }
                    }
                }
            }
            catch { }
            return IntPtr.Zero;
        }

        /// <summary>
        /// 用Java AccessBridge原生方式滚动：向上查找scroll pane，操作其vertical scrollbar。
        /// </summary>
        private bool TryScrollViaAccessBridge(int direction)
        {
            try
            {
                if (accessibleContextNode == null) return false;

                var node = accessibleContextNode;
                for (int depth = 0; depth < 30; depth++)
                {
                    var info = node.GetInfo();
                    if (info == null) break;

                    string role = (info.role_en_US ?? "").ToLower();
                    logger.LogInformation($"[JAB Scroll] depth={depth}, role='{role}', name='{info.name}'");

                    if (role == "scroll pane")
                    {
                        var children = node.GetChildren();
                        List<AccessibleContextNode> candidates = new List<AccessibleContextNode> { node };

                        foreach (var child in children)
                        {
                            var childCtx = child as AccessibleContextNode;
                            if (childCtx == null) continue;
                            var childInfo = childCtx.GetInfo();
                            logger.LogInformation($"[JAB Scroll] child role='{childInfo?.role_en_US}'");
                            candidates.Add(childCtx);
                        }

                        foreach (var candidate in candidates)
                        {
                            var cinfo = candidate.GetInfo();
                            logger.LogInformation($"[JAB Scroll] trying actions on role='{cinfo?.role_en_US}'");
                            bool result = TryScrollbarPageAction(candidate, direction);
                            if (result) return true;
                        }
                        break;
                    }

                    var parent = node.GetParent();
                    if (parent == null) break;
                    node = parent as AccessibleContextNode;
                    if (node == null) break;
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, "TryScrollViaAccessBridge error");
            }
            return false;
        }

        private bool TryScrollbarPageAction(AccessibleContextNode target, int direction)
        {
            try
            {
                AccessibleActions actions;
                if (!accessBridge.Functions.GetAccessibleActions(target.JvmId, target.AccessibleContextHandle, out actions))
                {
                    logger.LogInformation("[JAB Scroll] GetAccessibleActions failed");
                    return false;
                }

                logger.LogInformation($"[JAB Scroll] actions count={actions.actionsCount}");
                for (int a = 0; a < actions.actionsCount; a++)
                    logger.LogInformation($"[JAB Scroll] action[{a}]: '{actions.actionInfo[a].name}'");

                // 查page increment/decrement或increment/decrement
                string[] preferredNames = direction > 0
                    ? new[] { "block increment", "blockincrement", "page increment", "pageincrement", "increment", "increase", "down", "page down" }
                    : new[] { "block decrement", "blockdecrement", "page decrement", "pagedecrement", "decrement", "decrease", "up", "page up" };

                AccessibleActionInfo? found = null;
                foreach (var name in preferredNames)
                {
                    for (int a = 0; a < actions.actionsCount; a++)
                    {
                        var an = (actions.actionInfo[a].name ?? "").ToLower().Trim();
                        if (an == name || an.Contains(name))
                        { found = actions.actionInfo[a]; break; }
                    }
                    if (found != null) break;
                }

                if (found == null)
                {
                    logger.LogInformation("[JAB Scroll] No matching action found");
                    return false;
                }

                logger.LogInformation($"[JAB Scroll] executing: '{found.Value.name}'");

                int failure;
                var actiontodo = new AccessibleActionsToDo();
                actiontodo.actions = new AccessibleActionInfo[32];
                actiontodo.actions[0] = found.Value;
                actiontodo.actionsCount = 1;
                return accessBridge.Functions.DoAccessibleActions(target.JvmId, target.AccessibleContextHandle, ref actiontodo, out failure);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, "TryScrollbarPageAction error");
                return false;
            }
        }

        public void MouseConterClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            MouseMove(clickParams.cursorPos, offsetX, offsetY);
            GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => GUIUtils.MouseAction(clickParams.RPAMousePresses, RPAMouseButtons.Middle));
        }

        public void MouseConterDoubleClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            MouseMove(clickParams.cursorPos, offsetX, offsetY);
            GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => Mouse.DoubleClick(MouseButton.Middle));
        }
        public string GetSubElements(ParamGetSubElements param)
        {
            if (!accessBridgeProvider.CheckAccess())
                return accessBridgeProvider.Invoke(() => GetSubElements(param));

            //未单独实现的类型，按照老的逻辑返回子元素
            return this.GetSubElements();
        }
        public IGuiNode FindRelativeNode(int position, int offsetX, int offsetY)
        {
            throw new NotImplementedException();
        }

        public List<IGuiNode> FindAll(IGuiNode fromElement = null, int maxResult = 1, int milliseconds = 3000)
        {
            if (!accessBridgeProvider.CheckAccess())
                return accessBridgeProvider.Invoke(
                    () => FindAll(fromElement, maxResult, milliseconds));

            List<IGuiNode> result = null;
            var sw = new Stopwatch();
            sw.Start();
            int index = 0;
            var timeout = TimeSpan.FromMilliseconds(milliseconds);
            do
            {
                logger.LogDebug($"第{index}次查找：{result.Count}, Elapsed{sw.Elapsed}, timeout{timeout}");

                foreach (var jvm in EnumJvms())
                {
                    result = GetElementsWithuiSelector(jvm, fromElement as JavaNode, maxResult);
                    if (result?.Count() > 0)
                    {
                        List<IGuiNode> temp = new List<IGuiNode>();
                        foreach (var item in result)
                        {
                            temp.Add(item);
                        }
                        return temp;
                    }
                }
            }
            while (result.Count == 0 && sw.Elapsed < timeout);
            return new List<IGuiNode>();
        }
        public void MouseClickEx(int offsetX, int offsetY, bool issimulate, int cursorPos = 0, RPAMousePresses mousePresses = RPAMousePresses.Click)
        {
            if (!accessBridgeProvider.CheckAccess())
            {
                accessBridgeProvider.Invoke(
                    () => MouseClickEx(offsetX, offsetY, issimulate, cursorPos, mousePresses));
                return;
            }

            try
            {
                IntoView();
                if (issimulate)
                {
                    MouseMove(cursorPos, offsetX, offsetY);
                    Thread.Sleep(200);
                    GUIUtils.MouseAction(mousePresses, RPAMouseButtons.Left);
                }
                else
                {
                    AccessibleActions _actions;
                    var actions = new Dictionary<string, object>();
                    if (accessBridge.Functions.GetAccessibleActions(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, out _actions))
                    {
                        if (_actions.actionsCount > 0)
                        {
                            foreach (var a in _actions.actionInfo)
                            {
                                if (actions.Count >= _actions.actionsCount) continue;
                                if (!string.IsNullOrEmpty(a.name))
                                {
                                    if (!actions.ContainsKey(a.name))
                                    {
                                        actions.Add(a.name, a);
                                    }
                                }
                            }
                        }
                    }
                    if (actions.ContainsKey("单击") || actions.ContainsKey("click"))
                    {
                        int failure = 0;
                        AccessibleActionInfo info = new AccessibleActionInfo();
                        if (actions.ContainsKey("单击"))
                        {
                            info = (AccessibleActionInfo)actions["单击"];
                        }
                        else
                        {
                            info = (AccessibleActionInfo)actions["click"];
                        }
                        AccessibleActionsToDo actiontodo = new AccessibleActionsToDo();
                        actiontodo.actions = new AccessibleActionInfo[32];
                        actiontodo.actions[0] = info;
                        actiontodo.actionsCount = 1;
                        if (accessBridge.Functions.DoAccessibleActions(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, ref actiontodo, out failure))
                        {
                            logger.LogDebug("Success, click!");
                        }
                        else
                        {
                            throw new JavaException(eErrorMessageType.Error_ClickError, eComponentType.ElementClick);
                        }
                    }
                    else
                    {
                        // throw new RPAExecuteException("元素不支持点击操作");
                        MouseMove(cursorPos, offsetX, offsetY);
                        Mouse.LeftClick();
                    }
                }
            }
            catch (JavaException e)
            {
                throw new JavaException(eErrorMessageType.Error_ClickError, eComponentType.ElementClick, e);
            }
            catch (Exception e)
            {
                throw new RPAException(eGuiType.Java, eErrorMessageType.Error_ClickError, eComponentType.ElementClick, e);
            }
        }

        public void MouseClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => MouseClickEx(offsetX, offsetY, !(clickParams.operateType == 0 && offsetX == 0 && offsetY == 0), clickParams.cursorPos, RPAMousePresses.Click));
        }

        public void MouseDoubleClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            MouseMove(clickParams.cursorPos, offsetX, offsetY);
            GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => Mouse.LeftDoubleClick());
        }
        public void MouseRightClickEx(int offsetX, int offsetY, int cursorPos = 0, RPAMousePresses mousePresses = RPAMousePresses.Click)
        {
            MouseMove(cursorPos, offsetX, offsetY);
            GUIUtils.MouseAction(mousePresses, RPAMouseButtons.Right);
        }
        public void MouseRightClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => MouseRightClickEx(offsetX, offsetY, clickParams.cursorPos, RPAMousePresses.Click));
        }

        public void MouseRightDoubleClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            if (clickParams == null)
            {
                clickParams = new UiElementClickParams();
            }

            var clickablePoint = GetClickablePoint();
            Mouse.Position = new Point(clickablePoint.X + offsetX, clickablePoint.Y + offsetY);

            GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => Mouse.RightDoubleClick());
        }


        public void MouseHover(UiElementHoverParams hoverParams = null)
        {
            MouseMove(hoverParams.cursorPos);
        }

        public void Focus()
        {

        }

        public IGuiNode GetChildByIdx(int idx)
        {
            return null;
        }

        public string GetText()
        {
            if (!accessBridgeProvider.CheckAccess())
                return accessBridgeProvider.Invoke(() => GetText());

            try
            {
                if (info.accessibleText != 0)
                {
                    int len = 32767;
                    if (accessBridge.Functions.GetAccessibleTextInfo(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, out var textInfo, 0, 0))
                    {
                        len = Math.Min(textInfo.charCount, len);
                    }
                    StringBuilder sb = new StringBuilder(len);
                    string value = "";
                    accessBridge.Functions.GetAccessibleContextInfo(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, out AccessibleContextInfo info);
                    if ((info.accessibleInterfaces & AccessibleInterfaces.cAccessibleValueInterface) != 0 && accessBridge.Functions.GetCurrentAccessibleValueFromContext(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, sb, (short)len))
                    {
                        value = sb.ToString();
                    }
                    string text = "";
                    if ((info.accessibleInterfaces & AccessibleInterfaces.cAccessibleTextInterface) != 0)
                    {
                        for (int i = 0; i < len; i++)
                        {
                            if (accessBridge.Functions.GetAccessibleTextItems(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, out var itemsInfo, i))
                            {
                                sb.Append(itemsInfo.letter);
                            }
                        }
                        text = sb.ToString();
                    }
                    if (!string.IsNullOrEmpty(value))
                    {
                        return "";
                    }
                    return text;
                }
                if (info.accessibleSelection != 0)
                {
                    if (Role.Contains("combo"))
                    {
                        var tmp = accessBridge.Functions.GetAccessibleSelectionFromContext(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, 0);
                        AccessibleContextNode acn = new AccessibleContextNode(accessBridge, tmp);
                        var info = acn.GetInfo();
                        return info.name;
                    }
                    else
                    {
                        AccessibleTextSelectionInfo textItems;
                        if (accessBridge.Functions.GetAccessibleTextSelectionInfo(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, out textItems))
                        {
                            var result = textItems.selectedText;
                            if (result.EndsWith("\n")) result = result.Substring(0, result.Length - 1);
                            return result;
                        }
                    }
                }
                return info.name;
            }
            catch (JavaException e)
            {
                throw new JavaException(eErrorMessageType.Error_GetTextError, eComponentType.GetText, e);
            }
            catch (Exception e)
            {
                throw new RPAException(eGuiType.Java, eErrorMessageType.Error_GetTextError, eComponentType.GetText, e);
            }
        }

        public string GetTableData(string input)
        {
            if (!accessBridgeProvider.CheckAccess())
                return accessBridgeProvider.Invoke(() => GetTableData(input));

            string reuslt = string.Empty;
            try
            {
                List<List<string>> sub = new List<List<string>>();
                AccessibleTableInfo tableInfo;
                bool brt = AccessBridgeProvider.Singleton.AccessBridgeInstance.Functions.GetAccessibleTableInfo(this.accessibleNode.JvmId, this.accessibleContextNode.AccessibleContextHandle, out tableInfo);
                if (brt == true)
                {
                    int column = tableInfo.columnCount;
                    int row = tableInfo.rowCount;

                    for (int i = 0; i < row; i++)
                    {
                        List<string> items = new List<string>();
                        for (int j = 0; j < column; j++)
                        {
                            AccessibleTableCellInfo cellInfo;
                            AccessBridgeProvider.Singleton.AccessBridgeInstance.Functions.GetAccessibleTableCellInfo(this.accessibleNode.JvmId, this.accessibleContextNode.AccessibleContextHandle, i, j, out cellInfo);
                            AccessibleContextNode cn = new AccessibleContextNode(AccessBridgeProvider.Singleton.AccessBridgeInstance, cellInfo.accessibleContext);

                            var info = cn.GetInfo();
                            //var node = GetNodeByColRow(j, i);
                            items.Add(info.name);
                        }
                        sub.Add(items);
                    }

                    var rowCount = sub.FirstOrDefault().Count;
                    List<string> rowFirst = new List<string>();
                    for (int i = 0; i < rowCount; i++)
                    {
                        rowFirst.Add("");
                    }
                    sub.Insert(0, rowFirst);
                    reuslt = Newtonsoft.Json.JsonConvert.SerializeObject(sub);
                }
            }
            catch (RPAException e)
            {
                throw e;
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                throw new RPAException(eErrorMessageType.Error_GetTableDataError);
            }
            return reuslt;
        }

        public string ClickTableCell(string input)
        {
            throw new NotImplementedException();
        }

        public void SetElementAttribute(string name, string value)
        {

        }

        public string GetElementAttribute(string name, string apiVersion)
        {
            return string.Empty;
        }

        public string GetElementSelect(int selectType = 1)
        {
            if (!accessBridgeProvider.CheckAccess())
                return accessBridgeProvider.Invoke(() => GetElementSelect(selectType));

            try
            {
                if (Role.Contains(ConstFields.ROLE_VALUE_COMBO))
                {
                    var tmp = accessBridge.Functions.GetAccessibleSelectionFromContext(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, 0);
                    AccessibleContextNode acn = new AccessibleContextNode(accessBridge, tmp);
                    var info = acn.GetInfo();
                    return info.name;
                }
                else
                {
                    throw new JavaException(eErrorMessageType.Error_ElementNotOperation);
                }
            }
            catch (JavaException e)
            {
                throw new JavaException(e.ErrorMessageType, eComponentType.GetElementSelect, e);
            }
            catch (Exception e)
            {
                throw new RPAException(eGuiType.Java, eErrorMessageType.Error_GetSelectError, eComponentType.GetElementSelect, e);
            }
        }

        public string GetEnableClick()
        {
            var result = ButtonClickEnable();
            return result == true ? "true" : "false";
        }

        public void SetElementMultipleSelect(int selectType, string setValue)
        {

        }

        public string FindElementWithPicPath(string strPicPath)
        {
            return "true";
        }

        public string FindRelativeElementsInfo(string strRelativeSelector, ref JArray context)
        {
            return string.Empty;
        }

        public Bitmap ElementToBitmap()
        {
            throw new NotImplementedException();
        }

        public IGuiNode[] QueryTargetSelectorAll(ParamQueryTargetSelectorAllModel param)
        {
            throw new NotImplementedException();
        }

        public string GetElementAttribute_V1(string name, bool isIntact, string apiVersion)
        {
            throw new NotImplementedException();
        }

        public List<string> GetSelectorAll(List<IGuiNode> guiNodes, string sourceSelector)
        {
            List<string> list_selector = new List<string>();

            foreach (var _node in guiNodes)
            {
                var javaNode = _node as JavaNode;
                list_selector.Add(SelectorAdapter.BuildSimilarSelector(sourceSelector, javaNode.PathSegments));
            }
            return list_selector;
        }

        public List<Rectangle> GetRegionAll(List<IGuiNode> guiNodes)
        {
            List<Rectangle> rectangles = new List<Rectangle>();
            foreach (var node in guiNodes)
            {
                rectangles.Add(node.BoundingRectangle);
            }
            return rectangles;
        }

        public List<string> GetTextAll(List<IGuiNode> guiNodes)
        {
            List<string> list_text = new List<string>();
            foreach (var node in guiNodes)
            {
                list_text.Add(node.GetText());
            }
            return list_text;
        }

        public List<string> GetHtmlAll(List<IGuiNode> guiNodes)
        {
            throw new NotImplementedException();
        }

        public List<string> GetAttributeAll(List<IGuiNode> guiNodes, string attributeName, bool isIntact, string apiVersion)
        {
            List<string> list_attr = new List<string>();
            foreach (var node in guiNodes)
            {
                list_attr.Add(node.GetElementAttribute(attributeName, apiVersion));
            }
            return list_attr;
        }

        public void SetElementSelectWithMatchModel(int selectType, int matchModel, string setValue)
        {
            setValue = setValue.ToObject<List<string>>((ex) =>
            {
                throw new RPAException(this.GuiType, eErrorMessageType.Error_SelectValueIsNotValid, ex);
            }).First();
            SetElementSelect(selectType, setValue);
        }

        public string GetCleanHtml()
        {
            throw new NotImplementedException();
        }
    }
}

