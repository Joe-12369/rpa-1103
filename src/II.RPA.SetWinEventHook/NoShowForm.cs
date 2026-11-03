using II.RPA.WinAutomation.CommonUtility.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace II.RPA.SetWinEventHook
{

    public partial class NoShowForm : Form
    {

        public NoShowForm()
        {
            InitializeComponent();
        }
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
        private void Form1_Load(object sender, EventArgs e)
        {
            this.Location = new Point(-1000, -1000);
            base.SetVisibleCore(true);
            System.Timers.Timer timer = new System.Timers.Timer(10000);
            timer.Elapsed += new System.Timers.ElapsedEventHandler(killSelfProcess);
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        public static string FactoryProcessName
        {
            get
            {
                if (RPAEnvironment.GetInstance().CurrentLanguage == LanguageCultureEnum.zh_CN)
                {
                    return "实在设计器";
                }
                else
                {
                    return "Z-Factory";
                }
            }
        }

        public void killSelfProcess(object source, System.Timers.ElapsedEventArgs e)
        {
            Process[] existFactory = Process.GetProcessesByName(FactoryProcessName);
            if (existFactory.Length < 1)
            {
                existFactory = Process.GetProcessesByName("Z-Bot");
                if (existFactory.Length < 1)
                {
                    //强制关闭进程  
                    string exeName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                    string[] exeArray = exeName.Split('\\');
                    RunCmd("taskkill /im " + exeArray[exeArray.Length - 1] + " /f ");
                }
            }
        }
        /// <summary>  
        /// 运行DOS命令  
        /// DOS关闭进程命令(ntsd -c q -p PID )PID为进程的ID  
        /// </summary>  
        /// <param name="command"></param>  
        /// <returns></returns>  
        public static void RunCmd(string command)
        {
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.Arguments = "/c " + command;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;
            p.Start();
        }
    }
}
