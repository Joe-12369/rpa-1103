using II.RPA.Plugin.ZExplorer.V2.DownloaderBasic;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.DownloaderBasic;

namespace II.RPA.Plugin.ZExplorer.V2
{
    public class ZExplorerElement : PageElement
    {
        public ZExplorerElement(string userDefinedID, BrowserTabPage tabPage) : base(userDefinedID, tabPage)
        {

        }
        private ZExplorerDownloader zExplorerDownloader;
        public ZExplorerDownloader ZExplorerDownloader
        {
            get
            {
                if (zExplorerDownloader == null)
                {
                    zExplorerDownloader = new ZExplorerDownloader(this.TabPage);
                }
                return zExplorerDownloader;
            }
        }
    }
}
