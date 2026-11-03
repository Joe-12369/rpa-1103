using II.RPA.Plugin.Core;
using II.RPA.Plugin.Browsers.Web360X.DownloaderBasic;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.DownloaderBasic;
using II.RPA.WebBrowser.Common.Basic.Params;
using Microsoft.Extensions.Logging;
using System;

namespace II.RPA.Plugin.Browsers.Web360X
{
    internal class Web360XBrowserTabPage : BrowserTabPage
    {
        public Web360XBrowserTabPage(Browser browser, ParamCreateBrowserTabPageModel paramModel) : base(paramModel)
        {
            this.Browser = browser;
        }
        public override ElementDownloader Downloader => new Web360XDownloader(this);
        /// <summary>
        /// 创建元素对象
        /// </summary>
        /// <param name="userDefinedID">元素的自定义id</param>
        /// <returns>ElementGuiNode</returns>
        public override ElementGuiNode CreateElementGuiNode(string userDefinedID)
        {
            Web360XPageElement pageElement = new Web360XPageElement(userDefinedID, this);
            return new Web360XGuiNode(pageElement);
        }


    }
}
