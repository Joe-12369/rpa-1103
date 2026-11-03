using II.RPA.Plugin.Core;
using II.RPA.WebBrowser.Common.Basic.connection;
using II.RPA.WebBrowser.Common.Basic.connection.ClientTransfer;
using II.RPA.WebBrowser.Common.Basic.connection.Entities;
using II.RPA.WebBrowser.Common.Basic.connection.ServiceHub;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Threading;

namespace II.RPA.WebBrowser.Common.Basic
{
    internal class ServiceHubConnection : BrowserCrxComparator, IBrowserConnectionNew
    {
        private static readonly Lazy<ServiceHubConnection> LazyInstance =
            new Lazy<ServiceHubConnection>(CreateDefault);
        private readonly IServiceHubSessionFactory _sessionFactory;
        private readonly ClientTransferRequestDispatcher _dispatcher = new ClientTransferRequestDispatcher();
        private readonly object _lifecycleGate = new object();
        private IServiceHubSession _session;
        private long _generation;
        private int _stopped;

        public static ServiceHubConnection Instance => LazyInstance.Value;

        internal ServiceHubConnection(IServiceHubSessionFactory sessionFactory)
        {
            _sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
        }

        public string CrxVersion { get; set; }
        public bool CrxIsLatestOrNewer => IsLatestOrNewer(CrxVersion);
        public InitScripeStateType InitScripeState { get; set; } = InitScripeStateType.Complete;
        public int ConnectTimeout { get; set; }
        public PipeResponse PipeResponse { get; set; }
        public ConnectionResult ConnectionResult { get; set; } = new ConnectionResult();
        public bool IsConnected
        {
            get { lock (_lifecycleGate) return _session?.IsConnected == true; }
            set { }
        }

        public void Start()
        {
            lock (_lifecycleGate)
            {
                if (_session?.IsConnected == true) return;
                if (Volatile.Read(ref _stopped) != 0)
                    throw new BrowserConnectionException("The ServiceHub logical connection is stopped.", ClientTransferCompletionReason.Stopped);

                IServiceHubSession old = _session;
                int budget = ConnectTimeout > 0 ? ConnectTimeout : GlobleConfigurations.InitConnectTimeout;
                var stopwatch = Stopwatch.StartNew();
                Exception lastError = null;
                for (int attempt = 0; attempt < 10 && stopwatch.ElapsedMilliseconds < budget; attempt++)
                {
                    long generation = ++_generation;
                    IServiceHubSession session = null;
                    try
                    {
                        session = _sessionFactory.Create(attempt);
                        session.MessageReceived += message => OnMessage(session, generation, message);
                        session.Disconnected += error => OnDisconnected(session, generation, error);
                        int remaining = Math.Max(1, budget - (int)stopwatch.ElapsedMilliseconds);
                        session.Connect(remaining);
                        if (!session.IsConnected)
                            throw new BrowserConnectionException(
                                "The ServiceHub session disconnected before it could be published.",
                                ClientTransferCompletionReason.ConnectionClosed);
                        _session = session;
                        ConnectionResult.IsConnected = true;
                        ConnectionResult.HasTried = true;
                        old?.Dispose();
                        return;
                    }
                    catch (Exception error)
                    {
                        lastError = error;
                        session?.Dispose();
                    }
                }
                ConnectionResult.IsConnected = false;
                ConnectionResult.HasTried = true;
                throw new BrowserConnectionException(
                    "ServiceHub did not become ready within the connection timeout.",
                    ClientTransferCompletionReason.ConnectionClosed,
                    lastError);
            }
        }

        public bool InitServiceHubConnection(int retryCount = 0)
        {
            try { Start(); return true; } catch { return false; }
        }

        public System.Threading.Tasks.Task<bool> InitConnection(int timeout)
        {
            ConnectTimeout = timeout;
            return System.Threading.Tasks.Task.FromResult(InitServiceHubConnection());
        }

        public PipeResponse Send(eGuiType guiType, string command, JObject args, int timeout = 5000)
        {
            if (!IsConnected) Start();
            return SendToServiceHub(guiType, command, args, timeout);
        }

