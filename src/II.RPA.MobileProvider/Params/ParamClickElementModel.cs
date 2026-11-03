using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileProvider.Params
{
    public class ParamClickElementModel : MobileElementValidationOptions
    {
        public string sessionId { get; set; }
        public string selector { get; set; }
        ///参见EumClickType
        public int clickType { get; set; }
        public int offsetX { get; set; }
        public int offsetY { get; set; }
        /// <summary>
        /// /*- label: "中心"
        ///    value: '0'
        ///  - label: "左上"
        ///    value: '1'
        ///  - label: "右上"
        ///    value: '2'
        ///  - label: "左下"
        ///    value: '3'
        ///  - label: "右下"
        ///    value: '4'*/
        /// </summary>
        public int cursorPos { get; set; }
        public int timeout { get; set; }
    }
}
