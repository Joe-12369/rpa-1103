using II.RPA.Plugin.Core;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.connection;
using II.RPA.WebBrowser.Common.Basic.connection.ClientTransfer;
using II.RPA.WebBrowser.Common.Basic.connection.NamedPipe;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace II.RPA.WebBrowser.Common.Tests
{
    public class NamePipeConnectionTests
    {
        [Fact]
        public void ConstructionAndIsConnectedReadPerformNoPipeIo()
        {
            var factory = new FakeNamedPipeClientFactory();
            var connection = new NamePipeConnection("pipe", factory);

            Assert.False(connection.IsConnected);
            Assert.False(connection.IsConnected);
            Assert.Equal(0, factory.CreateCount);
        }

        [Fact]
        public void StartConnectsAndPingsOneOwnedClient()
        {
            var factory = new FakeNamedPipeClientFactory();
            var connection = new NamePipeConnection("pipe", factory) { ConnectTimeout = 500 };

            connection.Start();

            Assert.True(connection.IsConnected);
            Assert.Equal(1, factory.CreateCount);
            Assert.Equal(1, factory.Client.ConnectCount);
            Assert.Equal(1, factory.Client.PingCount);
        }

        [Fact]
        public void NoneStateReturnsCompatibilityErrorWithoutSending()
        {
            var factory = new FakeNamedPipeClientFactory();
            var connection = Start(factory);

            PipeResponse response = connection.Send(eGuiType.Chrome, "command", new JObject(), 500);

            Assert.Equal(-100099, response.Result.Error.Code);
            Assert.Empty(factory.Client.SentPayloads);
        }

        [Theory]
        [InlineData("getVersion")]
        [InlineData("getWindowMap")]
        public void NoneStateAllowsReadinessCommands(string command)
        {
            var factory = new FakeNamedPipeClientFactory();
            factory.Client.SendAction = payload =>
                factory.Client.RaiseMessage(Response(payload, 200));
            var connection = Start(factory);

            PipeResponse response = connection.Send(eGuiType.Chrome, command, new JObject(), 2000);

            Assert.Equal(200, response.Code);
            Assert.Single(factory.Client.SentPayloads);
        }

        [Fact]
        public void SuccessfulSendWritesOneLowNoiseDiagnostic()
        {
            var factory = new FakeNamedPipeClientFactory();
            factory.Client.SendAction = payload =>
                factory.Client.RaiseMessage(Response(payload, 200));
            var sink = new CapturingDiagnosticSink();
            var connection = Start(factory, sink, "hidden-server-key_12345");

            PipeResponse response = connection.Send(
                eGuiType.Chrome,
                "getVersion",
                new JObject { ["secret"] = "must-not-log" },
                2000);

            Assert.Equal(200, response.Code);
            DiagnosticRecord record = Assert.Single(sink.Records);
            Assert.Equal(NamedPipeDiagnosticLevel.Debug, record.Level);
            Assert.Contains("[NMH.NamedPipe.Send]", record.Message);
            Assert.Contains("NmhPid=12345", record.Message);
            Assert.Contains("Command=getVersion", record.Message);
            Assert.Contains("ElapsedMs=", record.Message);
            Assert.Contains("Result=Success", record.Message);
            Assert.Null(record.Exception);
        }

        [Fact]
        public void SuccessfulDiagnosticDoesNotExposeIdentifiersAddressOrPayload()
        {
            var factory = new FakeNamedPipeClientFactory();
            factory.Client.SendAction = payload =>
                factory.Client.RaiseMessage(Response(payload, 200));
            var sink = new CapturingDiagnosticSink();
            var connection = Start(factory, sink, "private-server-key_24680");

            connection.Send(
                eGuiType.Chrome,
                "getVersion",
                new JObject { ["password"] = "top-secret" },
                2000);

            string log = Assert.Single(sink.Records).Message;
            TransferMessage sent = Request(factory.Client.SentPayloads.Single());
            Assert.DoesNotContain(sent.MessageId, log);
            Assert.DoesNotContain("private-server-key", log);
            Assert.DoesNotContain("top-secret", log);
            Assert.DoesNotContain("password", log);
            Assert.DoesNotContain("CallbackReslut", log);
        }

        [Fact]
        public void TimeoutWritesOneDetailedDebugDiagnostic()
        {
            var factory = new FakeNamedPipeClientFactory();
            var sink = new CapturingDiagnosticSink();
            var connection = Start(factory, sink, "key_13579");
            connection.InitScripeState = InitScripeStateType.Complete;

            BrowserConnectionException response = Assert.Throws<BrowserConnectionException>(() =>
                connection.Send(eGuiType.Chrome, "slow", new JObject(), 30));
            Assert.Equal(ClientTransferCompletionReason.Timeout, response.Reason);
            DiagnosticRecord record = Assert.Single(sink.Records);
            Assert.Equal(NamedPipeDiagnosticLevel.Debug, record.Level);
            Assert.Contains("[NMH.NamedPipe.SendFailed]", record.Message);
            Assert.Contains("NmhPid=13579", record.Message);
            Assert.Contains("Command=slow", record.Message);
            Assert.Contains("Stage=WaitResponse", record.Message);
            Assert.Contains("Reason=Timeout", record.Message);
            Assert.Contains("TimeoutMs=30", record.Message);
            Assert.Contains("ElapsedMs=", record.Message);
        }

        [Fact]
        public void SendFailureWritesOneDetailedErrorDiagnostic()
        {
            var factory = new FakeNamedPipeClientFactory();
            var expected = new InvalidOperationException("write failed");
            factory.Client.SendException = expected;
            var sink = new CapturingDiagnosticSink();
            var connection = Start(factory, sink, "key_97531");
            connection.InitScripeState = InitScripeStateType.Complete;

            Assert.Throws<BrowserConnectionException>(() =>
                connection.Send(eGuiType.Chrome, "click", new JObject(), 2000));

            DiagnosticRecord record = Assert.Single(sink.Records);
            Assert.Equal(NamedPipeDiagnosticLevel.Error, record.Level);
            Assert.Contains("NmhPid=97531", record.Message);
            Assert.Contains("Command=click", record.Message);
            Assert.Contains("Stage=SendPipe", record.Message);
            Assert.Contains("Reason=SendFailed", record.Message);
            Assert.Contains("ExceptionType=InvalidOperationException", record.Message);
            Assert.Same(expected, record.Exception);
        }

        [Fact]
        public void SendFailureDiagnosticDoesNotExposePipeAddress()
        {
            const string pipeName = "private-server-key_86420";
            var factory = new FakeNamedPipeClientFactory();
            factory.Client.SendException = new InvalidOperationException(
                $"Failed to write to {pipeName} using private-server-key.");
            var sink = new CapturingDiagnosticSink();
            var connection = Start(factory, sink, pipeName);
            connection.InitScripeState = InitScripeStateType.Complete;

            Assert.Throws<BrowserConnectionException>(() =>
                connection.Send(eGuiType.Chrome, "click", new JObject(), 2000));

            DiagnosticRecord record = Assert.Single(sink.Records);
            Assert.DoesNotContain(pipeName, record.Message);
            Assert.DoesNotContain("private-server-key", record.Message);
            Assert.Contains("ExceptionType=InvalidOperationException", record.Message);
        }

        [Fact]
        public async Task ReverseOrderResponsesWriteOneSafeLogPerCommand()
        {
            var factory = new FakeNamedPipeClientFactory();
            var payloads = new List<byte[]>();
            factory.Client.SendAction = payload =>
            {
                lock (payloads)
                {
                    payloads.Add(payload);
                    if (payloads.Count == 2)
                    {
                        factory.Client.RaiseMessage(Response(payloads[1], 2));
                        factory.Client.RaiseMessage(Response(payloads[0], 1));
                    }
                }
            };
            var sink = new CapturingDiagnosticSink();
            var connection = Start(factory, sink, "key_11223");
            connection.InitScripeState = InitScripeStateType.Complete;

            await Task.WhenAll(
                Task.Run(() => connection.Send(
                    eGuiType.Chrome, "first", new JObject(), 2000)),
                Task.Run(() => connection.Send(
                    eGuiType.Chrome, "second", new JObject(), 2000)));

            DiagnosticRecord[] records = sink.Records.ToArray();
            Assert.Equal(2, records.Length);
            Assert.Single(records, r => r.Message.Contains("Command=first"));
            Assert.Single(records, r => r.Message.Contains("Command=second"));
            Assert.All(records, r => Assert.DoesNotContain("MessageId", r.Message));
        }

        [Fact]
        public void ResponseBeforeSendReturnsStillWritesOneSuccessLog()
        {
            var factory = new FakeNamedPipeClientFactory();
            factory.Client.SendAction = payload =>
                factory.Client.RaiseMessage(Response(payload, 200));
            var sink = new CapturingDiagnosticSink();
            var connection = Start(factory, sink, "key_44556");

            PipeResponse response = connection.Send(
                eGuiType.Chrome, "getVersion", new JObject(), 2000);

            Assert.Equal(200, response.Code);
            DiagnosticRecord record = Assert.Single(sink.Records);
            Assert.Contains("Result=Success", record.Message);
            Assert.DoesNotContain("ElapsedMs=-", record.Message);
        }

        [Fact]
        public void DiagnosticSinkFailureDoesNotChangeSuccessfulResponse()
        {
            var factory = new FakeNamedPipeClientFactory();
            factory.Client.SendAction = payload =>
                factory.Client.RaiseMessage(Response(payload, 200));
            var connection = Start(
                factory,
                new ThrowingDiagnosticSink(),
                "key_77889");

            PipeResponse response = connection.Send(
                eGuiType.Chrome, "getVersion", new JObject(), 2000);

            Assert.Equal(200, response.Code);
        }

        [Fact]
        public void InitializingStateAllowsInitializationCommand()
        {
            var factory = new FakeNamedPipeClientFactory();
            factory.Client.SendAction = payload =>
                factory.Client.RaiseMessage(Response(payload, 200));
            var connection = Start(factory);
            connection.InitScripeState = InitScripeStateType.Initializing;

            PipeResponse response = connection.Send(
                eGuiType.Chrome,
                "init",
                new JObject(),
                2000);

            Assert.Equal(200, response.Code);
            TransferMessage request = Request(factory.Client.SentPayloads.Single());
            Assert.Equal("SendTransferMessage", request.Action);
            Assert.Equal(SignalRClientType.Web, request.TargetClientType);
            Assert.Equal(eGuiType.Chrome.ToString(), request.TargetClientFlag);
        }

        [Fact]
        public async Task ConcurrentRequestsCompleteFromReverseOrderResponses()
        {
            var factory = new FakeNamedPipeClientFactory();
            var payloads = new List<byte[]>();
            factory.Client.SendAction = payload =>
            {
                lock (payloads)
                {
                    payloads.Add(payload);
                    if (payloads.Count == 2)
                    {
                        factory.Client.RaiseMessage(Response(payloads[1], 2));
                        factory.Client.RaiseMessage(Response(payloads[0], 1));
                    }
                }
            };
            var connection = Start(factory);
            connection.InitScripeState = InitScripeStateType.Complete;

            Task<PipeResponse> first = Task.Run(
                () => connection.Send(eGuiType.Chrome, "first", new JObject(), 2000));
            Task<PipeResponse> second = Task.Run(
                () => connection.Send(eGuiType.Chrome, "second", new JObject(), 2000));

            PipeResponse[] responses = await Task.WhenAll(first, second);
            Assert.Equal(new[] { 1, 2 }, responses.Select(r => r.Code).OrderBy(v => v).ToArray());
        }

        [Fact]
        public async Task DisconnectImmediatelyFailsAllPendingRequests()
        {
            var factory = new FakeNamedPipeClientFactory();
            var connection = Start(factory);
            connection.InitScripeState = InitScripeStateType.Complete;

            Task<PipeResponse> pending = Task.Run(
                () => connection.Send(eGuiType.Chrome, "command", new JObject(), 5000));
            Assert.True(SpinWait.SpinUntil(
                () => factory.Client.SentPayloads.Count == 1,
                1000));

            factory.Client.RaiseDisconnected();

            BrowserConnectionException error = await Assert.ThrowsAsync<BrowserConnectionException>(
                async () => await pending);
            Assert.Equal(ClientTransferCompletionReason.ConnectionClosed, error.Reason);
            Assert.False(connection.IsConnected);
        }

        [Fact]
        public void InvalidUtf8ClosesConnectionAsProtocolError()
        {
            var factory = new FakeNamedPipeClientFactory();
            factory.Client.SendAction = _ => factory.Client.RaiseMessage(new byte[] { 0xff });
            var connection = Start(factory);
            connection.InitScripeState = InitScripeStateType.Complete;

            BrowserConnectionException error = Assert.Throws<BrowserConnectionException>(
                () => connection.Send(eGuiType.Chrome, "command", new JObject(), 2000));

            Assert.Equal(ClientTransferCompletionReason.ProtocolError, error.Reason);
            Assert.False(connection.IsConnected);
        }

        [Fact]
        public void InvalidCallbackShapeClosesConnectionAsProtocolError()
        {
            var factory = new FakeNamedPipeClientFactory();
            factory.Client.SendAction = payload =>
            {
                TransferMessage request = Request(payload);
                var invalid = new JObject
                {
                    [nameof(TransferMessage.MessageId)] = request.MessageId,
                    [nameof(TransferMessage.CallbackReslut)] = "not-an-object"
                };
                factory.Client.RaiseMessage(Encoding.UTF8.GetBytes(invalid.ToString(Formatting.None)));
            };
            var connection = Start(factory);
            connection.InitScripeState = InitScripeStateType.Complete;

            BrowserConnectionException error = Assert.Throws<BrowserConnectionException>(
                () => connection.Send(eGuiType.Chrome, "command", new JObject(), 2000));

            Assert.Equal(ClientTransferCompletionReason.ProtocolError, error.Reason);
            Assert.False(connection.IsConnected);
        }

        [Fact]
        public void UnknownValidMessageIdIsDiscardedAndConnectionRemainsUsable()
        {
            var factory = new FakeNamedPipeClientFactory();
            factory.Client.SendAction = payload =>
            {
                TransferMessage request = Request(payload);
                factory.Client.RaiseMessage(Response(Guid.NewGuid().ToString("N"), 999));
                factory.Client.RaiseMessage(Response(request.MessageId, 200));
            };
            var connection = Start(factory);
            connection.InitScripeState = InitScripeStateType.Complete;

            PipeResponse response = connection.Send(
                eGuiType.Chrome,
                "command",
                new JObject(),
                2000);

            Assert.Equal(200, response.Code);
            Assert.True(connection.IsConnected);
        }

        [Fact]
        public void TimeoutDropsLateResponseAndOnlyEndsCurrentRequest()
        {
            var factory = new FakeNamedPipeClientFactory();
            var connection = Start(factory);
            connection.InitScripeState = InitScripeStateType.Complete;

            BrowserConnectionException timedOut = Assert.Throws<BrowserConnectionException>(() =>
                connection.Send(eGuiType.Chrome, "slow", new JObject(), 30));
            byte[] firstPayload = factory.Client.SentPayloads.Single();
            factory.Client.RaiseMessage(Response(firstPayload, 100));

            factory.Client.SendAction = payload =>
                factory.Client.RaiseMessage(Response(payload, 200));
            PipeResponse next = connection.Send(
                eGuiType.Chrome,
                "next",
                new JObject(),
                2000);

            Assert.Equal(ClientTransferCompletionReason.Timeout, timedOut.Reason);
            Assert.Equal(200, next.Code);
            Assert.True(connection.IsConnected);
        }

        [Fact]
        public void SendFailureClosesCurrentStreamAndFailsRequest()
        {
            var factory = new FakeNamedPipeClientFactory();
            var expected = new InvalidOperationException("write failed");
            factory.Client.SendException = expected;
            var connection = Start(factory);
            connection.InitScripeState = InitScripeStateType.Complete;

            BrowserConnectionException error = Assert.Throws<BrowserConnectionException>(
                () => connection.Send(eGuiType.Chrome, "command", new JObject(), 2000));

            Assert.Equal(ClientTransferCompletionReason.SendFailed, error.Reason);
            Assert.Same(expected, error.InnerException);
            Assert.False(connection.IsConnected);
        }

        [Fact]
        public async Task StopIsIdempotentAndImmediatelyEndsPendingRequest()
        {
            var factory = new FakeNamedPipeClientFactory();
            var connection = Start(factory);
            connection.InitScripeState = InitScripeStateType.Complete;
            Task<PipeResponse> pending = Task.Run(
                () => connection.Send(eGuiType.Chrome, "command", new JObject(), 5000));
            Assert.True(SpinWait.SpinUntil(
                () => factory.Client.SentPayloads.Count == 1,
                1000));

            connection.Stop();
            connection.Stop();

            BrowserConnectionException error = await Assert.ThrowsAsync<BrowserConnectionException>(
                async () => await pending);
            Assert.Equal(ClientTransferCompletionReason.Stopped, error.Reason);
            Assert.False(connection.IsConnected);
        }

        [Fact]
        public void PingFailureLeavesConnectionClosed()
        {
            var factory = new FakeNamedPipeClientFactory();
            factory.Client.PingException = new TimeoutException("pong missing");
            var connection = new NamePipeConnection("pipe", factory) { ConnectTimeout = 500 };

            BrowserConnectionException error =
                Assert.Throws<BrowserConnectionException>(() => connection.Start());

            Assert.Equal(ClientTransferCompletionReason.ConnectionClosed, error.Reason);
            Assert.False(connection.IsConnected);
        }

        private static NamePipeConnection Start(FakeNamedPipeClientFactory factory)
        {
            var connection = new NamePipeConnection("pipe", factory) { ConnectTimeout = 500 };
            connection.Start();
            return connection;
        }

        private static NamePipeConnection Start(
            FakeNamedPipeClientFactory factory,
            INamedPipeRequestDiagnosticSink sink,
            string pipeName)
        {
            var connection = new NamePipeConnection(pipeName, factory, sink)
            {
                ConnectTimeout = 500
            };
            connection.Start();
            return connection;
        }

        private static byte[] Response(byte[] requestPayload, int code)
        {
            return Response(Request(requestPayload).MessageId, code);
        }

        private static byte[] Response(string messageId, int code)
        {
            var response = new TransferMessage
            {
                MessageId = messageId,
                CallbackReslut = JObject.FromObject(new PipeResponse { Code = code })
            };
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response));
        }

        private static TransferMessage Request(byte[] payload)
        {
            return JsonConvert.DeserializeObject<TransferMessage>(Encoding.UTF8.GetString(payload));
        }

        private sealed class FakeNamedPipeClientFactory : INamedPipeClientAdapterFactory
        {
            internal FakeNamedPipeClientAdapter Client { get; } = new FakeNamedPipeClientAdapter();

            internal int CreateCount { get; private set; }

            public INamedPipeClientAdapter Create(string pipeName, TimeSpan connectTimeout)
            {
                CreateCount++;
                return Client;
            }
        }

        private sealed class CapturingDiagnosticSink : INamedPipeRequestDiagnosticSink
        {
            internal ConcurrentQueue<DiagnosticRecord> Records { get; } =
                new ConcurrentQueue<DiagnosticRecord>();

            public void Write(
                NamedPipeDiagnosticLevel level,
                string message,
                Exception exception)
            {
                Records.Enqueue(new DiagnosticRecord(level, message, exception));
            }
        }

        private sealed class ThrowingDiagnosticSink : INamedPipeRequestDiagnosticSink
        {
            public void Write(
                NamedPipeDiagnosticLevel level,
                string message,
                Exception exception)
            {
                throw new InvalidOperationException("logger unavailable");
            }
        }

        private sealed class DiagnosticRecord
        {
            internal DiagnosticRecord(
                NamedPipeDiagnosticLevel level,
                string message,
                Exception exception)
            {
                Level = level;
                Message = message;
                Exception = exception;
            }

            internal NamedPipeDiagnosticLevel Level { get; }
            internal string Message { get; }
            internal Exception Exception { get; }
        }

        private sealed class FakeNamedPipeClientAdapter : INamedPipeClientAdapter
        {
            public event Action<byte[]> MessageReceived;
            public event Action Disconnected;

            public bool IsConnected { get; private set; }
            internal int ConnectCount { get; private set; }
            internal int PingCount { get; private set; }
            internal Action<byte[]> SendAction { get; set; }
            internal Exception SendException { get; set; }
            internal Exception PingException { get; set; }
            internal ConcurrentQueue<byte[]> SentPayloads { get; } = new ConcurrentQueue<byte[]>();

            public Task ConnectAsync(CancellationToken cancellationToken)
            {
                ConnectCount++;
                IsConnected = true;
                return Task.CompletedTask;
            }

            public Task SendAsync(byte[] payload, CancellationToken cancellationToken)
            {
                SentPayloads.Enqueue(payload);
                if (SendException != null)
                {
                    throw SendException;
                }
                SendAction?.Invoke(payload);
                return Task.CompletedTask;
            }

            public Task PingAsync(TimeSpan timeout, CancellationToken cancellationToken)
            {
                PingCount++;
                if (PingException != null)
                {
                    throw PingException;
                }
                return Task.CompletedTask;
            }

            public Task DisconnectAsync()
            {
                IsConnected = false;
                return Task.CompletedTask;
            }

            public void Dispose()
            {
                IsConnected = false;
            }

            internal void RaiseMessage(byte[] payload)
            {
                MessageReceived?.Invoke(payload);
            }

            internal void RaiseDisconnected()
            {
                IsConnected = false;
                Disconnected?.Invoke();
            }
        }
    }
}
