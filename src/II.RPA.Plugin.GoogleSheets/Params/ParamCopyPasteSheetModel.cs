using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GoogleSheets.Params
{
    public class ParamCopyPasteSheetModel : II.RPA.Plugin.Core.PluginBase.PluginParameterBase
    {
        public string url { get; set; }
        public string sourceSheetName { get; set; }
        public string destSheetName { get; set; }
        public string sourceStart { get; set; }
        public string sourceEnd { get; set; }
        public string destStart { get; set; }
        public string destEnd { get; set; }
    }
}
