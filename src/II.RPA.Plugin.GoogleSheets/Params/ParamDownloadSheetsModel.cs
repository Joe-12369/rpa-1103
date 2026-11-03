using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GoogleSheets.Params
{
    public class ParamDownloadSheetsModel : II.RPA.Plugin.Core.PluginBase.PluginParameterBase
    {
        public string url { get; set; }
        public string format { get; set; }  //csv、xlsx..
        public int isOnlySheet { get; set; }
    }
}
