using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Params
{
    public class ParamGetTableDataModel : ParamBase
    {
        public int scopeType { get; set; }
        /// <summary>
        /// 是否包含列头
        /// </summary>
        public int containHeader { get; set; }
        public string rowNo { get; set; }
        public string columnNo { get; set; }
        public int timeout { get; set; } = 10000;
    }
}
