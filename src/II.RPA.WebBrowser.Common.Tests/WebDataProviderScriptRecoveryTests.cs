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
    public class WebDataProviderScriptRecoveryTests
    {
        [Fact]
        public void ConnectionFailureRecoversOnceFromReplacementConnection()
        {
            var first = new ScriptedConnection(
                Step.Success("getVersion", "2.9.0"),
                Step.Success("getCodeId", "different-code-id"),
                Step.Throw("init", new BrowserConnectionException(
                    "NMH-1 closed",
                    ClientTransferCompletionReason.ConnectionClosed)));
            var second = new ScriptedConnection(
                Step.Success("getVersion", "2.9.0"),
                Step.Success("getCodeId", ExtensionProvider.Instance.CodeId));
            var provider = new RecoveryWebDataProvider(second) { Connection = first };

            CrxScriptInitializationResult result = provider.InitializeForTest();

            Assert.Equal(CrxScriptInitializationCompletion.Success, result.Completion);
            Assert.Equal(2, result.Attempt);
            Assert.Equal(1, first.StopCount);
            Assert.Same(second, provider.Connection);
            Assert.Equal(1, provider.FactoryCallCount);
            Assert.Equal(new[] { "getVersion", "getCodeId" }, second.Commands);
            Assert.InRange(provider.LastRemainingMilliseconds, 1, 10000);
        }

        [Fact]
        public void ReplacementFailureDoesNotAcquireThirdConnection()
        {
            var failure = new BrowserConnectionException(
                "closed",
                ClientTransferCompletionReason.ConnectionClosed);
            var first = new ScriptedConnection(Step.Throw("getVersion", failure));
            var second = new ScriptedConnection(Step.Throw("getVersion", failure));
            var provider = new RecoveryWebDataProvider(second) { Connection = first };

            CrxScriptInitializationResult result = provider.InitializeForTest();

            Assert.Equal(CrxScriptInitializationCompletion.ConnectionFailed, result.Completion);
            Assert.Equal(2, result.Attempt);
            Assert.Equal(1, provider.FactoryCallCount);
            Assert.Same(second, provider.Connection);
        }

        [Fact]
        public void ConcurrentReplacementPublicationSurvivesFailedConnectionCleanup()
        {
            var second = new ScriptedConnection(
                Step.Success("getVersion", "2.9.0"),
                Step.Success("getCodeId", ExtensionProvider.Instance.CodeId));
            var first = new ScriptedConnection(
                Step.Throw("getVersion", new BrowserConnectionException(
                    "NMH-1 closed",
                    ClientTransferCompletionReason.ConnectionClosed)));
            var provider = new RecoveryWebDataProvider(second) { Connection = first };
            first.OnStop = () => provider.Connection = second;

            CrxScriptInitializationResult result = provider.InitializeForTest();

            Assert.Equal(CrxScriptInitializationCompletion.Success, result.Completion);
            Assert.Same(second, provider.Connection);
            Assert.Equal(0, provider.FactoryCallCount);
            Assert.Equal(new[] { "getVersion", "getCodeId" }, second.Commands);
        }

        [Fact]
        public void FactoryReturningFailedConnectionDoesNotRunSecondAttempt()
        {
            var failure = new BrowserConnectionException(
                "closed",
                ClientTransferCompletionReason.ConnectionClosed);
            var first = new ScriptedConnection(Step.Throw("getVersion", failure));
            var provider = new RecoveryWebDataProvider(first) { Connection = first };

            CrxScriptInitializationResult result = provider.InitializeForTest();

            Assert.Equal(CrxScriptInitializationCompletion.ConnectionFailed, result.Completion);
            Assert.Equal(1, result.Attempt);
            Assert.Equal(1, provider.FactoryCallCount);
            Assert.Equal(new[] { "getVersion" }, first.Commands);
        }

        [Fact]
        public void MissingReplacementEndsRecoveryWithoutSleepingOrThirdAttempt()
        {
            var failure = new BrowserConnectionException(
                "closed",
                ClientTransferCompletionReason.ConnectionClosed);
            var first = new ScriptedConnection(Step.Throw("getVersion", failure));
            var provider = new RecoveryWebDataProvider(null) { Connection = first };

            CrxScriptInitializationResult result = provider.InitializeForTest();

            Assert.Equal(CrxScriptInitializationCompletion.ConnectionFailed, result.Completion);
            Assert.Equal(1, result.Attempt);
            Assert.Equal(1, provider.FactoryCallCount);
            Assert.InRange(provider.LastRemainingMilliseconds, 1, 10000);
        }

        [Fact]
        public void UnavailablePipeRecoversOnceFromReplacementConnection()
        {
            var first = new ScriptedConnection { IsConnected = false };
            var second = new ScriptedConnection(
                Step.Success("getVersion", "2.9.0"),
                Step.Success("getCodeId", ExtensionProvider.Instance.CodeId));
            var provider = new RecoveryWebDataProvider(second) { Connection = first };

            CrxScriptInitializationResult result = provider.InitializeForTest();

            Assert.Equal(CrxScriptInitializationCompletion.Success, result.Completion);
            Assert.Equal(1, first.StopCount);
            Assert.Equal(1, provider.FactoryCallCount);
            Assert.Same(second, provider.Connection);
            Assert.Equal(new[] { "getVersion", "getCodeId" }, second.Commands);
        }

        [Fact]
        public void ExplicitInitialScripeDoesNotThrowWhenRecoveryAcquisitionThrows()
        {
            var first = new ScriptedConnection { IsConnected = false };
            var provider = new RecoveryWebDataProvider(null)
            {
                Connection = first,
                RecoveryException = new InvalidOperationException("factory failed")
            };

            Exception exception = Record.Exception(() => provider.InitialScripe());

            Assert.Null(exception);
            Assert.Equal(1, provider.FactoryCallCount);
        }

        [Fact]
        public void InitializationFailureDoesNotResendOriginalBusinessCommand()
        {
            var connection = new ScriptedConnection(
                Step.Error("tabs.query", -100099),
                Step.Success("getVersion", "2.9.0"),
                Step.Success("getCodeId", "different-code-id"),
                Step.Error("init", 42));
            var provider = new RecoveryWebDataProvider(null) { Connection = connection };

            RPAException exception = Assert.Throws<RPAException>(() =>
                provider.Send("tabs.query", new JObject()));

            Assert.Equal(eErrorMessageType.Error_CrxScriptInitializationError, exception.ErrorMessageType);
            Assert.Equal(1, Count(connection.Commands, "tabs.query"));
            Assert.Equal(new[] { "tabs.query", "getVersion", "getCodeId", "init" }, connection.Commands);
        }

        [Fact]
        public void SuccessfulInitializationResendsOriginalBusinessCommandExactlyOnce()
        {
            var connection = new ScriptedConnection(
                Step.Error("tabs.query", -100099),
                Step.Success("getVersion", "2.9.0"),
                Step.Success("getCodeId", ExtensionProvider.Instance.CodeId),
                Step.Success("tabs.query", "ok"));
            var provider = new RecoveryWebDataProvider(null) { Connection = connection };

            JToken result = provider.Send("tabs.query", new JObject(), 1000, true);

            Assert.Equal("ok", result.ToString());
            Assert.Equal(2, Count(connection.Commands, "tabs.query"));
            Assert.Equal(new[] { "tabs.query", "getVersion", "getCodeId", "tabs.query" }, connection.Commands);
        }

        [Fact]
        public void TwoInitializationConnectionFailuresDoNotResendBusinessCommand()
        {
            var firstFailure = new BrowserConnectionException(
                "NMH-1 closed",
                ClientTransferCompletionReason.ConnectionClosed);
            var secondFailure = new BrowserConnectionException(
                "NMH-2 closed",
                ClientTransferCompletionReason.SendFailed);
            var first = new ScriptedConnection(
                Step.Error("tabs.query", -100099),
                Step.Throw("getVersion", firstFailure));
            var second = new ScriptedConnection(Step.Throw("getVersion", secondFailure));
            var provider = new RecoveryWebDataProvider(second) { Connection = first };

            RPAException exception = Assert.Throws<RPAException>(() =>
                provider.Send("tabs.query", new JObject()));

            Assert.Same(secondFailure, exception.InnerException);
            Assert.Equal(1, Count(first.Commands, "tabs.query"));
            Assert.DoesNotContain("tabs.query", second.Commands);
            Assert.Equal(1, provider.FactoryCallCount);
        }

        [Fact]
        public void RecoverySuccessResendsBusinessCommandOnlyOnReplacement()
        {
            var first = new ScriptedConnection(
                Step.Error("tabs.query", -100099),
                Step.Throw("getVersion", new BrowserConnectionException(
                    "NMH-1 closed",
                    ClientTransferCompletionReason.ConnectionClosed)));
            var second = new ScriptedConnection(
                Step.Success("getVersion", "2.9.0"),
                Step.Success("getCodeId", ExtensionProvider.Instance.CodeId),
                Step.Success("tabs.query", "replacement-result"));
            var provider = new RecoveryWebDataProvider(second) { Connection = first };

            JToken result = provider.Send("tabs.query", new JObject());

            Assert.Equal("replacement-result", result.ToString());
            Assert.Equal(1, Count(first.Commands, "tabs.query"));
            Assert.Equal(1, Count(second.Commands, "tabs.query"));
            Assert.Equal(2, Count(AllCommands(first, second), "tabs.query"));
        }

        [Fact]
        public void DiagnosticsIdentifyInitStageWithoutLoggingPayloadOrConnectionSecrets()
        {
            var connection = new ScriptedConnection(
                Step.Success("getVersion", "2.9.0"),
                Step.Success("getCodeId", "different-code-id"),
                Step.Error("init", 42));
            var provider = new RecoveryWebDataProvider(null) { Connection = connection };

            CrxScriptInitializationResult result = provider.InitializeForTest();

            Assert.Equal(CrxScriptInitializationStage.Init, result.Stage);
            string diagnostics = string.Join("\n", provider.Diagnostics);
            Assert.Contains("Started", diagnostics);
            Assert.Contains("StageFailed", diagnostics);
            Assert.Contains("Stage=Init", diagnostics);
            Assert.Contains("Attempt=1", diagnostics);
            Assert.Contains("RecoveryCount=0", diagnostics);
            Assert.Contains("ElapsedMs=", diagnostics);
            Assert.DoesNotContain(ExtensionProvider.Instance.Backgroud, diagnostics);
            Assert.DoesNotContain(ExtensionProvider.Instance.CodeId, diagnostics);
            Assert.DoesNotContain("MessageId", diagnostics);
            Assert.DoesNotContain("ServerKey", diagnostics);
            Assert.DoesNotContain("pipe", diagnostics, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ThrowingDiagnosticSinkDoesNotChangeInitializationResult()
        {
            var connection = new ScriptedConnection(
                Step.Success("getVersion", "2.9.0"),
                Step.Success("getCodeId", ExtensionProvider.Instance.CodeId));
            var provider = new RecoveryWebDataProvider(null)
            {
                Connection = connection,
                ThrowDiagnostics = true
            };

            CrxScriptInitializationResult result = provider.InitializeForTest();

            Assert.Equal(CrxScriptInitializationCompletion.Success, result.Completion);
            Assert.Equal(CrxScriptInitializationPath.CodeIdMatched, result.Path);
        }

        [Fact]
        public void DerivedSendOverrideWorksWithoutConnectionOrCrxInitialization()
        {
            var provider = new ConnectionlessWebDataProvider();

            JToken result = provider.Send("ie.independent", new JObject());

            Assert.Equal("ie-result", result.ToString());
            Assert.Equal(1, provider.SendCount);
            Assert.Null(provider.Connection);
        }

        private static IEnumerable<string> AllCommands(
            ScriptedConnection first,
            ScriptedConnection second)
        {
            foreach (string command in first.Commands)
                yield return command;
            foreach (string command in second.Commands)
                yield return command;
        }

        private static int Count(IEnumerable<string> commands, string command)
        {
            int count = 0;
            foreach (string item in commands)
            {
                if (item == command)
                    count++;
            }
            return count;
        }

        private sealed class RecoveryWebDataProvider : WebDataProvider
        {
            private readonly IBrowserConnectionNew _replacement;

            internal RecoveryWebDataProvider(IBrowserConnectionNew replacement)
                : base(new ConnectionConfig { GuiType = eGuiType.Chrome })
            {
                _replacement = replacement;
            }

            internal int FactoryCallCount { get; private set; }
            internal int LastRemainingMilliseconds { get; private set; }
            internal List<string> Diagnostics { get; } = new List<string>();
            internal bool ThrowDiagnostics { get; set; }
            internal Exception RecoveryException { get; set; }

            internal CrxScriptInitializationResult InitializeForTest() =>
                InitializeScriptsWithRecovery(false);

            protected override IBrowserConnectionNew AcquireScriptRecoveryConnection(
                int remainingMilliseconds,
                bool ignoreConnection)
            {
                FactoryCallCount++;
                LastRemainingMilliseconds = remainingMilliseconds;
                if (RecoveryException != null)
                    throw RecoveryException;
                return _replacement;
            }

            protected override void WriteScriptInitializationDiagnostic(string message)
            {
                if (ThrowDiagnostics)
                    throw new InvalidOperationException("diagnostic sink failed");
                Diagnostics.Add(message);
            }
        }

        private sealed class ConnectionlessWebDataProvider : WebDataProvider
        {
            internal ConnectionlessWebDataProvider()
                : base(new ConnectionConfig { GuiType = eGuiType.IE })
            {
            }

            internal int SendCount { get; private set; }

            public override JToken Send(
                string command,
                JObject @params,
                int timeout = 30000,
                bool ignoreConnection = false)
            {
                SendCount++;
                return "ie-result";
            }
        }

        private sealed class Step
        {
            internal string Command { get; private set; }
            internal PipeResponse Response { get; private set; }
            internal Exception Exception { get; private set; }

            internal static Step Success(string command, string content) => new Step
            {
                Command = command,
                Response = new PipeResponse
                {
                    Code = 200,
                    Result = new PipeResult
                    {
                        Content = content == null ? null : JToken.FromObject(content),
                        Error = null
                    }
                }
            };

            internal static Step Throw(string command, Exception exception) => new Step
            {
                Command = command,
                Exception = exception
            };

            internal static Step Error(string command, int errorCode) => new Step
            {
                Command = command,
                Response = new PipeResponse
                {
                    Code = 200,
                    Result = new PipeResult
                    {
                        Error = new RPCResponseError { Code = errorCode, Message = "script unavailable" }
                    }
                }
            };
        }

        private sealed class ScriptedConnection : IBrowserConnectionNew
        {
            private readonly Queue<Step> _steps;

            internal ScriptedConnection(params Step[] steps) =>
                _steps = new Queue<Step>(steps);

            internal List<string> Commands { get; } = new List<string>();
            internal Action OnStop { get; set; }
            internal int StopCount { get; private set; }
            public bool IsConnected { get; set; } = true;
            public InitScripeStateType InitScripeState { get; set; }
            public PipeResponse PipeResponse { get; private set; }
            public string CrxVersion { get; set; }
            public int ConnectTimeout { get; set; }
            public bool CrxIsLatestOrNewer => true;
            public void Start() { }
            public void Stop()
            {
                StopCount++;
                IsConnected = false;
                OnStop?.Invoke();
            }

            public PipeResponse Send(eGuiType guiType, string command, JObject @params, int timeout = 5000)
            {
                Commands.Add(command);
                Step step = _steps.Dequeue();
                Assert.Equal(step.Command, command);
                if (step.Exception != null)
                    throw step.Exception;
                return PipeResponse = step.Response;
            }

            public bool TestConnection() => IsConnected;
            public bool TestConnection(eGuiType guiType, int timeOut = 300) => IsConnected;
        }
    }
}
