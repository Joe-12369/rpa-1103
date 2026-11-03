using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.Browsers.SuperBrowser.DownloaderBasic;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.DownloaderBasic;
using II.RPA.WebBrowser.Common.Basic.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace II.RPA.Plugin.Browsers.SuperBrowser
{
    internal class SuperBrowserTabPage : BrowserTabPage
    {
        public SuperBrowserTabPage(Browser browser, ParamCreateBrowserTabPageModel paramModel) : base(paramModel)
        {
            this.Browser = browser;
        }
        public override ElementDownloader Downloader => new SuperBrowserDownloader(this);
        /// <summary>
        /// 创建元素对象
        /// </summary>
        /// <param name="userDefinedID">元素的自定义id</param>
        /// <returns>ElementGuiNode</returns>
        public override ElementGuiNode CreateElementGuiNode(string userDefinedID)
        {
            SuperBrowserPageElement pageElement = new SuperBrowserPageElement(userDefinedID, this);
            return new SuperBrowserGuiNode(pageElement);
        }

        
    }
}
