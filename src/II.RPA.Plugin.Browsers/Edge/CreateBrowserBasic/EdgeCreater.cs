using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.CreateBrowserBasic;
using II.RPA.WebBrowser.Common.Basic.Params;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Browsers.Edge.CreateBrowserBasic
{
    public class EdgeCreater : BrowserTabCreaterBase
    {
        public override eGuiType GuiType => eGuiType.Edge;
        public override Browser Browser { get; }

        public EdgeCreater(Browser Browser)
        {
            this.Browser = Browser;
        }
    }
}
