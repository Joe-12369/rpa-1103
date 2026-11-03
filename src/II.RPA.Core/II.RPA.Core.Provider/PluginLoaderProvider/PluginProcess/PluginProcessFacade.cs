using System;
using System.Diagnostics;
using System.IO;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using II.RPA.Core.Common;
using II.RPA.Core.IPCExtension.Message;
using II.RPA.Core.Log;
using II.RPA.Core.Models;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Entities;
using Newtonsoft.Json;

namespace II.RPA.Core.Provider.PluginLoaderProvider.PluginProcess
{
    public class PluginProcessFacade:IDisposable
    {
        private string m_StartUpPath = ".\\II.RPA.PluginProcess.exe";
        private const string PluginPipeNameFormat = "net.pipe://localhost/SZ_ResiliencePlugin/5F924192070C4EC78E782A90049207DF_{0}";
        private const string PluginProcessName = "II.RPA.PluginProcess";
        private const string PluginProcessStartArgFormat = "-p {0} -c {1}";
        private readonly ILogger m_Logger;
        private ISubscriber broker;
        private object lockRunObj = new object();
        private object lockRestartObj = new object();
        private object lockStatus = new object();
        //private PluginProcessMonitor monitor;
        private volatile ProcessStatus pluginProcessStatus = ProcessStatus.New;
        private int PluginServicesStartTimeout = 60 * 1000;
        private Process process;
        private readonly ManualResetEventSlim m_InitialEvent = new ManualResetEventSlim(false);
        private readonly ManualResetEventSlim m_StartedEvent = new ManualResetEventSlim(false);
        private bool m_IsNeedKeepAlive = false;
        public PluginProcessFacade(ILogger aLogger, ISubscriber broker)
        {
            this.m_Logger = aLogger;
            this.PluginProcess.EnableRaisingEvents = true;
            this.PluginProcess.Exited += Process_Exited;
            this.PluginProcess.ErrorDataReceived += Process_ErrorDataReceived;
            this.PluginProcess.Disposed += Process_Disposed;
            //this.monitor = new PluginProcessMonitor(this, aLogger);
            this.broker = broker;
        }

        /// <summary>
        /// 独立插件进程启动后进程Id
        /// </summary>
        public int CurrentPluginProcessId
        {
            get;
            private set;
        }

