using II.RPA.Plugin.Core;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.DownloaderBasic;
using II.RPA.WebBrowser.Common.Utils;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Browsers.FireFox.DownloaderBasic
{
    public class FireFoxDownloader : ElementDownloader
    {
        public override eGuiType GuiType => eGuiType.FireFox;
        public override Browser Browser { get; }
        public FireFoxDownloader(BrowserTabPage tabPage) : base(tabPage)
        {
           this.Browser= tabPage.Browser;
        }
        private string GetFile(string path, string fileName)
        {
            DirectoryInfo hdDirectoryInWhichToSearch = new DirectoryInfo(path);
            var filesInDir = hdDirectoryInWhichToSearch.EnumerateFiles(fileName, SearchOption.AllDirectories);

            foreach (FileInfo foundFile in filesInDir)
            {
                if (foundFile.FullName.Contains(".default"))
                    return foundFile.FullName;
            }
            return string.Empty;
        }
        public override string GetBrowserPreferencesPath()
        {
            var localApplication = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var chromePath = @"Mozilla\Firefox\Profiles";
            var path = Path.Combine(localApplication, chromePath);
            return GetFile(path, "prefs.js");
        }
        public override bool IsTurnOnAskSaveFile()
        {
            var _chromeSettingPath = GetBrowserPreferencesPath();
            string value = string.Empty;
            string content = string.Empty;
            if (!File.Exists(_chromeSettingPath))
            {
                return false;
            }
            using (FileStream fs = new FileStream(_chromeSettingPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (StreamReader streamReader = new StreamReader(fs))
            {
                content = streamReader.ReadToEnd();
                return content.Contains(@"browser.download.useDownloadDir");
            }
        }
    }
}
