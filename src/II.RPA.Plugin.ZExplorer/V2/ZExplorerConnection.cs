using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.ZExplorer.Cef;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Threading;

namespace II.RPA.Plugin.ZExplorer.V2
{
    public class ZExplorerConnection : BrowserCrxComparator, IBrowserConnectionNew
    {
        private readonly static Lazy<ZExplorerConnection> _instance = new Lazy<ZExplorerConnection>(() => new ZExplorerConnection());
        private ILogger logger = Logger.OfContext();
        private ZExplorerConnection()
        {
            this.ClientConnector = new CdpClientConnector();
        }
        public static ZExplorerConnection Instance => _instance.Value;
        //连接器
        private readonly CdpClientConnector ClientConnector;
        public bool IsConnected
        {
            get
            {
                try
                {
                    if (!ClientConnector.Connected)
                    {
                        GetRuningZExplorerData("ZexplorerReady");
                        ClientConnector.Connect(CurrentPort);
                    }
                    return ClientConnector.Connected;
                }
                catch (Exception ex)
                {
                    logger.LogWarning("background page connection lost..!");
                    logger.LogError(ex);
                    return false;
                }

            }
            set { }
        }
        public InitScripeStateType InitScripeState { get; set; }

        public PipeResponse PipeResponse { get; set; }

        public string CrxVersion { get; set; }
        public bool CrxIsLatestOrNewer => IsLatestOrNewer(CrxVersion);
        public int ConnectTimeout { get; set; }

        public PipeResponse Send(eGuiType guiType, string command, JObject @params, int timeout = 5000)
        {
            if (IsConnected)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                var pipeRequest = new PipeRequest
                {
                    Method = command,
                    Params = @params,
                    CodeId = ExtensionProvider.Instance.CodeId,
                    Options = new JObject(new JProperty("requestId", DateTime.Now.Ticks.ToString()))
                };
                var script = $"RpaRequest({pipeRequest.ToJson()})";
                logger.LogDebug($"request timeout:{timeout},script:{script}");
                DateTime startTime = DateTime.Now;
                CefResult<JToken> cefResult = ClientConnector.Evaluate(script, timeout);
                logger.LogInformation($"{command} 耗时：{stopwatch.ElapsedMilliseconds}");
                if (cefResult.IsSuccess)
                {
                    var result = cefResult.Success.Value<string>();
                    logger.LogInformation($"{command}: {result}");
                    return JsonConvert.DeserializeObject<PipeResponse>(result);
                }
                else
                {
                    logger.LogInformation("Request timed out or returned null value.");
                    logger.LogInformation(JsonConvert.SerializeObject(cefResult));
                    if ("elements.buildSelector, elements.buildCssSelector".Contains(command))
                    {
                        throw new TimeoutException();
                    }
                    else
                    {
                        return null;
                    }

                }
            }
            else
            {
                logger.LogInformation("Service Disconnect!");
                throw new ZExplorerException(eErrorMessageType.Error_ZEXPLORERNOTFOUND);
            }
        }

        public void Start()
        {

        }

        public void Stop()
        {

        }

        public bool TestConnection()
        {
            return true;
        }

        public bool TestConnection(eGuiType guiType, int timeOut = 300)
        {
            try
            {
                return ClientConnector.Connected;
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                return false;
            }
        }

        public SharedZexploreData RuntimeData { get; set; }//正在运行的浏览器共享信息
        public int ProcessId { get; set; }
        public int CurrentPort { get; set; }
        public void GetRuningZExplorerData(string eventName = "ZexplorerTabCreated", int timeout = 5000)
        {
            logger.LogInformation($"get zexplorer data :{eventName},timeout:{timeout}");

            const int pollIntervalMs = 100;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < timeout)
            {
                EventWaitHandle eventHandler = null;
                try
                {
                    eventHandler = EventWaitHandle.OpenExisting(eventName);
                    int waitRemaining = Math.Max(0, timeout - (int)stopwatch.ElapsedMilliseconds);
                    logger.LogDebug($"remain time:{waitRemaining}");
                    bool completed = eventHandler.WaitOne(waitRemaining);

                    if (!completed)
                    {
                        throw new RPAException(eGuiType.ZExplorer, eErrorMessageType.Error_WaitTimeout, eComponentType.Capture);
                    }
                    ReadZExplorerData(eventName);
                    eventHandler.Dispose();
                    return;
                }
                catch (WaitHandleCannotBeOpenedException ex)
                {
                    Thread.Sleep(pollIntervalMs);
                }
            }

            throw new ZExplorerException(eErrorMessageType.Error_ZEXPLORERNOTFOUND);
        }
        private bool ReadZExplorerData(string eventName)
        {
            MemoryMappedFile mmf = null;
            MemoryMappedViewAccessor accessor = null;

            try
            {
                mmf = MemoryMappedFile.OpenExisting("ZexplorerData");
                accessor = mmf.CreateViewAccessor();
                SharedZexploreData data;
                accessor.Read(0, out data);
                RuntimeData = data;
                CurrentPort = data.Port;
                ProcessId = data.PId;

                logger.LogInformation($"event name:{eventName},read shared data: {JsonConvert.SerializeObject(data)}");
                return true;
            }
            catch
            {
                logger.LogInformation($"event name:{eventName},read shared data file not exist!");
                return false;
            }
            finally
            {
                if (accessor != null) accessor.Dispose();
                if (mmf != null) mmf.Dispose();
            }
        }
    }
}
