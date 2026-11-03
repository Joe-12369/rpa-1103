using II.RPA.WinAutomation.CommonUtility;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;


namespace II.RPA.AdminTransfer
{
    public class TelnetServer : AppServer<TelnetSession>
    {
        private static ILogger logger = Logging.Logger.OfContext();

        public TelnetServer() : base()
        {
            this.NewSessionConnected += new SessionHandler<TelnetSession>(appServer_NewSessionConnected);
        }

        protected override bool Setup(IRootConfig rootConfig, IServerConfig config)
        {
            return base.Setup(rootConfig, config);
        }

        protected override void OnStarted()
        {
            base.OnStarted();
        }

        protected override void OnStopped()
        {
            base.OnStopped();
            logger.LogInformation("Session Stoped");
        }

        void appServer_NewSessionConnected(TelnetSession session)
        {
            logger.LogInformation($"New Session:{session.RemoteEndPoint}");
            //JMessage message = new JMessage();
            //message.status = (int)MessageStatusType.SUCCESS_STATUS;
            //message.message = "Welcome to AdminTransfer Telnet Server";
            //session.TrySend(message);
        }
    }
}
