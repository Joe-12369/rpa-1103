using II.RPA.Plugin.GUI.Commons.WindowMatch;
using II.RPA.Plugin.GUI.Commons.WindowMatch.Entities;
using II.RPA.WebBrowser.Common.Basic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace II.RPA.WebBrowser.Common.MultiInstance
{
    //browser specification
    public class BrowserSpec
    {
        private BrowserSpec()
        {
        }
        public WindowType WindowType { get; set; }
        public MatchConfig MatchConfig { get; set; }
        public Type BrowserType { get; set; }
        public Type WebDataProviderType { get; set; }
        public Type GuiOperatorType { get; set; }
        public Type BrowserActionType { get; set; }
        public string[] NMHProcessNames { get; set; }

        public static BrowserSpec Create<
            TBrowser,
            TWebDataProvider,
            TGuiOperator,
            TBrowserAction>(string[] nmhProcessNames)
        where TBrowser : Browser
        where TWebDataProvider : WebDataProvider
        where TGuiOperator : GuiOperatorBase
        where TBrowserAction : BrowserActionBase
        {
            return new BrowserSpec
            {
                BrowserType = typeof(TBrowser),
                WebDataProviderType = typeof(TWebDataProvider),
                GuiOperatorType = typeof(TGuiOperator),
                BrowserActionType = typeof(TBrowserAction),
                NMHProcessNames = nmhProcessNames
            };
        }
    }
}
