using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.DownloaderBasic;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.IO;

namespace II.RPA.Plugin.Browsers.Chrome.DownloaderBasic
{
    public class ChromeDownloader : ElementDownloader
    {
        public override eGuiType GuiType => eGuiType.Chrome;
        public override Browser Browser { get; }
        public ChromeDownloader(BrowserTabPage tabPage) : base(tabPage)
        {
            Browser = tabPage.Browser;
        }

        public override string GetBrowserPreferencesPath()
        {
            var localApplication = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var chromePath = @"Google\Chrome\User Data\Default\Preferences";
            return Path.Combine(localApplication, chromePath);
        }
    }
}
