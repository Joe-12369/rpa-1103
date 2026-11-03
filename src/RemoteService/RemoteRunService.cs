using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace RemoteService
{
    // 注意: 使用“重构”菜单上的“重命名”命令，可以同时更改代码和配置文件中的类名“RemoteRunService”。
    public class RemoteRunService : IRemoteRunService
    {
        private static string CmdPath = @"C:\Windows\System32\cmd.exe";
        public async Task DoWorkAsync(string user, string pw, string ip, string port = "3389")
        {
            try
            {
                Console.WriteLine($"{ip}：远程开始！");
                string title = GetTitle(user, ip);
                string cmd = $"wfreerdp.exe /u:{user} /p:{pw} /v:{ip}:{port} /f /t:{title}";
                LogHelper.WriteLog(cmd);
                await RunCmd(cmd);
                Console.WriteLine($"{ip}：连接成功！");
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog("连接失败", ex);
                Console.WriteLine($"{ip}：连接失败！{ex.Message}");
            }

        }

        public async Task CancelConnectionAsync(string user, string ip)
        {
            try
            {
                string title = GetTitle(user, ip);
                Console.WriteLine($"{title}：连接关闭！");
                await CloseProcessByTitle(title);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog("取消连接失败", ex);
                Console.WriteLine($"{ip}：取消连接失败！{ex.Message}");
            }
        }

        public async Task RunCmd(string cmd)
        {
            cmd = cmd.Trim().TrimEnd('&') + "&exit";
            await Task.Run(() =>
            {
                using (Process p = new Process())
                {
                    p.StartInfo.FileName = CmdPath;
                    p.StartInfo.UseShellExecute = false;        //是否使用操作系统shell启动
                    p.StartInfo.RedirectStandardInput = true;   //接受来自调用程序的输入信息
                    p.StartInfo.RedirectStandardOutput = true;  //由调用程序获取输出信息
                    p.StartInfo.RedirectStandardError = true;   //重定向标准错误输出
                    p.StartInfo.CreateNoWindow = true;          //不显示程序窗口
                    p.Start();//启动程序

                    //向cmd窗口写入命令
                    p.StandardInput.WriteLine(cmd);
                    p.StandardInput.AutoFlush = true;

                    //ret = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();//等待程序执行完退出进程
                    p.Close();
                }
            });
        }

        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        private async Task CloseProcessByTitle(string title)
        {
            string className = "FreeRDP";
            if (string.IsNullOrEmpty(title))
            {
                return;
            }
            await Task.Run(() =>
            {
                var handel = FindWindow(className, title);
                if (handel != IntPtr.Zero)
                {
                    int calcID;
                    //获取进程ID  
                    GetWindowThreadProcessId(handel, out calcID);
                    Process p = Process.GetProcessById(calcID);
                    p.Kill();
                }
            });
        }

        private string GetTitle(string user, string ip)
        {
            return $"FreeRDP{user}{ip}";
        }
    }
}
