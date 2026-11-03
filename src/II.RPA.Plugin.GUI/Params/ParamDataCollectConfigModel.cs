using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Params
{
    public class ParamDataCollectConfigModel : ParamBase
    {
        public string guiType { get; set; } = string.Empty;

        public string windowType { get; set; } = string.Empty;

        //public string webData { get; set; }
        public string winData { get; set; } = string.Empty;

        public string TagNames { get; set; } = string.Empty;

        public string AttrKeywords { get; set; } = string.Empty;

        public string clsNames { get; set; } = string.Empty;
        /// <summary>
        /// normal  cvToNormal    webCombineCv
        /// </summary>
        public string sourceType { get; set; } = string.Empty;

        public bool async = false;
        public string asyncKey = string.Empty;
        public string prevCVSelector = string.Empty;
        [JsonProperty("special_operation")]
        public string SpecialOperation { get; set; } = string.Empty;
        [JsonProperty("task_info")]
        public object TaskInfo { get; set; }
        [JsonProperty("column_type")]
        public List<string> ColumnType { get; set; }
        [JsonProperty("is_enable_table")]
        public int iSEnableTable { get; set; } = 0;


        //非入参
        [JsonIgnore]
        internal IntPtr WndHandle { get; set; }
    }
}
