using II.RPA.Logging;
using MasterDevs.ChromeDevTools;
using MasterDevs.ChromeDevTools.Protocol.Chrome.Runtime;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using WebSocket4Net;

namespace II.RPA.Plugin.ZExplorer.Cef
{
    internal class ChromeSessionConnection : IDisposable
    {
        private ILogger logger = Logger.OfContext();
        private ChromeSession _chromeSession;
        private static ChromeSessionFactory _chromeSessionFactory = new ChromeSessionFactory();
        public ChromeSessionConnection(string socketUrl)
        {
            _chromeSession = _chromeSessionFactory.Create(socketUrl) as ChromeSession;
        }
        public Task<CommandResponse<EvaluateCommandResponse>> Send(string script)
        {
            var result = _chromeSession.SendAsync(new EvaluateCommand()
            {
                ReturnByValue = true,
                AwaitPromise = true,
                Expression = script
            });
            return result;
        }
        public bool? Connected
        {
            get
            {
                try
                {
                    logger.LogInformation($"SocketState: {_chromeSession?.SocketState}");
                    if (_chromeSession.SocketState == WebSocketState.Closed)
                    {
                        Dispose();
                        return false;
                    }
                    return _chromeSession.SocketState == WebSocketState.Open;

                }
                catch (Exception ex)
                {
                    logger.LogDebug($"socket connection error:{ex.Message}");
                    return null;
                }

            }
        }
        public void Dispose()
        {
            try
            {
                _chromeSession.Dispose();
            }
            catch (Exception)
            {
            }
        }
    }
}
