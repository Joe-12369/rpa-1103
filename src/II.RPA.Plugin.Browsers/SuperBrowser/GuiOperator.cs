using FlaUI.Core.AutomationElements;
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
using System.Drawing;
using System.Linq;

namespace II.RPA.Plugin.Browsers.SuperBrowser
{

    public class GuiOperator : GuiOperatorBase
    {
        #region 公共使用的全局变量
        private AutomationElement[] rootElementChilds = null;
        private static ILogger logger = Logger.OfContext();
        /// <summary>
        /// 支持的进程名，忽略大小写，非全程，判断包含的方式，可只配置特征字符串
        /// </summary>
        private readonly string[] SupportProcess = { "superbrowser.exe" };
        #endregion
        public override eGuiType GuiType => eGuiType.SuperBrowser;
        protected override Browser Browser { get; }
        public GuiOperator() : this(null)
        {
        }
        public GuiOperator(Browser browser)
        {
            this.Browser = browser;
        }

        #region 拾取相关

        public override bool IsCanUse(Point screenPoint, AutomationElement rootElement, AutomationElement pointElement)
        {
            try
            {
                logger.LogDebug($"Start IsCanUse  SuperBrowser 耗时：{RunContext.TimeInertval}");
                bool isChrome = IsChromeBrowerApp(rootElement);
                if (isChrome)        //Google Chrome处理 
                {
                    logger.LogDebug($"Start Find Chrome_RenderWidgetHostHWND  耗时：{RunContext.TimeInertval}");
                    rootElementChilds = rootElement.FindAllChildren();
                    var inspectBrowser = rootElementChilds.Any(e =>
                    e.Properties.ClassName.IsSupported && e.ClassName.Contains("Chrome_RenderWidgetHostHWND") && e.BoundingRectangle.Contains(screenPoint));
                    if (inspectBrowser)
                    {
                        logger.LogDebug($"Found Chrome_RenderWidgetHostHWND  耗时：{RunContext.TimeInertval}");
                        if (!IsExecutable)
                        {
                            Browser.CheckBrowserExtensions();
                        }
                        IsExecutable = true;
                        logger.LogDebug($" ChromeNode.IsExecution  耗时：{RunContext.TimeInertval}");
                        if (!sw_ChromeExtensionUnopen.IsRunning)
                        {
                            sw_ChromeExtensionUnopen.Reset();
                        }

                        var isMultipleDocmentWnd = rootElementChilds.Where(e => e.Properties.ClassName.IsSupported && e.ClassName.Contains("Chrome_RenderWidgetHostHWND")).Count() > 1;
                        if (isMultipleDocmentWnd)
                        {
                            var isDevTools = pointElement.Properties.Name.IsSupported && pointElement.Properties.Name.Equals("DevTools");
                            if (isDevTools)
                            {
                                var devToolsChildren = pointElement.FindAllChildren();
                                foreach (var devToolsChild in devToolsChildren)
                                {
                                    if (devToolsChild.Properties.ControlType.TryGetValue(out var controlType)
                                        && controlType == ControlType.Group
                                        && devToolsChild.Patterns.LegacyIAccessible.TryGetPattern(out var cPattern)
                                        && cPattern.Role.TryGetValue(out var cRole)
                                        && cRole == AccessibilityRole.ROLE_SYSTEM_GROUPING
                                        && devToolsChild.Properties.BoundingRectangle.TryGetValue(out var boundingRect)
                                        && boundingRect.Contains(screenPoint)
                                        )
                                    {
                                        return false;
                                    }
                                }
                            }
                            var isPrintWnd = pointElement.Patterns.LegacyIAccessible.IsSupported
                                && pointElement.Patterns.LegacyIAccessible.TryGetPattern(out var pattern)
                                && pattern.Value.TryGetValue(out var value)
                                && value.Contains("chrome://print");
                            if (isPrintWnd)
                            {
                                return false;
                            }
                        }
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
                logger.LogDebug($"End IsCanUse  Chrome ");
            }
            return false;
        }

        private bool IsChromeBrowerApp(AutomationElement rootElement)
        {
            //NLogger.Default.Trace($" rootElement className:{className},Name:{rootElement.Name}");
            if (rootElement.Properties.ClassName.TryGetValue(out var className) && className.Contains("Chrome_WidgetWin_1"))        //Google Chrome处理 
            {
                rootElement.Properties.NativeWindowHandle.TryGetValue(out var hWnd);
                string processName = UiCommon.GetProcessNameByCache(hWnd);
                if (processName.ToLower() == "superbrowser.exe" || processName.ToLower() == "superbrowser")
                {
                    try
                    {
                        var curActiveTab = this.Browser.AcquireCurrentActiveTab();
                        return curActiveTab.TabId != -1;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
                if (!string.IsNullOrEmpty(processName)
                       && SupportProcess.Any(p => processName.EqualIgnoreCase(p)))
                {
                    return true;
                }
            }
            return false;
        }

        public override bool InspectNode(Point screenPoint, AutomationElement rootElement, AutomationElement pointElement, string selector, out IGuiNode inspectNode)
        {
            logger.LogDebug($"start superbrowser InspectNode   耗时：{RunContext.TimeInertval}");
            inspectNode = null;
            bool inspectResult = false;
            //logger.LogDebug($"rootElement.FindAllChildren()   耗时：{RunContext.TimeInertval}");

            var child = UiCommon.TreeWalker.GetLastChild(rootElement);
            logger.LogDebug($"Find alert dialog Start   耗时：{RunContext.TimeInertval}");

            while (child != null)
            {
                try
                {
                    if (child.Patterns.LegacyIAccessible.IsSupported
                        && child.Patterns.LegacyIAccessible.Pattern.Role.TryGetValue(out var role) && role == AccessibilityRole.ROLE_SYSTEM_DIALOG)
                    {
                        return InspectWindowsNode(screenPoint, child, out inspectNode);

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
            inspectResult = base.InspectNode(screenPoint, rootElement, pointElement, selector, out inspectNode);
            //var curActiveTab = this.Browser.AcquireCurrentActiveTab();
            //inspectResult = curActiveTab.InspectNode(rootElement.Properties.NativeWindowHandle, screenPoint, selector, out inspectNode);
            //logger.LogDebug($"SuperBrowserNode.InspectNod（**)   耗时：{RunContext.TimeInertval}");
            //if (!inspectResult)
            //{
            //    this.Release();
            //}
            return inspectResult;
        }

        #endregion



    }
}
