using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using II.RPA.JavaBridgeHost.IPC;
using II.RPA.NamedPipeWrapper;
using II.RPA.WinAutomation.CommonUtility;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Logging.Formatting;

namespace II.RPA.JavaBridgeHost
{
    public partial class IIRPAJavaHost : Form, IExecuteMessageable
    {
        private static ILogger logger = Logger.OfContext();
        private static System.Timers.Timer aTimer = null;
        private static object lockObj = new object();
        private static JavaExecutor sdk;
        private bool allowshowdisplay = true;
        private bool quit = false;

        public IIRPAJavaHost()
        {
            this.Location = new Point(-1000, -1000);
            InitializeComponent();
            this.WindowState = FormWindowState.Minimized;
            LoadConfig();
            aTimer = new System.Timers.Timer();
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = false;
            aTimer.Interval = 1000;
            this.StartServer();
            this.StartOldServer();
            aTimer.Enabled = true;
            this.Load += IIRPAJavaHost_Load;
        }

        internal PipeWrapService OldPipeServie { get; private set; }
        internal PipeHostService ServiceHost { get; private set; }

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
            base.SetVisibleCore(allowshowdisplay ? value : allowshowdisplay);
        }

        private static void LoadConfig()
        {
            Task.Run(() =>
            {
                //ErrorMessageConfig.Load();//加载错误码信息配置文件
            });
        }

        private void IIRPAJavaHost_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (quit == false)
            {
                e.Cancel = true;
                SetVisibleCore(false);
            }
            else
            {
                if (MessageBox.Show(eCommonMessageType.IIRPAJavaHost_cs_QueDingGuanBiGuanBiHouWuFa.GetMessage(), eCommonMessageType.JavaNode_cs_TiShi.GetMessage(), MessageBoxButtons.YesNo) == DialogResult.No)
                {
                    e.Cancel = true;
                }
            }
        }

        private void IIRPAJavaHost_Load(object sender, EventArgs e)
        {
            this.Hide();
            if (sdk == null)
            {
                sdk = new JavaExecutor();
            }
        }

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            this.allowshowdisplay = true;
            this.Visible = !this.Visible;
        }

        private void OnTimedEvent(object source, System.Timers.ElapsedEventArgs e)
        {
            this.Invoke(new Action(() =>
            {
                allowshowdisplay = false;
                aTimer.Enabled = false;
                this.Close();
            }));
        }

        private void Pipe_ClientConnected(NamedPipeConnection<JavaMessage, JavaMessage> connection)
        {
            logger.LogInformation("Client Connected");
            try
            {
                this.Invoke(new Action(() =>
                {
                    this.richTextBox1.AppendText("Client Connected:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine);
                }));
            }
            catch
            {
            }
        }
        private void Pipe_ClientDisconnected(NamedPipeConnection<JavaMessage, JavaMessage> connection)
        {
            logger.LogInformation("Client Disconnect:");
            try
            {
                this.Invoke(new Action(() =>
                {
                    this.richTextBox1.AppendText("Client Disconnect:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine);
                }));
            }
            catch (Exception)
            {
            }
        }

        public JavaMessage ExecuteReceiveMessage(JavaMessage message)
        {
            ISelectorContext selectorContext = null;
            if (!message.IsDataColloetion)
            {
                SelectorAdapter adapter = new SelectorAdapter();
                selectorContext = adapter.GetSelectorJson(message.selector);
            }
            else
            {
                selectorContext = new SelectorContext();
                selectorContext.Version = "1.0";
                selectorContext.Selector = message.selector;
            }
            logger.LogDebug("JavaReceivedAction:" + message.action + Environment.NewLine + message.selector);
            lock (this)
            {
                string result = string.Empty;
                switch (message.action)
                {
                    case "GetTableData":
                        result = sdk.GetTableData(selectorContext);
                        break;

                    case "MouseHover":
                        result = sdk.MouseHover(selectorContext, message.offsetX, message.offsetY, message.cursorPos);
                        break;

                    case "MouseClick":
                        result = sdk.MouseClick(selectorContext, message.clickType, message.offsetX, message.offsetY, message.issimulate == 1, message.cursorPos, message.keyType);
                        break;

                    case "SetText":
                        //var isSimulate = message.issimulate == 1 ? true : false;
                        //result = sdk.SetText(message.selector, message.text, message.isReplace, isSimulate);
                        result = sdk.SetText(selectorContext.Selector, message.text, message.isReplace, message.issimulate, message.interval, message.isVerify);
                        break;

                    case "GetText":
                        result = sdk.GetText(selectorContext, 0);
                        break;

                    case "GetElementSelect":
                        result = sdk.GetElementSelect(selectorContext, 0);
                        break;
                    case "GetElementSelectEx":
                        result = sdk.GetElementSelectEx(selectorContext, message.JsonParam, 0);
                        break;

                    case "GetElementCheck":
                        result = sdk.GetElementCheck(selectorContext, 0);
                        break;

                    case "SetElementSelect":
                        result = sdk.SetElementSelect(selectorContext, message.selectType, message.setvalue, 0);
                        break;

                    case "SetElementCheck":
                        result = sdk.SetElementCheck(selectorContext, message.check, 0);
                        break;

                    case "GetRegion":
                        result = sdk.GetRegion(selectorContext, message.positionType, 0);
                        break;

                    case "IsElementExist":
                        result = sdk.IsElementExist(selectorContext, message.findOnce, 0);
                        break;

                    case "IsElementExistEx":
                        result = sdk.IsElementExistEx(selectorContext, message.findOnce, 0);
                        break;

                    case "GetSubElements":
                        result = sdk.GetSubElements(selectorContext, 0);
                        break;

                    case "ElementShot":
                        result = sdk.ElementShot(selectorContext, message.snapFilepath, 0);
                        break;

                    case "GetButtonEnable":
                        result = sdk.GetButtonEnable(selectorContext, 0);
                        break;
                }
                return new JavaMessage() { excuteResult = result };
            }
        }

        private void StartOldServer()
        {
            try
            {
                lock (lockObj)
                {
                    if (this.OldPipeServie == null)
                    {
                        this.OldPipeServie = new PipeWrapService(this);
                    }
                    this.OldPipeServie.StartServer();
                    logger.LogInformation("Pipe start");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
        }

        private void StartServer()
        {
            try
            {
                lock (lockObj)
                {
                    if (this.ServiceHost == null)
                    {
                        this.ServiceHost = new PipeHostService();
                    }
                    this.ServiceHost.StartHostService(new JavaIPCService(this));
                    logger.LogInformation("Pipe start");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
        }

        private void tlmiQuit_Click(object sender, EventArgs e)
        {
            quit = true;
            this.Close();
        }
    }
}