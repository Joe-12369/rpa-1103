using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Accessibility;
using FlaUI.Core.AutomationElements;
using II.RPA.Plugin.GUI.Commons;
using System.Drawing;
using Microsoft.Extensions.Logging;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using System.ComponentModel;

namespace II.RPA.Plugin.IE.V2.Automation.Provider
{
    internal class TabActivator
    {
        private static ILogger logger = Logger.OfContext();

        #region Nested type: OBJID

        private enum OBJID : uint
        {
            OBJID_WINDOW = 0x00000000,
        }

        #endregion

        #region Declarations

        private const int CHILDID_SELF = 0;
        private readonly IntPtr _hWnd;
        private IAccessible _accessible;
        private string _title;

        [DllImport("oleacc.dll")]
        private static extern int AccessibleObjectFromWindow(IntPtr hwnd, uint id, ref Guid iid,
                                                             [In, Out, MarshalAs(UnmanagedType.IUnknown)] ref object
                                                                 ppvObject);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass,
                                                  string lpszWindow);

        [DllImport("oleacc.dll")]
        private static extern int AccessibleChildren(IAccessible paccContainer, int iChildStart, int cChildren,
                                                     [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] object[] rgvarChildren, out int pcObtained);

        #endregion

        #region Constructors

        internal TabActivator(IntPtr ieHandle, string title = "")
        {
            _title = title;
            _hWnd = ieHandle;
            AccessibleObjectFromWindow(GetDirectUIHWND(ieHandle), OBJID.OBJID_WINDOW, ref _accessible);

            CheckForAccessible();
        }

        private TabActivator(IAccessible acc)
        {
            if (acc == null)
                throw new Exception("Could not get accessible");

            _accessible = acc;
        }

        #endregion

        private TabActivator[] Children
        {
            get
            {
                var num = 0;
                var res = GetAccessibleChildren(_accessible, out num);

                if (res == null)
                    return new TabActivator[0];

                var list = new List<TabActivator>(res.Length);

                foreach (object obj in res)
                {
                    var acc = obj as IAccessible;

                    if (acc != null)
                        list.Add(new TabActivator(acc));
                }

                return list.ToArray();
            }
        }

        private int ChildCount
        {
            get { return _accessible.accChildCount; }
        }

