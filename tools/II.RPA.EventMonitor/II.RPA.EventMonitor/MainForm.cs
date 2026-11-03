using Gma.UserActivityMonitor;
using II.RPA.EventMonitor.JSON;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace II.RPA.EventMonitor
{
    public partial class MainForm : Form
    {
        internal static Action<string> PrintMessage;
        internal static MouseKeyMonitor mouseKeyMonitor = null;
        private static readonly object taskListChanged = new object();

        public MainForm()
        {
            InitializeComponent();
#if !Trace
            this.ShowInTaskbar = false;
#endif
            //SetVisibleCore(false);
            
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.Hide();
            try
            {
#if !Trace
                this.Location = new Point(-1300, -1300);
                this.Size = new Size(1, 1);
#endif
                //this.InstallHook();
                //return;

                mouseKeyMonitor = new MouseKeyMonitor();
                TriggerBus.OnTaskListChanged += OnTaskListChanged;

                Thread t = new Thread(() =>
                {
                    PrintMessage = AddMessage;
                    //TriggerServer.Start();
                    SocketServer server = new SocketServer("127.0.0.1", 6446);
                    server.StartServer();
                });
                t.IsBackground = true;
                t.Start();
            }
            catch
            {

            }
        }

        #region test
        #endregion

        #region 新任务开始事件
        private void OnTaskListChanged(Dictionary<string, TriggerTask> TriggerTaskDic)
        {
            try
            {
                lock (taskListChanged)
                {
                    bool hasMonitorHotKey = false;
                    bool hasMonitorMouse = false;

                    hasMonitorMouse = TriggerTaskDic.Values.Any(task => task.Triggers.Any(trigger => trigger is MouseTrigger));
                    hasMonitorHotKey = TriggerTaskDic.Values.Any(task => task.Triggers.Any(trigger => trigger is HotkeyTrigger));

                    PrintMessage($"hasMonitorMouse,{hasMonitorMouse},hasMonitorHotKey:{hasMonitorHotKey}");
                    this.Invoke((MethodInvoker)delegate
                    {
                        if (hasMonitorMouse || hasMonitorHotKey)
                        {
                            mouseKeyMonitor.Start(hasMonitorHotKey, hasMonitorMouse);
                        }
                        if (!hasMonitorMouse || !hasMonitorHotKey)
                        {
                            mouseKeyMonitor.Stop(hasMonitorHotKey, hasMonitorMouse);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Loggers.Error($"更新鼠标按键监视器 状态异常,{ex}");
                throw ex;
            }
            //foreach (var task in TriggerTaskDic)
            //{
            //    foreach (var trigger in task.Value.Triggers)
            //    {
            //        if (trigger is MouseTrigger)
            //        {
            //            hasMonitorMouse = true;
            //        }
            //        if (trigger is HotkeyTrigger)
            //        {
            //            hasMonitorHotKey = true;
            //        }

            //        if (trigger is MouseTrigger || trigger is HotkeyTrigger)
            //        {
            //            if (!mouseKeyMonitor.IsMonitor)
            //            {
            //                this.Invoke((MethodInvoker)delegate
            //                {
            //                    mouseKeyMonitor.Start();
            //                });
            //            }
            //            return;
            //        }
            //    }
            //}
            //this.Invoke((MethodInvoker)delegate
            //{
            //    mouseKeyMonitor.Stop();
            //});
        }
        #endregion

        #region 设置消息日志
        private void AddMessage(string message)
        {
            try
            {

                Loggers.Debug(message);
#if Trace
            this.BeginInvoke(new Action(() =>
            {
                var id = Thread.CurrentThread.ManagedThreadId;
                rtxt_logs.AppendText($"{DateTime.Now.ToString("yyyy-MM-ss HH:mm:ss")}[{id}]:{message}{Environment.NewLine}");
                if (rtxt_logs.Lines.Length > 100)
                {
                    rtxt_logs.Lines = rtxt_logs.Lines.Skip(rtxt_logs.Lines.Length - 50).ToArray();
                }
                rtxt_logs.SelectionStart = rtxt_logs.TextLength;
                rtxt_logs.ScrollToCaret();
            }));
            Thread.Sleep(10);
#endif

            }
            catch
            {

            }
        }
        #endregion

        #region 让程序不显示在alt+Tab视图窗体中

        /// <summary>
        /// 让程序不显示在alt+Tab视图窗体中
        /// </summary>
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
        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(value);
        }
        #endregion


    }
}
