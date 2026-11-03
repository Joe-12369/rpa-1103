//using II.RPA.WinAutomation.CommonUtility;
//using System;
//using System.Text;
//using System.Threading;
//using System.Diagnostics;
//using System.IO;
//using SuperSocket.ClientEngine;
//using System.Net;
//using II.RPA.Plugin.Core;
//using System.Linq;
//using System.Management;
//using Newtonsoft.Json.Linq;
//using II.RPA.Logging;
//using Microsoft.Extensions.Logging;

//namespace II.RPA.Plugin.RPASystem
//{
//    internal class SupportSocketClient
//    {
//        private static ILogger logger = Logger.OfContext();
//        private static SupportSocketClient _client = null;
//        //创建客户端对象，默认连接本机127.0.0.1,端口为12345
//        //SocketClient socketClient = null;
//        private AsyncTcpSession tcpClient = null;
//        private bool clientConnectStatus = false;
//        private AutoResetEvent waitEvent = null;
//        private AutoResetEvent waitEvent_connect = null;
//        private string socketResult = "";
//        private const int TIME_OUT = 10000;

//        private static readonly object _lock = new object();

//        private SupportSocketClient()
//        {
//            logger.LogInformation("Start SupportPipeClient");
//            waitEvent = new AutoResetEvent(false);
//        }
//        internal static SupportSocketClient Client
//        {
//            get
//            {
//                if (_client == null)
//                {
//                    lock (_lock)
//                    {
//                        if (_client == null)
//                        {
//                            _client = new SupportSocketClient();
//                        }
//                    }
//                }
//                return _client;
//            }
//        }

//        public bool CheckStart(bool isBootUp = true)
//        {

//            try
//            {
//                if (clientConnectStatus)
//                {
//                    return true;
//                }
//                if (!CheckAdminTransferIsStart(isBootUp))
//                {
//                    logger.LogInformation($"未检测到admintransfer,且未拉起，请排查原因或重启再试");
//                    clientConnectStatus = false;
//                    return false;
//                }
//                return true;
//            }
//            catch (Exception ex)
//            {
//                _client = null;
//                tcpClient.Close();
//                clientConnectStatus = false;
//                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"连接服务失败 确认服务是否开启 以及服务的其他信息。");
//            }
//            return false;
//        }
//        public bool ConnectServer()
//        {
//            waitEvent_connect = new AutoResetEvent(false);
//            tcpClient = new AsyncTcpSession();
//            tcpClient.Connected += client_Connected;
//            tcpClient.DataReceived += client_DataReceived;
//            tcpClient.Error += client_Error;
//            tcpClient.Closed += client_Closed;
//            int connectCount = 0;
//            do
//            {
//                connectCount++;
//                logger.LogInformation($"Try Connect {connectCount}");
//                tcpClient.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6447));
//                if (waitEvent_connect.WaitOne(TIME_OUT))//阻塞等待 确认已经连接上
//                {
//                    if (tcpClient != null && !tcpClient.IsConnected)
//                    {
//                        logger.LogInformation("AdminTransfer Connected is fail");
//                        return false;
//                    }
//                    return clientConnectStatus;
//                }
//                else
//                {
//                    clientConnectStatus = false;
//                }
//            } while (connectCount <= 2);
//            return false;
//        }
//        #region Socket事件
//        private void client_Connected(object sender, EventArgs e)
//        {
//            logger.LogInformation("连接成功");
//            clientConnectStatus = true;
//            waitEvent_connect.Set();
//        }
//        private void client_DataReceived(object sender, DataEventArgs e)
//        {
//            try
//            {
//                byte[] buffer = new byte[e.Length];
//                Array.Copy(e.Data, buffer, buffer.Length);
//                socketResult = parseNetData(buffer);
//                logger.LogInformation("收到服务端消息：" + socketResult);

//            }
//            catch (Exception ex)
//            {
//                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"收到服务端消息,但是处理异常");
//            }
//            waitEvent.Set();

//        }
//        private void client_Closed(object sender, EventArgs e)
//        {
//            tcpClient.Close();
//            logger.LogInformation("连接断开");
//        }
//        private void client_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
//        {
//            logger.LogInformation(e.Exception);
//        }
//        private string parseNetData(byte[] data)
//        {
//            string str = Encoding.UTF8.GetString(data);
//            return str;
//        }
//        public bool Send(string command, string json)
//        {
//            try
//            {
//                if (tcpClient != null)
//                {
//                    if (tcpClient.IsConnected)
//                    {
//                        byte[] data = Encoding.UTF8.GetBytes($"{command} {json}\r\n");
//                        tcpClient.Send(data, 0, data.Length);
//                        logger.LogInformation("向admintransfer发送一条数据");
//                    }
//                    else
//                    {
//                        logger.LogInformation("发送数据失败");
//                    }
//                }
//                return true;
//            }
//            catch (Exception ex)
//            {
//                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"发送消息异常");
//                return false;
//            }
//        }
//        #endregion

//        //internal JMessage RDPUnlock(string username, string pwd)
//        //{

