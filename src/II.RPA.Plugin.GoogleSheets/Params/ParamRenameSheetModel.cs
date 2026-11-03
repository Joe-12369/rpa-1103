using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GoogleSheets.Params
{
    public class ParamRenameSheetModel : II.RPA.Plugin.Core.PluginBase.PluginParameterBase
    {
        public string url { get; set; }
        public string originalSheetName { get; set; }
        public string modifySheetName { get; set; }
    }
}
