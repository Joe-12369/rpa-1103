using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Params
{
    public class ParamExtractWinndowInfoModel : ParamBase
    {
        public string guiType { get; set; } = string.Empty;
        public string windowType { get; set; } = string.Empty;

        /// <summary>
        /// normal  cvToNormal    webCombineCv
        /// </summary>
        public string sourceType { get; set; } = string.Empty;
        public object query { get; set; } 

        //非入参
        [JsonIgnore]
        internal IntPtr WndHandle { get; set; }

    }
}
