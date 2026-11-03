using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.NamedPipeWrapper
{
    public class HIDPipeResult
    {
        public HIDPipeResult()
        {
            this.MessageID = string.Empty;
            this.Action = eHIDSendAction.None; ;
            this.Content = string.Empty;
            this.FormName = string.Empty;
            this.KeepDown = 50;
            this.Block = 100;
            this.ResponseMsg = new HIDResponseError();
        }
        [JsonProperty("message_id")]
        public string MessageID { get; set; }
        [JsonProperty("form_name")]
        public string FormName { get; set; }
        [JsonProperty("action")]
        [JsonConverter(typeof(StringEnumConverter))]
        public eHIDSendAction Action { get; set; }
        [JsonProperty("content")]
        public string Content { get; set; }
        [JsonProperty("keepdown")]
        public int KeepDown { get; set; }
        [JsonProperty("block")]
        public int Block { get; set; }
        [JsonProperty("response_msg")]
        public HIDResponseError ResponseMsg{ get; set; }
    }

   
    public class HIDResponseError
    {
        public HIDResponseError()
        {
            this.Code = 0;
            this.Message = "none";
        }
        [JsonProperty("code")]
        public int Code { get; set; }
        [JsonProperty("message")]
        public string Message { get; set; }

    }
    public enum eHIDSendAction
    {
        None,
        ClearAllText,
        SendText
    }
}
