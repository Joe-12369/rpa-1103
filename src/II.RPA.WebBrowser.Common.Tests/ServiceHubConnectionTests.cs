using II.RPA.Plugin.Core;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.connection;
using II.RPA.WebBrowser.Common.Basic.connection.ClientTransfer;
using II.RPA.WebBrowser.Common.Basic.connection.ServiceHub;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace II.RPA.WebBrowser.Common.Tests
{
    public class ServiceHubConnectionTests
    {
        [Fact]
        public async Task PendingSurvivesPhysicalDisconnectAndCompletesOnNewSession()
        {
            var first = new FakeServiceHubSession();
            var second = new FakeServiceHubSession();
            var connection = new ServiceHubConnection(new QueueSessionFactory(first, second));
            connection.Start();

            Task<PipeResponse> pending = Task.Run(() =>
                connection.Send(eGuiType.Chrome, "tabs.query", new JObject(), 5000));
            TransferMessage sent = await first.WaitForSend();
            first.Disconnect(new InvalidOperationException("generation one closed"));
            connection.Start();
            second.Respond(Response(sent.MessageId, 201));

            Assert.Equal(201, (await pending).Code);
            Assert.Equal(1, first.SendCount);
            Assert.True(connection.IsConnected);
        }

        [Fact]
        public async Task ErrorFromOldSessionDoesNotCloseNewSession()
        {
            var first = new FakeServiceHubSession();
            var second = new FakeServiceHubSession();
            var connection = new ServiceHubConnection(new QueueSessionFactory(first, second));
            connection.Start();
            first.Disconnect(new Exception("replace"));
            connection.Start();

            first.Disconnect(new Exception("late old event"));

            Assert.True(connection.IsConnected);
            Task<PipeResponse> request = Task.Run(() => connection.Send(eGuiType.Chrome, "ping", new JObject(), 1000));
            TransferMessage sent = await second.WaitForSend();
            second.Respond(Response(sent.MessageId, 200));
            Assert.Equal(200, (await request).Code);
        }

        [Fact]
        public async Task SendExceptionAfterInvocationStartedKeepsPendingWithoutRetry()
        {
            var session = new FakeServiceHubSession { ThrowAfterSendStarted = true };
            var connection = new ServiceHubConnection(new QueueSessionFactory(session));
            connection.Start();

            Task<PipeResponse> pending = Task.Run(() => connection.Send(eGuiType.Chrome, "ping", new JObject(), 1000));
            TransferMessage sent = await session.WaitForSend();
            session.Respond(Response(sent.MessageId, 202));

            Assert.Equal(202, (await pending).Code);
            Assert.Equal(1, session.SendCount);
        }

        [Fact]
        public async Task FailureBeforeInvocationImmediatelyCompletesAsSendFailed()
        {
            var session = new FakeServiceHubSession { ThrowBeforeInvocation = true };
            var connection = new ServiceHubConnection(new QueueSessionFactory(session));
            connection.Start();

            BrowserConnectionException error = await Assert.ThrowsAsync<BrowserConnectionException>(() =>
                Task.Run(() => connection.Send(eGuiType.Chrome, "ping", new JObject(), 200)));

            Assert.Equal(ClientTransferCompletionReason.SendFailed, error.Reason);
            Assert.Equal(0, session.SendCount);
        }

        [Fact]
        public async Task LegalLateResponseAfterTimeoutIsDiscardedAndLogicalConnectionRemainsUsable()
        {
            var session = new FakeServiceHubSession();
            var connection = new ServiceHubConnection(new QueueSessionFactory(session));
            connection.Start();

            BrowserConnectionException timedOut = await Assert.ThrowsAsync<BrowserConnectionException>(() => Task.Run(() =>
                connection.Send(eGuiType.Chrome, "first", new JObject(), 50)));
            TransferMessage first = await session.WaitForSend();
            Assert.Equal(ClientTransferCompletionReason.Timeout, timedOut.Reason);

            session.Respond(Response(first.MessageId, 299));
            Task<PipeResponse> next = Task.Run(() =>
                connection.Send(eGuiType.Chrome, "second", new JObject(), 1000));
            TransferMessage second = await session.WaitForSendCount(2);
            session.Respond(Response(second.MessageId, 203));

            Assert.Equal(203, (await next).Code);
            Assert.True(connection.IsConnected);
        }

        [Fact]
        public async Task MultiplePendingCompleteOutOfOrderAcrossPhysicalGeneration()
        {
            var first = new FakeServiceHubSession();
            var second = new FakeServiceHubSession();
            var connection = new ServiceHubConnection(new QueueSessionFactory(first, second));
            connection.Start();

            Task<PipeResponse> a = Task.Run(() => connection.Send(eGuiType.Chrome, "a", new JObject(), 2000));
            Task<PipeResponse> b = Task.Run(() => connection.Send(eGuiType.Chrome, "b", new JObject(), 2000));
            await first.WaitForSendCount(2);
            TransferMessage sentA = first.GetSentByMethod("a");
            TransferMessage sentB = first.GetSentByMethod("b");
            first.Disconnect(new Exception("generation one closed"));
            connection.Start();

            second.Respond(Response(sentB.MessageId, 222));
            second.Respond(Response(sentA.MessageId, 211));

            Assert.Equal(211, (await a).Code);
            Assert.Equal(222, (await b).Code);
            Assert.Equal(2, first.SendCount);
            Assert.Equal(0, second.SendCount);
        }

        [Fact]
        public void DisconnectDuringConnectCannotPublishDisconnectedSessionAsReady()
        {
            var session = new FakeServiceHubSession { DisconnectDuringConnect = true };
            var connection = new ServiceHubConnection(new QueueSessionFactory(session));

            Assert.Throws<BrowserConnectionException>(() => connection.Start());

            Assert.False(connection.IsConnected);
            Assert.False(connection.ConnectionResult.IsConnected);
        }

        [Fact]
        public void StartTriesNextPhysicalSessionWhenEarlierPortFails()
        {
            var first = new FakeServiceHubSession { ConnectException = new InvalidOperationException("port unavailable") };
            var second = new FakeServiceHubSession();
            var factory = new QueueSessionFactory(first, second);
            var connection = new ServiceHubConnection(factory) { ConnectTimeout = 1000 };

            connection.Start();

            Assert.True(connection.IsConnected);
            Assert.Equal(2, factory.CreateCount);
        }

        [Fact]
        public async Task LogicalStopCompletesAllPending()
        {
            var session = new FakeServiceHubSession();
            var connection = new ServiceHubConnection(new QueueSessionFactory(session));
            connection.Start();
            Task<PipeResponse> pending = Task.Run(() => connection.Send(eGuiType.Chrome, "ping", new JObject(), 5000));
            await session.WaitForSend();

            connection.Stop();

            BrowserConnectionException error = await Assert.ThrowsAsync<BrowserConnectionException>(async () => await pending);
            Assert.Equal(ClientTransferCompletionReason.Stopped, error.Reason);
        }

        private static TransferMessage Response(string messageId, int code)
        {
            return new TransferMessage
            {
                MessageId = messageId,
                CallbackReslut = JObject.FromObject(new PipeResponse { Code = code, Result = new PipeResult() })
            };
        }

        private sealed class QueueSessionFactory : IServiceHubSessionFactory
        {
            private readonly Queue<IServiceHubSession> _sessions;
            public QueueSessionFactory(params IServiceHubSession[] sessions) { _sessions = new Queue<IServiceHubSession>(sessions); }
            public int CreateCount { get; private set; }
            public IServiceHubSession Create(int attempt)
            {
                CreateCount++;
                return _sessions.Dequeue();
            }
        }

        private sealed class FakeServiceHubSession : IServiceHubSession
        {
            private readonly TaskCompletionSource<TransferMessage> _sent = new TaskCompletionSource<TransferMessage>();
            private readonly List<TransferMessage> _messages = new List<TransferMessage>();
            public event Action<TransferMessage> MessageReceived;
            public event Action<Exception> Disconnected;
            public bool IsConnected { get; private set; }
            public bool ThrowAfterSendStarted { get; set; }
            public bool ThrowBeforeInvocation { get; set; }
            public bool DisconnectDuringConnect { get; set; }
            public Exception ConnectException { get; set; }
            public int SendCount { get; private set; }
            public void Connect(int timeout)
            {
                if (ConnectException != null) throw ConnectException;
                IsConnected = true;
                if (DisconnectDuringConnect)
                    Disconnect(new InvalidOperationException("disconnected while connecting"));
            }
            public Task<ServiceHubSendResult> SendAsync(TransferMessage message)
            {
                if (ThrowBeforeInvocation)
                    return Task.FromResult(new ServiceHubSendResult(
                        false,
                        new InvalidOperationException("invoke was not started")));
                SendCount++;
                lock (_messages) _messages.Add(message);
                _sent.TrySetResult(message);
                return Task.FromResult(new ServiceHubSendResult(
                    true,
                    ThrowAfterSendStarted ? new InvalidOperationException("invoke outcome unknown") : null));
            }
            public Task<TransferMessage> WaitForSend() => _sent.Task;
            public async Task<TransferMessage> WaitForSendCount(int count)
            {
                for (int i = 0; i < 1000; i++)
                {
                    lock (_messages)
                        if (_messages.Count >= count) return _messages[count - 1];
                    await Task.Delay(1);
                }
                throw new TimeoutException("The expected send did not occur.");
            }
            public TransferMessage GetSentByMethod(string method)
            {
                lock (_messages)
                    return _messages.Find(message => ((PipeRequest)message.SendData).Method == method);
            }
            public void Respond(TransferMessage message) => MessageReceived?.Invoke(message);
            public void Disconnect(Exception error) { IsConnected = false; Disconnected?.Invoke(error); }
            public void Dispose() { IsConnected = false; }
        }
    }
}
