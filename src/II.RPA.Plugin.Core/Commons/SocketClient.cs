//using II.RPA.WinAutomation.CommonUtility;
//using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
//using SuperSocket.ClientEngine;
//using SuperSocket.ProtoBase;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;

//namespace II.RPA.Plugin.Core.Commons
//{
//    public class SocketClient
//    {
//        JMessage msg;
//        bool flag = false;
//        public bool IsRemoteExecute(string selector)
//        {
//            if (selector == null)
//            {
//                selector = "";
//            }
//            return selector.Contains("remoteName") && selector.Contains("remoteAddress") && selector.Contains("mstsc");
//        }

//        public JMessage SendExecute(string address, string param, string type, string operateType = "Operate")
//        {
//            AutoResetEvent initWait = new AutoResetEvent(false);
//            var c = new EasyClient();
//            try
//            {

//                msg = new JMessage(-1, "操作失败".I18n("SocketClient_E197F50C"));
//                c.Initialize(new MyReceiveFilter(), (request) =>
//                {
//                    var result = Newtonsoft.Json.JsonConvert.DeserializeObject<JMessage>(request.Body);
//                    msg.Code = result.Code;
//                    msg.UnifyCode = result.UnifyCode;
//                    msg.Message = result.Message;
//                    msg.Info = result.Info;
//                    flag = true;
//                    c.Close();
//                    c = null;
//                    initWait.Set();
//                });
//                string message = $"{operateType}:{type},{param}\r\n";
//                byte[] data = Encoding.Default.GetBytes(message);
//                char separator = ',';
//                if (address.IndexOf(":") > 0)
//                {
//                    separator = ':';
//                }
//                var isConnected = c.ConnectAsync(new IPEndPoint(IPAddress.Parse(address.Split(separator)[0]), int.Parse(address.Split(separator)[1]))).Result;
//                if (isConnected)
//                {
//                    c.Send(data);
//                }
//                else
//                {
//                    msg.Code = -1;
//                    msg.UnifyCode = "-1";
//                    msg.Message = "远程服务器连接失败！".I18n("SocketClient_2DEA6F72");
//                }
//                if (!initWait.WaitOne(1000 * 20))
//                {
//                    msg.Code = -1;
//                    msg.UnifyCode = "-1";
//                    msg.Message = "远程服务器未响应，请检查IP、端口参数以及网络状态！".I18n("SocketClient_12FB915");
//                }
//            }
//            catch (Exception e)
//            {
//                msg.Code = -1;
//                msg.UnifyCode = "-1";
//                msg.Message = e.Message;
//            }
//            finally
//            {
//                if (c != null)
//                {
//                    c.Close();
//                }
//                initWait.Dispose();
//            }
//            return msg;
//        }

//    }

//    class MyReceiveFilter : TerminatorReceiveFilter<StringPackageInfo>
//    {
//        public MyReceiveFilter()
//        : base(Encoding.UTF8.GetBytes("\r\n")) // two vertical bars as package terminator
//        {
//        }

//        public override StringPackageInfo ResolvePackage(IBufferStream bufferStream)
//        {
//            //获取接收到的完整数据，包括头和尾
//            var body = bufferStream.ReadString((int)bufferStream.Length, Encoding.UTF8);
//            return new StringPackageInfo("", body, new string[] { });
//        }
//    }

//    public class JRemoteMessage
//    {
//        public int code { get; set; } = 1;    //返回状态
//        public string message { get; set; } = ""; //如果失败，返回失败原因
//        public string info { get; set; } = "";             //非必填，只有当需要返回信息的接口时才需要
//    }
//}
