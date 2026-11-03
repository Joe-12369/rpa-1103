using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Params
{
    public class ParamGetSiblingsElementsModel : ParamBase
    {
        public int timeout { get; set; } = 10000;
        /// <summary>
        /// 兄弟元素位置  1 下一个 2 上一个  int
        /// </summary>
        public int Position { get; set; } = 0;
        /// <summary>
        ///  1高级, 0普通
        /// </summary>
        public int UseAdvanced { get; set; } = 0;
    }
}
