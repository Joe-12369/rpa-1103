using MSHTML;
using Newtonsoft.Json.Linq;
using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using II.RPA.WinAutomation.CommonUtility;
using System.Reflection;
using SHDocVw;
using Newtonsoft.Json;

namespace II.RPA.DataCapture
{
    //123456
    public partial class DataCaptureForm : Form
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool SetProcessDPIAware();
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetSystemMetrics(int nIndex);
        [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
        public static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetDC(IntPtr ptr);
        public IHTMLDocument2 _htmlDoc2;
        public int _Width;  //屏幕宽度    
        public int _Height; //屏幕高度
        public int _FirstLoad;
        public int _typeOperation;
        public string _strRule;
        public double _dpi;
        public int _BrowserType;
        public DataCaptureForm(int nType, string strUrl = "www.baidu.com")
        {
            SetProcessDPIAware();
            InitializeComponent();

            _Width = GetSystemMetrics(0);  //屏幕宽度    
            _Height = GetSystemMetrics(1); //屏幕高度

            _typeOperation = nType;
            _BrowserType = 1;
            PublicConfig.gbRulesGet = false;
            PublicConfig.RemoveIEDocAndListener();

            _dpi = GetDeviceCapsEX();
            _FirstLoad = 0;
            if (_typeOperation == 1)
            {
                InitControls();
            }
            else if (_typeOperation == 2)
            {
                InitControls();
            }

            //ShowInTaskbar = false;
        }
        public void InitControls()
        {
            Location = new Point(Convert.ToInt32(0), Convert.ToInt32((double)_Height * 0.7 - 20.0 * _dpi));
            Size = new Size(Convert.ToInt32(_Width), Convert.ToInt32((double)_Height * 0.3 - 8.0 * _dpi));

            pictureBox1.Location = new Point(Convert.ToInt32(10 * _dpi), Convert.ToInt32(4.0 * _dpi));
            pictureBox1.Width = Convert.ToInt32(16.0 * _dpi);
            pictureBox1.Height = Convert.ToInt32(16.0 * _dpi);
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;

            label1.Location = new Point(Convert.ToInt32(28 * _dpi), Convert.ToInt32(4.0 * _dpi));
            label1.Size = new Size(Convert.ToInt32(96.0 * _dpi), Convert.ToInt32(16.0 * _dpi));
            label1.Text = "数据采集";

            pictureBoxClose2.Location = new Point(Convert.ToInt32(_Width - 26.0 * _dpi), Convert.ToInt32(6.0 * _dpi));
            pictureBoxClose2.Width = Convert.ToInt32(14.0 * _dpi);
            pictureBoxClose2.Height = Convert.ToInt32(14.0 * _dpi);
            pictureBoxClose2.SendToBack();

            pictureBoxClose.Location = new Point(Convert.ToInt32(_Width - 24.0 * _dpi), Convert.ToInt32(8.0 * _dpi));
            pictureBoxClose.Width = Convert.ToInt32(10.0 * _dpi);
            pictureBoxClose.Height = Convert.ToInt32(10.0 * _dpi);
            pictureBoxClose.SizeMode = PictureBoxSizeMode.StretchImage;

            pictureBoxMini2.Location = new Point(Convert.ToInt32(_Width - 50.0 * _dpi), Convert.ToInt32(6.0 * _dpi));
            pictureBoxMini2.Width = Convert.ToInt32(14.0 * _dpi);
            pictureBoxMini2.Height = Convert.ToInt32(14.0 * _dpi);
            pictureBoxMini2.SendToBack();

            pictureBoxMini.Location = new Point(Convert.ToInt32(_Width - 49.0 * _dpi), Convert.ToInt32(6.0 * _dpi));
            pictureBoxMini.Width = Convert.ToInt32(12.0 * _dpi);
            pictureBoxMini.Height = Convert.ToInt32(8.0 * _dpi);
            pictureBoxMini.SizeMode = PictureBoxSizeMode.StretchImage;

            webBrowser1.Location = new Point(Convert.ToInt32(8.0 * _dpi), Convert.ToInt32(24.0 * _dpi));
            webBrowser1.Size = new Size(Convert.ToInt32(_Width - 16.0 * _dpi), Convert.ToInt32((double)_Height * 0.3 - 32.0 * _dpi));
            ((SHDocVw.IWebBrowser2)webBrowser1.ActiveXInstance).Silent = true;
            webBrowser1.ScrollBarsEnabled = true;
            webBrowser1.Navigate("http://localhost:3333/resources/support/xiaozhangyu/configure/configure.html");
            //webBrowser1.Navigate(strUrl);
        }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == PublicConfig.WM_WINFORM_CLOSE)
            {
                this.Close();
                return;
            }

