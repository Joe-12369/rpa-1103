using II.RPA.Plugin.Core;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.connection;
using II.RPA.WebBrowser.Common.Basic.connection.ClientTransfer;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System;
using System.Linq;
using Xunit;

namespace II.RPA.WebBrowser.Common.Tests
{
    public class CrxScriptInitializerTests
    {
        [Fact]
        public void Mv3CompletesWithoutLegacyInjection()
        {
            var connection = new ScriptedConnection(
                Step.Success("getVersion", "3.0.0"));

            CrxScriptInitializationResult result =
                new CrxScriptInitializer().Initialize(connection, eGuiType.Chrome, 1);

            Assert.Equal(CrxScriptInitializationCompletion.Success, result.Completion);
            Assert.Equal(CrxScriptInitializationPath.Mv3NoLegacyInjection, result.Path);
            Assert.Equal(InitScripeStateType.Complete, connection.InitScripeState);
            Assert.Equal(new[] { "getVersion" }, connection.Commands);
        }

        [Fact]
        public void MatchingCodeIdCompletesWithoutInjection()
        {
            var connection = new ScriptedConnection(
                Step.Success("getVersion", "2.9.0"),
                Step.Success("getCodeId", ExtensionProvider.Instance.CodeId));

            CrxScriptInitializationResult result =
                new CrxScriptInitializer().Initialize(connection, eGuiType.Chrome, 1);

            Assert.Equal(CrxScriptInitializationCompletion.Success, result.Completion);
            Assert.Equal(CrxScriptInitializationPath.CodeIdMatched, result.Path);
            Assert.Equal(InitScripeStateType.Complete, connection.InitScripeState);
            Assert.DoesNotContain("init", connection.Commands);
            Assert.DoesNotContain("initContentScript", connection.Commands);
        }

        public static IEnumerable<object[]> InvalidInitResponses()
        {
            yield return new object[] { null };
            yield return new object[] { Response(500, new PipeResult { Error = null }) };
            yield return new object[] { Response(200, null) };
            yield return new object[] { Response(200, new PipeResult { Error = new RPCResponseError { Code = 42 } }) };
        }

        [Theory]
        [MemberData(nameof(InvalidInitResponses))]
        public void InvalidInitResponseFailsAtInitAndDoesNotSendContent(PipeResponse initResponse)
        {
            var connection = NeedsInjection(Step.Return("init", initResponse));

            CrxScriptInitializationResult result =
                new CrxScriptInitializer().Initialize(connection, eGuiType.Chrome, 1);

            Assert.Equal(CrxScriptInitializationCompletion.InitializationFailed, result.Completion);
            Assert.Equal(CrxScriptInitializationStage.Init, result.Stage);
            Assert.Equal(InitScripeStateType.None, connection.InitScripeState);
            Assert.DoesNotContain("initContentScript", connection.Commands);
        }

        [Fact]
        public void InitExceptionFailsAtInitAndDoesNotSendContent()
        {
            var expected = new InvalidOperationException("init failed");
            var connection = NeedsInjection(Step.Throw("init", expected));

            CrxScriptInitializationResult result =
                new CrxScriptInitializer().Initialize(connection, eGuiType.Chrome, 1);

            Assert.Equal(CrxScriptInitializationCompletion.InitializationFailed, result.Completion);
            Assert.Equal(CrxScriptInitializationStage.Init, result.Stage);
            Assert.Same(expected, result.Exception);
            Assert.DoesNotContain("initContentScript", connection.Commands);
        }

        [Fact]
        public void InitWithNullContentContinuesToContentInitialization()
        {
            var connection = NeedsInjection(
                Step.Return("init", Response(200, new PipeResult { Error = null })),
                Step.Return("initContentScript", Response(200, new PipeResult { Error = null })));

            CrxScriptInitializationResult result =
                new CrxScriptInitializer().Initialize(connection, eGuiType.Chrome, 1);

            Assert.Equal(CrxScriptInitializationCompletion.Success, result.Completion);
            Assert.Equal(CrxScriptInitializationPath.Injected, result.Path);
            Assert.Equal(InitScripeStateType.Complete, connection.InitScripeState);
        }

        public static IEnumerable<object[]> InvalidContentResponses()
        {
            yield return new object[] { null };
            yield return new object[] { Response(500, new PipeResult { Error = null }) };
            yield return new object[] { Response(200, null) };
            yield return new object[] { Response(200, new PipeResult { Error = new RPCResponseError { Code = 42 } }) };
        }

