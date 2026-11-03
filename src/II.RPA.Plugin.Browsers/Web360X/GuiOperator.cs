using FlaUI.Core.AutomationElements;
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

namespace II.RPA.Plugin.Browsers.Web360X
{

    public class GuiOperator : GuiOperatorBase
    {
        #region 公共使用的全局变量
        private AutomationElement[] rootElementChilds = null;
        private static ILogger logger = Logger.OfContext();
        /// <summary>
        /// 支持的进程名，忽略大小写，非全程，判断包含的方式，可只配置特征字符串
        /// </summary>
        private readonly string[] SupportProcess = { "360ChromeX.exe", "360chrome.exe" };
        #endregion
        public override eGuiType GuiType => eGuiType.Web360X;
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
                if (!rootElement.Properties.ClassName.TryGetValue(out var className) || !className.Contains("Chrome_WidgetWin_1"))        //Google Chrome处理 
                {
                    return false;
                }
                rootElement.Properties.NativeWindowHandle.TryGetValue(out var hWnd);
                string processName = UiCommon.GetProcessNameByCache(hWnd);
                if (!rootElement.Properties.Name.TryGetValue(out var name) || !name.Contains(" - 360极速浏览器"))
                {
                    return false;
                }
                if (pointElement.Properties.ClassName.TryGetValue(out var claName) && claName.Equals("Chrome_WidgetWin_2"))
                {
                    return false;
                }
                if (!IsExecutable)
                {
                    Browser.CheckBrowserExtensions();
                }
                IsExecutable = true;
                if (!sw_ChromeExtensionUnopen.IsRunning)
                {
                    sw_ChromeExtensionUnopen.Reset();
                }
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
                    child = UiCommon.TreeWalker.GetNextSibling(child);
                }
                return true;
            }
            catch (RPAException ex)
            {
                if (ex.ErrorMessageType == eErrorMessageType.Error_360XExtensionUnopen)
                {
                    if (sw_ChromeExtensionUnopen.ElapsedMilliseconds >= 5000)
                    {
                        throw ex;
                    }
                }
                else if (ex.ErrorMessageType == eErrorMessageType.Error_360XExtensionUnInstalled)
                {
                    throw ex;
                }
            }
            return false;
        }
        public override bool InspectNode(Point screenPoint, AutomationElement rootElement, AutomationElement pointElement, string selector, out IGuiNode inspectNode)
        {
            logger.LogDebug($"start {GuiType.ToString()} InspectNode   耗时：{RunContext.TimeInertval}");
            var inspectResult = base.InspectNode(screenPoint, rootElement, pointElement, selector, out inspectNode);
            logger.LogDebug($"stop {GuiType.ToString()} InspectNode   耗时：{RunContext.TimeInertval}");
            return inspectResult;
        }

        #endregion


    }
}
