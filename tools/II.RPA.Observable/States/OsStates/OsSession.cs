using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Observable.States.OsStates
{
    /// <summary>
    /// 操作系统会话
    /// </summary>
    public class OsSession
    {
        public string ClientName { get; set; } = string.Empty;

        public ConnectionState ConnectionState { get; set; }

        public DateTime? ConnectTime { get; set; }

        public DateTime? CurrentTime { get; set; }

        public DateTime? DisconnectTime { get; set; }

        public DateTime? LastInputTime { get; set; }

        public DateTime? LoginTime { get; set; }

        public TimeSpan IdleTime { get; set; }

        public int SessionId { get; set; }

        public string DomainName { get; set; } = string.Empty;

        public string WindowStationName { get; set; } = string.Empty;

        public static OsSession Empty { get; } = new OsSession();

        public override string ToString()
        {
            return $"{SessionId} {WindowStationName} {ConnectionState} [{ConnectTime:yyy-MM-dd HH:mm:ss} => {CurrentTime:yyyy-MM-dd HH:mm:ss}]";
        }
    }
}
