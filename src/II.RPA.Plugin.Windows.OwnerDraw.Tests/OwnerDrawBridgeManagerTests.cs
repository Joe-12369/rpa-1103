using II.RPA.Plugin.Windows.OwnerDraw;
using II.RPA.Plugin.Windows.OwnerDraw.PowerBuilder;
using II.RPA.Plugin.Windows.OwnerDraw.Vcl;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;

namespace II.RPA.Plugin.Windows.OwnerDraw.Tests
{
    public sealed class OwnerDrawBridgeManagerTests
    {
        [Fact]
        public void WindowProbeBindsNativeGetClassNameEntryPoint()
        {
            IntPtr desktop = GetDesktopWindow();
            Assert.NotEqual(IntPtr.Zero, desktop);

            string className = OwnerDrawWindowProbe.GetClassName(desktop);

            Assert.False(string.IsNullOrWhiteSpace(className));
        }

        [Fact]
        public void ProductDescriptorsDeclareTheValidatedLegacyModuleNames()
        {
            Assert.Equal("MfcGdiAccBridge.dll",
                PowerBuilderDataWindowAdapter.Descriptor.LegacyFileName);
            Assert.Equal("VclGridAccBridge.dll",
                VclGridAdapter.Descriptor.LegacyFileName);
        }

        [Fact]
        public void LegacyConflictBlocksStartStopPingAndHasExplicitStatus()
        {
            int pingCount = 0;
            OwnerDrawBridgeDescriptor descriptor = CreateDescriptor(
                delegate(int processId,
                    OwnerDrawArchitecture expectedArchitecture,
                    out string status)
                {
                    pingCount++;
                    status = "unexpected PING";
                    return true;
                });
            var activator = new FakeActivator
            {
                LegacyFound = true,
            };

            using (var target = new SameProcessTarget(descriptor))
            {
                var manager = new OwnerDrawBridgeManager(target.BridgeDirectory,
                    activator);

                OwnerDrawResult<OwnerDrawBridgeSession> start
                    = manager.EnsureReady(target.WindowHandle, descriptor);
                OwnerDrawResult<bool> stop = manager.StopResident(
                    descriptor.AdapterId, target.ProcessId, descriptor);
                OwnerDrawBridgeRuntimeStatus runtime
                    = OwnerDrawBridgeControl.GetStatusForDescriptor(
                        descriptor.AdapterId, descriptor, target.ProcessId,
                        "test host", manager);

                Assert.False(start.IsSuccess);
                Assert.Equal(OwnerDrawFailureCode.LegacyConflict, start.Code);
                Assert.False(stop.IsSuccess);
                Assert.Equal(OwnerDrawFailureCode.LegacyConflict, stop.Code);
                Assert.Equal(OwnerDrawBridgeRuntimeState.LegacyConflict,
                    runtime.State);
                Assert.Contains("LegacyConflict", runtime.Status);
                Assert.Contains("restart", runtime.Status,
                    StringComparison.OrdinalIgnoreCase);
                Assert.Equal(0, activator.StartCount);
                Assert.Equal(0, activator.StopCount);
                Assert.Equal(0, pingCount);
            }
        }

