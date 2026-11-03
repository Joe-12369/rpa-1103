using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Params
{
    public class ParamGetSubElementsModel : ParamBase
    {
        public int timeout { get; set; } = 10000;
        /// <summary>
        /// 第几个,-100所有（与engine约定）
        /// </summary>
        public string Num { get; set; } = string.Empty;
        /// <summary>
        ///  1高级, 0普通
        /// </summary>
        public int UseAdvanced { get; set; } = 0;
    }
}
