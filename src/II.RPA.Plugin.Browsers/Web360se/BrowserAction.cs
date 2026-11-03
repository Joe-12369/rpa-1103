using II.RPA.Plugin.Browsers.Web360se.CreateBrowserBasic;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.CreateBrowserBasic;
using II.RPA.WinAutomation.CommonUtility;

namespace II.RPA.Plugin.Browsers.Web360se
{
    public class BrowserAction : BrowserActionBase
    {
        public override eGuiType GuiType => eGuiType.Web360se;

        public override Browser Browser { get; }
        public override BrowserTabCreaterBase Creater { get; }
        public BrowserAction()
        {

        }
        public BrowserAction(Browser browser)
        {
            this.Browser = browser;
            this.Creater = new Web360seCreater(browser);
        }
    }
}
