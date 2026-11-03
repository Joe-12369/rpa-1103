using Gma.UserActivityMonitor;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Threading;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.GUI
{
    public partial class CursorPositionForm : Form
    {
        //该窗口用作浮动工具栏
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        //当用户单击它时，以这种样式创建的顶级窗口不会成为前台窗口
        public const int WS_EX_NOACTIVATE = 0x08000000;
        public const int WS_EX_LAYERED = 0x00080000;
        public const int WS_EX_TRANSPARENT = 0x00000020;
        //如果未捕获鼠标，则消息将发送到光标下方的窗口。否则，消息将发送到捕获鼠标的窗口
        const int WM_NCHITTEST = 0x0084;
        const int HTTRANSPARENT = -1;
        private static DispatcherTimer dispatcherTimer = new DispatcherTimer();
        private bool enablePassThrough { get; set; }
        // Label tooltip;
        Point positionBase;
        public MousePosition mousePosition;
        private static ILogger logger = Logger.OfContext();

        public CursorPositionForm()
        {
            InitializeComponent();
            this.TopMost = true;
            //tooltip = new Label();
            //tooltip.AutoSize = true;
            //tooltip.ForeColor = Color.White;
            //tooltip.BackColor = Color.Black;
            //tooltip.Font = new Font("Microsoft YaHei,Helvetica Neue,Helvetica,Arial,sans-serif,Segoe UI",8,FontStyle.Regular);
            //tooltip.BackColor = Color.Black;
            //this.Controls.Add(tooltip);
            #region
            mousePosition = new MousePosition();
            mousePosition.Show();
            mousePosition.Topmost = true;
            #endregion
            this.Load += CursorPositionForm_Load;
            this.TopMost = true;
        }

        private void CursorPositionForm_Load(object sender, EventArgs e)
        {
            this.Width = recInput.Width;
            this.Height = recInput.Height;
            positionBase = recInput.Location;
            this.Location = positionBase;
        }

        public bool HasDoSelect { get; private set; }
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_TOOLWINDOW;//避免ATL+TAB时显示该窗体
                cp.ExStyle |= WS_EX_NOACTIVATE;//禁止激活，以便能够inspect菜单条目
                //cp.ExStyle |= WS_EX_TRANSPARENT;//禁止激活，以便能够inspect菜单条目
                //cp.ExStyle |= WS_EX_LAYERED;//禁止激活，以便能够inspect菜单条目
                return cp;
            }
        }
        protected override void WndProc(ref Message m)
        {
            if (enablePassThrough)
            {
                if (m.Msg == WM_NCHITTEST)
                {
                    m.Result = (IntPtr)HTTRANSPARENT;
                    return;
                }
            }
            base.WndProc(ref m);
        }
        public string ExceptionError { get; set; }
        bool hasTick;
        private Point cursorPoint;
        public Point CursorResult { get; private set; }
        int positionType;
        Rectangle recInput;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type">0：相对于桌面；1：相对于激活窗口</param>
        internal void StartCapture(int type, Rectangle rectangle)
        {
            positionType = type;
            HasDoSelect = false;
            hasTick = false;
            recInput = rectangle;
            InstallHook();
            dispatcherTimer.Tick -= new EventHandler(DispatcherTimer_Tick);
            dispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick);
            dispatcherTimer.Interval = TimeSpan.FromMilliseconds(100);
            dispatcherTimer.Start();

        }
        internal void StopHighlight()
        {
            UninstallHook();

            dispatcherTimer.Stop();
        }
        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (HasDoSelect == true)
            {
                return;
            }
            try
            {
                enablePassThrough = true;
                var screenPoint = System.Windows.Forms.Cursor.Position;
                //tooltip.Text = string.Format("鼠标位置 X：{0}，Y：{1}\r\nCtrl+鼠标左键 获取位置坐标", screenPoint.X-positionBase.X, screenPoint.Y-positionBase.Y);
                cursorPoint = this.PointToClient(screenPoint);
                //这里需要把显示的文本放到合适的地方
                if (cursorPoint.X > this.recInput.Width || cursorPoint.X < 0 || cursorPoint.Y < 0 || cursorPoint.Y > this.recInput.Height)
                {
                    return;
                }
                //先判断横坐标
                //int x = cursorPoint.X + 5 + tooltip.Width;
                //int y = cursorPoint.Y +5+ tooltip.Height;
                //int realX = cursorPoint.X + 5;
                //int realY = cursorPoint.Y + tooltip.Height;

                //if(x>this.recInput.Width)
                //{
                //    realX = cursorPoint.X - 5 - tooltip.Width;
                //}
                //if(y>this.recInput.Height)
                //{
                //    realY = cursorPoint.Y - tooltip.Height;
                //}
                //tooltip.Location = new Point(realX, realY);
                int x = cursorPoint.X + 5 + (int)mousePosition.Width;
                int y = cursorPoint.Y + 5 + (int)mousePosition.Height;
                int realX = cursorPoint.X + 5;
                int realY = cursorPoint.Y + (int)mousePosition.Height;

                if (x > this.recInput.Width)
                {
                    realX = cursorPoint.X - 5 - (int)mousePosition.Width;
                }
                if (y > this.recInput.Height)
                {
                    realY = cursorPoint.Y - (int)mousePosition.Height;
                }
                mousePosition.tb_PositionX.Text = (screenPoint.X - positionBase.X).ToString();
                mousePosition.tb_PositionY.Text = (screenPoint.Y - positionBase.Y).ToString();
                //mousePosition.setPosition(screenPoint.X - positionBase.X, screenPoint.Y - positionBase.Y);
                //mousePosition.Location = new Point(realX, realY);
                mousePosition.Left = realX;
                mousePosition.Top = realY;
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                ExceptionError = ex.Message;
                StopHighlight();
            }
            hasTick = true;
        }
        internal void InstallHook()
        {
            UninstallHook();
            HookManager.MouseUp += HookManager_MouseUp;
            HookManager.KeyDown += HookManager_KeyUp;
        }
        internal void UninstallHook()
        {
            HookManager.MouseUp -= HookManager_MouseUp;
            HookManager.KeyDown -= HookManager_KeyUp;
        }
        private void HookManager_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Escape:
                    e.Handled = true;
                    DoCancel();
                    break;
                default:
                    break;
            }
        }

        private void HookManager_MouseUp(object sender, MouseEventArgs e)
        {
            if (hasTick == false)
            {
                return;
            }
            switch (e.Button)
            {
                case MouseButtons.Left:
                    if ((System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
                    {
                        CursorResult = cursorPoint;
                        this.DialogResult = DialogResult.OK;
                        StopHighlight();
                    }
                    break;
                case MouseButtons.Right:
                    DoCancel();
                    break;
                default:
                    break;
            }
        }
        private void DoCancel()
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    mousePosition.Hide();
                    this.Invoke(new Action(() =>
                    {
                        StopHighlight();
                        this.DialogResult = DialogResult.Cancel;
                    }));
                }
                catch { }
            });
        }
    }
}
