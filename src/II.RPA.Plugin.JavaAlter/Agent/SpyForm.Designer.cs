namespace II.RPA.Plugin.JavaAlter.Agent
{
    partial class SpyForm
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SpyForm));
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.refreshButton = new System.Windows.Forms.ToolStripButton();
            this.traceButton = new System.Windows.Forms.Button();
            this.elementPath = new System.Windows.Forms.TextBox();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.statusMsg = new System.Windows.Forms.ToolStripStatusLabel();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.elementTree = new System.Windows.Forms.TreeView();
            this.btn_comp_reac = new System.Windows.Forms.Button();
            this.btn_comp_click = new System.Windows.Forms.Button();
            this.btn_comp_tableSize = new System.Windows.Forms.Button();
            this.btn_comp_exist = new System.Windows.Forms.Button();
            this.btn_comp_start = new System.Windows.Forms.Button();
            this.elementAttr = new System.Windows.Forms.RichTextBox();
            this.elementImg = new System.Windows.Forms.PictureBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.btnVerify = new System.Windows.Forms.Button();
            this.toolTip2 = new System.Windows.Forms.ToolTip(this.components);
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.显示ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.退出ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.chkPrint = new System.Windows.Forms.CheckBox();
            this.btn_edit = new System.Windows.Forms.Button();
            this.toolStrip1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.elementImg)).BeginInit();
            this.panel1.SuspendLayout();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.BackColor = System.Drawing.Color.Transparent;
            this.toolStrip1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.refreshButton});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(682, 25);
            this.toolStrip1.TabIndex = 3;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // refreshButton
            // 
            this.refreshButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.refreshButton.Image = ((System.Drawing.Image)(resources.GetObject("refreshButton.Image")));
            this.refreshButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.refreshButton.Name = "refreshButton";
            this.refreshButton.Size = new System.Drawing.Size(36, 22);
            this.refreshButton.Text = "刷新";
            this.refreshButton.Click += new System.EventHandler(this.refreshButton_Click);
            // 
            // traceButton
            // 
            this.traceButton.AllowDrop = true;
            this.traceButton.BackgroundImage = global::JavaSpy.Properties.Resources.定位;
            this.traceButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.traceButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.traceButton.FlatAppearance.BorderSize = 0;
            this.traceButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.traceButton.Location = new System.Drawing.Point(12, 25);
            this.traceButton.Name = "traceButton";
            this.traceButton.Size = new System.Drawing.Size(24, 24);
            this.traceButton.TabIndex = 4;
            this.traceButton.UseVisualStyleBackColor = true;
            this.traceButton.MouseHover += new System.EventHandler(this.traceButton_MouseHover);
            this.traceButton.MouseUp += new System.Windows.Forms.MouseEventHandler(this.traceButton_MouseUp);
            // 
            // elementPath
            // 
            this.elementPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.elementPath.Location = new System.Drawing.Point(42, 28);
            this.elementPath.Name = "elementPath";
            this.elementPath.Size = new System.Drawing.Size(488, 21);
            this.elementPath.TabIndex = 5;
            this.elementPath.Click += new System.EventHandler(this.elementPath_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusMsg});
            this.statusStrip1.Location = new System.Drawing.Point(0, 591);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(682, 22);
            this.statusStrip1.TabIndex = 8;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // statusMsg
            // 
            this.statusMsg.Name = "statusMsg";
            this.statusMsg.Size = new System.Drawing.Size(0, 17);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.elementTree);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.chkPrint);
            this.splitContainer1.Panel2.Controls.Add(this.btn_comp_reac);
            this.splitContainer1.Panel2.Controls.Add(this.btn_comp_click);
            this.splitContainer1.Panel2.Controls.Add(this.btn_comp_tableSize);
            this.splitContainer1.Panel2.Controls.Add(this.btn_comp_exist);
            this.splitContainer1.Panel2.Controls.Add(this.btn_comp_start);
            this.splitContainer1.Panel2.Controls.Add(this.elementAttr);
            this.splitContainer1.Panel2.Controls.Add(this.elementImg);
            this.splitContainer1.Size = new System.Drawing.Size(658, 526);
            this.splitContainer1.SplitterDistance = 278;
            this.splitContainer1.TabIndex = 11;
            // 
            // elementTree
            // 
            this.elementTree.Dock = System.Windows.Forms.DockStyle.Fill;
            this.elementTree.Location = new System.Drawing.Point(0, 0);
            this.elementTree.Name = "elementTree";
            this.elementTree.Size = new System.Drawing.Size(278, 526);
            this.elementTree.TabIndex = 6;
            this.elementTree.BeforeSelect += new System.Windows.Forms.TreeViewCancelEventHandler(this.elementTree_BeforeSelect);
            this.elementTree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.elementTree_AfterSelect);
            // 
            // btn_comp_reac
            // 
            this.btn_comp_reac.Location = new System.Drawing.Point(198, 264);
            this.btn_comp_reac.Name = "btn_comp_reac";
            this.btn_comp_reac.Size = new System.Drawing.Size(146, 23);
            this.btn_comp_reac.TabIndex = 10;
            this.btn_comp_reac.Tag = "rect";
            this.btn_comp_reac.Text = "生成获取元素区域组件";
            this.btn_comp_reac.UseVisualStyleBackColor = true;
            this.btn_comp_reac.Click += new System.EventHandler(this.btn_comp_Click);
            // 
            // btn_comp_click
            // 
            this.btn_comp_click.Location = new System.Drawing.Point(198, 235);
            this.btn_comp_click.Name = "btn_comp_click";
            this.btn_comp_click.Size = new System.Drawing.Size(146, 23);
            this.btn_comp_click.TabIndex = 10;
            this.btn_comp_click.Tag = "click";
            this.btn_comp_click.Text = "生成点击元素组件";
            this.btn_comp_click.UseVisualStyleBackColor = true;
            this.btn_comp_click.Click += new System.EventHandler(this.btn_comp_Click);
            // 
            // btn_comp_tableSize
            // 
            this.btn_comp_tableSize.Location = new System.Drawing.Point(15, 293);
            this.btn_comp_tableSize.Name = "btn_comp_tableSize";
            this.btn_comp_tableSize.Size = new System.Drawing.Size(153, 23);
            this.btn_comp_tableSize.TabIndex = 10;
            this.btn_comp_tableSize.Tag = "tableSize";
            this.btn_comp_tableSize.Text = "生成获取表格大小组件";
            this.btn_comp_tableSize.UseVisualStyleBackColor = true;
            this.btn_comp_tableSize.Click += new System.EventHandler(this.btn_comp_Click);
            // 
            // btn_comp_exist
            // 
            this.btn_comp_exist.Location = new System.Drawing.Point(15, 264);
            this.btn_comp_exist.Name = "btn_comp_exist";
            this.btn_comp_exist.Size = new System.Drawing.Size(153, 23);
            this.btn_comp_exist.TabIndex = 10;
            this.btn_comp_exist.Tag = "exist";
            this.btn_comp_exist.Text = "生成判断元素组件";
            this.btn_comp_exist.UseVisualStyleBackColor = true;
            this.btn_comp_exist.Click += new System.EventHandler(this.btn_comp_Click);
            // 
            // btn_comp_start
            // 
            this.btn_comp_start.Location = new System.Drawing.Point(15, 235);
            this.btn_comp_start.Name = "btn_comp_start";
            this.btn_comp_start.Size = new System.Drawing.Size(153, 23);
            this.btn_comp_start.TabIndex = 10;
            this.btn_comp_start.Tag = "service";
            this.btn_comp_start.Text = "生成启动服务组件";
            this.btn_comp_start.UseVisualStyleBackColor = true;
            this.btn_comp_start.Click += new System.EventHandler(this.btn_comp_Click);
            // 
            // elementAttr
            // 
            this.elementAttr.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.elementAttr.Location = new System.Drawing.Point(-1, 3);
            this.elementAttr.Name = "elementAttr";
            this.elementAttr.Size = new System.Drawing.Size(377, 212);
            this.elementAttr.TabIndex = 7;
            this.elementAttr.Text = "";
            // 
            // elementImg
            // 
            this.elementImg.Location = new System.Drawing.Point(3, 361);
            this.elementImg.Name = "elementImg";
            this.elementImg.Size = new System.Drawing.Size(360, 165);
            this.elementImg.TabIndex = 9;
            this.elementImg.TabStop = false;
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.Controls.Add(this.splitContainer1);
            this.panel1.Location = new System.Drawing.Point(12, 61);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(658, 526);
            this.panel1.TabIndex = 10;
            // 
            // toolTip1
            // 
            this.toolTip1.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            this.toolTip1.ToolTipTitle = "鼠标拖动图标到需要的元素上";
            // 
            // btnVerify
            // 
            this.btnVerify.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnVerify.Location = new System.Drawing.Point(611, 28);
            this.btnVerify.Name = "btnVerify";
            this.btnVerify.Size = new System.Drawing.Size(59, 23);
            this.btnVerify.TabIndex = 11;
            this.btnVerify.Text = "校验";
            this.btnVerify.UseVisualStyleBackColor = true;
            this.btnVerify.Click += new System.EventHandler(this.btnVerify_Click);
            this.btnVerify.MouseHover += new System.EventHandler(this.btnVerify_MouseHover);
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.ContextMenuStrip = this.contextMenuStrip1;
            this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            this.notifyIcon1.Text = "实在智能-Java 拾取程序";
            this.notifyIcon1.Visible = true;
            this.notifyIcon1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon1_MouseDoubleClick);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.显示ToolStripMenuItem,
            this.退出ToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(101, 48);
            // 
            // 显示ToolStripMenuItem
            // 
            this.显示ToolStripMenuItem.Name = "显示ToolStripMenuItem";
            this.显示ToolStripMenuItem.Size = new System.Drawing.Size(100, 22);
            this.显示ToolStripMenuItem.Text = "显示";
            this.显示ToolStripMenuItem.Click += new System.EventHandler(this.显示ToolStripMenuItem_Click);
            // 
            // 退出ToolStripMenuItem
            // 
            this.退出ToolStripMenuItem.Name = "退出ToolStripMenuItem";
            this.退出ToolStripMenuItem.Size = new System.Drawing.Size(100, 22);
            this.退出ToolStripMenuItem.Text = "退出";
            this.退出ToolStripMenuItem.Click += new System.EventHandler(this.退出ToolStripMenuItem_Click);
            // 
            // chkPrint
            // 
            this.chkPrint.AutoSize = true;
            this.chkPrint.Checked = true;
            this.chkPrint.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkPrint.Location = new System.Drawing.Point(15, 339);
            this.chkPrint.Name = "chkPrint";
            this.chkPrint.Size = new System.Drawing.Size(72, 16);
            this.chkPrint.TabIndex = 11;
            this.chkPrint.Text = "打印变量";
            this.chkPrint.UseVisualStyleBackColor = true;
            // 
            // btn_edit
            // 
            this.btn_edit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_edit.Location = new System.Drawing.Point(536, 28);
            this.btn_edit.Name = "btn_edit";
            this.btn_edit.Size = new System.Drawing.Size(59, 23);
            this.btn_edit.TabIndex = 12;
            this.btn_edit.Text = "编辑";
            this.btn_edit.UseVisualStyleBackColor = true;
            this.btn_edit.Click += new System.EventHandler(this.btn_edit_Click);
            // 
            // SpyForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(682, 613);
            this.ContextMenuStrip = this.contextMenuStrip1;
            this.Controls.Add(this.btn_edit);
            this.Controls.Add(this.btnVerify);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.elementPath);
            this.Controls.Add(this.traceButton);
            this.Controls.Add(this.toolStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "SpyForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Java元素拾取辅助工具  -- 有问题 @实在智能-文一";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SpyForm_FormClosing);
            this.Load += new System.EventHandler(this.FormLoad);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.elementImg)).EndInit();
            this.panel1.ResumeLayout(false);
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStripButton refreshButton;
        private System.Windows.Forms.Button traceButton;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.TextBox elementPath;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel statusMsg;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TreeView elementTree;
        private System.Windows.Forms.RichTextBox elementAttr;
        private System.Windows.Forms.PictureBox elementImg;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Button btnVerify;
        private System.Windows.Forms.ToolTip toolTip2;
        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem 显示ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 退出ToolStripMenuItem;
        private System.Windows.Forms.Button btn_comp_click;
        private System.Windows.Forms.Button btn_comp_tableSize;
        private System.Windows.Forms.Button btn_comp_exist;
        private System.Windows.Forms.Button btn_comp_start;
        private System.Windows.Forms.Button btn_comp_reac;
        private System.Windows.Forms.CheckBox chkPrint;
        private System.Windows.Forms.Button btn_edit;
    }
}

