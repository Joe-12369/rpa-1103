using II.RPA.Plugin.GUI.Commons.WindowMatch.Entities;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.CreateBrowserBasic;
using II.RPA.WebBrowser.Common.Basic.Params;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Browsers.Chromium.CreateBrowserBasic
{
    public class ChromiumCreater : BrowserTabCreaterBase
    {
        public override eGuiType GuiType => eGuiType.Chromium;
        public override Browser Browser { get; }
        public MatchConfig BrowserMatchConfig =>((ChromiumBrowser)this.Browser).BrowserMatchConfig;
        
        public ChromiumCreater(Browser Browser)
        {
            this.Browser = Browser;
        }
        public override bool VerifyBrowserMainWindowHandle()
        {
            return BrowserWindowProvider.GetFirstBrowserByZOrder((wndInfo) => wndInfo.HitMatchHandler.MatchConfig.ConfigKey == this.BrowserMatchConfig.ConfigKey,
                                                                 this.GuiType) != null;
        }
    }
}
