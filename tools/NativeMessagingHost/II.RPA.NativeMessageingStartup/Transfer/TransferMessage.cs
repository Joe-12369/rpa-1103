using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.NativeMessageingStartup
{
    public class TransferMessage
    {
        public string MessageId { get; set; }
        public string Action { get; set; }
        public SignalRClientType TargetClientType { get; set; }
        public string TargetClientFlag { get; set; }
        public object SendData { get; set; }
        public object CallbackReslut { get; set; }
        public MessageType MessageType { get; set; } = MessageType.Send;
        /// <summary>
        /// 消息超时时间
        /// </summary>
        public int Timeout { get; set; } = 0;
        public long Timestamp { get; set; } = 0;
        [JsonProperty("connType")]
        public string ConnType { get; set; } = "nativemsg_tMsg";
    }
}
