using FlaUI.Core.AutomationElements;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.ZExplorer.Config;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;

namespace II.RPA.Plugin.ZExplorer.V2
{
    public class GuiOperator : GuiOperatorBase
    {

        public override eGuiType GuiType => eGuiType.ZExplorer;
        private ILogger logger = Logger.OfContext();

        protected override Browser Browser { get; }
        public GuiOperator() : this(null)
        {
        }
        public GuiOperator(Browser browser)
        {
            this.Browser = browser;
        }

        public override bool IsCanUse(Point screenPoint, AutomationElement rootElement, AutomationElement pointElement)
        {
            try
            {
                logger.LogDebug($"Start IsCanUse  ZExplorer ");
                var proc_info = rootElement.GetProcessInfo();
                if (proc_info.ProcessName.Equals("zexplore"))
                {
                    var includeApp = ZExplorerConfigurations.Info.GetSetting("实在浏览器");
                    if (includeApp != null && includeApp.enable && pointElement.ClassName.Equals(includeApp.className))
                    {                      
                        return true;
                    }
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                logger.LogDebug($"End IsCanUse  ZExplorer ");
            }
        }
        public override bool InspectNode(Point screenPoint, AutomationElement rootElement, AutomationElement pointElement, string selector, out IGuiNode inspectNode)
        {
            logger.LogDebug($"start {GuiType.ToString()} InspectNode   耗时：{RunContext.TimeInertval}");
            var inspectResult = base.InspectNode(screenPoint, rootElement, pointElement, selector, out inspectNode);
            logger.LogDebug($"stop {GuiType.ToString()} InspectNode   耗时：{RunContext.TimeInertval}");
            return inspectResult;
        }

        public override void Release()
        {
            base.Release();
        }

    }
}
