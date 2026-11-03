using II.RPA.Core.Abstract;
using II.RPA.Core.Common;
using II.RPA.Core.Log;
using II.RPA.Core.Provider.PluginLoaderProvider.Command;
using II.RPA.Core.Server.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace II.RPA.Core.Server
{
    public class RPACoreServerBootContext : ApplicationContext, IServerContext
    {
        private readonly IEnumerable<IServiceBoot> m_ServiceBoots;
        private readonly ILogger m_Logger;
        private readonly IEndPointProvider m_EndPointProvider;
        private readonly CommandConsumer m_CommandConsumer;
        private readonly Form m_UiThreadExitMarshaller;
        private string m_ClientId;
        public RPACoreServerBootContext(IEnumerable<IServiceBoot> aServiceBoots,
            ILogger aLogger,
            IEndPointProvider anEndPointProvider,
            CommandConsumer aComsumer) : base()
        {
            m_UiThreadExitMarshaller = new Form
            {
                ShowInTaskbar = false,
                FormBorderStyle = FormBorderStyle.None,
                Size = new System.Drawing.Size(0, 0),
                StartPosition = FormStartPosition.Manual,
                Location = new System.Drawing.Point(-32000, -32000)
            };
            m_UiThreadExitMarshaller.CreateControl();

            m_Logger = aLogger;

            m_CommandConsumer = aComsumer;
            m_CommandConsumer.BackEvent += CommandConsumer_BackEvent;
            m_ClientId = CommandArgHelper.GetArgumentValue(CommonConstants.CLIENTID.KEY);
            if (!string.IsNullOrEmpty(m_ClientId) && CommonConstants.CLIENTID.VALUES.Contains(m_ClientId))
            {
                m_Logger.LogInfo($"Current Client: {m_ClientId}");
            }
            else
            {
                m_Logger.LogWarning($"Unexpected Client：{m_ClientId}");
            }
            m_EndPointProvider = anEndPointProvider;
            m_Logger.LogInfo("RPACoreServerBootContext is starting...");
            m_ServiceBoots = aServiceBoots;
            if (m_ClientId == CommonConstants.VALUE_ZDESKTOP)
            {
                // Enrol before services can launch processes. Descendants inherit this job;
                // closing the last job handle on core exit also terminates them.
                using (var currentProcess = Process.GetCurrentProcess())
                    II.RPA.Core.Common.Helpers.ChildProcessJob.AddChildProcess(currentProcess);
                StartOwnerProcessWatcher();
            }
            m_EndPointProvider.Initial(m_ServiceBoots);
            Run(m_ServiceBoots);
            m_Logger.LogInfo($"All ServiceBoots have been started.");
            m_Logger.LogInfo($"{Environment.NewLine}===============================Core Server Startup End==================================");

            #region 操作系统事件监听
            Microsoft.Win32.SystemEvents.SessionEnding += SystemEvents_SessionEnding;
            System.Net.NetworkInformation.NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;
            Microsoft.Win32.SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
            Microsoft.Win32.SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
            #endregion

            if (m_ClientId == CommonConstants.VALUE_BOT || m_ClientId == CommonConstants.VALUE_FACTORY)
            {
                var watcher = Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        await Task.Delay(1000).ConfigureAwait(false);
                        var processId = Global.ClientProcessId;
                        if (processId == -1)
                        {
                            m_Logger.LogInfo($"Can not get process Id.Server will be shutdown.");
                            Stop(m_ServiceBoots);
                            await ShutDown(100).ConfigureAwait(false);
                            return;
                        }
                        while (true)
                        {
                            await Task.Delay(1000).ConfigureAwait(false);
                            var process = Process.GetProcesses().FirstOrDefault(p => p.Id == processId);
                            if (process == null)
                            {
                                m_Logger.LogInfo($"There is no {m_ClientId}-{processId} process left.Server will be shutdown.");
                                Stop(m_ServiceBoots);
                                await Task.Delay(1000).ConfigureAwait(false);
                                await ShutDown(100).ConfigureAwait(false);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        m_Logger.LogError($"Client process watcher failed: {ex}");
                    }
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
                _ = watcher.ContinueWith(t =>
                {
                    if (t.IsFaulted)
                        m_Logger.LogError($"Client process watcher faulted: {t.Exception}");
                }, TaskContinuationOptions.OnlyOnFaulted);
            }

            m_Logger.LogDebug($"Start Message Loop");
        }

        private void StartOwnerProcessWatcher()
        {
            var ownerIdText = CommandArgHelper.GetArgumentValue("-zdesktop-owner-pid");
            var ownerStartText = CommandArgHelper.GetArgumentValue("-zdesktop-owner-start");
            _ = Task.Run(async () =>
            {
                Process owner = null;
                var startup = Stopwatch.StartNew();
                try
                {
                    while (true)
                    {
                        try
                        {
                            if (owner == null)
                            {
                                int ownerId;
                                long ownerStart;
                                if (!string.IsNullOrEmpty(ownerIdText))
                                {
                                    if (!int.TryParse(ownerIdText, out ownerId) ||
                                        !long.TryParse(ownerStartText, out ownerStart))
                                        break;
                                }
                                else
                                {
                                    // Compatibility with tasks registered by older windows.
                                    var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                        "Z-Pid", "Z-Desktop.txt");
                                    if (!int.TryParse(File.ReadAllText(path), out ownerId))
                                    {
                                        if (startup.Elapsed >= TimeSpan.FromSeconds(15))
                                            break;
                                        await Task.Delay(1000).ConfigureAwait(false);
                                        continue;
                                    }
                                    ownerStart = 0;
                                }

                                var candidate = Process.GetProcessById(ownerId);
                                try
                                {
                                    if (!candidate.ProcessName.Equals("II.RPA.ZDeskTop", StringComparison.OrdinalIgnoreCase) ||
                                        (ownerStart != 0 && candidate.StartTime.ToUniversalTime().Ticks != ownerStart))
                                        break;
                                    // Retain this process handle; never adopt a restarted window.
                                    _ = candidate.Handle;
                                    owner = candidate;
                                    candidate = null;
                                    m_Logger.LogInfo($"Watching ZDeskTop owner process {ownerId}.");
                                }
                                finally
                                {
                                    candidate?.Dispose();
                                }
                            }

                            if (owner.HasExited)
                                break;
                        }
                        catch (ArgumentException)
                        {
                            // A valid PID whose process no longer exists.
                            break;
                        }
                        catch (FileNotFoundException)
                        {
                            if (startup.Elapsed >= TimeSpan.FromSeconds(15))
                                break;
                        }
                        catch (DirectoryNotFoundException)
                        {
                            if (startup.Elapsed >= TimeSpan.FromSeconds(15))
                                break;
                        }
                        catch (Exception ex)
                        {
                            // Access and I/O errors do not prove the owner has exited.
                            m_Logger.LogDebug($"ZDeskTop owner probe failed, retrying: {ex.Message}");
                        }
                        await Task.Delay(1000).ConfigureAwait(false);
                    }
                }
                finally
                {
                    // Avoid global process-name cleanup here: the job owns this core's
                    // descendants and releases them even if logging or cleanup fails.
                    try
                    {
                        owner?.Dispose();
                        m_Logger.LogInfo("ZDeskTop owner has exited; terminating child core and its process job.");
                    }
                    finally
                    {
                        Environment.Exit(0);
                    }
                }
            });
        }

        private void CommandConsumer_BackEvent(object sender, CommandEventAgr e)
        {
            m_Logger.LogDebug($"CommandConsumer_BackEvent");
            //IDispatchCommand command = e.Value;
        }

        private void SystemEvents_SessionSwitch(object sender, Microsoft.Win32.SessionSwitchEventArgs e)
        {
            m_Logger.LogInfo($"SystemEvent:SessionSwitch because of {e.Reason}");
        }

        private void SystemEvents_PowerModeChanged(object sender, Microsoft.Win32.PowerModeChangedEventArgs e)
        {
            m_Logger.LogInfo($"SystemEvent:PowerMode is {e.Mode}");
        }

        private void NetworkChange_NetworkAvailabilityChanged(object sender, System.Net.NetworkInformation.NetworkAvailabilityEventArgs e)
        {
            m_Logger.LogInfo($"SystemEvent:NetworkAvailability is {e.IsAvailable}");
        }

        private async void SystemEvents_SessionEnding(object sender, Microsoft.Win32.SessionEndingEventArgs e)
        {
            var sessionId = Process.GetCurrentProcess().SessionId;
            m_Logger.LogInfo($"SystemEvent:Windows Session {sessionId} is ending");
            await ShutDown(100, $"Windows Session is Ended");
        }

        [STAThread]
        public void Run(IEnumerable<IServiceBoot> aServiceBoots)
        {
            var startTasks = aServiceBoots.Select(async boot =>
            {
                if (boot is IAdminServiceBoot adminBoot)
                {
                    adminBoot.AuthToManageServer(this);
                }

                await boot.StartAsync().ConfigureAwait(false);
                UpdatePortFile($"{boot.Port}");
                m_Logger.LogInfo($"{boot.BootName}:{boot.Port} has been started");
            });
            Task.WhenAll(startTasks).GetAwaiter().GetResult();
        }
        public void Stop(IEnumerable<IServiceBoot> aServiceBoots)
        {
            if (aServiceBoots != null && aServiceBoots.Any())
            {
                foreach (var aBoot in aServiceBoots)
                {
                    m_Logger.LogInfo($"Stop {aBoot.BootName}:{aBoot.Port}");
                    aBoot.StopServerOnly();
                    m_Logger.LogInfo($"{aBoot.BootName}:{aBoot.Port} has been stopped");
                }
            }
        }
        private void UpdatePortFile(string aPort)
        {
            lock (this)
            {
                var fileName = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\Z-Core\\{Global.ClientIDFormatString}.txt";
                if (!Directory.Exists(Path.GetDirectoryName(fileName)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                }
                File.WriteAllText(fileName, aPort, System.Text.Encoding.Default);
            }
        }
        public void KillAdb()
        {
            try
            {
                string facExeName = "实在设计器";
                string botExeName = "Z-Bot";
                #region 取出客户端的进程名
                DirectoryInfo dir = new DirectoryInfo(AppContext.BaseDirectory);
                while (dir != null && !dir.Name.Equals("rpa-engine", StringComparison.OrdinalIgnoreCase))
                {
                    dir = dir.Parent;
                }
                var enginePath = dir?.FullName;
                m_Logger.LogDebug($"engine目录：{enginePath}");
                if (enginePath != null)
                {
                    var exeTxtFilePath = Path.Combine(enginePath, "config", "client_exe_name.json");
                    if (File.Exists(exeTxtFilePath))
                    {
                        var config = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(exeTxtFilePath));
                        facExeName = config["Z_FACTORY_EXE"]?.Replace(".exe", "");
                        botExeName = config["Z_BOT_EXE"]?.Replace(".exe", "");
                    }
                }
                #endregion
                bool HasOtherProcess(string name) => Process.GetProcessesByName(name).Any();
                bool shouldRun = (Global.Client == ClientID.Factory && !HasOtherProcess(botExeName)) ||
                                 (Global.Client == ClientID.Bot && !HasOtherProcess(facExeName));
                if (!shouldRun) return;
                string targetAdbPath = Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "adb", "adb.exe"));
                string normalizedTarget = Path.GetFullPath(targetAdbPath).ToLower();
                Process[] adbProcesses = Process.GetProcessesByName("adb");
                foreach (Process pro in adbProcesses)
                {
                    string procPath = pro.MainModule.FileName;
                    string normalizedProc = Path.GetFullPath(procPath).ToLower();
                    if (normalizedProc == normalizedTarget)
                    {
                        m_Logger.LogInfo($"正在杀掉目标adb进程: {procPath} (ID: {pro.Id})");
                        pro.Kill();
                        pro.WaitForExit(2000); // 等待确认退出
                    }
                }
            }
            catch (Exception ex)
            {
                m_Logger.LogError($"处理adb进程时出错: {ex.Message}");
            }
        }
        public async Task ShutDown(int aDelayMilliSeconds, string aMessageAdditional = null)
        {
            #region 进程退出后EventHook进程一并退出
            Process[] ps = Process.GetProcessesByName("II.RPA.SetWinEventHook");
            if (ps.Length > 0)
            {
                m_Logger.LogInfo($"结束进程：II.RPA.SetWinEventHook");
                foreach (Process p in ps)
                    p.Kill();
            }
            #endregion
            #region 处理adb进程
            KillAdb();
            #endregion
            KillEventMonitorIfNeeded();
            m_Logger.LogInfo($"Server将在 {aDelayMilliSeconds} ms 内结束。附加消息 {aMessageAdditional}");
            await Task.Delay(aDelayMilliSeconds);
            m_Logger.LogInfo($"{Environment.NewLine}===============================Core Server Is Terminated==================================");
            m_Logger.LogInfo($"Application.MessageLoop:{Application.MessageLoop}");
            try
            {
                if (m_UiThreadExitMarshaller.IsHandleCreated && m_UiThreadExitMarshaller.InvokeRequired)
                    m_UiThreadExitMarshaller.Invoke(new Action(Application.Exit));
                else
                    Application.Exit();
            }
            catch (Exception ex)
            {
                m_Logger.LogError($"Application.Exit 失败: {ex}");
                Environment.Exit(1);
                return;
            }
            m_Logger.LogInfo($"结束进程失败,有正在运行的前台线程");
            var ts = Process.GetCurrentProcess().Threads;
            m_Logger.LogInfo($"结束进程失败,当前线程信息:{Environment.NewLine}{string.Join(";", ts.Cast<ProcessThread>().Select(s => s.Id.ToString() + " " + s.ThreadState.ToString()).ToArray())}");
            Environment.Exit(1);
        }

        private void KillEventMonitorIfNeeded()
        {
            var othersCoreServerProcess = GetSameSessionProcessesWithSameName();
            if (othersCoreServerProcess.Count == 0)
            {
                Process[] adbProcesses = Process.GetProcessesByName("II.RPA.EventMonitor");
                foreach (Process pro in adbProcesses)
                {
                    string procPath = pro.MainModule.FileName;
                    string normalizedProc = Path.GetFullPath(procPath).ToLower();
                    m_Logger.LogInfo($"正在杀掉目标II.RPA.EventMonitor进程: {procPath} (ID: {pro.Id})");
                    pro.Kill();
                    pro.WaitForExit(2000); // 等待确认退出
                }
            }
        }

        public List<Process> GetSameSessionProcessesWithSameName()
        {
            var currentProcess = Process.GetCurrentProcess();
            string currentProcessName = currentProcess.ProcessName;
            int currentSessionId = currentProcess.SessionId;
            int currentProcessId = currentProcess.Id;

            // 获取所有同名进程
            var allSameNameProcesses = Process.GetProcessesByName(currentProcessName);

            // 过滤：同一会话且不是当前进程
            return allSameNameProcesses
                .Where(p => p.SessionId == currentSessionId && p.Id != currentProcessId)
                .ToList();
        }
    }
}
