using II.RPA.Plugin.Browsers.Web360se;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.CreateBrowserBasic;
using II.RPA.WebBrowser.Common.Basic.Params;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Browsers.Web360se.CreateBrowserBasic
{
    public class Web360seCreater : BrowserTabCreaterBase
    {
        public override eGuiType GuiType => eGuiType.Web360se;
        public override Browser Browser { get; }
        public Web360seCreater(Browser Browser)
        {
            this.Browser = Browser;
        }
    }
}
