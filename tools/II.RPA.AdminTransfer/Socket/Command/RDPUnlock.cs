using Cassia;
using II.RPA.AdminTransfer;
using II.RPA.SupportService.Sockets.Entitys;
using II.RPA.WinAutomation.CommonUtility;
using SuperSocket.SocketBase.Command;
using SuperSocket.SocketBase.Protocol;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Logging.Formatting;

namespace II.RPA.EventMonitor.Comm.Command
{
    public class RDPUnlock : CommandBase<TelnetSession, StringRequestInfo>
    {
        private static ILogger logger = Logger.OfContext();

        public override void ExecuteCommand(TelnetSession session, StringRequestInfo requestInfo)
        {
            JMessage msg = new JMessage();
            session.Logger.Debug($"Key:{requestInfo.Key},Body:{requestInfo.Body}");
            RDPLockParams param = null;
            try
            {
                param = requestInfo.Body.ToObject<RDPLockParams>();
                if (param == null)
                {
                    msg.status = (int)MessageStatusType.FAIL_STATUS;
                    msg.message = eCommonMessageType.RDPLock_cs_XuYaoTiGongYouXiaoDeCanShu.GetMessage();
                    session.TrySend(msg);
                    return;
                }
                if (string.IsNullOrEmpty(param.UserName) || string.IsNullOrEmpty(param.Password))
                {
                    var exInfo = new RPAException(eErrorMessageType.Error_ScreenLock_AccountOrPwd, eComponentType.RDPLock);
                    msg.status = (int)MessageStatusType.FAIL_STATUS;
                    msg.message = exInfo.ErrorMessage;
                    session.TrySend(msg);
                    return;
                }
                //业务逻辑
                msg = RDPUnlockAction(param);
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                var rpaException = new RPAException(eErrorMessageType.Error_RDPUnlockError, eComponentType.RDPUnlock, ex);
                msg.status = (int)MessageStatusType.FAIL_STATUS;
                msg.message = rpaException.ErrorMessage;
            }
            session.TrySend(msg);
        }
        public JMessage RDPUnlockAction(RDPLockParams param)
        {
            logger.LogInformation("Enter RDPUnlock");
            JMessage msg = new JMessage();
            try
            {
                ITerminalServicesManager manager = new TerminalServicesManager();
                bool matchSession = false;
                string tsconFileName = "";
                //判断系统tscon是否存在
                string tsconPath = $@"{ Environment.GetFolderPath(Environment.SpecialFolder.Windows)}\Sysnative\tscon.exe";
                if (System.IO.File.Exists(tsconPath))
                {
                    logger.LogInformation("系统存在tscon.exe");
                    tsconFileName = tsconPath;
                }
                else
                {
                    tsconFileName = "tscon";
                    bool IsWin7 = Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor == 1;
                    if (IsWin7)
                    {
                        tsconFileName = "tscon5";
                    }
                }
                logger.LogInformation($"tsconFileName： {tsconFileName}");
                using (ITerminalServer server = manager.GetLocalServer())
                {
                    server.Open();
                    int _activeSessionID = -1;

                    foreach (ITerminalServicesSession session in server.GetSessions())
                    {
                        logger.LogInformation($"【会话信息】session.WindowStationName：{session.WindowStationName}，session.UserName：{session.UserName}，SessionId:{session.SessionId}");
                        if (session.ConnectionState == Cassia.ConnectionState.Active)
                        {
                            _activeSessionID = session.SessionId;
                        }
                        if (session.WindowStationName.Contains("#") && session.UserName == param.UserName)//
                        {
                            string name = session.WindowStationName;
                            int sId = session.SessionId;
                            string cmdText = string.Format("{0} {1} /dest:console  /PASSWORD:{2}", tsconFileName, sId, param.Password);
                            ExecuteCmd(cmdText, out string error);
                            if (string.IsNullOrEmpty(error))
                            {
                                matchSession = true;
                            }
                            logger.LogInformation($"cmd  excuted");
                            break;
                        }
                    }

                    if (matchSession == false && _activeSessionID > 0)
                    {
                        logger.LogInformation($"通过会话名和用户名，未匹配到合适的会话，直接尝试使用发现的活动会话");
                        string cmdText = string.Format("{0} {1} /dest:console /PASSWORD:{2}", tsconFileName, _activeSessionID, param.Password);
                        ExecuteCmd(cmdText, out string error);
                        if (string.IsNullOrEmpty(error))
                        {
                            matchSession = true;
                        }
                    }
                    if (matchSession == false && MainForm.StartUPSessionID > 0)
                    {
                        logger.LogInformation($"通过会话名和用户名，未匹配到合适的会话，直接尝试使用初始回话");
                        string cmdText = string.Format("{0} {1} /dest:console /PASSWORD:{2}", tsconFileName, MainForm.StartUPSessionID, param.Password);
                        ExecuteCmd(cmdText, out string error);
                        if (string.IsNullOrEmpty(error))
                        {
                            matchSession = true;
                        }
                    }
                    if (matchSession == false)
                    {
                        msg.status = (int)MessageStatusType.FAIL_STATUS;
                        msg.message = eCommonMessageType.RDPUnlock_cs_PingMuJieSuoShiBaiWeiPiPei.GetMessage(); 
                    }
                    else
                    {
                        msg.status = (int)MessageStatusType.SUCCESS_STATUS;
                        msg.message = eCommonMessageType.RDPUnlock_cs_PingMuJieSuoChengGong.GetMessage();
                    }
                    logger.LogInformation(LogBlock.Object(msg));
                }
            }
            catch (Exception ex)
            {
                msg.status = (int)MessageStatusType.FAIL_STATUS;
                msg.message = eCommonMessageType.RDPUnlock_cs_PingMuJieSuoGuoChengZhongFaSheng.GetMessage();
                logger.LogError(ex);
            }
            return msg;
        }
        private static string ExecuteCmd(string cmd, out string error)
        {
            string output = "";
            error = "";
            Process process = new Process();     //创建进程对象
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.LoadUserProfile = true;
            startInfo.FileName = "cmd.exe";      //设定需要执行的命令
            startInfo.Arguments = "/C " + cmd;   //设定参数，其中的“/C”表示执行完命令后马上退出
            startInfo.UseShellExecute = false;     //不使用系统外壳程序启动
            startInfo.RedirectStandardInput = false;   //不重定向输入
            startInfo.RedirectStandardOutput = true;   //重定向输出
            startInfo.RedirectStandardError = true;
            startInfo.CreateNoWindow = true;     //不创建窗口
            process.StartInfo = startInfo;
            try
            {
                if (process.Start())       //开始进程
                {
                    process.WaitForExit(2000);  //这里等待进程结束，等待时间为指定的毫秒
                    output = process.StandardOutput.ReadToEnd();//读取进程的输出
                    error = process.StandardError.ReadToEnd();
                    if (!string.IsNullOrEmpty(error))
                    {
                        logger.LogError(error);
                        //throw new Exception(error);
                    }

                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (process != null)
                    process.Close();
            }
            return output;
        }
    }
}
