using Cassia;
using II.RPA.NamedPipeWrapper.Sockets;
using II.RPA.NamedPipeWrapper.Sockets.Entitys;
using II.RPA.SupportService.Sockets.Entitys;
using II.RPA.WinAutomation.CommonUtility;
using SuperSocket.SocketBase;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SuperSocket.SocketEngine;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.AdminTransfer
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private static ILogger logger = Logger.OfContext();
        private static TelnetServer appServer = null;
        private static IBootstrap bootstrap = null;
        private static int startUPSessionID = -1;

        internal static int StartUPSessionID
        {
            get
            {
                return startUPSessionID;
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            this.Location = new Point(-1000, -1000);
            ITerminalServicesManager manager = new TerminalServicesManager();

            using (ITerminalServer server = manager.GetLocalServer())
            {
                server.Open();
                var ss = server.GetSessions();
                foreach (ITerminalServicesSession session in ss)
                {
                    if (session.ConnectionState == Cassia.ConnectionState.Active)
                    {
                        startUPSessionID = session.SessionId;
                        break;
                    }
                }
            }
            CheckApp();
            Task.Run(() =>
            {
                StartTelnetServer();
            });
        }
        #region 启动通讯服务
        private void StartTelnetServer()
        {
            logger.LogInformation("II.RPA.AdminTransfer Begin Start");
            try
            {
                #region bootstrap启动
                bootstrap = BootstrapFactory.CreateBootstrap();
                if (!bootstrap.Initialize())
                {
                    System.Environment.Exit(0);
                    return;
                }
                var result = bootstrap.Start();
                appServer = (TelnetServer)bootstrap.GetServerByName("TelnetServer");
                logger.LogInformation($"Start result: {result}!");
                if (result == StartResult.Failed)
                {
                    logger.LogInformation("Failed to start!");
                    System.Environment.Exit(0);
                    return;
                }
                return;
                #endregion

            }
            catch (Exception ex)
            {
                LoggingExtensions.LogError(logger, ex, "bootstrap启动服务失败");
            }
        }

        #endregion

        #region 原Socket实现方式  目前停用

        ////创建服务器对象，默认监听本机0.0.0.0，端口12345
        //SocketServer server = null;
        //void Start()
        //{
        //    logger.LogInformation("II.RPA.AdminTransfer Begin Start");
        //    server = new SocketServer(60110);
        //    //处理从客户端收到的消息
        //    server.HandleRecMsg = new Action<byte[], SocketConnection, SocketServer>((bytes, client, theServer) =>
        //    {
        //        string msg = Encoding.UTF8.GetString(bytes);
        //        logger.LogInformation($"收到消息:{msg}");
        //        SocketRequestMessage request = msg.ToObject<SocketRequestMessage>();
        //        DoTask(client, request);
        //    });

        //    //处理服务器启动后事件
        //    server.HandleServerStarted = new Action<SocketServer>(theServer =>
        //    {
        //        logger.LogInformation("服务已启动************");
        //    });

        //    //处理新的客户端连接后的事件
        //    server.HandleNewClientConnected = new Action<SocketServer, SocketConnection>((theServer, theCon) =>
        //    {
        //        logger.LogInformation($@"一个新的客户端接入，当前连接数：{theServer.ClientList.Count}");
        //    });

        //    //处理客户端连接关闭后的事件
        //    server.HandleClientClose = new Action<SocketConnection, SocketServer>((theCon, theServer) =>
        //    {
        //        logger.LogInformation($@"一个客户端关闭，当前连接数为：{theServer.ClientList.Count}");
        //    });

        //    //处理异常
        //    server.HandleException = new Action<Exception>(ex =>
        //    {
        //        logger.LogInformation(ex);
        //    });
        //    //服务器启动
        //    server.StartServer();
        //    logger.LogInformation("II.RPA.AdminTransfer End Start");
        //}

        //private void DoTask(SocketConnection connection, SocketRequestMessage request)
        //{
        //    SocketResponseMessage response;
        //    try
        //    {

        //        switch (request.Method)
        //        {
        //            case "RDPUnlock":
        //                RDPLockParams unlockParams = request.Params.ToObject<RDPLockParams>();
        //                response = RDPUnlock(unlockParams);
        //                break;
        //            case "RDPLock":
        //                RDPLockParams lockParams = request.Params.ToObject<RDPLockParams>();
        //                response = RDPLock(lockParams);
        //                break;
        //            default:
        //                response = new SocketResponseMessage { Code = (int)eSocketMessageStatus.Fail, Message = "指令不存在" };
        //                break;
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        logger.LogError($"{request.Method} 执行异常异常:{ex.StackTrace},{ex}");
        //        response = new SocketResponseMessage { Code = (int)eSocketMessageStatus.Fail, Message = "执行异常异常" };
        //    }
        //    connection.Send(response);
        //}

        //public SocketResponseMessage RDPUnlock(RDPLockParams param)
        //{
        //    logger.LogInformation("Enter RDPUnlock");
        //    SocketResponseMessage res = new SocketResponseMessage();
        //    try
        //    {
        //        ITerminalServicesManager manager = new TerminalServicesManager();
        //        bool matchSession = false;
        //        using (ITerminalServer server = manager.GetLocalServer())
        //        {
        //            server.Open();
        //            foreach (ITerminalServicesSession session in server.GetSessions())
        //            {
        //                logger.LogInformation($"【会话信息】session.WindowStationName：{session.WindowStationName}，session.UserName：{session.UserName}，SessionId:{session.SessionId}");
        //                if (session.UserName == param.UserName)//session.WindowStationName.Contains("#") && 
        //                {
        //                    matchSession = true;
        //                    string name = session.WindowStationName;
        //                    int sId = session.SessionId;
        //                    string tsconFileName = "tscon";
        //                    bool IsWin7 = Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor == 1;
        //                    if (IsWin7)
        //                    {
        //                        tsconFileName = "tscon5";
        //                    }
        //                    logger.LogInformation($"tsconFileName {tsconFileName}");
        //                    string cmdText = string.Format("{0} {1} /dest:console", tsconFileName, sId);
        //                    ExecuteCmd(cmdText);
        //                    logger.LogInformation($"cmd  excuted");

        //                    break;
        //                }
        //            }
        //            if (matchSession == false)
        //            {
        //                res.Code = (int)eSocketMessageStatus.Fail;
        //                res.Message = "屏幕解锁失败,无匹配会话";
        //            }
        //            else
        //            {
        //                res.Code = (int)eSocketMessageStatus.Success;
        //                res.Message = "屏幕解锁成功";
        //            }
        //            logger.LogInformation(res.ToJson());
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        res.Code = (int)eSocketMessageStatus.Fail;
        //        res.Message = "error";
        //        logger.LogError(ex);
        //    }
        //    return res;
        //}

        //FreeRDP.Core.RDP rdp;
        //internal SocketResponseMessage RDPLock(RDPLockParams param)
        //{
        //    SocketResponseMessage msg = new SocketResponseMessage();
        //    msg.Code = (int)eSocketMessageStatus.Success;
        //    rdp = new FreeRDP.Core.RDP();
        //    try
        //    {
        //        rdp.Connect("127.0.0.2", "", param.UserName, param.Password, 3389, null);
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.LogError(ex);
        //        msg.Code = (int)eSocketMessageStatus.Fail;
        //        msg.Message = "屏幕锁屏失败";
        //    }
        //    return msg;
        //}
        //private static string ExecuteCmd(string cmd)
        //{
        //    string output = "";
        //    string error = "";
        //    Process process = new Process();     //创建进程对象
        //    ProcessStartInfo startInfo = new ProcessStartInfo();
        //    startInfo.LoadUserProfile = true;
        //    startInfo.FileName = "cmd.exe";      //设定需要执行的命令
        //    startInfo.Arguments = "/C " + cmd;   //设定参数，其中的“/C”表示执行完命令后马上退出
        //    startInfo.UseShellExecute = false;     //不使用系统外壳程序启动
        //    startInfo.RedirectStandardInput = false;   //不重定向输入
        //    startInfo.RedirectStandardOutput = true;   //重定向输出
        //    startInfo.RedirectStandardError = true;
        //    startInfo.CreateNoWindow = true;     //不创建窗口
        //    process.StartInfo = startInfo;
        //    try
        //    {
        //        if (process.Start())       //开始进程
        //        {
        //            process.WaitForExit(2000);  //这里等待进程结束，等待时间为指定的毫秒
        //            output = process.StandardOutput.ReadToEnd();//读取进程的输出
        //            error = process.StandardError.ReadToEnd();
        //            if (!string.IsNullOrEmpty(error))
        //            {
        //                throw new Exception(error);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //    finally
        //    {
        //        if (process != null)
        //            process.Close();
        //    }
        //    return output;
        //}

        #endregion

        #region 检测HID工具进程，关闭多余的
        private void CheckApp()
        {
            Process[] processesByName = GetProcessesByName("II.RPA.AdminTransfer", onlyCurrentUser: true);
            int curPid = Process.GetCurrentProcess().Id;
            Process[] array = processesByName;
            foreach (Process process in array)
            {
                if (process.Id != curPid)
                {
                    try
                    {
                        process.Kill();
                    }
                    catch
                    {
                    }
                }
            }
        }
        public static string GetProcessOwner(int processId)
        {
            try
            {
                foreach (ManagementObject obj in new ManagementObjectSearcher("Select * From Win32_Process Where ProcessID = " + processId).Get())
                {
                    string[] argList = new string[2]
                    {
                    string.Empty,
                    string.Empty
                    };
                    object[] args = argList;
                    if (Convert.ToInt32(obj.InvokeMethod("GetOwner", args)) == 0)
                    {
                        return argList[0];
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError(e);
                return "NO OWNER";
            }
            return "NO OWNER";
        }
        public static Process[] GetProcessesByName(string processName, bool onlyCurrentUser)
        {
            try
            {
                if (!onlyCurrentUser)
                {
                    return Process.GetProcessesByName(processName);
                }
                return (from m in Process.GetProcessesByName(processName)
                        where GetProcessOwner(m.Id) == Environment.UserName
                        select m).ToArray();
            }
            catch (Exception e)
            {
                logger.LogError(e);
            }
            return null;
        }
        #endregion

        #region 让程序不显示在alt+Tab视图窗体中

        ///// <summary>
        ///// 让程序不显示在alt+Tab视图窗体中
        ///// </summary>
        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_EX_APPWINDOW = 0x40000;
                const int WS_EX_TOOLWINDOW = 0x80;
                CreateParams cp = base.CreateParams;
                cp.ExStyle &= (~WS_EX_APPWINDOW);    // 不显示在TaskBar
                cp.ExStyle |= WS_EX_TOOLWINDOW;      // 不显示在Alt+Tab
                return cp;
            }
        }

        #endregion
    }
}
