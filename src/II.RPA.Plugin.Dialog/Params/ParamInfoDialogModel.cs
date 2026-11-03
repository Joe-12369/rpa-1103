using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Plugin.Core.PluginBase;

namespace II.RPA.Plugin.Dialog.Params
{
    public class ParamInfoDialogModel : PluginParameterBase
    {
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("context")]
        public string Context { get; set; }
        [JsonProperty("infoType")]
        public EmInfoType InfoType { get; set; }
        [JsonProperty("infoResultType")]
        public EmInfoResultType InfoResultType { get; set; }
        [JsonProperty("autoOut")]
        public AutoOut AutoOut { get; set; }
    }

    public enum EmInfoType
    {
        Info = 1,
        Question = 2,
        Warn = 3,
        Error = 4,
    }

    [Flags]
    public enum EmInfoResultType
    {
        Confirm = 0x0001,
        Cancel = 0x0002,
        Retry = 0x0004,
        Abort = 0x0008,
    }

    //public enum EmInfoResultType
    //{
    //    Confirm = 1,
    //    Confirm_Cancel = 2,
    //    Retry_Cancel = 3,
    //    Retry_Abort_Cancel = 4,
    //}

    public enum EmResult
    {
        Confirm = 1,
        Cancel = 2,
        Retry = 3,
        Abort = 4,
    }
}
