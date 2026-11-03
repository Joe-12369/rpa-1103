using Gma.UserActivityMonitor;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.GUI
{
    public partial class InspectorStatusForm : Form
    {
        DateTime _lastKeyDownTime;
        List<Keys> _hookKeys;
        private static ILogger logger = Logger.OfContext();

        public InspectorStatusForm()
        {
            InitializeComponent();
            this.Location = new Point(0, 0);
            var screenPoint = System.Windows.Forms.Cursor.Position;
            label1.Text = $"({screenPoint.X.ToString()}, {screenPoint.Y.ToString()})";
            _lastKeyDownTime = DateTime.Now;
            _hookKeys = new List<Keys>() { Keys.Escape, Keys.F2, Keys.F3 };
        }
        public InspectorStatusForm(string sceneName)
        {
            InitializeComponent();
            this.Location = new Point(0, 0);
            var screenPoint = System.Windows.Forms.Cursor.Position;
            label1.Text = $"({screenPoint.X.ToString()}, {screenPoint.Y.ToString()})";
            _lastKeyDownTime = DateTime.Now;
            _hookKeys = new List<Keys>() { Keys.Escape, Keys.F2, Keys.F3 };
            ChangeCaptureStatusStyle(sceneName);
        }
        private bool isPause = false;
        public bool IsPause
        {
            get { return isPause; }
            set { isPause = value; }
        }
        private void HookManager_KeyUp(object sender, KeyEventArgs e)
        {
            var span = (DateTime.Now - _lastKeyDownTime).TotalMilliseconds;
            _lastKeyDownTime = DateTime.Now;
            if (span < 200)
            {
                e.Handled = _hookKeys.Contains(e.KeyCode);
                return;
            }
            switch (e.KeyCode)
            {
                case Keys.Escape:
                    //OverlayForm.CurrentRemoteCaptureClient.EndRemoteCapture();
                    GuiElement.overlayForm.KeyCancel();
                    break;
                case Keys.F2:
                    if (GuiElement.overlayForm.isCVParsing)
                    {
                        return;
                    }
                    GuiElement.overlayForm.IsCVCapture = false;
                    GuiElement.overlayForm.CurrentCaptureMode++;
                    ChangeCaptureStatusStyle();
                    e.Handled = true;
                    break;
                case Keys.F3:
                    if (GuiElement.overlayForm.isCVParsing)
                    {
                        return;
                    }
                    GuiElement.overlayForm.IsCVCapture = true;
                    GuiElement.overlayForm.CurrentCaptureMode = CaptureModeType.CV;
                    ChangeCaptureStatusStyle();
                    e.Handled = true;
                    break;
                default:
                    break;
            }
        }
        public void SetCaptureMsg(string msg)
        {
            try
            {
                if (l_captureMsg.InvokeRequired)
                {
                    l_captureMsg.Invoke((System.Windows.Forms.MethodInvoker)delegate
                    {
                        SetCaptureMsg(msg);
                    });
                    return;
                }
                if (string.IsNullOrEmpty(msg))
                {
                    l_captureMsg.Hide();
                }
                else
                {
                    l_captureMsg.Show();
                }
            }
            catch (ObjectDisposedException ex)
            {
            }
            //l_captureMsg.Text = msg;
        }
        public void HideSwitchPanle()
        {
            if (p_switchPanle.Visible)
            {
                this.Height -= p_switchPanle.Height;
                p_switchPanle.Hide();
            }
        }
        public void ChangeCaptureStatusStyle(string sceneName = null)
        {
            if (l_CommonCapture.InvokeRequired)
            {
                l_CommonCapture.Invoke((System.Windows.Forms.MethodInvoker)delegate
                {
                    ChangeCaptureStatusStyle(sceneName = null);
                });
                return;
            }
            if (sceneName == "区域拾取")
            {
                label_TypeValue.Text = "--";
                l_curCaptureName.Text = sceneName;
                l_singleClick.Text = "区域";
                this.Height -= p_switchPanle.Height;
                p_switchPanle.Hide();
                return;
            }

            //调整状态窗口大小，CV模式时，不显示当前拾取模式
            //if (GuiElement.overlayForm.IsCVCapture)
            //{
            //    if (p_switchPanle.Visible)
            //    {
            //        this.Height -= p_switchPanle.Height;
            //        p_switchPanle.Hide();
            //    }
            //    return;
            //}
            //else if (!p_switchPanle.Visible)
            //{
            //    p_switchPanle.Show();
            //    this.Height = 199;
            //    //this.Height += p_switchPanle.Height;
            //}

            //高亮显示当前拾取模式
            switch (GuiElement.overlayForm.CurrentCaptureMode)
            {
                case CaptureModeType.Intelligence:
                    l_curCaptureName.Text = "智能拾取";
                    l_ineedCapture.ForeColor = Color.FromArgb(255, 255, 255);
                    l_CommonCapture.ForeColor = Color.FromArgb(199, 199, 199);
                    l_FloatingCapute.ForeColor = Color.FromArgb(199, 199, 199);
                    break;
                case CaptureModeType.Ordinary:
                    l_curCaptureName.Text = "普通拾取";
                    l_ineedCapture.ForeColor = Color.FromArgb(199, 199, 199);
                    l_CommonCapture.ForeColor = Color.FromArgb(255, 255, 255);
                    l_FloatingCapute.ForeColor = Color.FromArgb(199, 199, 199);
                    break;
                case CaptureModeType.FloatWnd:
                    l_curCaptureName.Text = "浮窗拾取";
                    l_ineedCapture.ForeColor = Color.FromArgb(199, 199, 199);
                    l_CommonCapture.ForeColor = Color.FromArgb(199, 199, 199);
                    l_FloatingCapute.ForeColor = Color.FromArgb(255, 255, 255);
                    break;
                case CaptureModeType.CV:
                    label_TypeValue.Text = "--";
                    l_curCaptureName.Text = "CV拾取";
                    l_ineedCapture.ForeColor = Color.FromArgb(199, 199, 199);
                    l_CommonCapture.ForeColor = Color.FromArgb(199, 199, 199);
                    l_FloatingCapute.ForeColor = Color.FromArgb(199, 199, 199);
                    break;
            }
        }
        internal void InstallHook()
        {
            UninstallHook();
            HookManager.KeyDown += HookManager_KeyUp;

        }

        internal void UninstallHook()
        {
            HookManager.KeyDown -= HookManager_KeyUp;
        }
        private void InspectorStatusForm_Load(object sender, System.EventArgs e)
        {
            InstallHook();
            float size = 13.00F / NativeMethods.DpiX * 72;
            //this.Size = new Size(200, 160);
            //label1.Font = new System.Drawing.Font("Microsoft YaHei,Helvetica Neue,Helvetica,Arial,sans-serif,Segoe UI", size, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            //label_Local.Font = new System.Drawing.Font("Microsoft YaHei,Helvetica Neue,Helvetica,Arial,sans-serif,Segoe UI", size, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            //label_Status.Font = new System.Drawing.Font("Microsoft YaHei,Helvetica Neue,Helvetica,Arial,sans-serif,Segoe UI", size, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            //l_curCaptureName.Font = new System.Drawing.Font("Microsoft YaHei,Helvetica Neue,Helvetica,Arial,sans-serif,Segoe UI", size, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            //label3.Font = new System.Drawing.Font("Microsoft YaHei,Helvetica Neue,Helvetica,Arial,sans-serif,Segoe UI", size, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            //label_TypeValue.Font = new System.Drawing.Font("Microsoft YaHei,Helvetica Neue,Helvetica,Arial,sans-serif,Segoe UI", size, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            //label1.Font = new System.Drawing.Font("Microsoft YaHei,Helvetica Neue,Helvetica,Arial,sans-serif,Segoe UI", size, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            //label_F2.Font = new System.Drawing.Font("Microsoft YaHei,Helvetica Neue,Helvetica,Arial,sans-serif,Segoe UI", size, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            //label_Esc.Font = new System.Drawing.Font("Microsoft YaHei,Helvetica Neue,Helvetica,Arial,sans-serif,Segoe UI", size, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            //label_Type.Font = new System.Drawing.Font("Microsoft YaHei,Helvetica Neue,Helvetica,Arial,sans-serif,Segoe UI", size, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            //return;
            foreach (var item in this.Controls)
            {
                if (item is Label)
                {
                    Label lab = ((Label)item);
                    var unit = lab.Font.Unit;

                    if (lab.Font.Unit == GraphicsUnit.Pixel)
                    {
                        size = lab.Font.Size / NativeMethods.DpiX * 96;
                    }
                    else
                    {
                        size = lab.Font.Size / NativeMethods.DpiX * 72;
                    }
                    lab.Font = new System.Drawing.Font(lab.Font.FontFamily, size, lab.Font.Style, lab.Font.Unit, ((byte)(0)));
                    //((Label)item).Font = new System.Drawing.Font("Microsoft YaHei,Helvetica Neue,Helvetica,Arial,sans-serif,Segoe UI", size, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                }
            }
            foreach (var item in this.p_switchPanle.Controls)
            {
                if (item is Label)
                {
                    Label lab = ((Label)item);
                    var unit = lab.Font.Unit;

                    if (lab.Font.Unit == GraphicsUnit.Pixel)
                    {
                        size = lab.Font.Size / NativeMethods.DpiX * 96;
                    }
                    else
                    {
                        size = lab.Font.Size / NativeMethods.DpiX * 72;
                    }
                    lab.Font = new System.Drawing.Font(lab.Font.FontFamily, size, lab.Font.Style, lab.Font.Unit, ((byte)(0)));
                }
            }
        }
        public void MoveFromLocation(Point point)
        {
            var scale = (float)NativeMethods.DpiX / 96;
            Size OutTaskBarSize = new Size(SystemInformation.WorkingArea.Width, SystemInformation.WorkingArea.Height);
            Size ScreenSize = new Size(System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height);
            Size TaskBarSize;
            TaskBarSize = new Size(
                           ScreenSize.Width - (ScreenSize.Width - OutTaskBarSize.Width),
                            ScreenSize.Height - OutTaskBarSize.Height
                            );
            var ScreenArea = System.Windows.Forms.Screen.FromPoint(point).WorkingArea;
            if (point.X < this.Width + 5 && point.Y < this.Height + 5)
            {

                //窗口在右下角显示
                int x = ScreenArea.Width - this.Width;
                int y = ScreenArea.Height - this.Height;
                this.Location = new Point(x, y);
            }
            else if (point.X > ScreenArea.Width - this.Width - 5
                && point.Y > ScreenArea.Height - this.Height - 5)
            {
                this.Location = new Point(0, 0);
            }
            label1.Text = $"({point.X.ToString()}, {point.Y.ToString()})";
        }
        public void WindowType(string value)
        {
            try
            {
                label_TypeValue.Text = value;
            }
            catch (Exception ex)
            {
                label_TypeValue.Text = "Window";
                logger.LogError(ex);
            }
        }

        private void InspectorStatusForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            UninstallHook();
        }
    }
}

