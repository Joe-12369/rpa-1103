using Grpc.Core;
using Microsoft.Win32;
using Newtonsoft.Json;
using Rpa.Core.Plugin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Text;
using System.Threading;

namespace JavaPluginInstaller
{
    internal class Message
    {
        public int status { get; set; } = 1;    //返回状态
        public int code { get; set; } = 0;// 返回错误码
        public string message { get; set; } = ""; //如果失败，返回失败原因
        public object info { get; set; } = "";             //非必填，只有当需要返回信息的接口时才需要
    }
    internal class JvmInfo
    {
        [JsonProperty("appname")]
        public string AppName { get; set; }
        [JsonProperty("jvmpath")]
        public string JvmPath { get; set; }
        [JsonProperty("pid")]
        public int Pid { get; set; }
    }
    internal enum ReturnStatus
    {
        Cancel = -1,//取消
        Fail = 0,//失败
        Success = 1,//成功
    }
    public enum JavaExtensionStatue
    {
        JavaExtensionUnInstall = 0,
        JavaExtensionInstallSuccess = 1,
        JavaUnInstall = 2, //界面展示  未检测到Java环境，若确认有安装请点击确定，手动输入java.exe路径。
        JavaNotClose = 3, //界面展示 如需安装Java插件，请先关闭Java程序，再点击确定。
        FileCopyFail = 4, //界面展示 文件复制失败，查看原因。
        RegeditWriteFail = 5, //界面展示 Java插件安装成功。
        UnJvm = 6, //界面展示 未检测到已开启的【Java应用】，请检查环境后重试。
        MoreJvm = 7, //界面展示 检测到多个已开启的【Java应用】，请关闭多余应用后重试。
        LackJvmVersion = 8, //界面展示 缺少对应版本的Java拾取环境请联系管理员获取
        ListdllsTimeout = 9, //Listdlls查询超时
        ListdllsParseFail = 10, //Listdlls结果解析失败
        Unknown = 99 //未知异常

    }
    class Program
    {
        #region [Java安装]
        public static string GetProcessOwner(int processId)
        {
            foreach (ManagementObject obj in new ManagementObjectSearcher("Select * From Win32_Process Where ProcessID = " + processId).Get())
            {
                string[] argList = new string[2]
                {
                string.Empty,
                string.Empty
                };
                object[] args = argList;
                if (Convert.ToInt32(obj.InvokeMethod("GetOwner", args)) == 0)
                {
                    return argList[0];
                }
            }
            return "NO OWNER";
        }
        public static Process[] GetProcessesByName(string processName, bool onlyCurrentUser)
        {
            if (!onlyCurrentUser)
            {
                return Process.GetProcessesByName(processName);
            }
            return (from m in Process.GetProcessesByName(processName)
                    where GetProcessOwner(m.Id) == Environment.UserName
                    select m).ToArray();
        }
        private static void KillPrevHost()
        {
            Process[] processesByName = GetProcessesByName("JavaPluginInstaller", onlyCurrentUser: true);
            int curPid = Process.GetCurrentProcess().Id;
            Process[] array = processesByName;
            foreach (Process process in array)
            {
                if (process.Id != curPid)
                {
                    try
                    {
                        process.Kill();
                    }
                    catch
                    {
                    }
                }
            }
        }

