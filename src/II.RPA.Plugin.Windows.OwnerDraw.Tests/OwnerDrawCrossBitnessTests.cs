using II.RPA.Plugin.Windows.OwnerDraw;
using II.RPA.Plugin.Windows.OwnerDraw.PowerBuilder;
using II.RPA.Plugin.Windows.OwnerDraw.Vcl;
using System;
using System.Diagnostics;
using System.IO;
using Xunit;

namespace II.RPA.Plugin.Windows.OwnerDraw.Tests
{
    public sealed class OwnerDrawCrossBitnessTests
    {
        // The resident-lifecycle decision inside NativeOwnerDrawBridgeActivator
        // enforces ABI 6, matching the native Bridge ABI/BuildId. These tests
        // exercise that decision, so they pin the native ABI (6) explicitly rather
        // than reading the managed client's RequiredAbi.
        private const int NativePowerBuilderResidentAbi = 6;

        [Theory]
        [InlineData((int)OwnerDrawArchitecture.X86)]
        [InlineData((int)OwnerDrawArchitecture.X64)]
        public void SameBitnessPowerBuilderUsesDirectActivation(
            int architectureValue)
        {
            var architecture = (OwnerDrawArchitecture)architectureValue;
            Assert.True(OwnerDrawBridgeArchitecturePolicy.TryResolve(
                architecture, architecture, PowerBuilderDataWindowAdapter.Descriptor,
                out OwnerDrawBridgeActivationRoute route, out string status), status);
            Assert.Equal(OwnerDrawBridgeActivationRoute.Direct, route);
            Assert.Contains("route=direct", status);
        }

        [Theory]
        [InlineData((int)OwnerDrawArchitecture.X86)]
        [InlineData((int)OwnerDrawArchitecture.X64)]
        public void SameBitnessVclUsesDirectActivation(
            int architectureValue)
        {
            var architecture = (OwnerDrawArchitecture)architectureValue;
            Assert.True(OwnerDrawBridgeArchitecturePolicy.TryResolve(
                architecture, architecture, VclGridAdapter.Descriptor,
                out OwnerDrawBridgeActivationRoute route, out string status), status);
            Assert.Equal(OwnerDrawBridgeActivationRoute.Direct, route);
        }

        [Fact]
        public void X64ControllerToX86PowerBuilderUsesX86Broker()
        {
            Assert.True(OwnerDrawBridgeArchitecturePolicy.TryResolve(
                OwnerDrawArchitecture.X64, OwnerDrawArchitecture.X86,
                PowerBuilderDataWindowAdapter.Descriptor,
                out OwnerDrawBridgeActivationRoute route, out string status), status);
            Assert.Equal(OwnerDrawBridgeActivationRoute.X86Broker, route);
            Assert.Contains("route=x86-broker", status);
        }

