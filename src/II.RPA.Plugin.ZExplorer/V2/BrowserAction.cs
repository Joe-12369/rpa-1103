using II.RPA.Logging;
using II.RPA.Plugin.ZExplorer.V2.ZExplorerBrowserBasic;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.CreateBrowserBasic;
using II.RPA.WebBrowser.Common.Basic.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Threading;

namespace II.RPA.Plugin.ZExplorer.V2
{
    public class BrowserAction : BrowserActionBase
    {
        public override eGuiType GuiType => eGuiType.ZExplorer;

        public override Browser Browser { get; }
        public override BrowserTabCreaterBase Creater { get; }
        public BrowserAction()
        {

        }
        public BrowserAction(Browser browser)
        {
            this.Browser = browser;
            this.Creater = new ZExplorerCreater(browser);
        }
    }
}
