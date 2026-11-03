using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace II.RPA.Plugin.Test
{
    public partial class ExcelForm : Form
    {
        public Action<string> Callback;
        public Action<string> SaveConfig;
        public string Component
        {
            set
            {
                Invoke(new Action(() => Text = value));
            }
        }
        public string Paramter
        {
            set
            {
                Invoke(new Action(() => richTextBox1.Text = value));
            }
        }
        public string Result
        {
            set
            {
                Invoke(new Action(() => richTextBox2.Text = value));
            }
        }
        public ExcelForm()
        {
            InitializeComponent();

        }
        private void button1_Click(object sender, EventArgs e)
        {
            SaveConfig(richTextBox1.Text);
            Callback(richTextBox1.Text);
        }

        public Dictionary<string, dynamic> DefaultParamter = new Dictionary<string, dynamic>() {
            {
                "New",
                new {
                      path= "",
                      name="test1",
                      software="默认",
                      visible="是",
                      maximize="是",
                      show= "是"
                    }
            }
        };
    }

}
