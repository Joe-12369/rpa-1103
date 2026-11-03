using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using II.RPA.EventMonitor.JSON;
using Newtonsoft.Json;

namespace II.RPA.EventMonitor.TestTool
{
    public partial class TestTool : Form
    {
        public List<TriggerJson> TriggerList = new List<TriggerJson>();
        public TriggerJson CurrentTriggerJson { get; set; }

        private SocketClient socketClient = null;
        public TestTool()
        {
            InitializeComponent();
            this.gb_operation.Enabled = false;
            toolStripButton1.Enabled = true;
            toolStripButton2.Enabled = false;
            CurrentTriggerJson = new TriggerJson();
        }
        private void TestTool_Load(object sender, EventArgs e)
        {
            InitJson();
            this.lv_triggerList.Columns.Add("名称", 120, HorizontalAlignment.Left); //一步添加
            this.lv_triggerList.Columns.Add("类型", 120, HorizontalAlignment.Left); //一步添加
            this.lv_triggerList.Columns.Add("触发条件", 250); //一步添加

        }

        #region ListView
        void InitListView()
        {
            lv_triggerList.Items.Clear();
            //添加文件触发器
            foreach (var fileTrigger in CurrentTriggerJson.fileTriggers)
            {
                ListViewItem lvi = new ListViewItem();

                lvi.Text = fileTrigger.name;

                lvi.SubItems.Add("文件");

                lvi.SubItems.Add($"{fileTrigger.monitorEvent}:{fileTrigger.filePath}");

                this.lv_triggerList.Items.Add(lvi);

            }
            //添加邮件触发器
            foreach (var mailTrigger in CurrentTriggerJson.mailTriggers)
            {
                ListViewItem lvi = new ListViewItem();

                lvi.Text = mailTrigger.name;

                lvi.SubItems.Add("邮件");

                lvi.SubItems.Add($"监控{mailTrigger.email}");

                this.lv_triggerList.Items.Add(lvi);
            }
            //添加按键触发器
            foreach (var hotkeyTrigger in CurrentTriggerJson.hotkeyTriggers)
            {
                ListViewItem lvi = new ListViewItem();

                lvi.Text = hotkeyTrigger.name;

                lvi.SubItems.Add("按键");

                lvi.SubItems.Add($"监控{hotkeyTrigger.auxiliaryKey}+{hotkeyTrigger.key}");

                this.lv_triggerList.Items.Add(lvi);
            }
            //添加鼠标触发器
            foreach (var mouseTrigger in CurrentTriggerJson.mouseTriggers)
            {
                ListViewItem lvi = new ListViewItem();

                lvi.Text = mouseTrigger.name;

                lvi.SubItems.Add("鼠标");

                lvi.SubItems.Add($"监控{mouseTrigger.auxiliaryKey}+鼠标{mouseTrigger.mouseType}");

                this.lv_triggerList.Items.Add(lvi);
            }
        }
        #endregion
        #region JSON 视窗
        private void InitJson()
        {
            string str = JsonConvert.SerializeObject(CurrentTriggerJson);
            rtxt_Json.Text = ConvertJsonString(str);
            InitListView();
        }
        private string ConvertJsonString(string str)
        {
            //格式化json字符串
            JsonSerializer serializer = new JsonSerializer();
            TextReader tr = new StringReader(str);
            JsonTextReader jtr = new JsonTextReader(tr);
            object obj = serializer.Deserialize(jtr);
            if (obj != null)
            {
                StringWriter textWriter = new StringWriter();
                JsonTextWriter jsonWriter = new JsonTextWriter(textWriter)
                {
                    Formatting = Formatting.Indented,
                    Indentation = 4,
                    IndentChar = ' '
                };
                serializer.Serialize(jsonWriter, obj);
                return textWriter.ToString();
            }
            else
            {
                return str;
            }
        }
        #endregion

        #region 文件

        private void btn_file_save_Click(object sender, EventArgs e)
        {
            FileTriggerJson ft = GetFileTriggerJson();
            int index = CurrentTriggerJson.fileTriggers.FindIndex(f => f.name == ft.name);
            if (index < 0)
            {
                CurrentTriggerJson.fileTriggers.Add(ft);
            }
            else
            {
                CurrentTriggerJson.fileTriggers[index] = ft;
            }
            InitJson();
        }

