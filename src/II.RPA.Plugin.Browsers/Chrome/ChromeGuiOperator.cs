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
namespace II.RPA.Plugin.Browsers.Chrome
{

    public class ChromeGuiOperator : GuiOperatorBase
    {
        #region 公共使用的全局变量
        private AutomationElement[] rootElementChilds = null;
        private static ILogger logger = Logger.OfContext();
        /// <summary>
        /// 支持的进程名，忽略大小写，非全程，判断包含的方式，可只配置特征字符串
        /// </summary>
        private readonly string[] SupportProcess = { "chrome", "qaxbrowser" };
        #endregion
        public override eGuiType GuiType => eGuiType.Chrome;
        protected override Browser Browser { get; }
        public ChromeGuiOperator() : this(null)
        {
        }
        public ChromeGuiOperator(Browser browser)
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
                logger.LogDebug($"Start IsCanUse ChromeGuiOperator 耗时：{RunContext.TimeInertval}");
                rootElement.Properties.NativeWindowHandle.TryGetValue(out var rootHWnd);
                WindowObject windowObject = new WindowObject(rootHWnd);
                //if (!this.Browser.GetBrowserAppPath().EqualIgnoreCase(windowObject.AppExePath))
                //{
                //    return false;
                //}

                bool isChrome = IsChromeBrowerApp(rootElement);
                if (isChrome)        //Google Chrome处理 
                {
                    if (pointElement.Properties.ClassName.TryGetValue(out var pointElementClassName) && !pointElementClassName.Contains("Chrome_RenderWidgetHostHWND"))
                    {
                        return false;
                    }
                    DesktopWindow desktopWindow = new DesktopWindow();
                    var saveDialog = desktopWindow.FindAllSavaDialogWindow(wnd => NativeMethods.MatchAncestor(wnd.Handle, rootHWnd)).FirstOrDefault();
                    if (saveDialog != null)
                    {
                        return false;
                    }
                    var renderWnds = windowObject.EnumAllChildWindows(wnd => wnd.IsVisible && wnd.ClassName.Contains("Chrome_RenderWidgetHostHWND"), false);
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
                            || accValue.Contains("chrome://downloads")
                            || accValue.Contains("chrome-extension://")
                            )
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
                logger.LogDebug($"End IsCanUse  ChromeGuiOperator ");
            }
            return false;
        }

        private bool IsChromeBrowerApp(AutomationElement rootElement)
        {
            //return false;//取消本行注释以让本地网页接口支持chrome.exe等日常浏览器
            //NLogger.Default.Trace($" rootElement className:{className},Name:{rootElement.Name}");
            if (rootElement.Properties.ClassName.TryGetValue(out var className) && className.Contains("Chrome_WidgetWin_1"))        //Google Chrome处理 
            {
                rootElement.Properties.NativeWindowHandle.TryGetValue(out var hWnd);
                string processName = UiCommon.GetProcessNameByCache(hWnd);
                if (rootElement.Properties.Name.TryGetValue(out var name) && name.Contains(" - Google Chrome"))
                {
                    if (!string.IsNullOrEmpty(processName) && processName.Contains("飞鸽客服工作台.exe"))
                    {
                        return false;
                    }
                    return true;
                }
                else
                {
                    //在拾取含alert页面时  偶现 className ==Chrome_WidgetWin_1   name=alert标题，所以加一遍进程名过滤
                    if (!string.IsNullOrEmpty(processName)
                        && SupportProcess.Any(p => processName.EqualIgnoreCase(p)))
                    {
                        return true;
                    }
                }
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

