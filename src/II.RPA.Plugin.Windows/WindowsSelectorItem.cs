using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using IAccessible2Lib;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.WindowMatch;
using II.RPA.Plugin.Windows.Config;
using ISimpleDOM;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Automation.Provider;

namespace II.RPA.Plugin.Windows
{
    // This is the COM IServiceProvider interface, not System.IServiceProvider .Net interface!
    [ComImport(), ComVisible(true), Guid("6D5140C1-7436-11CE-8034-00AA006009FA"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IServiceProvider
    {
        [return: MarshalAs(UnmanagedType.I4)]
        [PreserveSig]
        int QueryService(ref Guid guidService, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppvObject);
    }

    public class WindowsSelectorItem : SelectorItem
    {
        private static ILogger logger = Logger.OfContext();

        [DllImport("user32.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsHungAppWindow(IntPtr hWnd);

        //比如class,id这些属性，本地元素中也有，所以目前需要加个前缀进行区分
        const string WebPropertyPrefix = "<web>";//之所以用尖括号，是因为这样可以把原属性名称变成非法的属性名称，因此不会和网页中用户自己取的其他属性名称重复
        string WebPN(string n)
        {
            return WebPropertyPrefix + n;
        }
        bool IsWebPN(string test_pn, out string real_pn)
        {
            if (test_pn.StartsWith(WebPropertyPrefix))
            {
                real_pn = test_pn.Substring(WebPropertyPrefix.Length);
                return true;
            }
            else
            {
                real_pn = test_pn;
                return false;
            }
        }
        public WindowsSelectorItem(IntPtr parentHWnd, IntPtr hWnd)
        {
            var element = UiCommon.UIAAutomation.FromHandle(hWnd);
            this.Element = new UIANode(element);
            this.Properties = new List<SelectorItemProperty>();
            this.ElementType = SelectorPropsType.Windows;
            this.ElementName = this.Element.ControlType;
            try
            {
                WindowObject wnd = null;
                if (hWnd != IntPtr.Zero)
                {
                    if (parentHWnd != IntPtr.Zero)
                    {
                        WindowObject parentWnd = new WindowObject(parentHWnd);
                        int indexInParent = -1;
                        wnd = parentWnd.FindAllChildWindowEx(childWnd =>
                        {
                            indexInParent++;
                            return childWnd.Handle == hWnd;
                        }).FirstOrDefault();
                        if (wnd != null && indexInParent > -1)
                        {
                            this.Properties.Add(new WinSelectorItemProperty(ConstFields.SELECTOR_INDEX_IN_PARENT, indexInParent.ToString()) { Accurate = false });
                        }
                        else
                        {
                            this.ElementType = this.ElementType | SelectorPropsType.DetachedChildWindow;
                        }
                    }
                    else
                    {
                        var sysRootProperties = WindowsSelectorItem.GenerateRootElementProps(element);
                        //如果是首节点那么添加首节点必要的数据，譬如进程节点，filePath等等。
                        this.Properties.AddRange(sysRootProperties);
                    }
                    wnd = wnd ?? new WindowObject(hWnd);
                    if (wnd.Title.Length > 0)
                    {
                        var titleSelectorItem = new WinSelectorItemProperty(ConstFields.SELECTOR_TITLE, wnd.Title);
                        titleSelectorItem.Accurate = true;
                        this.Properties.Add(titleSelectorItem);
                    }
                    if (wnd.ClassName.Length > 0)
                    {
                        var classNameSelectorItem = new WinSelectorItemProperty(ConstFields.SELECTOR_CLASS_NAME, wnd.ClassName);
                        classNameSelectorItem.Accurate = true;
                        string pattern = @"WindowsForms\d+\..*";
                        Regex regex = new Regex(pattern);
                        if (regex.IsMatch(wnd.ClassName))
                        {
                            classNameSelectorItem.OriginalValue = @"WindowsForms\d+\..*";
                            classNameSelectorItem.Pattern = ConstFields.PATTERN_REGULAR;
                        }
                        this.Properties.Add(classNameSelectorItem);
                    }
                    var nameSelectorItem = element.Properties.Name.GenerateSelectorProperty(ConstFields.SELECTOR_NAME);
                    if (nameSelectorItem != null)
                    {
                        nameSelectorItem.Accurate = wnd.Title.Length <= 0;
                        this.Properties.Add(nameSelectorItem);
                    }
                    var automationIdSelectorItem = element.Properties.AutomationId.GenerateSelectorProperty(ConstFields.SELECTOR_AUTOMATION_ID);
                    if (automationIdSelectorItem != null)
                    {
                        automationIdSelectorItem.Accurate = false;
                        this.Properties.Add(automationIdSelectorItem);
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError(e);
            }
            foreach (var p in this.Properties)
            {
                p.Enabled = true;
            }
            Enabled = true;
        }
        public WindowsSelectorItem(AutomationElement element, IItemContainerProvider itemContainer, int anIndex = 0, int indexInParent = -1)
        {
            this.Element = new UIANode(element);
            this.Properties = new List<SelectorItemProperty>();

            //TODO 假如ControlType不存在
            this.ElementType = element.AutomationType == AutomationType.UIA3 ? SelectorPropsType.UIA3 : SelectorPropsType.UIA2;
            this.ElementType = this.ElementType | SelectorPropsType.RawElement;
            this.ElementName = element.Properties.ControlType.IsSupported ? element.Properties.ControlType.ToString() : string.Empty;
            if (this.ElementName.EqualIgnoreCase(ConstFields.PROP_VALUE_CONTROL_TYPE_UNKNOW) || string.IsNullOrEmpty(this.ElementName))
            {
                this.ElementName = ConstFields.SELECTOR_Custom;
            }
            try
            {
                var nameSelectorItem = element.Properties.Name.GenerateSelectorProperty(ConstFields.SELECTOR_NAME);
                var classNameSelectorItem = element.Properties.ClassName.GenerateSelectorProperty(ConstFields.SELECTOR_CLASS_NAME);
                var automationIdSelectorItem = element.Properties.AutomationId.GenerateSelectorProperty(ConstFields.SELECTOR_AUTOMATION_ID);
                if (element.Properties.ControlType.IsSupported
                    && element.ControlType == FlaUI.Core.Definitions.ControlType.Window
                    && anIndex == 0)
                {
                    IntPtr hWnd = IntPtr.Zero;
                    if (!element.Properties.NativeWindowHandle.TryGetValue(out hWnd))
                    {
                        logger.LogDebug($"NativeWindowHandle：{hWnd}");
                        if (!element.TryGetWindowHandleFromAccessible(out hWnd))
                        {
                            logger.LogDebug($"TryGetWindowHandleFromAccessible：{hWnd}");
                        }
                    }
                    if (nameSelectorItem != null)
                    {
                        var wndText = UiCommon.GetWindowText(hWnd);
                        if (wndText.Length > 0)
                        {
                            nameSelectorItem.Accurate = true;
                            nameSelectorItem.OriginalValue = wndText;
                        }
                        else
                        {
                            nameSelectorItem.Accurate = nameSelectorItem.OriginalValue?.ToString().Length > 0;
                        }
                        this.Properties.Add(nameSelectorItem);
                    }
                    if (classNameSelectorItem != null)
                    {
                        if (hWnd != IntPtr.Zero)
                        {
                            StringBuilder classNameSB = new StringBuilder(256);
                            NativeMethods.GetClassName(hWnd, classNameSB, 256);
                            //classNameSelectorItem.Accurate = classNameSB.Length > 0;  //element.IsUniqueItemByClassName();
                            classNameSelectorItem.OriginalValue = classNameSB.ToString();
                        }
                        classNameSelectorItem.Accurate = classNameSelectorItem.OriginalValue?.ToString().Length > 0;
                        this.Properties.Add(classNameSelectorItem);
                    }
                    if (automationIdSelectorItem != null)
                    {
                        automationIdSelectorItem.Accurate = false;
                        this.Properties.Add(automationIdSelectorItem);
                    }
                }
                else
                {
                    if (nameSelectorItem != null)
                    {
                        nameSelectorItem.Accurate = !nameSelectorItem.Value.HasInvalidCharacters();//element.IsUniqueItemByName();
                        this.Properties.Add(nameSelectorItem);
                    }
                    if (classNameSelectorItem != null)
                    {
                        classNameSelectorItem.Accurate = true; //element.IsUniqueItemByClassName();
                        this.Properties.Add(classNameSelectorItem);
                    }
                }

                if (anIndex > 0 && indexInParent >= 0)
                {
                    this.Properties.Add(new WinSelectorItemProperty(ConstFields.SELECTOR_INDEX_IN_PARENT, indexInParent.ToString()) { Accurate = false });
                }

                if (element.Patterns.LegacyIAccessible.IsSupported)
                {
                    var acc = element.Patterns.LegacyIAccessible.Pattern.GetIAccessible();
                    if (acc == null)
                    {
                        if (itemContainer != null)
                        {
                            acc = UIANode.ConvertToAccByItemContainer(itemContainer, element);
                        }
                    }
                    if (acc != null)
                    {
                        IServiceProvider sp = (IServiceProvider)acc;
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
                                string attributes = acc2.attributes;
                                if (!string.IsNullOrEmpty(attributes))
                                {
                                    var arrAttributes = attributes.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                                    char[] chars_M = new char[] { ':' };
                                    foreach (var attr in arrAttributes)
                                    {
                                        var nv = attr.Split(chars_M);
                                        if (nv.Length > 0)
                                        {
                                            string n = WebPN(nv[0]);
                                            if (this.Properties.Exists(p => p.Name == n))
                                            {
                                                //实在RPA前端中目前有BUG，不允许出现重复的属性名称，所以这里暂时规避下
                                                continue;
                                            }
                                            string v = nv.Length > 1 ? nv[1] : string.Empty;
                                            this.Properties.Add(new WinSelectorItemProperty(n, v));
                                        }
                                    }
                                }
                            }

                            ISimpleDOMNode node;
                            {
                                object obj;
                                sp.QueryService(typeof(ISimpleDOMNode).GUID, typeof(ISimpleDOMNode).GUID, out obj);
                                node = obj as ISimpleDOMNode;
                            }
                            if (node != null)
                            {
                                ushort numAttribs = 100;
                                string[] names = new string[numAttribs];
                                string[] values = new string[numAttribs];
                                short[] nameSpaceIDs = new short[numAttribs];
                                node.get_attributes(numAttribs, names, nameSpaceIDs, values, out numAttribs);
                                if (numAttribs > 0)
                                {
                                    for (ushort i = 0; i < numAttribs; i++)
                                    {
                                        string n = WebPN(names[i]);
                                        if (this.Properties.Exists(p => p.Name == n))
                                        {
                                            //实在RPA前端中目前有BUG，不允许出现重复的属性名称，所以这里暂时规避下
                                            continue;
                                        }
                                        string v = values[i];
                                        if (v == null)
                                        {
                                            v = string.Empty;
                                        }
                                        this.Properties.Add(new WinSelectorItemProperty(n, v));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError(e);
            }
            foreach (var p in this.Properties)
            {
                p.Enabled = true;
            }
            Enabled = true;

            //父级节点中找不到的节点就不进行勾选
            if (element.Properties.ControlType.ValueOrDefault == ControlType.Window && element.Properties.NativeWindowHandle.ValueOrDefault == IntPtr.Zero)//这个条件后面可能会放大范围或去掉
            {
                var parent = element.Parent;
                if (parent != null)
                {
                    var runtimeId = element.Properties.RuntimeId.ValueOrDefault;
                    if (runtimeId != null)
                    {
                        var ele = parent.FindFirst(TreeScope.Subtree, new PropertyCondition(parent.Automation.PropertyLibrary.Element.RuntimeId, runtimeId));
                        if (ele == null)
                        {
                            //由于现在不支持默认不勾选，所以暂时先用不加入到节点树中的方式来实现
                            Enabled = false;
                            logger.LogDebug($"跳过了节点：{element}");
                        }
                    }
                }
            }
        }

        private WindowsSelectorItem()
        { }

        public string ElementName { get; set; }

        /// <summary>
        /// 放宽 ControlType 匹配。CEF/Chromium 冷启动时 UIA 树的 ControlType 可能与拾取时不同
        /// （如 Group→Text），设置此标记后 Match 方法跳过 ControlType 检查，仅匹配其他属性。
        /// </summary>
        internal bool SkipControlTypeMatch { get; set; }

        public SelectorPropsType ElementType
        {
            get; private set;
        }

        public string SelectorVersion { get; private set; }

        public override JObject json
        {
            get
            {
                SelectorProps selectorProp = new SelectorProps()
                {
                    Name = this.ElementName,
                    Accurate = this.Enabled.ToString().ToLower(),
                    Type = ((int)this.ElementType).ToString(),
                    Props = this.Properties.OfType<WinSelectorItemProperty>().Where(x => x.Enabled).Select(x => x.Convert2Props()).ToList()
                };

                return JObject.FromObject(selectorProp);
            }
        }

        /// <summary>
        /// 获取当前匹配条件是否是微信进程
        /// </summary>
        protected bool IsWeChatProcess
        {
            get
            {
                return this.Properties.Any(x =>
                {
                    bool c1 = x.Name.Equals(ConstFields.SELECTOR_CLASS_NAME, StringComparison.OrdinalIgnoreCase) && x.Value.Equals(ConstFields.PROP_CLASS_NAME_WECHAT);
                    bool c2 = x.Name.Equals(ConstFields.SELECTOR_NAME, StringComparison.OrdinalIgnoreCase) && x.Value.Equals(ConstFields.PROP_NAME_WECHAT_CH);
                    return (c1 || c2);
                });
            }
        }

        public static WindowsSelectorItem FromJObject(JObject o, string version)
        {
            WindowsSelectorItem selectorItem = new WindowsSelectorItem()
            {
                SelectorVersion = version
            };
            selectorItem.ElementName = o[ConstFields.SELECTOR_ELEMENT_NAME_TAG].ToString();
            if (int.TryParse(o[ConstFields.SELECTOR_ELEMENT_TYPE_TAG].ToString(), out int eleTypeVal))
            {
                selectorItem.ElementType = (SelectorPropsType)eleTypeVal;
            }
            else if (Enum.TryParse(o[ConstFields.SELECTOR_ELEMENT_TYPE_TAG].ToString(), out SelectorPropsType eleType))
            {
                selectorItem.ElementType = eleType;
            }
            var props = o[ConstFields.SELECTOR_ELEMENT_PROPS_TAG];
            var selectItemProperties = props.ToObject<List<WinSelectorItemProperty>>();
            selectItemProperties.ForEach(x => x.SelectorVersion = version);
            selectorItem.Properties = new List<SelectorItemProperty>(selectItemProperties);
            selectorItem.Enabled = o.Value<bool>(ConstFields.SELECTOR_ELEMENT_ACCURATE_TAG);
            return selectorItem;
        }

        public AutomationElement[] Matches(AutomationElement root, bool isDeskTop = false, bool useHiddenWindow = false, bool useChildrenWindow = true)
        {
            bool isExistStarProps;
            var matchs = new List<AutomationElement>();
            //因为自带的通过Condition查找的方式不支持通配符，所以需要自己后面进行特殊处理
            var conditions = GenerateConditionsWithoutStar(out isExistStarProps);
            AutomationElement[] elements = null;
            logger.LogDebug($"匹配条件：{conditions}");
            //如果是桌面顶层
            if (this.ElementType.HasFlag(SelectorPropsType.Windows))
            {
                if (!root.Properties.NativeWindowHandle.TryGetValue(out var rootHwnd))
                {
                    return null;
                }
                return this.MatchWindows(new WindowObject(rootHwnd))
                           .Select(wnd => wnd.AutomationElement)
                           .ToArray();
            }
            else if (isDeskTop)//老版本Selector首节点匹配逻辑
            {
                //微信匹配条件
                if (this.IsWeChatProcess && WeChatDataContext.CurrentProcessId != 0)
                {
                    var fItem = this.FindWeChatElement(root, conditions);
                    elements = fItem == null ? new AutomationElement[0] : new AutomationElement[] { fItem };
                }

                //常规Win32譬如WinForm查找匹配 //TODO 调整逻辑移除FindFirst分支,不用FindFirst存在性能问题，暂时无法解决。
                if (elements == null)
                {
                    logger.LogDebug("DeskTop FindFirst BEGIN:");
                    if (WinConfigurations.Info.MatchRootNodeMode == 1)
                    {
                        elements = this.FindSubChildren(root);//规避方案
                    }
                    else
                    {
                        //首节点包含* 匹配所有，否则匹配第一个
                        if (isExistStarProps)
                        {
                            logger.LogDebug("DeskTop FindFirst ALL:");
                            elements = this.FindSubChildren(root, conditions);
                        }
                        else
                        {
                            logger.LogDebug("DeskTop FindFirst First:");
                            var element = root.FindFirst(TreeScope.Children, conditions);
                            if (element != null)
                            {
                                elements = new AutomationElement[] { element };
                            }
                        }
                    }
                    logger.LogDebug("DeskTop FindFirst END");
                }
            }
            else //普通元素匹配
            {
                elements = this.FindSubChildren(root, conditions, useHiddenWindow);
                logger.LogDebug($"普通匹配元素数(Children)：{elements.Count()}");
                var isRawView = (this.ElementType & SelectorPropsType.RawElement) != 0;
                if (!isRawView)
                {
                    //在根据selector匹配元素时，存在找不到的情况，此时会扫描查找，不需要挑层
                    if (elements.Count() == 0)
                    {
                        //在生成selector时，会可能得到ControlType的类型是unknow,这种类型会被过滤掉，到时缺一层元素
                        elements = root.FindAll(TreeScope.Descendants, conditions);
                        //if (elements.Length == 0)
                        logger.LogDebug($"普通匹配元素数(Descendants)：{elements.Count()}");
                    }
                    if (elements == null || elements.Length == 0)
                    {
                        elements = this.FindSubChildren(root);
                    }
                }
            }

            //如果一个元素找不到，直接返回，ControlType查找元素必须>=1个
            if (elements == null)
            {
                return null;
            }

            //针对从父窗体找不到子元素的情况进行特殊处理；并级窗体，弹窗问题
            if (elements.Length == 0 && useChildrenWindow && this.FindAccordingName(ConstFields.SELECTOR_INDEX_IN_PARENT).Count() == 0)
            {
                logger.LogInformation("windows Immedite");
                elements = this.FindOutFrameSubChildren(root);
            }
            if (matchs.Count == 0)
            {
                matchs.AddRange(elements);
            }

            return matchs.ToArray();
        }

        public override string ToString()
        {
            return string.Join(",", this.Properties.Select(x => $"{x.Name}:{x.Value}"));
        }

        /// <summary>
        /// 获取不带通配符的匹配条件
        /// </summary>
        /// <param name="isExistStarProp">是否存在通配符的情况</param>
        /// <returns>不带通配符的条件</returns>
        internal AndCondition GenerateConditionsWithoutStar(out bool isExistStarProp)
        {
            var automation = UiCommon.UIAAutomation;
            var cond = new List<ConditionBase>();
            var props = this.Properties.OfType<WinSelectorItemProperty>().ToList();
            isExistStarProp = props.Exists(x => x.Accurate && x.Enabled && (!string.IsNullOrEmpty(x.Value) && x.Value.Contains("*")));
            var commonProps = props.OfType<WinSelectorItemProperty>().Where(x => x.Accurate && x.Enabled && (!string.IsNullOrEmpty(x.Value) && !x.Value.Contains("*")));

            if (!this.SkipControlTypeMatch)
            {
                ControlType ct = FlaUI.Core.Definitions.ControlType.Unknown;
                if (Enum.TryParse<ControlType>(this.ElementName, out ct) && ct != ControlType.Custom)
                {
                    cond.Add(new PropertyCondition(automation.PropertyLibrary.Element.ControlType, ct));
                }
            }
            foreach (var p in commonProps)
            {
                string propName = p.Name;
                string propValue = p.Value;

                if (p.Pattern.EqualIgnoreCase(ConstFields.PATTERN_EQUAL))
                {
                    if (propName.EqualIgnoreCase(ConstFields.SELECTOR_NAME))
                    {
                        cond.Add(new PropertyCondition(automation.PropertyLibrary.Element.Name, propValue));
                    }
                    else if (propName.EqualIgnoreCase(ConstFields.SELECTOR_CLASS_NAME))
                    {
                        cond.Add(new PropertyCondition(automation.PropertyLibrary.Element.ClassName, propValue));
                    }
                    else if (propName.EqualIgnoreCase(ConstFields.SELECTOR_AUTOMATION_ID))
                    {
                        cond.Add(new PropertyCondition(automation.PropertyLibrary.Element.AutomationId, propValue));
                    }
                }
            }
            return new AndCondition(cond);
        }
        /// <summary>
        /// 生成等值判断匹配条件，不支持通配
        /// </summary>
        /// <param name="isExistStarProp">是否存在通配符的情况</param>
        /// <returns>不带通配符的条件</returns>
        internal AndCondition GenerateConditionsByEqualProps(out bool isExistEnqValProp)
        {
            var automation = UiCommon.UIAAutomation;
            var cond = new List<ConditionBase>();
            var props = this.Properties.OfType<WinSelectorItemProperty>().ToList();
            isExistEnqValProp = props.Exists(p => p.Accurate && p.Enabled && !p.Pattern.EqualIgnoreCase(ConstFields.PATTERN_EQUAL));
            var commonProps = props.OfType<WinSelectorItemProperty>().Where(p => p.Accurate && p.Enabled && p.Pattern.EqualIgnoreCase(ConstFields.PATTERN_EQUAL));

            ControlType ct = ControlType.Unknown;
            if (Enum.TryParse<ControlType>(this.ElementName, out ct) && ct != ControlType.Custom)
            {
                cond.Add(new PropertyCondition(automation.PropertyLibrary.Element.ControlType, ct));
            }
            foreach (var p in commonProps)
            {
                string propName = p.Name;
                string propValue = p.Value;

                if (p.Pattern.EqualIgnoreCase(ConstFields.PATTERN_EQUAL))
                {
                    if (propName.EqualIgnoreCase(ConstFields.SELECTOR_NAME))
                    {
                        cond.Add(new PropertyCondition(automation.PropertyLibrary.Element.Name, propValue));
                    }
                    else if (propName.EqualIgnoreCase(ConstFields.SELECTOR_CLASS_NAME))
                    {
                        cond.Add(new PropertyCondition(automation.PropertyLibrary.Element.ClassName, propValue));
                    }
                    else if (propName.EqualIgnoreCase(ConstFields.SELECTOR_AUTOMATION_ID))
                    {
                        cond.Add(new PropertyCondition(automation.PropertyLibrary.Element.AutomationId, propValue));
                    }
                }
            }
            return new AndCondition(cond);
        }
        /// <summary>
        /// 查找模糊匹配的子节点
        /// </summary>
        /// <param name="elements">指定子节点</param>
        /// <returns>匹配结果</returns>
        private IEnumerable<AutomationElement> FindFuzzySubChildren(IEnumerable<AutomationElement> elements)
        {
            var manualCheck = this.Properties.OfType<WinSelectorItemProperty>().Count(x => x.Enabled && x.Accurate && x.Value != null && x.Value.Contains(ConstFields.COMPARE_SYMBOL_STAR));

            if (manualCheck > 0)
            {
                foreach (var item in elements)
                {
                    if (this.Match(item))
                    {
                        yield return item;
                    }
                }
            }
        }

        /// <summary>
        /// 针对从父窗体找不到子元素的情况进行特殊处理；并级窗体 主要针对子元素在父节点框内找不到，子元素是弹窗的问题
        /// </summary>
        /// <param name="root">父节点</param>
        /// <returns>能够找到的弹窗节点</returns>
        private AutomationElement[] FindOutFrameSubChildren(AutomationElement root)
        {
            //针对从父窗体找不到子元素的情况进行特殊处理；并级窗体


            logger.LogInformation("windows Immedite");

            var nameItem = this.FindAccordingName(ConstFields.SELECTOR_NAME).FirstOrDefault();

            List<IntPtr> matchWinHANDs = new List<IntPtr>();

            if (nameItem != null)
            {
                ///目前发现此UClient和javaw应用使用ScanEnumWindows遍历所有子窗口会出现无限循环遍历，导致一直退不出来，卡住的现象，先剔除此应用
                //var proInfo = root.GetProcessInfo();
                //if (proInfo.ProcessName == null || (!proInfo.ProcessName.Contains("javaw") && !proInfo.ProcessName.Contains("UClient")))
                //{
                List<Tuple<IntPtr, string>> windowsPair = this.ScanEnumWindows();

                foreach (var pair in windowsPair)
                {
                    if (nameItem.ConiditionMatch(pair.Item2))
                    {
                        matchWinHANDs.Add(pair.Item1);
                    }
                }
                //}
            }

            //不能用UI Automation通过父窗体找到子窗体，不是并级窗体
            if (matchWinHANDs.Count == 0 && root.Properties.NativeWindowHandle.IsSupported)
            {
                IntPtr intPtrCurrent = root.Properties.NativeWindowHandle.Value;

                List<Tuple<IntPtr, string>> windowsPair = this.ScanEnumChildWindows(intPtrCurrent);

                foreach (var pair in windowsPair)
                {
                    if (nameItem != null && nameItem.ConiditionMatch(pair.Item2))
                    {
                        matchWinHANDs.Add(pair.Item1);
                    }
                }
            }

            //var elements = matchWinHANDs.Distinct().Select(x => UiCommon.UIAAutomation.FromHandle(x)).ToArray();
            var elementList = new List<AutomationElement>();
            var elementList_matched = new List<AutomationElement>();
            foreach (var winHand in matchWinHANDs.Distinct())
            {
                try
                {
                    var pName = UiCommon.GetProcessNameByCache(winHand);
                    if (!WinConfigurations.Info.RpaProcess.Contains(pName, StringComparer.OrdinalIgnoreCase))
                    {
                        var wndEle = UiCommon.UIAAutomation.FromHandle(winHand);
                        if (this.Match(wndEle))
                        {
                            elementList_matched.Add(wndEle);
                        }
                        elementList.Add(wndEle);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogDebug($"处理到一个无效的窗口句柄，此异常会记录，但忽略。{ex}");
                }
            }
            elementList_matched.RemoveAll(item =>
            {
                if (!item.Properties.ProcessId.TryGetValue(out int pid))
                {
                    return false;
                }
                var pName = UiCommon.GetProcessNameById(pid);
                return WinConfigurations.Info.RpaProcess.Contains(pName, StringComparer.OrdinalIgnoreCase);
            });
            //通过matched更准确，如果没有一个通过match  使用未匹配过的所有
            return elementList_matched.Count > 0 ? elementList_matched.ToArray() : elementList.ToArray();
        }

        /// <summary>
        /// 根据父节点和指定条件，查找对应的子节点
        /// </summary>
        /// <param name="root">父节点</param>
        /// <param name="conditions">指定条件</param>
        /// <returns>找到的子节点</returns>
        private AutomationElement[] FindSubChildren(AutomationElement root, ConditionBase conditions, bool useHiddenWindow = false)
        {
            {
                var pros = this.FindAccordingName(ConstFields.SELECTOR_INDEX_IN_PARENT).Where(x => x.Accurate && !string.IsNullOrEmpty(x.Value)).ToArray();
                int indexInParent = -1;
                if (pros.Count() > 0)
                {
                    int.TryParse(pros.First().Value, out indexInParent);
                }
                // 坐标辅助模式：当boundingrect属性启用时，收集所有匹配候选，用坐标距离筛选
                bool useBoundingRect = this.FindAccordingName(ConstFields.SELECTOR_BOUNDING_RECT)
                    .Any(x => x.Accurate && !string.IsNullOrEmpty(x.Value));
                var matchList = new List<AutomationElement>();
                // BoundingRect无法确定唯一候选时，必须真正恢复到历史Index匹配结果。
                // 单独保存Index候选，避免仅记录“回退”日志却仍返回全部结构匹配元素。
                var indexMatchList = new List<AutomationElement>();
                if (CacheRequest.IsCachingActive)
                {
                    //var rootCache = root.FindFirst(TreeScope.Element, TrueCondition.Default);
                    var cacheChilden = root.CachedChildren;
                    int idx = -1;
                    for (int i = 0; i < cacheChilden.Length; i++)
                    {
                        idx++;
                        if (cacheChilden[i].Properties.ControlType.TryGetValue(out var controlType) && controlType == FlaUI.Core.Definitions.ControlType.ToolTip)
                        {
                            idx--;
                        }
                        else if (WinConfigurations.Info.FilterHiddenForIndex
                            && cacheChilden[i].Properties.IsOffscreen.TryGetValue(out var isOff) && isOff)
                        {
                            idx--;
                        }
                        if (useBoundingRect)
                        {
                            // BoundingRect模式: 收集所有匹配候选，后续用坐标距离筛选最佳匹配
                            bool isMatched = this.Match(cacheChilden[i]);
                            if (isMatched)
                            {
                                matchList.Add(cacheChilden[i]);
                                if (indexInParent != -1 && idx == indexInParent)
                                {
                                    indexMatchList.Add(cacheChilden[i]);
                                }
                            }
                        }
                        else
                        {
                            if (indexInParent != -1 && idx > indexInParent)
                            {
                                break;
                            }
                            if (indexInParent != -1 && idx == indexInParent)
                            {
                                if (this.Match(cacheChilden[i]))
                                {
                                    matchList.Add(cacheChilden[i]);
                                }
                            }
                            else if (indexInParent == -1 && this.Match(cacheChilden[i]))
                            {
                                matchList.Add(cacheChilden[i]);
                            }
                        }
                    }
                }
                else
                {
                    ITreeWalker treeWalker = null;
                    if ((int)(this.ElementType & SelectorPropsType.RawElement) > 0)
                    {
                        treeWalker = UiCommon.UIAAutomation.TreeWalkerFactory.GetRawViewWalker();
                    }
                    else
                    {
                        treeWalker = UiCommon.UIAAutomation.TreeWalkerFactory.GetControlViewWalker();
                    }
                    var chileEle = treeWalker.GetFirstChild(root);

                    List<AutomationElement> children = null;
                    int children_curIndex = 0;
                    if (chileEle == null)
                    {
                        children = root.FindAllChildrenByAcc();
                        if (children != null)
                        {
                            if (children.Count > 0)
                            {
                                chileEle = children[children_curIndex++];
                            }
                        }
                    }

                    int idx = -1;
                    while (chileEle != null)
                    {
                        idx++;
                        if (chileEle.Properties.ControlType.TryGetValue(out var controlType) && controlType == FlaUI.Core.Definitions.ControlType.ToolTip)
                        {
                            idx--;
                        }
                        else if (WinConfigurations.Info.FilterHiddenForIndex
                            && chileEle.Properties.IsOffscreen.TryGetValue(out var isOff) && isOff)
                        {
                            idx--;
                        }
                        if (useBoundingRect)
                        {
                            // BoundingRect模式: 收集所有匹配候选，后续用坐标距离筛选最佳匹配
                            bool isMatched = this.Match(chileEle);
                            if (isMatched)
                            {
                                matchList.Add(chileEle);
                                if (indexInParent != -1 && idx == indexInParent)
                                {
                                    indexMatchList.Add(chileEle);
                                }
                            }
                        }
                        else
                        {
                            if (indexInParent != -1 && idx > indexInParent)
                            {
                                break;
                            }
                            if (indexInParent == -1 || idx == indexInParent)
                            {
                                if (this.Match(chileEle))
                                {
                                    matchList.Add(chileEle);
                                }
                            }
                        }

                        if (children == null)
                        {
                            chileEle = treeWalker.GetNextSibling(chileEle);
                        }
                        else
                        {
                            if (children_curIndex < children.Count)
                            {
                                chileEle = children[children_curIndex++];
                            }
                            else
                            {
                                chileEle = null;
                            }
                        }
                    }

                    if (useHiddenWindow)
                    {
                        root.AddChildWindowToChildrenList(matchList, ele => this.Match(ele) && ele.Properties.ClassName.TryGetValue(out var claName) && claName.Contains("RenderWidgetHostHWND"));
                    }
                }

                // 坐标辅助匹配：当boundingrect启用且有多个候选时，筛选最佳匹配
                //
                // 风险场景：两个 Edit 间距仅 30px，目标元素重建后偏移 55px，邻居恰在
                //          40px 处。纯粹「选最近」会错误匹配到邻居。
                //
                // 改进策略 (clear-winner)：
                //   1. 计算所有候选到参考坐标的距离，记录 best + secondBest
                //   2. 「明显赢家」判定（满足任一即通过）：
                //      - 绝对接近：bestDist ≤ elementHeight（同元素重建的合理偏移不会超过自身高度）
                //      - 显著领先：bestDist < secondBest * 0.5（最近比次近近一倍以上，无歧义）
                //   3. 不满足 → 使用预先保存的Index候选，真正回退到历史Index匹配逻辑
                int structuralCandidateCount = matchList.Count;
                bool hasClearBoundingRectWinner = false;
                if (useBoundingRect && matchList.Count > 1)
                {
                    var bProp = this.FindAccordingName(ConstFields.SELECTOR_BOUNDING_RECT)
                        .FirstOrDefault(x => x.Accurate && !string.IsNullOrEmpty(x.Value));
                    if (bProp != null)
                    {
                        var parts = bProp.Value.Split(',');
                        if (parts.Length == 4
                            && double.TryParse(parts[0], out double refX)
                            && double.TryParse(parts[1], out double refY)
                            && double.TryParse(parts[2], out double refW)
                            && double.TryParse(parts[3], out double refH))
                        {
                            // 参考坐标是相对于窗口左上角的，匹配时也统一换算到窗口坐标系
                            double refCenterX = refX + refW / 2;
                            double refCenterY = refY + refH / 2;
                            double windowOffsetX = 0, windowOffsetY = 0;
                            try
                            {
                                // 从root向上查找顶层窗口（Desktop的直接子元素）
                                // 注意：Desktop.Parent==null，而Window.Parent==Desktop≠null。
                                // 原先的判断 parent==null 会导致 windowElement=Desktop，
                                // Desktop.BoundingRect 原点永远是(0,0)，导致使用屏幕绝对坐标
                                // 而非窗口相对坐标，与拾取阶段(pickup)的坐标系不一致。
                                // 修复：跟踪最近一个访问过的非Desktop元素(即Window)，而非Desktop本身。
                                var current = root;
                                AutomationElement windowElement = current;
                                while (current != null)
                                {
                                    AutomationElement parent = null;
                                    try { parent = current.Parent; } catch { }
                                    if (parent == null)
                                    {
                                        // current 是 Desktop（parent==null），
                                        // windowElement 是 Desktop 的直接子元素（即 Window）
                                        break;
                                    }
                                    windowElement = current;
                                    current = parent;
                                }
                                if (windowElement != null
                                    && windowElement.Properties.BoundingRectangle.TryGetValue(out var wRect)
                                    && !wRect.IsEmpty)
                                {
                                    windowOffsetX = wRect.X;
                                    windowOffsetY = wRect.Y;
                                }
                            }
                            catch { }
                            AutomationElement bestMatch = null;
                            double bestDist = double.MaxValue;
                            double secondBestDist = double.MaxValue;
                            foreach (var match in matchList)
                            {
                                try
                                {
                                    if (match.Properties.BoundingRectangle.TryGetValue(out var matchRect) && !matchRect.IsEmpty)
                                    {
                                        double matchCenterX = (matchRect.X - windowOffsetX) + matchRect.Width / 2.0;
                                        double matchCenterY = (matchRect.Y - windowOffsetY) + matchRect.Height / 2.0;
                                        double dist = Math.Sqrt(
                                            (matchCenterX - refCenterX) * (matchCenterX - refCenterX) +
                                            (matchCenterY - refCenterY) * (matchCenterY - refCenterY));
                                        if (dist < bestDist)
                                        {
                                            secondBestDist = bestDist;
                                            bestDist = dist;
                                            bestMatch = match;
                                        }
                                        else if (dist < secondBestDist)
                                        {
                                            secondBestDist = dist;
                                        }
                                    }
                                }
                                catch { }
                            }

                            // clear-winner 判定：
                            // (a) 绝对接近：偏移在元素自身高度以内（同元素重建不会超出此范围）
                            // (b) 显著领先：best 比 secondBest 近一倍以上（无歧义，不担心密邻干扰）
                            double absThreshold = Math.Max(refH, 30); // 至少30px，小于正常行间距
                            bool isCloseEnough    = bestDist <= absThreshold;
                            bool isClearlyWinner  = bestDist < secondBestDist * 0.5;
                            bool accepted         = bestMatch != null && (isCloseEnough || isClearlyWinner);

                            if (accepted)
                            {
                                hasClearBoundingRectWinner = true;
                                int candidateCount = matchList.Count;
                                matchList.Clear();
                                matchList.Add(bestMatch);
                                string reason = isCloseEnough
                                    ? $"距离{bestDist:F0}px ≤ 阈值{absThreshold:F0}px (元素高度{refH:F0}px)"
                                    : $"距离{bestDist:F0}px << 次近{secondBestDist:F0}px (领先{secondBestDist / bestDist:F1}x)";
                                logger.LogDebug($"坐标辅助匹配: 从{candidateCount}个候选 → {reason}");
                            }
                            else if (bestMatch != null)
                            {
                                logger.LogDebug($"坐标辅助匹配: 无明确赢家 (best={bestDist:F0}px, 2nd={secondBestDist:F0}px, 阈值={absThreshold:F0}px)，回退到Index匹配");
                            }
                        }
                    }
                }

                if (useBoundingRect
                    && structuralCandidateCount > 1
                    && !hasClearBoundingRectWinner
                    && indexInParent >= 0)
                {
                    matchList.Clear();
                    matchList.AddRange(indexMatchList);
                    logger.LogDebug($"坐标辅助匹配: 已回退到Index={indexInParent}，匹配候选数={indexMatchList.Count}");
                }

                return matchList.ToArray();
            }
        }


        /// <summary>
        /// 找到桌面下符合条件的子节点，即应用根节点
        /// </summary>
        /// <param name="root">父节点</param>
        /// <param name="conditions">指定条件</param>
        /// <returns>找到的子节点</returns>
        private AutomationElement[] FindSubChildren(AutomationElement root)
        {
            logger.LogDebug("start 查找根节点匹配属性");
            WinSelectorItemProperty classNameProp = null;
            WinSelectorItemProperty nameProp = null;
            WinSelectorItemProperty processNameProp = null;
            string processName = string.Empty;
            var props = this.Properties.OfType<WinSelectorItemProperty>().ToList();
            var commonProps = props.Where(x => x.Accurate && x.Enabled && !string.IsNullOrEmpty(x.Value));
            foreach (var prop in commonProps)
            {
                if (prop.Name.EqualIgnoreCase(ConstFields.SELECTOR_NAME))
                {
                    nameProp = prop;
                }
                else if (prop.Name.EqualIgnoreCase(ConstFields.SELECTOR_CLASS_NAME))
                {
                    classNameProp = prop;
                }
                else if (prop.Name.EqualIgnoreCase(ConstFields.SELECTOR_ROOT_PROCESSNAME))
                {
                    processNameProp = prop;
                    processName = processNameProp.Value;
                }
            }


            logger.LogDebug("end 查找根节点匹配属性");
            logger.LogDebug("start 查找所有可能匹配的窗口句柄");
            var candidateTimer = Stopwatch.StartNew();
            //获取桌面下 所有符合className ， name的窗口句柄，从而获取符合条件的窗口
            logger.LogDebug("读取根窗口句柄 begin");
            var parentHWnd = root.Properties.NativeWindowHandle.IsSupported ? root.Properties.NativeWindowHandle : NativeMethods.GetDesktopWindow();
            logger.LogDebug($"读取根窗口句柄 end: hwnd={parentHWnd}, elapsed={candidateTimer.ElapsedMilliseconds}ms");
            logger.LogDebug($"FindSubChildrenHandleList begin: parentHwnd={parentHWnd}");
            var childHwndList = FindSubChildrenHandleList(parentHWnd, classNameProp, nameProp);
            logger.LogDebug($"FindSubChildrenHandleList end: count={childHwndList.Count}, elapsed={candidateTimer.ElapsedMilliseconds}ms");
            List<AutomationElement> foundChildren = new List<AutomationElement>();
            string pName = string.Empty;
            foreach (var hwnd in childHwndList)
            {
                if (!NativeMethods.IsWindow(hwnd))
                {
                    logger.LogDebug($"FromHandle skip destroyed window: hwnd={hwnd}");
                    continue;
                }
                NativeMethods.GetWindowThreadProcessId(hwnd, out uint candidatePid);
                if (IsHungAppWindow(hwnd))
                {
                    logger.LogWarning($"FromHandle skip hung window: hwnd={hwnd}, pid={candidatePid}");
                    continue;
                }
                var handleTimer = Stopwatch.StartNew();
                logger.LogDebug($"FromHandle begin: hwnd={hwnd}, pid={candidatePid}");
                try
                {
                    var wndEle = UiCommon.UIAAutomation.FromHandle(hwnd);
                    foundChildren.Add(wndEle);
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, $"FromHandle 异常，会忽略调这个句柄: hwnd={hwnd}, pid={candidatePid}, HRESULT=0x{ex.HResult:X8}");
                }
                finally
                {
                    logger.LogDebug($"FromHandle end: hwnd={hwnd}, pid={candidatePid}, elapsed={handleTimer.ElapsedMilliseconds}ms");
                }
            }
            logger.LogDebug("end 查找所有可能匹配的窗口句柄");
            logger.LogDebug("start run Match function get elements");
            //二次过滤 主要是除className  name之外的属性
            var matchList = new List<AutomationElement>();
            foreach (var item in foundChildren)
            {
                if (this.Match(item))
                {
                    if (item.Properties.ProcessId.TryGetValue(out int pid))
                    {
                        pName = UiCommon.GetProcessNameById(pid);
                        if (WinConfigurations.Info.RpaProcess.Contains(pName, StringComparer.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                    }
                    matchList.Add(item);
                }
            }


            //foreach Match之后，如果没有一个满足的条件的，将使用未match窗口做为容错匹配窗口
            if (matchList.Count == 0 && foundChildren.Count > 0)
            {
                foundChildren.RemoveAll(item =>
                {
                    if (!item.Properties.ProcessId.TryGetValue(out int pid))
                    {
                        return false;
                    }
                    pName = UiCommon.GetProcessNameById(pid);
                    return WinConfigurations.Info.RpaProcess.Contains(pName, StringComparer.OrdinalIgnoreCase);
                });
                matchList.AddRange(foundChildren);
            }
            logger.LogDebug("end run Match function get elements");
            return matchList.ToArray();
        }
        private List<int> noRunProcessIDList = null;
        /// <summary>
        /// 获取子元素句柄列表
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="classNameProp"></param>
        /// <param name="nameProp"></param>
        /// <returns></returns>
        public List<IntPtr> FindSubChildrenHandleList(IntPtr hWnd, WinSelectorItemProperty classNameProp, WinSelectorItemProperty nameProp)
        {
            List<IntPtr> list = new List<IntPtr>();
            //获取根窗口信息
            IntPtr hDesktopWnd = NativeMethods.GetWindow(hWnd, 6u);
            if (hDesktopWnd != IntPtr.Zero)
            {
                bool isMatched = this.IsMatchByProperty(hDesktopWnd, classNameProp, nameProp);
                if (isMatched)
                {
                    list.Add(hDesktopWnd);
                }
            }
            //初始化className name值，只区分等值判断、非等值判断（正则、起始、结尾、包含）
            bool classNameProp_equal = false;
            bool nameProp_equal = false;
            string className = null, name = null;
            if ((classNameProp != null && classNameProp.Pattern.Equals(ConstFields.PATTERN_EQUAL, StringComparison.OrdinalIgnoreCase)))
            {
                classNameProp_equal = true;
                className = string.IsNullOrEmpty(classNameProp?.Value) ? null : classNameProp.Value;
            }
            if ((nameProp != null && nameProp.Pattern.Equals(ConstFields.PATTERN_EQUAL, StringComparison.OrdinalIgnoreCase)))
            {
                nameProp_equal = true;
                name = string.IsNullOrEmpty(nameProp.Value) ? null : nameProp.Value;
            }
            List<IntPtr> msatch_hWnds = new List<IntPtr>();
            //className name都存在
            if (classNameProp_equal && nameProp_equal)
            {
                msatch_hWnds = MatchWindowWithClassnameAngTitle(hWnd, classNameProp, nameProp, className, name);
            }
            //未匹配到数据，且className等值匹配，单独使用className
            if (msatch_hWnds?.Count() == 0 && classNameProp_equal)
            {
                msatch_hWnds = MatchWindowWithClassnameAngTitle(hWnd, classNameProp, null, className, null);
            }
            //未匹配到数据,className为空，name不为空
            if (msatch_hWnds?.Count() == 0 && !classNameProp_equal && nameProp_equal)
            {
                msatch_hWnds = MatchWindowWithClassnameAngTitle(hWnd, null, nameProp, null, name);
            }
            if (msatch_hWnds?.Count() > 0)
            {
                list.AddRange(msatch_hWnds);
            }
            else
            {
                IntPtr nextHWnd = NativeMethods.GetWindow(hWnd, 5u);
                var visitedHandles = new HashSet<IntPtr>();
                while (nextHWnd != IntPtr.Zero)
                {
                    if (!visitedHandles.Add(nextHWnd))
                    {
                        logger.LogWarning($"GetWindow traversal repeated handle: parent={hWnd}, hwnd={nextHWnd}, count={visitedHandles.Count}");
                        throw new InvalidOperationException("Window traversal repeated a handle; candidate enumeration is incomplete.");
                    }
                    bool isMatched = this.IsMatchByProperty(nextHWnd, classNameProp, nameProp);
                    if (isMatched)
                    {
                        list.Add(nextHWnd);
                    }
                    nextHWnd = NativeMethods.GetWindow(nextHWnd, 2u);
                }
            }
            return list.Where(h => NativeMethods.IsWindow(h) && NativeMethods.IsWindowVisible(h)).ToList();
        }

        private List<IntPtr> MatchWindowWithClassnameAngTitle(IntPtr hWnd, WinSelectorItemProperty classNameProp, WinSelectorItemProperty nameProp, string className, string wndTitle)
        {
            List<IntPtr> list = new List<IntPtr>();
            className = className ?? "";
            var clsList = className.Split('|');
            for (int i = 0; i < clsList.Length; i++)
            {
                IntPtr hwndChildAfter = IntPtr.Zero;
                var visitedHandles = new HashSet<IntPtr>();
                var cls = string.IsNullOrEmpty(clsList[i]) ? null : clsList[i];
                while (true)
                {
                    IntPtr hNextWnd = NativeMethods.FindWindowEx(hWnd, hwndChildAfter, cls, wndTitle);
                    #region  查询桌面图标的逻辑，先注释参考，可删除
                    //if (hNextWnd == IntPtr.Zero && cls == "SHELLDLL_DefView")
                    //{
                    //    WindowsByClassFinder.EnumWindows((hwnd, lParam) =>
                    //    {
                    //        var h1 = NativeMethods.FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                    //        if (h1 != IntPtr.Zero)
                    //        {
                    //            var h2 = NativeMethods.FindWindowEx(h1, IntPtr.Zero, "SysListView32", null);
                    //            if (h2 != IntPtr.Zero)
                    //            {
                    //                list.Clear();
                    //                list.Add(h2);
                    //            }
                    //            return false;
                    //        }
                    //        return true;
                    //    }, IntPtr.Zero);
                    //}
                    #endregion
                    if (hNextWnd == IntPtr.Zero)
                    {
                        break;
                    }
                    if (!visitedHandles.Add(hNextWnd))
                    {
                        logger.LogWarning($"FindWindowEx traversal repeated handle: parent={hWnd}, hwnd={hNextWnd}, count={visitedHandles.Count}");
                        throw new InvalidOperationException("Window traversal repeated a handle; candidate enumeration is incomplete.");
                    }
                    bool isMatched = this.IsMatchByProperty(hNextWnd, classNameProp, nameProp);
                    if (isMatched)
                    {
                        list.Add(hNextWnd);
                    }
                    hwndChildAfter = hNextWnd;
                }
            }
            return list;
        }
        /// <summary>
        /// 是否符合属性的匹配要求，优先className,如果className匹配，name不匹配，也通过，后边还有二次过滤
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="classNameProp"></param>
        /// <param name="nameProp"></param>
        /// <returns></returns>
        private bool IsMatchByProperty(IntPtr hWnd, WinSelectorItemProperty classNameProp, WinSelectorItemProperty nameProp)
        {
            try
            {
                //todo:过滤掉后台应用
                StringBuilder classNameSB = new StringBuilder(256);
                NativeMethods.GetClassName(hWnd, classNameSB, 256);
                if (classNameProp == null || string.IsNullOrEmpty(classNameProp.Value) || classNameProp.ConiditionMatch(classNameSB.ToString()))
                {
                    //nameProp 空 或者 不是等值属性，直接忽略后边 使用UIAELEMENT 对Name过滤
                    if (nameProp == null || string.IsNullOrEmpty(nameProp.Value))
                    {
                        return true;
                    }
                    var wndText = UiCommon.GetWindowText(hWnd);
                    if (wndText == null || string.IsNullOrEmpty(wndText))
                    {
                        //读取title为空，但className通过，先通过，后边使用UIAELEMENT 对Name过滤
                        return (classNameProp != null);
                    }
                    if (nameProp.ConiditionMatch(wndText))
                    {
                        if (classNameSB.ToString() == ConstFields.PROP_CLASS_NAME_Factory && ConstFields.PROP_NAME_Factory.Contains($",{wndText},"))
                        {
                            return false;
                        }
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"IsMatchByProperty 出现一个异常，忽略继续");
            }
            //logger.LogDebug("end IsMatchByProperty");
            return false;
        }
        /// <summary>
        /// 根据父级，索引属性，以及目标列表，匹配查找元素
        /// </summary>
        /// <param name="root">父级</param>
        /// <param name="indexProp">索引属性</param>
        /// <param name="elements">目标列表</param>
        /// <returns>匹配查找元素</returns>
        private IEnumerable<AutomationElement> FindSubChildrenAccordingIdx(AutomationElement root, WinSelectorItemProperty indexProp, AutomationElement[] elements)
        {
            int indexInParent = -1;
            ;
            if (int.TryParse(indexProp.Value, out indexInParent) && indexInParent > -1)
            {
                if (indexInParent == 0)
                {
                    yield return elements[indexInParent];
                }
                else
                {
                    ITreeWalker treeWalker = null;
                    if ((int)(this.ElementType & SelectorPropsType.RawElement) > 0)
                    {
                        treeWalker = UiCommon.UIAAutomation.TreeWalkerFactory.GetRawViewWalker();
                    }
                    else
                    {
                        treeWalker = UiCommon.UIAAutomation.TreeWalkerFactory.GetControlViewWalker();
                    }
                    var chileEle = treeWalker.GetFirstChild(root);
                    int count = -1;
                    while (chileEle != null)
                    {
                        count++;
                        if (chileEle.Properties.ControlType.IsSupported && chileEle.ControlType == FlaUI.Core.Definitions.ControlType.ToolTip)
                        {
                            count--;
                        }
                        else if (WinConfigurations.Info.FilterHiddenForIndex
                            && chileEle.Properties.IsOffscreen.TryGetValue(out var isOff) && isOff)
                        {
                            count--;
                        }
                        if (indexInParent == count && Match(chileEle))
                        {
                            yield return chileEle;
                        }
                        if (count > indexInParent)
                        {
                            break;
                        }
                        chileEle = treeWalker.GetNextSibling(chileEle);
                    }

                    //var childs = root.FindAllChildren();

                    ////indexInParent -= childs.Count(e => e.Properties.ControlType.IsSupported && e.ControlType == FlaUI.Core.Definitions.ControlType.ToolTip);
                    //if (childs.Any(e => e.Properties.ControlType.IsSupported && e.ControlType == FlaUI.Core.Definitions.ControlType.ToolTip))
                    //{
                    //    childs = childs.Where(e => e.Properties.ControlType.IsSupported && e.ControlType != FlaUI.Core.Definitions.ControlType.ToolTip).ToArray();
                    //}

                    //if (childs.Count() > indexInParent)
                    //{
                    //    yield return childs[indexInParent];
                    //}
                }
            }
        }

        /// <summary> 根据父级，索引属性，以及目标列表，匹配查找元素 </summary> <param name="root">父级</param> <param
        /// name="indexProp">索引属性</param> <param name="elements">目标列表</param> <returns>匹配查找元素</returns
        [Obsolete("老逻辑即将移除，主要指的是使用Index标签")]
        private IEnumerable<AutomationElement> FindSubChildrenAccordingIdx2(AutomationElement root)
        {
            var pros_index = this.FindAccordingName(ConstFields.SELECTOR_INDEX).Where(x => !string.IsNullOrEmpty(x.Value)).ToArray();

            if (pros_index.Count() > 0)
            {
                if (int.TryParse(pros_index.First().Value, out int index) && index >= 0)
                {
                    var cond = new List<ConditionBase>();

                    ControlType ct = (ControlType)Enum.Parse(typeof(ControlType), this.ElementName);
                    cond.Add(new PropertyCondition(UiCommon.UIAAutomation.PropertyLibrary.Element.ControlType, ct));

                    foreach (var p in this.Properties.Where(x => x.Enabled && (x.Value != null && !x.Value.Contains(ConstFields.COMPARE_SYMBOL_STAR))))
                    {
                        if (p.Name.Equals(ConstFields.SELECTOR_CLASS_NAME, StringComparison.OrdinalIgnoreCase))
                        {
                            cond.Add(new PropertyCondition(UiCommon.UIAAutomation.PropertyLibrary.Element.ClassName, p.Value));
                        }
                    }
                    AndCondition andCondition = new AndCondition(cond);
                    var childs = root.FindAllChildren(andCondition);
                    if (childs.Count() > index)
                    {
                        yield return childs[index];
                    }
                }
            }
        }

        /// <summary>
        /// 根据父级和指定查找条件查找微信元素
        /// </summary>
        /// <param name="root">父级</param>
        /// <param name="conditions">指定条件</param>
        /// <returns>微信元素</returns>
        private AutomationElement FindWeChatElement(AutomationElement root, ConditionBase conditions)
        {
            var elements = root.FindAll(TreeScope.Children, conditions);

            if (elements.Length > 0)
            {
                var ele = elements.Where(e => e.Properties.ProcessId == WeChatDataContext.CurrentProcessId);

                if (ele.Count() > 0)
                {
                    return ele.First();
                }
            }

            return null;
        }

        /// <summary>
        /// 生成顶层元素属性单元
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        internal static List<WinSelectorItemProperty> GenerateRootElementProps(AutomationElement element)
        {
            //if (CacheData.RootWndNodeProcessPropsCatch == null)
            //{
            //    CacheData.RootWndNodeProcessPropsCatch = new Dictionary<IntPtr, List<WinSelectorItemProperty>>();
            //}
            IntPtr handle = IntPtr.Zero;
            if (element.Properties.NativeWindowHandle.TryGetValue(out handle))
            {
                if (CacheData.RootWndNodeProcessPropsCatch.ContainsKey(handle))
                {
                    return CacheData.RootWndNodeProcessPropsCatch[handle];
                }
            }
            var sysRootProperties = GenerateRootElementPropsEnumerable(element);
            if (handle != IntPtr.Zero)
            {
                CacheData.RootWndNodeProcessPropsCatch[handle] = sysRootProperties;
            }
            return sysRootProperties;
        }
        private static List<WinSelectorItemProperty> GenerateRootElementPropsEnumerable(AutomationElement element)
        {
            List<WinSelectorItemProperty> list = new List<WinSelectorItemProperty>();
            if (element.Properties.ProcessId.IsSupported)   //如果是顶层进程元素
            {
                var info = element.GetProcessInfo();
                if (info.IsImmersiveProcess)
                {
                    list.Add(new WinSelectorItemProperty(nameof(info.IsImmersiveProcess).ToLower(), info.IsImmersiveProcess.ToString()) { Accurate = false, Enabled = true });
                    list.Add(new WinSelectorItemProperty(nameof(info.ApplicationUserModelId).ToLower(), info.ApplicationUserModelId) { Accurate = false, Enabled = true });
                }
                else
                {
                    if (!string.IsNullOrEmpty(info.Filename))
                    {
                        //if (info.Filename.ToLower().Contains("system32\\conhost.exe"))
                        //{
                        //    info.Filename = "%windir%\\system32\\cmd.exe";
                        //}
                        //yield return new WinSelectorItemProperty(ConstFields.SELECTOR_ROOT_FILE, info.Filename) { Accurate = true };
                    }
                    list.Add(new WinSelectorItemProperty(ConstFields.SELECTOR_ROOT_PROCESSNAME, info.ProcessName == null ? "" : info.ProcessName) { Accurate = true, Enabled = true });
                }
            }
            return list;
        }

        /// <summary>
        /// 根据指定的句柄获取子窗口
        /// </summary>
        /// <param name="pCurrent">指定的句柄</param>
        /// <returns>句柄，标题</returns>
        private List<Tuple<IntPtr, string>> ScanEnumChildWindows(IntPtr pCurrent)
        {
            List<Tuple<IntPtr, string>> ret = new List<Tuple<IntPtr, string>>();
            Func<IntPtr, IntPtr, bool> callBack = null;
            int count = 0;
            callBack = (testWindowHandle, includeChildren) =>
            {
                if (++count > 2500)
                {
                    logger.LogDebug($"根据指定的句柄获取子窗口 数量超过了阈值，存在阻塞风险，终止此行为");
                    return false;
                }
                string title = UiCommon.GetWindowText(testWindowHandle);
                ret.Add(new Tuple<IntPtr, string>(testWindowHandle, title));
                if (includeChildren.Equals(IntPtr.Zero) == false)
                {
                    UiCommon.EnumChildWindows(testWindowHandle, new UiCommon.EnumWindowsProc(callBack), IntPtr.Zero);
                }
                return true;
            };

            UiCommon.EnumChildWindows(pCurrent, new UiCommon.EnumWindowsProc(callBack), IntPtr.Zero);
            return ret;
        }

        /// <summary>
        /// 根据所有子窗口
        /// </summary>
        /// <returns>句柄，标题</returns>
        private List<Tuple<IntPtr, string>> ScanEnumWindows()
        {
            List<Tuple<IntPtr, string>> ret = new List<Tuple<IntPtr, string>>();
            Func<IntPtr, IntPtr, bool> callBack = null;
            int count = 0;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            const int timeoutMs = 1000;   // 可按实际调整：300~1000
            const int maxCount = 1000;    // 建议比 2500 小一些

            callBack = (testWindowHandle, includeChildren) =>
            {
                // 先做快速退出判断：超时或数量过大
                if (sw.ElapsedMilliseconds > timeoutMs || ++count > maxCount)
                {
                    logger.LogDebug($"枚举窗口终止：count={count}, elapsed={sw.ElapsedMilliseconds}ms");
                    return false;
                }

                var isVisible = NativeMethods.IsWindowVisible(testWindowHandle);
                if (isVisible)
                {
                    string title = UiCommon.GetWindowText(testWindowHandle);
                    ret.Add(new Tuple<IntPtr, string>(testWindowHandle, title));
                }

                if (!includeChildren.Equals(IntPtr.Zero))
                {
                    UiCommon.EnumChildWindows(testWindowHandle, new UiCommon.EnumWindowsProc(callBack), IntPtr.Zero);
                }

                return true;
            };

            UiCommon.EnumWindows(new UiCommon.EnumWindowsProc(callBack), IntPtr.Zero);
            logger.LogDebug($"枚举窗口结束：count={count}, elapsed={sw.ElapsedMilliseconds}ms");
            return ret;
        }
        internal List<WindowObject> MatchWindows(WindowObject parentWndObject)
        {
            bool ignoreControlType = ConstFields.SELCETOR_IGNORE_CONTROLTYPE.ContainsIgnoreCase(this.ElementName);
            var props = this.Properties.OfType<WinSelectorItemProperty>().Where(x => x.Accurate && x.Enabled && x.Value != null).ToDictionary(x => x.Name.ToLower(), x => x);
            props.TryGetValue(ConstFields.SELECTOR_TITLE, out var titleItem);
            props.TryGetValue(ConstFields.SELECTOR_CLASS_NAME, out var classNameItem);
            props.TryGetValue(ConstFields.SELECTOR_NAME, out var nameItem);
            props.TryGetValue(ConstFields.SELECTOR_INDEX_IN_PARENT, out var indexItem);
            props.TryGetValue(ConstFields.PROP_NAME_PROCESS_NAME, out var processItem);
            props.TryGetValue(ConstFields.SELECTOR_AUTOMATION_ID, out var automationIdItem);
            if (this.ElementType.HasFlag(SelectorPropsType.DetachedChildWindow))
            {
                string calssName = classNameItem?.Pattern == ConstFields.PATTERN_EQUAL ? classNameItem.Value : null;
                return new DesktopWindow().FindAllChildWindow(calssName, null, wnd =>
                {
                    var isMatch = wnd.IsVisible && (parentWndObject.IsDesktop || wnd.ParentHandle == parentWndObject.Handle)
                            && (titleItem == null || titleItem.ConiditionMatch(wnd.Title))
                            && (classNameItem == null || classNameItem.ConiditionMatch(wnd.ClassName))
                            && (processItem == null || processItem.ConiditionMatch(wnd.ProcessName));
                    if (!isMatch) return false;
                    Core.AccEnabler.EnableOnce(wnd.AutomationElement);
                    return (ignoreControlType || wnd.AutomationElement.Properties.ControlType.Match(this.ElementName, out _))
                    && (nameItem == null || wnd.AutomationElement.Properties.Name.ConiditionMatch(nameItem, out _))
                    && (automationIdItem == null || wnd.AutomationElement.Properties.AutomationId.ConiditionMatch(automationIdItem, out _));
                }).ToList();
            }
            int indexInParent = -1;
            //return windowObject.ChildWindows.Where(wnd =>
            bool isDesktopWindow = parentWndObject.Handle == NativeMethods.GetDesktopWindow();
            return parentWndObject.FindAllChildWindowEx(wnd =>
            {
                indexInParent++;
                var isMatch = (!isDesktopWindow || wnd.IsVisible)
                        && (indexItem == null || indexItem.ConiditionMatch(indexInParent.ToString()))
                        && (titleItem == null || titleItem.ConiditionMatch(wnd.Title))
                        && (classNameItem == null || classNameItem.ConiditionMatch(wnd.ClassName))
                        && (processItem == null || processItem.ConiditionMatch(wnd.ProcessName));
                if (!isMatch) return false;
                Core.AccEnabler.EnableOnce(wnd.AutomationElement);
                return (ignoreControlType || wnd.AutomationElement.Properties.ControlType.Match(this.ElementName, out _))
                && (nameItem == null || wnd.AutomationElement.Properties.Name.ConiditionMatch(nameItem, out _))
                && (automationIdItem == null || wnd.AutomationElement.Properties.AutomationId.ConiditionMatch(automationIdItem, out _));
            }).ToList();
        }
        /// <summary>
        /// 通过Selector获得的Item 和桌面元素进行匹配
        /// </summary>
        /// <param name="item">通过Selector获得的Item</param>
        /// <param name="element">桌面元素自动化节点</param>
        /// <returns>是否匹配</returns>
        private bool Match(SelectorItem item, AutomationElement element)
        {
            //TODO
            Stopwatch Stopwatch1 = Stopwatch.StartNew();
            logger.LogDebug($"Match time0:{Stopwatch1.ElapsedMilliseconds}");
            var props = item.Properties.OfType<WinSelectorItemProperty>().Where(x => x.Accurate && x.Enabled && x.Value != null).ToDictionary(x => x.Name.ToLower(), x => x);
            #region ControlType Match
            logger.LogDebug($"Match time1:{Stopwatch1.ElapsedMilliseconds}");
            WinSelectorItemProperty foundItem = null;
            var winSelectorItem = (item as WindowsSelectorItem);

            //部分情况ElementName可能为空，为空是允许的
            string itemControlTypeName = string.IsNullOrEmpty(winSelectorItem.ElementName) ? ConstFields.SELECTOR_Custom : winSelectorItem.ElementName;
            string eleControlTypeName = ConstFields.SELECTOR_Custom;
            if (element.Properties.ControlType.TryGetValue(out var _controlType))
            {
                eleControlTypeName = _controlType.ToString();
            }
            else
            {
                logger.LogInformation("Element ControlType is not supported");
            }
            if (!winSelectorItem.SkipControlTypeMatch && !itemControlTypeName.EqualIgnoreCase(eleControlTypeName))
            {
                logger.LogInformation($"{itemControlTypeName} is not equal to {eleControlTypeName}! (SkipControlTypeMatch={winSelectorItem.SkipControlTypeMatch})");
                logger.LogDebug($"Match time3:{Stopwatch1.ElapsedMilliseconds}");
                return false;
            }
            else if (winSelectorItem.SkipControlTypeMatch)
            {
                logger.LogInformation($"ControlType check skipped: expected {itemControlTypeName}, actual {eleControlTypeName} (SkipControlTypeMatch=true)");
            }
            else
            {
                logger.LogInformation($"{itemControlTypeName} is equal to {eleControlTypeName}!");
            }
            #endregion

            if (props.TryGetValue(ConstFields.SELECTOR_NAME, out foundItem))
            {
                if (element.Properties.Name.TryGetValue(out var name))
                {
                    if (!foundItem.ConiditionMatch(name))
                    {

                        logger.LogInformation(foundItem.Name + " mismatch '" + name + "' / '" + foundItem.Value + "'");
                        logger.LogInformation($"element Name:{name}");
                        return false;
                    }
                    else
                    {
                        logger.LogInformation($"meet the name condition, element Name:{name}");
                    }
                }
                else
                {
                    logger.LogInformation(foundItem.Name + " does not exists, but needed value '" + foundItem.Value + "'");
                    return false;
                }
            }

            if (props.TryGetValue(ConstFields.SELECTOR_CLASS_NAME, out foundItem))
            {
                if (element.Properties.ClassName.TryGetValue(out var className))
                {
                    object v = string.Empty;
                    if (!foundItem.ConiditionMatch(className))
                    {
                        logger.LogInformation(foundItem.Name + " mismatch '" + v.ToString() + "' / '" + foundItem.Value + "'");
                        logger.LogInformation($"element ClassName:{className}");

                        return false;
                    }
                    else
                    {
                        logger.LogInformation($"meet the ClassName condition, element ClassName:{className}");
                    }
                }
                else
                {
                    logger.LogInformation(foundItem.Name + " does not exists, but needed value '" + foundItem.Value + "'");
                    return false;
                }
            }
            if (props.TryGetValue(ConstFields.SELECTOR_AUTOMATION_ID, out foundItem))
            {
                if (element.Properties.AutomationId.TryGetValue(out var automationId))
                {
                    object v = string.Empty;
                    if (!foundItem.ConiditionMatch(automationId))
                    {
                        logger.LogInformation(foundItem.Name + " mismatch '" + v.ToString() + "' / '" + foundItem.Value + "'");
                        logger.LogInformation($"element automationId:{automationId}");
                        return false;
                    }
                    else
                    {
                        logger.LogInformation($"meet the automationId condition, element automationId:{automationId}");
                    }
                }
                else
                {
                    logger.LogInformation(foundItem.Name + " does not exists, but needed value '" + foundItem.Value + "'");
                    return false;
                }
            }
            logger.LogDebug($"Match time4:{Stopwatch1.ElapsedMilliseconds}");

            if (props.Count > 0)
            {
                bool has_try_get_web_acc_ex = false;
                IAccessible2_2 acc2_2 = null;
                ISimpleDOMNode node = null;

                foreach (var kv in props)
                {
                    var test_pn = kv.Key;
                    string n;
                    if (IsWebPN(test_pn, out n))
                    {
                        //进入了这里的话，说明勾选了WEB属性，我们现在也进行验证
                        bool foundProperty = false;
                        bool foundPropertyIsMatched = false;

                        if (!has_try_get_web_acc_ex)
                        {
                            has_try_get_web_acc_ex = true;

                            if (element.Patterns.LegacyIAccessible.IsSupported)
                            {
                                var acc = element.Patterns.LegacyIAccessible.Pattern.GetIAccessible();
                                if (acc == null)
                                {
                                    acc = UIANode.ConvertToAccByItemContainer(null, element);
                                }
                                if (acc != null)
                                {
                                    IServiceProvider sp = (IServiceProvider)acc;
                                    if (sp != null)
                                    {
                                        {
                                            object obj;
                                            sp.QueryService(typeof(IAccessible2).GUID, typeof(IAccessible2_2).GUID, out obj);
                                            acc2_2 = obj as IAccessible2_2;
                                        }
                                        {
                                            object obj;
                                            sp.QueryService(typeof(ISimpleDOMNode).GUID, typeof(ISimpleDOMNode).GUID, out obj);
                                            node = obj as ISimpleDOMNode;
                                        }
                                    }
                                }
                            }
                        }

                        var itemByEnum = kv.Value;

                        if (acc2_2 != null)
                        {
                            string v = acc2_2.get_attribute(n);
                            if (v != null)
                            {
                                foundProperty = true;
                                if (itemByEnum.ConiditionMatch(v))
                                {
                                    foundPropertyIsMatched = true;
                                }
                            }
                        }
                        if (!foundProperty)
                        {
                            if (node != null)
                            {
                                const ushort numAttribs = 1;
                                string[] names = new string[numAttribs] { n };
                                string[] values = new string[numAttribs];
                                short[] nameSpaceIDs = new short[numAttribs];
                                node.get_attributesForNames(numAttribs, names, nameSpaceIDs, values);
                                var v = values[0];
                                if (v != null)
                                {
                                    foundProperty = true;
                                    if (itemByEnum.ConiditionMatch(v))
                                    {
                                        foundPropertyIsMatched = true;
                                    }
                                }
                            }
                        }

                        if (!foundProperty)
                        {
                            return false;
                        }
                        if (!foundPropertyIsMatched)
                        {
                            return false;
                        }
                    }
                }
            }
            logger.LogDebug($"Match time5:{Stopwatch1.ElapsedMilliseconds}");
            return true;
        }

        /// <summary>
        /// 通过Selector获得的Item 和桌面元素进行匹配
        /// </summary>
        /// <param name="element">桌面元素自动化节点</param>
        /// <returns>是否匹配</returns>
        public bool Match(AutomationElement element)
        {
            return Match(this, element);
        }

    }
}