            base.WndProc(ref m);
        }
        public double GetDeviceCapsEX()
        {
            IntPtr hdc = GetDC(IntPtr.Zero);
            int zoom = GetDeviceCaps(hdc, 88);
            double dpi = 0;
            switch (zoom)
            {
                case 95:
                case 96:
                case 97:
                    dpi = 1;
                    break;
                case 119:
                case 120:
                case 121:
                    dpi = 1.25;
                    break;
                case 143:
                case 144:
                case 145:
                    dpi = 1.5;
                    break;
                case 167:
                case 168:
                case 169:
                    dpi = 1.75;
                    break;
                case 191:
                case 192:
                case 193:
                    dpi = 2;
                    break;
                case 215:
                case 216:
                case 217:
                    dpi = 2.25;
                    break;
                case 239:
                case 240:
                case 241:
                    dpi = 2.5;
                    break;
                case 263:
                case 264:
                case 265:
                    dpi = 2.75;
                    break;
                case 287:
                case 288:
                case 289:
                    dpi = 3.0;
                    break;
                default:
                    break;
            }

            return dpi;
        }
        private void DataCaptureForm_Load(object sender, EventArgs e)
        {
            
        }
        public void DocumentCapture()
        {
            bool bRet = false;
            bRet = GetWebBrowser(Convert.ToInt32(_Width + 16.0 * _dpi) / 2, Convert.ToInt32((double)_Height * 0.85 - 16.0 * _dpi));
            if (bRet == true)
            {
                PublicConfig.ghtmlDownDoc = _htmlDoc2 as HTMLDocument;
                PublicConfig.ghtmlDownDoc2 = _htmlDoc2 as IHTMLDocument2;

                bool retReadyState = PublicConfig.GetReadyStateComplete(PublicConfig.ghtmlDownDoc2);
                if (retReadyState == false)
                    return;

                PublicConfig.gdocEventListenerDown = new DocumentEventListenerDown();
                try
                {
                    PublicConfig.LogTrace(string.Format("Configure addEventListener 1"));
                    PublicConfig.ghtmlDownDoc.addEventListener(PublicConfig.gstrShiZaiEventOut, PublicConfig.gdocEventListenerDown, false);
                }
                catch (Exception ex)
                {
                    PublicConfig.ReturnErrorInfo(string.Format("配置页加载显示有误，请确保IE8及以上内核!"), 4);
                    return;
                }

                TransferEngineMessage nTransferToConfigure = new TransferEngineMessage();
                nTransferToConfigure.FromEngineToConfigureMessageBySetConfig();
            }
            else
            {
                PublicConfig.ReturnErrorInfo(string.Format("配置页加载显示有误!"), 6);
                return;
            }

            if (_typeOperation == 2)
            {
                //this.BeginInvoke(new Action(() =>
                //{
                //    //this.Hide();
                //    this.Opacity = 0.0;
                //}));
            }

            ConfigDataCapture nConfig = new ConfigDataCapture();
            nConfig.StartDataCapture(_strRule);
        }
        public void DocumentConfigure()
        {
            bool bRet = false;
            bRet = GetWebBrowser(Convert.ToInt32(_Width + 16.0 * _dpi) / 2, Convert.ToInt32((double)_Height * 0.85 - 16.0 * _dpi));
            if (bRet == true)
            {
                PublicConfig.ghtmlDownDoc = _htmlDoc2 as HTMLDocument;
                PublicConfig.ghtmlDownDoc2 = _htmlDoc2 as IHTMLDocument2;

                bool retReadState = PublicConfig.GetReadyStateComplete(PublicConfig.ghtmlDownDoc2);
                if (retReadState == false)
                    return;

                PublicConfig.gdocEventListenerDown = new DocumentEventListenerDown();
                try
                {
                    PublicConfig.LogTrace(string.Format("Configure addEventListener 2"));
                    PublicConfig.ghtmlDownDoc.addEventListener(PublicConfig.gstrShiZaiEventOut, PublicConfig.gdocEventListenerDown, false);

                    TransferEngineMessage nTransferToConfigure = new TransferEngineMessage();
                    nTransferToConfigure.FromEngineToConfigureMessageByRules();
                }
                catch (Exception ex)
                {
                    PublicConfig.ReturnErrorInfo(string.Format("配置页通信或者Rule格式有误，请确保IE8及以上内核且Rule格式正确!"), 7);
                    return;
                }
            }
            else
            {
                PublicConfig.ghtmlDownDoc = null;
                PublicConfig.ReturnErrorInfo(string.Format("配置页加载显示有误!"), 8);         
                return;
            }

            ConfigDataCapture nConfig = new ConfigDataCapture();
            nConfig.StartDataConfig();
        }
        private void DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            if (_FirstLoad == 0)
            {
                PublicConfig.gIntPtrWnd = Handle;
                this.TopMost = true;                              
                this.TopMost = false;

                if (_typeOperation == 1)
                {
                    PublicConfig.gbRulesGet = false;
                    DocumentConfigure();
                }
                else if (_typeOperation == 2)
                {
					PublicConfig.gbRulesGet = false;
                    DocumentCapture();
                }

                PublicConfig.gbExecJS = true;
            }

            _FirstLoad++;
        }
        public bool GetWebBrowser(int x, int y)
        {
            IHTMLDocument2 htmlDoc2;
            bool bRet = PublicConfig.GetDocument2(x, y, out htmlDoc2);
            _htmlDoc2 = htmlDoc2;
            return bRet;
        }
        private void DataCaptureForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            PublicConfig.gbExecJS = false;
        }

        private void DataCaptureClose(object sender, EventArgs e)
        {
            PublicConfig.gbExecJS = false;
            this.Close();
        }

        private void DataCaptureMini(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void pictureBoxClose_MouseMove(object sender, MouseEventArgs e)
        {
            pictureBoxClose2.BackColor = Color.FromArgb(255, 255, 0, 0);
            pictureBoxClose.BackColor = Color.FromArgb(255, 255, 0, 0);
            try
            {
                pictureBoxClose.Load(string.Format("./dll/ElementSDKV1/II.RPA/数据采集关闭White.png"));
            }
            catch (Exception ex)
            {

            }
        }

        private void pictureBoxClose_MouseLeave(object sender, EventArgs e)
        {
            pictureBoxClose2.BackColor = SystemColors.Control;
            pictureBoxClose.BackColor = SystemColors.Control;
            try
            {
                pictureBoxClose.Load(string.Format("./dll/ElementSDKV1/II.RPA/数据采集关闭.png"));
            }
            catch (Exception ex)
            {

            }
        }

        private void pictureBoxMini_MouseLeave(object sender, EventArgs e)
        {
            pictureBoxMini.BackColor = SystemColors.Control;
            pictureBoxMini2.BackColor = SystemColors.Control;
        }

        private void pictureBoxMini_MouseMove(object sender, MouseEventArgs e)
        {
            pictureBoxMini.BackColor = SystemColors.ControlLight;
            pictureBoxMini2.BackColor = SystemColors.ControlLight;
        }

        private void pictureBoxClose_MouseEnter(object sender, EventArgs e)
        {
            ToolTip p = new ToolTip();
            p.ShowAlways = true;
            p.SetToolTip(this.pictureBoxClose, string.Format("关闭"));
        }

        private void pictureBoxMini_MouseEnter(object sender, EventArgs e)
        {
            ToolTip p = new ToolTip();
            p.ShowAlways = true;
            p.SetToolTip(this.pictureBoxMini, string.Format("最小化"));
        }
    }
}
