using II.RPA.WinAutomation.CommonUtility;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace II.RPA.AdminTransfer
{
    public class TelnetSession : AppSession<TelnetSession, StringRequestInfo>
    {
        private readonly static object sendLockObj = new object();
        private static ILogger logger = Logging.Logger.OfContext();

        protected override void OnSessionStarted()
        {
            logger.LogInformation($"OnSessionStarted:{this.RemoteEndPoint}");
        }
        public override void Send(string message)
        {
            if (!message.EndsWith("\r\n"))
                message += "\r\n";
            base.Send(message);
        }
        public override bool TrySend(string message)
        {
            lock (sendLockObj)
            {
                try
                {
                    if (!message.EndsWith("\r\n"))
                        message += "\r\n";
                    return base.TrySend(message);
                }
                catch (Exception ex)
                {
                    LoggingExtensions.LogError(logger, ex, $"TrySend JMessage发生错误");
                    return false;
                }
            }
        }
        public bool TrySend(JMessage message)
        {
            try
            {
                bool ret = this.TrySend(message.ToJson());
                return ret;
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                return false;
            }
        }

        protected override void HandleUnknownRequest(StringRequestInfo requestInfo)
        {
            logger.LogInformation($"Unknow request Command[{this.RemoteEndPoint}]:{requestInfo.Key}");
            JMessage msg = new JMessage();
            msg.status = (int)MessageStatusType.FAIL_STATUS;
            msg.message = $"Unknow request Command:{requestInfo.Key}";
            this.TrySend(msg);
        }

        protected override void HandleException(Exception e)
        {
            logger.LogInformation($"Application error[{this.RemoteEndPoint}]: { e.Message}");
        }

        protected override void OnSessionClosed(CloseReason reason)
        {
            logger.LogInformation($"SessionClosed[{this.RemoteEndPoint}]:{reason.ToString()}");
            //add you logics which will be executed after the session is closed
            base.OnSessionClosed(reason);
        }
    }
}
