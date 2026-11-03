using II.RPA.Plugin.Core;
using II.RPA.WebBrowser.Common.Basic.connection;
using II.RPA.WebBrowser.Common.Basic.connection.ClientTransfer;
using II.RPA.WebBrowser.Common.Basic.connection.NamedPipe;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.WebBrowser.Common.Basic
{
    internal class NamePipeConnection : BrowserCrxComparator, IBrowserConnectionNew
    {
        private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);
        private readonly string _pipeName;
        private readonly string _nmhPid;
        private readonly INamedPipeClientAdapterFactory _clientFactory;
        private readonly INamedPipeRequestDiagnosticSink _diagnostics;
        private readonly object _lifecycleGate = new object();
        private INamedPipeClientAdapter _client;
        private ClientTransferRequestDispatcher _dispatcher;
        private int _connectionState;

        public NamePipeConnection(string pipeName)
            : this(pipeName, new NamedPipeClientAdapterFactory())
        {
        }

        internal NamePipeConnection(
            string pipeName,
            INamedPipeClientAdapterFactory clientFactory)
            : this(pipeName, clientFactory, new NamedPipeRequestDiagnosticSink())
        {
        }

        internal NamePipeConnection(
            string pipeName,
            INamedPipeClientAdapterFactory clientFactory,
            INamedPipeRequestDiagnosticSink diagnostics)
        {
            if (string.IsNullOrWhiteSpace(pipeName))
            {
                throw new ArgumentException("A pipe name is required.", nameof(pipeName));
            }

            _pipeName = pipeName;
            _nmhPid = ExtractNmhPid(pipeName);
            _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
            _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        }

        public string CrxVersion { get; set; }

        public bool CrxIsLatestOrNewer => IsLatestOrNewer(CrxVersion);

        public int ConnectTimeout { get; set; }

        public bool IsConnected
        {
            get
            {
                INamedPipeClientAdapter client = _client;
                return Volatile.Read(ref _connectionState) != 0 && client?.IsConnected == true;
            }
            set { }
        }

        public PipeResponse PipeResponse { get; private set; }

        public InitScripeStateType InitScripeState { get; set; } = InitScripeStateType.None;

        public void Start()
        {
            if (IsConnected)
            {
                return;
            }

            int budget = ConnectTimeout > 0 ? ConnectTimeout : 10000;
            INamedPipeClientAdapter client;
            lock (_lifecycleGate)
            {
                _dispatcher = new ClientTransferRequestDispatcher();
                client = _clientFactory.Create(_pipeName, TimeSpan.FromMilliseconds(budget));
                _client = client;
                client.MessageReceived += ClientMessageReceived;
                client.Disconnected += ClientDisconnected;
            }

            try
            {
                using (var cancellation = new CancellationTokenSource(budget))
                {
                    var stopwatch = Stopwatch.StartNew();
                    client.ConnectAsync(cancellation.Token).GetAwaiter().GetResult();
                    int remaining = Math.Max(1, budget - (int)stopwatch.ElapsedMilliseconds);
                    client.PingAsync(TimeSpan.FromMilliseconds(remaining), cancellation.Token)
                        .GetAwaiter()
                        .GetResult();
                }

                if (!client.IsConnected || _dispatcher.IsClosed)
                {
                    throw new InvalidOperationException("The named pipe disconnected during startup.");
                }

                Volatile.Write(ref _connectionState, 1);
            }
            catch (Exception ex)
            {
                CloseConnection(ClientTransferCompletionReason.ConnectionClosed, ex, true);
                throw new BrowserConnectionException(
                    "Named Pipe did not become ready within the connection timeout.",
                    ClientTransferCompletionReason.ConnectionClosed,
                    ex);
            }
        }

        public void Stop()
        {
            CloseConnection(ClientTransferCompletionReason.Stopped, null, false);
            INamedPipeClientAdapter client;
            lock (_lifecycleGate)
            {
                client = _client;
                _client = null;
            }

            if (client != null)
            {
                client.MessageReceived -= ClientMessageReceived;
                client.Disconnected -= ClientDisconnected;
                try { client.DisconnectAsync().GetAwaiter().GetResult(); } catch { }
                client.Dispose();
            }
        }

        public bool TestConnection()
        {
            return IsConnected;
        }

        public bool TestConnection(eGuiType guiType, int timeout = 3000)
        {
            if (!IsConnected)
            {
                return false;
            }

            try
            {
                _client.PingAsync(
                    TimeSpan.FromMilliseconds(Math.Max(1, timeout)),
                    CancellationToken.None).GetAwaiter().GetResult();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public PipeResponse Send(
            eGuiType guiType,
            string command,
            JObject @params,
            int timeout = 2000)
        {
            if (InitScripeState == InitScripeStateType.None &&
                command != "getVersion" &&
                command != "getWindowMap"&&
                command != "ping")
            {
                return CompatibilityNotInitialized();
            }

            INamedPipeClientAdapter client = _client;
            ClientTransferRequestDispatcher dispatcher = _dispatcher;
            if (!IsConnected || client == null || dispatcher == null)
            {
                throw new BrowserConnectionException(
                    "The Named Pipe connection is not connected.",
                    ClientTransferCompletionReason.ConnectionClosed);
            }

            var requestStopwatch = Stopwatch.StartNew();

            int requestTimeout = Math.Max(0, timeout);
            var pipeRequest = new PipeRequest
            {
                Method = command,
                Params = @params == null
                    ? new JObject()
                    : (JObject)JsonHelper.JsonDesByNewtonJson(@params.ToString()),
                CodeId = ExtensionProvider.Instance.CodeId
            };
            var message = new TransferMessage
            {
                MessageId = Guid.NewGuid().ToString("N"),
                Action = "SendTransferMessage",
                TargetClientType = SignalRClientType.Web,
                TargetClientFlag = guiType.ToString(),
                SendData = pipeRequest,
                Timeout = requestTimeout,
                Timestamp = Stopwatch.GetTimestamp()
            };
            ClientTransferEntry entry = dispatcher.Register(message.MessageId, requestTimeout);
            string stage = "PrepareRequest";

            try
            {
                byte[] payload = StrictUtf8.GetBytes(JsonConvert.SerializeObject(message));
                stage = "SendPipe";
                client.SendAsync(payload, CancellationToken.None).GetAwaiter().GetResult();
                stage = "WaitResponse";
            }
            catch (Exception ex)
            {
                CloseConnection(ClientTransferCompletionReason.SendFailed, ex, true);
            }

            ClientTransferDispatchResult result = entry.Completion.GetAwaiter().GetResult();
            switch (result.Reason)
            {
                case ClientTransferCompletionReason.Response:
                    PipeResponse = ConvertResponse(result.Response);
                    requestStopwatch.Stop();
                    SafeWriteDiagnostic(
                        NamedPipeDiagnosticLevel.Debug,
                        $"[NMH.NamedPipe.Send] NmhPid={_nmhPid} Command={command} " +
                        $"ElapsedMs={requestStopwatch.Elapsed.TotalMilliseconds:F3} Result=Success");
                    return PipeResponse;
                case ClientTransferCompletionReason.Timeout:
                    WriteFailureDiagnostic(
                        NamedPipeDiagnosticLevel.Debug,
                        command,
                        stage,
                        result,
                        requestTimeout,
                        requestStopwatch);
                    throw new BrowserConnectionException(
                        "The Named Pipe request timed out.",
                        ClientTransferCompletionReason.Timeout,
                        result.Exception);
                default:
                    NamedPipeDiagnosticLevel level =
                        result.Reason == ClientTransferCompletionReason.SendFailed ||
                        result.Reason == ClientTransferCompletionReason.ProtocolError
                            ? NamedPipeDiagnosticLevel.Error
                            : NamedPipeDiagnosticLevel.Warning;
                    WriteFailureDiagnostic(
                        level,
                        command,
                        stage,
                        result,
                        requestTimeout,
                        requestStopwatch);
                    throw new BrowserConnectionException(
                        "The Named Pipe request failed because the connection is unavailable.",
                        result.Reason,
                        result.Exception);
            }
        }

        private void WriteFailureDiagnostic(
            NamedPipeDiagnosticLevel level,
            string command,
            string stage,
            ClientTransferDispatchResult result,
            int timeout,
            Stopwatch stopwatch)
        {
            stopwatch.Stop();
            Exception exception = result.Exception;
            string exceptionType = exception == null
                ? "None"
                : exception.GetType().Name;
            string exceptionMessage = exception == null
                ? "None"
                : SanitizeExceptionMessage(exception.Message);

            SafeWriteDiagnostic(
                level,
                $"[NMH.NamedPipe.SendFailed] NmhPid={_nmhPid} Command={command} " +
                $"Stage={stage} Reason={result.Reason} TimeoutMs={timeout} " +
                $"ElapsedMs={stopwatch.Elapsed.TotalMilliseconds:F3} " +
                $"ExceptionType={exceptionType} ExceptionMessage={exceptionMessage}",
                exception);
        }

        private string SanitizeExceptionMessage(string message)
        {
            string sanitized = (message ?? string.Empty)
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Replace(_pipeName, "[PipeAddress]");
            int separator = _pipeName.LastIndexOf('_');
            if (separator > 0)
            {
                sanitized = sanitized.Replace(
                    _pipeName.Substring(0, separator),
                    "[ServerKey]");
            }

            return sanitized;
        }

        private static string ExtractNmhPid(string pipeName)
        {
            int separator = pipeName.LastIndexOf('_');
            if (separator < 0 || separator == pipeName.Length - 1)
            {
                return "Unknown";
            }

            string suffix = pipeName.Substring(separator + 1);
            return int.TryParse(suffix, out int pid) && pid >= 0
                ? pid.ToString()
                : "Unknown";
        }

        private void SafeWriteDiagnostic(
            NamedPipeDiagnosticLevel level,
            string message,
            Exception exception = null)
        {
            try { _diagnostics.Write(level, message, exception); }
            catch { }
        }

        private void ClientMessageReceived(byte[] payload)
        {
            try
            {
                TransferMessage response = ParseResponse(payload);
                _dispatcher?.TryComplete(response);
            }
            catch (Exception ex)
            {
                CloseConnection(ClientTransferCompletionReason.ProtocolError, ex, true);
            }
        }

        private void ClientDisconnected()
        {
            CloseConnection(
                ClientTransferCompletionReason.ConnectionClosed,
                new InvalidOperationException("The Named Pipe stream was disconnected."),
                false);
        }

        private void CloseConnection(
            ClientTransferCompletionReason reason,
            Exception exception,
            bool disconnectInBackground)
        {
            Volatile.Write(ref _connectionState, 0);
            InitScripeState = InitScripeStateType.None;
            _dispatcher?.CompleteAll(reason, exception);

            INamedPipeClientAdapter client = _client;
            if (disconnectInBackground && client != null)
            {
                Task.Run(async () =>
                {
                    try { await client.DisconnectAsync().ConfigureAwait(false); } catch { }
                });
            }
        }

        private static TransferMessage ParseResponse(byte[] payload)
        {
            if (payload == null)
            {
                throw new JsonSerializationException("The Named Pipe response payload is null.");
            }

            string json = StrictUtf8.GetString(payload);
            JObject root = JObject.Parse(json);
            string messageId = root.Value<string>(nameof(TransferMessage.MessageId));
            if (string.IsNullOrWhiteSpace(messageId) || !Guid.TryParse(messageId, out _))
            {
                throw new JsonSerializationException("A valid TransferMessage MessageId is required.");
            }

            JToken callback = root[nameof(TransferMessage.CallbackReslut)];
            if (!(callback is JObject callbackObject) ||
                callbackObject["code"]?.Type != JTokenType.Integer ||
                !(callbackObject["result"] is JObject))
            {
                throw new JsonSerializationException(
                    "TransferMessage CallbackReslut must contain integer code and object result fields.");
            }

            TransferMessage response = root.ToObject<TransferMessage>();
            if (response == null)
            {
                throw new JsonSerializationException("A TransferMessage response is required.");
            }

            return response;
        }

        private static PipeResponse ConvertResponse(TransferMessage response)
        {
            if (response?.CallbackReslut == null)
            {
                throw new BrowserConnectionException(
                    "The Named Pipe response does not contain CallbackReslut.",
                    ClientTransferCompletionReason.ProtocolError);
            }

            JToken callback = response.CallbackReslut as JToken
                ?? JToken.FromObject(response.CallbackReslut);
            return callback.ToObject<PipeResponse>();
        }

        private static PipeResponse CompatibilityNotInitialized()
        {
            return new PipeResponse
            {
                Code = 200,
                Result = new PipeResult
                {
                    Error = new RPCResponseError { Code = -100099 }
                }
            };
        }
    }
}

