using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.WindowsAPI;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.Windows.AutomationElements;
using II.RPA.Plugin.Windows.CompareRule;
using Interop.UIAutomationClient;
using Microsoft.Extensions.Logging;
using static InteropMy.UiaCore;
using FlaUI.Core.WindowsAPI;

namespace II.RPA.Plugin.Windows
{
    internal static class FlaUIExtensions
    {
        private static ILogger logger = Logger.OfContext();
        private const int CHILDID_SELF = 0;
        public static List<AutomationElement> FindAllChildrenByAcc(this AutomationElement parent)
        {
            //目前最新版本的Windows 10上调用ElementFromIAccessible容易导致目标程序崩溃，因此暂时限制仅在Flash渲染窗口中执行后续的遍历逻辑,
            //之前“双引擎”代码中企业微信崩溃也是其中的ElementFromIAccessible调用导致的，以后“新引擎”出来了就没这个问题了
            var windowHandle = parent.Properties.NativeWindowHandle.ValueOrDefault;
            if (windowHandle == IntPtr.Zero)
            {
                return null;
            }
            StringBuilder classNameSB = new StringBuilder(256);
            NativeMethods.GetClassName(windowHandle, classNameSB, 256);
            var clsName = classNameSB.ToString();
            if (!string.Equals(clsName, "ApolloRuntimeContentWindow") && !string.Equals(clsName, "ShockwaveFlash"))
            {
                return null;
            }

            List<AutomationElement> childrenList = null;
            if (parent.Patterns.LegacyIAccessible.IsSupported)
            {
                IAccessibleInternal acc = (IAccessibleInternal)parent.Patterns.LegacyIAccessible.Pattern.GetIAccessible();
                if (acc != null)
                {
                    int cChildren;
                    acc.get_accChildCount(out cChildren);
                    if (cChildren > 0)
                    {
                        childrenList = new List<AutomationElement>();
                        for (int i = 1; i < 10000; i++)
                        {
                            //System.Diagnostics.Debug.WriteLine($"childID={i} cChildren={cChildren}");
                            object child;
                            acc.get_accChild(i, out child);
                            if (child != null)
                            {
                                if (child is IAccessible childAcc)
                                {
                                    try
                                    {
                                        var role = childAcc.accRole[CHILDID_SELF];
                                        if (Convert.ToInt32(role) != (int)AccessibilityRole.ROLE_SYSTEM_CLIENT)
                                        {
                                            FlaUI.UIA3.UIA3Automation uia3 = (FlaUI.UIA3.UIA3Automation)parent.Automation;
                                            if (uia3 != null)
                                            {
                                                var result = uia3.NativeAutomation.ElementFromIAccessible(childAcc, CHILDID_SELF);
                                                if (result != null)
                                                {
                                                    var ele = uia3.WrapNativeElement(result);
                                                    if (ele != null)
                                                    {
                                                        childrenList.Add(ele);
                                                        if (childrenList.Count == cChildren)
                                                        {
                                                            break;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        logger.LogWarning($"FindAllChildrenByAcc函数执行异常:{e}");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return childrenList;
        }
        public static List<AutomationElement> AddChildWindowToChildrenList(this AutomationElement parent, List<AutomationElement> childrenList, Func<AutomationElement, bool> matchFunc = null)
        {
            var windowHandle = parent.GetWindowHandle();
            if (windowHandle != IntPtr.Zero)
            {
                HashSet<IntPtr> hwndChildren = new HashSet<IntPtr>();
                var hWndChild = NativeMethods.GetWindow(windowHandle, (uint)GET_WINDOW_CMD.GW_CHILD);
                if (hWndChild != IntPtr.Zero)
                {
                    foreach (var child in childrenList)
                    {
                        var childWindowHandle = child.GetWindowHandle();
                        if (childWindowHandle != IntPtr.Zero)
                        {
                            hwndChildren.Add(childWindowHandle);
                        }
                    }
                }
                while (hWndChild != IntPtr.Zero)
                {
                    bool Found = hwndChildren.Contains(hWndChild);
                    if (!Found && !NativeMethods.IsWindowVisible(hWndChild))
                    {
                        var ele = UiCommon.UIAAutomation.FromHandle(hWndChild);
                        if (matchFunc == null || matchFunc(ele))
                        {
                            childrenList.Add(ele);
                        }
                    }
                    hWndChild = NativeMethods.GetWindow(hWndChild, (uint)GET_WINDOW_CMD.GW_HWNDNEXT);
                }
            }
            return childrenList;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ele"></param>
        /// <param name="frameworkId">Win32</param>
        /// <returns></returns>
        public static IntPtr GetWindowHandle(this AutomationElement ele, string frameworkId = "")
        {
            if (ele.Properties.NativeWindowHandle.IsSupported)
            {
                var windowHandle = ele.Properties.NativeWindowHandle.ValueOrDefault;
                if (windowHandle != IntPtr.Zero)
                {
                    if (string.IsNullOrEmpty(frameworkId)
                        || (ele.Properties.FrameworkId.IsSupported && ele.Properties.FrameworkId == frameworkId))
                    {
                        return windowHandle;
                    }
                }
            }
            return IntPtr.Zero;
        }
        #region Children By RawView

        public static List<AutomationElement> GetChildrenByRawView(this AutomationElement parent, bool cache)
        {
            if (!cache && parent.CachedRawViewChildren != null)
            {
                logger.LogDebug($"return CachedRawViewChildren");
                return parent.CachedRawViewChildren;
            }

            List<AutomationElement> elements = new List<AutomationElement>();
            ITreeWalker treeWalker = UiCommon.UIAAutomation.TreeWalkerFactory.GetRawViewWalker();
            var ele = treeWalker.GetFirstChild(parent);
            while (ele != null)
            {
                ele.ScanCachedParent = parent;
                elements.Add(ele);
                ele = treeWalker.GetNextSibling(ele);
            }
            parent.CachedRawViewChildren = elements;
            return parent.CachedRawViewChildren;
        }

        #endregion
        public static bool IsUniqueItemByName(this AutomationElement element)
        {
            if (element.Properties.Name.IsSupported && !string.IsNullOrEmpty(element.Properties.Name.Value))
            {
                string name = element.Properties.Name.Value;
                var parent = UiCommon.RawViewWalker.GetParent(element);
                var foundItems = parent.FindAllChildren(new PropertyCondition(UiCommon.UIAAutomation.PropertyLibrary.Element.Name, name));
                return foundItems.Length == 1;
            }
            return false;
        }

        public static bool IsUniqueItemByClassName(this AutomationElement element)
        {
            if (element.Properties.ClassName.IsSupported && !string.IsNullOrEmpty(element.Properties.ClassName.Value))
            {
                string name = element.Properties.ClassName.Value;
                var parent = UiCommon.RawViewWalker.GetParent(element);
                var foundItems = parent.FindAllChildren(new PropertyCondition(UiCommon.UIAAutomation.PropertyLibrary.Element.ClassName, name));
                return foundItems.Length == 1;
            }
            return false;
        }

        public static bool IsUniqueItemByControlType(this AutomationElement element)
        {
            if (element.Properties.ControlType.IsSupported && element.Properties.ControlType.Value != FlaUI.Core.Definitions.ControlType.Unknown)
            {
                var name = element.Properties.ControlType.Value;
                var parent = UiCommon.RawViewWalker.GetParent(element);
                var foundItems = parent.FindAllChildren(new PropertyCondition(UiCommon.UIAAutomation.PropertyLibrary.Element.ControlType, name));
                return foundItems.Length == 1;
            }
            return false;
        }
        [DllImport("OLEACC", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern int WindowFromAccessibleObject(Accessibility.IAccessible param0, ref IntPtr phwnd);
        public static bool TryGetWindowHandleFromAccessible(this AutomationElement element, out IntPtr dWnd)
        {
            dWnd = IntPtr.Zero;
            if (element.Patterns.LegacyIAccessible.IsSupported)
            {
                var acc = element.Patterns.LegacyIAccessible.Pattern.GetIAccessible();
                WindowFromAccessibleObject(acc, ref dWnd);
            }
            return dWnd != IntPtr.Zero;
        }
        internal static bool ConiditionMatch(this WinSelectorItemProperty source, string propValue)
        {
            if (source.SelectorVersion == ConstFields.SELECTOR_OLD_FORMAT_VERSION &&
                source.Value.Contains(ConstFields.COMPARE_SYMBOL_STAR))
            {
                return PatternMatcher.FitsMask(propValue, source.Value);
            }
            else
            {
                if (RuleFactory.IsDecimalRule(source.Pattern))
                {
                    var rule = RuleFactory.GenerateDecimalRule(source.Pattern);
                    decimal d1 = 0;
                    decimal d2 = 0;
                    if (rule != null && decimal.TryParse(source.Value, out d1) && decimal.TryParse(propValue, out d2))
                    {
                        return rule.Compare(d1, d2);
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    var rule = RuleFactory.GenerateStringRule(source.Pattern);
                    if (rule != null)
                    {
                        var sVal = source.Value;
                        if (source.Name == ConstFields.SELECTOR_NAME)
                        {
                            sVal = sVal.Trim();
                            propValue = propValue.Trim();
                        }
                        if (rule is RuleEqual && sVal.IndexOf('|') > 0)
                        {
                            var vals = sVal.Split('|');
                            for (int i = 0; i < vals.Length; i++)
                            {
                                if (rule.Compare(propValue, vals[i]))
                                {
                                    return true;
                                }
                            }
                        }
                        return rule.Compare(propValue, sVal);
                    }
                    return false;
                }
            }
        }
        internal static bool Match(this AutomationProperty<ControlType> autoProp, string conrolType,out ControlType _controlType)
        {
            string eleControlTypeName = ConstFields.SELECTOR_Custom;
            if (autoProp.TryGetValue(out _controlType))
            {
                eleControlTypeName = _controlType.ToString();
            }
            else
            {
                logger.LogInformation("Element ControlType is not supported");
            }
            if (!conrolType.EqualIgnoreCase(eleControlTypeName))
            {
                logger.LogInformation($"{conrolType} is not equal to {eleControlTypeName}!");
                return false;
            }
            else
            {
                logger.LogInformation($"{conrolType} is equal to {eleControlTypeName}!");
            }
            return true;
        }
        internal static bool ConiditionMatch<T>(this AutomationProperty<T> autoProp, WinSelectorItemProperty source, out object value, string propName = null)
        {
            object compareValue = string.Empty;

            if (propName != null && !source.Name.EqualIgnoreCase(propName))
            {
                value = compareValue;
                return false;
            }
            if (source.Name.EqualIgnoreCase(ConstFields.SELECTOR_NAME))
            {
                source.OriginalValue = source.OriginalValue?.ToString().Trim();
            }
            if (autoProp.IsSupported)
            {
                Type dataType = typeof(T);

                if (dataType.IsClass)
                {
                    object valueObj = autoProp.Value as object;

                    if (valueObj is String valueStr)
                    {
                        compareValue = valueStr;
                    }
                    else if (valueObj != null)
                    {
                        compareValue = valueObj;
                    }
                }
                else if (dataType.IsValueType)
                {
                    object tempV = null;

                    if (dataType.IsEnum)
                    {
                        tempV = autoProp.Value.ToString();
                    }
                    else if (dataType == typeof(bool))
                    {
                        tempV = autoProp.Value.ToString();
                    }
                    else
                    {
                        tempV = autoProp.Value;
                    }

                    compareValue = tempV;
                }
                else
                {
                    compareValue = autoProp.Value;
                }
            }
            else
            {
                try
                {
                    compareValue = autoProp.ValueOrDefault;
                }
                catch
                { }
            }

            compareValue = compareValue ?? string.Empty;
            value = compareValue;
            if (source.Name.EqualIgnoreCase(ConstFields.SELECTOR_NAME))
            {
                compareValue = compareValue?.ToString().Trim();
            }
            bool ret = source.ConiditionMatch(compareValue.ToString());
            return ret;
        }

        internal static WinSelectorItemProperty GenerateSelectorProperty<T>(this AutomationProperty<T> autoProp, string name = null, bool enumValue2Str = false)
        {
            if (autoProp.IsSupported)
            {
                Type dataType = typeof(T);

                if (dataType.IsClass)
                {
                    object valueObj = autoProp.Value as object;

                    if (valueObj is String valueStr)
                    {
                        if (!string.IsNullOrEmpty(valueStr))
                        {
                            return new WinSelectorItemProperty(name, valueStr);
                        }
                    }
                    else if (valueObj != null)
                    {
                        return new WinSelectorItemProperty(name, autoProp.Value);
                    }
                }
                else if (dataType.IsValueType)
                {
                    object tempV = null;

                    if (dataType.IsEnum && enumValue2Str)
                    {
                        tempV = autoProp.Value.ToString();
                    }
                    else if (dataType == typeof(bool))
                    {
                        tempV = autoProp.Value.ToString();
                    }
                    else
                    {
                        tempV = autoProp.Value;
                    }

                    return new WinSelectorItemProperty(name, tempV);
                }
            }
            return null;
        }

        #region chrominum 内嵌页内的table扩展
        public static ChrominumHtmlTable AsChrominumHtmlTable(this AutomationElement self)
        {
            return self == null ? null : new ChrominumHtmlTable(self.FrameworkAutomationElement);
        }
        public static ChrominumHtmlTableRow AsChrominumHtmlTableRow(this AutomationElement self)
        {
            return self == null ? null : new ChrominumHtmlTableRow(self.FrameworkAutomationElement);
        }
        public static ChrominumHtmlTableColumnHeader AsChrominumHtmlTableColumnHeader(this AutomationElement self)
        {
            return self == null ? null : new ChrominumHtmlTableColumnHeader(self.FrameworkAutomationElement);
        }
        public static ChrominumHtmlTableCell AsChrominumHtmlTableCell(this AutomationElement self)
        {
            return self == null ? null : new ChrominumHtmlTableCell(self.FrameworkAutomationElement);
        }
        /// <summary>
        /// chrominum 内嵌页的table
        /// </summary>
        /// <param name="ele"></param>
        /// <returns></returns>
        public static bool IsPageTable(this AutomationElement ele)
        {
            if (ele.Patterns.LegacyIAccessible.IsSupported
                && ele.Patterns.LegacyIAccessible.Pattern.Role.TryGetValue(out var eleRole)
                && eleRole == AccessibilityRole.ROLE_SYSTEM_TABLE
                && ele.FrameworkType == FrameworkType.Chrome)
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// chrominum 内嵌页的行
        /// </summary>
        /// <param name="ele"></param>
        /// <returns></returns>
        public static bool IsPageTableRow(this AutomationElement ele)
        {
            if (!ele.Properties.ControlType.IsSupported
                && ele.Patterns.LegacyIAccessible.IsSupported
                && ele.Patterns.LegacyIAccessible.Pattern.Role.TryGetValue(out var trChildRole)
                && trChildRole == AccessibilityRole.ROLE_SYSTEM_ROW)
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// chrominum 内嵌页的单元格
        /// </summary>
        /// <param name="ele"></param>
        /// <returns></returns>
        public static bool IsPageTableCell(this AutomationElement ele)
        {
            if (ele.Patterns.LegacyIAccessible.IsSupported
                && ele.Patterns.LegacyIAccessible.Pattern.Role.TryGetValue(out var cellRole)
                && (cellRole == AccessibilityRole.ROLE_SYSTEM_CELL))
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// chrominum 内嵌页的列标题
        /// </summary>
        /// <param name="ele"></param>
        /// <returns></returns>
        public static bool IsPageTableColumnHeader(this AutomationElement ele)
        {
            if (ele.Patterns.LegacyIAccessible.IsSupported
                && ele.Patterns.LegacyIAccessible.Pattern.Role.TryGetValue(out var cellRole)
                && (cellRole == AccessibilityRole.ROLE_SYSTEM_COLUMNHEADER))
            {
                return true;
            }
            return false;
        }

        #endregion
        /// <summary>
        /// 获取祖先元素
        /// </summary>
        /// <param name="ele">AutomationElement</param>
        /// <param name="predicate">终止条件</param>
        /// <returns></returns>
        public static List<AutomationElement> GetAncestorSet(this AutomationElement ele, Predicate<AutomationElement> predicate = null)
        {
            var ancestorSet = new List<AutomationElement>();
            AutomationElement parent = ele;
            while (parent != null)
            {
                if (predicate.Invoke(parent))
                {
                    break;
                }
                ancestorSet.Add(parent);
                //遍历父元素获得整个控件链
                parent = parent.Parent;
            }
            ancestorSet.Reverse();
            return ancestorSet;
        }

        #region 使用RawView,遍历所有元素
        public static IEnumerable<AutomationElement> FindAllByRawView(this AutomationElement parent, Func<AutomationElement, int, bool> func = null)
        {
            ITreeWalker treeWalker = UiCommon.UIAAutomation.TreeWalkerFactory.GetRawViewWalker();
            var ele = treeWalker.GetFirstChild(parent);
            int idx = -1;
            while (ele != null)
            {
                if (func?.Invoke(ele, ++idx) == true)
                {
                    yield return ele;
                }
                ele = treeWalker.GetNextSibling(ele);
            }
        }

        #endregion
        #region 使用RawView,获取元素在同级中的index
        public static int IndexOfChildren(this AutomationElement parent, AutomationElement ele)
        {
            int index = -1;
            parent.FindAllByRawView((child, idx) =>
            {
                if (child.Equals(ele))
                {
                    index = idx;
                    return true;
                }
                return false;
            }).FirstOrDefault();
            ele.IndexInParent = index > -1 ? index : -1;
            return index;
        }
        #endregion
        public static Process GetProcess(this AutomationElement element)
        {
            if (!element.Properties.ProcessId.IsSupported) return null;
            return System.Diagnostics.Process.GetProcessById(element.Properties.ProcessId.Value);
        }
        public static void GetAncestorsPath(this List<AutomationElement> pathToRoot,
            out List<AutomationElement> ancestorsWindows,
            out List<AutomationElement> ancestorElements)
        {
            ancestorsWindows = new List<AutomationElement>();
            ancestorElements = new List<AutomationElement>();
            if (pathToRoot.Count <= 1)
            {
                ancestorsWindows.AddRange(pathToRoot);
                return;
            }
            var lastWndIdx = -1;
            for (int i = pathToRoot.Count - 1; i >= 0; i--)
            {
                if (!pathToRoot[i].Properties.NativeWindowHandle.TryGetValue(out var handle) || !NativeMethods.IsWindow(handle))
                {
                    continue;
                }
                if (i == pathToRoot.Count - 1)
                {
                    if (pathToRoot[i - 1].Properties.NativeWindowHandle.TryGetValue(out var nextHandle)
                        && handle != nextHandle)
                    {
                        lastWndIdx = i;
                        break;
                    }
                }
                else
                {
                    var wndElement = UiCommon.UIAAutomation.FromHandle(handle);
                    var firstEle = wndElement.FindAllByRawView((child, idx) => child.Equals(pathToRoot[i + 1]))
                                             .FirstOrDefault();
                    if (firstEle != null)
                    {
                        lastWndIdx = i;
                        break;
                    }
                }
            }
            if (lastWndIdx == -1)
            {
                lastWndIdx = 0;
            }
            ancestorsWindows = pathToRoot.Take(lastWndIdx + 1).ToList();
            ancestorElements = pathToRoot.Skip(lastWndIdx + 1).ToList();
        }
        public static bool GetDocRangeText(this TextBox textBox, out string content)
        {
            content = null;
            var isText = textBox.Patterns.Text.TryGetPattern(out var textPattern);
            if (isText)
            {
                content = textPattern.DocumentRange.GetText(-1);
            }
            return isText;
        }
    }

    internal static class SelectorItemExtendsion
    {
        internal static IEnumerable<WinSelectorItemProperty> FindAccordingName(this WindowsSelectorItem selectorItem, string propName)
        {
            var ret = selectorItem?.Properties.OfType<WinSelectorItemProperty>().Where(x => x.Enabled && x.Name.Equals(propName, StringComparison.OrdinalIgnoreCase));
            if (ret != null)
            {
                foreach (var item in ret)
                {
                    yield return item;
                }
            }
        }
        internal static WinSelectorItemProperty GetProperty(this WindowsSelectorItem selectorItem, string propName)
        {
            return selectorItem?.Properties.OfType<WinSelectorItemProperty>().Where(x => x.Name.Equals(propName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
        }
    }

}
