namespace II.RPA.Plugin.Test
{
    partial class DbForm
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.txtSID = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.txtFilepath = new System.Windows.Forms.TextBox();
            this.txtPwd = new System.Windows.Forms.TextBox();
            this.txtUid = new System.Windows.Forms.TextBox();
            this.txtDbPort = new System.Windows.Forms.TextBox();
            this.txtDbAddress = new System.Windows.Forms.TextBox();
            this.txtDbName = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.cboServerType = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.rtbResult = new System.Windows.Forms.RichTextBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.groupBox7 = new System.Windows.Forms.GroupBox();
            this.btnExecuteSQLBatch = new System.Windows.Forms.Button();
            this.groupBox8 = new System.Windows.Forms.GroupBox();
            this.txtSQLParam2 = new System.Windows.Forms.TextBox();
            this.label15 = new System.Windows.Forms.Label();
            this.btnExecuteSQL = new System.Windows.Forms.Button();
            this.txtSQL2 = new System.Windows.Forms.TextBox();
            this.label16 = new System.Windows.Forms.Label();
            this.txtDbID2 = new System.Windows.Forms.TextBox();
            this.label17 = new System.Windows.Forms.Label();
            this.groupBox6 = new System.Windows.Forms.GroupBox();
            this.label13 = new System.Windows.Forms.Label();
            this.txtDbID3 = new System.Windows.Forms.TextBox();
            this.btnCloseCon = new System.Windows.Forms.Button();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.btnQueryAll = new System.Windows.Forms.Button();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.txtSQLParam = new System.Windows.Forms.TextBox();
            this.cboReturnDic = new System.Windows.Forms.ComboBox();
            this.label11 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.btnSingleQuery = new System.Windows.Forms.Button();
            this.txtSQL1 = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.txtDbID1 = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.btnCreateDbConn = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox7.SuspendLayout();
            this.groupBox8.SuspendLayout();
            this.groupBox6.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.txtSID);
            this.groupBox1.Controls.Add(this.label8);
            this.groupBox1.Controls.Add(this.txtFilepath);
            this.groupBox1.Controls.Add(this.txtPwd);
            this.groupBox1.Controls.Add(this.txtUid);
            this.groupBox1.Controls.Add(this.txtDbPort);
            this.groupBox1.Controls.Add(this.txtDbAddress);
            this.groupBox1.Controls.Add(this.txtDbName);
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.cboServerType);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(2, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(263, 289);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "连接信息";
            // 
            // txtSID
            // 
            this.txtSID.Location = new System.Drawing.Point(88, 254);
            this.txtSID.Name = "txtSID";
            this.txtSID.Size = new System.Drawing.Size(160, 21);
            this.txtSID.TabIndex = 15;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(11, 257);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(29, 12);
            this.label8.TabIndex = 14;
            this.label8.Text = "SID:";
            // 
            // txtFilepath
            // 
            this.txtFilepath.Location = new System.Drawing.Point(88, 220);
            this.txtFilepath.Name = "txtFilepath";
            this.txtFilepath.Size = new System.Drawing.Size(160, 21);
            this.txtFilepath.TabIndex = 13;
            // 
            // txtPwd
            // 
            this.txtPwd.Location = new System.Drawing.Point(88, 187);
            this.txtPwd.Name = "txtPwd";
            this.txtPwd.Size = new System.Drawing.Size(160, 21);
            this.txtPwd.TabIndex = 12;
            this.txtPwd.Text = "88888888";
            // 
            // txtUid
            // 
            this.txtUid.Location = new System.Drawing.Point(88, 154);
            this.txtUid.Name = "txtUid";
            this.txtUid.Size = new System.Drawing.Size(160, 21);
            this.txtUid.TabIndex = 11;
            this.txtUid.Text = "root";
            // 
            // txtDbPort
            // 
            this.txtDbPort.Location = new System.Drawing.Point(88, 121);
            this.txtDbPort.Name = "txtDbPort";
            this.txtDbPort.Size = new System.Drawing.Size(160, 21);
            this.txtDbPort.TabIndex = 10;
            this.txtDbPort.Text = "3306";
            // 
            // txtDbAddress
            // 
            this.txtDbAddress.Location = new System.Drawing.Point(88, 88);
            this.txtDbAddress.Name = "txtDbAddress";
            this.txtDbAddress.Size = new System.Drawing.Size(160, 21);
            this.txtDbAddress.TabIndex = 9;
            this.txtDbAddress.Text = "127.0.0.1";
            // 
            // txtDbName
            // 
            this.txtDbName.Location = new System.Drawing.Point(88, 55);
            this.txtDbName.Name = "txtDbName";
            this.txtDbName.Size = new System.Drawing.Size(160, 21);
            this.txtDbName.TabIndex = 8;
            this.txtDbName.Text = "linxi";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(11, 224);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(59, 12);
            this.label7.TabIndex = 7;
            this.label7.Text = "文件路径:";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(11, 191);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(47, 12);
            this.label6.TabIndex = 6;
            this.label6.Text = "密  码:";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(11, 158);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(47, 12);
            this.label5.TabIndex = 5;
            this.label5.Text = "用户名:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(11, 125);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(71, 12);
            this.label4.TabIndex = 4;
            this.label4.Text = "数据库端口:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(11, 92);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(71, 12);
            this.label3.TabIndex = 3;
            this.label3.Text = "数据库地址:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(11, 59);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(59, 12);
            this.label2.TabIndex = 2;
            this.label2.Text = "数据库名:";
            // 
            // cboServerType
            // 
            this.cboServerType.FormattingEnabled = true;
            this.cboServerType.Items.AddRange(new object[] {
            "Mysql",
            "SqlServer",
            "Oracle",
            "Pgsql",
            "Sqlite",
            "Access",
            "Db2"});
            this.cboServerType.Location = new System.Drawing.Point(88, 23);
            this.cboServerType.Name = "cboServerType";
            this.cboServerType.Size = new System.Drawing.Size(160, 20);
            this.cboServerType.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(11, 26);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(71, 12);
            this.label1.TabIndex = 0;
            this.label1.Text = "数据库类型:";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.rtbResult);
            this.groupBox2.Location = new System.Drawing.Point(271, 3);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(343, 289);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "执行结果";
            // 
            // rtbResult
            // 
            this.rtbResult.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.rtbResult.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtbResult.Location = new System.Drawing.Point(3, 17);
            this.rtbResult.Name = "rtbResult";
            this.rtbResult.Size = new System.Drawing.Size(337, 269);
            this.rtbResult.TabIndex = 0;
            this.rtbResult.Text = "";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.groupBox7);
            this.groupBox3.Controls.Add(this.groupBox6);
            this.groupBox3.Controls.Add(this.groupBox4);
            this.groupBox3.Controls.Add(this.btnCreateDbConn);
            this.groupBox3.Location = new System.Drawing.Point(2, 299);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(621, 362);
            this.groupBox3.TabIndex = 2;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "操作";
            // 
            // groupBox7
            // 
            this.groupBox7.Controls.Add(this.btnExecuteSQLBatch);
            this.groupBox7.Controls.Add(this.groupBox8);
            this.groupBox7.Controls.Add(this.btnExecuteSQL);
            this.groupBox7.Controls.Add(this.txtSQL2);
            this.groupBox7.Controls.Add(this.label16);
            this.groupBox7.Controls.Add(this.txtDbID2);
            this.groupBox7.Controls.Add(this.label17);
            this.groupBox7.Location = new System.Drawing.Point(242, 60);
            this.groupBox7.Name = "groupBox7";
            this.groupBox7.Size = new System.Drawing.Size(208, 294);
            this.groupBox7.TabIndex = 5;
            this.groupBox7.TabStop = false;
            this.groupBox7.Text = "执行SQL查询";
            // 
            // btnExecuteSQLBatch
            // 
            this.btnExecuteSQLBatch.Location = new System.Drawing.Point(47, 248);
            this.btnExecuteSQLBatch.Name = "btnExecuteSQLBatch";
            this.btnExecuteSQLBatch.Size = new System.Drawing.Size(103, 23);
            this.btnExecuteSQLBatch.TabIndex = 20;
            this.btnExecuteSQLBatch.Text = "批量执行SQL语句";
            this.btnExecuteSQLBatch.UseVisualStyleBackColor = true;
            this.btnExecuteSQLBatch.Click += new System.EventHandler(this.btnExecuteSQLBatch_Click);
            // 
            // groupBox8
            // 
            this.groupBox8.Controls.Add(this.txtSQLParam2);
            this.groupBox8.Controls.Add(this.label15);
            this.groupBox8.Location = new System.Drawing.Point(4, 115);
            this.groupBox8.Name = "groupBox8";
            this.groupBox8.Size = new System.Drawing.Size(198, 61);
            this.groupBox8.TabIndex = 19;
            this.groupBox8.TabStop = false;
            this.groupBox8.Text = "可选";
            // 
            // txtSQLParam2
            // 
            this.txtSQLParam2.Location = new System.Drawing.Point(6, 32);
            this.txtSQLParam2.Name = "txtSQLParam2";
            this.txtSQLParam2.Size = new System.Drawing.Size(190, 21);
            this.txtSQLParam2.TabIndex = 22;
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(6, 17);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(77, 12);
            this.label15.TabIndex = 21;
            this.label15.Text = "SQL语句参数:";
            // 
            // btnExecuteSQL
            // 
            this.btnExecuteSQL.Location = new System.Drawing.Point(51, 204);
            this.btnExecuteSQL.Name = "btnExecuteSQL";
            this.btnExecuteSQL.Size = new System.Drawing.Size(92, 23);
            this.btnExecuteSQL.TabIndex = 1;
            this.btnExecuteSQL.Text = "执行SQL语句";
            this.btnExecuteSQL.UseVisualStyleBackColor = true;
            this.btnExecuteSQL.Click += new System.EventHandler(this.btnExecuteSQL_Click);
            // 
            // txtSQL2
            // 
            this.txtSQL2.Location = new System.Drawing.Point(6, 76);
            this.txtSQL2.Name = "txtSQL2";
            this.txtSQL2.Size = new System.Drawing.Size(190, 21);
            this.txtSQL2.TabIndex = 18;
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(6, 61);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(53, 12);
            this.label16.TabIndex = 17;
            this.label16.Text = "SQL语句:";
            // 
            // txtDbID2
            // 
            this.txtDbID2.Location = new System.Drawing.Point(81, 26);
            this.txtDbID2.Name = "txtDbID2";
            this.txtDbID2.Size = new System.Drawing.Size(115, 21);
            this.txtDbID2.TabIndex = 16;
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(6, 29);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(71, 12);
            this.label17.TabIndex = 15;
            this.label17.Text = "数据库标识:";
            // 
            // groupBox6
            // 
            this.groupBox6.Controls.Add(this.label13);
            this.groupBox6.Controls.Add(this.txtDbID3);
            this.groupBox6.Controls.Add(this.btnCloseCon);
            this.groupBox6.Location = new System.Drawing.Point(456, 60);
            this.groupBox6.Name = "groupBox6";
            this.groupBox6.Size = new System.Drawing.Size(156, 126);
            this.groupBox6.TabIndex = 4;
            this.groupBox6.TabStop = false;
            this.groupBox6.Text = "关闭连接";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(6, 21);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(71, 12);
            this.label13.TabIndex = 16;
            this.label13.Text = "数据库标识:";
            // 
            // txtDbID3
            // 
            this.txtDbID3.Location = new System.Drawing.Point(6, 36);
            this.txtDbID3.Name = "txtDbID3";
            this.txtDbID3.Size = new System.Drawing.Size(145, 21);
            this.txtDbID3.TabIndex = 17;
            // 
            // btnCloseCon
            // 
            this.btnCloseCon.Location = new System.Drawing.Point(34, 74);
            this.btnCloseCon.Name = "btnCloseCon";
            this.btnCloseCon.Size = new System.Drawing.Size(74, 23);
            this.btnCloseCon.TabIndex = 3;
            this.btnCloseCon.Text = "关闭连接";
            this.btnCloseCon.UseVisualStyleBackColor = true;
            this.btnCloseCon.Click += new System.EventHandler(this.btnCloseCon_Click);
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.btnQueryAll);
            this.groupBox4.Controls.Add(this.groupBox5);
            this.groupBox4.Controls.Add(this.btnSingleQuery);
            this.groupBox4.Controls.Add(this.txtSQL1);
            this.groupBox4.Controls.Add(this.label10);
            this.groupBox4.Controls.Add(this.txtDbID1);
            this.groupBox4.Controls.Add(this.label9);
            this.groupBox4.Location = new System.Drawing.Point(6, 60);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(208, 294);
            this.groupBox4.TabIndex = 2;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "执行SQL查询";
            // 
            // btnQueryAll
            // 
            this.btnQueryAll.Location = new System.Drawing.Point(101, 248);
            this.btnQueryAll.Name = "btnQueryAll";
            this.btnQueryAll.Size = new System.Drawing.Size(103, 23);
            this.btnQueryAll.TabIndex = 20;
            this.btnQueryAll.Text = "执行全SQL查询";
            this.btnQueryAll.UseVisualStyleBackColor = true;
            this.btnQueryAll.Click += new System.EventHandler(this.btnQueryAll_Click);
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.txtSQLParam);
            this.groupBox5.Controls.Add(this.cboReturnDic);
            this.groupBox5.Controls.Add(this.label11);
            this.groupBox5.Controls.Add(this.label12);
            this.groupBox5.Location = new System.Drawing.Point(4, 115);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(198, 112);
            this.groupBox5.TabIndex = 19;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "可选";
            // 
            // txtSQLParam
            // 
            this.txtSQLParam.Location = new System.Drawing.Point(2, 80);
            this.txtSQLParam.Name = "txtSQLParam";
            this.txtSQLParam.Size = new System.Drawing.Size(190, 21);
            this.txtSQLParam.TabIndex = 22;
            // 
            // cboReturnDic
            // 
            this.cboReturnDic.FormattingEnabled = true;
            this.cboReturnDic.Items.AddRange(new object[] {
            "否",
            "是"});
            this.cboReturnDic.Location = new System.Drawing.Point(77, 27);
            this.cboReturnDic.Name = "cboReturnDic";
            this.cboReturnDic.Size = new System.Drawing.Size(115, 20);
            this.cboReturnDic.TabIndex = 20;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(2, 30);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(59, 12);
            this.label11.TabIndex = 19;
            this.label11.Text = "返回字典:";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(2, 65);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(77, 12);
            this.label12.TabIndex = 21;
            this.label12.Text = "SQL语句参数:";
            // 
            // btnSingleQuery
            // 
            this.btnSingleQuery.Location = new System.Drawing.Point(3, 248);
            this.btnSingleQuery.Name = "btnSingleQuery";
            this.btnSingleQuery.Size = new System.Drawing.Size(92, 23);
            this.btnSingleQuery.TabIndex = 1;
            this.btnSingleQuery.Text = "执行单SQL查询";
            this.btnSingleQuery.UseVisualStyleBackColor = true;
            this.btnSingleQuery.Click += new System.EventHandler(this.btnSingleQuery_Click);
            // 
            // txtSQL1
            // 
            this.txtSQL1.Location = new System.Drawing.Point(6, 76);
            this.txtSQL1.Name = "txtSQL1";
            this.txtSQL1.Size = new System.Drawing.Size(190, 21);
            this.txtSQL1.TabIndex = 18;
            this.txtSQL1.Text = "select * from test1";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(6, 61);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(53, 12);
            this.label10.TabIndex = 17;
            this.label10.Text = "SQL语句:";
            // 
            // txtDbID1
            // 
            this.txtDbID1.Location = new System.Drawing.Point(81, 26);
            this.txtDbID1.Name = "txtDbID1";
            this.txtDbID1.Size = new System.Drawing.Size(115, 21);
            this.txtDbID1.TabIndex = 16;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(6, 29);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(71, 12);
            this.label9.TabIndex = 15;
            this.label9.Text = "数据库标识:";
            // 
            // btnCreateDbConn
            // 
            this.btnCreateDbConn.Location = new System.Drawing.Point(13, 21);
            this.btnCreateDbConn.Name = "btnCreateDbConn";
            this.btnCreateDbConn.Size = new System.Drawing.Size(103, 23);
            this.btnCreateDbConn.TabIndex = 0;
            this.btnCreateDbConn.Text = "创建数据库对象";
            this.btnCreateDbConn.UseVisualStyleBackColor = true;
            this.btnCreateDbConn.Click += new System.EventHandler(this.btnCreateDbConn_Click);
            // 
            // DbForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(626, 662);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Name = "DbForm";
            this.Text = "DbForm";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox7.ResumeLayout(false);
            this.groupBox7.PerformLayout();
            this.groupBox8.ResumeLayout(false);
            this.groupBox8.PerformLayout();
            this.groupBox6.ResumeLayout(false);
            this.groupBox6.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox txtSID;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox txtFilepath;
        private System.Windows.Forms.TextBox txtPwd;
        private System.Windows.Forms.TextBox txtUid;
        private System.Windows.Forms.TextBox txtDbPort;
        private System.Windows.Forms.TextBox txtDbAddress;
        private System.Windows.Forms.TextBox txtDbName;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cboServerType;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.RichTextBox rtbResult;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Button btnQueryAll;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.TextBox txtSQLParam;
        private System.Windows.Forms.ComboBox cboReturnDic;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Button btnSingleQuery;
        private System.Windows.Forms.TextBox txtSQL1;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox txtDbID1;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Button btnCreateDbConn;
        private System.Windows.Forms.GroupBox groupBox7;
        private System.Windows.Forms.Button btnExecuteSQLBatch;
        private System.Windows.Forms.GroupBox groupBox8;
        private System.Windows.Forms.TextBox txtSQLParam2;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.Button btnExecuteSQL;
        private System.Windows.Forms.TextBox txtSQL2;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.TextBox txtDbID2;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.GroupBox groupBox6;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.TextBox txtDbID3;
        private System.Windows.Forms.Button btnCloseCon;
    }
}