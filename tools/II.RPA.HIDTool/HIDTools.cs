
//using II.RPA.NamedPipeWrapper;
//using II.RPA.WinAutomation.CommonUtility;
using System.Windows.Forms;

namespace II.RPA.HIDTool
{
    public partial class HIDTools : Form
    {
        //public static NamedPipeServer<HIDPipeResult> pipeServer { get; set; }
        //public const string PIPE_NAME = "II_RPA_HIDTools";
        //private static object _lock = new object();
        //private HIDSendHelper hid;
        //public HIDTools()
        //{
        //    InitializeComponent();
        //    Init();
        //}

        //private void Init()
        //{
        //    NLogger.Default.Info("【HIDTools】Start HIDTools");
        //    CheckHIDTool();
        //    StartServer();
        //    hid = new HIDSendHelper();
        //}
        //private void StartServer()
        //{
        //    try
        //    {
        //        lock (_lock)
        //        {
        //            if (pipeServer == null)
        //            {
        //                pipeServer = new NamedPipeServer<HIDPipeResult>(PIPE_NAME);
        //                pipeServer.ClientConnected -= Pipe_ClientConnected;
        //                pipeServer.ClientConnected += Pipe_ClientConnected;
        //                pipeServer.ClientDisconnected += Pipe_ClientDisconnected;
        //                pipeServer.ClientMessage += Server_OnReceivedMessage;
        //                pipeServer.Start();
        //                NLogger.Default.Info("【HIDTools】HIDTools PipeServer start");
        //                //this.richTextBox1.AppendText("Start Complete" + Environment.NewLine);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        NLogger.Default.Error(ex);
        //    }
        //}

        //#region 释放、连接、接受等事件

        //private void Pipe_ClientDisconnected(NamedPipeConnection<HIDPipeResult, HIDPipeResult> connection)
        //{
        //    NLogger.Default.Info("【HIDTools】Client Disconnect:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        //    try
        //    {
        //        pipeServer.Stop();
        //    }
        //    catch (Exception)
        //    {

        //    }
        //}

        //private void Pipe_ClientConnected(NamedPipeConnection<HIDPipeResult, HIDPipeResult> connection)
        //{
        //    try
        //    {
        //        if (pipeServer != null && connection.IsConnected)
        //        {
        //            pipeServer.PushMessage(new HIDPipeResult());
        //        }
        //        NLogger.Default.Info("【HIDTools】Client Connected:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        //    }
        //    catch
        //    {

        //    }
        //}
        //private void Server_OnReceivedMessage(NamedPipeConnection<HIDPipeResult, HIDPipeResult> connection, HIDPipeResult message)
        //{
            
        //    NLogger.Default.Info("【HIDTools】Action:" + message.Action + Environment.NewLine + JsonHelper.JsonByNewtonJson(message));
        //    this.Invoke((Action)delegate
        //    {
        //        try
        //        {
        //            switch (message.Action)
        //            {
        //                case eHIDSendAction.ClearAllText:
        //                    hid.ClearAllText(message.FormName);
        //                    pipeServer.PushMessage(message);
        //                    break;
        //                case eHIDSendAction.SendText:
        //                    hid.SendText(message.FormName, message.Content, message.KeepDown, message.Block);
        //                    NLogger.Default.Info($"【HIDTools】 Send Params:{message.FormName}，{message.Content}，{message.KeepDown}，{message.Block}");
        //                    //hid.SendText("Tetherscript Virtual Keyboard Driver Reader", message.Content, 50, 100);
        //                    pipeServer.PushMessage(message);
        //                    break;
        //            }
        //            message.ResponseMsg.Code = 0;
        //            message.ResponseMsg.Message = "success";
        //        }
        //        catch (Exception ex)
        //        {
        //            message.ResponseMsg.Code = -1;
        //            message.ResponseMsg.Message = "消息发送异常";
        //            pipeServer.PushMessage(message);
        //        }
        //    });
        //}
        //private void HIDTools_Load(object sender, EventArgs e)
        //{

        //}

        //#endregion
        //#region 检测HID工具进程，关闭多余的
        //private void CheckHIDTool()
        //{
        //    Process[] processesByName = GetProcessesByName("II.RPA.HIDTool", onlyCurrentUser: true);
        //    int curPid = Process.GetCurrentProcess().Id;
        //    Process[] array = processesByName;
        //    foreach (Process process in array)
        //    {
        //        if (process.Id != curPid)
        //        {
        //            try
        //            {
        //                process.Kill();
        //            }
        //            catch
        //            {
        //            }
        //        }
        //    }
        //}
        //public Process[] GetProcessesByName(string processName, bool onlyCurrentUser)
        //{
        //    if (!onlyCurrentUser)
        //    {
        //        return Process.GetProcessesByName(processName);
        //    }
        //    return (from m in Process.GetProcessesByName(processName)
        //            where GetProcessOwner(m.Id) == Environment.UserName
        //            select m).ToArray();
        //}

        //public string GetProcessOwner(int processId)
        //{
        //    foreach (ManagementObject obj in new ManagementObjectSearcher("Select * From Win32_Process Where ProcessID = " + processId).Get())
        //    {
        //        string[] argList = new string[2]
        //        {
        //        string.Empty,
        //        string.Empty
        //        };
        //        object[] args = argList;
        //        if (Convert.ToInt32(obj.InvokeMethod("GetOwner", args)) == 0)
        //        {
        //            return argList[0];
        //        }
        //    }
        //    return "NO OWNER";
        //}
        //#endregion

        //#region 让程序不显示在alt+Tab视图窗体中

        /////// <summary>
        /////// 让程序不显示在alt+Tab视图窗体中
        /////// </summary>
        //protected override CreateParams CreateParams
        //{
        //    get
        //    {
        //        const int WS_EX_APPWINDOW = 0x40000;
        //        const int WS_EX_TOOLWINDOW = 0x80;
        //        CreateParams cp = base.CreateParams;
        //        cp.ExStyle &= (~WS_EX_APPWINDOW);    // 不显示在TaskBar
        //        cp.ExStyle |= WS_EX_TOOLWINDOW;      // 不显示在Alt+Tab
        //        return cp;
        //    }
        //}

        //#endregion

        //private void button1_Click(object sender, EventArgs e)
        //{
        //    hid.SendText(textBox2.Text, textBox1.Text, 50, 100);
        //}
    }
}