        private void btn_file_add_Click(object sender, EventArgs e)
        {
            FileTriggerJson ft = GetFileTriggerJson();
            int index = CurrentTriggerJson.fileTriggers.FindIndex(f => f.name == ft.name);
            if (index < 0)
            {
                CurrentTriggerJson.fileTriggers.Add(ft);
                InitJson();
            }
            else
            {
                MessageBox.Show("文件触发器重名");
            }
        }

        FileTriggerJson GetFileTriggerJson()
        {
            FileTriggerJson ft = new FileTriggerJson();
            ft.filePath = txt_filePath.Text;
            ft.fileType = txt_fileFilter.Text;
            ft.containSubfolder = cb_containChilds.Checked ? 1 : 0;
            ft.monitorEvent = "";
            for (int i = 0; i < cbList_eventTypes.CheckedItems.Count; i++)
            {
                ft.monitorEvent += cbList_eventTypes.CheckedItems[i] + ";";
            }
            ft.monitorEvent = ft.monitorEvent.TrimEnd(';');
            ft.name = txt_file_name.Text;
            return ft;
        }

        #endregion

        #region 邮件

        private void btn_mail_add_Click(object sender, EventArgs e)
        {
            MailTriggerJson mt = GetMailTriggerJson();
            int index = CurrentTriggerJson.mailTriggers.FindIndex(f => f.name == mt.name);
            if (index < 0)
            {
                CurrentTriggerJson.mailTriggers.Add(mt);
                InitJson();
            }
            else
            {
                MessageBox.Show("邮件触发器重名");
            }
        }

        private void btn_mail_save_Click(object sender, EventArgs e)
        {
            MailTriggerJson mt = GetMailTriggerJson();
            int index = CurrentTriggerJson.mailTriggers.FindIndex(f => f.name == mt.name);
            if (index < 0)
            {
                CurrentTriggerJson.mailTriggers.Add(mt);
                InitJson();
            }
            else
            {
                MessageBox.Show("邮件触发器重名");
            }
        }
        MailTriggerJson GetMailTriggerJson()
        {
            MailTriggerJson json = new MailTriggerJson()
            {
                protocol = cmb_mialprotocol.Text,
                interval = (int)num_MailInterval.Value,
                serverAddress = txt_mailHost.Text,
                port = int.Parse(txt_mailPort.Text),
                email = txt_account.Text,
                password = txt_mailPwd.Text,
                condition = rd_mailCondition_all.Checked ? 1 : 0,
                sendAddrContent = txt_sendAddrContent.Text,
                recAddrContent = txt_recAddrContent.Text,
                themeContent = txt_themeContent.Text,
                bodyContent = txt_bodyContent.Text,
            };
            return json;
        }
        #endregion

        #region 按键

        private void btn_hotkey_save_Click(object sender, EventArgs e)
        {
            HotKeyTriggerJson tt = GetHotKeyTriggerJson();
            int index = CurrentTriggerJson.hotkeyTriggers.FindIndex(f => f.name == tt.name);
            if (index < 0)
            {
                CurrentTriggerJson.hotkeyTriggers.Add(tt);
            }
            else
            {
                CurrentTriggerJson.hotkeyTriggers[index] = tt;
            }
            InitJson();
        }

        private void btn_hotkey_add_Click(object sender, EventArgs e)
        {
            HotKeyTriggerJson ft = GetHotKeyTriggerJson();
            int index = CurrentTriggerJson.hotkeyTriggers.FindIndex(f => f.name == ft.name);
            if (index < 0)
            {
                CurrentTriggerJson.hotkeyTriggers.Add(ft);
                InitJson();
            }
            else
            {
                MessageBox.Show("文件触发器重名");
            }
        }

        HotKeyTriggerJson GetHotKeyTriggerJson()
        {
            List<string> list = new List<string>();
            if (cb_hotkey_alt.Checked)
            {
                list.Add("alt");
            }
            if (cb_hotkey_ctrl.Checked)
            {
                list.Add("ctrl");
            }
            if (cb_hotkey_shift.Checked)
            {
                list.Add("shift");
            }
            if (cb_hotkey_win.Checked)
            {
                list.Add("win");
            }
            HotKeyTriggerJson json = new HotKeyTriggerJson()
            {
                auxiliaryKey = string.Join(",", list),
                key = cmb_hotkey_keytype.Text,
                name = txt_hotkey_name.Text
            };
            return json;
        }


        #endregion
        #region 鼠标


