using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.WebBrowser.Common.Basic.NMH;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace II.RPA.WebBrowser.Common.MultiInstance
{
    /// <summary>
    /// 浏览器进程树与 Native Messaging Host 进程的匹配逻辑，不依赖 BrowserContext 等实例上下文。
    /// </summary>
    public static class BrowserProcessMatcher
    {
        private static ILogger logger = Logger.OfContext();

        /// <summary>
        /// 根据窗口句柄解析浏览器主进程 ID。
        /// 从窗口所属进程出发，沿父进程链向上，返回最顶层的浏览器进程 PID
        /// （即 processFeatureNames 族中无同族父进程的那个进程）。
        /// </summary>
        public static int ResolveMainProcessIdFromWindow(IntPtr windowHandle, IEnumerable<string> processFeatureNames, int defaultValue = -1)
        {
            if (windowHandle == IntPtr.Zero)
                return defaultValue;

            NativeMethods.GetWindowThreadProcessId(windowHandle, out var processId);
            using (var wndProcess = Process.GetProcessById((int)processId))
            {
                int result = ResolveMainProcessIdFromProcess(wndProcess, processFeatureNames);
                return result != -1 ? result : defaultValue;
            }
        }

        /// <summary>
        /// 查找与指定浏览器主进程关联的所有 NMH Host 进程 ID。
        /// 对每个 NMH 进程，沿父进程链找到其所属的浏览器主进程，与 browserMainProcessId 比对。
        /// 父进程链遍历失败时回退到 NMHProcessSnapshot 快照查找。
        /// </summary>
        public static List<int> FindHostProcessIds(
            int browserMainProcessId,
            IEnumerable<string> nmhProcessNames,
            IEnumerable<string> processFeatureNames)
        {
            if (browserMainProcessId <= 0)
                return new List<int>();

            var hostIds = new List<int>();
            var hostProcesses = GetProcessesByNames(nmhProcessNames);
            try
            {
                foreach (var hostProcess in hostProcesses)
                {
                    int mainPid = ResolveMainProcessIdFromProcess(hostProcess, processFeatureNames);

                    // 回退：父进程链可能因中间匿名进程断开，通过快照查找 NMH 的父进程
                    if (mainPid == -1)
                    {
                        var mainSnapshot = NMHProcessSnapshot.GetParentBySnapshot(hostProcess);
                        if (mainSnapshot != null)
                            mainPid = mainSnapshot.Id;
                    }

                    if (mainPid == browserMainProcessId)
                        hostIds.Add(hostProcess.Id);
                }
            }
            finally
            {
                foreach (var p in hostProcesses)
                    p.Dispose();
            }

            return hostIds;
        }

        /// <summary>
        /// 从窗口句柄一步完成：解析浏览器主进程并匹配 NMH Host 进程 ID 列表。
        /// </summary>
        public static List<int> FindHostProcessIdsFromWindow(
            IntPtr windowHandle,
            IEnumerable<string> nmhProcessNames,
            IEnumerable<string> processFeatureNames)
        {
            if (windowHandle == IntPtr.Zero || nmhProcessNames == null || !nmhProcessNames.Any() || processFeatureNames == null || !processFeatureNames.Any())
                return new List<int>();

            logger.LogDebugLazy(() => $"根据句柄 {windowHandle} 获取浏览器进程信息");

            var browserMainPId = ResolveMainProcessIdFromWindow(windowHandle, processFeatureNames);
            if (browserMainPId <= 0)
                return new List<int>();

            return FindHostProcessIds(browserMainPId, nmhProcessNames, processFeatureNames);
        }

        /// <summary>
        /// 从任意进程出发，沿父进程链向上，找到最顶层的浏览器主进程 PID。
        /// 算法：
        ///   1. 浏览器进程族成员 → 记录 PID，继续向上（为了找到族内最顶层）
        ///   2. cmd.exe → 透明跳过（NMH 可能由批处理脚本启动）
        ///   3. 已进入浏览器族后遇到其他进程 → 停止，返回记录的顶层 PID
        ///   4. 尚未进入浏览器族就遇到其他进程 → 停止，返回 -1（无法关联）
        /// 找不到任何匹配则返回 -1。
        /// </summary>
        private static int ResolveMainProcessIdFromProcess(Process startProcess, IEnumerable<string> processFeatureNames)
        {
            const int MaxDepth = 20;
            var visited = new HashSet<int> { startProcess.Id };
            int depth = 0;
            int mainProcessId = -1;
            bool foundFamily = false;

            try
            {
                Process current = startProcess;

                while (current != null && depth < MaxDepth)
                {
                    if (IsProcessFeature(current.ProcessName, processFeatureNames))
                    {
                        // 浏览器进程族成员 → 记录，继续向上走到最顶层
                        mainProcessId = current.Id;
                        foundFamily = true;
                    }
                    else if (IsCmdProcess(current.ProcessName))
                    {
                        // cmd.exe 透明处理：NMH 可能由批处理脚本启动，穿透不阻断
                    }
                    else if (foundFamily)
                    {
                        // 已找到浏览器进程，当前进程不在族内 → 上一个记录的就是最顶层
                        break;
                    }
                    else if (current == startProcess)
                    {
                        // 起点进程（如 NMH 自身）不在浏览器族内 → 透明穿透，从父进程继续
                    }
                    else
                    {
                        // 已离开起点但尚未进入浏览器族，遇到未知进程 → 无法建立关联
                        break;
                    }

                    var parent = ParentProcessUtilities.GetParentProcess(current);
                    if (current != startProcess)
                        current.Dispose();
                    current = parent;

                    if (current != null)
                    {
                        if (!visited.Add(current.Id))
                        {
                            logger.LogWarning($"检测到父进程循环引用，返回 {mainProcessId}");
                            current.Dispose();
                            return mainProcessId;
                        }
                        depth++;
                    }
                }

                if (current != null && current != startProcess)
                    current.Dispose();

                if (depth >= MaxDepth)
                    logger.LogWarning($"达到最大遍历深度 {MaxDepth}，返回 {mainProcessId}");

                return mainProcessId;
            }
            catch (Exception ex)
            {
                logger.LogError($"获取父进程链时发生异常: {ex.Message}，返回 {mainProcessId}");
                return mainProcessId;
            }
        }

        /// <summary>
        /// 从 NMH 进程出发，沿父进程链向上自动发现浏览器主进程。
        /// 无需预先知道浏览器进程名——跳过 cmd.exe 等壳进程，首个非壳进程的 ProcessName
        /// 即为浏览器特征名，继续向上遍历同族进程直到最顶层。
        /// </summary>
        /// <param name="nmhProcess">NMH 进程对象</param>
        /// <param name="processFeatureName">[out] 发现的浏览器进程名（如 "chrome"），失败时为 null</param>
        /// <returns>浏览器主进程 PID，失败返回 -1</returns>
        public static int ResolveBrowserProcessFromNMH(Process nmhProcess, out string processFeatureName)
        {
            processFeatureName = null;

            const int MaxDepth = 20;
            var visited = new HashSet<int> { nmhProcess.Id };
            int depth = 0;
            int browserMainPid = -1;
            bool foundBrowser = false;

            try
            {
                // 从 NMH 的父进程开始
                var current = ParentProcessUtilities.GetParentProcess(nmhProcess);

                while (current != null && depth < MaxDepth)
                {
                    if (!visited.Add(current.Id))
                    {
                        logger.LogWarning($"ResolveBrowserProcessFromNMH: 检测到父进程循环引用，返回 {browserMainPid}");
                        current.Dispose();
                        return browserMainPid;
                    }

                    if (IsCmdProcess(current.ProcessName))
                    {
                        // cmd.exe 透明穿透
                        var cmd_parent = ParentProcessUtilities.GetParentProcess(current);
                        current.Dispose();
                        current = cmd_parent;
                        depth++;
                        continue;
                    }

                    if (!foundBrowser)
                    {
                        // 首个非 cmd 进程 → 认定为浏览器，其进程名即为特征名
                        processFeatureName = current.ProcessName;
                        browserMainPid = current.Id;
                        foundBrowser = true;
                    }
                    else if (IsProcessFeature(current.ProcessName, new[] { processFeatureName }))
                    {
                        // 同族进程 → 继续向上，更新顶层 PID
                        browserMainPid = current.Id;
                    }
                    else
                    {
                        // 不再同族 → 停止
                        current.Dispose();
                        break;
                    }

                    var parent = ParentProcessUtilities.GetParentProcess(current);
                    current.Dispose();
                    current = parent;
                    depth++;
                }

                if (current != null)
                    current.Dispose();

                if (depth >= MaxDepth)
                    logger.LogWarning($"ResolveBrowserProcessFromNMH: 达到最大遍历深度 {MaxDepth}，返回 {browserMainPid}");

                return browserMainPid;
            }
            catch (Exception ex)
            {
                logger.LogError($"ResolveBrowserProcessFromNMH 异常: {ex.Message}，返回 {browserMainPid}");
                return browserMainPid;
            }
        }

        /// <summary>
        /// 判断进程名是否为 cmd.exe（NMH 经由批处理/命令行启动时的透明壳进程）。
        /// </summary>
        private static bool IsCmdProcess(string processName)
        {
            return string.Equals(processName, "cmd", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 按进程名列表枚举当前运行的进程（调用方负责 Dispose）。
        /// </summary>
        public static Process[] GetProcessesByNames(IEnumerable<string> processNames)
        {
            if (processNames == null)
                return Array.Empty<Process>();

            var names = processNames as string[] ?? processNames.ToArray();
            if (names.Length == 0)
                return Array.Empty<Process>();

            var processes = new List<Process>();
            int sid = Process.GetCurrentProcess().SessionId;
            foreach (var procName in names)
            {
                var hostProcesses = Process.GetProcessesByName(procName).Where(p => p.SessionId == sid);

                processes.AddRange(hostProcesses);
            }

            logger.LogDebugBlock(() =>
            {
                logger.LogDebug($"获取到 NMH 进程：{processes.Select(p => p.Id).ToJson()}");
            });
            return processes.ToArray();
        }

        public static bool IsProcessFeature(string processName, IEnumerable<string> processFeatureNames)
        {
            if (processFeatureNames == null)
                return false;
            return processFeatureNames.Any(pName =>
                pName.Equals(processName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
