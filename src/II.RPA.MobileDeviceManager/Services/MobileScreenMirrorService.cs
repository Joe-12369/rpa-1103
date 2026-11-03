using II.RPA.MobileDeviceManager.Common;
using II.RPA.MobileDeviceManager.Log;
using II.RPA.MobileDeviceManager.Win32;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;

namespace II.RPA.MobileDeviceManager.Services
{
    internal class MobileScreenMirrorService : IDisposable
    {
        private const int StartRetryCount = 1000;
        private const int StartRetryDelayMilliseconds = 10;
        private const int WindowReadyStableMilliseconds = 120;
        private const int EmbedRetryCount = 8;
        private const int EmbedRetryDelayMilliseconds = 120;
        private const int SettleAfterParentMilliseconds = 30;
        private const int StartupWindowPosition = -32000;

        private readonly ILogger _logger;
        private Process _process;
        private IntPtr _mobileHandle = IntPtr.Zero;
        private string _deviceId = string.Empty;
        private string _windowTitle = string.Empty;

        public Process Process => _process;
        public IntPtr MobileHandle => _mobileHandle;

        public MobileScreenMirrorService(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<IntPtr> StartAsync(string deviceId, Action ensureCurrentConnection = null)
        {
            StopTrackedProcess();
            _deviceId = deviceId ?? string.Empty;
            _windowTitle = $"{_deviceId}-{Guid.NewGuid():N}";

            string rootDir = AppDomain.CurrentDomain.BaseDirectory;
            string fileName = Path.Combine(rootDir, "MobileScreen/II.RPA.MobileScreen.exe");
            string workingDirectory = Path.GetDirectoryName(fileName) ?? rootDir;

            _logger?.LogInfo($"Start mobile screen mirror process, deviceId[{_deviceId}], exe[{fileName}], workingDirectory[{workingDirectory}]");

            _process = new Process();
            _process.StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                // 保持SDL正常完成首次显示和尺寸初始化，但在嵌入前不暴露桌面独立窗口。
                Arguments = $"-m  1024  -s {deviceId} --window-title {_windowTitle} --no-audio --window-x={StartupWindowPosition} --window-y={StartupWindowPosition}",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = workingDirectory
            };
            // 保存本轮设备标识，避免旧进程晚到的日志被记到新设备上。
            string processDeviceId = _deviceId;
            _process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                    _logger?.LogDebug($"投屏输出，设备[{processDeviceId}]：{e.Data}");
            };
            _process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                    _logger?.LogDebug($"投屏诊断输出，设备[{processDeviceId}]：{e.Data}");
            };
            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            int retryGetMobileHandle = 0;
            var readinessTimer = Stopwatch.StartNew();
            IntPtr candidateWindow = IntPtr.Zero;
            int candidateWidth = 0;
            int candidateHeight = 0;
            long stableSince = 0;
            do
            {
                ensureCurrentConnection?.Invoke();
                if (_process.HasExited)
                {
                    _logger?.LogWarning($"投屏进程在创建窗口前退出，设备[{_deviceId}]，PID[{_process.Id}]，退出码[{_process.ExitCode}]");
                    break;
                }

                IntPtr window = Win32Helper.FindWindow("SDL_app", _windowTitle);
                if (window != IntPtr.Zero)
                {
                    Win32Helper.GetWindowThreadProcessId((HWND)window, out uint ownerId);
                    // 句柄存在不代表投屏已就绪；仅在可见且客户区尺寸稳定后嵌入，
                    // 避免与投屏程序自身的首次显示、尺寸初始化交错。
                    if (ownerId == (uint)_process.Id
                        && Win32Helper.IsWindowVisible((HWND)window)
                        && Win32Helper.GetClientRect((HWND)window, out RECT clientRect)
                        && clientRect.right > clientRect.left && clientRect.bottom > clientRect.top)
                    {
                        int width = clientRect.right - clientRect.left;
                        int height = clientRect.bottom - clientRect.top;
                        if (candidateWindow != window || candidateWidth != width || candidateHeight != height)
                        {
                            candidateWindow = window;
                            candidateWidth = width;
                            candidateHeight = height;
                            stableSince = readinessTimer.ElapsedMilliseconds;
                        }
                        else if (readinessTimer.ElapsedMilliseconds - stableSince >= WindowReadyStableMilliseconds)
                        {
                            _mobileHandle = window;
                            break;
                        }
                    }
                    else
                    {
                        candidateWindow = IntPtr.Zero;
                    }
                }
                else
                {
                    candidateWindow = IntPtr.Zero;
                }

                await Task.Delay(StartRetryDelayMilliseconds);
            }
            while (++retryGetMobileHandle < StartRetryCount);

            if (_mobileHandle == IntPtr.Zero)
            {
                int processId = SafeGetProcessId();
                StopTrackedProcess();
                _logger?.LogError($"Get mobile screen window failed, deviceId[{deviceId}], processId[{processId}]");
                throw new Exception("Get mobile screen window failed".I18n("controlType_DeviceScreenView.xaml_431EAA5B"));
            }

