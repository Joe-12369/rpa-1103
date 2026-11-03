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
using System.Runtime.InteropServices;

namespace II.RPA.Plugin.Browsers.Sogou
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
        public override eGuiType GuiType => eGuiType.Sogou;
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
                logger.LogDebug($"Start IsCanUse  Sougou ");
                //logger.LogDebug("Start IsCanUse");
                bool isSogou = IsSogouBrowerApp(rootElement);
                if (isSogou)        //Google Sogou处理 
                {
                    var inspectBrowser = false;
                    rootElementChilds = rootElement.FindAllChildren();
                    //SogouExplorer v12
                    if (rootElement.Properties.ClassName.TryGetValue(out var rootClaName) && rootClaName.EqualIgnoreCase("SogouExplorer_WidgetWin_1"))
                    {
                        var chromElement = rootElementChilds.FirstOrDefault(e => e.Properties.ClassName.TryGetValue(out var eClaName) && eClaName == "Chrome_WidgetWin_1");
                        if (chromElement != null)
                        {
                            var isMultipleDocmentWnd = chromElement.FindAllChildren().Where(e => e.Properties.ClassName.IsSupported && e.ClassName.Contains("Chrome_RenderWidgetHostHWND")).Count() > 1;
                            inspectBrowser = !isMultipleDocmentWnd;
                        }
                    }
                    else
                    {
                        Array.Reverse(rootElementChilds);
                        inspectBrowser = rootElementChilds.Any(e =>
                        e.Properties.ClassName.IsSupported && e.ClassName.StartsWith("SE_Tuo") && e.BoundingRectangle.Contains(screenPoint));
                        if (inspectBrowser)
                        {
                            var child = UiCommon.TreeWalker.GetLastChild(pointElement);
                            while (child != null)
                            {
                                try
                                {
                                    if (child.Properties.Name.IsSupported && child.Name.StartsWith("打印"))
                                    {
                                        return false;
                                    }
                                    child = UiCommon.TreeWalker.GetPreviousSibling(child);
                                }
                                catch (Exception e)
                                {
                                    logger.LogError($"find element error：{e}");
                                    break;
                                }
                            }
                        }
                    }
                    if (inspectBrowser)
                    {
                        if (!IsExecutable)
                        {
                            Browser.CheckBrowserExtensions();
                        }
                        IsExecutable = true;
                        if (!sw_ChromeExtensionUnopen.IsRunning)
                        {
                            sw_ChromeExtensionUnopen.Reset();
                        }
                        return true;
                    }
                    return false;
                }
            }
            catch (RPAException ex)
            {
                if (ex.ErrorMessageType == eErrorMessageType.Error_ServerHubUnopen)
                {
                    //if (sw_ChromeExtensionUnopen.ElapsedMilliseconds >= 5000)
                    //{
                    throw ex;
                    //}
                }
                else if (ex.ErrorMessageType == eErrorMessageType.Error_SogouExtensionUnInstalled)
                {
                    throw ex;
                }
            }
            finally
            {
                logger.LogDebug($"End IsCanUse  Sougou ");
            }

            return false;
        }
        private bool IsSogouBrowerApp(AutomationElement rootElement)
        {
            var className = rootElement.ClassName;
            if (className.StartsWith("SE_SogouExplorer") || className.EqualIgnoreCase("SogouExplorer_WidgetWin_1"))        //Google Sogou处理 
            {
                //logger.LogDebug("Is Sogou_WidgetWin_1");
                if (rootElement.Properties.NativeWindowHandle.IsSupported)
                {
                    var hWnd = rootElement.Properties.NativeWindowHandle.Value;
                    string processName = UiCommon.GetProcessNameByCache(hWnd);
                    if (!string.IsNullOrEmpty(processName) && processName.Contains("飞鸽客服工作台.exe"))
                    {
                        return false;
                    }
                    return true;
                }
            }
            return false;
        }
        protected override bool SpecialScenariosInspectNode(Point screenPoint, AutomationElement rootElement, AutomationElement pointElement, string selector, out IGuiNode inspectNode)
        {
            inspectNode = null;
            var child = UiCommon.TreeWalker.GetLastChild(pointElement);
            logger.LogDebug($"SpecialScenariosInspectNode alert dialog 耗时：{RunContext.TimeInertval}");
            while (child != null)
            {
                try
                {
                    if (child.Properties.ControlType.IsSupported
                      && child.ControlType == ControlType.Group
                      && child.Patterns.LegacyIAccessible.IsSupported
                      && child.Patterns.LegacyIAccessible.Pattern?.Role == AccessibilityRole.ROLE_SYSTEM_GROUPING
                      || child.Patterns.LegacyIAccessible.Pattern.Role == AccessibilityRole.ROLE_SYSTEM_DIALOG)

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

        #endregion


    }
}
