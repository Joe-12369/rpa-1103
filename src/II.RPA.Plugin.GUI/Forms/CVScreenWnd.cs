using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Imaging;
using Gma.UserActivityMonitor;
using II.RPA.Plugin.Core;
using System.Windows.Threading;
using II.RPA.WinAutomation.CommonUtility;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;


namespace II.RPA.Plugin.GUI
{
    public partial class CVScreenWnd : Form
    {
        Bitmap bitmap;
        Bitmap maskBitmap;
        Boolean isFirst = true;
        int formalX = 0;
        int formalY = 0;
        int downX;
        int downY;
        int upX;
        int upY;
        bool isCtrlKeyDown = false;
        bool isMouseDown = false;
        bool isDrawComplete = false;
        bool enablePassThrough = true;
        //ExtendedPanel panelBorderX;
        //ExtendedPanel panelBorderY;
        internal InspectorStatusForm inspectorStatusForm;
        Color lineColor;
        public string FilePath;
        private static ILogger logger = Logger.OfContext();
        public Point startPoint;
        public Point endPoint;
        private DispatcherTimer keyStateDispatcherTimer;

        public CVScreenWnd(string savePath = null, Bitmap bitmap = null)
        {
            InitializeComponent();
            this.TopMost = true;
            FilePath = savePath;
            this.bitmap = bitmap;
            maskBitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            if (bitmap != null)
            {
                Graphics gg = Graphics.FromImage(maskBitmap);
                gg.DrawImage(bitmap, 0, 0);
                gg.FillRectangle(new SolidBrush(Color.FromArgb(100, 0, 0, 0)), 0, 0, Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            }
            else
            {
                SetPenetrate();

                GuiElement.overlayForm.inspectorStatus.SetWndShow();
            }
            this.BackgroundImage = maskBitmap;
            this.Load += CVScreenWnd_Load;
            this.FormClosed += CVScreenWnd_FormClosed;
            lineColor = Color.LightSkyBlue;
            //panelBorderX = new ExtendedPanel
            //{
            //    Size = new Size(Screen.PrimaryScreen.Bounds.Width, 2),
            //    Parent = this,
            //    BackColor = Color.LightSkyBlue
            //};
            //panelBorderY = new ExtendedPanel
            //{
            //    Size = new Size(2, Screen.PrimaryScreen.Bounds.Height),
            //    Parent = this,
            //    BackColor = Color.LightSkyBlue
            //};

            keyStateDispatcherTimer = new DispatcherTimer();
            keyStateDispatcherTimer.Tick -= new EventHandler(KeyStateDispatcherTimer_Tick);
            keyStateDispatcherTimer.Tick += new EventHandler(KeyStateDispatcherTimer_Tick);
            keyStateDispatcherTimer.Interval = TimeSpan.FromMilliseconds(20);
            keyStateDispatcherTimer.Start();
        }

        private void CVScreenWnd_Load(object sender, EventArgs e)
        {
            InstallHook();
        }

        private void CVScreenWnd_FormClosed(object sender, FormClosedEventArgs e)
        {
            UninstallHook();
            keyStateDispatcherTimer?.Stop();
        }

        public CVScreenWnd(Rectangle location, Bitmap bitmap, Point point, string savepath = null) : this(savepath, bitmap)
        {
            this.WindowState = FormWindowState.Normal;
            this.Width = location.Width;
            this.Height = location.Height;
            this.Location = new Point(location.X, location.Y);

            isDrawComplete = false;
            isCtrlKeyDown = true;
            isMouseDown = true;
            endPoint = startPoint = this.PointToClient(point);
            downX = startPoint.X;
            downY = startPoint.Y;
        }
        public void SetPenetrate()
        {
            NativeMethods.SetWindowLong(this.Handle, NativeMethods.GWL_EXSTYLE, NativeMethods.WS_EX_TRANSPARENT | NativeMethods.WS_EX_LAYERED);
            NativeMethods.SetLayeredWindowAttributes(this.Handle, 3, 10, NativeMethods.LWA_ALPHA);
            this.Opacity = 0.5;
        }

        internal void InstallHook()
        {
            UninstallHook();
            HookManager.KeyDown += HookManager_KeyDown;
            HookManager.KeyUp += HookManager_KeyUp;
        }
        internal void UninstallHook()
        {
            HookManager.KeyDown -= HookManager_KeyDown;
            HookManager.KeyUp -= HookManager_KeyUp;
        }
        private void HookManager_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                UninstallHook();
                keyStateDispatcherTimer?.Stop();
                this.DialogResult = DialogResult.Cancel;
            }
            else if (e.KeyCode == Keys.Control || e.KeyCode == Keys.LControlKey || e.KeyCode == Keys.RControlKey)
            {
                if (isCtrlKeyDown)
                {
                    return;
                }
                isCtrlKeyDown = true;
                isDrawComplete = false;
                GuiElement.overlayForm.inspectorStatus.SetWndHide();
                if (bitmap == null)
                {
                    this.Hide();
                    //bitmap?.Dispose();
                    bitmap = GuiElement.CaptureDesktop();
                    this.Show();
                    using (Graphics gg = Graphics.FromImage(maskBitmap))
                    {
                        gg.DrawImage(bitmap, 0, 0);
                        gg.FillRectangle(new SolidBrush(Color.FromArgb(100, 0, 0, 0)), 0, 0, Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
                    }
                }
                this.FormBorderStyle = FormBorderStyle.None;
                lineColor = Color.Red;
                //panelBorderX.BackColor = panelBorderY.BackColor = Color.Red;
                //初始化化坐标
                endPoint = startPoint = this.PointToClient(Cursor.Position);
                downX = startPoint.X;
                downY = startPoint.Y;
                if (isMouseDown)
                {
                    isDrawComplete = false;
                }
            }
        }
        private void HookManager_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Control || e.KeyCode == Keys.LControlKey || e.KeyCode == Keys.RControlKey)
            {
                isCtrlKeyDown = false;
                isDrawComplete = true;
                lineColor = Color.LightSkyBlue;
                //panelBorderX.BackColor = panelBorderY.BackColor = Color.LightSkyBlue;
            }
        }
        private void Form2_MouseDown(object sender, MouseEventArgs e)
        {
            isMouseDown = true;
            if (isCtrlKeyDown)
            {
                isDrawComplete = false;
                endPoint = startPoint = new Point(e.X, e.Y);
                downX = startPoint.X;
                downY = startPoint.Y;
            }
        }

        private void Form2_MouseUp(object sender, MouseEventArgs e)
        {
            isMouseDown = false;
            GuiElement.overlayForm.inspectorStatus.SetWndShow();
            lineColor = Color.LightSkyBlue;
            //panelBorderX.BackColor = panelBorderY.BackColor = Color.LightSkyBlue;
            //panelBorderX.Location = panelBorderY.Location = new Point(0, 0);
            if (startPoint != endPoint)
            {
                isDrawComplete = true;
                Point localPoint = this.PointToScreen(new Point
                {
                    X = Math.Max(startPoint.X, endPoint.X),
                    Y = Math.Max(startPoint.Y, endPoint.Y)
                });
                contextMenuStrip1.Show(localPoint);
            }
            else
            {
                isDrawComplete = false;
            }

        }
        private void KeyStateDispatcherTimer_Tick(object sender, EventArgs e)
        {
            var screenPoint = System.Windows.Forms.Cursor.Position;
            GuiElement.overlayForm.inspectorStatus.MoveFromLocation(screenPoint);
            Point cursorPoint = this.PointToClient(screenPoint);
            bool isHideinspectorStatus = false;
            if (!isDrawComplete)
            {
                using (Graphics g = this.CreateGraphics())
                {
                    if (!isCtrlKeyDown && this.bitmap == null)
                        g.Clear(this.TransparencyKey);
                    g.DrawImage(maskBitmap, formalX < cursorPoint.X ? formalX - 2 : cursorPoint.X + 2, 0, new Rectangle(formalX < cursorPoint.X ? formalX - 2 : cursorPoint.X + 2, 0,
Math.Abs(cursorPoint.X - formalX), Screen.PrimaryScreen.Bounds.Height), GraphicsUnit.Pixel);
                    g.DrawImage(maskBitmap, 0, formalY < cursorPoint.Y ? formalY - 2 : cursorPoint.Y + 2, new Rectangle(0, formalY < cursorPoint.Y ? formalY - 2 : cursorPoint.Y + 2,
                        Screen.PrimaryScreen.Bounds.Width, Math.Abs(cursorPoint.Y - formalY)), GraphicsUnit.Pixel);
                    if (isMouseDown && isCtrlKeyDown)
                    {
                        //panelBorderX.Location = panelBorderY.Location = new Point(0, 0);
                        endPoint = cursorPoint;
                        g.DrawImage(bitmap, downX, downY, new Rectangle(downX, downY, cursorPoint.X - downX, cursorPoint.Y - downY), GraphicsUnit.Pixel);
                        g.DrawRectangle(new Pen(Color.FromArgb(126, 167, 255), 2), new Rectangle(downX < cursorPoint.X ? downX : cursorPoint.X, downY < cursorPoint.Y ? downY : cursorPoint.Y, Math.Abs(cursorPoint.X - downX), Math.Abs(cursorPoint.Y - downY)));
                        isHideinspectorStatus = true;
                    }
                    else if (!isMouseDown)
                    {
                        //panelBorderX.Location = new Point(0, cursorPoint.Y);
                        //panelBorderY.Location = new Point(cursorPoint.X, 0);
                        g.DrawLine(new Pen(lineColor, 2), cursorPoint.X, 0, cursorPoint.X, Screen.PrimaryScreen.Bounds.Height);
                        g.DrawLine(new Pen(lineColor, 2), 0, cursorPoint.Y, Screen.PrimaryScreen.Bounds.Width, cursorPoint.Y);
                    }
                }
                formalX = cursorPoint.X;
                formalY = cursorPoint.Y;
            }
            if (isHideinspectorStatus)
            {
                if (GuiElement.overlayForm.inspectorStatus.Visibility == System.Windows.Visibility.Visible)
                    GuiElement.overlayForm.inspectorStatus.SetWndHide();
            }
            else
            {
                if (GuiElement.overlayForm.inspectorStatus.Visibility == System.Windows.Visibility.Hidden)
                    GuiElement.overlayForm.inspectorStatus.SetWndShow();
            }
        }
        private void Form2_MouseMove(object sender, MouseEventArgs e)
        {
        }

        private void tlsmCapture_Click(object sender, EventArgs e)
        {
            string filepath = string.Empty;
            if (!string.IsNullOrEmpty(FilePath))
            {
                filepath = FilePath;
            }
            else
            {
                filepath = string.Format("{0}.png", Guid.NewGuid().ToString());
            }

            upX = endPoint.X;
            upY = endPoint.Y;
            if (downX == upX || downY == upY)
            {
                bitmap.Save(filepath);
            }
            else
            {
                Bitmap downBitmap = new Bitmap(Math.Abs(downX - upX), Math.Abs(downY - upY));
                Graphics g = Graphics.FromImage(downBitmap);
                g.DrawImage(bitmap, 0, 0,
                    new Rectangle(downX < upX ? downX : upX, downY < upY ? downY : upY, Math.Abs(downX - upX), Math.Abs(downY - upY)), GraphicsUnit.Pixel);
                downBitmap.Save(filepath);
            }
            UninstallHook();
            keyStateDispatcherTimer?.Stop();
            this.DialogResult = DialogResult.OK;
        }
        public new void Dispose()
        {
            UninstallHook();
            keyStateDispatcherTimer?.Stop();
            //GuiElement.overlayForm?.inspectorStatus?.Close();
            GuiElement.overlayForm.inspectorStatus.SetWndClose();
            bitmap?.Dispose();
            maskBitmap?.Dispose();
            base.Dispose();
        }
    }
}