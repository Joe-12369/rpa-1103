using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.PatchUpdate.Helpers
{
    public static class ApplicationHelper
    {
        private static Dictionary<string, Mutex> MutexForSingletonExeDic = new Dictionary<string, Mutex>();

        /// <summary>
        /// StartApplication 启动一个应用程序/进程。
        /// </summary>
        /// <param name="appFilePath"></param>
        /// <param name="args"></param>
        public static void StartApplication(string appFilePath, string args = null)
        {
            Process process = new Process();
            process.StartInfo.FileName = appFilePath;
            if (!string.IsNullOrEmpty(args))
            {
                process.StartInfo.Arguments = args;
            }
            process.Start();
        }

        /// <summary>
        /// IsAppInstanceExist 目标应用程序是否已经启动。通常用于判断单实例应用。将占用锁。
        /// </summary>
        /// <param name="instanceName"></param>
        /// <returns></returns>
        public static bool IsAppInstanceExist(string instanceName)
        {
            bool createdNew = false;
            Mutex value = new Mutex(initiallyOwned: true, instanceName, out createdNew);
            if (createdNew)
            {
                MutexForSingletonExeDic.Add(instanceName, value);
            }
            return !createdNew;
        }

        /// <summary>
        /// 判断目标应用程序是否已经启动
        /// </summary>
        /// <param name="processName"></param>
        /// <returns></returns>
        public static bool FindExistByProcessName(string processName)
        {
            return Process.GetProcessesByName(processName).Length > 0;
        }

        /// <summary>
        /// 根据进程名杀掉进程
        /// </summary>
        /// <param name="processName"></param>
        /// <returns></returns>
        public static bool KillExistByProcessName(string processName)
        {
            try
            {
                Process[] ps = Process.GetProcessesByName(processName);
                if (ps.Length > 0)
                {
                    foreach (Process p in ps)
                        p.Kill();
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// 释放由IsAppInstanceExist占用的锁。
        /// </summary>
        /// <param name="instanceName"></param>
        public static void ReleaseAppInstance(string instanceName)
        {
            Mutex mutex = null;
            if (MutexForSingletonExeDic.ContainsKey(instanceName))
            {
                mutex = MutexForSingletonExeDic[instanceName];
            }

            if (mutex != null)
            {
                mutex.Close();
                MutexForSingletonExeDic.Remove(instanceName);
            }
        }

        /// <summary>
        /// 执行DOS命令，返回DOS命令的输出
        /// </summary>
        /// <param name="command">dos命令</param>
        /// <param name="seconds">等待命令执行的时间（单位：毫秒），如果设定为0，则无限等待</param>
        /// <returns>返回DOS命令的输出</returns>
        public static string ExecuteCmd(string command, int seconds)
        {
            string output = ""; //输出字符串
            if (command != null && !command.Equals(""))
            {
                Process process = new Process(); //创建进程对象
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.Verb = "runas";
                startInfo.FileName = "cmd.exe"; //设定需要执行的命令
                startInfo.Arguments = "/C" + command; //"/C"表示执行完命令后马上退出
                startInfo.UseShellExecute = false; //不使用系统外壳程序启动
                startInfo.RedirectStandardInput = false; //不重定向输入
                startInfo.RedirectStandardOutput = true; //重定向输出
                startInfo.CreateNoWindow = true; //不创建窗口

                process.StartInfo = startInfo;
                try
                {
                    if (process.Start()) //开始进程
                    {
                        if (seconds == 0)
                        {
                            process.WaitForExit(); //无限等待进程结束
                        }
                        else
                        {
                            process.WaitForExit(seconds); //等待进程结束，等待时间为制定的毫秒
                        }
                        output = process.StandardOutput.ReadToEnd(); //读取进程的输出
                    }
                }
                catch (Exception ex)
                {
                }
                finally
                {
                    if (process != null)
                        process.Close();
                }
            }
            return output;
        }

        /// <summary>
        /// OpenUrl 在浏览器中打开wsUrl链接
        /// </summary>
        /// <param name="url"></param>
        public static void OpenUrl(string url)
        {
            Process.Start(url);
        }

        /// <summary>
        /// 获取Patch包信息
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static PatchInfo GetPatchInfo(string filePath = "")
        {
            if (File.Exists(filePath))
            {
                PatchInfo patchInfo = JsonConvert.DeserializeObject<PatchInfo>(File.ReadAllText(filePath));
                return patchInfo;
            }
            else
                return null;
        }

        /// <summary>
        /// 获取Factory或Bot的行业信息
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static ConfigInfo GetConfigInfo(string filePath = "")
        {
            if (File.Exists(filePath))
            {
                ConfigInfo configInfo = JsonConvert.DeserializeObject<ConfigInfo>(File.ReadAllText(filePath));
                return configInfo;
            }
            else
                return null;
        }

        /// <summary>
        /// 获取Factory或Bot的版本信息
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetVersion(string filePath)
        {
            string version = string.Empty;
            FileVersionInfo file = FileVersionInfo.GetVersionInfo(filePath);
            //版本号显示为“主版本号.次版本号.内部版本号.专用部件号”。
            //version = String.Format("{0}.{1}.{2}.{3}", file.FileMajorPart, file.FileMinorPart, file.FileBuildPart, file.FilePrivatePart);
            //使用文件版本信息
            version = file.FileVersion;
            return version;
        }

        /// <summary>
        /// 获取Factory或Bot安装目录 约定离线更新需将Patch包放在Factory或Bot根目录下
        /// </summary>
        /// <param name="patchInfo"></param>
        /// <returns></returns>
        public static string GetInstPath(PatchInfo patchInfo)
        {
            //string matchType = patchInfo.type == "实在设计器" ? "Z-Factory" : "Z-Bot";
            //string matchVersion = patchInfo.version;
            string dir = Path.GetFullPath(@"....");
            return dir;
            //不做壳程序，以下代码先注释
            //DirectoryInfo folderInfo = new DirectoryInfo(dir);
            //foreach(DirectoryInfo dirInfo in folderInfo.GetDirectories())
            //{
            //    if (dirInfo.FullName.Contains(matchType) && dirInfo.FullName.Contains(matchVersion))
            //    {
            //        return dirInfo.FullName;
            //    }
            //}
            //return "";
        }

        /// <summary>
        /// 获取Patch包状态
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static PatchStatus GetPatchStatus(string filePath)
        {
            if (File.Exists(filePath))
            {
                PatchStatus patchStatus = JsonConvert.DeserializeObject<PatchStatus>(File.ReadAllText(filePath));
                return patchStatus;
            }
            else
                return null;
        }

        /// <summary>
        /// 更新更新状态
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="patchStatus"></param>
        /// <exception cref="Exception"></exception>
        public static void UpdatePatchStatus(string filePath, PatchStatus patchStatus)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    string convertString = JsonConvert.SerializeObject(patchStatus, Formatting.Indented);//将json装换为string
                    File.WriteAllText(filePath, convertString);//将内容写进json文件中
                }
                else
                    throw new Exception("Patch状态文件不存在");
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
