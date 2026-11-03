using II.RPA.Core.Runtime;
using II.RPA.Core.Talker.Talkers;
using Xunit;

namespace II.RPA.Core.Talker.Tests
{
    /// <summary>
    /// Locks the heartbeat budget contract: the probe timeout has to stay inside the heartbeat
    /// interval, every configured value is validated, and explicit constructor values win over
    /// configuration.
    /// </summary>
    public class MobileProviderTalkerHeartbeatSettingsTests
    {
        [Fact]
        public void Constructor_WithHeartbeatTimeoutEqualToInterval_Throws()
        {
            var context = CreateContext(new()
            {
                ["MobileHeartbeatInterval"] = "5",
                ["MobileHeartbeatTimeout"] = "5",
            });

            Assert.Throws<ArgumentOutOfRangeException>(() => CreateTalker(context));
        }

        [Fact]
        public void Constructor_WithNonPositiveHeartbeatTimeout_Throws()
        {
            var context = CreateContext(new()
            {
                ["MobileHeartbeatTimeout"] = "0",
            });

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => CreateTalker(context));

            Assert.Equal("MobileHeartbeatTimeout", exception.ParamName);
        }

        [Fact]
        public void Constructor_WithNonNumericHeartbeatInterval_Throws()
        {
            var context = CreateContext(new()
            {
                ["MobileHeartbeatInterval"] = "fast",
            });

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => CreateTalker(context));

            Assert.Equal("MobileHeartbeatInterval", exception.ParamName);
        }

        [Fact]
        public void Constructor_WithNonPositiveFailureThreshold_Throws()
        {
            var context = CreateContext(new()
            {
                ["MobileHeartbeatFailureThreshold"] = "0",
            });

            Assert.Throws<ArgumentOutOfRangeException>(() => CreateTalker(context));
        }

        [Fact]
        public void Constructor_WithValidHeartbeatSettings_Succeeds()
        {
            var context = CreateContext(new()
            {
                ["MobileHeartbeatInterval"] = "10",
                ["MobileHeartbeatTimeout"] = "4",
                ["MobileHeartbeatFailureThreshold"] = "2",
            });

            var talker = CreateTalker(context);

            Assert.Equal("mobileprovider", talker.Name);
        }

        [Fact]
        public void Constructor_WithExplicitSettings_IgnoresConfiguration()
        {
            var context = CreateContext(new()
            {
                ["MobileHeartbeatInterval"] = "0",
                ["MobileHeartbeatTimeout"] = "not-a-number",
                ["MobileHeartbeatFailureThreshold"] = "-1",
            });

            var talker = new MobileProviderTalker(
                NullLogger.Instance,
                context,
                "test-channel",
                startProcess: false,
                heartbeatInterval: TimeSpan.FromSeconds(10),
                heartbeatTimeout: TimeSpan.FromSeconds(4),
                heartbeatFailureThreshold: 2);

            Assert.Equal("mobileprovider", talker.Name);
        }

        private static MobileProviderTalker CreateTalker(FakeRuntimeContext context)
        {
            return new MobileProviderTalker(
                NullLogger.Instance,
                context,
                "test-channel",
                startProcess: false);
        }

        private static FakeRuntimeContext CreateContext(Dictionary<string, string?> configuration)
        {
            return new FakeRuntimeContext(configuration);
        }

        private sealed class FakeRuntimeContext : IRpaRuntimeContext
        {
            private readonly Dictionary<string, string?> _configuration;

            public FakeRuntimeContext(Dictionary<string, string?> configuration)
            {
                _configuration = new Dictionary<string, string?>(configuration, StringComparer.OrdinalIgnoreCase);
            }

            public RpaClientKind ClientKind => RpaClientKind.Bot;

            public string ClientId => "Z-Bot";

            public string ProducedClientId => "Z-Bot";

            public string RootClientId => "Z-Bot";

            public int CurrentProcessId => 1;

            public int SessionId => 1;

            public int ServerPort => 7183;

            public int RootServerPort => 7183;

            public bool LoopAdb => false;

            public bool IsMiniModeActive => false;

            public bool ForceRestartHost => false;

            public string EngineExecutablePath => string.Empty;

            public string PythonEnvironmentVersion => string.Empty;

            public string? UserInfoParameter { get; set; }

            public string BuildUserInfoParameter() => string.Empty;

            public string FixZBrainUriForZDesktop(string parameter) => parameter;

            public void SetSetting(string key, object value)
            {
            }

            public string? GetConfiguration(string commandLineName, string configName)
            {
                if (_configuration.TryGetValue(commandLineName, out var configuredByCommandLine))
                {
                    return configuredByCommandLine;
                }

                return _configuration.TryGetValue(configName, out var configuredBySetting)
                    ? configuredBySetting
                    : null;
            }

            public bool IsMobileProject(string packagePath) => false;
        }
    }
}
