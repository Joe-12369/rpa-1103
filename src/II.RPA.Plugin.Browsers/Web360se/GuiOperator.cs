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

namespace II.RPA.Plugin.Browsers.Web360se
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
        public override eGuiType GuiType => eGuiType.Web360se;
        protected override Browser Browser { get; }
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
            logger.LogDebug($"Find alert dialog Start   耗时：{RunContext.TimeInertval}");
            var child = UiCommon.TreeWalker.GetLastChild(rootElement);
            while (child != null)
            {
                try
                {
                    if (!child.Properties.ControlType.IsSupported
                        && child.Patterns.LegacyIAccessible.IsSupported
                        && child.Patterns.LegacyIAccessible.Pattern.Role.ValueOrDefault == AccessibilityRole.ROLE_SYSTEM_DIALOG)
                    {
                        return base.SpecialScenariosInspectNode(screenPoint, child, pointElement, selector, out inspectNode);
                    }
                    child = UiCommon.TreeWalker.GetPreviousSibling(child);
                }
                catch (Exception e)
                {
                    logger.LogError($"find element error：{e}");
                    break;
                }
            }
            logger.LogDebug($"Find alert dialog End  耗时：{RunContext.TimeInertval}");
            return false;
        }
        public override bool IsCanUse(Point screenPoint, AutomationElement rootElement, AutomationElement pointElement)
        {
            try
            {
                logger.LogDebug("Start IsCanUse 360se");
                var className = rootElement.ClassName;
                if (className.Contains("360se6_Frame") && rootElement.Name.Contains(" - 360安全浏览器") && rootElement.Name != "RPA Studio")
                {
                    var child = UiCommon.TreeWalker.GetFirstChild(rootElement);
                    while (child != null)
                    {
                        var childBoundingRectangle = child.Properties.BoundingRectangle.ValueOrDefault;
                        if (!childBoundingRectangle.Contains(screenPoint))
                        {
                            child = UiCommon.TreeWalker.GetNextSibling(child);
                            continue;
                        }
                        var childClaName = child.Properties.ClassName.ValueOrDefault;
                        if (child.Patterns.LegacyIAccessible.IsSupported && child.Patterns.LegacyIAccessible.Pattern.Role.ValueOrDefault == AccessibilityRole.ROLE_SYSTEM_DIALOG)
                        {
                            return false;
                        }
                        else if (childClaName == "Chrome_RenderWidgetHostHWND")
                        {
                            if (child.Properties.Name.ValueOrDefault == "DevTools")
                            {
                                return false;
                            }
                            if (!sw_ChromeExtensionUnopen.IsRunning)
                            {
                                sw_ChromeExtensionUnopen.Reset();
                            }
                            this.Browser.CheckBrowserExtensions();
                            return true;
                        }
                        child = UiCommon.TreeWalker.GetNextSibling(child);
                    }
                    return false;
                }
            }
            catch (RPAException ex)
            {
                if (ex.ErrorMessageType == eErrorMessageType.Error_360ExtensionUnopen)
                {
                    if (sw_ChromeExtensionUnopen.ElapsedMilliseconds >= 5000)
                    {
                        throw ex;
                    }
                }
                else if (ex.ErrorMessageType == eErrorMessageType.Error_360ExtensionUnInstalled)
                {
                    throw ex;
                }
            }
            finally
            {
                logger.LogDebug("End IsCanUse 360se");
            }

            return false;
        }

        public override bool InspectNode(Point screenPoint, AutomationElement rootElement, AutomationElement pointElement, string selector, out IGuiNode inspectNode)
        {
            logger.LogDebug($"start {GuiType.ToString()} InspectNode   耗时：{RunContext.TimeInertval}");
            var inspectResult = this.SpecialScenariosInspectNode(screenPoint, rootElement, pointElement, selector, out inspectNode);
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