        [Theory]
        [MemberData(nameof(InvalidContentResponses))]
        public void InvalidContentResponseFailsAtContentStage(PipeResponse contentResponse)
        {
            var connection = NeedsInjection(
                SuccessfulInit(),
                Step.Return("initContentScript", contentResponse));

            CrxScriptInitializationResult result =
                new CrxScriptInitializer(new FakeRetryClock()).Initialize(connection, eGuiType.Chrome, 1);

            Assert.Equal(CrxScriptInitializationCompletion.InitializationFailed, result.Completion);
            Assert.Equal(CrxScriptInitializationStage.InitContentScript, result.Stage);
            Assert.Equal(InitScripeStateType.None, connection.InitScripeState);
            Assert.Equal(1, connection.Commands.Count(command => command == "initContentScript"));
        }

        [Theory]
        [InlineData(3)]
        [InlineData(-100099)]
        [InlineData(-1000100)]
        public void RetryableContentErrorRetriesThenSucceeds(int errorCode)
        {
            var clock = new FakeRetryClock();
            var connection = NeedsInjection(
                SuccessfulInit(),
                Step.Return("initContentScript", ErrorResponse(errorCode)),
                Step.Return("initContentScript", SuccessfulResponse()));

            CrxScriptInitializationResult result =
                new CrxScriptInitializer(clock).Initialize(connection, eGuiType.Chrome, 1);

            Assert.Equal(CrxScriptInitializationCompletion.Success, result.Completion);
            Assert.Equal(CrxScriptInitializationPath.Injected, result.Path);
            Assert.Equal(2, connection.Commands.Count(command => command == "initContentScript"));
            Assert.Equal(new[] { 500 }, clock.Delays);
        }

        [Fact]
        public void NonRetryableContentErrorDoesNotRetry()
        {
            var connection = NeedsInjection(
                SuccessfulInit(),
                Step.Return("initContentScript", ErrorResponse(42)));

            CrxScriptInitializationResult result =
                new CrxScriptInitializer(new FakeRetryClock()).Initialize(connection, eGuiType.Chrome, 1);

            Assert.Equal(CrxScriptInitializationCompletion.InitializationFailed, result.Completion);
            Assert.Equal(CrxScriptInitializationStage.InitContentScript, result.Stage);
            Assert.Equal(1, connection.Commands.Count(command => command == "initContentScript"));
        }

        [Fact]
        public void RetryableContentErrorStopsAtTenSecondBudget()
        {
            var clock = new FakeRetryClock();
            var retrySteps = Enumerable.Range(0, 20)
                .Select(_ => Step.Return("initContentScript", ErrorResponse(3)))
                .ToArray();
            var connection = NeedsInjection(new[] { SuccessfulInit() }.Concat(retrySteps).ToArray());

            CrxScriptInitializationResult result =
                new CrxScriptInitializer(clock).Initialize(connection, eGuiType.Chrome, 1);

            Assert.Equal(CrxScriptInitializationCompletion.InitializationFailed, result.Completion);
            Assert.Equal(CrxScriptInitializationStage.InitContentScript, result.Stage);
            Assert.Equal(InitScripeStateType.None, connection.InitScripeState);
            Assert.Equal(20, connection.Commands.Count(command => command == "initContentScript"));
            Assert.Equal(10000, clock.ElapsedMilliseconds);
        }

        [Fact]
        public void ContentRequestTimeoutRetriesThenSucceeds()
        {
            var clock = new FakeRetryClock();
            var connection = NeedsInjection(
                SuccessfulInit(),
                Step.Throw("initContentScript", new BrowserConnectionException(
                    "timeout", ClientTransferCompletionReason.Timeout)),
                Step.Return("initContentScript", SuccessfulResponse()));
            connection.OnSend = (command, timeout) =>
            {
                if (command == "initContentScript") clock.Advance(2000);
            };

            var result = new CrxScriptInitializer(clock).Initialize(connection, eGuiType.Edge, 1);

            Assert.Equal(CrxScriptInitializationCompletion.Success, result.Completion);
            Assert.Equal(InitScripeStateType.Complete, connection.InitScripeState);
            Assert.Equal(new[] { 500 }, clock.Delays);
        }

        [Fact]
        public void ContentRequestTimeoutUsesRemainingBudgetAndPreservesLastException()
        {
            var clock = new FakeRetryClock();
            var expected = new BrowserConnectionException("timeout", ClientTransferCompletionReason.Timeout);
            var connection = NeedsInjection(new[] { SuccessfulInit() }.Concat(
                Enumerable.Range(0, 5).Select(_ => Step.Throw("initContentScript", expected))).ToArray());
            var timeouts = new List<int>();
            connection.OnSend = (command, timeout) =>
            {
                if (command != "initContentScript") return;
                timeouts.Add(timeout);
                clock.Advance(timeout);
            };

            var result = new CrxScriptInitializer(clock).Initialize(connection, eGuiType.Edge, 1);

            Assert.Equal(CrxScriptInitializationCompletion.InitializationFailed, result.Completion);
            Assert.Equal(CrxScriptInitializationStage.InitContentScript, result.Stage);
            Assert.Same(expected, result.Exception);
            Assert.Equal(new[] { 2000, 2000, 2000, 2000 }, timeouts);
            Assert.Equal(10000, clock.ElapsedMilliseconds);
            Assert.Equal(InitScripeStateType.None, connection.InitScripeState);
        }

