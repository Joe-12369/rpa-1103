using II.RPA.Plugin.Browsers.Sogou;
using II.RPA.Plugin.Core;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.connection;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace II.RPA.WebBrowser.Common.Tests
{
    public class BrowserConnectionRoutingTests
    {
        [Fact]
        public void NewSogouEnablesNamedPipeConnection()
        {
            var browser = new NewVersionSogouBrowser();

            var config = browser.GetConnectionConfig();

            Assert.Equal(typeof(NamePipeConnection), config.NamePipeConnectionType);
            Assert.Equal(typeof(IPCConnection), config.ServiceConnectionType);
            Assert.Equal("1b2c46b3e673ce7f4aff9c4f5065d256459a36df", config.ServiceKey);
        }

        [Fact]
        public void LegacySogouKeepsServiceHubRouting()
        {
            var config = new LegacyVersionSogouBrowser().GetConnectionConfig();

            Assert.Null(config.NamePipeConnectionType);
            Assert.Null(config.ServiceConnectionType);
            Assert.Equal(typeof(ServiceHubConnection), config.SignalRConnectionType);
        }

        [Fact]
        public void DisconnectedServiceHubIsStartedBeforeItIsProbed()
        {
            var connection = new RecordingServiceHubConnection();
            var config = new ConnectionConfig
            {
                GuiType = eGuiType.Chrome,
                ConnectTimeout = 4321
            };

            var result = ConnectionFactory.GetServiceHubConnection(config, connection);

            Assert.Same(connection, result);
            Assert.Equal(4321, connection.ConnectTimeout);
            Assert.Equal(1, connection.StartCount);
            Assert.Equal(1, connection.ProbeCount);
        }

        [Theory]
        [InlineData("U2FsdGVkX19Xagmra6VZ0BB2FitqhBx8")]
        [InlineData("8440b9eb7d03917b2509b5baeab2a299ce257eb0")]
        [InlineData("66819f50ea4d7ec659d7013416a623f1fd8ca6da")]
        [InlineData("a1773d62a609dd09e98ea1aebeddbd949b78bf83")]
        [InlineData("1b9199950f8edc7d0d09200a516cdf0fc78c4c17")]
        [InlineData("1b2c46b3e673ce7f4aff9c4f5065d256459a36df")]
        public void NativePipeNameIsUnifiedWithoutChangingWcfServiceKey(string serviceKey)
        {
            var config = new ConnectionConfig
            {
                ServiceKey = serviceKey,
                NativeMessageHostProcessID = 737
            };

            Assert.Contains(serviceKey, config.IPCServiceAddress);
            Assert.Equal("U2FsdGVkX19Xagmra6VZ0BB2FitqhBx8_737", config.NamedPipeServerName);
        }

        private sealed class NewVersionSogouBrowser : SogouBrowser
        {
            public override string GetBrowserAppPath()
            {
                return typeof(JsonConvert).Assembly.Location;
            }
        }

        private sealed class LegacyVersionSogouBrowser : SogouBrowser
        {
            public override string GetBrowserAppPath()
            {
                return null;
            }
        }

        private sealed class RecordingServiceHubConnection : IBrowserConnectionNew
        {
            public bool IsConnected { get; set; }
            public InitScripeStateType InitScripeState { get; set; }
            public PipeResponse PipeResponse { get; private set; }
            public string CrxVersion { get; set; }
            public int ConnectTimeout { get; set; }
            public bool CrxIsLatestOrNewer => true;
            public int StartCount { get; private set; }
            public int ProbeCount { get; private set; }

            public void Start()
            {
                StartCount++;
                IsConnected = true;
            }

            public bool TestConnection(eGuiType guiType, int timeOut = 300)
            {
                ProbeCount++;
                return IsConnected;
            }

            public bool TestConnection() => IsConnected;
            public void Stop() => IsConnected = false;
            public PipeResponse Send(eGuiType guiType, string command, JObject @params, int timeout = 5000) => null;
        }
    }
}
