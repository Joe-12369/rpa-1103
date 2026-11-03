using II.RPA.IPC;
using II.RPA.IPC.Config;
using II.RPA.Plugin.Core;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.connection;
using II.RPA.WebBrowser.Common.Basic.connection.ClientTransfer;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Xunit;

namespace II.RPA.WebBrowser.Common.Tests
{
    public class ConnectionFactoryTests
    {
        [Fact]
        public void ConnectionConfigSeparatesBrowserSpecificWcfKeyFromUnifiedNamedPipeKey()
        {
            var config = Config();

            Assert.Equal("browser-key_1234", config.WcfServiceName);
            Assert.Equal(
                "net.pipe://localhost/SZ_RPAService/browser-key_1234",
                config.IPCServiceAddress);
            Assert.Equal(
                "U2FsdGVkX19Xagmra6VZ0BB2FitqhBx8_1234",
                config.NamedPipeServerName);
        }

        [Fact]
        public void NamedPipeCapabilityCreatesAndStartsOnlyNamedPipeConnection()
        {
            var resolver = new StubResolver(Capability(NmhConnectionProtocol.NamedPipe));
            var activator = new RecordingActivator();
            var factory = new NmhConnectionFactory(resolver, activator);
            ConnectionConfig config = Config();

            IBrowserConnectionNew result = factory.GetConnection(config);

            Assert.Same(activator.Connection, result);
            Assert.Single(activator.Calls);
            Assert.Equal(typeof(FakeNamedPipeConnection), activator.Calls[0].ConnectionType);
            Assert.Equal(config.NamedPipeServerName, activator.Calls[0].Endpoint);
            Assert.Equal(config.ConnectTimeout, result.ConnectTimeout);
            Assert.Equal(1, activator.Connection.StartCount);
        }

        [Fact]
        public void WcfCapabilityCreatesAndStartsOnlyWcfConnection()
        {
            var resolver = new StubResolver(Capability(NmhConnectionProtocol.WcfIpc));
            var activator = new RecordingActivator();
            var factory = new NmhConnectionFactory(resolver, activator);
            ConnectionConfig config = Config();

            IBrowserConnectionNew result = factory.GetConnection(config);

            Assert.Same(activator.Connection, result);
            Assert.Single(activator.Calls);
            Assert.Equal(typeof(FakeWcfConnection), activator.Calls[0].ConnectionType);
            Assert.Equal(config.IPCServiceAddress, activator.Calls[0].Endpoint);
            Assert.Equal(1, activator.Connection.StartCount);
        }

        [Fact]
        public void CapabilityFailureDoesNotConstructAnyConnection()
        {
            var expected = new InvalidOperationException("version unavailable");
            var resolver = new StubResolver(new NmhConnectionCapability(
                1234,
                null,
                null,
                NmhConnectionProtocol.Failure,
                expected));
            var activator = new RecordingActivator();
            var factory = new NmhConnectionFactory(resolver, activator);

            BrowserConnectionException error = Assert.Throws<BrowserConnectionException>(
                () => factory.GetConnection(Config()));

            Assert.Empty(activator.Calls);
            Assert.Equal(ClientTransferCompletionReason.ProtocolError, error.Reason);
            Assert.Same(expected, error.InnerException);
        }

        [Fact]
        public void NamedPipeStartFailureStopsConnectionAndNeverFallsBackToWcf()
        {
            var expected = new InvalidOperationException("pipe unavailable");
            var resolver = new StubResolver(Capability(NmhConnectionProtocol.NamedPipe));
            var activator = new RecordingActivator
            {
                Connection = { StartException = expected }
            };
            var factory = new NmhConnectionFactory(resolver, activator);

            BrowserConnectionException error = Assert.Throws<BrowserConnectionException>(
                () => factory.GetConnection(Config()));

            Assert.Single(activator.Calls);
            Assert.Equal(typeof(FakeNamedPipeConnection), activator.Calls[0].ConnectionType);
            Assert.Equal(1, activator.Connection.StopCount);
            Assert.Equal(ClientTransferCompletionReason.ConnectionClosed, error.Reason);
            Assert.Same(expected, error.InnerException);
        }

