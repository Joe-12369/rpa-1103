using II.RPA.IPC;
using II.RPA.IPC.Config;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.WebBrowser.Common.Basic.connection;
using II.RPA.WinAutomation.CommonUtility;
using II.RPA.WinAutomation.CommonUtility.Utils;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.WebBrowser.Common.Basic
{
    public class ConnectionConfig
    {
        internal const string UnifiedNamedPipeServiceKey = "U2FsdGVkX19Xagmra6VZ0BB2FitqhBx8";

        public eGuiType GuiType { get; set; }
        public string NativeMessageHostProcessName { get; set; }
        public int NativeMessageHostProcessID { get; set; }
        public string BrowserProcessName { get; set; }
        public string ServiceKey { get; set; }
        public string WcfServiceName => $"{ServiceKey}_{NativeMessageHostProcessID}";

        public string NamedPipeServerName =>
            $"{UnifiedNamedPipeServiceKey}_{NativeMessageHostProcessID}";
        /// <summary>
        /// 新IPC服务地址
        /// </summary>
        public string IPCServiceAddress
        {
            get
            {
                return $"net.pipe://localhost/SZ_RPAService/{WcfServiceName}";
            }
        }
        /// <summary>
        /// SignalRUrl
        /// </summary>
        public string SignalRUrl { get; set; }

        [JsonIgnore]
        public Type ServiceConnectionType { get; set; }

        [JsonIgnore]
        public Type NamePipeConnectionType { get; set; }

        [JsonIgnore]
        public Type SignalRConnectionType { get; set; }

        [JsonIgnore]
        public Func<bool> CheckBrowserExtensions = null;

        [JsonIgnore]
        public Func<bool, int, int> DetectionMessageProcess = null;
        public eErrorMessageType ExtensionUnInstalledErrorMessageType { get; set; }
        public eErrorMessageType ExtensionUnopenErrorMessageType { get; set; }
        public int ConnectTimeout { get; set; }
        public bool IgnoreConnection { get; set; }
    }
    public class ConnectionFactory
    {
        private const string _SERVICEHUBPROCESSNAME = "II.RPA.ServiceHub";
        protected static ILogger logger = Logger.OfContext();
        private static readonly NmhConnectionFactory _nmhConnectionFactory =
            new NmhConnectionFactory();
        public static ConnectionFactory Instance { get; private set; }


        protected static readonly object objConnecttionLock = new object();
        public static IBrowserConnectionNew GetConnection(ConnectionConfig connConfig)
        {
            TimeTrackHelper timeTrackHelper = new TimeTrackHelper();
            timeTrackHelper.Start($"WebData GetConnection", TimeTrackLogLevel.info);
            lock (objConnecttionLock)
            {
                IBrowserConnectionNew conn = null;
                connConfig.CheckBrowserExtensions?.Invoke();
                timeTrackHelper.Pick("WebData GetConnection 1", TimeTrackLogLevel.info);

                if ((GlobleConfigurations.BrowserConnType & BrowserConnType.NativeMessageHost) != 0
                    && !string.IsNullOrEmpty(connConfig.NativeMessageHostProcessName))
                {
                    try
                    {
                        connConfig.NativeMessageHostProcessID = connConfig.DetectionMessageProcess(false, 10);//isServiceHub fasle
                        timeTrackHelper.Pick("WebData GetConnection 5", TimeTrackLogLevel.info);
                        if (connConfig.NativeMessageHostProcessID == 0)
                        {
                            timeTrackHelper.Stop("WebData GetConnection");
                            throw new RPAException(connConfig.GuiType, connConfig.ExtensionUnopenErrorMessageType);
                        }
                        timeTrackHelper.Stop("WebData GetConnection");
                        return _nmhConnectionFactory.GetConnection(connConfig);
                    }
                    finally
                    {
                        timeTrackHelper.Pick("WebData GetConnection 6", TimeTrackLogLevel.info);
                        timeTrackHelper.Stop("WebData GetConnection");
                    }
                }

                if ((GlobleConfigurations.BrowserConnType & BrowserConnType.ServiceHub) != 0)
                {
                    var processArr = Process.GetProcessesByName(_SERVICEHUBPROCESSNAME);
                    timeTrackHelper.Pick("WebData GetConnection 2", TimeTrackLogLevel.info);
                    if (processArr.Length == 0 && GlobleConfigurations.BrowserConnType == BrowserConnType.ServiceHub)
                    {
                        timeTrackHelper.Pick("WebData GetConnection 3", TimeTrackLogLevel.info);
                        timeTrackHelper.Stop("WebData GetConnection");
                        logger.LogError($"配置的浏览器连接类型为SH,但II.RPA.ServiceHub服务未启动，如果已经安装，可以在服务里边中寻找并启动此服务，或者将连接类型设置为AUTO");
                        throw new RPAException(connConfig.GuiType, eErrorMessageType.Error_ServerHubUnopen);
                    }
                    processArr.Dispose();
                    conn = ConnectionFactory.GetServiceHubConnection(connConfig);
                    timeTrackHelper.Pick("WebData GetConnection 4", TimeTrackLogLevel.info);
                    if (conn != null)
                    {
                        timeTrackHelper.Stop("WebData GetConnection");
                        return conn;
                    }
                }

                timeTrackHelper.Stop("WebData GetConnection");
                return conn;
            }
        }

        private static IBrowserConnectionNew GetServiceHubConnection(ConnectionConfig connConfig)
        {
            return GetServiceHubConnection(connConfig, ServiceHubConnection.Instance);
        }

        internal static IBrowserConnectionNew GetServiceHubConnection(
            ConnectionConfig connConfig,
            IBrowserConnectionNew connection)
        {
            try
            {
                connection.ConnectTimeout = connConfig.ConnectTimeout;
                if (!connection.IsConnected)
                {
                    connection.Start();
                }

                if (connection.IsConnected && connection.TestConnection(connConfig.GuiType, 300))
                {
                    return connection;
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "Fail to generate connection with ServiceHub.");
            }
            return null;
        }

        public static bool VerifyServiceHubConnection()
        {
            return ServiceHubConnection.Instance.IsConnected;
        }

        public static IBrowserConnectionNew GetConnection()
        {
            return ServiceHubConnection.Instance;
        }

    }
}
