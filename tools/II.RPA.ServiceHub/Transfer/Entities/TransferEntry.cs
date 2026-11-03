using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.ServiceHub.Transfer
{
    public class TransferEntry
    {
        public long Timestamp { get; set; }
        public long ElapsedMilliseconds => (long)((Stopwatch.GetTimestamp() - Timestamp) * 1000D / Stopwatch.Frequency);
        public string MessageId => TransferMessage?.MessageId;
        public ConnectionInfo ConnectionInfo { get; set; }
        public DateTime CreateTime { get; set; } = DateTime.Now;
        /// <summary>
        /// 消息超时时间
        /// </summary>
        public int Timeout { get; set; } = 0;
        public TransferMessage TransferMessage { get; set; }

        public MessageState MessageState { get; set; } = MessageState.None;
        public int ReEnqueueCount { get; set; } = 0;


    }
}
