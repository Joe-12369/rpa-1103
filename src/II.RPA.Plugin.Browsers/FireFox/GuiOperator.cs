using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.WindowsAPI;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace II.RPA.Plugin.Browsers.FireFox
{

    public class GuiOperator : GuiOperatorBase
    {
        #region 公共使用的全局变量
        private AutomationElement[] rootElementChilds = null;
        private static ILogger logger = Logger.OfContext();
        /// <summary>
        /// 支持的进程名，忽略大小写，非全程，判断包含的方式，可只配置特征字符串
        /// </summary>
        private readonly string[] SupportProcess = { "chrome.exe", "360chrome" };
        #endregion
        public override eGuiType GuiType => eGuiType.FireFox;
        protected override Browser Browser
        {
            get;
        }
        public GuiOperator() : this(null)
        {
        }
        public GuiOperator(Browser browser)
        {
            this.Browser = browser;
        }

        #region 拾取相关
        protected override bool SpecialScenariosInspectNode(Point screenPoint, AutomationElement rootElement, AutomationElement pointElement, string selector, out IGuiNode inspectNode)
        {
            inspectNode = null;
            //var children = UiCommon.TreeWalker.GetFirstChild(rootElement);
            //while (children != null)
            //{
            //    if ((children.Properties.LocalizedControlType.IsSupported
            //        && children.Properties.LocalizedControlType.ValueOrDefault == "对话框")
            //        || (children.Patterns.LegacyIAccessible.IsSupported
            //        && children.Patterns.LegacyIAccessible.Pattern.Role.IsSupported
            //        && children.Patterns.LegacyIAccessible.Pattern?.Role == AccessibilityRole.ROLE_SYSTEM_DIALOG)
            //        && children.BoundingRectangle.Contains(screenPoint)
            //        && children.BoundingRectangle.Width * children.BoundingRectangle.Height <= pointElement.BoundingRectangle.Width * pointElement.BoundingRectangle.Height)
            //    {
            //        if (children.Equals(pointElement))
            //        {
            //            continue;
            //        }
            //        return base.SpecialScenariosInspectNode(screenPoint, children, pointElement, selector, out inspectNode);

            //    }
            //    children = UiCommon.TreeWalker.GetNextSibling(children);
            //}
            return false;
        }
        public override bool IsCanUse(Point screenPoint, AutomationElement rootElement, AutomationElement pointElement)
        {
            try
            {
                logger.LogDebug($"Start IsCanUse  Firefox ");

                rootElement.Properties.NativeWindowHandle.TryGetValue(out var hWnd);
                var browserWindow = BrowserWindowProvider.MatchBrowserWindowByHandle(hWnd);
                if (browserWindow?.WindowType != GUI.Commons.WindowMatch.WindowType.FireFox)
                {
                    return false;
                }
                var children = UiCommon.TreeWalker.GetFirstChild(rootElement);
                while (children != null)
                {
                    if ((children.Properties.LocalizedControlType.IsSupported
                        && children.Properties.LocalizedControlType.ValueOrDefault == "对话框")
                        || (children.Patterns.LegacyIAccessible.IsSupported
                        && children.Patterns.LegacyIAccessible.Pattern.Role.IsSupported
                        && children.Patterns.LegacyIAccessible.Pattern?.Role == AccessibilityRole.ROLE_SYSTEM_DIALOG)
                      )
                    {
                        return false;

                    }
                    children = UiCommon.TreeWalker.GetNextSibling(children);
                }
                {
                    List<ConditionBase> conditions = new List<ConditionBase>
                    {
                        new PropertyCondition(UiCommon.UIAAutomation.PropertyLibrary.Element.ControlType, ControlType.Document)
                    };
                    AndCondition ac = new AndCondition(conditions);
                    var child = rootElement.FindFirstDescendant(ac);
                    if (child?.Properties.BoundingRectangle.TryGetValue(out var rect) == true && rect.Contains(screenPoint))
                    {
                        if (!sw_ChromeExtensionUnopen.IsRunning)
                        {
                            sw_ChromeExtensionUnopen.Start();
                        }
                        this.Browser.CheckBrowserExtensions();
                        return true;
                    }
                    return false;
                }

            }
            catch (RPAException ex)
            {
                if (ex.ErrorMessageType == eErrorMessageType.Error_FireFoxExtensionUnopen)
                {

                    if (sw_ChromeExtensionUnopen.ElapsedMilliseconds >= 5000)
                    {
                        throw ex;
                    }
                }
                else if (ex.ErrorMessageType == eErrorMessageType.Error_FireFoxExtensionUnInstalled)
                {
                    throw ex;
                }
            }
            finally
            {
                logger.LogDebug($"End IsCanUse  Firefox ");
            }

            return false;
        }
        public override bool InspectNode(Point screenPoint, AutomationElement rootElement, AutomationElement pointElement, string selector, out IGuiNode inspectNode)
        {
            logger.LogDebug($"start {GuiType.ToString()} InspectNode   耗时：{RunContext.TimeInertval}");
            var inspectResult = this.SpecialScenariosInspectNode(screenPoint, rootElement, pointElement, selector, out inspectNode);
            logger.LogDebug($"start {GuiType.ToString()} InspectNode  win32  耗时：{RunContext.TimeInertval}");
            if (!inspectResult)
            {
                inspectResult = base.InspectNode(screenPoint, rootElement, pointElement, selector, out inspectNode);
            }
            logger.LogDebug($"stop {GuiType.ToString()} InspectNode   耗时：{RunContext.TimeInertval}");
            return inspectResult;
        }
        /// <summary>
        /// 获取当前激活的TabPage,获取浏览器特殊，不使用缓存
        /// </summary>
        /// <param name="wnd"></param>
        /// <returns></returns>
        protected override BrowserTabPage GetCurActiveTab(IntPtr wnd) => this.Browser.AcquireCurrentActiveTab();
        #endregion


    }
}