        [Fact]
        public void ContentRequestIsClampedToBudgetRemainingAfterSlowResponse()
        {
            var clock = new FakeRetryClock();
            var connection = NeedsInjection(SuccessfulInit(),
                Step.Return("initContentScript", ErrorResponse(3)),
                Step.Return("initContentScript", SuccessfulResponse()));
            var timeouts = new List<int>();
            connection.OnSend = (command, timeout) =>
            {
                if (command != "initContentScript") return;
                timeouts.Add(timeout);
                if (timeouts.Count == 1) clock.Advance(8800);
            };

            var result = new CrxScriptInitializer(clock).Initialize(connection, eGuiType.Edge, 1);

            Assert.Equal(CrxScriptInitializationCompletion.Success, result.Completion);
            Assert.Equal(new[] { 2000, 700 }, timeouts);
        }

        public static IEnumerable<object[]> ConnectionFailuresAtEveryStage()
        {
            var stages = new[]
            {
                CrxScriptInitializationStage.TestConnection,
                CrxScriptInitializationStage.GetVersion,
                CrxScriptInitializationStage.GetCodeId,
                CrxScriptInitializationStage.Init,
                CrxScriptInitializationStage.InitContentScript
            };
            var reasons = new[]
            {
                ClientTransferCompletionReason.ConnectionClosed,
                ClientTransferCompletionReason.SendFailed,
                ClientTransferCompletionReason.ProtocolError,
                ClientTransferCompletionReason.Stopped
            };

            foreach (CrxScriptInitializationStage stage in stages)
            foreach (ClientTransferCompletionReason reason in reasons)
                yield return new object[] { (int)stage, (int)reason };
        }

        [Theory]
        [MemberData(nameof(ConnectionFailuresAtEveryStage))]
        public void StructuredConnectionFailureIsClassifiedAtExactStage(
            int stageValue,
            int reasonValue)
        {
            var stage = (CrxScriptInitializationStage)stageValue;
            var reason = (ClientTransferCompletionReason)reasonValue;
            var expected = new BrowserConnectionException("disconnected", reason);
            ScriptedConnection connection = ConnectionFailingAt(stage, expected);

            CrxScriptInitializationResult result =
                new CrxScriptInitializer(new FakeRetryClock()).Initialize(connection, eGuiType.Chrome, 1);

            Assert.Equal(CrxScriptInitializationCompletion.ConnectionFailed, result.Completion);
            Assert.Equal(stage, result.Stage);
            Assert.Same(connection, result.Connection);
            Assert.Same(expected, result.Exception);
            Assert.Equal(InitScripeStateType.None, connection.InitScripeState);
            Assert.Equal(0, connection.StopCount);
        }

        [Fact]
        public void TimeoutIsInitializationFailureNotConnectionFailure()
        {
            var expected = new TimeoutException("business timeout");
            ScriptedConnection connection = ConnectionFailingAt(CrxScriptInitializationStage.Init, expected);

            CrxScriptInitializationResult result =
                new CrxScriptInitializer(new FakeRetryClock()).Initialize(connection, eGuiType.Chrome, 1);

            Assert.Equal(CrxScriptInitializationCompletion.InitializationFailed, result.Completion);
            Assert.Equal(CrxScriptInitializationStage.Init, result.Stage);
            Assert.Same(expected, result.Exception);
        }

        [Fact]
        public void StaticInjectScriptReturnsFalseForNullConnectionWithoutThrowing()
        {
            var parameters = new InjectScriptParam
            {
                Connection = null,
                GuiType = eGuiType.Chrome,
                IsPreInjection = true
            };

            bool result = WebDataProvider.InjectScript(parameters);

            Assert.False(result);
            Assert.False(parameters.IsInitial);
            Assert.False(string.IsNullOrWhiteSpace(parameters.ErrMessage));
        }

        [Fact]
        public void StaticInjectScriptDoesNotReportSuccessWhenInitFails()
        {
            var connection = NeedsInjection(
                Step.Return("init", Response(500, new PipeResult { Error = null })));
            var parameters = new InjectScriptParam
            {
                Connection = connection,
                GuiType = eGuiType.Chrome
            };

            bool result = WebDataProvider.InjectScript(parameters);

            Assert.False(result);
            Assert.False(parameters.IsInitial);
            Assert.Equal(InitScripeStateType.None, connection.InitScripeState);
        }