        /// <summary>
        /// 独立插件进程
        /// </summary>
        public Process PluginProcess
        {
            get
            {
                if (process == null)
                {
                    if (Global.Client == ClientID.DeskTop)
                    {
                        m_StartUpPath = Path.Combine(Global.RunTimePath, "II.RPA.PluginProcess.exe");
                        m_Logger.LogInfo($"ZDeskTop run path:{m_StartUpPath}");
                    }
                    this.process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = m_StartUpPath,
                            UseShellExecute = false,
                        }
                    };
                }
                return process;
            }
        }

        /// <summary>
        /// 独立进程启动名称
        /// </summary>
        public string PluginProcessSetupPath
        {
            get
            {
                return this.PluginProcess.StartInfo.FileName;
            }
            set
            {
                this.PluginProcess.StartInfo.FileName = value;
            }
        }

        /// <summary>
        /// 独立插件启动参数
        /// </summary>
        public string PluginProcessStartArg
        {
            get
            {
                string pid = Process.GetCurrentProcess().Id.ToString();
                int clientid = 0;
                if (Global.Client == ClientID.Factory)
                {
                    clientid = 1;
                }
                else if (Global.Client == ClientID.Bot)
                {
                    clientid = 2;
                }
                else if (Global.Client == ClientID.DeskTop)
                {
                    clientid = 3;
                }
                return string.Format(PluginProcessStartArgFormat, pid, clientid);
            }
        }

        /// <summary>
        /// 独立进程管道服务地址
        /// </summary>
        public string PluginServicePipeName
        {
            get
            {
                string pid = this.PluginProcess.Id.ToString();
                return string.Format(PluginPipeNameFormat, pid);
            }
        }

        /// <summary>
        /// 独立进程服务状态
        /// </summary>
        public ProcessStatus Status
        {
            get
            {
                lock (lockStatus)
                {
                    return this.pluginProcessStatus;
                }
            }
            private set
            {
                lock (lockStatus)
                {
                    this.pluginProcessStatus = value;
                }
            }
        }
        public bool IsSetToken
        {
            get;
            private set;
        }
        public ProcessStatus EnsureProcessStarted(TimeSpan aTimeSpan)
        {
            if (Status != ProcessStatus.Started)
            {
                if (m_StartedEvent.Wait(aTimeSpan))
                {
                    if (Status != ProcessStatus.Started)
                    {
                        throw new Exception("Process is unavailable now!");
                    }
                    else
                    {
                        return Status;
                    }
                }
                else
                {
                    throw new Exception("Process is unavailable now!");
                }
            }
            else
            {
                return Status;
            }
        }
        /// <summary>
        /// 插件进程启动后，初始化加载包容器
        /// </summary>
        /// <param name="anInitParam">初始化参数</param>
        /// <returns>初始化结果</returns>
        public async Task<string> Init(PluginIPCInitializeRequest anInitParam)
        {
            IsSetToken = false;
            m_InitialEvent.Reset();
            if (anInitParam == null)
            {
                throw new NullReferenceException("参数[anInitParam]不能空!");
            }

            ProcessStatus oldStatus = this.Status;

            try
            {
                if (this.Status == ProcessStatus.New || this.Status == ProcessStatus.Dispose)
                {
                    throw new Exception("插件进程没有启动!");
                }
                else if (this.Status == ProcessStatus.Initialize)
                {
                    throw new Exception("插件进程正在初始化，无法执行重复请求!");
                }
                else if (this.Status == ProcessStatus.Ready)
                {
                    //throw new InvalidOperationException("插件进程已经初始化，无效请求!");
                    m_Logger.LogWarning("Duplicate init request!");
                }
                this.Status = ProcessStatus.Initialize;
                var jData = JsonConvert.SerializeObject(anInitParam);
                var ret = await IPCExtension.IPCClientASync.Send(this.PluginServicePipeName, "Initialize", jData);
                if (ret == null)
                {
                    throw new NullReferenceException("初始化执行返回结果不能空!");
                }
                m_InitialEvent.Set();
                this.Status = ProcessStatus.Ready;
                return ret.Result;
            }
            catch
            {
                this.Status = oldStatus;
                throw;
            }
            
        }
        public bool WaitInit(int aWaitms)
        {
            if (Status != ProcessStatus.Ready)
            {
                return m_InitialEvent.Wait(aWaitms);
            }
            else
            {
                return true;
            }
        }
        public string Run(PluginIPCRequest anRunParam)
        {
            if (anRunParam == null)
            {
                throw new NullReferenceException("参数[anRunParam]不能空!");
            }
            if (this.Status != ProcessStatus.Ready)
            {
                throw new EndpointNotFoundException($"插件进程[状态:{this.Status}]没有启动或者初始化!");
            }
            var jData = JsonConvert.SerializeObject(anRunParam);
            var ret = IPCExtension.IPCClient.Send(this.PluginServicePipeName, "Run", jData);
            if (ret == null)
            {
                throw new NullReferenceException("运行执行返回结果不能空!");
            }
            return ret.Result;
        }
        public async Task<string> RunAsync(PluginIPCRequest anRunParam)
        {
            return await Task.Run(async () => 
            {
                if (anRunParam == null)
                {
                    throw new NullReferenceException("参数[anRunParam]不能空!");
                }
                if (this.Status != ProcessStatus.Ready)
                {
                    throw new EndpointNotFoundException($"插件进程[状态:{this.Status.ToString()}]没有启动或者初始化!");
                }
                var jData = JsonConvert.SerializeObject(anRunParam);
                var ret = await IPCExtension.IPCClientASync.Send(this.PluginServicePipeName, "MTARun", jData);
                if (ret == null)
                {
                    throw new NullReferenceException("运行执行返回结果不能空!");
                }
                return ret.Result;
            });
        }

        public async Task<string> ShareCommonValue(PluginIPCShareValueRequest anRunParam)
        {
            try
            {
                var jData = JsonConvert.SerializeObject(anRunParam);
                var ret = await IPCExtension.IPCClientASync.Send(this.PluginServicePipeName, "ShareValue", jData);
                IsSetToken = true;
                return ret.Result;
            }
            catch(Exception e)
            {
                m_Logger.LogError($"Fail to execute ShareCommonValue for {e.Message}");
                IsSetToken = false;
                throw e;
            }
        }

        /// <summary>
        /// 使用指定参数启动插件独立进程
        /// </summary>
        /// <param name="aStartParam">启动进程参数</param>
        /// <param name="enableMonitor">是否启动保活进程，如果为true启动，为false不启动</param>
        /// <returns>是否启动成功</returns>
        public bool Start(string aStartParam, bool enableMonitor = true)
        {
            lock (lockRunObj)
            {
                this.PluginProcess.StartInfo.Arguments = aStartParam;
                this.PluginProcess.EnableRaisingEvents = true;
                m_Logger.LogInfo($"process start args: {aStartParam}");
                bool bootRet = false;
                try
                {
                    using (Subject subscriber = new Subject("PluginServicesStatus", this.broker))
                    {
                        bootRet = this.PluginProcess.Start();
                        m_Logger.LogInfo($"启动pluginprocess成功:{PluginProcess.Id}");
                        this.Status = ProcessStatus.Started;
                        subscriber.Pending(this.PluginServicesStartTimeout);
                        string value = subscriber.Value;
                        var statusMsg = JsonConvert.DeserializeObject<IPCStatusMessage>(value);
                        bootRet = (bootRet && (statusMsg.Status == 1));
                    }
                }
                catch(Exception ex)
                {
                    m_Logger.LogError($"启动pluginprocess失败:{PluginProcess.StartInfo.FileName}", ex);
                }
                //启动成功
                if (bootRet)
                {
                    m_Logger.LogInfo($"Pluginprocess IPC服务启动成功!");
                    m_StartedEvent.Set();
                    this.CurrentPluginProcessId = this.PluginProcess.Id;
                    this.Status = ProcessStatus.Started;
                    m_IsNeedKeepAlive = true;
                }
                //启动失败
                else
                {
                    this.Status = ProcessStatus.New;
                }
                return bootRet;
            }
        }

        /// <summary>
        /// 停止插件独立进程，同时会关闭守护线程
        /// </summary>
        public void StopProcess()
        {
            m_IsNeedKeepAlive = false;
            lock (lockRunObj)
            {
                m_StartedEvent.Reset();
                bool isAllExit = KillProcess(2000);
                m_Logger.LogInfo($"KillProcess:{CurrentPluginProcessId}");
                Status = ProcessStatus.Stop;
            }
        }
        private bool KillProcess(int waitTimeout)
        {
            try
            {
                if (PluginProcess != null)
                {
                    PluginProcess.PostQuitGuiThread();
                    return PluginProcess.WaitForExit(waitTimeout);
                }
            }
            catch(Exception ex)
            {
                m_Logger.LogError(ex);
            }
            return false;
        }

        /// <summary>
        /// 使用指定进程Id获取进程，如果找不到会抛出异常
        /// </summary>
        /// <param name="pid">进程id</param>
        /// <returns>找到的进程</returns>
        internal Process GetProcessByPid(int pid)
        {
            var processes = Process.GetProcessById(pid);
            return processes;
        }

        /// <summary>
        /// 重启服务
        /// </summary>
        internal void Restart()
        {
            m_IsNeedKeepAlive = false;
            lock (this.lockRunObj)
            {
                m_StartedEvent.Reset();
                bool isAllExit = KillProcess(2000);
                this.Start(this.PluginProcessStartArg);
            }
        }
        internal void Close()
        {
            m_IsNeedKeepAlive = false;
            this.StopProcess();
        }
        internal void TimeOutRestart()
        {
            m_IsNeedKeepAlive = false;
            m_Logger.LogDebug($"Wait lock: {nameof(lockRestartObj)}");
            lock (this.lockRestartObj)
            {
                m_StartedEvent.Reset();
                bool isAllExit = KillProcess(2000);
                m_Logger.LogDebug($"KillProcess done!");
                Start($"{PluginProcessStartArg} -r true");
                m_Logger.LogDebug($"ReStart done!");
            }
        }

        private void Process_Disposed(object sender, EventArgs e)
        {
            m_Logger.LogInfo($"[{Status} {CurrentPluginProcessId}]:Disposed");
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            m_Logger.LogInfo($"[{Status} {PluginProcess.Id} {PluginProcess.ProcessName}]:ErrorDataReceived {e.Data}");
        }

        private void Process_Exited(object sender, EventArgs e)
        {
            int pid = -1;
            if (sender != null)
            {
                try
                {
                    if (sender is Process p)
                    {
                        pid = p.Id;
                        m_Logger.LogWarning($"PluginProcess Exited:{pid}; {p.StartTime}-{p.ExitTime}; {p.ExitCode}");
                    }
                }
                catch (InvalidOperationException ie)
                {
                    m_Logger.LogFatal(ie);
                }
            }
            if (Status != ProcessStatus.Stop 
                && Status != ProcessStatus.Dispose
                && Status != ProcessStatus.New
                && m_IsNeedKeepAlive)
            {
                if (pid == CurrentPluginProcessId && pid != -1)
                {
                    m_Logger.LogWarning($"PluginProcess may crash.Restart:{CurrentPluginProcessId}");
                    Start($"{PluginProcessStartArg} -r true");
                }
                else
                {
                    m_Logger.LogWarning($"Old PluginProcess exited: oldpid {pid}; Current {CurrentPluginProcessId}");
                }
            }
        }

        public void Dispose()
        {
            PluginProcess.Exited -= Process_Exited;
            PluginProcess?.Dispose();
            this.Status = ProcessStatus.Dispose;
        }
    }
}