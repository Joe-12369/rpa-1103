using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.DownloaderBasic;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Browsers.SuperBrowser.DownloaderBasic
{
    public class SuperBrowserDownloader : ElementDownloader
    {
        public override eGuiType GuiType => eGuiType.SuperBrowser;
        public override Browser Browser { get; }
        public SuperBrowserDownloader(BrowserTabPage tabPage) : base(tabPage)
        {
            this.Browser = tabPage.Browser;
        }
        public override bool IsTurnOnAskSaveFile()
        { 
            return true;
        }
        public override string GetBrowserPreferencesPath()
        {
            var localApplication = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var chromePath = @"SuperBrowser\User Data\BrowserWorkbench_1\Default\Preferences";
            return Path.Combine(localApplication, chromePath);
        }
    }
}