        [Fact]
        public void ExplicitInitialScripeDoesNotThrowWhenConnectionIsNull()
        {
            var provider = new CompatibilityWebDataProvider();

            Exception exception = Record.Exception(() => provider.InitialScripe());

            Assert.Null(exception);
        }

        [Fact]
        public void GetConnCrxVersionReturnsCachedValueWithoutSending()
        {
            var connection = new ScriptedConnection { CrxVersion = "4.2.3" };
            var provider = new CompatibilityWebDataProvider { Connection = connection };

            string version = provider.GetConnCrxVersion();

            Assert.Equal("4.2.3", version);
            Assert.Empty(connection.Commands);
        }

        private static ScriptedConnection ConnectionFailingAt(
            CrxScriptInitializationStage stage,
            Exception exception)
        {
            if (stage == CrxScriptInitializationStage.TestConnection)
                return new ScriptedConnection { TestConnectionException = exception };

            var steps = new List<Step>();
            if (stage == CrxScriptInitializationStage.GetVersion)
                steps.Add(Step.Throw("getVersion", exception));
            else
            {
                steps.Add(Step.Success("getVersion", "2.9.0"));
                if (stage == CrxScriptInitializationStage.GetCodeId)
                    steps.Add(Step.Throw("getCodeId", exception));
                else
                {
                    steps.Add(Step.Success("getCodeId", "different-code-id"));
                    if (stage == CrxScriptInitializationStage.Init)
                        steps.Add(Step.Throw("init", exception));
                    else
                    {
                        steps.Add(SuccessfulInit());
                        steps.Add(Step.Throw("initContentScript", exception));
                    }
                }
            }
            return new ScriptedConnection(steps.ToArray());
        }

        private static Step SuccessfulInit() => Step.Return("init", SuccessfulResponse());

        private static PipeResponse SuccessfulResponse() =>
            Response(200, new PipeResult { Error = null });

        private static PipeResponse ErrorResponse(int errorCode) =>
            Response(200, new PipeResult { Error = new RPCResponseError { Code = errorCode } });

        private static ScriptedConnection NeedsInjection(params Step[] tail)
        {
            var steps = new List<Step>
            {
                Step.Success("getVersion", "2.9.0"),
                Step.Success("getCodeId", "different-code-id")
            };
            steps.AddRange(tail);
            return new ScriptedConnection(steps.ToArray());
        }

        private static PipeResponse Response(int code, PipeResult result) =>
            new PipeResponse { Code = code, Result = result };

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

            internal static Step Return(string command, PipeResponse response) => new Step
            {
                Command = command,
                Response = response
            };

            internal static Step Throw(string command, Exception exception) => new Step
            {
                Command = command,
                Exception = exception
            };
        }

        private sealed class ScriptedConnection : IBrowserConnectionNew
        {
            private readonly Queue<Step> _steps;

            internal ScriptedConnection(params Step[] steps) =>
                _steps = new Queue<Step>(steps);

            internal List<string> Commands { get; } = new List<string>();
            internal Action<string, int> OnSend { get; set; }
            internal Exception TestConnectionException { get; set; }
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
            }

            public PipeResponse Send(eGuiType guiType, string command, JObject @params, int timeout = 5000)
            {
                Commands.Add(command);
                Step step = _steps.Dequeue();
                Assert.Equal(step.Command, command);
                OnSend?.Invoke(command, timeout);
                if (step.Exception != null)
                {
                    throw step.Exception;
                }
                return PipeResponse = step.Response;
            }

            public bool TestConnection() => IsConnected;
            public bool TestConnection(eGuiType guiType, int timeOut = 300)
            {
                if (TestConnectionException != null)
                    throw TestConnectionException;
                return IsConnected;
            }
        }

        private sealed class FakeRetryClock : ICrxScriptInitializationRetryClock
        {
            internal List<int> Delays { get; } = new List<int>();
            public long ElapsedMilliseconds { get; private set; }

            public void Restart()
            {
                ElapsedMilliseconds = 0;
                Delays.Clear();
            }

            public void Delay(int milliseconds)
            {
                Delays.Add(milliseconds);
                ElapsedMilliseconds += milliseconds;
            }

            internal void Advance(int milliseconds) => ElapsedMilliseconds += milliseconds;
        }

        private sealed class CompatibilityWebDataProvider : WebDataProvider
        {
            internal CompatibilityWebDataProvider()
                : base(new ConnectionConfig { GuiType = eGuiType.Chrome })
            {
            }
        }
    }
}
