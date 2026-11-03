using II.RPA.Plugin.Browsers.Chrome.DownloaderBasic;
using FlaUI.Core.AutomationElements;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.DownloaderBasic;
using II.RPA.WebBrowser.Common.Basic.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Browsers.Chrome
{
    internal class ChromeBrowserTabPage : BrowserTabPage
    {
        public ChromeBrowserTabPage(Browser browser, ParamCreateBrowserTabPageModel paramModel) : base(paramModel)
        {
            this.Browser = browser;
        }


        /// <summary>
        /// 创建元素对象
        /// </summary>
        /// <param name="userDefinedID">元素的自定义id</param>
        /// <returns>ElementGuiNode</returns>
        public override ElementGuiNode CreateElementGuiNode(string userDefinedID)
        {
            ChromePageElement pageElement = new ChromePageElement(userDefinedID, this);
            return new ChromeGuiNode(pageElement);
        }
        public override ElementDownloader Downloader => new ChromeDownloader(this);
        //public override void ActiveTab()
        //{
        //    base.ActiveTab();
        //    //多实例也需要重置、激活等
        //    if (this.Browser.ActiveTabCount == 1)
        //    {
        //        Task.Delay(500).Wait();
        //        //指定窗口无需重置
        //        if (!this.BrowserWindowInfo.SpecifiedWindow)
        //            this.SetWebWindowInfo(null);
        //        this.Switch();//避免 激活tab是同标题窗口，SetForeground不能准确命中
        //        this.GetTabDetail(true);
        //    }
        //}
    }
}
