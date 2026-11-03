using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
    public partial class UsbHubForm : Form
    {
        private object _pluginLoaderObject = null;
        private Type _pluginLoaderType = null;
        public UsbHubForm(object pluginLoaderObject, Type pluginLoaderType)
        {
            InitializeComponent();
            _pluginLoaderObject = pluginLoaderObject;
            _pluginLoaderType = pluginLoaderType;
        }

        private void btnGetCom_Click(object sender, EventArgs e)
        {
            MethodInfo methodRun = _pluginLoaderType.GetMethod("RunAsync");
            string coms = methodRun.Invoke(_pluginLoaderObject, new object[] { "UsbHub.LocalGetComs", "Excute", JsonConvert.SerializeObject("{}") }).ToString();
            try
            {

                JObject obj = JObject.Parse(coms);
                string rsult = obj.Value<string>("info");
                JArray array = JArray.Parse(rsult);
                foreach (var item in array.Children())
                {
                    cboComList.Items.Add(item.ToString());
                }

                cboComList.SelectedIndex = 0;

            }
            catch (Exception)
            {
                rtbResult.Text = "异常：" + coms;
            }
        }

        private void btnOpenChannel_Click(object sender, EventArgs e)
        {
            //string result = usb.Open(cboComList.SelectedItem.ToString(), txtChannel.Text);
            MethodInfo methodRun = _pluginLoaderType.GetMethod("RunAsync");
            var data = new
            {
                portName = cboComList.SelectedItem.ToString(),
                channel = txtChannel.Text
            };
            string result = methodRun.Invoke(_pluginLoaderObject, new object[] { "UsbHub.LocalOpen", "Excute", JsonConvert.SerializeObject(data) }).ToString();
            rtbResult.Text = result;
        }

        private void btnCloseChannel_Click(object sender, EventArgs e)
        {
            MethodInfo methodRun = _pluginLoaderType.GetMethod("RunAsync");
            var data = new
            {
                portName = cboComList.SelectedItem.ToString(),
                channel = txtChannel.Text
            };
            string result = methodRun.Invoke(_pluginLoaderObject, new object[] { "UsbHub.LocalClose", "Excute", JsonConvert.SerializeObject(data) }).ToString();

            rtbResult.Text = result;
        }

        private void btnAddNetChannel_Click(object sender, EventArgs e)
        {
            //string result = net.Open(txtIP.Text, int.Parse(txtPort.Text), txtNetChannel.Text);
            MethodInfo methodRun = _pluginLoaderType.GetMethod("RunAsync");
            var data = new
            {
                ipAddress= txtIP.Text,
                port = int.Parse(txtPort.Text),
                channel = txtNetChannel.Text
            };
            string result = methodRun.Invoke(_pluginLoaderObject, new object[] { "UsbHub.NetworkOpen", "Excute", JsonConvert.SerializeObject(data) }).ToString();

            rtbNetResult.Text = result;
        }

        private void btnCloseNetChannel_Click(object sender, EventArgs e)
        {
            //string result = net.Close(txtIP.Text, int.Parse(txtPort.Text), txtNetChannel.Text);
            MethodInfo methodRun = _pluginLoaderType.GetMethod("RunAsync");
            var data = new
            {
                ipAddress = txtIP.Text,
                port = int.Parse(txtPort.Text),
                channel = txtNetChannel.Text
            };
            string result = methodRun.Invoke(_pluginLoaderObject, new object[] { "UsbHub.NetworkClose", "Excute", JsonConvert.SerializeObject(data) }).ToString();

            rtbNetResult.Text = result;
        }
    }
}
