using II.RPA.Plugin.Core;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace II.RPA.Plugin.Test
{
    public partial class OutLookTest : Form
    {
        private object _pluginLoaderObject = null;
        private Type _pluginLoaderType = null;
        public OutLookTest(object pluginLoaderObject, Type pluginLoaderType)
        {
            InitializeComponent();
            _pluginLoaderObject = pluginLoaderObject;
            _pluginLoaderType = pluginLoaderType;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            MethodInfo methodRun = _pluginLoaderType.GetMethod("RunAsync");
            var data = new
            {
            };
            string result = methodRun.Invoke(_pluginLoaderObject, new object[] { "OutLook.CheckAccount", "Excute", JsonConvert.SerializeObject(data) }).ToString();
            rtbResult.Text = result;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            MethodInfo methodRun = _pluginLoaderType.GetMethod("RunAsync");
            var data = new
            {
                MailAccount = textBox1.Text,
                BoxFolderType = 0,
                BoxFolderName = "",
                CheckScopeType = 0,
                MailToContains = "",
                SendToContains = "",
                MailSubjectContains = textBox4.Text,
                MailHTMLBodyContains = "",
                MailAttachmentType = 0,
                TopCount = 1
            };
            string result = methodRun.Invoke(_pluginLoaderObject, new object[] { "OutLook.CheckMail", "Excute", JsonConvert.SerializeObject(data) }).ToString();
            rtbResult.Text = result;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            MethodInfo methodRun = _pluginLoaderType.GetMethod("RunAsync");

            var mailInfo = rtbResult.Text.ToObject<JMessage>();
            if (mailInfo == null)
                return;
            if (mailInfo.Code == 0)
            {
                var data = new
                {
                    MailAccount = textBox1.Text,
                    MailType = 1,
                    AwaitingOperateMail = mailInfo?.Info,
                    MailReplyType = 0,
                    ReplyMailSubject = textBox4.Text,
                    ReplyMailHTMLBody = textBox5.Text
                };
                string result = methodRun.Invoke(_pluginLoaderObject, new object[] { "OutLook.ReplyMail", "Excute", JsonConvert.SerializeObject(data) }).ToString();
                rtbResult.Text = result;
            }

        }

        private void button4_Click(object sender, EventArgs e)
        {
            MethodInfo methodRun = _pluginLoaderType.GetMethod("RunAsync");

            var mailInfo = rtbResult.Text.ToObject<JMessage>();
            if (mailInfo == null)
                return;
            if (mailInfo.Code == 0)
            {
                var data = new
                {
                    MailAccount = "873716111@qq.com",
                    MailType = 1,
                    AwaitingOperateMail = mailInfo?.Info,
                    MailOperateType = 1
                };
                string result = methodRun.Invoke(_pluginLoaderObject, new object[] { "OutLook.OperateMail", "Excute", JsonConvert.SerializeObject(data) }).ToString();
                rtbResult.Text = result;
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            MethodInfo methodRun = _pluginLoaderType.GetMethod("RunAsync");

            var mailInfo = rtbResult.Text.ToObject<JMessage>();
            if (mailInfo == null)
                return;
            if (mailInfo.Code == 0)
            {
                var data = new
                {
                    MailAccount = "873716111@qq.com",
                    MailTo = "motianyu@i-i.ai",
                    MailSubject = "outlook发送邮件测试",
                    MailHTMLBody = "outlook发送邮件测试outlook发送邮件测试",
                    MailAttachments = @"C:\rpa-workspace\manifest.json"
                };
                string result = methodRun.Invoke(_pluginLoaderObject, new object[] { "OutLook.SendMail", "Excute", JsonConvert.SerializeObject(data) }).ToString();
                rtbResult.Text = result;
            }
        }
    }
}
