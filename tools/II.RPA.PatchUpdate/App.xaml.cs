using II.RPA.PatchUpdate.Helpers;
using II.RPA.PatchUpdate.UserControl;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace II.RPA.PatchUpdate
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        LogHelper log = LogHelper.GetLogHelper();
        bool updateSuccess = true;
        bool cancelUpdate = false;
        string appInstPath = "";
        PatchInfo patchInfo = null;
        bool isOnline = true;
        PatchStatus patchStatus = null;
        string filePath_Patch = string.Empty;

        private string HashString(string text, string salt = "")
        {
            if (String.IsNullOrEmpty(text))
            {
                return String.Empty;
            }

            // Uses SHA256 to create the hash
            using (var sha = new System.Security.Cryptography.SHA256Managed())
            {
                // Convert the string to a byte array first, to be processed
                byte[] textBytes = System.Text.Encoding.UTF8.GetBytes(text + salt);
                byte[] hashBytes = sha.ComputeHash(textBytes);

                // Convert back to a string, removing the '-' that BitConverter adds
                string hash = BitConverter
                    .ToString(hashBytes)
                    .Replace("-", String.Empty);

                return hash;
            }
        }

        private Mutex _instanceMutex = null;
        private bool WaitForMutex(int timeout)
        {
            var key = HashString(Assembly.GetExecutingAssembly().Location);
            var src = new CancellationTokenSource(timeout);
            while (true)
            {
                if (!src.Token.IsCancellationRequested)
                {
                    _instanceMutex = new Mutex(true, key, out var createdNew);
                    if (!createdNew)
                    {
                        _instanceMutex.Dispose();
                        _instanceMutex = null;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    return false;
                }
                Thread.Sleep(300);
            }
        }

        public App()
        {
            Current.Exit += (_, __) => { _instanceMutex?.Dispose(); };
            var mutexGot = WaitForMutex(3000);
            if (!mutexGot)
            {
                // Timeout
                Current.Shutdown(2);
                return;
            }
            //wait cursor
            Mouse.OverrideCursor = Cursors.Wait;
            Thread.Sleep(3000);
        }

        public static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static void StartAsAdmin(string fileName, string arguments)
        {
            var proc = new Process
            {
                StartInfo =
                {
                    FileName = fileName,
                    UseShellExecute = true,
                    Verb = "runas",
                    Arguments = arguments
                }
            };
            proc.Start();
        }

        private void OnAppStartup(object sender, StartupEventArgs e)
        {
            if (!IsAdministrator() || !e.Args.Contains("-actual"))
            {
                try
                {
                    StartAsAdmin(Assembly.GetExecutingAssembly().Location, string.Join(" ", new List<string>
                    {
                        string.Join(" ", e.Args),
                        "-actual"
                    }));
                }
                catch
                {
                    Current.Shutdown(1);
                    return;
                }
                Current.Shutdown(0);
                return;
            }

            log.Info("------------------------");
            log.Info("启动更新程序......");

            var rawArgs = new List<string>(e.Args).Where(arg => arg != "-actual" && !string.IsNullOrWhiteSpace(arg)).ToArray();

            try
            {
                string runningProcessNames = ConfigurationManager.AppSettings["CheckRunningProcessNames"];
                string[] arrRunningProcessNames = runningProcessNames.Split(';');
                string title = ConfigurationManager.AppSettings["Title"];

                string needKillProcessNames = "";
                string displayNeedKillProcessNames = "";
                foreach (string name in arrRunningProcessNames)
                {
                    if (ApplicationHelper.FindExistByProcessName(name))
                    {
                        needKillProcessNames += $"【{name}】，";

                        if (name == "实在设计器" || name == "实在机器人")
                        {
                            displayNeedKillProcessNames += $"【{name}】，";
                        }
                    }
                }

                bool executeUpdate = false;
                if (needKillProcessNames != "")
                {
                    log.Info($"需要关闭应用程序{needKillProcessNames.Substring(0, needKillProcessNames.Length - 1)}");
                    if (displayNeedKillProcessNames.Length == 0)
                        displayNeedKillProcessNames = "关联程序，";

                    Mouse.OverrideCursor = Cursors.Arrow;
                    //if (MessageBox.Show(PatchUpdate.Properties.Resources.CloseRunningProcessConfirmationMessage.Replace("$0", displayNeedKillProcessNames.Substring(0, displayNeedKillProcessNames.Length - 1)), PatchUpdate.Properties.Resources.PromptInfoCaption, MessageBoxButton.OKCancel, MessageBoxImage.Information) == MessageBoxResult.OK)
                    Window tempWindow = new Window();
                    tempWindow.Height = 0;
                    tempWindow.Width = 0;
                    tempWindow.AllowsTransparency = true;
                    tempWindow.WindowStyle = WindowStyle.None;
                    tempWindow.ShowInTaskbar = false;
                    tempWindow.Left = -1000;
                    tempWindow.Top = -1000;
                    tempWindow.Show();
                    if (XMessageBox.Question(PatchUpdate.Properties.Resources.CloseRunningProcessConfirmationMessage.Replace("$0", displayNeedKillProcessNames.Substring(0, displayNeedKillProcessNames.Length - 1)), tempWindow))
                    {
                        Mouse.OverrideCursor = Cursors.Wait;
                        foreach (string processName in arrRunningProcessNames)
                        {
                            //Task.Run( () =>
                            //{
                            if (ApplicationHelper.KillExistByProcessName(processName))
                            {
                                executeUpdate = true;
                                log.Info($"关闭应用程序【{processName}】成功");
                            }
                            else
                            {
                                //MessageBox.Show(PatchUpdate.Properties.Resources.FailedToCloseTargetProcessMessage.Replace("$0", "【" + processName + "】"), PatchUpdate.Properties.Resources.WrongInfoCaption, MessageBoxButton.OK, MessageBoxImage.Error);
                                //return;
                                throw new Exception($"关闭应用程序【{processName}】失败");
                            }
                            //});
                        }

                    }
                    else
                    {
                        log.Info($"取消关闭应用程序{needKillProcessNames.Substring(0, needKillProcessNames.Length - 1)}");
                        cancelUpdate = true;
                    }
                }
                else
                {
                    log.Info("无需关闭应用程序");
                    executeUpdate = true;
                }

                if (executeUpdate)
                {
                    patchInfo = ApplicationHelper.GetPatchInfo(AppDomain.CurrentDomain.BaseDirectory + "\\patch.json");
                    if (patchInfo == null)
                    {
                        //MessageBox.Show(PatchUpdate.Properties.Resources.PatchInfoNotExistMessage, PatchUpdate.Properties.Resources.WrongInfoCaption, MessageBoxButton.OK, MessageBoxImage.Error);
                        //return;
                        throw new Exception(PatchUpdate.Properties.Resources.PatchInfoNotExistMessage);
                    }

                    if (rawArgs.Length == 0) //离线更新，约定补丁包需要和安装目录同级
                    {
                        isOnline = false;
                        appInstPath = ConfigurationManager.AppSettings["AppInstPath"];
                        if (appInstPath == null || appInstPath == "")
                        {
                            appInstPath = ApplicationHelper.GetInstPath(patchInfo);
                        }
                    }
                    else //在线更新，Factory或Bot调用传入，绝对路径
                    {
                        appInstPath = rawArgs[1].ToString();
                    }
                    log.Info($"【{patchInfo.type}】安装目录路径{appInstPath}");
                    if (!Directory.Exists(appInstPath))
                    {
                        //MessageBox.Show(PatchUpdate.Properties.Resources.InstallPathNotExistMessage, PatchUpdate.Properties.Resources.WrongInfoCaption, MessageBoxButton.OK, MessageBoxImage.Error);
                        //return;
                        throw new Exception(PatchUpdate.Properties.Resources.InstallPathNotExistMessage);
                    }

                    if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + patchInfo.name + ".zip"))
                    {
                        //MessageBox.Show(PatchUpdate.Properties.Resources.PatchNotExistMessage, PatchUpdate.Properties.Resources.WrongInfoCaption, MessageBoxButton.OK, MessageBoxImage.Error);
                        //return;
                        throw new Exception(PatchUpdate.Properties.Resources.PatchNotExistMessage);
                    }

                    #region 版本行业等信息检查
                    //ConfigInfo configInfo = ApplicationHelper.GetConfigInfo(appInstPath + "\\" + (patchInfo.type == "实在设计器" ? "Z-Factory" : "Z-Bot") + "\\config.json");
                    //ConfigInfo configInfo = ApplicationHelper.GetConfigInfo(appInstPath + "\\" + (patchInfo.type == "Z-Factory" ? "Z-Factory" : "Z-Bot") + "\\config.json");
                    //有后缀(社区版/企业版)
                    ConfigInfo configInfo = ApplicationHelper.GetConfigInfo(appInstPath + "\\" + (patchInfo.type.Contains("Z-Factory") ? "Z-Factory" : "Z-Bot") + "\\config.json");
                    log.Info(configInfo.custom + "," + patchInfo.custom + "/" + configInfo.industryType + "," + patchInfo.industryType);
                    if (configInfo == null)
                    {
                        throw new Exception(PatchUpdate.Properties.Resources.ConfigInfoNotExistMessage);
                    }
                    else if (configInfo.custom != patchInfo.custom || configInfo.industryType != patchInfo.industryType)
                    {
                        throw new Exception(PatchUpdate.Properties.Resources.PatchInfoNotMatchMessage);
                    }

                    //string appPatch = appInstPath + "\\" + (patchInfo.type == "实在设计器" ? "Z-Factory\\" : "Z-Bot\\") + patchInfo.type + ".exe";
                    string appPatch = appInstPath + "\\" + (patchInfo.type.Contains("Z-Factory") ? "Z-Factory\\" : "Z-Bot\\") + (patchInfo.type.Contains("Z-Factory") ? "实在设计器" : "实在机器人") + ".exe";
                    string appVersion = ApplicationHelper.GetVersion(appPatch);
                    log.Info(patchInfo.version + "/" + appVersion.Substring(0, appVersion.LastIndexOf('.')));
                    if (patchInfo.version != appVersion.Substring(0, appVersion.LastIndexOf('.')))
                    {
                        throw new Exception(PatchUpdate.Properties.Resources.PatchInfoNotMatchMessage);
                    }
                    #endregion

                    string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    //string filePath_Patch = appDataPath + "\\" + (patchInfo.type == "实在设计器" ? "Z-Factory-Patch" : "Z-Bot-Patch");
                    //此处需要和桃确认patch文件位置有无问题
                    filePath_Patch = appDataPath + "\\" + (patchInfo.type.Contains("Z-Factory") ? "Z-Factory-Patch" : "Z-Bot-Patch");
                    filePath_Patch = $"{filePath_Patch}\\{patchInfo.type}-{patchInfo.version}-{patchInfo.industryType}-{patchInfo.custom}";
                    if (!Directory.Exists(filePath_Patch))
                    {
                        throw new Exception(PatchUpdate.Properties.Resources.PatchStatusNotExistMessage);
                        //if (isOnline)
                        //{
                        //    throw new Exception(PatchUpdate.Properties.Resources.PatchStatusNotExistMessage);
                        //}
                        //else
                        //{
                        //    Directory.CreateDirectory(filePath_Patch);
                        //}
                    }

                    patchStatus = ApplicationHelper.GetPatchStatus(filePath_Patch + "\\patch.json");
                    if (patchStatus == null)
                    {
                        throw new Exception(PatchUpdate.Properties.Resources.PatchStatusNotExistMessage);
                        //if (isOnline)
                        //{
                        //    throw new Exception(PatchUpdate.Properties.Resources.PatchStatusNotExistMessage);
                        //}
                        //else
                        //{
                        //    patchStatus = new PatchStatus();
                        //    patchStatus.version = patchInfo.patchVersion;
                        //    patchStatus.hasPatchUpdate = true;
                        //    patchStatus.patchUpdated = false;
                        //}
                    }
                    else if (patchStatus.version != patchInfo.patchVersion)
                    {
                        if (isOnline)
                        {
                            throw new Exception(PatchUpdate.Properties.Resources.PatchInfoNotMatchMessage);
                        }
                        else
                        {
                            if (string.Compare(patchStatus.version, patchInfo.patchVersion, true) > 0)
                            {
                                throw new Exception(PatchUpdate.Properties.Resources.PatchVersionNotMatchMessage.Replace("$0", "【" + patchInfo.type + "】"));
                            }
                        }
                    }
                    else if (!patchStatus.hasPatchUpdate)
                    {
                        if (isOnline)
                        {
                            throw new Exception(PatchUpdate.Properties.Resources.NoPatchToUpdate);
                        }
                    }
                    else if (patchStatus.patchUpdated)
                    {
                        if (isOnline)
                        {
                            throw new Exception(PatchUpdate.Properties.Resources.PatchHasUpdated);
                        }
                    }

                    log.Info("替换Patch包开始");
                    var MainWindow = new MainWindow(title, appInstPath, patchInfo, configInfo.engineDirName, log);
                    //MainWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                    //MainWindow.Left = -1000;
                    //MainWindow.Top = -1000;
                    if (MainWindow.ShowDialog() == true)
                    {
                        updateSuccess = true;
                        log.Info("替换Patch包正常结束");
                    }
                    else
                    {
                        updateSuccess = false;
                        log.Info("替换Patch包异常结束");
                    }

                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString());
                log.Error(ex.Message);
                updateSuccess = false;
            }
            finally
            {
                if (!cancelUpdate)
                {
                    //记录更新结果
                    if (updateSuccess)
                    {
                        log.Info("更新成功......");
                        log.Info("更新状态文件patch.json");
                        patchStatus.version = patchInfo.patchVersion;
                        patchStatus.hasPatchUpdate = true;
                        patchStatus.patchUpdated = true;
                        patchStatus.displayPatchUpdatedInfo = false;
                        patchStatus.count = 0;
                        ApplicationHelper.UpdatePatchStatus(filePath_Patch + "\\patch.json", patchStatus);
                    }
                    else
                    {
                        log.Info("更新失败......");
                        //TO DO 回滚
                        log.Info("更新状态文件patch.json");
                        patchStatus.version = patchInfo.patchVersion;
                        patchStatus.hasPatchUpdate = true;
                        patchStatus.patchUpdated = false;
                        patchStatus.displayPatchUpdatedInfo = false;
                        patchStatus.count = patchStatus.count + 1;
                        ApplicationHelper.UpdatePatchStatus(filePath_Patch + "\\patch.json", patchStatus);
                    }
                }
                //唤起Factory或Bot
                Task[] tasks = new Task[1];
                try
                {
                    if (appInstPath == "")
                    {
                        if (rawArgs.Length == 0) //离线更新，约定补丁包需要和安装目录同级
                        {
                            isOnline = false;
                            appInstPath = ConfigurationManager.AppSettings["AppInstPath"];
                            if (appInstPath == null || appInstPath == "")
                            {
                                appInstPath = ApplicationHelper.GetInstPath(patchInfo);
                            }
                            log.Info($"离线路径【{appInstPath}】");
                        }
                        else //在线更新，Factory或Bot调用传入，绝对路径
                        {
                            appInstPath = rawArgs[1].ToString();
                        }
                    }
                    if (patchInfo == null)
                    {
                        patchInfo = ApplicationHelper.GetPatchInfo(AppDomain.CurrentDomain.BaseDirectory + "\\patch.json");
                        if (patchInfo == null)
                        {
                            throw new Exception(PatchUpdate.Properties.Resources.PatchInfoNotExistMessage);
                        }
                    }
                    if (cancelUpdate)
                    {
                        tasks[0] = Task.Run(() =>
                        {
                            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                            //string filePath_Patch = appDataPath + "\\" + (patchInfo.type == "实在设计器" ? "Z-Factory-Patch" : "Z-Bot-Patch");
                            string filePath_Patch = appDataPath + "\\" + (patchInfo.type.Contains("Z-Factory") ? "Z-Factory-Patch" : "Z-Bot-Patch");
                            filePath_Patch = $"{filePath_Patch}\\{patchInfo.type}-{patchInfo.version}-{patchInfo.industryType}-{patchInfo.custom}";
                            PatchStatus patchStatus = ApplicationHelper.GetPatchStatus(filePath_Patch + "\\patch.json");
                            UpdateTempPatch(patchStatus, filePath_Patch);
                        });
                    }
                    log.Info("启动路径:" + appInstPath + "\\" + (patchInfo.type.Contains("Z-Factory") ? "Z-Factory\\" : "Z-Bot\\") + (patchInfo.type.Contains("Z-Factory") ? "实在设计器" : "实在机器人") + ".exe");
                    //ApplicationHelper.StartApplication("explorer.exe", appInstPath + "\\" + (patchInfo.type == "实在设计器" ? "Z-Factory\\" : "Z-Bot\\") + patchInfo.type + ".exe");
                    ApplicationHelper.StartApplication("explorer.exe", appInstPath + "\\" + (patchInfo.type.Contains("Z-Factory") ? "Z-Factory\\" : "Z-Bot\\") + (patchInfo.type.Contains("Z-Factory") ? "实在设计器" : "实在机器人") + ".exe");
                    //ApplicationHelper.StartApplication(appInstPath + "\\" + (patchInfo.type == "实在设计器" ? "Z-Factory\\" : "Z-Bot\\") + patchInfo.type + ".exe", "gogogo");
                    log.Info($"唤起【{patchInfo.type}】成功");
                }
                catch (Exception ex)
                {
                    log.Info($"唤起【{patchInfo.type}】失败：{ex.Message}");
                }
                if (cancelUpdate)
                {
                    Task.WaitAll(tasks);
                }
                //更新完成后删除patch包和备份文件
                if (updateSuccess && !cancelUpdate)
                {
                    DelayDeleteFile(appInstPath + "\\" + $"backup-{patchInfo.patchVersion}" + "\\");
                    DelayDeleteFile(System.AppDomain.CurrentDomain.BaseDirectory);
                }
                Application.Current.Shutdown();
            }
        }
        #region 点击取消杀进程先修改配置文件的的属性以保证当前这次唤起 设计器/机器人顺利成功,然后再还原属性值
        public void UpdateTempPatch(PatchStatus patchStatus, string filePath_Patch)
        {
            log.Info($"patch.json路径:" + filePath_Patch);
            if (patchStatus != null)
            {
                int originCount = patchStatus.count;
                patchStatus.count = 4;
                ApplicationHelper.UpdatePatchStatus(filePath_Patch + "\\patch.json", patchStatus);
                Thread.Sleep(5000);
                patchStatus.count = 0;
                ApplicationHelper.UpdatePatchStatus(filePath_Patch + "\\patch.json", patchStatus);
            }
        }
        #endregion


        private void DelayDeleteFile(string fileName, int delaySecond = 5)
        {
            try
            {
                if (fileName.Contains("backup-"))
                {
                    var arguments = $"/c timeout /t {delaySecond} && rmdir  /s/q {fileName} ";
                    log.Info($"执行cmd命令字符:{arguments}");
                    var workingTempDir = fileName.TrimEnd(@"\".ToCharArray());
                    var workingDir = workingTempDir.Substring(0, workingTempDir.LastIndexOf(@"\") + 1);
                    log.Info($"处理路径:{workingDir}");
                    var processStartInfo = new ProcessStartInfo()
                    {
                        Verb = "runas", // 如果程序是管理员权限，那么运行 cmd 也是管理员权限
                        FileName = "cmd",
                        UseShellExecute = false,
                        CreateNoWindow = true, // 如果需要隐藏窗口，设置为 true 就不显示窗口
                        Arguments = arguments,
                        WorkingDirectory = workingDir
                    };
                    Process.Start(processStartInfo);
                }
                else
                {

                    var workingTempDir = fileName.TrimEnd(@"\".ToCharArray());
                    var workingDir = workingTempDir.Substring(0, workingTempDir.LastIndexOf(@"\") + 1);
                    var copyDir = workingTempDir.Substring(0, workingTempDir.LastIndexOf(@"\"));
                    //1、先等待5s 2、拷贝更新文本文件出来 3、进行patch包文件删除 4、再将第2步拷贝的文件再次粘贴到patch包文件
                    var arguments = $"/c timeout /t {delaySecond} && copy /y {workingTempDir}\\updateItems.txt {workingDir} && del /f /q {fileName} && copy /y {copyDir}\\updateItems.txt {fileName} && del {copyDir}\\updateItems.txt";
                    log.Info($"执行cmd命令字符:{arguments}");

                    log.Info($"处理路径:{workingDir}");
                    var processStartInfo = new ProcessStartInfo()
                    {
                        Verb = "runas", // 如果程序是管理员权限，那么运行 cmd 也是管理员权限
                        FileName = "cmd",
                        UseShellExecute = false,
                        CreateNoWindow = true, // 如果需要隐藏窗口，设置为 true 就不显示窗口
                        Arguments = arguments,
                        WorkingDirectory = workingDir
                    };
                    Process.Start(processStartInfo);
                }
            }
            catch (Exception ex)
            {
                if (fileName.Contains("backup-"))
                    log.Error("删除备份文件失败:" + ex.ToString());
                else
                    log.Error("删除patch文件失败:" + ex.ToString());
            }
        }
    }
}