        [Fact]
        public void FailedPingAfterStartRollsBackExactlyOnceAndCachesNothing()
        {
            int pingCount = 0;
            OwnerDrawArchitecture? pingArchitecture = null;
            OwnerDrawBridgeDescriptor descriptor = CreateDescriptor(
                delegate(int processId,
                    OwnerDrawArchitecture expectedArchitecture,
                    out string status)
                {
                    pingCount++;
                    pingArchitecture = expectedArchitecture;
                    status = "synthetic PING failure";
                    return false;
                });
            var activator = new FakeActivator
            {
                StartSucceeds = true,
                StopSucceeds = true,
                StopState = OwnerDrawBridgeStopState.StoppedResident,
            };

            using (var target = new SameProcessTarget(descriptor))
            {
                var manager = new OwnerDrawBridgeManager(target.BridgeDirectory,
                    activator, startProbeAttempts: 3, startProbeDelayMs: 0);

                OwnerDrawResult<OwnerDrawBridgeSession> result
                    = manager.EnsureReady(target.WindowHandle, descriptor);

                Assert.False(result.IsSuccess);
                Assert.Equal(OwnerDrawFailureCode.BridgeStartFailed, result.Code);
                Assert.Contains("rollback EndWork completed", result.Status);
                Assert.Equal(1, activator.StartCount);
                Assert.Equal(1, activator.StopCount);
                Assert.Equal(1, pingCount);
                Assert.Equal(Environment.Is64BitProcess
                        ? OwnerDrawArchitecture.X64 : OwnerDrawArchitecture.X86,
                    pingArchitecture);
                Assert.Equal(Process.GetCurrentProcess().Id,
                    activator.LastControllerProcessId);
                Assert.Equal(descriptor.LegacyFileName,
                    activator.LastStartLegacyFileName);
                Assert.Equal(descriptor.LegacyFileName,
                    activator.LastStopLegacyFileName);
                Assert.False(manager.TryGetSession(descriptor.AdapterId,
                    target.ProcessId, descriptor, out OwnerDrawBridgeSession session,
                    out _));
                Assert.Null(session);
            }
        }

        [Fact]
        public void TransientPingRaceAfterStartIsRetriedWithoutRollback()
        {
            int pingCount = 0;
            OwnerDrawBridgeDescriptor descriptor = CreateDescriptor(
                delegate(int processId,
                    OwnerDrawArchitecture expectedArchitecture,
                    out string status)
                {
                    pingCount++;
                    bool ready = pingCount == 3;
                    status = ready ? "healthy" : "pipe is not published yet";
                    return ready;
                });
            var activator = new FakeActivator { StartSucceeds = true };

            using (var target = new SameProcessTarget(descriptor))
            {
                var manager = new OwnerDrawBridgeManager(target.BridgeDirectory,
                    activator, startProbeAttempts: 3, startProbeDelayMs: 0);

                OwnerDrawResult<OwnerDrawBridgeSession> result
                    = manager.EnsureReady(target.WindowHandle, descriptor);

                Assert.True(result.IsSuccess, result.Status);
                Assert.Contains("startup PING ready attempt=3", result.Status);
                Assert.Equal(3, pingCount);
                Assert.Equal(1, activator.StartCount);
                Assert.Equal(0, activator.StopCount);
            }
        }

        [Theory]
        [InlineData("PING pipe=\"PB\" returned an empty response elapsedMs=2.", true)]
        [InlineData("PING pipe is not published yet", true)]
        [InlineData("PING pipe=\"PB\" failed elapsedMs=250: TimeoutException: The operation has timed out.", true)]
        [InlineData("PING success=False abi=4 buildId=0x0 adapter=\"powerbuilder-datawindow\"", false)]
        [InlineData("PING pipe=\"PB\" rejected: Named-pipe endpoint PID mismatch.", false)]
        [InlineData("PING failed: UnauthorizedAccessException: Access denied.", false)]
        [InlineData("PING failed: TimeoutException: Bridge pipe read timed out after 3000 ms.", false)]
        [InlineData("PING failed: TimeoutException: Bridge pipe write timed out after 3000 ms.", false)]
        public void StartupPingRetryClassificationIsFailClosed(string status,
            bool expectedTransient)
        {
            Assert.Equal(expectedTransient,
                OwnerDrawBridgeManager.IsTransientStartupProbeFailure(status));
        }

        [Fact]
        public void StartupPingDeadlinePreventsRepeatingSlowTransientFailure()
        {
            int pingCount = 0;
            OwnerDrawBridgeDescriptor descriptor = CreateDescriptor(
                delegate(int processId,
                    OwnerDrawArchitecture expectedArchitecture,
                    out string status)
                {
                    pingCount++;
                    Thread.Sleep(20);
                    status = "PING pipe is not published yet";
                    return false;
                });
            var activator = new FakeActivator
            {
                StartSucceeds = true,
                StopSucceeds = true,
                StopState = OwnerDrawBridgeStopState.StoppedResident,
            };

            using (var target = new SameProcessTarget(descriptor))
            {
                var manager = new OwnerDrawBridgeManager(target.BridgeDirectory,
                    activator, startProbeAttempts: 5, startProbeDelayMs: 0,
                    startProbeDeadlineMs: 5);

                OwnerDrawResult<OwnerDrawBridgeSession> result
                    = manager.EnsureReady(target.WindowHandle, descriptor);

                Assert.False(result.IsSuccess);
                Assert.Contains("stopReason=retry-deadline", result.Status);
                Assert.Equal(1, pingCount);
                Assert.Equal(1, activator.StopCount);
            }
        }

