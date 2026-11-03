using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.ServiceHub
{
    [Flags]
    public enum SignalRClientType
    {
        All = 0,
        Unknown = 1 << 30,
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
    public enum MessageState
    {
        None = 0,
        WaitSend = 1,
        WaitCollback = 2
    }
    public enum ConnectionState
    {
        Connected = 0,
        Disconnected = 1
    }
}
