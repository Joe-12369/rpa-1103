using II.RPA.Plugin.Core;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.WebBrowser.Common.Basic.connection
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
    }

    [Flags]
    public enum SignalRClientType
    {
        All = 0,
        Factory = 1 << 0,
        Bot = 1 << 1,
        Web = 1 << 2,
        DeskTop = 1 << 3,
        ZAdmin = 1 << 4,


        CoreServer = 1 << 10,
        PluginProcess = 1 << 11,


        Other = 1 << 31
    }
    public enum MessageType
    {
        None = 0,
        Send = 1,
        Collback = 2
    }
}
