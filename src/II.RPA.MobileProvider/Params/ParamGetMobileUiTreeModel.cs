using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileProvider.Params
{
    public class ParamGetMobileUiTreeModel
    {
        /// <summary>
        /// 连接ID
        /// </summary>
        public string sessionId { get; set; }
        /// <summary>
        /// 是否保存为文件输出
        /// </summary>
        public bool iSSaveAsFile { get; set; }
        /// <summary>
        /// 保存路径
        /// </summary>
        public string savePath { get; set; }
        /// <summary>
        /// 文件名
        /// </summary>
        public string fileName { get; set; }
        /// <summary>
        /// 是否覆盖同名文件
        /// </summary>
        public bool iSCoverSameFile { get; set; }
        public int timeout { get; set; }
    }
}
