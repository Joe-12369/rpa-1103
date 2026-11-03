using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.DownloaderBasic;
using II.RPA.WebBrowser.Common.Utils;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.IO;

namespace II.RPA.Plugin.Browsers.Edge.DownloaderBasic
{
    public class EdgeDownloader : ElementDownloader
    {
        public override eGuiType GuiType => eGuiType.Edge;
        public override Browser Browser { get; }
        public EdgeDownloader(BrowserTabPage tabPage) : base(tabPage)
        {
            this.Browser =tabPage.Browser;
        }
        public override IntPtr WaittingSaveDialog(int timeout = 20000)
        {
            DialogHelper.ConfirmDownload(this.Browser.BrowserProcessName, timeout);
            return base.WaittingSaveDialog(timeout);
        }
        public override IntPtr WaittingUploadSaveDialog(int timeout = 20000)
        {
            DialogHelper.ConfirmDownload(this.Browser.BrowserProcessName, timeout);
            return base.WaittingUploadSaveDialog(timeout);
        }
        public override string GetBrowserPreferencesPath()
        {
            var localApplication = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var chromePath = @"Microsoft\Edge\User Data\Default\Preferences";
            return Path.Combine(localApplication, chromePath);
        }
    }
}
