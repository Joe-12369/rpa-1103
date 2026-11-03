using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.WebBrowser.Common
{
    public class BrowserParams
    {
        public string windowId { get; set; }
        public string tabId { get; set; }
        public string browserType { get; set; }
        public string webData { get; set; }
        public int isBackend { get; set; }
        public string arguments { get; set; }
        /// <summary>
        /// 获取浏览器ActiveBase实例，默认超时时间20s,正常需使用组件超时最大时间
        /// </summary>
        public int timeout { get; set; } = 0;
    }
}
