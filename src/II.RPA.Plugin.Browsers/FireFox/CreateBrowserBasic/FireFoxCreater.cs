using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.CreateBrowserBasic;
using II.RPA.WebBrowser.Common.Basic.Params;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Browsers.FireFox.CreateBrowserBasic
{
    public class FireFoxCreater : BrowserTabCreaterBase
    {
        public override eGuiType GuiType => eGuiType.FireFox;
        public override Browser Browser { get; }
        public FireFoxCreater(Browser Browser)
        {
            this.Browser = Browser;
        }
    }
}