        [Fact]
        public void HealthyCachedSessionSkipsRepeatedActivationPreflight()
        {
            int pingCount = 0;
            OwnerDrawBridgeDescriptor descriptor = CreateDescriptor(
                delegate(int processId,
                    OwnerDrawArchitecture expectedArchitecture,
                    out string status)
                {
                    pingCount++;
                    status = "healthy";
                    return true;
                });
            var activator = new FakeActivator { StartSucceeds = true };

            using (var target = new SameProcessTarget(descriptor))
            {
                var manager = new OwnerDrawBridgeManager(target.BridgeDirectory,
                    activator);

                Assert.True(manager.EnsureReady(target.WindowHandle,
                    descriptor).IsSuccess);
                Assert.True(manager.EnsureReady(target.WindowHandle,
                    descriptor).IsSuccess);

                Assert.Equal(1, activator.LegacyCheckCount);
                Assert.Equal(1, activator.StartCount);
                Assert.Equal(2, pingCount);
            }
        }

        [Fact]
        public void FailedCachedPingRerunsStrictPreflightBeforeRestart()
        {
            bool pingSucceeds = true;
            OwnerDrawBridgeDescriptor descriptor = CreateDescriptor(
                delegate(int processId,
                    OwnerDrawArchitecture expectedArchitecture,
                    out string status)
                {
                    status = pingSucceeds ? "healthy" : "pipe failed";
                    return pingSucceeds;
                });
            var activator = new FakeActivator { StartSucceeds = true };

            using (var target = new SameProcessTarget(descriptor))
            {
                var manager = new OwnerDrawBridgeManager(target.BridgeDirectory,
                    activator);
                Assert.True(manager.EnsureReady(target.WindowHandle,
                    descriptor).IsSuccess);

                pingSucceeds = false;
                activator.LegacyFound = true;
                OwnerDrawResult<OwnerDrawBridgeSession> result
                    = manager.EnsureReady(target.WindowHandle, descriptor);

                Assert.False(result.IsSuccess);
                Assert.Equal(OwnerDrawFailureCode.LegacyConflict, result.Code);
                Assert.Equal(2, activator.LegacyCheckCount);
                Assert.Equal(1, activator.StartCount);
            }
        }

        [Fact]
        public void LegacyModuleAppearingAfterStartInvalidatesCachedSession()
        {
            int pingCount = 0;
            OwnerDrawBridgeDescriptor descriptor = CreateDescriptor(
                delegate(int processId,
                    OwnerDrawArchitecture expectedArchitecture,
                    out string status)
                {
                    pingCount++;
                    status = "healthy";
                    return true;
                });
            var activator = new FakeActivator
            {
                StartSucceeds = true,
            };

            using (var target = new SameProcessTarget(descriptor))
            {
                var manager = new OwnerDrawBridgeManager(target.BridgeDirectory,
                    activator);
                Assert.True(manager.EnsureReady(target.WindowHandle,
                    descriptor).IsSuccess);
                Assert.Equal(1, pingCount);

                activator.LegacyFound = true;
                Assert.False(manager.TryGetSession(descriptor.AdapterId,
                    target.ProcessId, descriptor,
                    out OwnerDrawBridgeSession session, out string sessionStatus));
                Assert.Null(session);
                Assert.Contains("LegacyConflict", sessionStatus);
                Assert.Equal(1, pingCount);

                OwnerDrawBridgeRuntimeStatus runtime
                    = OwnerDrawBridgeControl.GetStatusForDescriptor(
                        descriptor.AdapterId, descriptor, target.ProcessId,
                        "test host", manager);
                Assert.Equal(OwnerDrawBridgeRuntimeState.LegacyConflict,
                    runtime.State);
                Assert.Equal(1, pingCount);
            }
        }

