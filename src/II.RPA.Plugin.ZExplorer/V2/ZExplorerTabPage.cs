using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.WindowMatch;
using II.RPA.Plugin.ZExplorer.V2.DownloaderBasic;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.DownloaderBasic;
using II.RPA.WebBrowser.Common.Basic.Params;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace II.RPA.Plugin.ZExplorer.V2
{
    internal class ZExplorerTabPage : BrowserTabPage
    {
        public ZExplorerTabPage(Browser browser, ParamCreateBrowserTabPageModel paramModel) : base(paramModel)
        {
            this.Browser = browser;
        }
        public override ElementDownloader Downloader => new ZExplorerDownloader(this);
        /// <summary>
        /// 创建元素对象
        /// </summary>
        /// <param name="userDefinedID">元素的自定义id</param>
        /// <returns>ElementGuiNode</returns>
        public override ElementGuiNode CreateElementGuiNode(string userDefinedID)
        {
            ZExplorerElement pageElement = new ZExplorerElement(userDefinedID, this);
            return new ZExplorerGuiNode(pageElement);
        }

    }
}