//        //    if (tcpClient == null)
//        //    {
//        //        if (!CheckStart(false))
//        //        {
//        //            logger.LogInformation($"在“解锁”时未检测到admintransfer,请排查原因");
//        //            var res = new JMessage { Code = 1, Message = "在“解锁”时未检测到admintransfer，请排查原因" };
//        //            return res;
//        //        }
//        //    }
//        //    if (!CheckAdminTransferIsStart(false))
//        //    {
//        //        logger.LogInformation($"在“解锁”时未检测到admintransfer,请排查原因");
//        //        var res = new JMessage { Code = 1, Message = "在“解锁”时未检测到admintransfer，请排查原因" };
//        //        return res;
//        //    }
//        //    if (!ConnectServer())
//        //    {
//        //        logger.LogInformation($"在“解锁”时检测到admintransfer,但无法链接，请排查原因");
//        //        var res = new JMessage { Code = 1, Message = "在“解锁”时已检测到admintransfer，但无法使用，可以关闭ii.rpa.admintransfer进程后重试" };
//        //        return res;
//        //    }
//        //    JObject param = new JObject();
//        //    param.Add("UserName", username);
//        //    param.Add("Password", pwd);

//        //    try
//        //    {
//        //        string paramJson = param.ToJson();
//        //        string command = "RDPUnlock";
//        //        logger.LogInformation($"Command:{command},Param:{paramJson}");
//        //        Send(command, paramJson);
//        //        if (waitEvent.WaitOne(TIME_OUT))
//        //        {
//        //            logger.LogInformation($"开始处理admintransfer返回数据：{socketResult}");

//        //            string result = socketResult;
//        //            socketResult = "";
//        //            if (result.EndsWith("\r\n"))
//        //            {
//        //                result = result.Substring(0, result.Length - 2);
//        //            }
//        //            return result.ToObject<JMessage>();
//        //        }
//        //        else
//        //        {
//        //            var res = new JMessage { Code = 1, Message = "Socket通信超时失败，请确认是否开启解锁程序" };
//        //            return res;
//        //        }
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        logger.LogError(ex);
//        //        var res = new JMessage { Code = 1, Message = "屏幕解锁可能异常，注意" };
//        //        return res;
//        //    }
//        //}
//        /// <summary>
//        /// 检查管理员身份程序是否启动
//        /// </summary>
//        /// <param name="isBootUP">如否启动，true 如果未启动，则直接尝试启动</param>
//        /// <returns></returns>
//        internal bool CheckAdminTransferIsStart(bool isBootUp = true)
//        {
//            bool ret = false;
//            try
//            {
//                string proName = "II.RPA.AdminTransfer";
//                var processArray = GetProcessesByName(proName, false);
//                if (processArray.Length == 0 && isBootUp)
//                {
//                    string fileName = Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.FullName, $@"II.RPA.AdminTransfer\{proName}.exe");
//#if Trace
//                    fileName = $@"..\II.RPA.AdminTransfer\{proName}.exe";
//#endif
//                    logger.LogDebug(fileName);
//                    //var pro = Process.Start(fileName);


//                    //创建启动对象
//                    System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
//                    //设置运行文件
//                    startInfo.FileName = fileName;
//                    //设置启动动作,确保以管理员身份运行
//                    startInfo.Verb = "runas";
//                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
//                    var pro = Process.Start(startInfo);
//                    Thread.Sleep(2000);
//                    return pro != null;
//                }
//                else
//                {
//                    ret = true;
//                }
//            }
//            catch (Exception ex)
//            {

//            }
//            return ret;

//        }


//        internal Process[] GetProcessesByName(string processName, bool onlyCurrentUser)
//        {
//            try
//            {
//                if (!onlyCurrentUser)
//                {
//                    return Process.GetProcessesByName(processName);
//                }
//                return (from m in Process.GetProcessesByName(processName)
//                        where GetProcessOwner(m.Id) == Environment.UserName
//                        select m).ToArray();
//            }
//            catch (Exception e)
//            {
//                logger.LogError(e);
//            }
//            return null;
//        }
//        internal static string GetProcessOwner(int processId)
//        {
//            try
//            {
//                foreach (ManagementObject obj in new ManagementObjectSearcher("Select * From Win32_Process Where ProcessID = " + processId).Get())
//                {
//                    string[] argList = new string[2]
//                    {
//                    string.Empty,
//                    string.Empty
//                    };
//                    object[] args = argList;
//                    if (Convert.ToInt32(obj.InvokeMethod("GetOwner", args)) == 0)
//                    {
//                        return argList[0];
//                    }
//                }
//            }
//            catch (Exception e)
//            {
//                logger.LogError(e);
//                return "NO OWNER";
//            }
//            return "NO OWNER";
//        }
//        public Process[] GetProcessesBySessionId(string processName)
//        {
//            try
//            {
//                int sid = Process.GetCurrentProcess().SessionId;
//                return (from m in Process.GetProcessesByName(processName)
//                        where m.SessionId == sid
//                        select m).ToArray();
//            }
//            catch (Exception e)
//            {
//                logger.LogError(e);
//            }
//            return null;
//        }
//    }
//}

