using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileProvider.Params
{
    public class ParamAppInfoModel : ParamBase
    {
        public string sessionId { get; set; }
        public string packageName { get; set; }
        /// <summary>
        /// 打开前是否先关闭
        /// </summary>
        public bool isCloseBeforeOpen { get; set; }
    }
}
