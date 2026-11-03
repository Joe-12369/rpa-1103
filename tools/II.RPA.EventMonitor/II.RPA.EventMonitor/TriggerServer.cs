using System;
using System.Net;

namespace II.RPA.EventMonitor
{
    public class TriggerServer
    {
        private static SocketServer socketServer = null;

        internal static void Start()
        {
            try
            {
                socketServer = new SocketServer("127.0.0.1", 6446);
                socketServer.StartServer();
            }
            catch (Exception ex)
            {
                Loggers.Error($"启动服务失败: {ex}");
            }
        }

        internal static void Stop()
        {
            try
            {
                Loggers.Fatal("The server was stopped!");
                if (socketServer != null)
                {
                    socketServer.StopServer();
                    socketServer = null;
                }
            }
            catch (Exception ex)
            {
                Loggers.Error($"The server stop exception: {ex}");
            }
        }

        internal static bool SendMessage(string sessionID, JMessage message)
        {
            try
            {
                if (string.IsNullOrEmpty(sessionID))
                {
                    Loggers.Info("发送消息时，未找到客户端会话【会话编号为空】");
                    return false;
                }
                var session = SocketServer.ClientConnectionItems.ContainsKey(sessionID)
                    ? SocketServer.ClientConnectionItems[sessionID] : null;
                if (session == null)
                {
                    Loggers.Info("发送消息时，未找到客户端会话");
                    return false;
                }
                bool flag = session.TrySend(message);
                if (message != null && message.postBack.targetTriggers != null)
                    message.postBack.targetTriggers.Clear();
                return flag;
            }
            catch (Exception ex)
            {
                Loggers.Error($"发送消息时异常：{ex}");
                return false;
            }
        }
    }
}
