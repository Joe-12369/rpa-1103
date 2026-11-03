namespace II.RPA.Plugin.Test
{
    partial class UsbHubForm
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
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnCloseChannel = new System.Windows.Forms.Button();
            this.txtChannel = new System.Windows.Forms.TextBox();
            this.btnOpenChannel = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.rtbResult = new System.Windows.Forms.RichTextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.cboComList = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btnGetCom = new System.Windows.Forms.Button();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.txtPort = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.txtIP = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.btnCloseNetChannel = new System.Windows.Forms.Button();
            this.txtNetChannel = new System.Windows.Forms.TextBox();
            this.btnAddNetChannel = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.rtbNetResult = new System.Windows.Forms.RichTextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.groupBox1);
            this.groupBox2.Controls.Add(this.rtbResult);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.cboComList);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.btnGetCom);
            this.groupBox2.Location = new System.Drawing.Point(7, 15);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox2.Size = new System.Drawing.Size(769, 332);
            this.groupBox2.TabIndex = 11;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "本地版USB Hub";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.btnCloseChannel);
            this.groupBox1.Controls.Add(this.txtChannel);
            this.groupBox1.Controls.Add(this.btnOpenChannel);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Location = new System.Drawing.Point(15, 79);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox1.Size = new System.Drawing.Size(340, 206);
            this.groupBox1.TabIndex = 16;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "操作";
            // 
            // btnCloseChannel
            // 
            this.btnCloseChannel.Location = new System.Drawing.Point(213, 125);
            this.btnCloseChannel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnCloseChannel.Name = "btnCloseChannel";
            this.btnCloseChannel.Size = new System.Drawing.Size(100, 29);
            this.btnCloseChannel.TabIndex = 10;
            this.btnCloseChannel.Text = "关闭通道";
            this.btnCloseChannel.UseVisualStyleBackColor = true;
            this.btnCloseChannel.Click += new System.EventHandler(this.btnCloseChannel_Click);
            // 
            // txtChannel
            // 
            this.txtChannel.Location = new System.Drawing.Point(63, 50);
            this.txtChannel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.txtChannel.Name = "txtChannel";
            this.txtChannel.Size = new System.Drawing.Size(268, 25);
            this.txtChannel.TabIndex = 8;
            // 
            // btnOpenChannel
            // 
            this.btnOpenChannel.Location = new System.Drawing.Point(63, 125);
            this.btnOpenChannel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnOpenChannel.Name = "btnOpenChannel";
            this.btnOpenChannel.Size = new System.Drawing.Size(100, 29);
            this.btnOpenChannel.TabIndex = 7;
            this.btnOpenChannel.Text = "打开通道";
            this.btnOpenChannel.UseVisualStyleBackColor = true;
            this.btnOpenChannel.Click += new System.EventHandler(this.btnOpenChannel_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(4, 54);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(60, 15);
            this.label3.TabIndex = 9;
            this.label3.Text = "通道号:";
            // 
            // rtbResult
            // 
            this.rtbResult.Location = new System.Drawing.Point(433, 28);
            this.rtbResult.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.rtbResult.Name = "rtbResult";
            this.rtbResult.Size = new System.Drawing.Size(327, 256);
            this.rtbResult.TabIndex = 15;
            this.rtbResult.Text = "";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(363, 28);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(63, 15);
            this.label2.TabIndex = 14;
            this.label2.Text = "Result:";
            // 
            // cboComList
            // 
            this.cboComList.FormattingEnabled = true;
            this.cboComList.Location = new System.Drawing.Point(59, 25);
            this.cboComList.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.cboComList.Name = "cboComList";
            this.cboComList.Size = new System.Drawing.Size(160, 23);
            this.cboComList.TabIndex = 13;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 31);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(39, 15);
            this.label1.TabIndex = 12;
            this.label1.Text = "Com:";
            // 
            // btnGetCom
            // 
            this.btnGetCom.Location = new System.Drawing.Point(228, 25);
            this.btnGetCom.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnGetCom.Name = "btnGetCom";
            this.btnGetCom.Size = new System.Drawing.Size(100, 29);
            this.btnGetCom.TabIndex = 11;
            this.btnGetCom.Text = "获取Com端口";
            this.btnGetCom.UseVisualStyleBackColor = true;
            this.btnGetCom.Click += new System.EventHandler(this.btnGetCom_Click);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.groupBox4);
            this.groupBox3.Controls.Add(this.rtbNetResult);
            this.groupBox3.Controls.Add(this.label5);
            this.groupBox3.Location = new System.Drawing.Point(7, 355);
            this.groupBox3.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox3.Size = new System.Drawing.Size(769, 332);
            this.groupBox3.TabIndex = 12;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "网络版USB Hub";
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.txtPort);
            this.groupBox4.Controls.Add(this.label7);
            this.groupBox4.Controls.Add(this.txtIP);
            this.groupBox4.Controls.Add(this.label6);
            this.groupBox4.Controls.Add(this.btnCloseNetChannel);
            this.groupBox4.Controls.Add(this.txtNetChannel);
            this.groupBox4.Controls.Add(this.btnAddNetChannel);
            this.groupBox4.Controls.Add(this.label4);
            this.groupBox4.Location = new System.Drawing.Point(15, 28);
            this.groupBox4.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox4.Size = new System.Drawing.Size(340, 258);
            this.groupBox4.TabIndex = 16;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "操作";
            // 
            // txtPort
            // 
            this.txtPort.Location = new System.Drawing.Point(75, 76);
            this.txtPort.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.txtPort.Name = "txtPort";
            this.txtPort.Size = new System.Drawing.Size(100, 25);
            this.txtPort.TabIndex = 14;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(8, 80);
            this.label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(60, 15);
            this.label7.TabIndex = 13;
            this.label7.Text = "端口号:";
            // 
            // txtIP
            // 
            this.txtIP.Location = new System.Drawing.Point(75, 39);
            this.txtIP.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.txtIP.Name = "txtIP";
            this.txtIP.Size = new System.Drawing.Size(237, 25);
            this.txtIP.TabIndex = 12;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(8, 42);
            this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(31, 15);
            this.label6.TabIndex = 11;
            this.label6.Text = "IP:";
            // 
            // btnCloseNetChannel
            // 
            this.btnCloseNetChannel.Location = new System.Drawing.Point(213, 196);
            this.btnCloseNetChannel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnCloseNetChannel.Name = "btnCloseNetChannel";
            this.btnCloseNetChannel.Size = new System.Drawing.Size(100, 29);
            this.btnCloseNetChannel.TabIndex = 10;
            this.btnCloseNetChannel.Text = "关闭通道";
            this.btnCloseNetChannel.UseVisualStyleBackColor = true;
            this.btnCloseNetChannel.Click += new System.EventHandler(this.btnCloseNetChannel_Click);
            // 
            // txtNetChannel
            // 
            this.txtNetChannel.Location = new System.Drawing.Point(75, 118);
            this.txtNetChannel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.txtNetChannel.Name = "txtNetChannel";
            this.txtNetChannel.Size = new System.Drawing.Size(100, 25);
            this.txtNetChannel.TabIndex = 8;
            // 
            // btnAddNetChannel
            // 
            this.btnAddNetChannel.Location = new System.Drawing.Point(63, 196);
            this.btnAddNetChannel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnAddNetChannel.Name = "btnAddNetChannel";
            this.btnAddNetChannel.Size = new System.Drawing.Size(100, 29);
            this.btnAddNetChannel.TabIndex = 7;
            this.btnAddNetChannel.Text = "打开通道";
            this.btnAddNetChannel.UseVisualStyleBackColor = true;
            this.btnAddNetChannel.Click += new System.EventHandler(this.btnAddNetChannel_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(4, 121);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(60, 15);
            this.label4.TabIndex = 9;
            this.label4.Text = "通道号:";
            // 
            // rtbNetResult
            // 
            this.rtbNetResult.Location = new System.Drawing.Point(433, 28);
            this.rtbNetResult.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.rtbNetResult.Name = "rtbNetResult";
            this.rtbNetResult.Size = new System.Drawing.Size(327, 256);
            this.rtbNetResult.TabIndex = 15;
            this.rtbNetResult.Text = "";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(363, 28);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(63, 15);
            this.label5.TabIndex = 14;
            this.label5.Text = "Result:";
            // 
            // UsbHubForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(789, 719);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "UsbHubForm";
            this.Text = "UsbHubForm";
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btnCloseChannel;
        private System.Windows.Forms.TextBox txtChannel;
        private System.Windows.Forms.Button btnOpenChannel;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.RichTextBox rtbResult;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cboComList;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnGetCom;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.TextBox txtPort;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox txtIP;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button btnCloseNetChannel;
        private System.Windows.Forms.TextBox txtNetChannel;
        private System.Windows.Forms.Button btnAddNetChannel;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.RichTextBox rtbNetResult;
        private System.Windows.Forms.Label label5;
    }
}