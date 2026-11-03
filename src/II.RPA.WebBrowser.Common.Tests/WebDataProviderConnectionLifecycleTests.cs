using II.RPA.Plugin.Core;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.connection;
using II.RPA.WebBrowser.Common.Basic.connection.ClientTransfer;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json.Linq;
using System;
using Xunit;

namespace II.RPA.WebBrowser.Common.Tests
{
    public class WebDataProviderConnectionLifecycleTests
    {
        [Fact]
        public void TransferTimeoutUsesDedicatedRpaErrorTypeAndPreservesConnectionException()
        {
            var connectionException = new BrowserConnectionException(
                "request timed out",
                ClientTransferCompletionReason.Timeout);

            RPAException exception = connectionException.ToRpaException(eGuiType.Chrome);

            Assert.Equal(eErrorMessageType.Error_TransferMsg_Timeout, exception.ErrorMessageType);
            Assert.Same(connectionException, exception.InnerException);
        }

        [Fact]
        public void LateFailureFromOldConnectionDoesNotClearNewConnection()
        {
            var provider = new TestWebDataProvider();
            var replacement = new FakeConnection();
            var failed = new FakeConnection
            {
                OnSend = () =>
                {
                    provider.Connection = replacement;
                    throw new BrowserConnectionException(
                        "old connection failed",
                        ClientTransferCompletionReason.ConnectionClosed);
                }
            };
            provider.Connection = failed;

            RPAException exception = Assert.Throws<RPAException>(() => provider.SendRequestForTest());
            Assert.IsType<BrowserConnectionException>(exception.InnerException);

            Assert.Equal(1, failed.SendCount);
            Assert.Equal(1, failed.StopCount);
            Assert.Equal(0, replacement.StopCount);
            Assert.Same(replacement, provider.Connection);
        }

        [Fact]
        public void TimeoutNullKeepsCurrentConnection()
        {
            var provider = new TestWebDataProvider();
            var connection = new FakeConnection();
            provider.Connection = connection;

            PipeResponse result = provider.SendRequestForTest();

            Assert.Null(result);
            Assert.Same(connection, provider.Connection);
            Assert.Equal(1, connection.SendCount);
            Assert.Equal(0, connection.StopCount);
        }

        [Fact]
        public void CompatibleCommandTimeoutReturnsNullAndKeepsHealthyConnection()
        {
            var provider = new TestWebDataProvider();
            var connection = new FakeConnection
            {
                OnSend = () => throw new BrowserConnectionException(
                    "timed out",
                    ClientTransferCompletionReason.Timeout)
            };
            provider.Connection = connection;

            PipeResponse result = provider.SendRequestForTest("tabs.query");

            Assert.Null(result);
            Assert.Same(connection, provider.Connection);
            Assert.Equal(0, connection.StopCount);
        }

        [Fact]
        public void SelectorCommandTimeoutThrowsTimeoutException()
        {
            var provider = new TestWebDataProvider();
            var connection = new FakeConnection
            {
                OnSend = () => throw new BrowserConnectionException(
                    "timed out",
                    ClientTransferCompletionReason.Timeout)
            };
            provider.Connection = connection;

            TimeoutException exception = Assert.Throws<TimeoutException>(() =>
                provider.SendRequestForTest("elements.buildSelector"));

            Assert.IsType<BrowserConnectionException>(exception.InnerException);
            Assert.Same(connection, provider.Connection);
            Assert.Equal(0, connection.StopCount);
        }

        [Fact]
        public void TimeoutReleasesUnhealthyConnectionBeforeApplyingCommandPolicy()
        {
            var provider = new TestWebDataProvider();
            var connection = new FakeConnection
            {
                IsConnected = false,
                OnSend = () => throw new BrowserConnectionException(
                    "timed out",
                    ClientTransferCompletionReason.Timeout)
            };
            provider.Connection = connection;

            PipeResponse result = provider.SendRequestForTest("tabs.query");

            Assert.Null(result);
            Assert.Equal(1, connection.StopCount);
            Assert.Null(provider.Connection);
        }

        [Fact]
        public void NextCallReentersConnectionFactoryAfterConnectionFailure()
        {
            var provider = new TestWebDataProvider();
            var failed = new FakeConnection
            {
                OnSend = () => throw new BrowserConnectionException(
                    "connection failed",
                    ClientTransferCompletionReason.ConnectionClosed)
            };
            var replacement = new FakeConnection
            {
                Response = new PipeResponse { Code = 200, Result = new PipeResult() }
            };
            int factoryCalls = 0;
            provider.Connection = failed;
            provider.SetConnectionFactory(() => { factoryCalls++; return replacement; });

            RPAException exception = Assert.Throws<RPAException>(() => provider.SendRequestForTest());
            Assert.IsType<BrowserConnectionException>(exception.InnerException);
            PipeResponse result = provider.SendRequestForTest();

            Assert.Equal(1, factoryCalls);
            Assert.Same(replacement, provider.Connection);
            Assert.Same(replacement.Response, result);
            Assert.Equal(1, failed.SendCount);
            Assert.Equal(1, replacement.SendCount);
        }

        [Fact]
        public void ConnectionFailureStopsAndClearsCurrentConnectionWithoutRetry()
        {
            var provider = new TestWebDataProvider();
            var connection = new FakeConnection
            {
                OnSend = () => throw new BrowserConnectionException(
                    "connection failed",
                    ClientTransferCompletionReason.ConnectionClosed)
            };
            provider.Connection = connection;

            RPAException exception = Assert.Throws<RPAException>(() => provider.SendRequestForTest());
            Assert.IsType<BrowserConnectionException>(exception.InnerException);

            Assert.Equal(1, connection.SendCount);
            Assert.Equal(1, connection.StopCount);
            Assert.Null(provider.Connection);
        }

        private sealed class TestWebDataProvider : WebDataProvider
        {
            internal TestWebDataProvider()
                : base(new ConnectionConfig { GuiType = eGuiType.Chrome })
            {
            }

            internal PipeResponse SendRequestForTest(string command = "tabs.query") =>
                SendRequest(command, new JObject(), 1000);

            internal void SetConnectionFactory(Func<IBrowserConnectionNew> factory) =>
                InitConnectionFunc = factory;
        }

        private sealed class FakeConnection : IBrowserConnectionNew
        {
            public Action OnSend { get; set; }
            public PipeResponse Response { get; set; }
            public int SendCount { get; private set; }
            public int StopCount { get; private set; }
            public bool IsConnected { get; set; } = true;
            public InitScripeStateType InitScripeState { get; set; } = InitScripeStateType.Complete;
            public PipeResponse PipeResponse { get; private set; }
            public string CrxVersion { get; set; }
            public int ConnectTimeout { get; set; }
            public bool CrxIsLatestOrNewer => true;
            public void Start() { }
            public void Stop() { StopCount++; IsConnected = false; }
            public PipeResponse Send(eGuiType guiType, string command, JObject @params, int timeout = 5000)
            {
                SendCount++;
                OnSend?.Invoke();
                return Response;
            }
            public bool TestConnection() => IsConnected;
            public bool TestConnection(eGuiType guiType, int timeOut = 300) => IsConnected;
        }
    }
}
