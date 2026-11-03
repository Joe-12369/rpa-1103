using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.MobileDeviceManager.CV
{
    public class AdbCVHelper
    {
        private static ILogger logger = Logger.OfContext();

        public static void SetText(string text)
        {
            string command = $"adb shell am broadcast -a ADB_INPUT_TEXT --es msg '{text}'";
            ExShell(command);
        }
        public static void SetText(string text, string id)
        {
            string command = $"adb {id} shell am broadcast -a ADB_INPUT_TEXT --es msg '{text}'";
            logger.LogInformation(command);
            ExShell(command);
        }
        private static void ExShell(string command)
        {
            try
            {
                using (Process p = new Process())
                {
                    //实例一个process类
                    Process process = new Process();
                    process.StartInfo.FileName = "cmd.exe";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardInput = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;
                    //开始
                    process.Start();
                    //输入命令，退出
                    process.StandardInput.WriteLine(command);
                    process.StandardInput.WriteLine("exit");
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static void SetDefaultIme(string id, string ime)
        {
            string command = $"adb {id} shell ime set {ime}";
            ExShell(command);
        }

        public static bool GetLastResultCmd(string cmd, out string result)
        {
            cmd = cmd.Trim().TrimEnd('&') + "&exit";
            using (Process p = new Process())
            {
                p.StartInfo.FileName = "cmd.exe";
                p.StartInfo.UseShellExecute = false;        //是否使用操作系统shell启动
                p.StartInfo.RedirectStandardInput = true;   //接受来自调用程序的输入信息
                p.StartInfo.RedirectStandardOutput = true;  //由调用程序获取输出信息
                p.StartInfo.RedirectStandardError = true;   //重定向标准错误输出
                p.StartInfo.CreateNoWindow = true;          //不显示程序窗口
                p.Start();//启动程序
                //向cmd窗口写入命令
                p.StandardInput.WriteLine(cmd);
                p.StandardInput.AutoFlush = true;
                p.WaitForExit();//等待程序执行完退出进程
                if (p.ExitCode == 0)
                {
                    do
                    {
                        result = p.StandardOutput.ReadLine();
                    } while (!p.StandardOutput.EndOfStream);
                    return true;
                }
                else
                {
                    result = p.StandardError.ReadToEnd();
                    result += $"ExitCode:{p.ExitCode}";
                    return false;
                }
            }
        }
    }
}
