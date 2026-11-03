using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.Browsers.Web360X;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.CreateBrowserBasic;
using II.RPA.WebBrowser.Common.Basic.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace II.RPA.Plugin.Browsers.Web360X.CreateBrowserBasic
{
    public class Web360XCreater : BrowserTabCreaterBase
    {
        public override eGuiType GuiType => eGuiType.Web360X;
        public override Browser Browser { get; }
        public Web360XCreater(Browser Browser)
        {
            this.Browser = Browser;
        }
        private bool FindWebBrowserProcess()
        {
            foreach (var processName in this.Browser.BrowserProcessNames)
            {
                if (RPAUtils.GetProcessesBySessionId(processName).Length > 0)
                    return true;
            }
            return false;
        }
        public override bool WaitBrowserProcess(ParamCreateTabModel paramCreate, out int code)
        {
            code = 0;
            if (!RPAUtils.Retry(() => FindWebBrowserProcess(), paramCreate.RunParam.Timeout / 2))
            {
                logger.LogDebug($"再次打开浏览器");
                this.CreateBrowserProcess(out code);
                logger.LogDebug($"再次打开浏览器结束");
            }
            return RPAUtils.Retry(() => VerifyBrowserMainWindowHandle(), paramCreate.RunParam.Timeout);
        }
    }
}
