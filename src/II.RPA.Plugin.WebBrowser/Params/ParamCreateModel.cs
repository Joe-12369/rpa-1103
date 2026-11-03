using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.WebBrowser.Params
{
    public class ParamCreateModel : ParamBase
    {
        public bool isRemote { get; set; }
        public string address { get; set; }
        public string url { get; set; }
        public string browserType { get; set; }
        public int timeout { get; set; }
        public string arguments { get; set; }
        //是否是后台启动，启动时最小化 add by wenyi 07/16/2021
        public int isBackend { get; set; }
        /// <summary>
        /// 等待网页加载，0:不等待，1:等待，默认1等待
        /// </summary>
        public int waitingload { get; set; } = 1;
        /// <summary>
        /// 打开新Tab 0:不打开新Tab，当前页跳转，1:打开新Tab
        /// </summary>
        public int openNewTab { get; set; } = 1;
    }
}
