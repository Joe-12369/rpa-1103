using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.NativeMessageingStartup
{
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
        Callback = 2,
        NativeMessage = 3,   // 内部发送（平台自身）
        NativeMessageCallback = 4 // 内部回复（平台自身）
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
