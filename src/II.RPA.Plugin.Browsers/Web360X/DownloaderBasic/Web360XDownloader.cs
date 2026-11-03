using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.DownloaderBasic;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.IO;

namespace II.RPA.Plugin.Browsers.Web360X.DownloaderBasic
{
    public class Web360XDownloader : ElementDownloader
    {
        public override eGuiType GuiType => eGuiType.Web360X;
        public override Browser Browser { get; }
        //public Web360XDownloader(PageElement pageElement) : base(pageElement)
        //{
        //}
        public Web360XDownloader(BrowserTabPage tabPage) : base(tabPage)
        {
            this.Browser = tabPage.Browser;
        }

        public override string GetBrowserPreferencesPath()
        {
            var localApplication = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var chromePath = @"360ChromeX\Chrome\User Data\Default\Preferences";
            var path = Path.Combine(localApplication, chromePath);
            if (!File.Exists(path))
            {
                localApplication = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                path = Path.Combine(localApplication, chromePath);
            }
            return path;
        }
    }
}
