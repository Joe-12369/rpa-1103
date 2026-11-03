using FlaUI.Core.AutomationElements;
using II.RPA.Plugin.Core;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.CreateBrowserBasic;
using II.RPA.WebBrowser.Common.Basic.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace II.RPA.Plugin.ZExplorer.V2.ZExplorerBrowserBasic
{
    public class ZExplorerCreater : BrowserTabCreaterBase
    {
        public override eGuiType GuiType => eGuiType.ZExplorer;
        public override Browser Browser { get; }
        public ZExplorerConnection ZExplorerConnection { get; }

        public ZExplorerCreater(Browser Browser)
        {
            this.Browser = Browser;
            ZExplorerConnection = (ZExplorerConnection)Browser.WebDataProvider.Connection;
        }



        public override BrowserTabPage BuilderBrowserTab(ParamCreateTabModel paramCreate)
        {
            if (!((ZExplorer)Browser).IsLatestVersion)
            {
                throw new ZExplorerException(eErrorMessageType.Error_ZEXPLORE_NEED_UPDATE);
            }
            var stopWatch = Stopwatch.StartNew();
            this.CreateProcessArgs = $" {paramCreate.url} {paramCreate.CreateProcessArgs} --new={paramCreate.IsOpenNewTab} --wait={paramCreate.IsWaitingload}";

            var psi = new ProcessStartInfo
            {
                FileName = Browser.GetBrowserAppPath(),
                Arguments = CreateProcessArgs,
                UseShellExecute = true,
                CreateNoWindow = false
            };
            var tmpId = GetCurrentUnixTimestamp();
            Process.Start(psi);
            logger.LogInformation($"tab create event:{paramCreate.url}");
            ZExplorerConnection.GetRuningZExplorerData(timeout: paramCreate.timeout);
            if (ZExplorerConnection.RuntimeData.Port < 1)
            {
                ZExplorerConnection.GetRuningZExplorerData("ZexplorerReady", timeout: paramCreate.timeout);
            }
            this.TabPage = this.Browser.CreateBrowserTabPage(new ParamCreateBrowserTabPageModel() { TabId = ZExplorerConnection.RuntimeData.TabId });
            stopWatch.Stop();
            logger.LogInformation($"create zexplorer time : {stopWatch.ElapsedMilliseconds}");
            return this.TabPage;
        }
        private int GetCurrentUnixTimestamp()
        {
            DateTime epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime now = DateTime.UtcNow;
            int unixTimestamp = (int)(now - epochStart).TotalSeconds;
            return unixTimestamp;
        }
    }
}
