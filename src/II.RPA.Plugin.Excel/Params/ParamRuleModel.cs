using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Excel.Params
{

    public class Componentmodal
    {
        public string type { get; set; }
        public string argName { get; set; }
    }
    public class ParamRuleModel : II.RPA.Plugin.Core.PluginBase.PluginParameterBase
    {
        public Componentmodal componentModal { get; set; }

        #region 数据可视化
        public string ActionType { get; set; }
        public string filePath { get; set; }
        public int Software { get; set; }
        #endregion

        public string componentId
        {
            get
            {
                return componentModal?.type?.Replace("Excel_Cs_", "");

            }
        }        
    }
}
