using II.RPA.EventMonitor.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.EventMonitor
{
    public static class SocketExtension
    {
        public static bool TrySend(this Socket connection, string message)
        {
            try
            {
                if (!message.EndsWith("\r\n"))
                    message += "\r\n";
                MainForm.PrintMessage("TrySend:" + message);
                int count = connection.Send(Encoding.UTF8.GetBytes(message));
                return count > 0;
            }
            catch (Exception ex)
            {
                Loggers.Error($"TrySend JMessage 异常.{ex}");
                return false;
            }
        }
        public static bool TrySend(this Socket connection, JMessage message)
        {
            try
            {
                if (string.IsNullOrEmpty(message.message))
                {
                    message.message = Configurations.GetErrorMessge(message.status);
                }
                bool ret = connection.TrySend(message.ToJson());
                return ret;
            }
            catch (Exception ex)
            {
                Loggers.Error(ex);
                return false;
            }
        }
    }
}
