using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Params
{
    public class ParamGetParentModel : ParamBase
    {
        public int timeout { get; set; } = 10000;
        /// <summary>
        /// 层级  int
        /// </summary>
        public string Level { get; set; } = string.Empty;
        /// <summary>
        ///  1高级, 0普通
        /// </summary>
        public int UseAdvanced { get; set; } = 0;
    }
}
