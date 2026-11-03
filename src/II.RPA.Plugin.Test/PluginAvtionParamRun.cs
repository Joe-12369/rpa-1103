using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace II.RPA.Plugin.Test
{
    public partial class PluginAvtionParamRun : Form
    {
        private object _pluginLoaderObject = null;
        private Type _pluginLoaderType = null;
        public PluginAvtionParamRun(object pluginLoaderObject, Type pluginLoaderType)
        {
            InitializeComponent();
            _pluginLoaderObject = pluginLoaderObject;
            _pluginLoaderType = pluginLoaderType;
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        #region JSON格式化方法
        private string ConvertJsonString(string str)
        {
            //格式化json字符串
            JsonSerializer serializer = new JsonSerializer();
            using (TextReader tr = new StringReader(str))
            {

                using (JsonTextReader jtr = new JsonTextReader(tr))
                {
                    object obj = serializer.Deserialize(jtr);
                    if (obj != null)
                    {
                        using (StringWriter textWriter = new StringWriter())
                        {

                            using (JsonTextWriter jsonWriter = new JsonTextWriter(textWriter)
                            {
                                Formatting = Formatting.Indented,
                                Indentation = 4,
                                IndentChar = ' '
                            })
                            {
                                serializer.Serialize(jsonWriter, obj);
                                return textWriter.ToString();
                            }
                        }
                    }
                    else
                    {
                        return str;
                    }
                }
            }
        }

        #endregion

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            MethodInfo methodRun = _pluginLoaderType.GetMethod("RunAsync");
            string result = methodRun.Invoke(_pluginLoaderObject, new object[] { txtActionName.Text.Trim(), "Excute", rtb_runparam.Text }).ToString();
            rtbResult.Text = result;
        }

        private void rtb_runparam_TextChanged_1(object sender, EventArgs e)
        {
            try
            {
                rtb_runparam.Text = ConvertJsonString(rtb_runparam.Text);
            }
            catch
            {
            }
        }

        private void rtbResult_TextChanged_1(object sender, EventArgs e)
        {
            try
            {
                rtbResult.Text = ConvertJsonString(rtbResult.Text);
            }
            catch
            {
            }
        }
    }
}
