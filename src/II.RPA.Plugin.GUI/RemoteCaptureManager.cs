//using System;
//using System.Text;
//using II.RPA.Logging;
//using Microsoft.Extensions.Logging;

//namespace II.RPA.Plugin.GUI
//{
//    public class RemoteCaptureClient : AsyncTcpSession
//    {
//        private static ILogger logger = Logger.OfContext();

//        public RemoteCaptureClient()
//        {
//            this.ReceiveBufferSize = 4 * 1024 * 1024;
//        }
//        /// <summary>
//        /// 开始拾取操作
//        /// </summary>
//        /// <param name="paramsStr"></param>
//        internal void StartRemoteCapture(string paramsStr)
//        {
//            string msg = $"Start:{paramsStr}\r\n";
//            SendMessage(msg);
//        }
//        /// <summary>
//        /// 技术远程拾取操作
//        /// </summary>
//        internal void EndRemoteCapture()
//        {
//            string msg = $"End:1,2\r\n";
//            SendMessage(msg);
//        }
//        /// <summary>
//        /// 开始执行远程捕获元素
//        /// </summary>
//        /// <param name="address"></param>
//        internal void DoRemoteCapture(string address)
//        {
//            string msg = $"Capture:{address}\r\n";
//            SendMessage(msg);
//        }
//        internal bool SendMessage(string msg)
//        {
//            try
//            {
//                if (!this.IsConnected)
//                {
//                    return false;
//                }
//                byte[] data = Encoding.Default.GetBytes(msg);
//                ArraySegment<byte> arry = new ArraySegment<byte>(data, 0, data.Length);
//                return this.TrySend(arry);
//            }
//            catch (Exception ex)
//            {
//                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, $"发送消息异常");
//                return false;
//            }
//        }
//    }
//}

