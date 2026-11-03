using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace II.RPA.EventMonitor.TestTool
{
    class SocketClient
    {
        static TcpClient tcpClient = null;
        static NetworkStream networkStream = null;
        public event Action<string> OnMessage;
        public bool IsConnected
        {
            get { return tcpClient != null && tcpClient.Connected; }
        }
        internal void Start()
        {
            try
            {
                tcpClient = new TcpClient();
                tcpClient.Connect(IPAddress.Parse("127.0.0.1"), 6446);
                networkStream = tcpClient.GetStream();
                OnMessage?.Invoke("连接成功");
                BeginRead();
            }
            catch (Exception ex)
            {
                OnMessage?.Invoke("Connected is fail: " + ex.Message);
            }
        }
        public void Close()
        {
            try
            {
                networkStream?.Close();
                tcpClient?.Close();
            }
            catch { }
            OnMessage?.Invoke("连接关闭");
        }
        public bool Send(string command, string json)
        {
            try
            {
                if (tcpClient != null && tcpClient.Connected)
                {
                    byte[] data = Encoding.UTF8.GetBytes($"{command} {json}\r\n");
                    networkStream.Write(data, 0, data.Length);
                    OnMessage?.Invoke("向服务端发送一条数据");
                }
                else
                {
                    OnMessage?.Invoke("发送数据失败");
                }
                return true;
            }
            catch (Exception ex)
            {
                OnMessage?.Invoke("发送消息异常: " + ex.Message);
                return false;
            }
        }
        private async void BeginRead()
        {
            byte[] buffer = new byte[1024 * 1024];
            try
            {
                while (tcpClient != null && tcpClient.Connected)
                {
                    int length = await networkStream.ReadAsync(buffer, 0, buffer.Length);
                    if (length == 0) break;
                    byte[] data = new byte[length];
                    Array.Copy(buffer, data, length);
                    string result = Encoding.UTF8.GetString(data);
                    OnMessage?.Invoke("收到服务端消息：" + result);
                }
            }
            catch { }
            OnMessage?.Invoke("连接断开");
        }
    }
}