        [Fact]
        public void NotConnectedAfterStartIsStoppedAndNeverReturned()
        {
            var resolver = new StubResolver(Capability(NmhConnectionProtocol.NamedPipe));
            var activator = new RecordingActivator();
            activator.Connection.IsConnected = false;
            var factory = new NmhConnectionFactory(resolver, activator);

            Assert.Throws<BrowserConnectionException>(() => factory.GetConnection(Config()));

            Assert.Single(activator.Calls);
            Assert.Equal(1, activator.Connection.StopCount);
        }

        [Fact]
        public void IgnoreConnectionReturnsNullAfterCleanupWithoutChangingProtocol()
        {
            var resolver = new StubResolver(Capability(NmhConnectionProtocol.NamedPipe));
            var activator = new RecordingActivator();
            activator.Connection.StartException = new InvalidOperationException("pipe unavailable");
            var factory = new NmhConnectionFactory(resolver, activator);
            ConnectionConfig config = Config();
            config.IgnoreConnection = true;

            IBrowserConnectionNew result = factory.GetConnection(config);

            Assert.Null(result);
            Assert.Single(activator.Calls);
            Assert.Equal(typeof(FakeNamedPipeConnection), activator.Calls[0].ConnectionType);
            Assert.Equal(1, activator.Connection.StopCount);
        }

        [Fact]
        public void EveryFactoryCallResolvesCapabilityAgain()
        {
            var resolver = new StubResolver(Capability(NmhConnectionProtocol.NamedPipe));
            var activator = new RecordingActivator();
            var factory = new NmhConnectionFactory(resolver, activator);

            factory.GetConnection(Config());
            factory.GetConnection(Config());

            Assert.Equal(2, resolver.ResolveCount);
            Assert.Equal(2, activator.Calls.Count);
        }

        private static ConnectionConfig Config()
        {
            return new ConnectionConfig
            {
                NativeMessageHostProcessID = 1234,
                ServiceKey = "browser-key",
                ConnectTimeout = 4567,
                ServiceConnectionType = typeof(FakeWcfConnection),
                NamePipeConnectionType = typeof(FakeNamedPipeConnection)
            };
        }

        private static NmhConnectionCapability Capability(NmhConnectionProtocol protocol)
        {
            return new NmhConnectionCapability(
                1234,
                "C:\\nmh.exe",
                new Version(7, 3, 7, 0),
                protocol);
        }

        private sealed class StubResolver : INmhConnectionCapabilityResolver
        {
            private readonly NmhConnectionCapability _capability;

            internal StubResolver(NmhConnectionCapability capability)
            {
                _capability = capability;
            }

            internal int ResolveCount { get; private set; }

            public NmhConnectionCapability Resolve(int processId)
            {
                ResolveCount++;
                return _capability;
            }
        }

        private sealed class RecordingActivator : IBrowserConnectionActivator
        {
            internal RecordingActivator()
            {
                Connection = new FakeConnection();
            }

            internal FakeConnection Connection { get; set; }

            internal List<ActivationCall> Calls { get; } = new List<ActivationCall>();

            public IBrowserConnectionNew Create(Type connectionType, string endpoint)
            {
                Calls.Add(new ActivationCall(connectionType, endpoint));
                return Connection;
            }
        }

        private sealed class ActivationCall
        {
            internal ActivationCall(Type connectionType, string endpoint)
            {
                ConnectionType = connectionType;
                Endpoint = endpoint;
            }

            internal Type ConnectionType { get; }

            internal string Endpoint { get; }
        }

        private sealed class FakeConnection : IBrowserConnectionNew
        {
            public bool IsConnected { get; set; } = true;
            public InitScripeStateType InitScripeState { get; set; }
            public PipeResponse PipeResponse => null;
            public string CrxVersion { get; set; }
            public int ConnectTimeout { get; set; }
            public bool CrxIsLatestOrNewer => true;
            internal int StartCount { get; private set; }
            internal int StopCount { get; private set; }
            internal Exception StartException { get; set; }

            public void Start()
            {
                StartCount++;
                if (StartException != null)
                {
                    throw StartException;
                }
            }

            public void Stop()
            {
                StopCount++;
                IsConnected = false;
            }

            public PipeResponse Send(eGuiType guiType, string command, JObject @params, int timeout = 5000)
            {
                throw new NotSupportedException();
            }

            public bool TestConnection()
            {
                return IsConnected;
            }

            public bool TestConnection(eGuiType guiType, int timeOut = 300)
            {
                return IsConnected;
            }
        }

        private sealed class FakeWcfConnection
        {
        }

        private sealed class FakeNamedPipeConnection
        {
        }
    }
}