        /// <summary>
        /// Gets LocationUrl of the tab
        /// </summary>
        private string LocationUrl
        {
            get
            {
                var url = _accessible.accDescription[CHILDID_SELF];
                try
                {
                    if (url.Contains(Environment.NewLine))
                        url = url.Split('\n')[1];

                    return url;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        private void CheckForAccessible()
        {
            if (_accessible == null)
                throw new Exception("Could not get accessible.  Accessible can't be null");
        }

        internal void ActivateByTabsUrl(string tabsUrl)
        {

            var tabIndexToActivate = GetTabIndexToActivate(tabsUrl);

            AccessibleObjectFromWindow(GetDirectUIHWND(_hWnd), OBJID.OBJID_WINDOW, ref _accessible);

            CheckForAccessible();

            var index = 0;
            var ieDirectUIHWND = new TabActivator(_hWnd);

            foreach (var accessor in ieDirectUIHWND.Children)
            {
                foreach (var child in accessor.Children)
                {
                    foreach (var tab in child.Children)
                    {
                        if (tabIndexToActivate >= child.ChildCount - 1)
                            return;

                        if (index == tabIndexToActivate)
                        {
                            tab.ActivateTab();
                            return;
                        }
                        index++;
                    }
                }
            }
        }

        private void ActivateTab()
        {
            _accessible.accDoDefaultAction(CHILDID_SELF);
        }
        public void SelectTabItem(int tabId)
        {
            FlaUI.Core.AutomationElements.TabItem tabElement = null;
            try
            {
                var handle = GetDirectUIHWND(_hWnd);
                var element = UiCommon.UIAAutomation.FromHandle(handle);
                var tabItems = element.FindAllDescendants();
                var title = _title;

                // 补充逻辑 
                //title.Contains(ele.Name) 针对Win32 方式获取Name短于document 获取到的title数据
                var matchedItems = (from ele in tabItems
                                    where title.Length <= 256 ? ele.Name.StartsWith(title) : title.Contains(ele.Name)
                                    select ele);
                var matchedTabItems = matchedItems.ToList<AutomationElement>();
                if (matchedTabItems.Count == 1)
                {
                    if (FlaUI.Core.Definitions.ControlType.TabItem == matchedTabItems[0].ControlType)
                    {
                        tabElement = matchedTabItems[0].AsTabItem();
                        if (tabElement != null)
                        {
                            try
                            {
                                logger.LogDebug($"tabElement Select");
                                tabElement.Select();
                                RPAUtils.Retry(() => NativeMethods.IsWindowVisible((IntPtr)tabId), 500, 10);
                            }
                            catch { }
                        }
                    }
                    if (!NativeMethods.IsWindowVisible((IntPtr)tabId) && matchedTabItems[0].Properties.BoundingRectangle.IsSupported)
                    {
                        logger.LogDebug($"MouseClick SelectTabItem");
                        var rect = matchedTabItems[0].BoundingRectangle;
                        Point point = new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
                        AutoItHelper.MouseClick("left", point);
                    }
                }
            }
            catch (Exception ex)
            {
                if ((ex is COMException) && tabElement != null)
                {
                    var rect = tabElement.BoundingRectangle;
                    Point point = new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
                    AutoItHelper.MouseClick("left", point);
                }
                else
                {
                    throw ex;
                }
            }
            RPAUtils.Retry(() => NativeMethods.IsWindowVisible((IntPtr)tabId), 200, 10);
        }
        private int GetTabIndexToActivate(string tabsUrl)
        {
            var directUIHWND = GetDirectUIHWND(_hWnd);

            AccessibleObjectFromWindow(directUIHWND, OBJID.OBJID_WINDOW, ref _accessible);

            CheckForAccessible();

            var index = 0;
            var ieDirectUIHWND = new TabActivator(_hWnd);

            foreach (var accessor in ieDirectUIHWND.Children)
            {
                foreach (var child in accessor.Children)
                {
                    foreach (var tab in child.Children)
                    {
                        try
                        {
                            var tabUrl = tab.LocationUrl;

                            if (!string.IsNullOrEmpty(tabUrl))
                            {
                                if (tab.LocationUrl.Equals(tabsUrl))
                                    return index;
                            }

                            index++;
                        }
                        catch (Exception)
                        {
                        }

                    }
                }
            }

            return -1;
        }

        private IntPtr GetDirectUIHWND(IntPtr ieFrame)
        {
            // For IE 8:
            var directUI = FindWindowEx(ieFrame, IntPtr.Zero, "CommandBarClass", null);
            directUI = FindWindowEx(directUI, IntPtr.Zero, "ReBarWindow32", null);
            directUI = FindWindowEx(directUI, IntPtr.Zero, "TabBandClass", null);
            directUI = FindWindowEx(directUI, IntPtr.Zero, "DirectUIHWND", null);

            if (directUI == IntPtr.Zero)
            {
                // For IE 9:
                //directUI = FindWindowEx(ieFrame, IntPtr.Zero, "WorkerW", "Navigation Bar");
                directUI = FindWindowEx(ieFrame, IntPtr.Zero, "WorkerW", null);
                directUI = FindWindowEx(directUI, IntPtr.Zero, "ReBarWindow32", null);
                directUI = FindWindowEx(directUI, IntPtr.Zero, "TabBandClass", null);
                directUI = FindWindowEx(directUI, IntPtr.Zero, "DirectUIHWND", null);
            }

            return directUI;
        }

        private static int AccessibleObjectFromWindow(IntPtr hwnd, OBJID idObject, ref IAccessible acc)
        {
            var guid = new Guid("{618736e0-3c3d-11cf-810c-00aa00389b71}"); // IAccessibleobject obj = null;
            object obj = null;

            var num = AccessibleObjectFromWindow(hwnd, (uint)idObject, ref guid, ref obj);

            acc = (IAccessible)obj;

            return num;
        }

        private static object[] GetAccessibleChildren(IAccessible ao, out int childs)
        {
            childs = 0;
            object[] ret = null;
            var count = ao.accChildCount;

            if (count > 0)
            {
                ret = new object[count];
                AccessibleChildren(ao, 0, count, ret, out childs);
            }

            return ret;
        }
    }
}

