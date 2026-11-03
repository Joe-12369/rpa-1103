using II.RPA.IPC;
using II.RPA.IPC.Config;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.WebBrowser.Common.Basic.connection;
using II.RPA.WebBrowser.Common.Basic.connection.ClientTransfer;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;

namespace II.RPA.WebBrowser.Common.Basic
{

    public class IPCConnection : BrowserCrxComparator, IBrowserConnectionNew
    {
        private ILogger logger = Logger.OfContext();
        private readonly string pipeName;
        private readonly IWcfConnectionProbeFactory _probeFactory;
        private readonly IConnectionWaitClock _waitClock;
        private int _connectionState;
        public string CrxVersion { get; set; }
        public bool CrxIsLatestOrNewer => IsLatestOrNewer(CrxVersion);

        public IPCConnection(string pipeName)
            : this(pipeName, new WcfConnectionProbeFactory(), new ConnectionWaitClock())
        {
        }

        internal IPCConnection(
            string pipeName,
            IWcfConnectionProbeFactory probeFactory,
            IConnectionWaitClock waitClock)
        {
            this.pipeName = pipeName;
            _probeFactory = probeFactory ?? throw new ArgumentNullException(nameof(probeFactory));
            _waitClock = waitClock ?? throw new ArgumentNullException(nameof(waitClock));
        }
        public InitScripeStateType InitScripeState { get; set; } = InitScripeStateType.None;
        public bool IsConnected
        {
            get { return Volatile.Read(ref _connectionState) == 1; }
            set
            { }
        }
        public int ConnectTimeout { get; set; }
        public bool TestConnection(eGuiType guiType, int timeout = 3000)
        {
            return TestConnection();
        }
        public bool TestConnection()
        {
            try
            {
                using (IWcfConnectionProbe probe = _probeFactory.Create(pipeName))
                {
                    probe.Pending(500);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
        public PipeResponse PipeResponse { get; }

        public PipeResponse Send(eGuiType guiType, string command, JObject @params, int timeout = 30000)
        {
            try
            {
                //兼容老版本crx,老版本crx使用规则种，每次和浏览器做有效数据交互式，必然是在初始化脚本之后；而最新版本：不主动过去codeID,被动获取未初始化脚本的逻辑
                if (this.InitScripeState == InitScripeStateType.None &&
                    RequiresInitializedScript(command))
                {
                    return new PipeResponse
                    {
                        Code = 200,
                        Result = new PipeResult
                        {
                            Error = new RPCResponseError
                            {
                                Code = -100099
                            }
                        }
                    };
                }
                var address = this.pipeName;
                PipeRequest pipeRequest = new PipeRequest();
                pipeRequest.Method = command;
                pipeRequest.Params = (JObject)JsonHelper.JsonDesByNewtonJson(@params.ToString());
                pipeRequest.CodeId = ExtensionProvider.Instance.CodeId;
                string submitStr = this.GetSubmitString(pipeRequest);

                IResponseResult iret = null;
                lock (this)
                {
                    var config = PipeConfig.DefaultPipe;
                    PipeConfig tempConfig = new PipeConfig()
                    {
                        CloseTimeout = config.CloseTimeout,
                        OpenTimeout = config.OpenTimeout,
                        SendTimeout = config.SendTimeout,
                        ReceiveTimeout = new TimeSpan(0, 0, 0, 0, timeout),
                        MaxBufferPoolSize = config.MaxBufferPoolSize,
                        MaxBufferSize = config.MaxBufferSize,
                        MaxConnections = config.MaxConnections,
                        MaxReceivedMessageSize = config.MaxReceivedMessageSize,
                        TransferMode = config.TransferMode
                    };
                    RequestParams param = new RequestParams();
                    if (string.IsNullOrEmpty(CrxVersion))
                    {
                        param.Parameters = submitStr;
                    }
                    else
                    {
                        TransferMessage message = new TransferMessage
                        {
                            MessageId = Guid.NewGuid().ToString("N"),
                            Action = "SendTransferMessage",
                            //TargetClientType = SignalRClientType.Web,
                            //TargetClientFlag = guiType.ToString(),
                            SendData = pipeRequest,
                            Timeout = timeout,
                            Timestamp = Stopwatch.GetTimestamp()  //后期要加上组件传递过来的时间戳
                        };
                        param.Parameters = JsonConvert.SerializeObject(message);
                    }
                    //logger.LogDebug($" init remoteClient {command},{address.Substring(address.LastIndexOf('_'))}");
                    logger.LogDebug($" init remoteClient {command},{address}");
                    using (var remoteClient = ClientGenerator.Instance.Generate<IMessageExchange>(tempConfig, address))
                    {
                        IContextChannel channel = remoteClient.Service as IContextChannel;
                        channel.OperationTimeout = new TimeSpan(0, 0, 0, 0, timeout);

                        //logger.LogDebug($" remoteClient ping");
                        //remoteClient.Service.Pending();
                        logger.LogDebug($" remoteClient Request");
                        iret = remoteClient.Service.Request(param);
                        logger.LogDebug($" remoteClient response");
                    }
                }
                if (iret != null && !string.IsNullOrEmpty(iret.Result))
                {
                    JObject paramsObj = JObject.Parse(iret.Result);
                    if (paramsObj.ContainsKey("MessageId"))
                    {
                        var retMsg = JsonConvert.DeserializeObject<TransferMessage>(iret.Result);
                        return retMsg?.CallbackReslut?.ToObject<PipeResponse>();
                    }
                    else
                    {
                        return JsonConvert.DeserializeObject<PipeResponse>(iret.Result);
                    }
                }
                if (iret?.Result == null)
                {
                    throw new TimeoutException("NativeMessagingHost返回值为null,判定为NativeMessagingHost与浏览器通信超时".I18n("controlType_IPCConnection_BD30A665"));
                }
                throw new IOException("The remote host has alreay replied,but the content was  empty!");
            }
            catch (EndpointNotFoundException ex)
            {
                logger.LogError($"Sending Eror EndpointNotFoundException。{ex}");
                throw ex;
            }
            catch (TimeoutException ex)
            {
                logger.LogError($"Chrome Sending Eror TimeoutException。{ex}");
                throw new BrowserConnectionException(
                    "The IPC request timed out.",
                    ClientTransferCompletionReason.Timeout,
                    ex);
            }
            catch (Exception ex)
            {
                logger.LogError($"Chrome Sending Eror。{ex}");
                return null;
            }
        }

        public void Start()
        {
            if (IsConnected)
            {
                return;
            }

            int budget = ConnectTimeout > 0 ? ConnectTimeout : 10000;
            Exception lastException = null;
            _waitClock.Restart();
            Volatile.Write(ref _connectionState, 0);

            while (_waitClock.ElapsedMilliseconds < budget)
            {
                int remaining = Math.Max(
                    1,
                    budget - (int)_waitClock.ElapsedMilliseconds);
                try
                {
                    using (IWcfConnectionProbe probe = _probeFactory.Create(pipeName))
                    {
                        probe.Pending(remaining);
                    }

                    Volatile.Write(ref _connectionState, 1);
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    logger.LogDebug($"连接异常，可能是尚未初始化完成，继续尝试 。{ex}");
                }

                int wait = Math.Min(
                    50,
                    Math.Max(0, budget - (int)_waitClock.ElapsedMilliseconds));
                _waitClock.Wait(wait);
            }

            throw new BrowserConnectionException(
                "WCF IPC did not become ready within the connection timeout.",
                ClientTransferCompletionReason.ConnectionClosed,
                lastException);
        }

        public void Stop()
        {
            Volatile.Write(ref _connectionState, 0);
            this.InitScripeState = InitScripeStateType.None;
        }

        private string GetSubmitString(PipeRequest obj)
        {
            string json = JsonConvert.SerializeObject(obj);
            return json;
        }

        internal static bool RequiresInitializedScript(string command)
        {
            return command != "getVersion" && command != "getWindowMap" && command != "ping";
        }

    }
}
