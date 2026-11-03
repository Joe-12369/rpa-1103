
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.ServiceHub
{
    public class RPAConcurrentDictionary<TKey, TValue> : ConcurrentDictionary<TKey, TValue>
    {
        private readonly object objLock = new object();
        public RPAConcurrentDictionary()
        {
        }

        public RPAConcurrentDictionary(IEqualityComparer<TKey> comparer)
            : base(comparer)
        {

        }

    }

    public class ConnectionInfo
    {
        public string ConnectionId { get; set; }
        public int Port { get; set; }
        public int SessionId { get; set; }
        /// <summary>
        /// 客户端类型
        /// </summary>
        public SignalRClientType ClientType { get; set; }
        /// <summary>
        /// 客户端标签，
        /// </summary>
        public string ClientFlag { get; set; }
        public DateTime ConnectedTime { get; set; } = DateTime.Now;
        public ConnectionState ConnectionState { get; set; } = ConnectionState.Connected;
    }



}
