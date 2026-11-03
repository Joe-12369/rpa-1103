using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.DownloaderBasic;
using II.RPA.WebBrowser.Common.Utils;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Permissions;

namespace II.RPA.Plugin.ZExplorer.V2.DownloaderBasic
{
    public class ZExplorerDownloader : ElementDownloader
    {
        public override eGuiType GuiType => eGuiType.ZExplorer;
        public override Browser Browser { get; }

        public ZExplorerDownloader(BrowserTabPage tabPage) : base(tabPage)
        {
            this.Browser = tabPage.Browser;
        }
        public override void AddCreatedEvent(ParamDownloaderModel param)
        {
            Browser.WebDataProvider.Send("download", new Newtonsoft.Json.Linq.JObject { { "action", "beforeDownload" }, { "filePath", param.saveFile }, { "fileName", param.fileName }, { "saveModel", (int)param?.saveMode } });
        }
        public override void RemoveCreatedEvent()
        {

        }
        public override string Downloader(string saveFile, string fileName, int interval, InputMode inputMode, SaveMode saveMode, int timeout, int waitDialogTime)
        {
            var downloadResponse = Browser.WebDataProvider.Send("download", new Newtonsoft.Json.Linq.JObject { { "action", "afterDownload" }, { "timout", timeout } }, timeout).ToString();
            if (string.IsNullOrWhiteSpace(downloadResponse))
            {
                throw new RPAException(this.GuiType, eErrorMessageType.Error_DownloadTimeOut);
            }
            var downloadInfo = JToken.Parse(downloadResponse);
            var filePath = downloadInfo.Value<string>("resultFilePath");
            return filePath;
        }
        public override string GetBrowserPreferencesPath()
        {
            string customUserDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
            var userName = Process.GetCurrentProcess().SessionId > 1 ? "z-desktop" : "default";
            return Path.Combine(customUserDataFolder, "zexplorer", userName, "EBWebView", "Default");
        }
    }
}
