using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DisconnectRemote
{
    class Program
    {
        static void Main(string[] args)
        {
            /**
              * 当前用户是管理员的时候，直接启动应用程序
              * 如果不是管理员，则使用启动对象启动程序，以确保使用管理员身份运行
              */
            //获得当前登录的Windows用户标示
            System.Security.Principal.WindowsIdentity identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            System.Security.Principal.WindowsPrincipal principal = new System.Security.Principal.WindowsPrincipal(identity);
            //判断当前登录用户是否为管理员
            if (principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator))
            {
                //如果是管理员，则直接运行
                Start(args);
            }
            else
            {
                //创建启动对象
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.UseShellExecute = true;
                startInfo.WorkingDirectory = Environment.CurrentDirectory;
                startInfo.FileName = Application.ExecutablePath;
                //设置启动动作,确保以管理员身份运行
                startInfo.Verb = "runas";
                try
                {
                    System.Diagnostics.Process.Start(startInfo);
                }
                catch
                {
                    return;
                }
                //退出
                Application.Exit();
            }
        }
        private static void Start(string[] args)
        {
            try
            {
                string cmsText = "query session";
                string result = ExecuteCmd(cmsText, out string error);
                if (!string.IsNullOrEmpty(error))
                {
                    WriteLogInfo($"{cmsText}:{error}");
                    return;
                }
                int sid = -1;
                StringReader sr = new StringReader(result);
                while (sr.Peek() > -1)
                {
                    string lins = sr.ReadLine();
                    if (lins.StartsWith(">"))
                    {
                        var items = lins.Split(' ').ToList();
                        items.RemoveAll(s => s == "");
                        var index = items.FindLastIndex(s => s == "运行中");
                        index = index > 0 ? index : items.FindLastIndex(s => s == "Active");
                        if (index > 1)
                        {
                            int.TryParse(items[index - 1], out sid);
                            if (items[0] == ">console")
                            {
                                MessageBox.Show("此程序只可以在远程桌面时使用哦", "信息提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                        }
                    }
                }
                if (sid <= 0)
                {
                    return;
                }
                cmsText = $"tscon.exe {sid} /dest:console";
                result = ExecuteCmd(cmsText, out error);
                if (!string.IsNullOrEmpty(error))
                {
                    WriteLogInfo($"{cmsText}:{error}");
                    return;
                }
            }
            catch (Exception ex)
            {
                WriteLogInfo(ex);
            }
        }
        private static string ExecuteCmd(string cmd, out string error)
        {
            string output = "";
            error = "";
            string cmdPath = $@"{ Environment.GetFolderPath(Environment.SpecialFolder.Windows)}\Sysnative\cmd.exe";
            Process process = new Process();     //创建进程对象
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.LoadUserProfile = true;
            startInfo.FileName = cmdPath;      //设定需要执行的命令
            startInfo.Arguments = "/C " + cmd;   //设定参数，其中的“/C”表示执行完命令后马上退出
            startInfo.UseShellExecute = false;     //不使用系统外壳程序启动
            startInfo.RedirectStandardInput = false;   //不重定向输入
            startInfo.RedirectStandardOutput = true;   //重定向输出
            startInfo.RedirectStandardError = true;
            startInfo.CreateNoWindow = true;     //不创建窗口
            process.StartInfo = startInfo;
            try
            {
                if (process.Start())       //开始进程
                {
                    process.WaitForExit(2000);  //这里等待进程结束，等待时间为指定的毫秒
                    output = process.StandardOutput.ReadToEnd();//读取进程的输出
                    error = process.StandardError.ReadToEnd();

                    if (!string.IsNullOrEmpty(error))
                    {
                        WriteLogInfo(error);
                        //throw new Exception(error);
                    }

                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (process != null)
                    process.Close();
            }
            return output;
        }
        /// <summary> 
        /// 创建日志文件 
        /// </summary> 
        /// <param name="ex">异常类</param> 
        private static void WriteLogInfo(string log)
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
                if (!Directory.Exists(path))
                {
                    //创建日志文件夹 
                    Directory.CreateDirectory(path);
                }
                //发生异常每天都创建一个单独的日子文件[*.log],每天的错误信息都在这一个文件里。方便查找 
                path = Path.Combine(path, DateTime.Now.ToString("yyyy-MM-dd") + ".log");
                using (StreamWriter sw = new StreamWriter(path, true, Encoding.Default))
                {
                    sw.WriteLine(log);
                }
            }
            catch (Exception)
            {

            }
        }
        /// <summary> 
        /// 写日志信息 
        /// </summary> 
        /// <param name="ex">异常类</param> 
        /// <param name="path">日志文件存放路径</param> 
        private static void WriteLogInfo(Exception ex)
        {
            try
            {

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("*****************************************【"
                               + DateTime.Now.ToLongTimeString()
                               + "】*****************************************");
                if (ex != null)
                {
                    sb.AppendLine("【ErrorType】" + ex.GetType());
                    sb.AppendLine("【TargetSite】" + ex.TargetSite);
                    sb.AppendLine("【Message】" + ex.Message);
                    sb.AppendLine("【Source】" + ex.Source);
                    sb.AppendLine("【StackTrace】" + ex.StackTrace);
                }
                else
                {
                    sb.AppendLine("Exception is NULL");
                }
                sb.AppendLine();
                WriteLogInfo(sb.ToString());
            }
            catch (Exception)
            {

            }
        }
    }
}
