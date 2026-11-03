using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.WebBrowser.Params
{
    public class ParamGetCookiesModel : ParamBase
    {
        /// <summary>
        /// auto 默认链接
        /// assignment 指定链接
        /// </summary>
        public string cookieModel { get; set; }
        public string url { get; set; }
        public string name { get; set; }
    }
}