        private static bool WriteRegedit()
        {
            try
            {
                SetJavaRegistry();
                return true;
            }
            catch (Exception e)
            {
                LogError(e);
                return false;
            }
        }
        private static void InstallJavaExtension(string javaPath = null)
        {
            /*
             删除Java更新程序
             检测java 环境
                1.命令行模式 拿到版本 是否为64位
                    默认 java -version
                    注册表路径 java -version
                    指定路径 java -version
                2.检测Java程序是否打开
                    判断java进程 
                3.确定版本 写入对应文件
                4.写入注册表
            */

            Message msg = new Message();
            DelJavaUpdate();
            Thread.Sleep(500);
            string filePath;
            try
            {
                if (!string.IsNullOrEmpty(javaPath))
                {
                    filePath = Path.Combine(new FileInfo(javaPath).Directory.Parent.FullName, "java.exe");
                }
                else
                {
                    if (!Helper.GetJvmPath(out filePath, out JavaExtensionStatue mes, out object extraInfo))
                    {
                        msg.status = (int)ReturnStatus.Fail;
                        msg.code = (int)mes;
                        if (mes == JavaExtensionStatue.MoreJvm)
                        {
                            msg.info = extraInfo;
                        }
                        Loggin(msg);
                        return;
                    }
                }
            }
            catch (InstallerException ex)
            {
                msg.status = (int)ReturnStatus.Fail;
                msg.code = (int)ex.Statue;
                msg.message = ex.Message;
                Loggin(msg);
                return;
            }
            JavaInfo javaInfo = Helper.GetJvmVersion(filePath);

            var versionInfo = FileVersionInfo.GetVersionInfo(filePath);
            bool highVersion = versionInfo.FileMajorPart > 8;
            javaInfo.HighVersion = highVersion;
            string current = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

            if (!CopyWindowsAccessBridge(javaInfo, current))
            {
                msg.status = (int)ReturnStatus.Fail;
                msg.code = (int)JavaExtensionStatue.LackJvmVersion;
                //msg.message = $"缺少{javaInfo.GetVersion()}版本的Java拾取环境请联系运营人员获取";
                msg.message = javaInfo.GetVersion();
                Loggin(msg);
                return;
            }

            if (!CopyProperties(javaInfo, current))
            {
                msg.status = (int)ReturnStatus.Fail;
                msg.code = (int)JavaExtensionStatue.FileCopyFail;
                Loggin(msg);
                return;
            }

            //写入Jab文件
            if (!CopyExt(javaInfo, current))
            {
                msg.status = (int)ReturnStatus.Fail;
                msg.code = (int)JavaExtensionStatue.LackJvmVersion;
                msg.message = javaInfo.GetVersion();
                Loggin(msg);
                return;
            }

            //写入Runtime文件
            if (!CopyBin(javaInfo, current))
            {
                msg.status = (int)ReturnStatus.Fail;
                msg.code = (int)JavaExtensionStatue.LackJvmVersion;
                msg.message = javaInfo.GetVersion();
                Loggin(msg);
                return;
            }
            Exectue(Path.GetDirectoryName(filePath));
            //写入jab文件
            //if (!CopyJabFileToPath(version))
            //{
            //    msg.status = (int)ReturnStatus.Fail;
            //    msg.code = (int)JavaExtensionStatue.FileCopyFail;
            //    Loggin(msg);
            //    return;
            //}

            //写入注册表
            //if (!WriteRegedit())
            //{
            //    msg.status = (int)ReturnStatus.Fail;
            //    msg.code = (int)JavaExtensionStatue.RegeditWriteFail;
            //    Loggin(msg);
            //    return;
            //}

            msg.status = (int)ReturnStatus.Success;
            msg.code = (int)JavaExtensionStatue.JavaExtensionInstallSuccess;
            Loggin(msg);
            RestartRuntimeHost();
            return;


        }
        private static void RestartRuntimeHost()
        {
            try
            {
                int port = 7181;
                if (!AppDomain.CurrentDomain.BaseDirectory.Contains("Z-Factory"))
                {
                    port = 7182;
                }
                Channel channel = new Channel($"127.0.0.1:{port}", ChannelCredentials.Insecure);
                var client = new PluginService.PluginServiceClient(channel);
                _ = client.PluginStopUnary(new StopPluginRequest
                {
                    ClientId = Assembly.GetExecutingAssembly().GetName().Name,
                    PackagePath = "",
                });
            }
            catch (Exception)
            {
                //MessageBox.Show(ex.ToString());
            }
        }
        private static bool CopyBin(JavaInfo javaInfo, string current)
        {
            try
            {
                if (javaInfo.HighVersion)
                {
                    return true;
                }
                var runtimeDlls = javaInfo.GetBinFile();
                foreach (var item in runtimeDlls)
                {
                    string targetFile = Path.Combine(javaInfo.GetBinPath(), item);
                    if (File.Exists(targetFile))
                    {
                        continue;
                    }
                    string originFile = Path.Combine(current, javaInfo.GetVersion(), item);
                    if (!File.Exists(originFile))
                    {
                        return false;
                    }
                    DoCopyFile(originFile, targetFile);
                }
                return true;
            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
            }
        }

