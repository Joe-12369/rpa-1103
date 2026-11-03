using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.WindowsAPI;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.WindowMatch;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
namespace II.RPA.Plugin.Browsers.Chromium
{

    public class ChromiumGuiOperator : GuiOperatorBase
    {
        #region 公共使用的全局变量
        private static ILogger logger = Logger.OfContext();
        #endregion

        public override eGuiType GuiType => eGuiType.Chromium;
        protected override Browser Browser { get; }
        public ChromiumGuiOperator() : this(null)
        {
        }
        public ChromiumGuiOperator(Browser browser)
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
                    logger.LogError($"find GetPreviousSibling 1 {RunContext.TimeInertval}");
                    child = UiCommon.TreeWalker.GetPreviousSibling(child);
                    logger.LogError($"find GetPreviousSibling 2 {RunContext.TimeInertval}");
                }
                catch (Exception e)
                {
                    logger.LogError($"find element error：{e}");
                    break;
                }
            }
            try
            {
                var cond = new List<ConditionBase>();
                cond.Add(new PropertyCondition(UiCommon.UIAAutomation.PropertyLibrary.Element.ClassName, "RootView"));
                cond.Add(new PropertyCondition(UiCommon.UIAAutomation.PropertyLibrary.Element.AriaRole, "dialog"));
                var dialog = rootElement.FindFirst(TreeScope.Descendants, new AndCondition(cond));
                if (dialog != null)
                {
                    return base.SpecialScenariosInspectNode(screenPoint, dialog, pointElement, selector, out inspectNode);
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"find dialog error");
            }
            logger.LogDebug($"Find alert dialog End  耗时：{RunContext.TimeInertval}");
            return false;
        }
        public override bool IsCanUse(Point screenPoint, AutomationElement rootElement, AutomationElement pointElement)
        {
            try
            {
                rootElement.Properties.NativeWindowHandle.TryGetValue(out var hWnd);
                var browserWindow = BrowserWindowProvider.MatchBrowserWindowByHandle(hWnd);
                if (browserWindow?.WindowType != GUI.Commons.WindowMatch.WindowType.Chromium)
                {
                    return false;
                }
                if (pointElement.Properties.ClassName.TryGetValue(out var pointElementClassName) && !pointElementClassName.Contains("Chrome_RenderWidgetHostHWND"))
                {
                    return false;
                }
                rootElement.Properties.NativeWindowHandle.TryGetValue(out var rootHWnd);
                DesktopWindow desktopWindow = new DesktopWindow();
                var saveDialog = desktopWindow.FindAllSavaDialogWindow(wnd => NativeMethods.MatchAncestor(wnd.Handle, rootHWnd)).FirstOrDefault();
                if (saveDialog != null)
                {
                    return false;
                }
                WindowObject windowObject = new WindowObject(rootHWnd);
                var allChildWindows = windowObject.FindAllChildWindowEx(wnd => wnd.IsVisible, 99).ToList();
                var renderWnds = allChildWindows.Where(wnd => wnd.ClassName.Contains("Chrome_RenderWidgetHostHWND"));
                logger.LogDebug($"EnumAllChildWindows Chrome_RenderWidgetHostHWND 耗时：：{RunContext.TimeInertval}");
                WindowObject devToolWnd = null;
                WindowObject printWnd = null;
                List<WindowObject> otherWnd = new List<WindowObject>();
                foreach (var renderWnd in renderWnds)
                {
                    var elementWnd = renderWnd.ToAutomationElement();
                    var accValue = string.Empty;
                    if (elementWnd != null && elementWnd.Patterns.LegacyIAccessible.TryGetPattern(out var pattern)
                                && pattern.Value.TryGetValue(out var value))
                    {
                        accValue = value;
                    }
                    if (accValue.Contains("chrome://print")
                            || accValue.Contains("chrome://settings")
                            || accValue.Contains("chrome://extensions")
                            || accValue.Contains("chrome://downloads"))
                    {
                        printWnd = renderWnd;
                        return false;
                    }
                    if (accValue.StartsWith("devtools:"))
                    {
                        devToolWnd = renderWnd;
                    }
                    else
                    {
                        otherWnd.Add(renderWnd);
                    }
                }
                logger.LogDebug($"foreach  renderWnd 耗时：{RunContext.TimeInertval}");
                if (devToolWnd == null && printWnd == null && otherWnd.Any(wnd => wnd.WindowRect.Contains(screenPoint)))
                {
                    return true;
                }
                if (devToolWnd != null && otherWnd.FirstOrDefault()?.WindowRect.Contains(screenPoint) == true)
                {
                    return true;
                }
                return false;
            }
            catch (RPAException ex)
            {
                if (ex.ErrorMessageType == eErrorMessageType.Error_ChromeExtensionUnopen)
                {
                    if (sw_ChromeExtensionUnopen.ElapsedMilliseconds >= 5000)
                    {
                        logger.LogDebug("beyond5000ms..............");
                        throw ex;
                    }
                }
                else if (ex.ErrorMessageType == eErrorMessageType.Error_ChromeExtensionUnInstalled)
                {
                    throw ex;
                }
            }
            finally
            {
                logger.LogDebug($"End IsCanUse  Chrome ");
            }
            return false;
        }


        public override bool InspectNode(Point screenPoint, AutomationElement rootElement, AutomationElement pointElement, string selector, out IGuiNode inspectNode)
        {
            logger.LogDebug($"start {GuiType.ToString()} InspectNode   耗时：{RunContext.TimeInertval}");
            var inspectResult = this.SpecialScenariosInspectNode(screenPoint, rootElement, pointElement, selector, out inspectNode);
            logger.LogDebug($"end {GuiType.ToString()} SpecialScenariosInspectNode  win32  耗时：{RunContext.TimeInertval}");
            if (!inspectResult)
            {
                inspectResult = base.InspectNode(screenPoint, rootElement, pointElement, selector, out inspectNode);
            }
            logger.LogDebug($"stop {GuiType.ToString()} InspectNode   耗时：{RunContext.TimeInertval}");
            return inspectResult;
        }


        private BrowserTabPage curActiveTab = null;
        #endregion



    }
}