        private void btn_mouse_save_Click(object sender, EventArgs e)
        {
            MouseTriggerJson mt = GetMouseTriggerJson();
            int index = CurrentTriggerJson.mouseTriggers.FindIndex(f => f.name == mt.name);
            if (index < 0)
            {
                CurrentTriggerJson.mouseTriggers.Add(mt);
            }
            else
            {
                CurrentTriggerJson.mouseTriggers[index] = mt;
            }
            InitJson();
        }

        private void btn_mouse_add_Click(object sender, EventArgs e)
        {
            MouseTriggerJson mt = GetMouseTriggerJson();
            int index = CurrentTriggerJson.mouseTriggers.FindIndex(f => f.name == mt.name);
            if (index < 0)
            {
                CurrentTriggerJson.mouseTriggers.Add(mt);
                InitJson();
            }
            else
            {
                MessageBox.Show("文件触发器重名");
            }
        }

        MouseTriggerJson GetMouseTriggerJson()
        {
            List<string> list = new List<string>();
            if (cb_mouse_alt.Checked)
            {
                list.Add("alt");
            }
            if (cb_mouse_ctrl.Checked)
            {
                list.Add("ctrl");
            }
            if (cb_mouse_shift.Checked)
            {
                list.Add("shift");
            }
            if (cb_mouse_win.Checked)
            {
                list.Add("win");
            }
            MouseTriggerJson json = new MouseTriggerJson()
            {
                auxiliaryKey = string.Join(",", list),
                mouseType = com_mouse_mousetype.SelectedIndex,
                name = txt_mouse_name.Text
            };
            return json;
        }
        #endregion

        private void txtTriggerName_TextChanged(object sender, EventArgs e)
        {
            this.CurrentTriggerJson.postBack.appId = txtTriggerName.Text;
            InitJson();
        }

        private void rbtn_con_CheckedChanged(object sender, EventArgs e)
        {
            this.CurrentTriggerJson.condition = rbtn_con.Checked ? 0 : 1;
            InitJson();

        }
        #region MyRegion
        private void btn_startMonitor_Click(object sender, EventArgs e)
        {
            //string json = JsonConvert.SerializeObject(this.CurrentTriggerJson);
            string json = rtxt_Json.Text;
            socketClient?.Send("StartTrigger", json);
        }

        private void btn_restartMonitor_Click(object sender, EventArgs e)
        {

        }

        private void btn_stopMonitor_Click(object sender, EventArgs e)
        {
            socketClient?.Send("CloseTrigger",JsonConvert.SerializeObject( new[] { this.CurrentTriggerJson.postBack.appId }));
            AddMessage($"关闭触发器");
        }
        private void btn_keepLive_Click(object sender, EventArgs e)
        {
            socketClient?.Send("KeepLive", "0");
            AddMessage($"发送心跳，参数：0");
        }
        #endregion

        #region 设置消息日志
        private void AddMessage(string message)
        {
            this.BeginInvoke(new Action(() =>
            {
                var id = Thread.CurrentThread.ManagedThreadId;
                rtxt_logMessage.AppendText($"{DateTime.Now.ToString("yyyy-MM-ss HH:mm:ss")}[{id}]:{message}{Environment.NewLine}");
                if (rtxt_logMessage.Lines.Length > 50)
                {
                    rtxt_logMessage.Lines = rtxt_logMessage.Lines.Skip(rtxt_logMessage.Lines.Length - 50).ToArray();
                }
                rtxt_logMessage.SelectionStart = rtxt_logMessage.TextLength;
                rtxt_logMessage.ScrollToCaret();
            }));
            Thread.Sleep(10);
        }
        #endregion

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            socketClient = new SocketClient();
            socketClient.OnMessage += AddMessage;
            socketClient.Start();
            if (socketClient.IsConnected)
            {
                this.gb_operation.Enabled = true;
                toolStripButton1.Enabled = false;
                toolStripButton2.Enabled = true;
            }

        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            if (socketClient.IsConnected)
                socketClient?.Close();
            socketClient = null;
            toolStripButton1.Enabled = true;
            this.gb_operation.Enabled = false;
        }

        private void rtxt_Json_TextChanged(object sender, EventArgs e)
        {
           
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.CurrentTriggerJson = JsonConvert.DeserializeObject<TriggerJson>(rtxt_Json.Text);
            txtTriggerName.Text = CurrentTriggerJson.postBack.appId;
            rbtn_con.Checked = CurrentTriggerJson.condition == 0;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string json = rtxt_Json.Text;
            socketClient?.Send("VerifyTriggerConfig", json);
        }
    }
}
