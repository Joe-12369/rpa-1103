using II.RPA.Plugin.GUI.Commons;
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Threading;

namespace II.RPA.Plugin.CV
{
    public partial class CVElementWnd : Form
    {
        private static DispatcherTimer dispatcherTimer = new DispatcherTimer();
        /// <summary>
        /// 四个边框
        /// </summary>
        private ExtendedPanel panelBorderLeft, panelBorderTop, panelBorderRight, panelBorderBottom;
        /// <summary>
        /// 内部边框
        /// </summary>
        private ExtendedPanel panelInside;

        CVObjs InspectObjs;
        string FilePath;
        public CVElementWnd(string filePath,CVObjs inspectObjs)
        {
            InitializeComponent();
            this.FilePath = filePath;
            Image img = Image.FromFile(filePath);
            Bitmap bitmap = new Bitmap(img);
            InspectObjs = inspectObjs;
            //this.BackgroundImage = bitmap;
            DrawLines(bitmap);
            dispatcherTimer.Tick -= new EventHandler(DispatcherTimer_Tick);
            dispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick);
            Color borderColor = Color.FromArgb(253, 46, 57);
            Color insideColor = Color.FromArgb(126, 167, 255);
            this.panelBorderLeft = CreatePanel(borderColor);
            this.panelBorderTop = CreatePanel(borderColor);
            this.panelBorderRight = CreatePanel(borderColor);
            this.panelBorderBottom = CreatePanel(borderColor);
            this.panelInside = CreatePanel(insideColor);
            this.FormClosing += CVElementWnd_FormClosing;
        }
        private void DrawLines(Bitmap bitmap)
        {
            Graphics g = Graphics.FromImage(bitmap);
            Pen p1 = new Pen(new SolidBrush(Color.Red));
            Pen p2 = new Pen(new SolidBrush(Color.Green));
            int index = 0;
            Font f = new Font(FontFamily.GenericSansSerif, 8);
           foreach(var shape in InspectObjs.TextResults)
            {
                PointF[] pf = new PointF[5];
                pf[0] = new PointF(shape.left_top[0], shape.left_top[1]);
                pf[1] = new PointF(shape.right_top[0], shape.right_top[1]);
                pf[2]= new PointF(shape.right_bottom[0], shape.right_bottom[1]);
                pf[3] = new PointF(shape.left_bottom[0], shape.left_bottom[1]);
                pf[4] = pf[0];
                g.DrawString(index.ToString(), f, new SolidBrush(Color.Blue), pf[2]);
                g.DrawLines(p1, pf);
                index++;
            }
            bitmap.Save(System.IO.Path.GetFileNameWithoutExtension(FilePath) + "_bk" + ".bmp");
            this.BackgroundImage = bitmap;
        }
        private void CVElementWnd_FormClosing(object sender, FormClosingEventArgs e)
        {
            dispatcherTimer.Tick -= new EventHandler(DispatcherTimer_Tick);
            dispatcherTimer.Stop();
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {

        }
        private ExtendedPanel CreatePanel(Color color)
        {
            var panel = new ExtendedPanel();
            panel.Size = new Size(0, 0);
            panel.Parent = this;
            panel.BackColor = color;
            panel.Show();
            return panel;
        }
    }

}
