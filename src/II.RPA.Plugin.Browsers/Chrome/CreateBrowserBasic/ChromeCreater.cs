using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.CreateBrowserBasic;
using II.RPA.WebBrowser.Common.Basic.Params;
using II.RPA.WinAutomation.CommonUtility;
using System.Linq;

namespace II.RPA.Plugin.Browsers.Chrome.CreateBrowserBasic
{
    public class ChromeCreater : BrowserTabCreaterBase
    {
        public override eGuiType GuiType => eGuiType.Chrome;
        public override Browser Browser { get; }
        public ChromeCreater(Browser Browser)
        {
            this.Browser = Browser;
        }
    }
}
