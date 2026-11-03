using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GoogleSheets.Params
{
    public class ParamAddFilterViewModel : II.RPA.Plugin.Core.PluginBase.PluginParameterBase
    {
        public string url { get; set; }
        public string sheetName { get; set; }
        public string filterName { get; set; }
        public string startPostion { get; set; }
        public string endPostion { get; set; }
        public string Condition { get; set; }//[["1","NUMBER_EQ","abc"],["2","NUMBER_EQ","abc"]] //第一列和第二列的刷选condition
        public string filterViewId { get; set; }//创建的filterViewId

    }
}
