using II.RPA.Plugin.Core;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.RPASystem.Helper
{
    public class RdpHelper
    {
        private static ILogger logger = Logger.OfContext();
        public const string CLASS_NAME = "FreeRDP";
        private static readonly string cmdPath = @"C:\Windows\System32\cmd.exe";
        public static void Connection(string user, string pw, string ip, string port = "3389")
        {
            string title = GetTitle(user, ip);
            string path = AppDomain.CurrentDomain.BaseDirectory;
            logger.LogWarning("远程路径：" + path);
            string cmd = $"start owfreerdp.exe /u:{user} /p:{pw} /v:{ip}:{port} /w:{SystemParameters.FullPrimaryScreenWidth} /h:{SystemParameters.FullPrimaryScreenHeight} /t:{title}";
            RunCmd(path, cmd);
        }
        private static void RunCmd(string path, string cmd)
        {
            cmd = cmd.Trim().TrimEnd('&') + "&exit";
            using (Process p = new Process())
            {
                p.StartInfo.FileName = cmdPath;
                p.StartInfo.UseShellExecute = false;        //是否使用操作系统shell启动
                p.StartInfo.RedirectStandardInput = true;   //接受来自调用程序的输入信息
                p.StartInfo.RedirectStandardOutput = true;  //由调用程序获取输出信息
                p.StartInfo.RedirectStandardError = true;   //重定向标准错误输出
                p.StartInfo.CreateNoWindow = true;          //不显示程序窗口
                p.Start();//启动程序

                //向cmd窗口写入命令
                p.StandardInput.WriteLine("cd /d " + path);
                p.StandardInput.WriteLine(cmd);
                p.StandardInput.AutoFlush = true;

                //ret = p.StandardOutput.ReadToEnd();
                p.WaitForExit();//等待程序执行完退出进程
                p.Close();
            }
        }

        public static string GetTitle(string user, string ip)
        {
            return $"FreeRDP{user}{ip}";
        }
    }
}
