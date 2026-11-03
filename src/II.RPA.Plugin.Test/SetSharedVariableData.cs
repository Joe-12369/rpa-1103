using II.RPA.WinAutomation.CommonUtility;
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
    public partial class SetSharedVariableData : Form
    {
        private object _pluginLoaderObject = null;
        private Type _pluginLoaderType = null;
        public SetSharedVariableData(object pluginLoaderObject, Type pluginLoaderType)
        {
            InitializeComponent();
            _pluginLoaderObject = pluginLoaderObject;
            _pluginLoaderType = pluginLoaderType;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var dic = new Dictionary<string, object>();
            dic["CLIENT-USER"] = "";
            dic["CLIENT-MAC"] = "";
            dic["IsRemoteCaptureServer"] = 0;
            dic["token"] = "";
            dic["engineCode"] = "";
            richTextBox1.Text = ConvertJsonString(dic.ToJson());
        }

        private void button2_Click(object sender, EventArgs e)
        {
            MethodInfo methodRun = _pluginLoaderType.GetMethod("SetSharedVariable");
            methodRun.Invoke(_pluginLoaderObject, new object[] { richTextBox1.Text });
            MessageBox.Show("设置完成");
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

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
