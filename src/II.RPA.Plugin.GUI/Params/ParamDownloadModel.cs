using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Params
{
    public class ParamDownloadModel : ParamBase
    {
        public string saveFile { get; set; }
        public string fileName { get; set; }
        public int timeout { get; set; } = 10000;
    }
}
