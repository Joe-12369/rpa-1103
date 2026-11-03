using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.EnumWindow;
using II.RPA.Plugin.Browsers.Edge.DownloaderBasic;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.DownloaderBasic;
using II.RPA.WebBrowser.Common.Basic.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;

namespace II.RPA.Plugin.Browsers.Edge
{
    internal class EdgeBrowserTabPage : BrowserTabPage
    {
        public EdgeBrowserTabPage(Browser browser, ParamCreateBrowserTabPageModel paramModel) : base(paramModel)
        {
            this.Browser = browser;
        }
        public override ElementDownloader Downloader => new EdgeDownloader(this);
        /// <summary>
        /// 创建元素对象
        /// </summary>
        /// <param name="userDefinedID">元素的自定义id</param>
        /// <returns>ElementGuiNode</returns>
        public override ElementGuiNode CreateElementGuiNode(string userDefinedID)
        {
            EdgePageElement pageElement = new EdgePageElement(userDefinedID, this);
            return new EdgeGuiNode(pageElement);
        }
    }
}
