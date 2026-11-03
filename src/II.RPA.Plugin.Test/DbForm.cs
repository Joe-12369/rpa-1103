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
    public partial class DbForm : Form
    {
        private object _pluginLoaderObject = null;
        private Type _pluginLoaderType = null;
        public DbForm(object pluginLoaderObject, Type pluginLoaderType)
        {

            InitializeComponent();
            cboReturnDic.SelectedIndex = 0;
            cboServerType.SelectedIndex = 0;
            _pluginLoaderObject = pluginLoaderObject;
            _pluginLoaderType = pluginLoaderType;
        }

        private void btnCreateDbConn_Click(object sender, EventArgs e)
        {
            //DbServerConnStr dbserver = new DbServerConnStr();
            //dbserver.DataSource = txtDbAddress.Text;
            //dbserver.DbName = txtDbName.Text;
            //dbserver.Port = txtDbPort.Text;
            //dbserver.Pwd = txtPwd.Text;
            //dbserver.Sid = txtSID.Text;
            //dbserver.UserName = txtUid.Text;
            //dbserver.Filepath = txtFilepath.Text;
            //string constr = JsonHelper.JsonByNewtonJson(dbserver);
            //string result = db.CreateDbConnection(cboServerType.SelectedIndex, constr);
            //rtbResult.Text = result;
            MethodInfo methodRun = _pluginLoaderType.GetMethod("RunAsync");
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("DataSource", txtDbAddress.Text);
            dic.Add("DbName", txtDbName.Text);
            dic.Add("Port", txtDbPort.Text);
            dic.Add("Pwd", txtPwd.Text);
            dic.Add("Sid", txtSID.Text);
            dic.Add("UserName", txtUid.Text);
            dic.Add("Filepath", txtFilepath.Text);
            var data = new
            {
                @DbType = cboServerType.SelectedIndex,
                ConnectStr = JsonConvert.SerializeObject(dic)
            };
            string result = methodRun.Invoke(_pluginLoaderObject, new object[] { "Database.CreateDbConnection", "Excute", JsonConvert.SerializeObject(data) }).ToString();
            rtbResult.Text = result;
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                txtDbID1.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }

        private void btnSingleQuery_Click(object sender, EventArgs e)
        {
            //string result = db.QuerySingle(txtDbID1.Text, txtSQL1.Text, txtSQLParam.Text, cboReturnDic.SelectedIndex);
            //rtbResult.Text = result;
            MethodInfo methodRun = _pluginLoaderType.GetMethod("RunAsync");
            var data = new
            {
                ConnectionID = txtDbID1.Text,
                Sql = txtSQL1.Text,
                Paras = txtSQLParam.Text,
                Showdic = cboReturnDic.SelectedIndex
            };
            string result = methodRun.Invoke(_pluginLoaderObject, new object[] { "Database.QuerySingle", "Excute", JsonConvert.SerializeObject(data) }).ToString();
            rtbResult.Text = result;
        }

        private void btnQueryAll_Click(object sender, EventArgs e)
        {
            //string result = db.QueryAll(txtDbID1.Text, txtSQL1.Text, txtSQLParam.Text, cboReturnDic.SelectedIndex);
            //rtbResult.Text = result;
            MethodInfo methodRun = _pluginLoaderType.GetMethod("RunAsync");
            var data = new
            {
                ConnectionID = txtDbID1.Text,
                Sql = txtSQL1.Text,
                Paras = txtSQLParam.Text,
                Showdic = cboReturnDic.SelectedIndex
            };
            string result = methodRun.Invoke(_pluginLoaderObject, new object[] { "Database.QueryAll", "Excute", JsonConvert.SerializeObject(data) }).ToString();
            rtbResult.Text = result;
        }

        private void btnExecuteSQL_Click(object sender, EventArgs e)
        {
            //string result = db.ExecuteSql(txtDbID2.Text, txtSQL2.Text, txtSQLParam2.Text);
            //rtbResult.Text = result;

            MethodInfo methodRun = _pluginLoaderType.GetMethod("RunAsync");
            var data = new
            {
                ConnectionID = txtDbID1.Text,
                Sql = txtSQL1.Text,
                Paras = txtSQLParam.Text
            };
            string result = methodRun.Invoke(_pluginLoaderObject, new object[] { "Database.ExecuteSql", "Excute", JsonConvert.SerializeObject(data) }).ToString();
            rtbResult.Text = result;
        }

        private void btnExecuteSQLBatch_Click(object sender, EventArgs e)
        {
            //string result = db.BatchExecuteSql(txtDbID2.Text, txtSQL2.Text, txtSQLParam2.Text);
            //rtbResult.Text = result;

            MethodInfo methodRun = _pluginLoaderType.GetMethod("RunAsync");
            var data = new
            {
                ConnectionID = txtDbID1.Text,
                Sql = txtSQL1.Text,
                Paras = txtSQLParam.Text
            };
            string result = methodRun.Invoke(_pluginLoaderObject, new object[] { "Database.BatchExecuteSql", "Excute", JsonConvert.SerializeObject(data) }).ToString();
            rtbResult.Text = result;
        }

        private void btnCloseCon_Click(object sender, EventArgs e)
        {
            //string result = db.CloseConnection(txtDbID3.Text);
            //rtbResult.Text = result;
            MethodInfo methodRun = _pluginLoaderType.GetMethod("RunAsync");
            var data = new
            {
                ConnectionID = txtDbID1.Text,
            };
            string result = methodRun.Invoke(_pluginLoaderObject, new object[] { "Database.CloseConnection", "Excute", JsonConvert.SerializeObject(data) }).ToString();
            rtbResult.Text = result;
        }
    }
    class DbServerConnStr
    {
        public string DataSource { get; set; }
        public string DbName { get; set; }
        public string UserName { get; set; }
        public string Pwd { get; set; }
        public string Charset { get; set; }
        public string Port { get; set; }
        public string Sid { get; set; }
        public string Filepath { get; set; }
    }
}
