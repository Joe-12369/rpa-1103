namespace II.RPA.DataCapture
{
    partial class DataCaptureForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DataCaptureForm));
            this.webBrowser1 = new System.Windows.Forms.WebBrowser();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.label1 = new System.Windows.Forms.Label();
            this.pictureBoxClose = new System.Windows.Forms.PictureBox();
            this.pictureBoxMini = new System.Windows.Forms.PictureBox();
            this.pictureBoxClose2 = new System.Windows.Forms.PictureBox();
            this.pictureBoxMini2 = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxClose)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxMini)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxClose2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxMini2)).BeginInit();
            this.SuspendLayout();
            // 
            // webBrowser1
            // 
            this.webBrowser1.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
            this.webBrowser1.Location = new System.Drawing.Point(18, 92);
            this.webBrowser1.Margin = new System.Windows.Forms.Padding(0);
            this.webBrowser1.MinimumSize = new System.Drawing.Size(25, 27);
            this.webBrowser1.Name = "webBrowser1";
            this.webBrowser1.ScriptErrorsSuppressed = true;
            this.webBrowser1.Size = new System.Drawing.Size(931, 70);
            this.webBrowser1.TabIndex = 1;
            this.webBrowser1.DocumentCompleted += new System.Windows.Forms.WebBrowserDocumentCompletedEventHandler(this.DocumentCompleted);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(403, 17);
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(32, 32);
            this.pictureBox1.TabIndex = 2;
            this.pictureBox1.TabStop = false;
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("微软雅黑", 9.2F);
            this.label1.Location = new System.Drawing.Point(522, 48);
            this.label1.Margin = new System.Windows.Forms.Padding(0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(60, 23);
            this.label1.TabIndex = 3;
            this.label1.Text = "label1";
            // 
            // pictureBoxClose
            // 
            this.pictureBoxClose.Image = ((System.Drawing.Image)(resources.GetObject("pictureBoxClose.Image")));
            this.pictureBoxClose.Location = new System.Drawing.Point(696, 17);
            this.pictureBoxClose.Margin = new System.Windows.Forms.Padding(0);
            this.pictureBoxClose.Name = "pictureBoxClose";
            this.pictureBoxClose.Size = new System.Drawing.Size(32, 32);
            this.pictureBoxClose.TabIndex = 4;
            this.pictureBoxClose.TabStop = false;
            this.pictureBoxClose.Click += new System.EventHandler(this.DataCaptureClose);
            this.pictureBoxClose.MouseEnter += new System.EventHandler(this.pictureBoxClose_MouseEnter);
            this.pictureBoxClose.MouseLeave += new System.EventHandler(this.pictureBoxClose_MouseLeave);
            this.pictureBoxClose.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pictureBoxClose_MouseMove);
            // 
            // pictureBoxMini
            // 
            this.pictureBoxMini.Image = ((System.Drawing.Image)(resources.GetObject("pictureBoxMini.Image")));
            this.pictureBoxMini.Location = new System.Drawing.Point(835, 17);
            this.pictureBoxMini.Margin = new System.Windows.Forms.Padding(0);
            this.pictureBoxMini.Name = "pictureBoxMini";
            this.pictureBoxMini.Size = new System.Drawing.Size(32, 32);
            this.pictureBoxMini.TabIndex = 5;
            this.pictureBoxMini.TabStop = false;
            this.pictureBoxMini.Click += new System.EventHandler(this.DataCaptureMini);
            this.pictureBoxMini.MouseEnter += new System.EventHandler(this.pictureBoxMini_MouseEnter);
            this.pictureBoxMini.MouseLeave += new System.EventHandler(this.pictureBoxMini_MouseLeave);
            this.pictureBoxMini.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pictureBoxMini_MouseMove);
            // 
            // pictureBoxClose2
            // 
            this.pictureBoxClose2.Location = new System.Drawing.Point(631, 17);
            this.pictureBoxClose2.Margin = new System.Windows.Forms.Padding(0);
            this.pictureBoxClose2.Name = "pictureBoxClose2";
            this.pictureBoxClose2.Size = new System.Drawing.Size(39, 32);
            this.pictureBoxClose2.TabIndex = 6;
            this.pictureBoxClose2.TabStop = false;
            this.pictureBoxClose2.Click += new System.EventHandler(this.DataCaptureClose);
            // 
            // pictureBoxMini2
            // 
            this.pictureBoxMini2.Location = new System.Drawing.Point(765, 9);
            this.pictureBoxMini2.Margin = new System.Windows.Forms.Padding(0);
            this.pictureBoxMini2.Name = "pictureBoxMini2";
            this.pictureBoxMini2.Size = new System.Drawing.Size(44, 36);
            this.pictureBoxMini2.TabIndex = 7;
            this.pictureBoxMini2.TabStop = false;
            this.pictureBoxMini2.Click += new System.EventHandler(this.DataCaptureMini);
            // 
            // DataCaptureForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(13F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(980, 213);
            this.Controls.Add(this.pictureBoxMini2);
            this.Controls.Add(this.pictureBoxClose2);
            this.Controls.Add(this.pictureBoxMini);
            this.Controls.Add(this.pictureBoxClose);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.webBrowser1);
            this.Font = new System.Drawing.Font("宋体", 15F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "DataCaptureForm";
            this.Opacity = 0D;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "数据采集";
            this.TopMost = true;
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.DataCaptureForm_FormClosed);
            this.Load += new System.EventHandler(this.DataCaptureForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxClose)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxMini)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxClose2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxMini2)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.WebBrowser webBrowser1;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.PictureBox pictureBoxClose;
        private System.Windows.Forms.PictureBox pictureBoxMini;
        private System.Windows.Forms.PictureBox pictureBoxClose2;
        private System.Windows.Forms.PictureBox pictureBoxMini2;
    }
}