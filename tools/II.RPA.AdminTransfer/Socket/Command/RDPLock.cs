using II.RPA.AdminTransfer;
using II.RPA.SupportService.Sockets.Entitys;
using II.RPA.WinAutomation.CommonUtility;
using SuperSocket.SocketBase.Command;
using SuperSocket.SocketBase.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.EventMonitor.Comm.Command
{
    public class RDPLock : CommandBase<TelnetSession, StringRequestInfo>
    {
        private static ILogger logger = Logger.OfContext();

        public override void ExecuteCommand(TelnetSession session, StringRequestInfo requestInfo)
        {
            JMessage msg = new JMessage();
            session.Logger.Debug($"Key:{requestInfo.Key},Body:{requestInfo.Body}");
            RDPLockParams param = null;
            try
            {
                param = requestInfo.Body.ToObject<RDPLockParams>();
                if (param == null)
                {
                    msg.status = (int)MessageStatusType.FAIL_STATUS;
                    msg.message = eCommonMessageType.RDPLock_cs_XuYaoTiGongYouXiaoDeCanShu.GetMessage();
                    return;
                }
                if (string.IsNullOrEmpty(param.UserName) || string.IsNullOrEmpty(param.Password))
                {
                    var exInfo = new RPAException(eErrorMessageType.Error_ScreenLock_AccountOrPwd, eComponentType.RDPLock);
                    msg.status = (int)MessageStatusType.FAIL_STATUS;
                    msg.message = exInfo.ErrorMessage;
                    session.TrySend(msg);
                    return;
                }
                //业务逻辑
                msg = RDPLockAction(param);
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                msg.status = (int)MessageStatusType.FAIL_STATUS;
            }
            session.TrySend(msg);

        }
        FreeRDP.Core.RDP rdp;
        internal JMessage RDPLockAction(RDPLockParams param)
        {
            JMessage msg = new JMessage();
            msg.status = (int)MessageStatusType.SUCCESS_STATUS;
            rdp = new FreeRDP.Core.RDP();
            try
            {
                rdp.Connect("127.0.0.2", "", param.UserName, param.Password, 3389, null);
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                var exInfo = new RPAException(eErrorMessageType.Error_RDPLock_RdpError, eComponentType.RDPLock, ex);
                msg.status = (int)MessageStatusType.FAIL_STATUS;
                msg.message = exInfo.ErrorMessage;
            }
            return msg;
        }
    }
}