        private static bool CopyExt(JavaInfo javaInfo, string current)
        {
            try
            {
                if (javaInfo.HighVersion)
                {
                    return true;
                }
                var runtimeDlls = javaInfo.GetExtFile();
                foreach (var item in runtimeDlls)
                {
                    string targetFile = Path.Combine(javaInfo.GetExtPath(), item);
                    if (File.Exists(targetFile))
                    {
                        continue;
                    }
                    string originFile = Path.Combine(current, javaInfo.GetVersion(), item);
                    if (!File.Exists(originFile))
                    {
                        return false;
                    }
                    DoCopyFile(originFile, targetFile);
                }
                return true;
            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
            }
        }
        /// <summary>
        /// 64 位 Windows 上「真正的」System32（供 WindowsAccessBridge-64.dll）。32 位进程若直接使用 SpecialFolder.System，
        /// 文件会被 WOW64 重定向写到 SysWOW64，64 位客户端在 System32 下看不到该 DLL，须使用 Sysnative。
        /// </summary>
        private static string GetNativeSystem32Directory()
        {
            if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Sysnative");
            }

            return Environment.GetFolderPath(Environment.SpecialFolder.System);
        }

        /// <summary>
        /// 部署系统目录下的 WindowsAccessBridge。安装程序无法预知 AT/RPA 客户端是 32 还是 64 位，
        /// 因此在 64 位 Windows 上同时写入 System32 的 -64 与 SysWOW64 的 -32；纯 32 位系统仅写入 -32。
        /// </summary>
        private static bool CopyWindowsAccessBridge(JavaInfo javaInfo, string current)
        {
            try
            {
                string versionDir = Path.Combine(current, "WindowsAccessBridge", javaInfo.GetVersion());

                if (Environment.Is64BitOperatingSystem)
                {
                    string src64 = Path.Combine(versionDir, "WindowsAccessBridge-64.dll");
                    string src32 = Path.Combine(versionDir, "WindowsAccessBridge-32.dll");
                    if (!File.Exists(src64) || !File.Exists(src32))
                    {
                        LogError(new IOException(
                            "CopyWindowsAccessBridge 未复制任何文件：安装目录下缺少源 DLL。"
                            + $" GetVersion()={javaInfo.GetVersion()}, 目录={versionDir}, "
                            + $"WindowsAccessBridge-64.dll 存在={File.Exists(src64)}, "
                            + $"WindowsAccessBridge-32.dll 存在={File.Exists(src32)}。"
                            + " 64 位 Windows 要求上述两个文件在同一子目录内。"));
                        return false;
                    }

                    string dest64 = Path.Combine(GetNativeSystem32Directory(), "WindowsAccessBridge-64.dll");
                    string dest32 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86), "WindowsAccessBridge-32.dll");
                    CopyWindowsAccessBridgeOne(src64, dest64);
                    CopyWindowsAccessBridgeOne(src32, dest32);
                    return true;
                }

                string srcOnly32 = Path.Combine(versionDir, "WindowsAccessBridge-32.dll");
                if (!File.Exists(srcOnly32))
                {
                    LogError(new IOException(
                        "CopyWindowsAccessBridge 未复制：缺少源文件 WindowsAccessBridge-32.dll。"
                        + $" GetVersion()={javaInfo.GetVersion()}, 目录={versionDir}"));
                    return false;
                }

