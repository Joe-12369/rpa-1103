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

namespace II.RPA.Plugin.Browsers.Edge
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
        public override eGuiType GuiType => eGuiType.Edge;
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
            if (!InspectIsTimeout)
            {
                return false;
            }
            var children = UiCommon.TreeWalker.GetLastChild(rootElement);
            while (children != null)
            {
                if (children.Properties.ClassName.IsSupported
                   && children.ClassName.Equals("BrowserRootView"))
                {
                    var child = UiCommon.TreeWalker.GetLastChild(children);
                    while (child != null)
                    {
                        if ((child.Properties.LocalizedControlType.IsSupported
                            && child.Properties.LocalizedControlType.TryGetValue(out string value)
                            && value == "对话框")
                            || (child.Patterns.LegacyIAccessible.IsSupported
                            && child.Patterns.LegacyIAccessible.Pattern.Role == AccessibilityRole.ROLE_SYSTEM_DIALOG))
                        {
                            if (children.Properties.ClassName.IsSupported &&
                                children.ClassName == "Chrome_RenderWidgetHostHWND"
                                && children.Equals(pointElement))
                            {
                                continue;
                            }
                            return base.SpecialScenariosInspectNode(screenPoint, rootElement, pointElement, selector, out inspectNode);
                        }
                        child = UiCommon.TreeWalker.GetPreviousSibling(child);
                    }
                    break; //找到就可以退出
                }
                break;
            }
            return false;
        }
        public override bool IsCanUse(Point screenPoint, AutomationElement rootElement, AutomationElement pointElement)
        {
            try
            {
                logger.LogDebug($"Start IsCanUse  Edge ");
                var className = rootElement.ClassName;
                var name = rootElement.Name;

                if (className.Contains("Chrome_WidgetWin_1") && name.Contains("Microsoft") && name.Contains("Edge"))
                {
                    try
                    {
                        var element = UiCommon.UIAAutomation.FromPoint(screenPoint);
                        while (element != null)
                        {
                            element = UiCommon.TreeWalker.GetParent(element);
                            if (element == null)
                            {
                                break;
                            }
                            ///pin Edge浏览器下载窗口钉上
                            if (element.Properties.ClassName.ValueOrDefault == "HubWebViewContainerView" &&
                                element.Properties.ControlType.ValueOrDefault == ControlType.Pane)
                            {
                                return false;
                            }
                            ///unpin Edge浏览器下载窗口没有钉上
                            if (element.Properties.ClassName.ValueOrDefault == "BubbleDialogDelegateView" &&
                                element.Properties.ControlType.ValueOrDefault == ControlType.Pane)
                            {
                                return false;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        //ignore
                    }
                    var browser = pointElement;
                    if (browser.Properties.ClassName.IsSupported
                        && browser.ClassName == "Chrome_RenderWidgetHostHWND"
                        && browser.BoundingRectangle.Contains(screenPoint))
                    {
                        if (!sw_ChromeExtensionUnopen.IsRunning)
                        {
                            sw_ChromeExtensionUnopen.Reset();
                        }
                        this.Browser.CheckBrowserExtensions();

                        return true;
                    }
                    return false;
                }
            }
            catch (RPAException ex)
            {
                if (ex.ErrorMessageType == eErrorMessageType.Error_EdgeExtensionUnopen)
                {
                    if (sw_ChromeExtensionUnopen.ElapsedMilliseconds >= 5000)
                    {
                        throw ex;
                    }
                }
                else if (ex.ErrorMessageType == eErrorMessageType.Error_EdgeExtensionUnInstalled)
                {
                    throw ex;
                }
            }
            finally
            {
                logger.LogDebug($"End IsCanUse  Edge ");
            }

            return false;
        }
        public override bool InspectNode(Point screenPoint, AutomationElement rootElement, AutomationElement pointElement, string selector, out IGuiNode inspectNode)
        {
            logger.LogDebug($"start {GuiType.ToString()} InspectNode   耗时：{RunContext.TimeInertval}");
            var inspectResult = this.SpecialScenariosInspectNode(screenPoint, rootElement, pointElement, selector, out inspectNode);
            logger.LogDebug($"start {GuiType.ToString()} WIN32 InspectNode   耗时：{RunContext.TimeInertval}");
            if (!inspectResult)
            {
                inspectResult = base.InspectNode(screenPoint, rootElement, pointElement, selector, out inspectNode);
            }
            logger.LogDebug($"stop {GuiType.ToString()} InspectNode   耗时：{RunContext.TimeInertval}");
            return inspectResult;
        }

        #endregion


    }
}
