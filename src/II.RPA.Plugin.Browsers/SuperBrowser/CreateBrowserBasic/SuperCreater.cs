using II.RPA.Logging;
using II.RPA.Plugin.Core.Commons;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.CreateBrowserBasic;
using II.RPA.WebBrowser.Common.Basic.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Browsers.SuperBrowser.CreateBrowserBasic
{
    public class SuperCreater : BrowserTabCreaterBase
    {
        private static ILogger logger = Logger.OfContext();
        public override eGuiType GuiType => eGuiType.SuperBrowser;
        public override Browser Browser { get; }
        public SuperCreater(Browser Browser)
        {
            this.Browser = Browser;
        }

        public override void CreateBrowserProcess(out int code)
        {
            code = 0;
            if (Browser.IsRunBrowser)
            {
                try
                {
                    if (Browser.CheckNativeMeaageHostIsStarted())
                    {
                        logger.LogInformation($"Detect alive process named with {Browser.BrowserProcessName};Host connection is avaliable!");
                        return;
                    }
                }
                catch
                {
                    logger.LogInformation($"Detect alive process named with {Browser.BrowserProcessName};Host connection is unavaliable!");
                }
            }
            logger.LogInformation($"There is no process named with {Browser.BrowserProcessName}");
            var resultPath = this.Browser.GetBrowserAppPath();
            WindowCreateProcess.CreateBrowser(resultPath, this.Browser.BrowserProcessName, string.Empty, this.CreateProcessArgs, this.Browser.IgorneCreateSetting, out code, Browser.NeedCareProfileOccupation);
        }
    }
}
