using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.EventMonitor
{
    public class SocketServer
    {
        //创建一个和客户端通信的套接字
        static Socket SocketWatch = null;
        //定义一个集合，存储客户端信息
        public static Dictionary<string, Socket> ClientConnectionItems = new Dictionary<string, Socket> { };
        private IPAddress ipAddress;
        private int port;
        protected Thread threadwatch;
        public SocketServer(string ip, int port)
        {
            this.ipAddress = IPAddress.Parse(ip);
            this.port = port;
        }
        public void StartServer()
        {
            try
            {
                IPEndPoint ipe = new IPEndPoint(ipAddress, port);
                //定义一个套接字用于监听客户端发来的消息，包含三个参数（IP4寻址协议，流式连接，Tcp协议） 
                SocketWatch = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                SocketWatch.Bind(ipe);
                SocketWatch.Listen(50);
                MainForm.PrintMessage("Start result: true!");
            }
            catch
            {
                MainForm.PrintMessage("Failed to start!");
                System.Environment.Exit(0);
                return;
            }
            //负责监听客户端的线程:创建一个监听线程 
            threadwatch = new Thread(WatchConnecting);
            threadwatch.IsBackground = true;
            threadwatch.Start();
        }
        //监听客户端发来的请求 
        static void WatchConnecting()
        {
            Socket connection = null;
            //持续不断监听客户端发来的请求   
            while (true)
            {
                try
                {
                    connection = SocketWatch.Accept();
                }
                catch (Exception ex)
                {
                    Loggers.Error(ex);
                    break;
                }
                string remoteEndPoint = connection.RemoteEndPoint.ToString();
                //添加客户端信息 
                ClientConnectionItems.Add(remoteEndPoint, connection);
                MainForm.PrintMessage($"New Session:{remoteEndPoint}");
                Loggers.Info($"New Session:{remoteEndPoint}");
                JMessage message = new JMessage();
                message.message = "Welcome to Trigger Telnet Server";
                connection.TrySend(message);
                //创建一个通信线程   
                Thread thread = new Thread(receive);
                thread.IsBackground = true;
                thread.Start(connection);
            }
        }
        /// <summary>
        /// 接收客户端发来的信息，客户端套接字对象
        /// </summary>
        /// <param name="socketclientpara"></param>  
        static void receive(object socketclientpara)
        {
            Socket socketServer = socketclientpara as Socket;
            //bool flag = true;
            while (true)
            {
                //flag = false;
                //创建一个内存缓冲区，其大小为1024*1024字节 即1M   
                byte[] arrServerRecMsg = new byte[1024 * 1024];
                try
                {
                    int length = socketServer.Receive(arrServerRecMsg);
                    if (length == 0)
                    {
                        // 客户端已关闭连接
                        Loggers.Warn($"客户端 {socketServer.RemoteEndPoint} 已关闭连接。将释放触发器任务");
                        string key = socketServer.RemoteEndPoint.ToString();
                        socketServer.Close();
                        TriggerBus.CloseTriggerBySessionID(key);
                        if (ClientConnectionItems.ContainsKey(key))
                            ClientConnectionItems.Remove(key);
                        break;
                    }
                    string strSRecMsg = Encoding.UTF8.GetString(arrServerRecMsg, 0, length);
                    //将发送的字符串信息附加到文本框txtMsg上   
                    Loggers.Info("\r\n[客户端：" + socketServer.RemoteEndPoint + " 时间：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff") + "]\r\n" + strSRecMsg);
                    //处理接收的客户端请求
                    if (ClientConnectionItems.Count > 0)
                    {
                        ReceivedEvent.ExecuteCommand(socketServer, strSRecMsg);
                    }
                }
                catch (Exception)
                {
                    ClientConnectionItems.Remove(socketServer.RemoteEndPoint.ToString());
                    Loggers.Error("\r\n[客户端\"" + socketServer.RemoteEndPoint + "\"已经中断连接！ 客户端数量：" + ClientConnectionItems.Count + "]");
                    string key = socketServer.RemoteEndPoint.ToString();
                    socketServer.Close();
                    if (ClientConnectionItems.ContainsKey(key))
                        ClientConnectionItems.Remove(key);
                    break;
                }
                finally
                {
                    //flag = true;
                }
            }
        }
        public void StopServer()
        {
            threadwatch.Abort();
            this.EndListenAndSend();
        }
        protected void EndListenAndSend()
        {
            Thread.Sleep(200);//以防消息没有发完，或收完
            foreach (var keyValue in ClientConnectionItems)
            {
                ClientConnectionItems.Remove(keyValue.Key);
            }
        }
    }
}
