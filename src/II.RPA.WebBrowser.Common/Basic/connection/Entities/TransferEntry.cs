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
    public class TransferEntry
    {
        private ManualResetEvent waitHandle;
        public string MessageId { get; set; }
        public DateTime CreateTime { get; set; } = DateTime.Now;
        /// <summary>
        /// 消息超时时间，无限超时使用intmax
        /// </summary>
        public int Timeout { get; set; }
        public long Timestamp { get; set; } = 0;
        public long ElapsedMilliseconds => (long)((Stopwatch.GetTimestamp() - Timestamp) * 1000D / Stopwatch.Frequency);
        public TransferMessage ResultTransferMessage { get; set; } = null;

        private bool IsComplete { get; set; } = false;
        public TransferEntry()
        {
            waitHandle = new ManualResetEvent(initialState: false);
        }
        internal T WaitResult<T>(Func<TransferMessage, T> callback)
        {
            this.WaitOne();
            return callback(this.ResultTransferMessage);
        }


        internal void WaitOne()
        {
            if (this.Timeout > ElapsedMilliseconds)
            {
                this.WaitOne(this.Timeout);
            }
            else //已经超时，直接结束
            {
                this.Complete(null);
            }
        }
        private void WaitOne(int timeout)
        {
            waitHandle.WaitOne(timeout);
        }
        internal void Complete(TransferMessage resultTransferMessage)
        {
            this.ResultTransferMessage = resultTransferMessage;
            this.Complete();
        }
        internal void Complete()
        {
            this.IsComplete = true;
            if (waitHandle != null)
            {
                waitHandle.Set();
            }
        }
        ~TransferEntry()
        {
            if (waitHandle != null)
            {
                waitHandle.Close();
            }
        }
    }
}
