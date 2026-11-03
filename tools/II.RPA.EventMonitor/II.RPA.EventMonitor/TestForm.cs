using Gma.UserActivityMonitor;
using II.RPA.EventMonitor.JSON;
using II.RPA.EventMonitor.Mail;
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

namespace II.RPA.EventMonitor
{
    public partial class TestForm : Form
    {
        private FileSystemWatcher watcher;
        public TestForm()
        {
            InitializeComponent();
            button2.Enabled = false;
        }
        private void TestForm_Load(object sender, EventArgs e)
        {
            tabControl1.SelectedIndex = 1;
        }
        private void InstallHook()
        {
            HookManager.MouseClick += HookManager_MouseClickExt;
        }
        private void UninstallHook()
        {
            //HookManager.MouseClickExt -= HookManager_MouseClickExt;
        }
        private void HookManager_MouseClickExt(object sender, MouseEventArgs e)
        {
            try
            {
                AddMessage(e.Button.ToString());
                //if (this.HasTriggered) return;
                //PrintLog?.Invoke($"按下鼠标：{e.Button.ToString()}，已记录{string.Join(",", userPressKeys)}");
                //if (this.monitorMouseKeyType == e.Button)
                //{
                //    if (this.userPressKeys.Count == this.monitorKeys.Count && this.userPressKeys.All(this.monitorKeys.Contains))
                //    {
                //        PrintLog?.Invoke($"执行事件");
                //        this.TryTrigger(this, e);
                //    }
                //}
            }
            catch (Exception ex)
            {
            }
        }
        #region 让程序不显示在alt+Tab视图窗体中

