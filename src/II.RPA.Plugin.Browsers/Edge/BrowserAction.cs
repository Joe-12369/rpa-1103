using II.RPA.Plugin.Browsers.Edge.CreateBrowserBasic;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.CreateBrowserBasic;
using II.RPA.WinAutomation.CommonUtility;

namespace II.RPA.Plugin.Browsers.Edge
{
    public class BrowserAction : BrowserActionBase
    {
        public override eGuiType GuiType => eGuiType.Edge;

        public override Browser Browser { get; }
        public override BrowserTabCreaterBase Creater { get; }
        public BrowserAction()
        {

        }
        public BrowserAction(Browser browser)
        {
            this.Browser = browser;
            this.Creater = new EdgeCreater(browser);
        }
    }
}
