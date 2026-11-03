using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.DownloaderBasic;
using II.RPA.WebBrowser.Common.Basic.Params;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.WebBrowser.Common.Utils;

namespace II.RPA.Plugin.Browsers.Sogou.DownloaderBasic
{
    public class SogouDownloader : ElementDownloader
    {
        protected ILogger logger = Logger.OfContext();
        public override eGuiType GuiType => eGuiType.Sogou;
        public override Browser Browser { get; }
        public SogouDownloader(BrowserTabPage tabPage) : base(tabPage)
        {
            this.Browser = tabPage.Browser;
        }
        public override string Downloader(string saveFile, string fileName, int interval, InputMode inputMode, SaveMode saveMode, int timeout, int waitDialogTime)
        {
            var sogouPath = this.Browser.GetBrowserAppPath();
            if (FileHelper.IsHighVersionSogouExplorer(sogouPath))
            {
                throw new NotSupportedException();
            }
            return base.Downloader(saveFile, fileName, interval, inputMode, saveMode, timeout, waitDialogTime);
        }
        public override string DownloadDialog(string saveFile, string fileName, int interval, InputMode inputMode, SaveMode saveMode, int timeout, int waitDialogTime, int buttonType)
        {
            var sogouPath = this.Browser.GetBrowserAppPath();
            if (FileHelper.IsHighVersionSogouExplorer(sogouPath))
            {
                throw new NotSupportedException();
            }
            return base.DownloadDialog(saveFile, fileName, interval, inputMode, saveMode, timeout, waitDialogTime, buttonType);
        }
        public override string Save(SaveMode saveMode, string downloadPath, string path, int timeout)
        {
            downloadPath = downloadPath.Replace(".sgdownload", "");
            return base.Save(saveMode, downloadPath, path, timeout);
        }
        public override string FormattedFilePath(string saveFile, string fileName, string defaultDirectory)
        {
            defaultDirectory = defaultDirectory.Replace(".sgdownload", "");
            return base.FormattedFilePath(saveFile, fileName, defaultDirectory);
        }

        public override bool IsDownloadComplete(DownloadDataInfo dataInfo, string path)
        {
            var defaultDirectory = string.Empty;
            if (string.IsNullOrEmpty(dataInfo.Filename) && string.IsNullOrEmpty(path))
            {
                defaultDirectory = PathAlternativeOffer();
            }
            if (string.IsNullOrEmpty(path))
                path = string.IsNullOrEmpty(defaultDirectory) ? dataInfo.Filename : defaultDirectory;
            path = path.Replace(".sgdownload", "");
            if (dataInfo.FileSize == -1)
                return FileHelper.Exists(path) && FileHelper.FileSize(path) != 0;
            if (dataInfo.BytesReceived == 0)
                return FileHelper.Exists(path) && FileHelper.FileSize(path) == dataInfo.FileSize;
            else
                return FileHelper.Exists(path) && dataInfo.BytesReceived == dataInfo.FileSize;
        }

        public override string GetBrowserPreferencesPath()
        {
            var localApplication = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var chromePath = @"Sogou\SogouExplorer\User Data\Default\Preferences";
            return Path.Combine(localApplication, chromePath);
        }
    }
}