        [Fact]
        public void X64ControllerToX86VclRemainsUnsupported()
        {
            Assert.False(OwnerDrawBridgeArchitecturePolicy.TryResolve(
                OwnerDrawArchitecture.X64, OwnerDrawArchitecture.X86,
                VclGridAdapter.Descriptor,
                out OwnerDrawBridgeActivationRoute route, out string status));
            Assert.Equal(OwnerDrawBridgeActivationRoute.Direct, route);
            Assert.Contains("does not allow", status);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void X86ControllerNeverActivatesX64Target(bool powerBuilder)
        {
            OwnerDrawBridgeDescriptor descriptor = powerBuilder
                ? PowerBuilderDataWindowAdapter.Descriptor
                : VclGridAdapter.Descriptor;

            Assert.False(OwnerDrawBridgeArchitecturePolicy.TryResolve(
                OwnerDrawArchitecture.X86, OwnerDrawArchitecture.X64,
                descriptor, out _, out string status));
            Assert.Contains("x86 RPA cannot", status);
        }

        [Fact]
        public void MissingDescriptorFailsClosed()
        {
            Assert.False(OwnerDrawBridgeArchitecturePolicy.TryResolve(
                OwnerDrawArchitecture.X64, OwnerDrawArchitecture.X86, null,
                out _, out string status));
            Assert.Contains("requires a Bridge descriptor", status);
        }

        [Fact]
        public void PackagedPowerBuilderBuildIdIsAccepted()
        {
            Assert.Equal(0x50420005u,
                NativeOwnerDrawBridgeActivator.RequiredPowerBuilderBridgeBuildId);
            Assert.True(NativeOwnerDrawBridgeActivator.IsExpectedPowerBuilderBuildId(
                NativeOwnerDrawBridgeActivator.RequiredPowerBuilderBridgeBuildId,
                out string status), status);
        }

        [Fact]
        public void StaleResidentPowerBuilderBuildIsRejectedEvenWhenDiskPathWasUpdated()
        {
            // A mapped image can keep the old executable pages after an installer
            // replaces the DLL at the same path. Comparing module.FileName hashes
            // would then hash the new disk file twice; the in-memory export is the
            // independent identity that must fail closed before StartWork/EndWork.
            uint staleResidentBuildId
                = NativeOwnerDrawBridgeActivator.RequiredPowerBuilderBridgeBuildId - 1;

            Assert.False(NativeOwnerDrawBridgeActivator.IsExpectedPowerBuilderBuildId(
                staleResidentBuildId, out string status));
            Assert.Contains("resident image is not this packaged build", status);
            Assert.Contains("Restart", status);
            Assert.Contains("not called or unloaded", status);
        }

        [Fact]
        public void MatchingResidentPowerBuilderIsReusedAndTransfersController()
        {
            const int controllerProcessId = 456;
            PowerBuilderResidentLifecycleDecision decision
                = NativeOwnerDrawBridgeActivator.DecidePowerBuilderResidentLifecycle(
                    resident: true, abiAvailable: true,
                    residentAbi: NativePowerBuilderResidentAbi,
                    buildIdAvailable: true,
                    residentBuildId: NativeOwnerDrawBridgeActivator
                        .RequiredPowerBuilderBridgeBuildId,
                    start: true, controllerProcessId: controllerProcessId);

            Assert.False(decision.LoadLibrary);
            Assert.True(decision.CallStartWork);
            Assert.False(decision.CallEndWork);
            Assert.False(decision.CallFreeLibrary);
            Assert.False(decision.RestartRequired);
            Assert.Equal(controllerProcessId, decision.ControllerProcessId);
            Assert.Contains("transfer", decision.Status);
        }

        [Fact]
        public void MatchingResidentPowerBuilderStopNeverUnloads()
        {
            PowerBuilderResidentLifecycleDecision decision
                = NativeOwnerDrawBridgeActivator.DecidePowerBuilderResidentLifecycle(
                    resident: true, abiAvailable: true,
                    residentAbi: NativePowerBuilderResidentAbi,
                    buildIdAvailable: true,
                    residentBuildId: NativeOwnerDrawBridgeActivator
                        .RequiredPowerBuilderBridgeBuildId,
                    start: false, controllerProcessId: 456);

            Assert.False(decision.LoadLibrary);
            Assert.False(decision.CallStartWork);
            Assert.True(decision.CallEndWork);
            Assert.False(decision.CallFreeLibrary);
            Assert.False(decision.RestartRequired);
            Assert.Contains("remains resident", decision.Status);
        }

        [Theory]
        [InlineData(false, 0u, true, 0x50420005u)]
        [InlineData(true, 4u, true, 0x50420005u)]
        [InlineData(true, 6u, false, 0u)]
        [InlineData(true, 6u, true, 0x50420000u)]
        public void UnverifiedResidentPowerBuilderRequiresRestartAndNoLifecycleCall(
            bool abiAvailable, uint residentAbi, bool buildIdAvailable,
            uint residentBuildId)
        {
            PowerBuilderResidentLifecycleDecision start
                = NativeOwnerDrawBridgeActivator.DecidePowerBuilderResidentLifecycle(
                    true, abiAvailable, residentAbi, buildIdAvailable,
                    residentBuildId, true, 456);
            PowerBuilderResidentLifecycleDecision stop
                = NativeOwnerDrawBridgeActivator.DecidePowerBuilderResidentLifecycle(
                    true, abiAvailable, residentAbi, buildIdAvailable,
                    residentBuildId, false, 456);

            AssertRestartRequiredWithoutLifecycleCall(start);
            AssertRestartRequiredWithoutLifecycleCall(stop);
        }

        [Fact]
        public void FreshPowerBuilderStartLoadsOnceAndNeverPlansFreeLibrary()
        {
            PowerBuilderResidentLifecycleDecision decision
                = NativeOwnerDrawBridgeActivator.DecidePowerBuilderResidentLifecycle(
                    resident: false, abiAvailable: false, residentAbi: 0,
                    buildIdAvailable: false, residentBuildId: 0,
                    start: true, controllerProcessId: 456);

            Assert.True(decision.LoadLibrary);
            Assert.True(decision.CallStartWork);
            Assert.False(decision.CallEndWork);
            Assert.False(decision.CallFreeLibrary);
            Assert.False(decision.RestartRequired);
        }

        [Theory]
        [InlineData("x86", (int)OwnerDrawArchitecture.X86)]
        [InlineData("x64", (int)OwnerDrawArchitecture.X64)]
        public void PowerBuilderPingUsesExpectedTargetArchitecture(
            string responseArchitecture,
            int expectedArchitectureValue)
        {
            var expectedArchitecture
                = (OwnerDrawArchitecture)expectedArchitectureValue;
            string response = "OK\tabi=" + PowerBuilderGdiBridgeClient.RequiredAbi
                + "\tbuildId=" + NativeOwnerDrawBridgeActivator
                    .RequiredPowerBuilderBridgeBuildId
                + "\tadapter=powerbuilder-datawindow\tarch="
                + responseArchitecture + "\trunning=1\tstatus64=cG9uZw==";

            Assert.True(PowerBuilderGdiBridgeClient.TryValidatePingResponse(
                response, expectedArchitecture, "synthetic transport",
                out string status), status);
            OwnerDrawArchitecture wrongArchitecture
                = expectedArchitecture == OwnerDrawArchitecture.X86
                    ? OwnerDrawArchitecture.X64 : OwnerDrawArchitecture.X86;
            Assert.False(PowerBuilderGdiBridgeClient.TryValidatePingResponse(
                response, wrongArchitecture, "synthetic transport", out _));
        }

        [Theory]
        [InlineData("x86", (int)OwnerDrawArchitecture.X86)]
        [InlineData("x64", (int)OwnerDrawArchitecture.X64)]
        public void VclPingUsesExpectedTargetArchitecture(
            string responseArchitecture,
            int expectedArchitectureValue)
        {
            var expectedArchitecture
                = (OwnerDrawArchitecture)expectedArchitectureValue;
            string response = "OK\tabi=" + VclGridBridgeClient.RequiredAbi
                + "\tadapter=vcl-grid\tarch=" + responseArchitecture
                + "\trunning=1\tgdiVisibleCell=1\tgdiPlusText=1\tvisibleTable=0"
                + "\texactHeaderScan=1\trecordScrollResolve=1\trttiHelper=0"
                + "\tlogicalMetadata=0\tlogicalCellRead=0\tlogicalTable=0"
                + "\toffscreenRead=0\tfullLogicalModel=0\ttargetDependent=1"
                + "\tstatus64=cG9uZw==";

            Assert.True(VclGridBridgeClient.TryValidateCapabilitiesResponse(
                response, expectedArchitecture, "synthetic transport",
                out _, out string status), status);
            OwnerDrawArchitecture wrongArchitecture
                = expectedArchitecture == OwnerDrawArchitecture.X86
                    ? OwnerDrawArchitecture.X64 : OwnerDrawArchitecture.X86;
            Assert.False(VclGridBridgeClient.TryValidateCapabilitiesResponse(
                response, wrongArchitecture, "synthetic transport",
                out _, out _));
        }

        [Fact]
        public void BrokerArgumentsUseFixedGrammarAndPreserveControllerPid()
        {
            const string bridgePath = @"C:\Program Files\II RPA|现场\Mfc.x86.dll";
            string commandLine = OwnerDrawInjectorBrokerProtocol.BuildArguments(
                OwnerDrawInjectorBrokerOperation.Start, 123, 456, bridgePath,
                "MfcGdiAccBridge.dll", PowerBuilderGdiBridgeClient.RequiredAbi);

            string[] arguments = commandLine.Split(' ');
            Assert.Equal(7, arguments.Length);
            Assert.DoesNotContain(bridgePath, commandLine);
            Assert.True(OwnerDrawInjectorBrokerProtocol.TryParseRequest(arguments,
                out OwnerDrawInjectorBrokerRequest request, out string status), status);
            Assert.Equal(OwnerDrawInjectorBrokerOperation.Start, request.Operation);
            Assert.Equal(123, request.TargetProcessId);
            Assert.Equal(456, request.ControllerProcessId);
            Assert.Equal(PowerBuilderGdiBridgeClient.RequiredAbi,
                request.RequiredAbi);
            Assert.Equal(bridgePath, request.BridgePath);
            Assert.Equal("MfcGdiAccBridge.dll", request.LegacyFileName);
        }

        [Fact]
        public void BrokerArgumentsRejectInvalidMandatoryFields()
        {
            Assert.Throws<ArgumentException>(() =>
                OwnerDrawInjectorBrokerProtocol.BuildArguments(
                    OwnerDrawInjectorBrokerOperation.Start, 0, 456,
                    @"C:\Mfc.x86.dll", "MfcGdiAccBridge.dll", 5));
            Assert.Throws<ArgumentException>(() =>
                OwnerDrawInjectorBrokerProtocol.BuildArguments(
                    OwnerDrawInjectorBrokerOperation.Start, 123, 0,
                    @"C:\Mfc.x86.dll", "MfcGdiAccBridge.dll", 5));
            Assert.Throws<ArgumentException>(() =>
                OwnerDrawInjectorBrokerProtocol.BuildArguments(
                    OwnerDrawInjectorBrokerOperation.Start, 123, 456,
                    string.Empty, "MfcGdiAccBridge.dll", 5));
            Assert.Throws<ArgumentException>(() =>
                OwnerDrawInjectorBrokerProtocol.BuildArguments(
                    (OwnerDrawInjectorBrokerOperation)999, 123, 456,
                    @"C:\Mfc.x86.dll", "MfcGdiAccBridge.dll", 5));
        }

        [Theory]
        [InlineData("0")]
        [InlineData("1")]
        [InlineData("2")]
        [InlineData("start")]
        public void BrokerRequestRejectsNonCanonicalOperationToken(
            string operationToken)
        {
            string valid = OwnerDrawInjectorBrokerProtocol.BuildArguments(
                OwnerDrawInjectorBrokerOperation.Start, 123, 456,
                @"C:\Mfc.x86.dll", "MfcGdiAccBridge.dll", 5);
            string[] arguments = valid.Split(' ');
            arguments[1] = operationToken;

            Assert.False(OwnerDrawInjectorBrokerProtocol.TryParseRequest(arguments,
                out _, out _));
        }

        [Fact]
        public void BrokerResponseRoundTripsFailureWithoutFlatteningItsCause()
        {
            string output = OwnerDrawInjectorBrokerProtocol.SerializeResponse(
                OwnerDrawInjectorBrokerOperation.Start, false,
                "RestartRequired", "ResidentBuildMismatch",
                "驻留版本不一致；restart required; no FreeLibrary.") + "\r\n";

            Assert.True(OwnerDrawInjectorBrokerProtocol.TryParseResponse(output,
                OwnerDrawInjectorBrokerOperation.Start,
                out OwnerDrawInjectorBrokerResponse response,
                out string parseStatus), parseStatus);
            Assert.False(response.Success);
            Assert.Equal("RestartRequired", response.Code);
            Assert.Equal("ResidentBuildMismatch", response.Value);
            Assert.Contains("no FreeLibrary", response.Status);
        }

        [Theory]
        [InlineData("ODIBR1|Start|1|Ok|Started|\nextra")]
        [InlineData("ODIBR1|Stop|1|Ok|Started|")]
        [InlineData("ODIBR1|start|1|Ok|Started|")]
        [InlineData("ODIBR1|Start|2|Ok|Started|")]
        [InlineData("ODIBR1|Start|1|not-safe!|Started|")]
        [InlineData("ODIBR1|Start|1|Ok|Started|%%%")]
        [InlineData("ODIBR1|999|1|Ok|Started|")]
        [InlineData("ODIBR1|0|1|Ok|Started|")]
        [InlineData("ODIBR1|1|1|Ok|Started|")]
        [InlineData("ODIBR1|2|1|Ok|Started|")]
        public void BrokerResponseRejectsMalformedOrUnexpectedOutput(string output)
        {
            Assert.False(OwnerDrawInjectorBrokerProtocol.TryParseResponse(output,
                OwnerDrawInjectorBrokerOperation.Start, out _, out _));
        }

        [Fact]
        public void BrokerResponseRejectsOversizedOutput()
        {
            string output = new string('A',
                OwnerDrawInjectorBrokerProtocol.MaximumEncodedFieldLength + 1);
            Assert.False(OwnerDrawInjectorBrokerProtocol.TryParseResponse(output,
                OwnerDrawInjectorBrokerOperation.Start,
                out _, out string status));
            Assert.Contains("size limit", status);
        }

        [Fact]
        public void BrokerClientUsesHiddenNonShellProcessAndFixedTimeout()
        {
            using (var fixture = new BrokerClientFixture(new FakeBrokerRunner
            {
                Result = SuccessBrokerResult(OwnerDrawInjectorBrokerOperation.Start,
                    "Started"),
            }))
            {
                Assert.True(fixture.Client.TryStart(123, 456, fixture.BridgePath,
                    "MfcGdiAccBridge.dll", 5, out string status), status);
                ProcessStartInfo info = fixture.Runner.LastStartInfo;
                Assert.Equal(fixture.BrokerPath, info.FileName);
                Assert.Equal(fixture.DirectoryPath, info.WorkingDirectory);
                Assert.False(info.UseShellExecute);
                Assert.True(info.CreateNoWindow);
                Assert.True(info.RedirectStandardOutput);
                Assert.True(info.RedirectStandardError);
                Assert.Equal(ProcessWindowStyle.Hidden, info.WindowStyle);
                Assert.Equal(OwnerDrawInjectorBrokerClient.StartTimeoutMs,
                    fixture.Runner.LastTimeoutMs);

                Assert.True(OwnerDrawInjectorBrokerProtocol.TryParseRequest(
                    info.Arguments.Split(' '), out OwnerDrawInjectorBrokerRequest request,
                    out string parseStatus), parseStatus);
                Assert.Equal(456, request.ControllerProcessId);
            }
        }

        [Fact]
        public void BrokerClientFailsClosedOnTimeout()
        {
            using (var fixture = new BrokerClientFixture(new FakeBrokerRunner
            {
                Result = new OwnerDrawBrokerProcessResult
                {
                    Started = true,
                    TimedOut = true,
                    StandardOutput = string.Empty,
                    StandardError = string.Empty,
                    Error = string.Empty,
                },
            }))
            {
                Assert.False(fixture.Client.TryStart(123, 456,
                    fixture.BridgePath, "MfcGdiAccBridge.dll", 5,
                    out string status));
                Assert.Contains("bounded lifetime", status);
                Assert.Contains("never unloaded", status);
            }
        }

        [Fact]
        public void BrokerClientRejectsNonzeroExitEvenWithSuccessFrame()
        {
            OwnerDrawBrokerProcessResult result = SuccessBrokerResult(
                OwnerDrawInjectorBrokerOperation.Start, "Started");
            result.ExitCode = 17;
            using (var fixture = new BrokerClientFixture(new FakeBrokerRunner
            {
                Result = result,
            }))
            {
                Assert.False(fixture.Client.TryStart(123, 456,
                    fixture.BridgePath, "MfcGdiAccBridge.dll", 5,
                    out string status));
                Assert.Contains("exit=17", status);
            }
        }

        [Fact]
        public void BrokerClientRejectsUnexpectedStdErrOrExtraStdOut()
        {
            OwnerDrawBrokerProcessResult stderr = SuccessBrokerResult(
                OwnerDrawInjectorBrokerOperation.Start, "Started");
            stderr.StandardError = "debug noise";
            using (var fixture = new BrokerClientFixture(new FakeBrokerRunner
            {
                Result = stderr,
            }))
            {
                Assert.False(fixture.Client.TryStart(123, 456,
                    fixture.BridgePath, "MfcGdiAccBridge.dll", 5,
                    out string status));
                Assert.Contains("unexpected stderr", status);
            }

            OwnerDrawBrokerProcessResult stdout = SuccessBrokerResult(
                OwnerDrawInjectorBrokerOperation.Start, "Started");
            stdout.StandardOutput += "extra\n";
            using (var fixture = new BrokerClientFixture(new FakeBrokerRunner
            {
                Result = stdout,
            }))
            {
                Assert.False(fixture.Client.TryStart(123, 456,
                    fixture.BridgePath, "MfcGdiAccBridge.dll", 5, out _));
            }
        }

        [Fact]
        public void BrokerStopMapsOnlyKnownResidentStates()
        {
            using (var fixture = new BrokerClientFixture(new FakeBrokerRunner
            {
                Result = SuccessBrokerResult(OwnerDrawInjectorBrokerOperation.Stop,
                    "StoppedResident"),
            }))
            {
                Assert.True(fixture.Client.TryStop(123, 456, fixture.BridgePath,
                    "MfcGdiAccBridge.dll", 5,
                    out OwnerDrawBridgeStopState state, out string status), status);
                Assert.Equal(OwnerDrawBridgeStopState.StoppedResident, state);
                Assert.Equal(OwnerDrawInjectorBrokerClient.StopTimeoutMs,
                    fixture.Runner.LastTimeoutMs);
            }
        }

        [Fact]
        public void BrokerRouteAcceptsOnlyDirectChildOfOwnerDrawDirectory()
        {
            string directory = Path.Combine(Path.GetTempPath(),
                "owner-draw-root");
            string direct = Path.Combine(directory, "MfcGdiAccBridge.x86.dll");
            string nested = Path.Combine(directory, "nested",
                "MfcGdiAccBridge.x86.dll");

            Assert.True(OwnerDrawBridgeActivatorRouter.IsDirectChild(directory,
                direct));
            Assert.False(OwnerDrawBridgeActivatorRouter.IsDirectChild(directory,
                nested));
            Assert.False(OwnerDrawBridgeActivatorRouter.IsDirectChild(directory,
                null));
        }

        private static OwnerDrawBrokerProcessResult SuccessBrokerResult(
            OwnerDrawInjectorBrokerOperation operation, string value)
        {
            return new OwnerDrawBrokerProcessResult
            {
                Started = true,
                ExitCode = 0,
                StandardOutput = OwnerDrawInjectorBrokerProtocol.SerializeResponse(
                    operation, true, "Ok", value, "synthetic success") + "\n",
                StandardError = string.Empty,
                Error = string.Empty,
            };
        }

        private static void AssertRestartRequiredWithoutLifecycleCall(
            PowerBuilderResidentLifecycleDecision decision)
        {
            Assert.True(decision.RestartRequired);
            Assert.False(decision.LoadLibrary);
            Assert.False(decision.CallStartWork);
            Assert.False(decision.CallEndWork);
            Assert.False(decision.CallFreeLibrary);
            Assert.Contains("Restart", decision.Status);
            Assert.Contains("forbidden", decision.Status);
        }

        private sealed class FakeBrokerRunner : IOwnerDrawBrokerProcessRunner
        {
            internal OwnerDrawBrokerProcessResult Result { get; set; }

            internal ProcessStartInfo LastStartInfo { get; private set; }

            internal int LastTimeoutMs { get; private set; }

            public OwnerDrawBrokerProcessResult Run(ProcessStartInfo startInfo,
                int timeoutMs)
            {
                LastStartInfo = startInfo;
                LastTimeoutMs = timeoutMs;
                return Result;
            }
        }

        private sealed class BrokerClientFixture : IDisposable
        {
            internal BrokerClientFixture(FakeBrokerRunner runner)
            {
                Runner = runner;
                DirectoryPath = Path.Combine(Path.GetTempPath(),
                    "ii-rpa-broker-tests-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(DirectoryPath);
                BrokerPath = Path.Combine(DirectoryPath,
                    OwnerDrawInjectorBrokerClient.BrokerFileName);
                BridgePath = Path.Combine(DirectoryPath,
                    "MfcGdiAccBridge.x86.dll");
                File.WriteAllBytes(BrokerPath, Array.Empty<byte>());
                File.WriteAllBytes(BridgePath, Array.Empty<byte>());
                Client = new OwnerDrawInjectorBrokerClient(DirectoryPath, runner);
            }

            internal FakeBrokerRunner Runner { get; private set; }

            internal string DirectoryPath { get; private set; }

            internal string BrokerPath { get; private set; }

            internal string BridgePath { get; private set; }

            internal OwnerDrawInjectorBrokerClient Client { get; private set; }

            public void Dispose()
            {
                if (Directory.Exists(DirectoryPath))
                    Directory.Delete(DirectoryPath, true);
            }
        }
    }
}
