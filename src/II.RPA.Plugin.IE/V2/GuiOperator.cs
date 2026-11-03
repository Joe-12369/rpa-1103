using FlaUI.Core.AutomationElements;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.IE.V2;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using V2Browser = II.RPA.WebBrowser.Common.Basic;

namespace II.RPA.Plugin.IE
{
    public class GuiOperator : GuiOperatorBase
    {
        #region 公共使用的全局变量
        private AutomationElement[] rootElementChilds = null;
        private static ILogger logger = Logger.OfContext();
        /// <summary>
        /// 支持的进程名，忽略大小写，非全程，判断包含的方式，可只配置特征字符串
        /// </summary>
        #endregion
        public override eGuiType GuiType => eGuiType.IE;
        protected override Browser Browser { get; }
        public GuiOperator() : this(null)
        {
        }
        public GuiOperator(Browser browser)
        {
            this.Browser = browser;
        }
        public override void Release()
        {
            base.Release();
            //增加拾取后释放IE com对象的方法（IEAutomation）
            ((V2.IEBrowser)this.Browser).ClearIHtmlObject();
        }

        #region 拾取相关
        public override bool IsCanUse(Point screenPoint, AutomationElement rootElement, AutomationElement pointElement)
        {
            StringBuilder lpString = new StringBuilder(256);
            var handle = pointElement.Properties.NativeWindowHandle.Value;
            //var handle = NativeMethods.WindowFromPoint(screenPoint);
            try
            {
                logger.LogDebug("Start IE IsCanUse");
                NativeMethods.GetClassName(handle, lpString, lpString.Capacity);
                if (lpString.ToString() == "Internet Explorer_Server")
                {
                    return true;
                }
            }
            finally
            {
                logger.LogInformation($"End IE IsCanUse:{lpString.ToString()}-{handle.ToString("x")}");
            }

            return false;
        }

        public override bool InspectNode(Point screenPoint, AutomationElement rootElement, AutomationElement pointElement, string selector, out IGuiNode inspectNode)
        {
            //如果遇到不能正常拾取的内嵌IE ，使用这一行代码获取拾取tab对象
            //var curActiveTab = ((IEBrowser) this.Browser).AcquireCurrentActiveTab(new List<IntPtr>() { rootElement.Properties.NativeWindowHandle });
            var curActiveTab = this.Browser.CreateBrowserTabPage(new V2Browser.Params.ParamCreateBrowserTabPageModel() { TabId = -1 });
            var inspectResult = curActiveTab.InspectNode(rootElement.Properties.NativeWindowHandle, screenPoint, selector, out inspectNode);
            if (!inspectResult)
            {
                this.Release();
            }
            return inspectResult;
        }
        #endregion 
    }
}
