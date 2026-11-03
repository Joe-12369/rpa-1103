using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

public class JavaProcessLocator
{
    private Microsoft.Extensions.Logging.ILogger logger = Logger.OfContext();
    // Win32 API 用于获取更准确的窗口标题
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    // 预设的 Java 进程白名单
    private readonly HashSet<string> _processWhiteList;

    private readonly Dictionary<string, int> _pidCache = new Dictionary<string, int>();
    private readonly object _lock = new object();

    public JavaProcessLocator(IEnumerable<string> additionalExes = null)
    {
        _processWhiteList = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "java", "javaw" };
        if (additionalExes != null)
        {
            foreach (var exe in additionalExes) _processWhiteList.Add(exe.Replace(".exe", ""));
        }
    }

    /// <summary>
    /// 定位目标 PID
    /// </summary>
    /// <param name="targetMainClass">XPath 中的主类名或标识</param>
    /// <param name="titlePart">辅助定位的标题片段（支持部分匹配）</param>
    /// <returns>PID，未找到返回 0</returns>
    public int LocatePid(string targetMainClass, string titlePart = "")
    {
        logger.LogDebug($"[JavaProcessLocator] Start locating PID for MainClass: {targetMainClass}, TitlePart: {titlePart}");
        string cacheKey = $"{targetMainClass}:{titlePart ?? ""}";
        lock (_lock)
        {
            if (_pidCache.TryGetValue(cacheKey, out int cachedPid))
            {
                if (IsPidValid(cachedPid, titlePart))
                {
                    logger.LogDebug($"[JavaProcessLocator] Found valid PID in cache: {cachedPid}");
                    return cachedPid;
                }
                else
                {
                    logger.LogDebug($"[JavaProcessLocator] Invalid PID in cache removed: {cachedPid}");
                    _pidCache.Remove(cacheKey);
                }
            }
        }
   
        var candidatePids = Process.GetProcesses()
            .Where(p => _processWhiteList.Contains(p.ProcessName))
            .Select(p => p.Id)
            .ToList();

        logger.LogDebug($"[JavaProcessLocator] Candidate PIDs found: {string.Join(", ", candidatePids)}");

        if (candidatePids.Count == 0)
        {
            logger.LogDebug("[JavaProcessLocator] No candidate Java processes found.");
            return 0;
        }

        string condition = string.Join(" OR ", candidatePids.Select(id => $"ProcessId = {id}"));
        string query = $"SELECT ProcessId, CommandLine FROM Win32_Process WHERE {condition}";

        try
        {
            logger.LogDebug($"[JavaProcessLocator] Querying WMI for command lines...");
            using (var searcher = new ManagementObjectSearcher(query))
            using (var results = searcher.Get())
            {
                foreach (ManagementObject obj in results)
                {
                    uint pid = (uint)obj["ProcessId"];
                    string cmdLine = obj["CommandLine"]?.ToString() ?? "";

                    logger.LogDebug($"[JavaProcessLocator] Checking PID: {pid}, CmdLine: {cmdLine}");
                    
                    if (!string.IsNullOrEmpty(targetMainClass) && cmdLine.Contains(targetMainClass))
                    {
                        AddCache(cacheKey, (int)pid);
                        logger.LogDebug($"[JavaProcessLocator] Main class matched perfectly. PID located: {pid}");
                        return (int)pid;
                    }
                 
                    if (!string.IsNullOrEmpty(titlePart))
                    {                       
                        if (HasWindowWithTitle(pid, titlePart))
                        {
                            AddCache(cacheKey, (int)pid);
                            logger.LogDebug($"[JavaProcessLocator] Main class mismatch, but Title matched. PID located: {pid}");
                            return (int)pid;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "[JavaProcessLocator] Exception during WMI query.");
        }

        logger.LogDebug("[JavaProcessLocator] Target PID not found.");
        return 0;
    }

    private void AddCache(string key, int pid)
    {
        lock (_lock)
        {
            _pidCache[key] = pid;
        }
    }

    private bool IsPidValid(int pid, string titlePart)
    {
        try
        {
            using (var p = Process.GetProcessById(pid))
            {
                if (p.HasExited) return false;
                if (!_processWhiteList.Contains(p.ProcessName)) return false;

                if (!string.IsNullOrEmpty(titlePart))
                {
                    if (!HasWindowWithTitle((uint)pid, titlePart))
                    {
                        return false;
                    }
                }
                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查指定 PID 是否拥有包含特定标题片段的窗口
    /// </summary>
    private bool HasWindowWithTitle(uint targetPid, string titlePart)
    {
        bool found = false;

        EnumWindows((hWnd, lParam) =>
        {
            uint windowPid;
            GetWindowThreadProcessId(hWnd, out windowPid);

            if (windowPid == targetPid)
            {
                StringBuilder sb = new StringBuilder(512);
                if (GetWindowText(hWnd, sb, sb.Capacity) > 0)
                {
                    string title = sb.ToString();
                    if (!string.IsNullOrEmpty(title))
                    {
                        logger.LogDebug($"[JavaProcessLocator] Window found for PID {targetPid}: '{title}' (Matching against: '{titlePart}')");
                        if (title.IndexOf(titlePart, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            found = true;
                            return false; // 找到匹配窗口，停止遍历
                        }
                    }
                }
            }
            return true;
        }, IntPtr.Zero);

        return found;
    }

    public bool Is64BitProcess(int processId)
    {
        // 如果当前操作系统本身就是 32 位，那么进程一定是 32 位
        if (!Environment.Is64BitOperatingSystem) return false;

        IntPtr handle = OpenProcess(0x1000 /* PROCESS_QUERY_LIMITED_INFORMATION */, false, (uint)processId);
        if (handle == IntPtr.Zero) return Environment.Is64BitProcess; // 备选方案

        try
        {
            if (IsWow64Process(handle, out bool isWow64))
            {
                // IsWow64 为 true 表示进程是 32 位（运行在 64 位系统上）
                // IsWow64 为 false 表示进程是 64 位
                return !isWow64;
            }
        }
        finally
        {
            CloseHandle(handle);
        }
        return Environment.Is64BitProcess;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, uint processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool IsWow64Process(IntPtr hProcess, out bool wow64Process);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);
}
