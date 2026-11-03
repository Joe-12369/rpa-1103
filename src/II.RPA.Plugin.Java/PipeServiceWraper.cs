using System.Threading;
using System.Threading.Tasks;
using II.RPA.NamedPipeWrapper;
using II.RPA.Plugin.Core;
using II.RPA.WinAutomation.CommonUtility;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.Java
{
    public class PipeServiceWraper
    {
        private static ILogger logger = Logger.OfContext();
        private const int TIME_OUT = 10000;
        private AutoResetEvent _initWait = new AutoResetEvent(false);
        private NamedPipeClient<Core.Entities.JavaMessage> _pipeclient = null;
        private ManualResetEvent _waitEvent;
        private string PipeResult;

        public PipeServiceWraper(string address)
        {
            _waitEvent = new ManualResetEvent(false);
            Task.Factory.StartNew(() =>
            {
                if (_pipeclient == null)
                {
                    _pipeclient = new NamedPipeClient<Core.Entities.JavaMessage>(address);
                    _pipeclient.Connected += Pipeclient_Connected;
                    _pipeclient.ServerMessage += Pipeclient_ServerMessage;
                    _pipeclient.AutoReconnect = true;
                    _pipeclient.Start();
                    _initWait.WaitOne(5000);
                }
            });
        }

        public bool IsConnected
        {
            get
            {
                if (_pipeclient != null && _pipeclient.isConnected)
                {
                    return true;
                }
                return false;
            }
        }

        public JMessage SendJavaActinMessage(Core.Entities.JavaMessage javaMessage)
        {
            _waitEvent.Reset();
            _pipeclient.PushMessage(javaMessage);
            if (_waitEvent.WaitOne(TIME_OUT))
            {
                string result = PipeResult;
                return result.ToObject<JMessage>();
            }
            else
            {
                throw new JavaException(eErrorMessageType.Error_WaitTimeout);
            }
        }

        private void Pipeclient_Connected(NamedPipeConnection<Core.Entities.JavaMessage, Core.Entities.JavaMessage> connection)
        {
            logger.LogInformation("Pipeclient_Connected 连接成功");
            _initWait.Set();
        }

        private void Pipeclient_ServerMessage(NamedPipeConnection<Core.Entities.JavaMessage, Core.Entities.JavaMessage> connection, Core.Entities.JavaMessage message)
        {
            PipeResult = message.excuteResult;
            _waitEvent.Set();
        }
    }
}