        public PipeResponse SendToServiceHub(eGuiType guiType, string command, object args, int timeout)
        {
            IServiceHubSession session;
            lock (_lifecycleGate)
            {
                session = _session;
                if (session?.IsConnected != true)
                    throw new BrowserConnectionException(
                        "ServiceHub is not connected.",
                        ClientTransferCompletionReason.ConnectionClosed);
            }

            var request = new PipeRequest
            {
                Method = command,
                Params = args == null ? new JObject() : (JObject)JsonHelper.JsonDesByNewtonJson(args.ToString()),
                CodeId = ExtensionProvider.Instance.CodeId
            };
            //V2版本定制TargetClientFlag  V3全部使用Chrome,SH已成极端通信本分，需要定制Flag时 在提新需求开发
            var message = new TransferMessage
            {
                MessageId = Guid.NewGuid().ToString("N"),
                TargetClientType = SignalRClientType.Web,
                TargetClientFlag = IsCrxV2(CrxVersion)
                    ? guiType.ToString()
                    : eGuiType.Chrome.ToString(),
                SendData = request,
                Timeout = timeout,
                Timestamp = Stopwatch.GetTimestamp()
            };
            ClientTransferEntry entry = _dispatcher.Register(message.MessageId, Math.Max(0, timeout));

            ServiceHubSendResult send = session.SendAsync(message).GetAwaiter().GetResult();
            if (!send.InvocationStarted)
                entry.TryComplete(new ClientTransferDispatchResult(
                    ClientTransferCompletionReason.SendFailed,
                    exception: send.Exception));

            ClientTransferDispatchResult result = entry.Completion.GetAwaiter().GetResult();
            if (result.Reason == ClientTransferCompletionReason.Response)
                return PipeResponse = ((JToken)result.Response.CallbackReslut).ToObject<PipeResponse>();
            if (result.Reason == ClientTransferCompletionReason.Timeout)
            {
                throw new BrowserConnectionException(
                    "The ServiceHub request timed out.",
                    ClientTransferCompletionReason.Timeout,
                    result.Exception);
            }
            throw new BrowserConnectionException("The ServiceHub request did not complete.", result.Reason, result.Exception);
        }

        private static bool IsCrxV2(string crxVersion)
        {
            return !string.IsNullOrWhiteSpace(crxVersion) &&
                   crxVersion.StartsWith("2.", StringComparison.Ordinal);
        }

        private void OnMessage(IServiceHubSession session, long generation, TransferMessage message)
        {
            // Responses identify logical requests by MessageId. They remain valid across physical
            // generations, including a response already queued by the replaced session.
            _dispatcher.TryComplete(message);
        }

        private void OnDisconnected(IServiceHubSession session, long generation, Exception error)
        {
            lock (_lifecycleGate)
            {
                if (!ReferenceEquals(_session, session) || _generation != generation) return;
                _session = null;
                ConnectionResult.IsConnected = false;
            }
            session.Dispose();
        }

        public void Stop()
        {
            if (Interlocked.Exchange(ref _stopped, 1) != 0) return;
            _dispatcher.CompleteAll(ClientTransferCompletionReason.Stopped);
            IServiceHubSession session;
            lock (_lifecycleGate) { session = _session; _session = null; }
            session?.Dispose();
        }

        public bool TestConnection() => IsConnected;
        public bool TestConnection(eGuiType guiType, int timeout = 3000)
        {
            PipeResponse result = Send(guiType, "ping", new JObject(), timeout);
            return result != null && result.Code == 200;
        }

        private static ServiceHubConnection CreateDefault()
        {
            ServiceHubConnection logical = null;
            var factory = new SignalRServiceHubSessionFactory(
                attempt => string.Format("http://127.0.0.1:{0}/rpa", GlobleConfigurations.ServiceHubPort + attempt % 10),
                GetDefaultClientType);
            logical = new ServiceHubConnection(factory);
            return logical;
        }

        private static SignalRClientType GetDefaultClientType()
        {
            SignalRClientType type;
            switch (GlobleConfigurations.ClientType)
            {
                case ClientType.Factory: type = SignalRClientType.Factory; break;
                case ClientType.Bot: type = SignalRClientType.Bot; break;
                case ClientType.DeskTop: type = SignalRClientType.DeskTop; break;
                default: type = SignalRClientType.ZAdmin; break;
            }
            string processName = Process.GetCurrentProcess().ProcessName;
            if (processName.Contains("CoreServer")) type |= SignalRClientType.CoreServer;
            else if (processName.Contains("PluginProcess")) type |= SignalRClientType.PluginProcess;
            return type;
        }
    }
}

