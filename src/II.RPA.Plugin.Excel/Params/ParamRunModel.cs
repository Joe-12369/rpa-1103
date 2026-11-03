using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Excel.Params
{
    public class ParamRunModel : II.RPA.Plugin.Core.PluginBase.PluginParameterBase
    {
        public string Method { get; set; }
        public string Paras { get; set; }
        //是否是单个组件运行 add by wenyi: ZOCO-23125
        public bool? SingleRun { get; set; }
    }
}