        ///// <summary>
        ///// 让程序不显示在alt+Tab视图窗体中
        ///// </summary>
        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_EX_APPWINDOW = 0x40000;
                const int WS_EX_TOOLWINDOW = 0x80;
                CreateParams cp = base.CreateParams;
                cp.ExStyle &= (~WS_EX_APPWINDOW);    // 不显示在TaskBar
                cp.ExStyle |= WS_EX_TOOLWINDOW;      // 不显示在Alt+Tab
                return cp;
            }
        }

        #endregion
        #region 设置消息日志
        private void AddMessage(string message)
        {
            this.BeginInvoke(new Action(() =>
            {
                var id = Thread.CurrentThread.ManagedThreadId;
                richTextBox1.AppendText($"{DateTime.Now.ToString("yyyy-MM-ss HH:mm:ss")}[{id}]:{message}{Environment.NewLine}");
                if (richTextBox1.Lines.Length > 100)
                {
                    richTextBox1.Lines = richTextBox1.Lines.Skip(richTextBox1.Lines.Length - 100).ToArray();
                }
                richTextBox1.SelectionStart = richTextBox1.TextLength;
                richTextBox1.ScrollToCaret();

            }));
            Thread.Sleep(10);
        }
        #endregion

        #region 文件监控

        FileTrigger ft;
        private void button1_Click(object sender, EventArgs e)
        {
            ft = new FileTrigger();
            ft.FilePath = txt_filePath.Text;
            ft.FileType = txt_fileFilter.Text;
            ft.ContainSub = cb_containChilds.Checked;
            for (int i = 0; i < cbList_eventTypes.CheckedItems.Count; i++)
            {
                ft.MonitorEvent += cbList_eventTypes.CheckedItems[i] + ";";
            }
            ft.MonitorEvent = ft.MonitorEvent.TrimEnd(';');
            ft.TriggerName = Guid.NewGuid().ToString("N");
            ft.EventTrigger += (s, e2) =>
            {
                FileSystemEventArgs fe = (FileSystemEventArgs)e2;
                //AddMessage("触发完成");
                AddMessage("【文件触发器】" + fe.ChangeType + ":" + fe.FullPath);
            };
            ft.StartTrigger();
            AddMessage("开始监控文件：" + ft.MonitorEvent);
            button1.Enabled = false;
            button2.Enabled = true;
        }



        private void button2_Click(object sender, EventArgs e)
        {
            ft?.StopTrigger();
            ft?.Dispose();
            button1_Click(sender, e);
        }

        #endregion

        #region 邮件监控
        MailTrigger mailTrigger = null;
        //开始
        private void button3_Click(object sender, EventArgs e)
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

            mailTrigger = new MailTrigger(json);

            AddMessage($"【邮件触发器】：开始。。。");
            mailTrigger.StartTrigger();
            //ReceiveEmailServiceApi.DownloadBodyParts(json);
            AddMessage($"【邮件触发器】：已开始");
        }
        //停止
        private void button4_Click(object sender, EventArgs e)
        {
            mailTrigger?.StopTrigger();
        }
        //重新
        private void button6_Click(object sender, EventArgs e)
        {

        }

        private void btn_sendMail_Click(object sender, EventArgs e)
        {
            MailTriggerJson json = new MailTriggerJson()
            {
                protocol = cmb_mialprotocol.Text,
                interval = (int)num_MailInterval.Value,
                serverAddress = "smtp.qq.com",
                port = 465,
                email = txt_account.Text,
                password = txt_mailPwd.Text,
                condition = rd_mailCondition_all.Checked ? 1 : 0,
                sendAddrContent = txt_sendAddrContent.Text,
                recAddrContent = txt_recAddrContent.Text,
                themeContent = txt_themeContent.Text,
                bodyContent = txt_bodyContent.Text,
            };
            //MailBodyEntity mailEntity = new MailBodyEntity();
            //mailEntity.MailTextBody = "MailTextBody";
            //mailEntity.MailBodyType = "plain";
            //mailEntity.Recipients =new List<string>() { "120190411@qq.com" };
            //mailEntity.Sender = "haha";
            //mailEntity.SenderAddress = "1838777688@qq.com";
            //mailEntity.Subject = "Subject";
            //mailEntity.Body = "Body";
            //AddMessage($"【邮件触发器】：开始发送邮件。。。");
            //var config = ReceiveEmailServiceApi.SetSendMessage(json);
            //var ret = MailServiceApi.SendMail(mailEntity, config);
            //AddMessage($"【邮件触发器】：{ret.ResultStatus}");
            //AddMessage($"【邮件触发器】：发送邮件完成");

        }
        #region MyRegion

        #endregion

        #endregion

        #region 按键监控
        HotkeyTrigger hotkeyTrigger = null;
        private void btn_hotkey_start_Click(object sender, EventArgs e)
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
            hotkeyTrigger = new HotkeyTrigger(json);
            AddMessage($"【按键触发器】：开始。。。");
            hotkeyTrigger.EventTrigger += HotKey_EventTrigger;
            //hotkeyTrigger.PrintLog += (string str) =>
            //{
            //    AddMessage($"【按键触发器】：{str}");
            //};
            hotkeyTrigger.StartTrigger();
            AddMessage($"【按键触发器】：已开始");
        }
        private void btn_hotkey_stop_Click(object sender, EventArgs e)
        {
            hotkeyTrigger?.StopTrigger();
        }
        private void HotKey_EventTrigger(object sender, EventArgs e)
        {
            HotkeyTrigger ht = (HotkeyTrigger)sender;
            AddMessage($"【按键触发器】：{ht.TriggerName} 操作已经被触发");
            ht.Continue();
        }

        #endregion

        #region 鼠标监控

        MouseTrigger mouseTrigger = null;
        private void btn_mouse_start_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
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
                mouseTrigger = new MouseTrigger(json);
                AddMessage($"【鼠标触发器】：开始。。。");
                mouseTrigger.EventTrigger += Mouse_EventTrigger;
                //mouseTrigger.PrintLog += (string str) =>
                //{
                //    AddMessage($"【鼠标触发器】：{str}");
                //};
                mouseTrigger.StartTrigger();
                AddMessage($"【鼠标触发器】：已开始");

            });
        }

        private void btn_mouse_stop_Click(object sender, EventArgs e)
        {
            mouseTrigger?.StopTrigger();
        }
        private void Mouse_EventTrigger(object sender, EventArgs e)
        {
            MouseTrigger mt = (MouseTrigger)sender;
            AddMessage($"【鼠标触发器】：{mt.TriggerName} 操作已经被触发");
            mt.Continue();
        }
        #endregion
    }
}
