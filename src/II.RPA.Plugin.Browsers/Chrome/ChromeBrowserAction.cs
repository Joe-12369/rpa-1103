using II.RPA.Plugin.Chrome.BrowserProfile;
using II.RPA.Plugin.Browsers.Chrome.CreateBrowserBasic;
using II.RPA.Plugin.Core;
using II.RPA.WebBrowser.Common;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.CreateBrowserBasic;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace II.RPA.Plugin.Browsers.Chrome
{
    public class ChromeBrowserAction : BrowserActionBase
    {
        public override eGuiType GuiType => eGuiType.Chrome;
        public override Browser Browser { get; }
        public override BrowserTabCreaterBase Creater { get; }
        public ChromeBrowserAction()
        {

        }
        public ChromeBrowserAction(Browser browser)
        {
            this.Browser = browser;
            this.Creater = new ChromeCreater(browser);
        }
    }
}