        private static OwnerDrawBridgeDescriptor CreateDescriptor(
            OwnerDrawBridgeHealthProbe healthProbe)
        {
            return new OwnerDrawBridgeDescriptor(
                "legacy-conflict-test",
                "TestBridge.x86.dll",
                "TestBridge.x64.dll",
                "TestBridge.dll",
                1,
                healthProbe);
        }

        private sealed class FakeActivator : IOwnerDrawBridgeActivator
        {
            internal bool LegacyFound { get; set; }

            internal bool CheckSucceeds { get; set; } = true;

            internal bool StartSucceeds { get; set; } = true;

            internal bool StopSucceeds { get; set; } = true;

            internal OwnerDrawBridgeStopState StopState { get; set; }
                = OwnerDrawBridgeStopState.StoppedResident;

            internal int StartCount { get; private set; }

            internal int LegacyCheckCount { get; private set; }

            internal int StopCount { get; private set; }

            internal string LastStartLegacyFileName { get; private set; }

            internal int LastControllerProcessId { get; private set; }

            internal string LastStopLegacyFileName { get; private set; }

            public bool TryCheckLegacyConflict(int processId,
                string legacyFileName, out bool found, out string status)
            {
                LegacyCheckCount++;
                found = LegacyFound;
                status = found
                    ? "LegacyConflict: \"" + legacyFileName
                        + "\" is resident; restart the target application."
                    : "No legacy Bridge is resident.";
                return CheckSucceeds;
            }

            public bool TryStart(int processId, string bridgePath,
                string legacyFileName, int requiredAbi, int controllerProcessId,
                out string status)
            {
                StartCount++;
                LastStartLegacyFileName = legacyFileName;
                LastControllerProcessId = controllerProcessId;
                status = StartSucceeds ? "started" : "start failed";
                return StartSucceeds;
            }

            public bool TryStopResident(int processId, string bridgePath,
                string legacyFileName, int requiredAbi,
                out OwnerDrawBridgeStopState stopState, out string status)
            {
                StopCount++;
                LastStopLegacyFileName = legacyFileName;
                stopState = StopState;
                status = StopSucceeds ? "stopped" : "stop failed";
                return StopSucceeds;
            }
        }

        private sealed class SameProcessTarget : IDisposable
        {
            internal SameProcessTarget(OwnerDrawBridgeDescriptor descriptor)
            {
                ProcessId = Process.GetCurrentProcess().Id;
                BridgeDirectory = Path.Combine(Path.GetTempPath(),
                    "ii-rpa-owner-draw-tests-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(BridgeDirectory);
                string bridgeFile = descriptor.GetFileName(
                    Environment.Is64BitProcess
                        ? OwnerDrawArchitecture.X64 : OwnerDrawArchitecture.X86);
                File.WriteAllBytes(Path.Combine(BridgeDirectory, bridgeFile),
                    Array.Empty<byte>());
                WindowHandle = CreateWindowExW(0, "STATIC", string.Empty, 0,
                    0, 0, 1, 1, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
                    IntPtr.Zero);
                if (WindowHandle == IntPtr.Zero)
                {
                    throw new InvalidOperationException(
                        "Could not create the same-process test window; win32="
                        + Marshal.GetLastWin32Error() + ".");
                }
            }

            internal int ProcessId { get; private set; }

            internal string BridgeDirectory { get; private set; }

            internal IntPtr WindowHandle { get; private set; }

            public void Dispose()
            {
                if (WindowHandle != IntPtr.Zero)
                {
                    DestroyWindow(WindowHandle);
                    WindowHandle = IntPtr.Zero;
                }
                if (Directory.Exists(BridgeDirectory))
                    Directory.Delete(BridgeDirectory, true);
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode,
            SetLastError = true)]
        private static extern IntPtr CreateWindowExW(uint extendedStyle,
            string className, string windowName, uint style, int x, int y,
            int width, int height, IntPtr parent, IntPtr menu,
            IntPtr instance, IntPtr parameter);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyWindow(IntPtr window);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();
    }
}
