using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.NamedPipeWrapper;
using II.RPA.WinAutomation.CommonUtility;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Logging.Formatting;

namespace II.RPA.JavaBridgeHost.IPC
{
    class PipeWrapService
    {
        private static ILogger logger = Logger.OfContext();
        private NamedPipeServer<JavaMessage> pipe { get; set; }
        private const string PIPE_NAME = "c5e9b4c4ee837260dd147d34d1c7cfe4ee1e043b";
        private object _lock = new object();
        private IExecuteMessageable execute;
        public PipeWrapService(IExecuteMessageable execute)
        {
            this.execute = execute;
        }

        public void StartServer()
        {
            try
            {
                lock (_lock)
                {
                    if (pipe == null)
                    {
                        var id = System.Diagnostics.Process.GetCurrentProcess().Id;
                        pipe = new NamedPipeServer<JavaMessage>($"{PIPE_NAME}_{id}");
                        pipe.ClientConnected += Pipe_ClientConnected;
                        pipe.ClientDisconnected += Pipe_ClientDisconnected;
                        pipe.ClientMessage += Server_OnReceivedMessage;
                        pipe.Start();
                        logger.LogInformation("Pipe start");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
        }

        public void Close()
        {
            try
            {
                this.pipe.Stop();
                this.pipe = null;
            }
            catch
            { }
        }

        private void Server_OnReceivedMessage(NamedPipeConnection<JavaMessage, JavaMessage> connection, JavaMessage message)
        {
            var block = LogBlock.Rows(
                default,
                message.action.Tag("action"),
                message.selector.Tag("selector"));

            logger.LogDebug($"JavaReceivedAction: {block}");

            var ret = execute.ExecuteReceiveMessage(message);
            this.pipe.PushMessage(ret);
        }

        private void Pipe_ClientDisconnected(NamedPipeConnection<JavaMessage, JavaMessage> connection)
        {
            logger.LogInformation("Client Disconnect:");
        }

        private void Pipe_ClientConnected(NamedPipeConnection<JavaMessage, JavaMessage> connection)
        {
            logger.LogInformation("Client Connected:");
        }
    }
}