            _logger?.LogInfo($"Get mobile screen window success, deviceId[{_deviceId}], handle[{_mobileHandle}], visibleClientSize[{candidateWidth}x{candidateHeight}], readyElapsed[{readinessTimer.ElapsedMilliseconds}ms]");
            return _mobileHandle;
        }

        public void Embed(IntPtr parentHandle, int width, int height)
        {
            if (_mobileHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException("Mobile screen handle is empty, cannot embed");
            }

            if (parentHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException("Mobile screen parent handle is empty, cannot embed");
            }

            string lastFailure = null;
            for (int attempt = 1; attempt <= EmbedRetryCount; attempt++)
            {
                if (_process == null || _process.HasExited || !Win32Helper.IsWindow((HWND)_mobileHandle))
                {
                    throw new InvalidOperationException($"Mobile screen process or window exited before embed, deviceId[{_deviceId}], processId[{SafeGetProcessId()}]");
                }
                if (TryEmbedOnce(parentHandle, width, height, attempt, out lastFailure))
                {
                    return;
                }

                if (attempt < EmbedRetryCount)
                {
                    Thread.Sleep(EmbedRetryDelayMilliseconds);
                }
            }

            throw new InvalidOperationException(lastFailure ?? "Mobile screen embed failed");
        }

        public bool Resize(int width, int height)
        {
            if (_mobileHandle == IntPtr.Zero)
            {
                return false;
            }

            HWND handle = (HWND)_mobileHandle;
            uint flags = (uint)(Win32Helper.SetWindowPosFlags.SWP_NOZORDER
                | Win32Helper.SetWindowPosFlags.SWP_NOACTIVATE
                | Win32Helper.SetWindowPosFlags.SWP_NOOWNERZORDER);

            if (Win32Helper.SetWindowPos(handle, new HWND(IntPtr.Zero), 0, 0, width, height, flags))
            {
                return true;
            }

            int setWindowPosError = Marshal.GetLastWin32Error();
            _logger?.LogWarning($"SetWindowPos resize failed, deviceId[{_deviceId}], handle[{_mobileHandle}], width[{width}], height[{height}], error[{setWindowPosError}] {DescribeLastError(setWindowPosError)}, fallback to MoveWindow");

            if (Win32Helper.MoveWindow(handle, 0, 0, width, height, true))
            {
                return true;
            }

            int moveWindowError = Marshal.GetLastWin32Error();
            _logger?.LogWarning($"MoveWindow resize failed, deviceId[{_deviceId}], handle[{_mobileHandle}], width[{width}], height[{height}], error[{moveWindowError}] {DescribeLastError(moveWindowError)}");
            return false;
        }

        public bool IsAlive()
        {
            return _process != null && !_process.HasExited && _mobileHandle != IntPtr.Zero
                && Win32Helper.IsWindow((HWND)_mobileHandle);
        }

        public void Stop()
        {
            StopTrackedProcess();
        }

        public void KillAllScreenProcesses()
        {
            Process[] mobileScreenProcesses = Process.GetProcessesByName("II.RPA.MobileScreen");
            if (mobileScreenProcesses == null)
            {
                return;
            }

            foreach (Process item in mobileScreenProcesses.Where(p => p != null))
            {
                try
                {
                    item.Kill();
                }
                catch
                {
                }
            }
        }

        private bool TryEmbedOnce(IntPtr parentHandle, int width, int height, int attempt, out string failureReason)
        {
            failureReason = null;

            HWND childHandle = (HWND)_mobileHandle;
            HWND parent = (HWND)parentHandle;

            if (!Win32Helper.IsWindow(childHandle))
            {
                failureReason = BuildFailureReason("window handle invalid before embed", attempt, childHandle, parentHandle);
                _logger?.LogWarning(failureReason);
                return false;
            }

            if (!Win32Helper.IsWindow(parent))
            {
                failureReason = BuildFailureReason("parent handle invalid", attempt, childHandle, parentHandle);
                _logger?.LogWarning(failureReason);
                return false;
            }

            int currentStyle = Win32Helper.GetWindowLong(childHandle, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
            int styleError = Marshal.GetLastWin32Error();
            if (currentStyle == 0 && styleError != 0)
            {
                failureReason = BuildFailureReason("GetWindowLong failed", attempt, childHandle, parentHandle, styleError);
                _logger?.LogWarning(failureReason);
                return false;
            }

            int desiredStyle = (currentStyle
                | Win32Helper.WS_CHILD
                | Win32Helper.WS_VISIBLE
                | Win32Helper.WS_CLIPCHILDREN
                | Win32Helper.WS_CLIPSIBLINGS)
                & ~Win32Helper.WS_POPUP
                & ~Win32Helper.WS_WINDOW_DECORATION;

            HWND previousParent = Win32Helper.SetParent(childHandle, parent);
            int setParentError = Marshal.GetLastWin32Error();
            if (previousParent.Value == IntPtr.Zero && setParentError != 0)
            {
                failureReason = BuildFailureReason("SetParent failed", attempt, childHandle, parentHandle, setParentError, currentStyle, desiredStyle);
                _logger?.LogWarning(failureReason);
                return false;
            }

            Marshal.SetLastPInvokeError(0);
            int setStyleResult = Win32Helper.SetWindowLong(childHandle, WINDOW_LONG_PTR_INDEX.GWL_STYLE, desiredStyle);
            int setStyleError = Marshal.GetLastWin32Error();
            if (setStyleResult == 0 && setStyleError != 0)
            {
                failureReason = BuildFailureReason("SetWindowLong failed", attempt, childHandle, parentHandle, setStyleError, currentStyle, desiredStyle);
                _logger?.LogWarning(failureReason);
                return false;
            }

            Marshal.SetLastPInvokeError(0);
            int appliedStyle = Win32Helper.GetWindowLong(childHandle, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
            int verifyStyleError = Marshal.GetLastWin32Error();
            if (appliedStyle == 0 && verifyStyleError != 0)
            {
                failureReason = BuildFailureReason("GetWindowLong after style update failed", attempt, childHandle, parentHandle, verifyStyleError, currentStyle, desiredStyle);
                _logger?.LogWarning(failureReason);
                return false;
            }
            if ((appliedStyle & Win32Helper.WS_WINDOW_DECORATION) != 0
                || (appliedStyle & Win32Helper.WS_CHILD) == 0)
            {
                failureReason = BuildFailureReason("window style verification failed", attempt, childHandle, parentHandle, 0, appliedStyle, desiredStyle);
                _logger?.LogWarning(failureReason);
                return false;
            }

            uint flags = (uint)(Win32Helper.SetWindowPosFlags.SWP_NOZORDER
                | Win32Helper.SetWindowPosFlags.SWP_NOACTIVATE
                | Win32Helper.SetWindowPosFlags.SWP_NOOWNERZORDER
                | Win32Helper.SetWindowPosFlags.SWP_FRAMECHANGED
                | Win32Helper.SetWindowPosFlags.SWP_SHOWWINDOW);

            if (!Win32Helper.SetWindowPos(childHandle, new HWND(IntPtr.Zero), 0, 0, width, height, flags))
            {
                int setWindowPosError = Marshal.GetLastWin32Error();
                failureReason = BuildFailureReason("SetWindowPos failed", attempt, childHandle, parentHandle, setWindowPosError, currentStyle, desiredStyle);
                _logger?.LogWarning(failureReason);
                return false;
            }

            Thread.Sleep(SettleAfterParentMilliseconds);

            string actualClientSize = Win32Helper.GetClientRect(childHandle, out RECT embeddedClientRect)
                ? $"{embeddedClientRect.right - embeddedClientRect.left}x{embeddedClientRect.bottom - embeddedClientRect.top}"
                : "unavailable";
            _logger?.LogInfo($"Mobile screen embed success, deviceId[{_deviceId}], attempt[{attempt}], handle[{_mobileHandle}], parent[{parentHandle}], style[0x{currentStyle:X8} -> 0x{appliedStyle:X8}], size[{width}x{height}], actualClientSize[{actualClientSize}]");
            return true;
        }

        private string BuildFailureReason(string step, int attempt, HWND childHandle, IntPtr parentHandle, int errorCode = 0, int currentStyle = 0, int desiredStyle = 0)
        {
            string stylePart = string.Empty;
            if (currentStyle != 0 || desiredStyle != 0)
            {
                stylePart = $", style[0x{currentStyle:X8} -> 0x{desiredStyle:X8}]";
            }

            string errorPart = errorCode != 0
                ? $", error[{errorCode}] {DescribeLastError(errorCode)}"
                : string.Empty;

            return $"Mobile screen embed {step}, deviceId[{_deviceId}], attempt[{attempt}], handle[{childHandle.Value}], parent[{parentHandle}]{stylePart}{errorPart}";
        }

        private string DescribeLastError(int errorCode)
        {
            if (errorCode == 0)
            {
                return string.Empty;
            }

            try
            {
                return new Win32Exception(errorCode).Message;
            }
            catch
            {
                return string.Empty;
            }
        }

        private int SafeGetProcessId()
        {
            try
            {
                return _process == null ? 0 : _process.Id;
            }
            catch
            {
                return 0;
            }
        }

        private void StopTrackedProcess()
        {
            Process process = _process;
            _process = null;
            _mobileHandle = IntPtr.Zero;
            _deviceId = string.Empty;
            _windowTitle = string.Empty;
            if (process == null)
            {
                return;
            }
            try
            {
                if (process.HasExited)
                {
                    _logger?.LogDebug($"清理已退出投屏进程，PID[{process.Id}]，退出码[{process.ExitCode}]");
                }
                else
                {
                    process.Kill();
                    if (!process.WaitForExit(500))
                    {
                        _logger?.LogWarning($"Mobile screen process did not exit after kill, processId[{process.Id}]");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"Stop mobile screen process failed: {ex}");
            }
            finally
            {
                process.Dispose();
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
