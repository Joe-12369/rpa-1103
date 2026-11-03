using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.CreateBrowserBasic;
using II.RPA.WebBrowser.Common.Basic.Params;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Browsers.Sogou.CreateBrowserBasic
{
    public class SogouCreater : BrowserTabCreaterBase
    {
        public override eGuiType GuiType => eGuiType.Sogou;
        public override Browser Browser { get; }
        public SogouCreater(Browser Browser)
        {
            this.Browser = Browser;
        }
    }
}