                string destOnly32 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsAccessBridge-32.dll");
                CopyWindowsAccessBridgeOne(srcOnly32, destOnly32);
                return true;
            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
            }
        }

        private static void CopyWindowsAccessBridgeOne(string sourceFileName, string destFileName)
        {
            string fileName = Path.GetFileName(destFileName);
            string backupdestFileName = Path.Combine(Path.GetDirectoryName(destFileName), $"{fileName}-{DateTime.Now.ToFileTime()}.iibak");
            if (File.Exists(destFileName) && !File.Exists(backupdestFileName))
            {
                File.Move(destFileName, backupdestFileName);
            }

            File.Copy(sourceFileName, destFileName, overwrite: true);
        }

        private static bool CopyProperties(JavaInfo javaInfo, string current)
        {
            try
            {
                if (javaInfo.HighVersion)
                {
                    return true;
                }
                string targetFile = Path.Combine(javaInfo.GetLibPath(), javaInfo.GetLibFile());
                string originFile = Path.Combine(current, javaInfo.GetLibFile());
                DoCopyFile(originFile, targetFile, false);
                return true;
            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
            }
        }

        /// <summary>
        /// 判断注册表项是否存在
        /// </summary>
        /// <returns>bool</returns>
        private static bool IsExistRegeditKey(string key, string value, string path)
        {
            RegistryKey keyNode = Registry.CurrentUser;
            RegistryKey software = keyNode.OpenSubKey(path);
            if (software != null)
            {
                foreach (var item in software.GetValueNames())
                {
                    if (item.Equals(key))
                    {
                        if (software.GetValue(item).Equals(value))
                        {
                            keyNode.Close();
                            return true;
                        }
                    }
                }
                keyNode.Close();
                return false;
            }
            keyNode.Close();
            return false;
        }
        private static bool IsExistRegeditKey()
        {
            return IsExistRegeditKey("Configuration", "oracle_javaaccessbridge", @"Software\Microsoft\Windows NT\CurrentVersion\Accessibility");
        }

        //检测插件是否安装
        private static void IsInstallJavaExtension()
        {
            Message msg = new Message();
            //if (IsExistRegeditKey())
            if (MatchWindowsAccessBridge())
            {
                msg.status = (int)ReturnStatus.Success;
                msg.code = (int)JavaExtensionStatue.JavaExtensionInstallSuccess;
            }
            else
            {
                msg.status = (int)ReturnStatus.Fail;
                msg.code = (int)JavaExtensionStatue.JavaExtensionUnInstall;
            }
            Loggin(msg);
        }
        private static bool Jre64;
        private static List<string> listexePath = new List<string>();

        private static bool JreDetail(string path = null)
        {
            try
            {
                var p = new Process();
                if (string.IsNullOrEmpty(path))
                    p.StartInfo.FileName = "java";
                else
                    p.StartInfo.FileName = path;
                p.StartInfo.Arguments = "-version";
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                var result = p.StandardError.ReadToEnd();
                version = result.Split(' ')[2].Replace("\"", "").Substring(0, 3);
                Jre64 = result.Contains("64-Bit");
                p.Close();
                return !string.IsNullOrEmpty(version);
            }
            catch (Exception e)
            {
                LogError(e);
                return false;
            }
        }

        #endregion

        #region [文件写入]
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceFileName"></param>
        /// <param name="destFileName"></param>
        /// <param name="keepOriginVersion">是否保留原版本，默认保留</param>
        private static void DoCopyFile(string sourceFileName, string destFileName, bool keepOriginVersion = true)
        {
            try
            {
                if (!File.Exists(sourceFileName))
                    return;
                if (File.Exists(destFileName) && keepOriginVersion)//如果要拷贝的文件已经存在 
                {
                    return;
                }
                File.Copy(sourceFileName, destFileName, true);
            }
            catch (Exception e)
            {
                LogError(e);
            }
        }
        #endregion

        //删除java更新进行
        private static void DelJavaUpdate()
        {
            try
            {
                ////Console.WriteLine("删除java更新程序!");
                Process[] processes = Process.GetProcesses();
                for (int j = 0; j < processes.Length; j++)
                {
                    Process p = processes[j];
                    if (p.ProcessName == "jusched")
                    {
                        ////Console.WriteLine("删除java更新程序成功!");
                        p.Kill();
                        Thread.Sleep(400);
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                LogError(e);
                //Console.WriteLine("Error Java安装失败");
            }
        }

        private static void Exectue(string javaDir)
        {
            var filePath = Path.Combine(javaDir, "jabswitch.exe");
            if (File.Exists(filePath))
            {
                try
                {
                    using (Process p = new Process())
                    {
                        p.StartInfo.FileName = javaDir + "\\jabswitch.exe";
                        p.StartInfo.Arguments = "-enable";
                        p.StartInfo.UseShellExecute = false;
                        p.StartInfo.RedirectStandardInput = true;
                        p.StartInfo.RedirectStandardOutput = true;
                        p.StartInfo.RedirectStandardError = true;
                        p.StartInfo.CreateNoWindow = true;
                        p.Start();
                        p.WaitForExit();
                    }
                }
                catch (Exception e)
                {
                    LogError(e);
                }
            }
            else
            {
                LogError(new FileNotFoundException(filePath));
            }
        }

        private static string version = string.Empty;
        private static string windowsHome = string.Empty;
        static void Main(string[] args)
        {
            try
            {
                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
                KillPrevHost();
                windowsHome = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                //if (args.Length <= 0)
                //    return;
                var methodName = args[0].Split('=')[1];
                //var methodName = "InstallJavaExtension";
                switch (methodName)
                {
                    case "IsInstallJavaExtension":
                        IsInstallJavaExtension();
                        break;
                    case "InstallJavaExtension":

                        //获得当前登录的Windows用户标示
                        System.Security.Principal.WindowsIdentity identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                        System.Security.Principal.WindowsPrincipal principal = new System.Security.Principal.WindowsPrincipal(identity);
                        //判断当前登录用户是否为管理员
                        if (principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator))
                        {
                            TryGetSpecificJvm(args, out string jvmPath);
                            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "timeout.log")))
                            {
                                DateTime dt1 = DateTime.Parse(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "timeout.log")));
                                DateTime dt2 = DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"));
                                TimeSpan ts = dt2.Subtract(dt1);
                                if (ts.TotalMilliseconds <= 8000)
                                {
                                    InstallJavaExtension(jvmPath);
                                }
                                File.Delete(Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "timeout.log"));
                                return;
                            }
                            else
                            {
                                InstallJavaExtension(jvmPath);
                            }
                        }
                        else
                        {
                            // runas 提权启动不会继承父进程的进程级环境变量，DOTNET_ROOT 会丢失，
                            // 导致提权后的本程序找不到 bundled 运行时。这里用 cmd 包装，
                            // 把当前进程的 DOTNET_ROOT/DOTNET_ROOT_X86/DOTNET_ROOT_X64 显式写入提权进程环境。
                            var selfArgs = string.Join(" ", args.Select(arg =>
                                arg.Contains(' ') ? $"\"{arg}\"" : arg
                            ));

                            //创建启动对象
                            ProcessStartInfo startInfo = new ProcessStartInfo
                            {
                                //设置运行文件
                                FileName = Environment.GetEnvironmentVariable("COMSPEC") ?? "cmd.exe",
                                //设置启动参数
                                Arguments = "/d /s /c \"" + BuildElevatedCommandLine(System.Windows.Forms.Application.ExecutablePath, selfArgs) + "\"",
                                UseShellExecute = true,
                                //设置启动动作,确保以管理员身份运行
                                Verb = "runas",
                                WindowStyle = ProcessWindowStyle.Hidden
                            };
                            try
                            {
                                Thread.Sleep(200);
                                Loggin();
                                //如果不是管理员，则启动UAC
                                System.Diagnostics.Process.Start(startInfo);
                                //退出
                                System.Windows.Forms.Application.Exit();
                            }
                            catch (Exception e)
                            {
                                Message msg = new Message
                                {
                                    status = (int)ReturnStatus.Cancel,
                                    code = (int)JavaExtensionStatue.Unknown
                                };
                                Loggin(msg);
                                LogError(e);
                            }
                        }
                        break;
                    default:
                        return;
                }
            }
            catch (Exception e)
            {
                LogError(e);
            }
        }

        /// <summary>
        /// 构建提权启动的命令行：先在提权进程里 set 当前进程的 DOTNET_ROOT 系列变量，再执行目标程序。
        /// </summary>
        private static string BuildElevatedCommandLine(string targetExe, string arguments)
        {
            var command = new StringBuilder();
            foreach (var name in new[] { "DOTNET_ROOT", "DOTNET_ROOT_X86", "DOTNET_ROOT_X64" })
            {
                var value = Environment.GetEnvironmentVariable(name);
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                if (command.Length > 0)
                {
                    command.Append(" && ");
                }

                command.Append("set \"").Append(name).Append('=').Append(value).Append('"');
            }

            if (command.Length > 0)
            {
                command.Append(" && ");
            }

            command.Append('"').Append(targetExe).Append('"');
            if (!string.IsNullOrWhiteSpace(arguments))
            {
                command.Append(' ').Append(arguments);
            }

            return command.ToString();
        }

        private static void Loggin()
        {
            var content = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
            string logfile = AppDomain.CurrentDomain.BaseDirectory + "timeout.log";
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(content);
            File.WriteAllText(logfile, stringBuilder.ToString());
        }
        private static void Loggin(object obj)
        {
            string content = JsonConvert.SerializeObject(obj);
            string logfile = AppDomain.CurrentDomain.BaseDirectory + "JavaExtension.txt";
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(content);
            File.WriteAllText(logfile, stringBuilder.ToString());
        }

        public static void LogError(Exception ex)
        {
            string logfile = Path.Combine(Path.GetTempPath(), "JavaPluginInstall.log");
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]");
            do
            {
                stringBuilder.AppendLine(ex.Message);
                stringBuilder.AppendLine(ex.StackTrace);
                ex = ex.InnerException;
            }
            while (ex != null);
            stringBuilder.AppendLine();
            File.AppendAllText(logfile, stringBuilder.ToString());
        }

        private static void SetJavaRegistry()
        {
            //写注册表
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\Accessibility", true);
            if (registryKey == null)
            {
                registryKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\Accessibility");
            }
            registryKey.SetValue("Configuration", "oracle_javaaccessbridge");
        }
        private static void FindFile(string dir, string pattern, ref List<string> matchedFiles, int maxDepth = 9)
        {
            if (dir.Split(new char[]
            {
                '\\'
            }).Length > maxDepth)
            {
                return;
            }
            DirectoryInfo directoryInfo = new DirectoryInfo(dir);
            try
            {
                DirectoryInfo[] directories = directoryInfo.GetDirectories();
                for (int i = 0; i < directories.Length; i++)
                {
                    DirectoryInfo directoryInfo2 = directories[i];
                    FindFile(dir + directoryInfo2.ToString() + "\\", pattern, ref matchedFiles, maxDepth);
                }
                FileInfo[] files = directoryInfo.GetFiles(pattern);
                for (int i = 0; i < files.Length; i++)
                {
                    FileInfo fileInfo = files[i];
                    matchedFiles.Add(dir + fileInfo.ToString());
                }
            }
            catch (Exception e)
            {
                LogError(e);
                //Console.WriteLine(e.Message);
            }
        }
        public static bool MatchWindowsAccessBridge()
        {
            if (FolderHasWindowsAccessBridge(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86)))
            {
                return true;
            }
            if (Environment.Is64BitOperatingSystem)
            {
                return FolderHasWindowsAccessBridge(GetNativeSystem32Directory());
            }
            return false;
        }

        private static bool FolderHasWindowsAccessBridge(string folder)
        {
            var files = GetSubFileSystemInfos(folder);
            foreach (var file in files)
            {
                if (!string.IsNullOrWhiteSpace(file.Key) && file.Key.ToLower().StartsWith("windowsaccessbridge"))
                {
                    return true;
                }
            }
            return false;
        }
        public static Dictionary<string, FileSystemInfo> GetSubFileSystemInfos(string aFolderName)
        {
            Dictionary<string, FileSystemInfo> subFileSystemInfos = new Dictionary<string, FileSystemInfo>();
            DirectoryInfo theFolder = new DirectoryInfo(aFolderName);
            var fileInfos = theFolder.GetFileSystemInfos();
            if (fileInfos != null && fileInfos.Any())
            {
                foreach (var sub in fileInfos)
                {
                    if (!subFileSystemInfos.ContainsKey(sub.Name))
                    {
                        subFileSystemInfos.Add(sub.Name, sub);
                    }
                }
            }
            return subFileSystemInfos;
        }

        private static bool TryGetSpecificJvm(string[] args, out string jvmPath)
        {
            if (args.Length >= 2)
            {
                var jvmPathArgs = args[1].Split('=');
                if (jvmPathArgs.Length >= 2 && jvmPathArgs[0].Trim().ToLower().EndsWith("jvm"))
                {
                    jvmPath = jvmPathArgs[1];
                    return true;
                }
            }
            jvmPath = null;
            return false;
        }
    }
}
