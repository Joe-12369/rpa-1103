using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.WebBrowser.Common.Utils;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace II.RPA.WebBrowser.Common.Basic.BrowserManagers
{
    /// <summary>
    /// 通用BrowserShutdownManager，如需单独实现BrowserShutdownManager  可在具体模块中继承BrowserShutdownManagerBase
    /// </summary>
    internal class BrowserShutdownManager : BrowserShutdownManagerBase
    {
        public BrowserShutdownManager(Browser browser) : base(browser)
        {
        }
    }

    /// <summary>
    /// 浏览器关闭管理器基类
    /// 定义了浏览器关闭的通用流程，子类可以重写特定步骤以实现自定义行为
    /// </summary>
    public abstract class BrowserShutdownManagerBase
    {
        private readonly Browser _browser;
        protected ILogger _logger = Logger.OfContext();

        // Windows API 声明
        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private const uint WM_CLOSE = 0x0010;

        // 配置参数 - 使用 protected virtual 属性，允许子类重写
        protected virtual int WaitSaveConfigDelay => 5000; // 等待浏览器保存配置的时间
        protected virtual int GracefulCloseTimeout => 5000; // 优雅关闭的超时时间
        protected virtual int ProcessCloseTimeout => 5000; // 进程关闭的超时时间（从3秒增加到5秒，给后台进程更多保存配置的时间）
        /// <summary>无窗口主进程等在 <see cref="CloseProcesses"/> 中：短等后 <see cref="KillProcessAndWait"/>。</summary>
        protected virtual int ProcessCloseNoWindowTimeout => 1000;
        /// <summary>子进程第一阶段（CloseMainWindow）结束后，重新枚举进程并进入第二阶段强杀前的等待（毫秒）。</summary>
        protected virtual int ChildProcessesRecheckDelayMs => 2000;
        protected virtual int WaitWindowCloseDelay => 2000; // 等待窗口关闭的时间（毫秒）
        protected virtual int WaitChildProcessExitDelay => 2000; // 等待子进程退出的时间（毫秒）
        protected virtual int WaitMainProcessSaveDelay => 5000; // 等待主进程保存配置的时间（毫秒）

        public BrowserShutdownManagerBase(Browser browser)
        {
            _browser = browser ?? throw new ArgumentNullException(nameof(browser));
        }

        /// <summary>
        /// 消耗已用时间，重新计算剩余超时时间并重置计时器
        /// </summary>
        /// <param name="timeout">当前剩余超时时间（毫秒）</param>
        /// <param name="stopwatch">计时器</param>
        /// <returns>更新后的剩余超时时间</returns>
        private int ConsumeTime(ref int timeout, Stopwatch stopwatch)
        {
            timeout = Math.Max(0, timeout - (int)stopwatch.ElapsedMilliseconds);
            stopwatch.Restart();
            return timeout;
        }

        /// <summary>
        /// 获取剩余超时时间（不重置计时器）
        /// </summary>
        /// <param name="timeout">当前剩余超时时间（毫秒）</param>
        /// <param name="stopwatch">计时器</param>
        /// <returns>剩余超时时间</returns>
        private int GetRemainingTimeout(int timeout, Stopwatch stopwatch)
        {
            return Math.Max(0, timeout - (int)stopwatch.ElapsedMilliseconds);
        }

        /// <summary>
        /// 关闭所有浏览器窗口和进程
        /// </summary>
        /// <param name="killProcess">
        /// 是否在关闭浏览器之后，强制清理所有残留的浏览器进程。
        /// true 表示允许强杀后台残留进程；false 表示只通过优雅方式关闭，不做强杀兜底。
        /// </param>
        /// <param name="timeout">本次关闭操作的总超时时间（毫秒）</param>
        public virtual void CloseAll(bool killProcess, int timeout = 10000, bool isCloseAlert = false)
        {
            _logger.LogDebug($"准备关闭 {_browser.BrowserProcessName} 浏览器");
            var stopWatch = System.Diagnostics.Stopwatch.StartNew();

            if (!HasBrowserProcess())
            {
                _logger.LogInformation($"未检测到浏览器进程，跳过关闭流程");
                return;
            }
            try
            {
                PingCrx();
                _browser.WebDataProvider.CloseAll(5000, killProcess, isCloseAlert);//调用接口关闭浏览器，给浏览器5秒钟的时间来响应和关闭窗口
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(_logger, ex, $"调用浏览器CloseAll发生异常，仍将尝试通过结束浏览器进程的方式关闭进程");
            }

            _logger.LogDebug($"等待 {WaitWindowCloseDelay}ms 让窗口关闭...");
            Task.Delay(WaitWindowCloseDelay).Wait();// 再额外等一会，确保窗口资源完全释放，与内容写入完成

            if (!HasBrowserProcess())
            {
                _logger.LogDebug("浏览器窗口已关闭，未检测到浏览器进程，关闭流程结束，本次关闭无任性任何强制性操作");
                return;
            }
            _logger.LogDebug("检测到浏览器进程仍在运行，继续执行后续关闭流程...");
            // 步骤2: 等待可视标签页/窗口关闭
            ConsumeTime(ref timeout, stopWatch);
            _logger.LogDebug($"等待 最多 5000ms 让窗口完全关闭...");
            WaitBrowserTabsClosed(timeout + 1000);


            if (killProcess)
            {
                // 步骤3: 允许强杀，都先尝试优雅关闭所有浏览器窗口
                // KillMainWindowGracefully的作用：找到所有浏览器窗口，直接向窗口句柄发送 WM_CLOSE 消息
                ConsumeTime(ref timeout, stopWatch);
                if (KillMainWindowGracefully())
                {
                    _logger.LogDebug("所有浏览器窗口已优雅关闭");
                }
                else
                {
                    _logger.LogDebug("部分浏览器窗口未能完全关闭，后续根据 killProcess 决定是否强杀残留进程");
                }

                // 步骤5: 清理残留进程（按顺序：先子进程，后主进程）
                ConsumeTime(ref timeout, stopWatch);
                KillRemainingProcessesInOrder(timeout);
                Task.Delay(WaitWindowCloseDelay).Wait();
            }
            stopWatch.Stop();
            if (stopWatch.ElapsedMilliseconds > timeout)
            {
                _logger.LogDebug("关闭浏览器超时或异常");
            }
            else if (killProcess)
            {
                // 步骤6: 检查 NativeMessagingHost 是否退出
                int remainingTimeout = GetRemainingTimeout(timeout, stopWatch);
                WaitNativeMessagingHostExited(remainingTimeout);
            }
        }

        private void PingCrx()
        {
            RPAUtils.Retry(() =>
            {
                try
                {
                    return _browser.NativeMessageHostProcessID <= 0 || _browser.WebDataProvider.Ping();
                }
                catch (Exception)
                {
                    return false;
                }
            }, 10000, 200); // 先Ping一下，确保WebDataProvider可用，避免后续调用CloseAll时因WebDataProvider不可用而跳过优雅关闭步骤
        }

        /// <summary>
        /// 等待 NativeMessagingHost 进程退出
        /// </summary>
        /// <param name="timeout">超时时间（毫秒）</param>
        protected virtual void WaitNativeMessagingHostExited(int timeout = 10000)
        {
            if (_browser.NativeMessageHostProcessID <= 0)
                return;
            _logger.LogDebug($"等待 NativeMessagingHost 进程退出（进程ID: {_browser.NativeMessageHostProcessID}）");
            RPAUtils.Retry(() =>
            {
                try
                {

                    using (Process.GetProcessById(_browser.NativeMessageHostProcessID))
                        return false;
                }
                catch (ArgumentException)
                {
                    _logger.LogDebug($"NativeMessagingHost 进程 {_browser.NativeMessageHostProcessID} 已退出");
                    return true;
                }
                catch
                {
                    return false;
                }
            }, timeout);
        }
        /// <summary>
        /// 等待浏览器标签页（可视窗口）关闭
        /// </summary>
        protected virtual void WaitBrowserTabsClosed(int timeout = 5000)
        {
            _logger.LogDebug($"等待浏览器tab页（可视窗口）消失");
            RPAUtils.Retry(() =>
            {
                if (BrowserWindowProvider.GetFirstBrowserByZOrder(null, _browser.GuiType) == null)
                    return true;

                var activeTabIds = _browser.TryGetActiveTabIds();
                if (activeTabIds.Count() == 0)
                    return true;

                if (_browser.IgnoreUrl?.Count > 0)
                {
                    PredicateFilter<BrowserTabPage> tabFilter = new PredicateFilter<BrowserTabPage>();
                    tabFilter.AddLast((tabPage) => !_browser.IgnoreUrl.Any(e => tabPage.Url.Contains(e)));
                    return _browser.MatchTabPageByPriority(activeTabIds, tabFilter) == null;
                }

                return false;
            }, Math.Min(timeout, 5000), 500, 500);
        }
        protected virtual bool HasBrowserProcess()
        {
            var processes = GetBrowserProcesses();
            return processes.Length > 0;
        }
        /// </summary>
        /// <returns>匹配的浏览器进程数组，如果没有找到则返回空数组</returns>
        protected virtual Process[] GetBrowserProcesses()
        {
            var processAll = UiCommon.GetProcessesBySessionId(_browser.BrowserProcessName);
            if (processAll.Length == 0)
            {
                return Array.Empty<Process>();
            }

            string exePath = _browser.GetBrowserAppPath();
            var processes = processAll
                .Where(p => string.IsNullOrEmpty(exePath) || exePath.EqualIgnoreCase(p.GetExeFilePath()))
                .ToArray();

            return processes;
        }

        /// <summary>
        /// 按顺序清理残留进程（先子进程，后主进程）
        /// 注意：此时所有浏览器窗口已通过 KillMainWindowGracefully() 关闭
        /// 但Chrome可能保留一些残留进程（性能优化机制），包括主进程和其他后台进程
        /// </summary>
        protected virtual void KillRemainingProcessesInOrder(int timeout = 10000)
        {
            _logger.LogDebug($"开始按顺序清理浏览器残留进程（窗口已关闭，先子进程后主进程）");

            var stopWatch = System.Diagnostics.Stopwatch.StartNew();

            while (stopWatch.ElapsedMilliseconds < timeout)
            {
                var processes = GetBrowserProcesses();

                try
                {
                    if (processes.Length == 0)
                    {
                        _logger.LogDebug("所有进程已退出");
                        return;
                    }

                    // 检查进程是否还在运行
                    var stillRunning = GetRemainingProcesses(processes).ToArray();

                    if (stillRunning.Length == 0)
                    {
                        _logger.LogDebug("所有进程已退出");
                        return;
                    }

                    // 识别主进程和子进程（通过父进程）
                    var (mainProcesses, childProcesses) = IdentifyMainAndChildProcesses(stillRunning);

                    _logger.LogDebug($"检测到 {mainProcesses.Count} 个主进程，{childProcesses.Count} 个子进程");

                    //if (childProcesses.Count > 0)
                    //{
                    //    _logger.LogDebug($"开始两阶段关闭 {childProcesses.Count} 个子进程...");
                    //    CloseChildProcessesTwoPhase(childProcesses);
                    //}

                    if (mainProcesses.Count > 0)
                    {
                        _logger.LogDebug($"等待 {WaitMainProcessSaveDelay}ms 让主进程保存配置...");
                        Task.Delay(WaitMainProcessSaveDelay).Wait();

                        var remainingMainProcesses = GetRemainingProcesses(mainProcesses);
                        if (remainingMainProcesses.Count > 0)
                        {
                            _logger.LogDebug($"开始关闭 {remainingMainProcesses.Count} 个主进程...");
                            CloseProcesses(remainingMainProcesses, "主进程");
                            Task.Delay(500).Wait();
                        }
                    }
                    var finalProcesses = GetRemainingProcesses(GetBrowserProcesses()).ToArray();
                    if (finalProcesses.Length == 0)
                    {
                        _logger.LogDebug("所有进程已退出");
                        return;
                    }

                    Task.Delay(WaitMainProcessSaveDelay).Wait();
                    var finalIds = finalProcesses.Select(p => p.Id).ToArray();
                    _logger.LogWarning($"强制终止残留进程: {string.Join(", ", finalIds)}");
                    Utils.ProcessKiller.KillProcessByIds(finalIds);
                    Task.Delay(1000).Wait();
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, "清理残留进程时出错");
                    break;
                }
            }

            var timeoutRemainingProcesses = GetRemainingProcesses(GetBrowserProcesses()).ToArray();
            if (timeoutRemainingProcesses.Length > 0)
            {
                var timeoutIds = timeoutRemainingProcesses.Select(p => p.Id).ToArray();
                _logger.LogWarning($"清理残留进程超时（{timeout}ms），强制终止所有剩余进程: {string.Join(", ", timeoutIds)}");
                Utils.ProcessKiller.KillProcessByIds(timeoutIds);
            }
            else
            {
                _logger.LogWarning($"清理残留进程超时（{timeout}ms）");
            }
        }

        /// <summary>
        /// 子进程：1）对具备主窗口的先 CloseMainWindow + WaitForExit；2）等待 <see cref="ChildProcessesRecheckDelayMs"/> 后重新获取进程；
        /// 3）对仍识别为子进程且存活的逐个 Kill + WaitForExit。
        /// </summary>
        private void CloseChildProcessesTwoPhase(List<Process> childProcesses)
        {
            if (childProcesses.Count == 0)
                return;

            var withMainWindow = new List<Process>();
            foreach (var process in childProcesses)
            {
                try
                {
                    if (process.MainWindowHandle != IntPtr.Zero)
                        withMainWindow.Add(process);
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(_logger, ex, $"检查子进程 {process.Id} 主窗口失败，将留待第二阶段强杀");
                }
            }

            if (withMainWindow.Count > 0)
            {
                _logger.LogDebug($"子进程中带主窗口的共 {withMainWindow.Count} 个：CloseMainWindow 并等待退出");
                foreach (var process in withMainWindow)
                {
                    try
                    {
                        if (process.CloseMainWindow())
                        {
                            if (process.WaitForExit(ProcessCloseTimeout))
                                _logger.LogDebug($"子进程 {process.Id} 已优雅关闭");
                            else
                                _logger.LogWarning($"子进程 {process.Id} 未在 {ProcessCloseTimeout}ms 内退出");
                        }
                        else
                            _logger.LogWarning($"子进程 {process.Id} CloseMainWindow 失败");
                    }
                    catch (Exception ex)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(_logger, ex, $"子进程 {process.Id} CloseMainWindow 流程异常");
                    }
                }
            }

            _logger.LogDebug($"子进程第一阶段结束，等待 {ChildProcessesRecheckDelayMs}ms 后重新获取进程并强杀仍存活的子进程");
            if (ChildProcessesRecheckDelayMs > 0)
                Task.Delay(ChildProcessesRecheckDelayMs).Wait();

            var refreshed = GetBrowserProcesses();
            var stillRunning = GetRemainingProcesses(refreshed).ToArray();
            if (stillRunning.Length == 0)
            {
                _logger.LogDebug("刷新后无残留浏览器进程");
                return;
            }

            var (_, remainingChildren) = IdentifyMainAndChildProcesses(stillRunning);
            if (remainingChildren.Count == 0)
            {
                _logger.LogDebug("刷新后无仍存活的子进程");
                return;
            }

            _logger.LogDebug($"子进程第二阶段：对 {remainingChildren.Count} 个仍存活的子进程 Kill + WaitForExit");
            foreach (var process in remainingChildren)
            {
                try
                {
                    KillProcessAndWait(process, "子进程");
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(_logger, ex, $"第二阶段子进程 {process.Id} 强杀异常");
                }
            }
        }

        private void CloseProcesses(List<Process> processes, string processType = "进程")
        {
            if (processes.Count == 0)
                return;

            foreach (var process in processes)
            {
                try
                {
                    bool hasWindow = process.MainWindowHandle != IntPtr.Zero;
                    if (hasWindow)
                    {
                        if (process.CloseMainWindow())
                        {
                            if (process.WaitForExit(ProcessCloseTimeout))
                            {
                                _logger.LogDebug($"{processType} {process.Id} 已优雅关闭");
                            }
                            else
                            {
                                _logger.LogWarning($"{processType} {process.Id} 未在 {ProcessCloseTimeout}ms 内关闭，将由本轮末尾统一强杀");
                            }
                        }
                        else
                        {
                            _logger.LogWarning($"{processType} {process.Id} CloseMainWindow 失败，将由本轮末尾统一强杀");
                        }
                    }
                    else
                    {
                        // 先短等：无窗子进程有时随主进程收尾自行退出。WaitForExit 不结束进程；若仍存活则 Kill + WaitForExit 收束句柄（API 强杀，与末尾 taskkill 批量互补）。
                        _logger.LogDebug($"无窗口{processType} {process.Id} 等待自然退出（最多 {ProcessCloseNoWindowTimeout}ms）...");
                        if (!process.WaitForExit(ProcessCloseNoWindowTimeout))
                        {
                            _logger.LogWarning($"无窗口{processType} {process.Id} 未自然退出，执行 Process.Kill 并等待进程句柄释放");
                            KillProcessAndWait(process, processType);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(_logger, ex, $"关闭{processType} {process.Id} 失败");
                }
            }
        }

        /// <summary>强杀后 WaitForExit，避免仅 Kill 不等待导致句柄与资源回收滞后；Kill 失败时回退到 ProcessKiller。</summary>
        protected virtual int KillWaitForExitTimeout => 5000;

        private void KillProcessAndWait(Process process, string processType)
        {
            try
            {
                if (process.HasExited)
                    return;
                try
                {
                    process.Kill();
                }
                catch (Exception kex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(_logger, kex, $"Process.Kill 失败（如权限），改用 ProcessKiller: {processType} {process.Id}");
                    Utils.ProcessKiller.KillProcessByIds(new[] { process.Id });
                }

                if (!process.WaitForExit(KillWaitForExitTimeout))
                    _logger.LogWarning($"{processType} {process.Id} 强杀后未在 {KillWaitForExitTimeout}ms 内确认退出，仍由本轮末尾统一强杀兜底");
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(_logger, ex, $"强杀或等待退出失败: {processType} {process.Id}");
            }
        }

        /// <summary>
        /// 获取仍在运行的进程
        /// </summary>
        private List<Process> GetRemainingProcesses(IEnumerable<Process> processes)
        {
            var remaining = new List<Process>();
            foreach (var process in processes)
            {
                try
                {
                    process.Refresh(); // 强迫获取最新状态
                    if (!process.HasExited)
                    {
                        remaining.Add(process);
                    }
                }
                catch (InvalidOperationException ex)
                {
                    // 进程尚未启动或已退出
                    Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(_logger, ex, "检查进程 {ProcessId} 状态时发现进程已失效，视为已安全退出", process.Id);
                }
                catch (Exception ex)
                {
                    // 严禁吞没不可预知的异常
                    Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(_logger, ex, "获取进程 {ProcessId} 状态时发生未知异常", process.Id);
                }
            }
            return remaining;
        }
        protected virtual bool KillMainWindowGracefully()
        {
            var browserWnds = _browser.GetAllBrowserByZOrder();
            if (browserWnds.Count == 0)
                return true;

            string targetExePath = _browser.GetBrowserAppPath();
            var targetWindows = browserWnds.Where(wndObj => wndObj.AppExePath.EqualIgnoreCase(targetExePath)).ToList();
            if (targetWindows.Count == 0)
            {
                _logger.LogDebug($"没有找到匹配的浏览器窗口（共 {browserWnds.Count} 个窗口）");
                return true;
            }

            _logger.LogDebug($"找到 {targetWindows.Count} 个匹配的浏览器窗口（共 {browserWnds.Count} 个窗口）");

            var processIds = targetWindows.Select(w => w.ProcessId).Distinct().ToList();
            if (processIds.Count > 1)
            {
                _logger.LogWarning($"检测到窗口属于 {processIds.Count} 个不同的进程，这可能不符合预期");
            }
            else
            {
                _logger.LogDebug($"检测到 {targetWindows.Count} 个窗口，都属于进程 {processIds[0]}（主进程）");
            }

            bool allMessagesSent = true;
            foreach (var browserWnd in targetWindows)
            {
                try
                {
                    if (PostMessage(browserWnd.Handle, WM_CLOSE, IntPtr.Zero, IntPtr.Zero))
                    {
                        _logger.LogDebug($"已向窗口 {browserWnd.Handle} (进程 {browserWnd.ProcessId}) 发送 WM_CLOSE 消息");
                    }
                    else
                    {
                        _logger.LogWarning($"向窗口 {browserWnd.Handle} 发送 WM_CLOSE 消息失败");
                        allMessagesSent = false;
                    }
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, $"向窗口 {browserWnd.Handle} 发送 WM_CLOSE 消息时出错");
                    allMessagesSent = false;
                }
            }

            _logger.LogDebug($"已向所有匹配的窗口发送 WM_CLOSE 消息，窗口关闭任务完成");

            _logger.LogDebug($"等待 {WaitWindowCloseDelay}ms 让窗口关闭...");
            Task.Delay(WaitWindowCloseDelay).Wait();
            return allMessagesSent;
        }


        private (List<Process> mainProcesses, List<Process> childProcesses) IdentifyMainAndChildProcesses(Process[] processes)
        {
            var mainProcesses = new List<Process>();
            var childProcesses = new List<Process>();
            var chromeProcessIds = processes.Select(p => p.Id).ToHashSet();

            foreach (var process in processes)
            {
                try
                {
                    Process parentProcess = ParentProcessUtilities.GetParentProcess(process);
                    if (parentProcess == null)
                    {
                        _logger.LogWarning($"无法获取进程 {process.Id} 的父进程，认为是主进程");
                        mainProcesses.Add(process);
                        continue;
                    }
                    if (!parentProcess.ProcessName.EqualIgnoreCase(_browser.BrowserProcessName))
                    {
                        _logger.LogWarning($"进程 {process.Id} 的父进程 {parentProcess.Id} 名称与浏览器进程名不同，认为是主进程,parentProcess Name:{parentProcess.ProcessName}");
                        mainProcesses.Add(process);
                        continue;
                    }

                    if (chromeProcessIds.Contains(parentProcess.Id))
                    {
                        childProcesses.Add(process);
                        _logger.LogDebug($"识别为子进程: PID={process.Id}, 父进程PID={parentProcess.Id}");
                    }
                    else
                    {
                        mainProcesses.Add(process);
                        _logger.LogDebug($"识别为主进程: PID={process.Id}, 父进程PID={parentProcess.Id}");
                    }
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(_logger, ex, $"识别进程 {process.Id} 类型失败，认为是主进程");
                    mainProcesses.Add(process);
                }
            }

            if (mainProcesses.Count == 0 && processes.Length > 0)
            {
                _logger.LogWarning("未能识别到主进程，所有进程将按子进程处理");
                childProcesses.AddRange(processes);
            }

            return (mainProcesses, childProcesses);
        }
    }
}

