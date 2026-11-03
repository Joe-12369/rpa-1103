using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GoogleSheets.Params
{
    public class ParamWriteToSheetRegionModel : II.RPA.Plugin.Core.PluginBase.PluginParameterBase
    {
        public string url { get; set; }
        public string dimension { get; set; }//rows行、columns列、region区域
        public string startPosition { get; set; } 
        public string endPosition { get; set; }
        public string sheetName { get; set; }
        public string Values { get; set; }//[[1,2,3],[1,2,3]]
        public int isFormula { get; set; } = 0;//是否公式
    }
}
