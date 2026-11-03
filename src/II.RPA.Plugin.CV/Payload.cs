using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.CV
{
   public class Payload
    {
        public string image { get; set; }
        public string app { get; set; }
    }
    public class PayloadV2
    {
        public string image { get; set; }
    }
    public class PayloadGetText
    {
        public string image { get; set; }
        public List<List<int>> points { get; set; }
    }
    public class OperationMatchingParams
    {
        /// <summary>
        /// base64编码的页面图片，为实际RPA流程执行过程中的页面图片
        /// </summary>
        public string image { get; set; }
        /// <summary>
        /// 流程编辑阶段用户选择的元素的返回结构体信息
        /// </summary>
        public CVItem element_obj { get; set; }
        /// <summary>
        /// 流程编辑阶段用户选择的元素对应的锚点的返回结构体信息
        /// </summary>
        public CVItem anchor_obj { get; set; }
    }
}
