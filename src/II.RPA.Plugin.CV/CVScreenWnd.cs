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

namespace II.RPA.Plugin.CV
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
        bool isMouseDown = false;
        bool isDrawComplete = false;
        public string FilePath;

        public Point startPoint;
        public Point endPoint;
        public CVScreenWnd(Bitmap bitmap,string savePath=null)
        {
            InitializeComponent();
            FilePath = savePath;
            this.bitmap = bitmap;
            maskBitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            Graphics gg = Graphics.FromImage(maskBitmap);
            gg.DrawImage(bitmap, 0, 0);
            gg.FillRectangle(new SolidBrush(Color.FromArgb(100, 0, 0, 0)), 0, 0, Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            this.BackgroundImage = maskBitmap;
            this.Load += CVScreenWnd_Load;
            this.FormClosed += CVScreenWnd_FormClosed;
            this.TopMost = true;
        }

        private void CVScreenWnd_Load(object sender, EventArgs e)
        {
            InstallHook();
        }

        private void CVScreenWnd_FormClosed(object sender, FormClosedEventArgs e)
        {
            UninstallHook();
        }
        public CVScreenWnd(Rectangle location,Bitmap bitmap,string savepath=null):this(bitmap,savepath)
        {
            this.WindowState = FormWindowState.Normal;
            this.Width = location.Width;
            this.Height = location.Height;
            this.Location = new Point(location.X, location.Y);
        }
        internal void InstallHook()
        {
            UninstallHook();
            HookManager.KeyUp += HookManager_KeyUp;
        }
        internal void UninstallHook()
        {
            HookManager.KeyUp -= HookManager_KeyUp;
        }
        private void HookManager_KeyUp(object sender, KeyEventArgs e)
        {
            if(e.KeyCode==Keys.Escape)
            {
                UninstallHook();
                this.DialogResult = DialogResult.Cancel;
            }
        }
      private void Form2_MouseDown(object sender, MouseEventArgs e)
        {
            downX = e.X;
            downY = e.Y;
            isMouseDown = true;
            isDrawComplete = false;

            startPoint = new Point(downX, downY);
        }

        private void Form2_MouseUp(object sender, MouseEventArgs e)
        {
            isMouseDown = false;
            isDrawComplete = true;
            upX = e.X;
            upY = e.Y;
            endPoint = new Point(upX, upY);
           Point clientPoint= this.PointToScreen(e.Location);
            contextMenuStrip1.Show(clientPoint);
        }

        private void Form2_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDrawComplete)
            {
                Graphics g = this.CreateGraphics();
                g.DrawImage(maskBitmap, formalX < e.X ? formalX - 2 : e.X + 2, 0, new Rectangle(formalX < e.X ? formalX - 2 : e.X + 2, 0,
                    Math.Abs(e.X - formalX), Screen.PrimaryScreen.Bounds.Height), GraphicsUnit.Pixel);
                g.DrawImage(maskBitmap, 0, formalY < e.Y ? formalY - 2 : e.Y + 2, new Rectangle(0, formalY < e.Y ? formalY - 2 : e.Y + 2,
                    Screen.PrimaryScreen.Bounds.Width, Math.Abs(e.Y - formalY)), GraphicsUnit.Pixel);
                if (!isMouseDown)
                {
                    g.DrawLine(new Pen(Color.FromArgb(126, 167, 255), 2), e.X, 0, e.X, Screen.PrimaryScreen.Bounds.Height);
                    g.DrawLine(new Pen(Color.FromArgb(126, 167, 255), 2), 0, e.Y, Screen.PrimaryScreen.Bounds.Width, e.Y);
                }
                else
                {
                    g.DrawImage(bitmap, downX, downY, new Rectangle(downX, downY, e.X - downX, e.Y - downY), GraphicsUnit.Pixel);
                    g.DrawRectangle(new Pen(Color.FromArgb(126, 167, 255), 2), new Rectangle(downX < e.X ? downX : e.X, downY < e.Y ? downY : e.Y, Math.Abs(e.X - downX), Math.Abs(e.Y - downY)));
                }
                formalX = e.X;
                formalY = e.Y;
            }
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
            if (downX==upX||downY==upY)
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
            this.DialogResult = DialogResult.OK;
        }
    }
}