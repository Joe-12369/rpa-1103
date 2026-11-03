using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using FlaUI.Core.AutomationElements;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.WdtErp.Models;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace II.RPA.Plugin.WdtErp
{
    /// <summary>
    /// 旺店通服务通讯代理
    /// </summary>
    internal class WdtErpProxy
    {
        private const int DefaultPipeConnectTimeoutMilliseconds = 30000;
        private const int DetectPipeConnectTimeoutMilliseconds = 100;
        private const int WaitPipeConnectTimeoutMilliseconds = 500;
        private static readonly TimeSpan InjectionTempDirectoryRetention = TimeSpan.FromDays(3);
        private static readonly HashSet<string> TextStableSelectorTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "button",
            "li",
            "option",
            "td",
            "caption",
            "text"
        };

        public ILogger logger = Logger.OfContext();
        private readonly object connectServerLock = new object();
        private readonly object cleanupWatcherLock = new object();
        private readonly Dictionary<int, Process> cleanupWatchers = new Dictionary<int, Process>();
        private readonly Dictionary<int, string> processInjectionTempDirectories = new Dictionary<int, string>();

        private static readonly Lazy<WdtErpProxy> _instance = new Lazy<WdtErpProxy>(() => new WdtErpProxy());
        private volatile bool hasConnectedServer;
        private int connectedProcessId;
        private string connectedProcessName;

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool IsWow64Process(IntPtr process, out bool wow64Process);

        private static readonly string[] InjectionDependencyFileNames =
        {
            "htmlayout_bootstrap.dll",
            "htmlayout_bootstrap32.dll",
            "htmlayout_automation.dll",
            "htmlayout_automation.runtimeconfig.json",
            "htmlayout_automation.deps.json",
            "Newtonsoft.Json.dll"
        };
        public static WdtErpProxy Instance { get { return _instance.Value; } }

        private WdtErpProxy()
        {
            logger.LogInformation($"Desktop DPI: {NativeMethods.DpiX / 96}");
        }

        private string pipeName { get; set; }

        bool IsDllInUse(int processId)
        {
            try
            {
                string tempRootDirectory = GetInjectionTempRootDirectory();
                if (!Directory.Exists(tempRootDirectory))
                {
                    return false;
                }

                string directoryPattern = $"inject_{processId}_*";
                foreach (string injectionDirectory in Directory.EnumerateDirectories(
                    tempRootDirectory,
                    directoryPattern,
                    SearchOption.TopDirectoryOnly))
                {
                    if (IsInjectionDllInUse(injectionDirectory, "htmlayout_bootstrap.dll") ||
                        IsInjectionDllInUse(injectionDirectory, "htmlayout_bootstrap32.dll") ||
                        IsInjectionDllInUse(injectionDirectory, "htmlayout_automation.dll"))
                    {
                        logger.LogDebug($"injection dll is in use, pid:{processId}, directory:{injectionDirectory}");
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                logger.LogWarning($"check injection dll in use failed, processId:{processId}, error:{ex.Message}");
                return false;
            }
        }

        private bool IsInjectionDllInUse(string injectionDirectory, string fileName)
        {
            string dllPath = Path.Combine(injectionDirectory, fileName);
            if (!File.Exists(dllPath))
            {
                return false;
            }

            try
            {
                using (FileStream fs = new FileStream(dllPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    return false;
                }
            }
            catch (FileNotFoundException)
            {
                return false;
            }
            catch (DirectoryNotFoundException)
            {
                return false;
            }
            catch (IOException)
            {
                return true;
            }
            catch (Exception ex)
            {
                logger.LogWarning($"check injection dll in use failed, path:{dllPath}, error:{ex.Message}");
                return true;
            }
        }

        private string BuildPipeName(int processId)
        {
            return $"WDTERP_{processId}";
        }

        private void MarkConnectedServer()
        {
            hasConnectedServer = true;
            connectedProcessId = GuiOperator.ProcessId;
            connectedProcessName = GuiOperator.ProcessName;
        }

        private void ResetConnectionState(string reason = null)
        {
            string previousPipeName = pipeName;
            int previousProcessId = connectedProcessId;
            string previousProcessName = connectedProcessName;

            hasConnectedServer = false;
            pipeName = null;
            connectedProcessId = 0;
            connectedProcessName = null;

            if (!string.IsNullOrWhiteSpace(reason))
            {
                logger.LogInformation($"reset wdterp connection state, reason:{reason}, pipe:{previousPipeName}, process:{previousProcessName}, pid:{previousProcessId}");
            }
        }

        private bool IsConnectedProcessAlive()
        {
            if (connectedProcessId < 1)
            {
                return false;
            }

            try
            {
                var process = Process.GetProcessById(connectedProcessId);
                if (!string.IsNullOrWhiteSpace(connectedProcessName) &&
                    !string.Equals(process.ProcessName, connectedProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private bool IsCachedConnectionStale()
        {
            if (!hasConnectedServer || string.IsNullOrWhiteSpace(pipeName))
            {
                return true;
            }

            if (!IsConnectedProcessAlive())
            {
                return true;
            }

            if (TryGetCurrentContextProcessId(out int currentContextProcessId) && currentContextProcessId != connectedProcessId)
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(GuiOperator.ProcessName) &&
                !string.IsNullOrWhiteSpace(connectedProcessName) &&
                !string.Equals(GuiOperator.ProcessName, connectedProcessName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private bool TryGetCurrentGuiProcess(out Process targetProcess)
        {
            targetProcess = null;

            if (GuiOperator.ProcessId < 1)
            {
                return false;
            }

            try
            {
                var process = Process.GetProcessById(GuiOperator.ProcessId);
                if (string.IsNullOrWhiteSpace(GuiOperator.ProcessName) ||
                    string.Equals(process.ProcessName, GuiOperator.ProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    targetProcess = process;
                    return true;
                }

                logger.LogWarning($"current gui process mismatch, cachedProcess:{GuiOperator.ProcessName}, actualProcess:{process.ProcessName}, pid:{GuiOperator.ProcessId}");
                return false;
            }
            catch (ArgumentException)
            {
                logger.LogInformation($"current gui process not found, pid:{GuiOperator.ProcessId}");
                return false;
            }
            catch (InvalidOperationException)
            {
                logger.LogInformation($"current gui process exited before connect, pid:{GuiOperator.ProcessId}");
                return false;
            }
        }

        private bool TryGetForegroundTargetProcess(out Process targetProcess)
        {
            targetProcess = null;

            if (string.IsNullOrWhiteSpace(GuiOperator.ProcessName))
            {
                return false;
            }

            IntPtr foregroundWindow = GetForegroundWindow();
            if (foregroundWindow == IntPtr.Zero)
            {
                return false;
            }

            GetWindowThreadProcessId(foregroundWindow, out int foregroundProcessId);
            if (foregroundProcessId < 1)
            {
                return false;
            }

            try
            {
                var process = Process.GetProcessById(foregroundProcessId);
                if (!string.Equals(process.ProcessName, GuiOperator.ProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                targetProcess = process;
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private bool TryGetCurrentContextProcessId(out int processId)
        {
            processId = 0;

            if (TryGetCurrentGuiProcess(out var currentGuiProcess))
            {
                processId = currentGuiProcess.Id;
                return true;
            }

            if (TryGetForegroundTargetProcess(out var foregroundProcess))
            {
                processId = foregroundProcess.Id;
                return true;
            }

            return false;
        }

        private bool TryResolveTargetProcess(out Process targetProcess)
        {
            targetProcess = null;

            if (TryGetCurrentGuiProcess(out targetProcess))
            {
                return true;
            }

            if (TryGetForegroundTargetProcess(out targetProcess))
            {
                logger.LogInformation($"resolved wdterp target from foreground window, process:{targetProcess.ProcessName}, pid:{targetProcess.Id}");
                return true;
            }

            if (string.IsNullOrWhiteSpace(GuiOperator.ProcessName))
            {
                return false;
            }

            var processes = Process.GetProcessesByName(GuiOperator.ProcessName);
            if (processes.Length < 1)
            {
                return false;
            }

            targetProcess = processes[0];
            if (processes.Length > 1)
            {
                logger.LogWarning($"multiple wdterp processes detected for {GuiOperator.ProcessName}, foreground window did not resolve a target, fallback to first instance pid:{targetProcess.Id}.");
            }

            return true;
        }

        private bool InjectService(Process targetProcess)
        {
            string injectorPath = ResolveInjectorPath();
            string sourceDirectory = ResolveInjectionSourceDirectory();
            var injectDirectories = ResolveInjectionDirectories(sourceDirectory, targetProcess.Id).ToList();

            if (!File.Exists(injectorPath))
            {
                logger.LogError($"injector not found, path:{injectorPath}");
                return false;
            }

            for (int i = 0; i < injectDirectories.Count; i++)
            {
                string injectDirectory = injectDirectories[i];
                string bootstrapDllPath = ResolveBootstrapDllPath(targetProcess, injectDirectory);

                logger.LogInformation($"inject service start, attempt:{i + 1}/{injectDirectories.Count}, injector:{injectorPath}, processId:{targetProcess.Id}, pipe:{pipeName}, dll:{bootstrapDllPath}");

                if (!File.Exists(bootstrapDllPath))
                {
                    logger.LogWarning($"inject attempt skipped, bootstrap dll not found, attempt:{i + 1}, path:{bootstrapDllPath}");
                    continue;
                }

                if (TryRunInjector(injectorPath, targetProcess.Id, bootstrapDllPath, out int exitCode))
                {
                    RegisterInjectionTempCleanup(targetProcess, sourceDirectory, injectDirectory);
                    logger.LogInformation($"inject service success, processId:{targetProcess.Id}, pipe:{pipeName}, exitCode:{exitCode}, attempt:{i + 1}");
                    return true;
                }

                logger.LogWarning($"inject service finished with non-zero exit code, processId:{targetProcess.Id}, pipe:{pipeName}, exitCode:{exitCode}, attempt:{i + 1}");
            }

            logger.LogError($"inject service fail after all attempts, processId:{targetProcess.Id}, pipe:{pipeName}");
            return false;
        }

        private bool TryRunInjector(string injectorPath, int processId, string bootstrapDllPath, out int exitCode)
        {
            exitCode = -1;
            using (var process = new Process())
            {
                try
                {
                    process.StartInfo.FileName = injectorPath;
                    process.StartInfo.Arguments = $"{processId} \"{bootstrapDllPath}\"";
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    process.StartInfo.WorkingDirectory = Path.GetDirectoryName(injectorPath);
                    process.Start();

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    exitCode = process.ExitCode;

                    logger.LogDebug($"IndeedInjector Output: {output}");
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        logger.LogError($"IndeedInjector Error: {error}");
                    }

                    return exitCode == 0;
                }
                catch (Exception ex)
                {
                    logger.LogError($"inject service fail, processId:{processId}, pipe:{pipeName}, dll:{bootstrapDllPath}, error:{ex.Message}");
                    return false;
                }
            }
        }

        private string ResolveInjectorPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IndeedInjector.exe");
        }

        private string ResolveInjectionSourceDirectory()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        private string ResolveInjectionDirectory(string sourceDirectory, int processId)
        {
            bool useTempDirectory = Configurations.Info?.UseInjectionTempDirectory == true;
            if (!useTempDirectory)
            {
                logger.LogInformation($"inject temp directory disabled by config, use source directory:{sourceDirectory}");
                return sourceDirectory;
            }

            try
            {
                string tempRootDirectory = GetInjectionTempRootDirectory();
                CleanupStaleInjectionTempDirectories(tempRootDirectory);

                string tempDirectory = Path.Combine(tempRootDirectory, $"inject_{processId}_{Guid.NewGuid():N}");
                Directory.CreateDirectory(tempDirectory);

                CopyInjectionDependencies(sourceDirectory, tempDirectory);
                logger.LogInformation($"inject dependencies copied to temp directory:{tempDirectory}");
                return tempDirectory;
            }
            catch (Exception ex)
            {
                logger.LogWarning($"prepare temp injection directory failed, fallback to source directory:{sourceDirectory}, error:{ex.Message}");
                return sourceDirectory;
            }
        }

        private IEnumerable<string> ResolveInjectionDirectories(string sourceDirectory, int processId)
        {
            string preferredDirectory = ResolveInjectionDirectory(sourceDirectory, processId);
            var candidates = new List<string>
            {
                preferredDirectory,
                sourceDirectory,
                AppDomain.CurrentDomain.BaseDirectory
            };

            foreach (var candidate in candidates)
            {
                if (string.IsNullOrWhiteSpace(candidate))
                {
                    continue;
                }

                if (!Directory.Exists(candidate))
                {
                    continue;
                }

                yield return candidate;
            }
        }

        private string GetInjectionTempRootDirectory()
        {
            return Path.Combine(Path.GetTempPath(), "IIRPA", "wdterp");
        }

        private void CleanupStaleInjectionTempDirectories(string tempRootDirectory)
        {
            try
            {
                if (!Directory.Exists(tempRootDirectory))
                {
                    return;
                }

                DateTime cutoffUtc = DateTime.UtcNow.Subtract(InjectionTempDirectoryRetention);
                foreach (var directory in Directory.GetDirectories(tempRootDirectory, "inject_*"))
                {
                    string directoryName = Path.GetFileName(directory) ?? string.Empty;
                    string[] segments = directoryName.Split('_');

                    bool shouldDelete = false;
                    if (segments.Length >= 3 && int.TryParse(segments[1], out int pid))
                    {
                        shouldDelete = !IsProcessAlive(pid);
                    }
                    else
                    {
                        shouldDelete = Directory.GetCreationTimeUtc(directory) < cutoffUtc;
                    }

                    if (!shouldDelete)
                    {
                        continue;
                    }

                    TryDeleteInjectionTempDirectory(directory, "stale cleanup");
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug($"cleanup stale temp directories failed, error:{ex.Message}");
            }
        }

        private bool IsProcessAlive(int pid)
        {
            try
            {
                var process = Process.GetProcessById(pid);
                return !process.HasExited;
            }
            catch
            {
                return false;
            }
        }

        private void RegisterInjectionTempCleanup(Process targetProcess, string sourceDirectory, string injectionDirectory)
        {
            if (string.IsNullOrWhiteSpace(injectionDirectory) ||
                string.Equals(sourceDirectory, injectionDirectory, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string tempRootDirectory = GetInjectionTempRootDirectory();
            if (!injectionDirectory.StartsWith(tempRootDirectory, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            int pid = targetProcess.Id;
            lock (cleanupWatcherLock)
            {
                if (cleanupWatchers.TryGetValue(pid, out var existingWatcher))
                {
                    existingWatcher.Exited -= OnInjectedProcessExited;
                    existingWatcher.Dispose();
                    cleanupWatchers.Remove(pid);
                }

                processInjectionTempDirectories[pid] = injectionDirectory;

                targetProcess.EnableRaisingEvents = true;
                targetProcess.Exited += OnInjectedProcessExited;
                cleanupWatchers[pid] = targetProcess;
            }

            logger.LogInformation($"registered temp injection cleanup, pid:{pid}, dir:{injectionDirectory}");
        }

        private void OnInjectedProcessExited(object sender, EventArgs e)
        {
            if (!(sender is Process process))
            {
                return;
            }

            string directory = null;
            int pid = 0;
            lock (cleanupWatcherLock)
            {
                try
                {
                    pid = process.Id;
                }
                catch (InvalidOperationException)
                {
                    foreach (var pair in cleanupWatchers)
                    {
                        if (ReferenceEquals(pair.Value, process))
                        {
                            pid = pair.Key;
                            break;
                        }
                    }
                }

                if (pid < 1)
                {
                    logger.LogWarning("injected process exited but pid could not be resolved");
                    return;
                }

                try
                {
                    logger.LogWarning($"injected process exited, pid:{pid}, exitCode:{process.ExitCode}");
                }
                catch (InvalidOperationException)
                {
                }

                try
                {
                    process.Exited -= OnInjectedProcessExited;
                }
                catch (InvalidOperationException)
                {
                }

                if (cleanupWatchers.TryGetValue(pid, out var watcher))
                {
                    watcher.Dispose();
                    cleanupWatchers.Remove(pid);
                }

                if (processInjectionTempDirectories.TryGetValue(pid, out var trackedDirectory))
                {
                    directory = trackedDirectory;
                    processInjectionTempDirectories.Remove(pid);
                }
            }

            if (!string.IsNullOrWhiteSpace(directory))
            {
                TryDeleteInjectionTempDirectory(directory, $"process exited pid:{pid}");
            }
        }

        private void TryDeleteInjectionTempDirectory(string directory, string reason)
        {
            try
            {
                if (Directory.Exists(directory))
                {
                    PreserveInjectionBootstrapLog(directory);
                    Directory.Delete(directory, true);
                    logger.LogInformation($"deleted injection temp directory, reason:{reason}, dir:{directory}");
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug($"delete injection temp directory failed, reason:{reason}, dir:{directory}, error:{ex.Message}");
            }
        }

        private void PreserveInjectionBootstrapLog(string injectionDirectory)
        {
            string sourcePath = Path.Combine(injectionDirectory, "bootstrap.log");
            if (!File.Exists(sourcePath))
            {
                return;
            }

            try
            {
                string logDirectory = Path.Combine(GetInjectionTempRootDirectory(), "logs");
                Directory.CreateDirectory(logDirectory);
                string destinationPath = Path.Combine(
                    logDirectory,
                    $"{Path.GetFileName(injectionDirectory)}.bootstrap.log");
                File.Copy(sourcePath, destinationPath, true);
                logger.LogInformation($"preserved bootstrap log, source:{sourcePath}, destination:{destinationPath}");
            }
            catch (Exception ex)
            {
                logger.LogWarning($"preserve bootstrap log failed, dir:{injectionDirectory}, error:{ex.Message}");
            }
        }

        private void CopyInjectionDependencies(string sourceDirectory, string destinationDirectory)
        {
            var copiedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var fileName in InjectionDependencyFileNames)
            {
                string sourcePath = Path.Combine(sourceDirectory, fileName);
                if (string.Equals(fileName, "htmlayout_bootstrap32.dll", StringComparison.OrdinalIgnoreCase) && !File.Exists(sourcePath))
                {
                    // 兼容历史拼写错误文件名。
                    string typoPath = Path.Combine(sourceDirectory, "htmlayout_boostrap32.dll");
                    if (File.Exists(typoPath))
                    {
                        sourcePath = typoPath;
                    }
                }

                if (!File.Exists(sourcePath))
                {
                    continue;
                }

                string destinationPath = Path.Combine(destinationDirectory, fileName);
                File.Copy(sourcePath, destinationPath, true);
                copiedFiles.Add(Path.GetFileName(destinationPath));
            }
            logger.LogInformation($"copied injection dependencies, source:{sourceDirectory}, destination:{destinationDirectory}, count:{copiedFiles.Count}");
        }

        private string ResolveBootstrapDllPath(Process targetProcess, string injectDirectory)
        {
            bool targetIs64Bit = IsProcess64Bit(targetProcess);
            string bootstrapDllName = targetIs64Bit ? "htmlayout_bootstrap.dll" : "htmlayout_bootstrap32.dll";
            string bootstrapPath = Path.Combine(injectDirectory, bootstrapDllName);

            if (targetIs64Bit || File.Exists(bootstrapPath))
            {
                return bootstrapPath;
            }

            string typoBootstrap32Path = Path.Combine(injectDirectory, "htmlayout_boostrap32.dll");
            if (File.Exists(typoBootstrap32Path))
            {
                return typoBootstrap32Path;
            }

            return bootstrapPath;
        }

        private bool IsProcess64Bit(Process process)
        {
            if (!Environment.Is64BitOperatingSystem)
            {
                return false;
            }

            if (!IsWow64Process(process.Handle, out bool wow64Process))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"detect process architecture failed, pid:{process.Id}");
            }

            return !wow64Process;
        }

        private bool IsConnected(int connectTimeoutMilliseconds = DetectPipeConnectTimeoutMilliseconds)
        {
            try
            {
                logger.LogDebug($"connecting pipe {pipeName}, timeout:{connectTimeoutMilliseconds}ms");
                var response = SendPipeRequest("/", string.Empty, "GET", connectTimeoutMilliseconds);
                if (string.IsNullOrWhiteSpace(response))
                {
                    return false;
                }

                int expectedProcessId = GuiOperator.ProcessId > 0 ? GuiOperator.ProcessId : connectedProcessId;
                if (expectedProcessId > 0)
                {
                    return response.Contains($"({expectedProcessId})");
                }

                return true;
            }
            catch (TimeoutException)
            {
                return false;
            }
            catch
            {
                return false;
            }
        }

        private bool WaitForPipeConnected(int retryCount = 20, int delayMilliseconds = 300, int connectTimeoutMilliseconds = WaitPipeConnectTimeoutMilliseconds)
        {
            for (int i = 0; i < retryCount; i++)
            {
                if (IsConnected(connectTimeoutMilliseconds))
                {
                    logger.LogInformation($"pipe connected success, pipe:{pipeName}, attempt:{i + 1}");
                    return true;
                }

                Thread.Sleep(delayMilliseconds);
            }

            logger.LogWarning($"pipe connected failed, pipe:{pipeName}, retryCount:{retryCount}, delayMilliseconds:{delayMilliseconds}");
            return false;
        }

        public bool ConnectServer()
        {
            if (string.IsNullOrWhiteSpace(GuiOperator.ProcessName))
            {
                ResetConnectionState("process name is empty");
                logger.LogWarning("wdterp process name is empty, skip connect server.");
                return false;
            }

            if (!TryResolveTargetProcess(out var targetProcess))
            {
                ResetConnectionState($"process {GuiOperator.ProcessName} is not running");
                logger.LogWarning($"{GuiOperator.ProcessName} not running!");
                return false;
            }

            GuiOperator.ProcessName = targetProcess.ProcessName;
            GuiOperator.ProcessId = targetProcess.Id;
            pipeName = BuildPipeName(GuiOperator.ProcessId);
            logger.LogInformation($"connect server start, process:{GuiOperator.ProcessName}, pid:{GuiOperator.ProcessId}, pipe:{pipeName}");

            if (IsConnected())
            {
                MarkConnectedServer();
                logger.LogInformation($"connect server success, pipe already available:{pipeName}");
                return true;
            }

            var injected = IsDllInUse(targetProcess.Id);
            if (injected)
            {
                logger.LogInformation($"wdterp core already injected, wait pipe connect:{pipeName}");
                if (WaitForPipeConnected())
                {
                    MarkConnectedServer();
                    logger.LogInformation($"connect server success after waiting injected service, pipe:{pipeName}");
                    return true;
                }
                logger.LogWarning($"pipe unavailable after waiting injected service, continue to reinject, pipe:{pipeName}");
            }

            logger.LogInformation($"start IndeedInjector injection, pipe:{pipeName}");
            if (!InjectService(targetProcess))
            {
                ResetConnectionState($"inject service failed, pipe:{pipeName}");
                return false;
            }

            if (WaitForPipeConnected())
            {
                MarkConnectedServer();
                logger.LogInformation($"connect server success after injection, pipe:{pipeName}");
                return true;
            }

            ResetConnectionState($"pipe {pipeName} unavailable after injection");
            logger.LogError($"connect server failed, pipe {pipeName} unavailable after injection.");
            return false;

        }

        private void EnsureServerConnected()
        {
            if (hasConnectedServer && !IsCachedConnectionStale())
            {
                return;
            }

            lock (connectServerLock)
            {
                if (hasConnectedServer && !IsCachedConnectionStale())
                {
                    return;
                }

                if (hasConnectedServer)
                {
                    ResetConnectionState($"detected stale cached connection for process:{GuiOperator.ProcessName}, pid:{GuiOperator.ProcessId}");
                }

                if (!ConnectServer())
                {
                    throw new InvalidOperationException($"connect server failed, pipe:{pipeName}");
                }
            }
        }

        private int ScaleDesktop(int value, bool isLogic = true)
        {
            var scale = (float)NativeMethods.DpiX / 96;
            int newValue;
            if (isLogic)
            {
                newValue = Convert.ToInt32(Math.Ceiling(value / scale));
            }
            else
            {
                newValue = Convert.ToInt32(Math.Ceiling(value * scale));
            }
            return newValue;
        }

        public bool InspectNode(AutomationElement rootElement, AutomationElement pointElement, System.Drawing.Point point, string similarSelector, out IGuiNode node)
        {
            node = null;
            try
            {
                int scaledX = ScaleDesktop(point.X);
                int scaledY = ScaleDesktop(point.Y);
                var postData = JsonHelper.JsonByNewtonJson(new { x = scaledX, y = scaledY });
                var response = HttpRequest("/FindElement", postData);
                if (response.ErrNo == 0)
                {
                    var data = JToken.Parse(response.Data);
                    var element = data.Value<int>("element");
                    if (element < 1)
                    {
                        //无效元素
                        logger.LogDebug(data?.ToString());
                        return false;
                    }
                    node = new WdtErpNode
                    {
                        SimilarElementsSelector = similarSelector,
                        ProcessID = GuiOperator.ProcessId,
                        UserDefinedID = data.Value<string>("uid"),
                        ClassName = data.Value<string>("text"),
                        Name = rootElement.Name ?? GuiOperator.ProcessName,
                        WindowHandle = new IntPtr(data.Value<int>("hwnd")),
                        ElementPtr = new IntPtr(data.Value<int>("element")),
                        ControlType = data.Value<string>("ctype"),
                        BoundingRectangle = new Rectangle
                        {
                            X = ScaleDesktop(data.First.First.Value<int>("left"), false),
                            Y = ScaleDesktop(data.First.First.Value<int>("top"), false),
                            Width = ScaleDesktop(data.First.First.Value<int>("right") - data.First.First.Value<int>("left"), false),
                            Height = ScaleDesktop(data.First.First.Value<int>("bottom") - data.First.First.Value<int>("top"), false)
                        }
                    };
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                logger.LogError("Inspect WdtErp Exception:" + e.Message);
                return false;
            }

        }

        public void ProcessSimilarSelector(JArray currentSelector, string _similarSelector)
        {
            try
            {
                var similarSelector = JObject.Parse(_similarSelector)["selector"] as JArray;

                if (currentSelector.Count != similarSelector.Count)
                {
                    logger.LogInformation("selector not equal!");
                    return;
                }

                JToken tr = null, td = null;
                bool rowEqual = false;
                bool columnEqual = false;
                for (int i = 0; i < currentSelector.Count; i++)
                {
                    JToken currentNode = currentSelector[i];
                    JToken similarNode = similarSelector[i];
                    var name = currentNode.Value<string>("name");
                    if ((name == similarNode.Value<string>("name")) &&
                        (currentNode.Value<string>("type") == similarNode.Value<string>("type")) &&
                        (currentNode["props"][0].Value<string>("name") == similarNode["props"][0].Value<string>("name")))
                    {
                        var index1 = currentNode["props"][0].Value<string>("value");
                        var index2 = similarNode["props"][0].Value<string>("value");
                        if (name == "tr")
                        {
                            tr = currentNode;
                            rowEqual = index1 == index2;
                        }
                        else if (name == "td")
                        {
                            columnEqual = index1 == index2;
                            td = currentNode;
                        }

                        else
                        {
                            if (index1 != index2)
                            {
                                logger.LogInformation("index not equal");
                                return;
                            }
                        }
                    }
                    else
                    {
                        logger.LogInformation($"{name} not equal!");
                        return;
                    }
                }

                if (td != null)
                {
                    if (rowEqual)
                    {
                        ModifyIndexPropAccuracy(td);
                    }
                    else if (columnEqual)
                    {
                        ModifyIndexPropAccuracy(tr);
                    }
                    else
                    {
                        logger.LogInformation($"the cells are located in different rows and different columns.");
                    }

                }
                else if (tr != null)
                {
                    ModifyIndexPropAccuracy(tr);
                }
                else
                {
                    logger.LogInformation("not table element!");
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex);
            }

        }

        private void ModifyIndexPropAccuracy(JToken node)
        {
            JArray props = node["props"] as JArray;
            if (props != null)
            {
                foreach (JObject prop in props)
                {
                    if (prop != null && prop["name"]?.ToString() == "@index")
                    {
                        prop["accurate"] = false;
                        break;
                    }
                }
            }
        }

        public IGuiNode GetElementFromSelector(string selector, object extend = null)
        {
            var postData = JsonHelper.JsonByNewtonJson(new { selector = selector });
            var response = HttpRequest("/GetElementBySelector", postData);
            if (response.ErrNo != 0)
            {
                return null;
            }
            else
            {
                var data = JToken.Parse(response.Data);
                var element = data.Value<int>("element");
                if (element < 1)
                {
                    //无效元素
                    logger.LogDebug(data?.ToString());
                    return null;
                }
                var newNode = new WdtErpNode
                {
                    ProcessID = GuiOperator.ProcessId,
                    userDefinedID = data.Value<string>("uid"),
                    ClassName = data.Value<string>("panel"),
                    Name = data.Value<string>("window"),
                    ControlType = data.Value<string>("ctype"),
                    ElementPtr = new IntPtr(element),
                    WindowHandle = new IntPtr(data.Value<int>("hwnd")),
                    BoundingRectangle = new Rectangle
                    {
                        X = ScaleDesktop(data.First.First.Value<int>("left"), false),
                        Y = ScaleDesktop(data.First.First.Value<int>("top"), false),
                        Width = ScaleDesktop(data.First.First.Value<int>("right") - data.First.First.Value<int>("left"), false),
                        Height = ScaleDesktop(data.First.First.Value<int>("bottom") - data.First.First.Value<int>("top"), false)
                    }
                };
                return newNode;
            }

        }

        public int GetElementChildrenCount(WdtErpNode table)
        {
            var postData = JsonHelper.JsonByNewtonJson(new { element = (int)table.ElementPtr });
            var response = HttpRequest("/GetChildrenCount", postData);
            if (response.ErrNo != 0)
            {
                return 0;
            }
            else
            {
                var data = JToken.Parse(response.Data);
                return Convert.ToInt32(data);
            }

        }

        public string GetElementPath(IntPtr hwnd, IntPtr element)
        {
            var postData = JsonHelper.JsonByNewtonJson(new { hwnd = (int)hwnd, element = (int)element });
            var response = HttpRequest("/GetElementPath", postData);
            if (response == null || response.ErrNo != 0)
            {
                return null;
            }
            else
            {
                return response?.Data;
            }
        }

        private string NormalizeSelectorText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            string normalized = Regex.Replace(text, "\\s+", " ").Trim();
            normalized = normalized.Replace("[", "").Replace("]", "").Replace("\"", "");
            if (normalized.Length > 40)
            {
                normalized = normalized.Substring(0, 40);
            }

            return normalized;
        }

        public string ConvertPathToSelector(string window, string panel, string xpath, string elementInnerText = null)
        {
            WdtErpSelector selector = new WdtErpSelector();
            try
            {
                var regex = new Regex(@"^(.*)\[index=(.*)\]");
                var fragment = xpath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                selector.Nodes = new Node[fragment.Length + 2];
                selector.Nodes[0] = new Node
                {
                    name = "CoolWindow",
                    type = "win",
                    props = new Prop[] {
                     new Prop{ name="title",value=window,pattern="equal",accurate="true" }
                    }

                };
                selector.Nodes[1] = new Node
                {
                    name = "CoolWindow",
                    type = "win",
                    props = new Prop[] {
                     new Prop{ name="title",value=panel,pattern="equal",accurate="true" }
                    }

                };
                selector.Nodes[2] = new Node
                {
                    name = "html",
                    type = "web",
                    props = new Prop[] {
                     new Prop{ name="@index",value="0",pattern="equal",accurate="true" }
                    }

                };
                for (int i = 1; i < fragment.Length; i++)
                {
                    var matchs = regex.Match(fragment[i]);
                    if (matchs.Success)
                    {
                        string tagName = matchs.Groups[1].Value;
                        string indexValue = matchs.Groups[2].Value;
                        bool isLeafNode = i == fragment.Length - 1;
                        string stableText = isLeafNode && TextStableSelectorTags.Contains(tagName)
                            ? NormalizeSelectorText(elementInnerText)
                            : string.Empty;

                        var props = new List<Prop>
                        {
                            new Prop
                            {
                                name = "@index",
                                value = indexValue,
                                pattern = "equal",
                                accurate = string.IsNullOrEmpty(stableText) ? "true" : "false"
                            }
                        };

                        if (!string.IsNullOrEmpty(stableText))
                        {
                            props.Add(new Prop
                            {
                                name = "innertext",
                                value = stableText,
                                pattern = "contain",
                                accurate = "true"
                            });
                        }

                        selector.Nodes[i + 2] = new Node
                        {
                            name = tagName,
                            type = "web",
                            props = props.ToArray()
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex.StackTrace);
                throw new Exception("生成Selector 失败".I18n("controlType_WdtErpProxy_56E0E8D3"));
            }

            return JsonConvert.SerializeObject(selector.Nodes);
        }

        public void Screenshot(Rectangle rectangle, string picPath)
        {
            var width = rectangle.Size.Width;
            var height = rectangle.Size.Height;
            if (width == 0)
            {
                width = 10;
            }
            if (height == 0)
            {
                height = 10;
            }
            using (Bitmap b = new Bitmap(width, height))
            {
                using (Graphics g = Graphics.FromImage(b))
                {
                    g.CopyFromScreen(rectangle.X, rectangle.Y, 0, 0, new Size(width, height));
                }
                b.Save(picPath);
            }
        }

        /// <summary>
        /// 请求服务端数据
        /// </summary>
        /// <param name="url">接口地址</param>
        /// <param name="postData">请求体</param>
        /// <param name="retry">是否重试</param>
        /// <returns>返回结果</returns>
        public HLResult HttpRequest(string url, string postData, string method = "POST", bool retry = true)
        {
            try
            {
                EnsureServerConnected();
                string responseValue = SendPipeRequest(url, postData, method);
                logger.LogDebug($"response data：{responseValue}");
                if (string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase))
                {
                    return new HLResult { ErrNo = 0, Data = responseValue };
                }

                var token = JToken.Parse(responseValue);
                return JsonConvert.DeserializeObject<HLResult>(token.First.First.ToString());
            }
            catch (Exception we)
            {
                ResetConnectionState($"pipe request failed:{we.Message}");
                if (retry && ConnectServer())
                {
                    return HttpRequest(url, postData, method, false);
                }
                logger.LogError($"pipe connected error,{we.Message}");
                return new HLResult();
            }
        }

        private string SendPipeRequest(string url, string postData, string method, int connectTimeoutMilliseconds = DefaultPipeConnectTimeoutMilliseconds)
        {
            if (string.IsNullOrWhiteSpace(pipeName))
            {
                throw new InvalidOperationException("pipe name has not been initialized.");
            }

            string apiName = NormalizeApiName(url);
            logger.LogDebug($"request pipe：{pipeName}{apiName} post data:{postData}, connectTimeout:{connectTimeoutMilliseconds}ms");
            using (var pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.None))
            {
                pipeClient.Connect(connectTimeoutMilliseconds);

                WritePipeMessage(pipeClient, JsonConvert.SerializeObject(new
                {
                    ApiName = apiName,
                    Body = postData ?? string.Empty,
                    Method = method ?? "POST"
                }));

                string responseText = ReadPipeMessage(pipeClient);
                var response = JsonConvert.DeserializeObject<JObject>(responseText);
                if (response?.Value<bool?>("Success") != true)
                {
                    throw new Exception(response?["Content"]?.ToString() ?? "pipe request failed");
                }

                return response?["Content"]?.ToString() ?? string.Empty;
            }
        }

        private string NormalizeApiName(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return "/";
            }

            return url.StartsWith("/") ? url : $"/{url}";
        }

        private static void WritePipeMessage(Stream stream, string message)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message ?? string.Empty);
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write(buffer.Length);
                writer.Write(buffer);
                writer.Flush();
            }

            stream.Flush();
        }

        private static string ReadPipeMessage(Stream stream)
        {
            using (var reader = new BinaryReader(stream, Encoding.UTF8, true))
            {
                int length = reader.ReadInt32();
                byte[] buffer = reader.ReadBytes(length);
                if (buffer.Length != length)
                {
                    throw new EndOfStreamException("Pipe message was not fully received.");
                }

                return Encoding.UTF8.GetString(buffer);
            }
        }
    }
}
