using II.RPA.Plugin.Browsers.Chromium.CreateBrowserBasic;
using II.RPA.WebBrowser.Common;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.CreateBrowserBasic;
using II.RPA.WinAutomation.CommonUtility;

namespace II.RPA.Plugin.Browsers.Chromium
{
    public class ChromiumBrowserAction : BrowserActionBase
    {
        public override eGuiType GuiType => eGuiType.Chromium;
        public override Browser Browser { get; }
        public override BrowserTabCreaterBase Creater { get; }
        public ChromiumBrowserAction() : this(null)
        {

        }
        public ChromiumBrowserAction(Browser browser)
        {
            this.Browser = browser;
            this.Creater = new ChromiumCreater(browser);

        }
        public override void Close(bool isCloseAlert)
        {
            Wrapper(() =>
            {
                var allTabIds = this.Browser.GetAllTabIds();
                if (allTabIds.Contains(this.TabPage.TabId))
                {
                    this.TabPage.Close();
                    this.Browser.StopConnecttion();
                }
            });
        }
    }
}
