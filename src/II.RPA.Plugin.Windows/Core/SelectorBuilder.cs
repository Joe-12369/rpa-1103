using FlaUI.Core.AutomationElements;
using FlaUI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons;
using System.Xml.XPath;
using Newtonsoft.Json.Linq;

namespace II.RPA.Plugin.Windows.Core
{
    internal class SelectorBuilderHelper
    {
        public static (List<WindowsSelectorItem> validSelectorItems, List<IntPtr> hwndList, List<AutomationElement> newPathToRoot) BuildWindowSelector(List<AutomationElement> pathToRoot)
        {

            (var hwndList, var elementList) = SplitIntoWindowAndElements(pathToRoot);
            List<WindowsSelectorItem> validSelectorItems = new List<WindowsSelectorItem>();
            for (int i = 0; i < hwndList.Count; i++)
            {
                var item = new WindowsSelectorItem(i == 0 ? IntPtr.Zero : hwndList[i - 1], hwndList[i]);
                validSelectorItems.Add(item);
            }
            if (elementList.Count > 0 && hwndList.Count > 0)
            {
                elementList.Insert(0, UiCommon.UIAAutomation.FromHandle(hwndList.Last()));
            }
            StandardizeDesktopSelector(validSelectorItems, hwndList);
            DisableDetachedWindowAncestors(validSelectorItems);
            return (validSelectorItems, hwndList, elementList);
        }

        public static void DisableDetachedWindowAncestors(List<WindowsSelectorItem> validSelectorItems)
        {
            bool disable = false;
            for (int i = validSelectorItems.Count - 1; i >= 0; i--)
            {
                if (disable)
                {
                    validSelectorItems[i].Enabled = false;
                    continue;
                }
                if (validSelectorItems[i].ElementType.HasFlag(SelectorPropsType.DetachedChildWindow))
                {
                    disable = true;
                }
            }
        }
        public static (List<IntPtr>, List<AutomationElement>) SplitIntoWindowAndElements(List<AutomationElement> pathToRoot)
        {
            IntPtr lastHwnd = IntPtr.Zero;
            int lastWndIdx = -1;
            AutomationElement wndEle = null;
            for (int i = pathToRoot.Count - 1; i >= 0; i--)
            {
                if (!pathToRoot[i].Properties.NativeWindowHandle.TryGetValue(out lastHwnd) || !NativeMethods.IsWindow(lastHwnd))
                {
                    continue;
                }
                wndEle = UiCommon.UIAAutomation.FromHandle(lastHwnd);
                if (i == pathToRoot.Count - 1)
                {
                    if (wndEle.Equals(pathToRoot[i]))
                    {
                        lastWndIdx = i;
                        break;
                    }
                }
                else
                {
                    int indexInParent = wndEle.IndexOfChildren(pathToRoot[i + 1]);
                    if (indexInParent > -1)
                    {
                        lastWndIdx = i;
                        break;
                    }
                }
            }
            List<IntPtr> hwndList = UiCommon.GetAncestorWindowList(lastHwnd);
            List<AutomationElement> elementList = pathToRoot.Skip(lastWndIdx + 1).ToList();
            return (hwndList, elementList);
        }
        private static void StandardizeDesktopSelector(List<WindowsSelectorItem> validSelectorItems, List<IntPtr> hwndList)
        {
            var sysListViewHandle = GetDesktopListViewHWND();
            if (hwndList.IndexOf(sysListViewHandle) < 0)
            {
                return;
            }
            int syslistview32_node_index = validSelectorItems.FindIndex(st => st.Properties.Any(p => p.Name == ConstFields.SELECTOR_CLASS_NAME && p.Value.EqualIgnoreCase(ConstFields.PROP_CLASS_NAME_SYSLISTVIEW32)));
            if (syslistview32_node_index >= 0)
            {
                validSelectorItems[syslistview32_node_index].Properties.RemoveAll(p => p.Name == ConstFields.SELECTOR_NAME);
                var processProp = validSelectorItems[0].Properties.Where(p => p.Name == ConstFields.SELECTOR_ROOT_PROCESSNAME).FirstOrDefault();
                if (syslistview32_node_index > 0)
                {
                    validSelectorItems.RemoveRange(0, 1);
                }
                //增加通用首节点
                string progmanSelectorStr_first = ConstFields.SELECTOR_ROOT_TOP_NODESELECTOR;
                var progmanSelector_first = WindowsSelectorItem.FromJObject(JObject.Parse(progmanSelectorStr_first), "1.0");
                progmanSelector_first.Properties.Add(processProp);
                validSelectorItems.Insert(0, (WindowsSelectorItem)progmanSelector_first);

                //如果没有SHELLDLL_DefView  增加此节点
                int shelldll_defview_node_index = validSelectorItems.FindIndex(st => st.Properties.Any(p => p.Name == ConstFields.SELECTOR_CLASS_NAME && p.Value.EqualIgnoreCase(ConstFields.PROP_CLASS_NAME_SHELLDLL_DEFVIEW)));
                if (shelldll_defview_node_index < 0)
                {
                    string progmanSelectorStr_second = ConstFields.SELECTOR_ROOT_TOP_SHELLDLL_DefView;
                    var progmanSelector_second = WindowsSelectorItem.FromJObject(JObject.Parse(progmanSelectorStr_second), "1.0");
                    validSelectorItems.Insert(1, (WindowsSelectorItem)progmanSelector_second);
                }
            }
        }
        private static IntPtr GetDesktopListViewHWND()
        {
            IntPtr hDesktopListView = IntPtr.Zero;
            IntPtr hWorkerW = IntPtr.Zero;

            IntPtr hProgman = NativeMethods.FindWindow("Progman", null);
            IntPtr hDesktopWnd = NativeMethods.GetDesktopWindow();

            // If the main Program Manager window is found
            if (hProgman != IntPtr.Zero)
            {
                // Get and load the main List view window containing the icons (found using Spy++).
                IntPtr hShellViewWin = NativeMethods.FindWindowEx(hProgman, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (hShellViewWin != IntPtr.Zero)
                {
                    hDesktopListView = NativeMethods.FindWindowEx(hShellViewWin, IntPtr.Zero, "SysListView32", null);
                }
                else
                {
                    // When this fails (happens in Windows-7 when picture rotation is turned ON), then look for the WorkerW windows list to get the
                    // correct desktop list handle.
                    // As there can be multiple WorkerW windows, so iterate through all to get the correct one
                    do
                    {
                        hWorkerW = NativeMethods.FindWindowEx(hDesktopWnd, hWorkerW, "WorkerW", null);
                        hShellViewWin = NativeMethods.FindWindowEx(hWorkerW, IntPtr.Zero, "SHELLDLL_DefView", null);
                    } while (hShellViewWin == IntPtr.Zero && hWorkerW != IntPtr.Zero);

                }
                // Get the ListView control
                hDesktopListView = NativeMethods.FindWindowEx(hShellViewWin, IntPtr.Zero, "SysListView32", null);
            }

            return hDesktopListView;
        }
    }
}
