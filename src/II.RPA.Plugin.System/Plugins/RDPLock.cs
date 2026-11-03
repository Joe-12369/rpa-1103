using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.RPASystem.Params;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading;
using System.IO;
using Cassia;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace II.RPA.Plugin.RPASystem.Plugins
{
    public class RDPLock : SystemComponentBase<ParamRDPLockModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.RDPLock;

        protected override void ValidateParameter(ParamRDPLockModel paramModel)
        {
            //try
            //{
                if (string.IsNullOrEmpty(paramModel.UserName) || string.IsNullOrEmpty(paramModel.Pwd))
                {
                    throw new RPAException(eErrorMessageType.Error_ScreenLock_AccountOrPwd);
                }
            //}
            //catch (Exception ex)
            //{
            //    Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"参数格式不符合要求：{jsonParam}");
            //    return false;
            //}
        }

        protected override JMessage Execute(ParamRDPLockModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            var cts = new CancellationTokenSource(30 * 1000);
            var msg = new JMessage();

            var userName = paramModel.UserName;
            var password = paramModel.Pwd;
            int port = 3389;
            var rdpExeFile = "wfreerdp.exe";
            var processName = Path.GetFileNameWithoutExtension(rdpExeFile);
            var dir = AppDomain.CurrentDomain.BaseDirectory;

            if (paramModel.Port != null)
            {
                port = paramModel.Port.Value;
            }
            else if (TryGetSystemRdpPort(out var autoPort))
            {
                port = autoPort;
            }

            var hasRdpSession = CheckHasRdpLockSession(
                paramModel.UserName,
                processName,
                dir,
                cts.Token,
                out var initSession);

            if (hasRdpSession)
            {
                logger.LogInformation($"当前有一个锁屏会话{initSession.WindowStationName}正在生效，跳过锁屏");
                return msg;
            }
            else if (initSession == null)
            {
                logger.LogWarning($"无法读取当前用户{userName}的会话信息，可能是偶发问题，请重试");
                Exception(null, eErrorMessageType.Error_RDPLock, ref msg);
                return msg;
            }
            else
            {
                logger.LogInformation($"执行锁屏的初始会话:{initSession.WindowStationName}");
                CleanupRdpClient(processName);
            }

            long successPid = 0;

            try
            {
                var screenInfo = ScreenInfo.Get();
                var scaleFactor = paramModel.ScaleFactor ?? screenInfo.ScalingFactor;
                var width = paramModel.Width ?? screenInfo.Width;
                var height = paramModel.Height ?? screenInfo.Height;

                var startInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(dir, rdpExeFile),
                    WorkingDirectory = dir,
                    Arguments = $"/u:{userName} /p:{password} /disable-output /v:127.0.0.1:{port} /h:{height} /w:{width} /cert:ignore /scale:100 /scale-desktop:{scaleFactor} +offscreen-cache -clipboard -decorations +auto-reconnect",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                logger.LogInformation($"执行锁屏操作，使用目录 {startInfo.WorkingDirectory}，使用屏幕 {screenInfo}，使用端口{port}");

                var process = Process.Start(startInfo);

                Thread.Sleep(4 * 1000);

                if (process.HasExited)
                {
                    logger.LogWarning($"锁屏进程退出过早，需要检查是否支持RDP，是否使用了正确的用户名密码");
                    Exception(null, eErrorMessageType.Error_RDPLock_RdpError, ref msg);
                    return msg;
                }

                successPid = process.Id;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "调用锁屏组件期间发生了错误");
                Exception(ex, eErrorMessageType.Error_RDPLock, ref msg);
            }

            var sessionChanged = this.CancellableExecute(() =>
            {
                var changedSession = GetCurrentSession(userName);

                if (changedSession == null ||
                    !changedSession.WindowStationName.Contains("RDP") ||
                    changedSession.Equals(initSession))
                {
                    logger.LogInformation($"锁屏拉起后，会话没有发生变化，继续等待");
                    Thread.Sleep(1000);
                    throw new Exception("continue");
                }

                logger.LogInformation($"锁屏执行后的会话:{changedSession.WindowStationName}");
                return true;
            }, cts.Token);

            if (!sessionChanged)
            {
                logger.LogWarning($"锁屏拉起后，当前会话没有发生变化，且超出了等待时间");
                Exception(null, eErrorMessageType.Error_RDPLock_RdpError, ref msg);
            }

            SavePid(dir, successPid);

            return msg;
        }

        private ITerminalServicesSession GetCurrentSession(string userName)
        {
            ITerminalServicesManager manager = new TerminalServicesManager();

            using (ITerminalServer server = manager.GetLocalServer())
            {
                server.Open();

                var sessions = server.GetSessions().ToList();

                var session = sessions
                    .Where(x => x.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault();

                server.Close();

                return session;
            }
        }

        /// <summary>
        /// 一个简易的循环执行（支持超时）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="coreAction"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private T CancellableExecute<T>(
            Func<T> coreAction,
            CancellationToken token)
        {
            var task = Task<T>.Factory.StartNew(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        return coreAction();
                    }
                    catch
                    {
                    }
                }

                return default(T);
            }, TaskCreationOptions.LongRunning);

            try
            {
                task.Wait(token);
                return task.Result;
            }
            catch
            {
                return default(T);
            }
        }

        private void SavePid(string dir, long pid)
        {
            try
            {
                File.WriteAllText(Path.Combine(dir, "PID.txt"), pid.ToString());
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"保存PID失败");
            }
        }

        private long ReadLastPid(string dir)
        {
            try
            {
                var txt = File.ReadAllText(Path.Combine(dir, "PID.txt"));

                if (long.TryParse(txt, out var pid))
                {
                    return pid;
                }
                return 0;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"读取PID失败");
                return 0;
            }
        }

        private void CleanupRdpClient(string processName)
        {
            try
            {
                var processes = Process.GetProcessesByName(processName);

                foreach (var process in processes)
                {
                    logger.LogInformation($"清理残余的RDP客户端{process.ProcessName} # {process.Id}");
                    process.Kill();
                }
            }
            catch
            {
            }
        }

        private bool TryGetSystemRdpPort(out int port)
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Terminal Server\\WinStations\\RDP-Tcp"))
                {
                    var textPort = key.GetValue("PortNumber")?.ToString();
                    return int.TryParse(textPort, out port);
                }
            }
            catch
            {
                port = -1;
                return false;
            }

            //var tsInfo = ReadAfterExecuteCommand("tasklist /svc | findstr TermService");
            //var pid = tsInfo
            //    .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
            //    .Select(x => int.TryParse(x, out var ix) ? ix : -1)
            //    .Where(x => x > 0)
            //    .FirstOrDefault();

            //var pInfo = ReadAfterExecuteCommand($"netstat /ano | findstr {pid}");

            //var pLine = pInfo.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            //    .Select(x =>
            //        x.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries)
            //          .Skip(1)
            //          .FirstOrDefault())
            //    .FirstOrDefault(x => Regex.IsMatch(x, "\\:[0-9]+"));

            //var part = pLine.Split(':').LastOrDefault();

            //return int.TryParse(part, out port);
        }

        private bool CheckHasRdpLockSession(
            string userName,
            string processName,
            string pidDir,
            CancellationToken token,
            out ITerminalServicesSession currentSession)
        {
            currentSession = this.CancellableExecute(
                () => GetCurrentSession(userName),
                token);

            if (currentSession == null ||
                !currentSession.WindowStationName.Contains("RDP"))
            {
                return false;
            }

            try
            {
                var pid = ReadLastPid(pidDir);
                var process = Process.GetProcessById((int)pid);
                return process.ProcessName.Contains(processName);
            }
            catch
            {
                return false;
            }
        }

        ///// <summary>
        ///// 执行cmd，并获取返回值
        ///// </summary>
        ///// <param name="fileName"></param>
        ///// <param name="arguments"></param>
        ///// <returns></returns>
        //private static string ReadAfterExecuteCommand(string cmd)
        //{
        //    cmd = cmd.TrimEnd('&') + "&";
        //    var cmdExe = Path.Combine(
        //        Environment.GetFolderPath(Environment.SpecialFolder.SystemX86),
        //        "cmd.exe");

        //    var process = new Process
        //    {
        //        StartInfo = new ProcessStartInfo
        //        {
        //            FileName = cmdExe,
        //            Arguments = cmd,
        //            RedirectStandardOutput = true,
        //            RedirectStandardInput = true,
        //            CreateNoWindow = true,
        //            UseShellExecute = false,
        //        },
        //    };

        //    var sb = new StringBuilder();

        //    process.OutputDataReceived += (s, e) =>
        //    {
        //        sb.AppendLine(e.Data);
        //    };

        //    process.Start();
        //    process.BeginOutputReadLine();
        //    process.StandardInput.WriteLine(cmd);

        //    Task.Factory.StartNew(
        //        () => process.WaitForExit(),
        //         TaskCreationOptions.LongRunning).Wait(2000);


        //    return sb.ToString();
        //}
    }
}

