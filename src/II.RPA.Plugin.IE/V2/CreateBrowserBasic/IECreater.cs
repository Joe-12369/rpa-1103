using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.CreateBrowserBasic;
using II.RPA.WinAutomation.CommonUtility;
using System.Collections.Generic;

namespace II.RPA.Plugin.IE.V2.CreateBrowserBasic
{
    public class IECreater : BrowserTabCreaterBase
    {
        public override eGuiType GuiType => eGuiType.IE;
        public override Browser Browser { get; }
        public IECreater(Browser Browser)
        {
            this.Browser = Browser;
        }
    }
}
