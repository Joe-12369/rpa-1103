using II.RPA.Plugin.Core;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;


namespace II.RPA.Plugin.ZExplorer.V2
{
    public class ZExplorerWebDataProvider : WebDataProvider
    {
        private readonly static Lazy<ZExplorerWebDataProvider> _instance = new Lazy<ZExplorerWebDataProvider>(() => new ZExplorerWebDataProvider(new ConnectionConfig { GuiType = eGuiType.ZExplorer }));
        public static ZExplorerWebDataProvider Instance { get { return _instance.Value; } }
        public ZExplorerWebDataProvider(ConnectionConfig connConfig) : base(connConfig)
        {
            this.Connection = ZExplorerConnection.Instance;
        }
        //聚水潭 多个iframe 嵌套会偶尔超时，增加点超时时间
        public override JToken ElementFormPoint(int tabId, Point point)
        {
            return this.Send("tabs.elementFromPoint",
                 new JObject {
                    { "tabId", tabId },
                    { "x", point.X * NativeMethods.ScaleX },
                    { "y", point.Y * NativeMethods.ScaleY },
                    { "ApiVersion","v2" }
              }, 3000);
        }
    }
}
