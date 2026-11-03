namespace II.RPA.EventMonitor
{
    partial class TestForm
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
            this.button1 = new System.Windows.Forms.Button();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.cbList_eventTypes = new System.Windows.Forms.CheckedListBox();
            this.txt_filePath = new System.Windows.Forms.TextBox();
            this.txt_fileFilter = new System.Windows.Forms.TextBox();
            this.cb_containChilds = new System.Windows.Forms.CheckBox();
            this.button2 = new System.Windows.Forms.Button();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.btn_sendMail = new System.Windows.Forms.Button();
            this.button6 = new System.Windows.Forms.Button();
            this.button4 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.radioButton2 = new System.Windows.Forms.RadioButton();
            this.rd_mailCondition_all = new System.Windows.Forms.RadioButton();
            this.cmb_mialprotocol = new System.Windows.Forms.ComboBox();
            this.num_MailInterval = new System.Windows.Forms.NumericUpDown();
            this.label6 = new System.Windows.Forms.Label();
            this.txt_bodyContent = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.txt_mailPwd = new System.Windows.Forms.TextBox();
            this.txt_themeContent = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.txt_account = new System.Windows.Forms.TextBox();
            this.txt_recAddrContent = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.txt_mailPort = new System.Windows.Forms.TextBox();
            this.txt_sendAddrContent = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.txt_mailHost = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.txt_hotkey_name = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.cmb_hotkey_keytype = new System.Windows.Forms.ComboBox();
            this.cb_hotkey_win = new System.Windows.Forms.CheckBox();
            this.cb_hotkey_shift = new System.Windows.Forms.CheckBox();
            this.cb_hotkey_ctrl = new System.Windows.Forms.CheckBox();
            this.cb_hotkey_alt = new System.Windows.Forms.CheckBox();
            this.btn_hotkey_stop = new System.Windows.Forms.Button();
            this.btn_hotkey_start = new System.Windows.Forms.Button();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.txt_mouse_name = new System.Windows.Forms.TextBox();
            this.label14 = new System.Windows.Forms.Label();
            this.com_mouse_mousetype = new System.Windows.Forms.ComboBox();
            this.cb_mouse_win = new System.Windows.Forms.CheckBox();
            this.cb_mouse_shift = new System.Windows.Forms.CheckBox();
            this.cb_mouse_ctrl = new System.Windows.Forms.CheckBox();
            this.cb_mouse_alt = new System.Windows.Forms.CheckBox();
            this.btn_mouse_stop = new System.Windows.Forms.Button();
            this.btn_mouse_start = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.num_MailInterval)).BeginInit();
            this.tabPage3.SuspendLayout();
            this.tabPage4.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(114, 143);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(145, 23);
            this.button1.TabIndex = 0;
            this.button1.Text = "开始文件监控";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // richTextBox1
            // 
            this.richTextBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBox1.Location = new System.Drawing.Point(3, 21);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(864, 354);
            this.richTextBox1.TabIndex = 1;
            this.richTextBox1.Text = "";
            // 
            // cbList_eventTypes
            // 
            this.cbList_eventTypes.FormattingEnabled = true;
            this.cbList_eventTypes.Items.AddRange(new object[] {
            "全部",
            "创建",
            "删除",
            "更新",
            "重命名"});
            this.cbList_eventTypes.Location = new System.Drawing.Point(21, 42);
            this.cbList_eventTypes.Name = "cbList_eventTypes";
            this.cbList_eventTypes.Size = new System.Drawing.Size(87, 124);
            this.cbList_eventTypes.TabIndex = 2;
            // 
            // txt_filePath
            // 
            this.txt_filePath.Location = new System.Drawing.Point(114, 42);
            this.txt_filePath.Name = "txt_filePath";
            this.txt_filePath.Size = new System.Drawing.Size(288, 25);
            this.txt_filePath.TabIndex = 3;
            this.txt_filePath.Text = "D:\\fileTriggerTest";
            // 
            // txt_fileFilter
            // 
            this.txt_fileFilter.Location = new System.Drawing.Point(114, 79);
            this.txt_fileFilter.Name = "txt_fileFilter";
            this.txt_fileFilter.Size = new System.Drawing.Size(288, 25);
            this.txt_fileFilter.TabIndex = 3;
            this.txt_fileFilter.Text = "*.*";
            // 
            // cb_containChilds
            // 
            this.cb_containChilds.AutoSize = true;
            this.cb_containChilds.Location = new System.Drawing.Point(115, 118);
            this.cb_containChilds.Name = "cb_containChilds";
            this.cb_containChilds.Size = new System.Drawing.Size(119, 19);
            this.cb_containChilds.TabIndex = 4;
            this.cb_containChilds.Text = "是否监控子级";
            this.cb_containChilds.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(281, 143);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(121, 23);
            this.button2.TabIndex = 5;
            this.button2.Text = "重新监控";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Controls.Add(this.tabPage4);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Top;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(870, 246);
            this.tabControl1.TabIndex = 7;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.cbList_eventTypes);
            this.tabPage1.Controls.Add(this.txt_filePath);
            this.tabPage1.Controls.Add(this.button2);
            this.tabPage1.Controls.Add(this.txt_fileFilter);
            this.tabPage1.Controls.Add(this.button1);
            this.tabPage1.Controls.Add(this.cb_containChilds);
            this.tabPage1.Location = new System.Drawing.Point(4, 25);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(862, 217);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "文件监控";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.btn_sendMail);
            this.tabPage2.Controls.Add(this.button6);
            this.tabPage2.Controls.Add(this.button4);
            this.tabPage2.Controls.Add(this.button3);
            this.tabPage2.Controls.Add(this.radioButton2);
            this.tabPage2.Controls.Add(this.rd_mailCondition_all);
            this.tabPage2.Controls.Add(this.cmb_mialprotocol);
            this.tabPage2.Controls.Add(this.num_MailInterval);
            this.tabPage2.Controls.Add(this.label6);
            this.tabPage2.Controls.Add(this.txt_bodyContent);
            this.tabPage2.Controls.Add(this.label11);
            this.tabPage2.Controls.Add(this.txt_mailPwd);
            this.tabPage2.Controls.Add(this.txt_themeContent);
            this.tabPage2.Controls.Add(this.label5);
            this.tabPage2.Controls.Add(this.label10);
            this.tabPage2.Controls.Add(this.txt_account);
            this.tabPage2.Controls.Add(this.txt_recAddrContent);
            this.tabPage2.Controls.Add(this.label4);
            this.tabPage2.Controls.Add(this.label9);
            this.tabPage2.Controls.Add(this.txt_mailPort);
            this.tabPage2.Controls.Add(this.txt_sendAddrContent);
            this.tabPage2.Controls.Add(this.label3);
            this.tabPage2.Controls.Add(this.label12);
            this.tabPage2.Controls.Add(this.label8);
            this.tabPage2.Controls.Add(this.txt_mailHost);
            this.tabPage2.Controls.Add(this.label2);
            this.tabPage2.Controls.Add(this.label7);
            this.tabPage2.Controls.Add(this.label1);
            this.tabPage2.Location = new System.Drawing.Point(4, 25);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(862, 217);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "邮件监控";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // btn_sendMail
            // 
            this.btn_sendMail.Location = new System.Drawing.Point(496, 175);
            this.btn_sendMail.Name = "btn_sendMail";
            this.btn_sendMail.Size = new System.Drawing.Size(121, 36);
            this.btn_sendMail.TabIndex = 5;
            this.btn_sendMail.Text = "试试发送";
            this.btn_sendMail.UseVisualStyleBackColor = true;
            this.btn_sendMail.Click += new System.EventHandler(this.btn_sendMail_Click);
            // 
            // button6
            // 
            this.button6.Location = new System.Drawing.Point(360, 175);
            this.button6.Name = "button6";
            this.button6.Size = new System.Drawing.Size(121, 36);
            this.button6.TabIndex = 5;
            this.button6.Text = "重新开始";
            this.button6.UseVisualStyleBackColor = true;
            this.button6.Click += new System.EventHandler(this.button6_Click);
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(224, 175);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(121, 36);
            this.button4.TabIndex = 5;
            this.button4.Text = "关闭监控";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(97, 175);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(121, 36);
            this.button3.TabIndex = 5;
            this.button3.Text = "开始监控";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // radioButton2
            // 
            this.radioButton2.AutoSize = true;
            this.radioButton2.Checked = true;
            this.radioButton2.Location = new System.Drawing.Point(574, 20);
            this.radioButton2.Name = "radioButton2";
            this.radioButton2.Size = new System.Drawing.Size(58, 19);
            this.radioButton2.TabIndex = 4;
            this.radioButton2.TabStop = true;
            this.radioButton2.Tag = "role";
            this.radioButton2.Text = "之一";
            this.radioButton2.UseVisualStyleBackColor = true;
            // 
            // rd_mailCondition_all
            // 
            this.rd_mailCondition_all.AutoSize = true;
            this.rd_mailCondition_all.Location = new System.Drawing.Point(487, 19);
            this.rd_mailCondition_all.Name = "rd_mailCondition_all";
            this.rd_mailCondition_all.Size = new System.Drawing.Size(58, 19);
            this.rd_mailCondition_all.TabIndex = 4;
            this.rd_mailCondition_all.Tag = "role";
            this.rd_mailCondition_all.Text = "全部";
            this.rd_mailCondition_all.UseVisualStyleBackColor = true;
            // 
            // cmb_mialprotocol
            // 
            this.cmb_mialprotocol.FormattingEnabled = true;
            this.cmb_mialprotocol.Items.AddRange(new object[] {
            "POP3",
            "IMAP"});
            this.cmb_mialprotocol.Location = new System.Drawing.Point(281, 16);
            this.cmb_mialprotocol.Name = "cmb_mialprotocol";
            this.cmb_mialprotocol.Size = new System.Drawing.Size(75, 23);
            this.cmb_mialprotocol.TabIndex = 3;
            this.cmb_mialprotocol.Text = "imap";
            // 
            // num_MailInterval
            // 
            this.num_MailInterval.Location = new System.Drawing.Point(100, 19);
            this.num_MailInterval.Name = "num_MailInterval";
            this.num_MailInterval.Size = new System.Drawing.Size(50, 25);
            this.num_MailInterval.TabIndex = 2;
            this.num_MailInterval.Value = new decimal(new int[] {
            30,
            0,
            0,
            0});
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(232, 19);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(37, 15);
            this.label6.TabIndex = 0;
            this.label6.Text = "协议";
            // 
            // txt_bodyContent
            // 
            this.txt_bodyContent.Location = new System.Drawing.Point(487, 144);
            this.txt_bodyContent.Name = "txt_bodyContent";
            this.txt_bodyContent.Size = new System.Drawing.Size(201, 25);
            this.txt_bodyContent.TabIndex = 1;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(399, 147);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(82, 15);
            this.label11.TabIndex = 0;
            this.label11.Text = "正文中包含";
            // 
            // txt_mailPwd
            // 
            this.txt_mailPwd.Location = new System.Drawing.Point(98, 144);
            this.txt_mailPwd.Name = "txt_mailPwd";
            this.txt_mailPwd.Size = new System.Drawing.Size(258, 25);
            this.txt_mailPwd.TabIndex = 1;
            // 
            // txt_themeContent
            // 
            this.txt_themeContent.Location = new System.Drawing.Point(487, 113);
            this.txt_themeContent.Name = "txt_themeContent";
            this.txt_themeContent.Size = new System.Drawing.Size(201, 25);
            this.txt_themeContent.TabIndex = 1;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(49, 147);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(37, 15);
            this.label5.TabIndex = 0;
            this.label5.Text = "密码";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(399, 116);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(82, 15);
            this.label10.TabIndex = 0;
            this.label10.Text = "主题中包含";
            // 
            // txt_account
            // 
            this.txt_account.Location = new System.Drawing.Point(98, 113);
            this.txt_account.Name = "txt_account";
            this.txt_account.Size = new System.Drawing.Size(258, 25);
            this.txt_account.TabIndex = 1;
            this.txt_account.Text = "1838777688@qq.com";
            // 
            // txt_recAddrContent
            // 
            this.txt_recAddrContent.Location = new System.Drawing.Point(487, 82);
            this.txt_recAddrContent.Name = "txt_recAddrContent";
            this.txt_recAddrContent.Size = new System.Drawing.Size(201, 25);
            this.txt_recAddrContent.TabIndex = 1;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(49, 116);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(37, 15);
            this.label4.TabIndex = 0;
            this.label4.Text = "邮箱";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(399, 85);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(82, 15);
            this.label9.TabIndex = 0;
            this.label9.Text = "收件人包含";
            // 
            // txt_mailPort
            // 
            this.txt_mailPort.Location = new System.Drawing.Point(98, 82);
            this.txt_mailPort.Name = "txt_mailPort";
            this.txt_mailPort.Size = new System.Drawing.Size(258, 25);
            this.txt_mailPort.TabIndex = 1;
            this.txt_mailPort.Text = "993";
            // 
            // txt_sendAddrContent
            // 
            this.txt_sendAddrContent.Location = new System.Drawing.Point(487, 50);
            this.txt_sendAddrContent.Name = "txt_sendAddrContent";
            this.txt_sendAddrContent.Size = new System.Drawing.Size(201, 25);
            this.txt_sendAddrContent.TabIndex = 1;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(49, 85);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(37, 15);
            this.label3.TabIndex = 0;
            this.label3.Text = "端口";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(399, 21);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(67, 15);
            this.label12.TabIndex = 0;
            this.label12.Text = "监控规则";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(399, 50);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(82, 15);
            this.label8.TabIndex = 0;
            this.label8.Text = "发件人包含";
            // 
            // txt_mailHost
            // 
            this.txt_mailHost.Location = new System.Drawing.Point(98, 50);
            this.txt_mailHost.Name = "txt_mailHost";
            this.txt_mailHost.Size = new System.Drawing.Size(258, 25);
            this.txt_mailHost.TabIndex = 1;
            this.txt_mailHost.Text = "imap.qq.com";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(4, 53);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(82, 15);
            this.label2.TabIndex = 0;
            this.label2.Text = "服务器地址";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(150, 24);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(22, 15);
            this.label7.TabIndex = 0;
            this.label7.Text = "秒";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(49, 21);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(37, 15);
            this.label1.TabIndex = 0;
            this.label1.Text = "间隔";
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.txt_hotkey_name);
            this.tabPage3.Controls.Add(this.label13);
            this.tabPage3.Controls.Add(this.cmb_hotkey_keytype);
            this.tabPage3.Controls.Add(this.cb_hotkey_win);
            this.tabPage3.Controls.Add(this.cb_hotkey_shift);
            this.tabPage3.Controls.Add(this.cb_hotkey_ctrl);
            this.tabPage3.Controls.Add(this.cb_hotkey_alt);
            this.tabPage3.Controls.Add(this.btn_hotkey_stop);
            this.tabPage3.Controls.Add(this.btn_hotkey_start);
            this.tabPage3.Location = new System.Drawing.Point(4, 25);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(862, 217);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "按键监控";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // txt_hotkey_name
            // 
            this.txt_hotkey_name.Location = new System.Drawing.Point(161, 34);
            this.txt_hotkey_name.Name = "txt_hotkey_name";
            this.txt_hotkey_name.Size = new System.Drawing.Size(192, 25);
            this.txt_hotkey_name.TabIndex = 4;
            this.txt_hotkey_name.Text = "按键测试1";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(63, 39);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(97, 15);
            this.label13.TabIndex = 3;
            this.label13.Text = "触发器名称：";
            // 
            // cmb_hotkey_keytype
            // 
            this.cmb_hotkey_keytype.FormattingEnabled = true;
            this.cmb_hotkey_keytype.Items.AddRange(new object[] {
            "",
            "a",
            "b",
            "C",
            "D",
            "E",
            "F",
            "G",
            "H",
            "I",
            "J",
            "K",
            "L",
            "M",
            "N",
            "O",
            "P",
            "Q",
            "R",
            "S",
            "T",
            "U",
            "V",
            "W",
            "X",
            "Y",
            "Z"});
            this.cmb_hotkey_keytype.Location = new System.Drawing.Point(388, 91);
            this.cmb_hotkey_keytype.Name = "cmb_hotkey_keytype";
            this.cmb_hotkey_keytype.Size = new System.Drawing.Size(99, 23);
            this.cmb_hotkey_keytype.TabIndex = 2;
            // 
            // cb_hotkey_win
            // 
            this.cb_hotkey_win.AutoSize = true;
            this.cb_hotkey_win.Location = new System.Drawing.Point(284, 95);
            this.cb_hotkey_win.Name = "cb_hotkey_win";
            this.cb_hotkey_win.Size = new System.Drawing.Size(53, 19);
            this.cb_hotkey_win.TabIndex = 1;
            this.cb_hotkey_win.Text = "Win";
            this.cb_hotkey_win.UseVisualStyleBackColor = true;
            // 
            // cb_hotkey_shift
            // 
            this.cb_hotkey_shift.AutoSize = true;
            this.cb_hotkey_shift.Location = new System.Drawing.Point(203, 95);
            this.cb_hotkey_shift.Name = "cb_hotkey_shift";
            this.cb_hotkey_shift.Size = new System.Drawing.Size(69, 19);
            this.cb_hotkey_shift.TabIndex = 1;
            this.cb_hotkey_shift.Text = "Shift";
            this.cb_hotkey_shift.UseVisualStyleBackColor = true;
            // 
            // cb_hotkey_ctrl
            // 
            this.cb_hotkey_ctrl.AutoSize = true;
            this.cb_hotkey_ctrl.Location = new System.Drawing.Point(122, 95);
            this.cb_hotkey_ctrl.Name = "cb_hotkey_ctrl";
            this.cb_hotkey_ctrl.Size = new System.Drawing.Size(61, 19);
            this.cb_hotkey_ctrl.TabIndex = 1;
            this.cb_hotkey_ctrl.Text = "Ctrl";
            this.cb_hotkey_ctrl.UseVisualStyleBackColor = true;
            // 
            // cb_hotkey_alt
            // 
            this.cb_hotkey_alt.AutoSize = true;
            this.cb_hotkey_alt.Location = new System.Drawing.Point(63, 95);
            this.cb_hotkey_alt.Name = "cb_hotkey_alt";
            this.cb_hotkey_alt.Size = new System.Drawing.Size(53, 19);
            this.cb_hotkey_alt.TabIndex = 1;
            this.cb_hotkey_alt.Text = "alt";
            this.cb_hotkey_alt.UseVisualStyleBackColor = true;
            // 
            // btn_hotkey_stop
            // 
            this.btn_hotkey_stop.Location = new System.Drawing.Point(198, 158);
            this.btn_hotkey_stop.Name = "btn_hotkey_stop";
            this.btn_hotkey_stop.Size = new System.Drawing.Size(134, 39);
            this.btn_hotkey_stop.TabIndex = 0;
            this.btn_hotkey_stop.Text = "停止监控";
            this.btn_hotkey_stop.UseVisualStyleBackColor = true;
            this.btn_hotkey_stop.Click += new System.EventHandler(this.btn_hotkey_stop_Click);
            // 
            // btn_hotkey_start
            // 
            this.btn_hotkey_start.Location = new System.Drawing.Point(58, 158);
            this.btn_hotkey_start.Name = "btn_hotkey_start";
            this.btn_hotkey_start.Size = new System.Drawing.Size(134, 39);
            this.btn_hotkey_start.TabIndex = 0;
            this.btn_hotkey_start.Text = "开始监控";
            this.btn_hotkey_start.UseVisualStyleBackColor = true;
            this.btn_hotkey_start.Click += new System.EventHandler(this.btn_hotkey_start_Click);
            // 
            // tabPage4
            // 
            this.tabPage4.Controls.Add(this.txt_mouse_name);
            this.tabPage4.Controls.Add(this.label14);
            this.tabPage4.Controls.Add(this.com_mouse_mousetype);
            this.tabPage4.Controls.Add(this.cb_mouse_win);
            this.tabPage4.Controls.Add(this.cb_mouse_shift);
            this.tabPage4.Controls.Add(this.cb_mouse_ctrl);
            this.tabPage4.Controls.Add(this.cb_mouse_alt);
            this.tabPage4.Controls.Add(this.btn_mouse_stop);
            this.tabPage4.Controls.Add(this.btn_mouse_start);
            this.tabPage4.Location = new System.Drawing.Point(4, 25);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage4.Size = new System.Drawing.Size(862, 217);
            this.tabPage4.TabIndex = 3;
            this.tabPage4.Text = "鼠标监控";
            this.tabPage4.UseVisualStyleBackColor = true;
            // 
            // txt_mouse_name
            // 
            this.txt_mouse_name.Location = new System.Drawing.Point(183, 24);
            this.txt_mouse_name.Name = "txt_mouse_name";
            this.txt_mouse_name.Size = new System.Drawing.Size(192, 25);
            this.txt_mouse_name.TabIndex = 13;
            this.txt_mouse_name.Text = "按键测试1";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(85, 29);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(97, 15);
            this.label14.TabIndex = 12;
            this.label14.Text = "触发器名称：";
            // 
            // com_mouse_mousetype
            // 
            this.com_mouse_mousetype.FormattingEnabled = true;
            this.com_mouse_mousetype.Items.AddRange(new object[] {
            "左",
            "中",
            "右"});
            this.com_mouse_mousetype.Location = new System.Drawing.Point(410, 81);
            this.com_mouse_mousetype.Name = "com_mouse_mousetype";
            this.com_mouse_mousetype.Size = new System.Drawing.Size(99, 23);
            this.com_mouse_mousetype.TabIndex = 11;
            this.com_mouse_mousetype.Text = "左";
            // 
            // cb_mouse_win
            // 
            this.cb_mouse_win.AutoSize = true;
            this.cb_mouse_win.Location = new System.Drawing.Point(306, 85);
            this.cb_mouse_win.Name = "cb_mouse_win";
            this.cb_mouse_win.Size = new System.Drawing.Size(53, 19);
            this.cb_mouse_win.TabIndex = 7;
            this.cb_mouse_win.Text = "Win";
            this.cb_mouse_win.UseVisualStyleBackColor = true;
            // 
            // cb_mouse_shift
            // 
            this.cb_mouse_shift.AutoSize = true;
            this.cb_mouse_shift.Location = new System.Drawing.Point(225, 85);
            this.cb_mouse_shift.Name = "cb_mouse_shift";
            this.cb_mouse_shift.Size = new System.Drawing.Size(69, 19);
            this.cb_mouse_shift.TabIndex = 8;
            this.cb_mouse_shift.Text = "Shift";
            this.cb_mouse_shift.UseVisualStyleBackColor = true;
            // 
            // cb_mouse_ctrl
            // 
            this.cb_mouse_ctrl.AutoSize = true;
            this.cb_mouse_ctrl.Location = new System.Drawing.Point(144, 85);
            this.cb_mouse_ctrl.Name = "cb_mouse_ctrl";
            this.cb_mouse_ctrl.Size = new System.Drawing.Size(61, 19);
            this.cb_mouse_ctrl.TabIndex = 9;
            this.cb_mouse_ctrl.Text = "Ctrl";
            this.cb_mouse_ctrl.UseVisualStyleBackColor = true;
            // 
            // cb_mouse_alt
            // 
            this.cb_mouse_alt.AutoSize = true;
            this.cb_mouse_alt.Location = new System.Drawing.Point(85, 85);
            this.cb_mouse_alt.Name = "cb_mouse_alt";
            this.cb_mouse_alt.Size = new System.Drawing.Size(53, 19);
            this.cb_mouse_alt.TabIndex = 10;
            this.cb_mouse_alt.Text = "alt";
            this.cb_mouse_alt.UseVisualStyleBackColor = true;
            // 
            // btn_mouse_stop
            // 
            this.btn_mouse_stop.Location = new System.Drawing.Point(220, 148);
            this.btn_mouse_stop.Name = "btn_mouse_stop";
            this.btn_mouse_stop.Size = new System.Drawing.Size(134, 39);
            this.btn_mouse_stop.TabIndex = 5;
            this.btn_mouse_stop.Text = "停止监控";
            this.btn_mouse_stop.UseVisualStyleBackColor = true;
            this.btn_mouse_stop.Click += new System.EventHandler(this.btn_mouse_stop_Click);
            // 
            // btn_mouse_start
            // 
            this.btn_mouse_start.Location = new System.Drawing.Point(80, 148);
            this.btn_mouse_start.Name = "btn_mouse_start";
            this.btn_mouse_start.Size = new System.Drawing.Size(134, 39);
            this.btn_mouse_start.TabIndex = 6;
            this.btn_mouse_start.Text = "开始监控";
            this.btn_mouse_start.UseVisualStyleBackColor = true;
            this.btn_mouse_start.Click += new System.EventHandler(this.btn_mouse_start_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.richTextBox1);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox1.Location = new System.Drawing.Point(0, 246);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(870, 378);
            this.groupBox1.TabIndex = 8;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "日志";
            // 
            // TestForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(870, 624);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.tabControl1);
            this.Name = "TestForm";
            this.Text = "TestForm";
            this.Load += new System.EventHandler(this.TestForm_Load);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.num_MailInterval)).EndInit();
            this.tabPage3.ResumeLayout(false);
            this.tabPage3.PerformLayout();
            this.tabPage4.ResumeLayout(false);
            this.tabPage4.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.CheckedListBox cbList_eventTypes;
        private System.Windows.Forms.TextBox txt_filePath;
        private System.Windows.Forms.TextBox txt_fileFilter;
        private System.Windows.Forms.CheckBox cb_containChilds;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txt_mailPwd;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txt_account;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txt_mailPort;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txt_mailHost;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown num_MailInterval;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ComboBox cmb_mialprotocol;
        private System.Windows.Forms.TextBox txt_bodyContent;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TextBox txt_themeContent;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox txt_recAddrContent;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox txt_sendAddrContent;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.RadioButton radioButton2;
        private System.Windows.Forms.RadioButton rd_mailCondition_all;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button button6;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Button btn_sendMail;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.Button btn_hotkey_start;
        private System.Windows.Forms.Button btn_hotkey_stop;
        private System.Windows.Forms.CheckBox cb_hotkey_alt;
        private System.Windows.Forms.ComboBox cmb_hotkey_keytype;
        private System.Windows.Forms.CheckBox cb_hotkey_win;
        private System.Windows.Forms.CheckBox cb_hotkey_shift;
        private System.Windows.Forms.CheckBox cb_hotkey_ctrl;
        private System.Windows.Forms.TextBox txt_hotkey_name;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.TabPage tabPage4;
        private System.Windows.Forms.TextBox txt_mouse_name;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.ComboBox com_mouse_mousetype;
        private System.Windows.Forms.CheckBox cb_mouse_win;
        private System.Windows.Forms.CheckBox cb_mouse_shift;
        private System.Windows.Forms.CheckBox cb_mouse_ctrl;
        private System.Windows.Forms.CheckBox cb_mouse_alt;
        private System.Windows.Forms.Button btn_mouse_stop;
        private System.Windows.Forms.Button btn_mouse_start;
    }
}