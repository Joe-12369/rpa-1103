using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.Plugin.GUI.Commons.Win32Tools;
using II.RPA.Plugin.GUI.Commons.WindowMatch;
using II.RPA.WebBrowser.Common.Basic.Params;
using II.RPA.WebBrowser.Common.Utils;
using II.RPA.WinAPI;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.WebBrowser.Common.Basic.DownloaderBasic
{
    public abstract class ElementDownloader
    {
        private List<eGuiType> notSuppertDialog = new List<eGuiType>() { eGuiType.Web360X, eGuiType.Sogou };
        protected ILogger logger = Logger.OfContext();
        public abstract eGuiType GuiType { get; }
        public abstract Browser Browser { get; }
        public BrowserTabPage TabPage { get; }

        protected ElementDownloader(BrowserTabPage tabPage)
        {
            this.TabPage = tabPage;
        }

        public bool VerifyBrowserDownloadMode()
        {
            if (notSuppertDialog.Contains(GuiType))
            {
                if (IsTurnOnAskSaveFile())
                    throw new RPAException(GuiType, eErrorMessageType.Error_NotSupportDownloadMode_Error);
                else
                    return false;
            }
            return IsTurnOnAskSaveFile();
        }
        public virtual void AddCreatedEvent(ParamDownloaderModel param = null)
        {
            this.TabPage.AddCreatedEvent(new Params.ParamModel());
        }
        public virtual void RemoveCreatedEvent()
        {
            this.TabPage.RemoveCreatedEvent(new Params.ParamModel());
        }
        public virtual bool IsCurrentBrowser(Process process)
        {
            //var browserAppPath = this.PageElement.TabPage.Browser.GetBrowserAppPath();
            /* 
             * 这是不使用低开销的Browser.GetBrowserAppPath()的原因：GetBrowserAppPath 是按照固定规则 或 配置获取浏览器程序地址
             * 如果后续做通配、多实例、或 固定规则发生变化、多套规则，则可能会带来新问题
             * BrowserWindowInfo.AppExePath 依赖且要求窗口定位准确,极低概率获取不到 默认true
            */
            var browserAppPath = this.TabPage.BrowserWindowInfo?.AppExePath ?? string.Empty;
            if (string.IsNullOrEmpty(browserAppPath)) return true;
            var exePath = process.GetExeFilePath();
            return browserAppPath.EqualIgnoreCase(exePath);
        }
        public string GetCommandLineOfProcess()
        {
            var commandKey = "--user-data-dir";
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            Process[] processes = UiCommon.GetProcessesBySessionId(this.Browser.BrowserProcessName);
            foreach (var process in processes)
            {
                try
                {
                    if (!IsCurrentBrowser(process))
                    {
                        continue;
                    }
                    var commandAll = II.RPA.WinAPI.Ntdll.GetCommandLineOfProcess(process.Id);
                    if (string.IsNullOrEmpty(commandAll))
                        continue;
                    if (commandAll.Contains(commandKey))
                    {
                        var input = commandAll;
                        string pattern = @"""--user-data-dir=([^""]+)""";
                        Match match = Regex.Match(input, pattern);
                        if (match.Success)
                        {
                            var userDataDir = match.Groups[1].Value.Trim('"');
                            if (userDataDir != null)
                            {
                                logger.LogDebug($"user data:{userDataDir}");
                                stopWatch.Stop();
                                logger.LogDebug($"GetCommandLineOfProcess :{stopWatch.ElapsedMilliseconds}");
                                return userDataDir;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex);
                }
            }
            processes.Dispose();
            stopWatch.Stop();
            logger.LogDebug($"GetCommandLineOfProcess fail:{stopWatch.ElapsedMilliseconds}");
            return string.Empty;
        }
        public abstract string GetBrowserPreferencesPath();

        /// <summary>
        /// 根据进程命令行获取userdata数据，不是每个浏览器都能获取，如果读不到，再使用规定好的userdata路径
        /// firefox IE 不走此实现
        /// </summary>
        /// <returns></returns>
        public string ValidPreferencesPath()
        {
            var preferences = @"Default\Preferences";
            var commandLine = GetCommandLineOfProcess();
            var preferencesPath = Path.Combine(commandLine, preferences);
            if (!File.Exists(preferencesPath))
            {
                preferencesPath = GetBrowserPreferencesPath();
            }
            return preferencesPath;
        }

        public virtual bool IsTurnOnAskSaveFile()
        {
            try
            {
                var _chromeSettingPath = ValidPreferencesPath();
                string value = string.Empty;
                string content = string.Empty;
                if (!File.Exists(_chromeSettingPath))
                {
                    return false;
                }
                using (FileStream fs = new FileStream(_chromeSettingPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (StreamReader streamReader = new StreamReader(fs))
                {
                    content = streamReader.ReadToEnd();
                    JObject jobject = JObject.Parse(content);
                    JObject download = JObject.Parse(jobject["download"].ToString());
                    if (download.TryGetValue("prompt_for_download", out JToken result))
                        return result.As<bool>();
                    else
                        throw new RPAException(GuiType, eErrorMessageType.Error_UnavailableDownloadWindow);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                return false;
            }
        }
        public virtual string DownloadDefaultDirectory()
        {
            try
            {
                var _chromeSettingPath = ValidPreferencesPath();
                string value = string.Empty;
                string content = string.Empty;
                using (FileStream fs = new FileStream(_chromeSettingPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (StreamReader streamReader = new StreamReader(fs))
                {
                    content = streamReader.ReadToEnd();
                    JObject jobject = JObject.Parse(content);
                    JObject download = JObject.Parse(jobject["download"]?.ToString());
                    if (download != null && download.TryGetValue("default_directory", out JToken result))
                        return result.ToString();

                    //发现文件保存路径 可能在 savefile目录 ，将savefile作为备选
                    JObject saveFile = JObject.Parse(jobject["savefile"]?.ToString());
                    if (saveFile != null && saveFile.TryGetValue("default_directory", out result))
                        return result.ToString();

                    throw new RPAException(GuiType, eErrorMessageType.Error_UnavailableDownloadDefaultDirectory);
                }
            }
            catch (Exception ex)
            {
                throw new RPAException(GuiType, eErrorMessageType.Error_UnavailableDownloadDefaultDirectory, ex);
            }
        }
        private IntPtr FindDialogHandle()
        {
            var browserHandle = this.TabPage.BrowserWindowInfo.Handle;
            //激活目标浏览器窗口
            this.TabPage.BrowserWindowInfo.Activate();
            DesktopWindow desktopWindow = new DesktopWindow();
            var saveDialogWnd = desktopWindow.FindAllSavaDialogWindow(wnd => wnd.IsVisibleEnabled && NativeMethods.MatchAncestor(wnd.Handle, browserHandle))
                 .FirstOrDefault();
            return saveDialogWnd == null ? IntPtr.Zero : saveDialogWnd.Handle;
        }
        /// <summary>
        /// 默认30秒
        /// </summary>
        /// <param name="timeout"></param>
        public virtual IntPtr WaittingSaveDialog(int timeout = 30000)
        {
            return this.WaittingDialog(isDowload: true, timeout);
        }
        public virtual IntPtr WaittingUploadSaveDialog(int timeout = 30000)
        {
            return this.WaittingDialog(isDowload: false, timeout);
        }
        public IntPtr WaittingDialog(bool isDowload, int timeout = 30000)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            while (stopWatch.ElapsedMilliseconds <= timeout)
            {
                var handle = FindDialogHandle();
                if (handle != IntPtr.Zero)
                {
                    stopWatch.Stop();
                    return handle;
                }
                Task.Delay(20).Wait();
            }
            throw new RPAException(GuiType, isDowload ? eErrorMessageType.Error_WaittingSaveDialog_Timeout_Error : eErrorMessageType.Error_WaittingUploadSaveDialog_Timeout_Error);
        }
        public virtual string AutomaticOperationDialog(IntPtr handle, string saveFile, string fileName, int interval, InputMode inputMode)
        {
            return DialogHelper.SaveDialogWindow(handle, saveFile, fileName, interval, inputMode);
        }
        public virtual void AutomaticOperationUploadDialog(IntPtr browserHandle, IntPtr handle, string filePath, int interval, InputMode inputMode, UploadType uploadType)
        {
            DialogHelper.SaveUploadDialogWindow(browserHandle, handle, filePath, interval, inputMode, uploadType);
        }
        private bool IsOccupied(string filePath)
        {
            IntPtr handler = Kernel32._lopen(filePath, Kernel32.OF_READWRITE | Kernel32.OF_SHARE_DENY_NONE);
            Kernel32.CloseHandle(handler);
            return handler == Kernel32.HFILE_ERROR;
        }
        public virtual string Save(SaveMode saveMode, string downloadPath, string path, int timeout)
        {

            if (FileHelper.Exists(path) && IsOccupied(path))
            {
                throw new RPAException(GuiType, eErrorMessageType.Error_FileOccupancy);
            }

            if (!RPAUtils.Retry(() => { return FileHelper.Exists(downloadPath) && !IsOccupied(downloadPath); }, timeout, 100))
            {
                throw new RPAException(GuiType, eErrorMessageType.Error_FileOccupancy);
            }
            try
            {
                switch (saveMode)
                {
                    case SaveMode.None:
                        break;
                    case SaveMode.Copy:
                        int index = 0;
                        var dirName = Path.GetDirectoryName(path);
                        var fileName = Path.GetFileNameWithoutExtension(path);
                        var exetension = Path.GetExtension(path);
                        if (FileHelper.Exists(path))
                        {
                            index++;
                            path = Path.Combine(dirName, $"{fileName} ({index}){exetension}");
                        }
                        while (true)
                        {
                            if (FileHelper.Exists(path))
                            {
                                index++;
                                path = Path.Combine(dirName, $"{fileName} ({index}){exetension}");
                            }
                            else
                            {
                                File.Move(downloadPath, path);
                                break;
                            }
                        }
                        break;
                    case SaveMode.Replace:
                        if (FileHelper.Exists(path))
                            File.Delete(path);
                        Thread.Sleep(200);
                        File.Move(downloadPath, path);
                        break;
                    case SaveMode.Skip:
                        if (FileHelper.Exists(path))
                            File.Delete(downloadPath);
                        else
                            File.Move(downloadPath, path);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                throw new RPAException(eErrorMessageType.Error_FileOperateFail, ex);
            }
            return path;
        }

        private string GetLatestCreatedFile(string directoryPath)
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(directoryPath);
                FileInfo[] files = dir.GetFiles(); // 获取当前目录下所有文件
                FileInfo latestFile = null;
                foreach (FileInfo file in files)
                {
                    if (latestFile == null || file.CreationTimeUtc > latestFile.CreationTimeUtc)
                    {
                        latestFile = file;
                    }
                }
                return latestFile?.FullName; // 返回最新创建文件的完整路径，如果没有文件则返回null
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
            return string.Empty;
        }

        public string PathAlternativeOffer()
        {
            var filePath = DownloadDefaultDirectory();
            if (!Directory.Exists(filePath))
            {
                throw new RPAException(eErrorMessageType.Error_UnavailableDownloadDefaultDirectory);
            }
            var defaultDirectory = GetLatestCreatedFile(filePath);
            logger.LogDebug($"compensate directory:{defaultDirectory}");
            if (string.IsNullOrEmpty(defaultDirectory))
                throw new RPAException(eErrorMessageType.Error_UnavailableDownloadDefaultDirectory);
            return defaultDirectory;
        }
        public virtual bool IsDownloadComplete(DownloadDataInfo dataInfo, string path)
        {
            var defaultDirectory = string.Empty;
            if (string.IsNullOrEmpty(dataInfo.Filename) && string.IsNullOrEmpty(path))
            {
                defaultDirectory = PathAlternativeOffer();
            }
            if (string.IsNullOrEmpty(path))
                path = string.IsNullOrEmpty(defaultDirectory) ? dataInfo.Filename : defaultDirectory;

            logger.LogDebug($"path：{path}");
            logger.LogDebug($"dataInfo.Filename：{dataInfo.Filename}");
            logger.LogDebug($"dataInfo.FileSize：{dataInfo.FileSize},dataInfo.BytesReceived：{dataInfo.BytesReceived}");
            bool fileExists = false;
            if (FileHelper.Exists(path))
            {
                DateTime lastModified = File.GetLastWriteTime(path);
                DateTime startTimeOfUTC = DateTime.Parse(dataInfo.StartTime);
                DateTime startTimeOfLocal = startTimeOfUTC.ToLocalTime();  // 转为本地时间
                logger.LogDebug($"文件开始下载时间：{startTimeOfLocal.ToString("yyyy-MM-dd HH:mm:ss:ffff")}");
                logger.LogDebug($"已存在文件最后编辑时间：{lastModified.ToString("yyyy-MM-dd HH:mm:ss:ffff")}");
                //确保本地存在的文件最后的编辑时间在下载开始时间之后方可定义为此次下载产生的文件
                if (startTimeOfLocal.CompareTo(lastModified) <= 0)
                {
                    fileExists = true;
                }
            }
            if (dataInfo.FileSize == -1)
            {
                return fileExists && FileHelper.FileSize(path) != 0;
            }
            if (dataInfo.BytesReceived == 0)
            {
                return fileExists && FileHelper.FileSize(path) == dataInfo.FileSize;
            }
            else
            {
                if (dataInfo.BytesReceived != dataInfo.FileSize)
                {
                    return fileExists && FileHelper.FileSize(path) == dataInfo.FileSize;
                }
                else
                {
                    return fileExists
                        || (!string.IsNullOrEmpty(dataInfo.Filename) && FileHelper.Exists(dataInfo.Filename));
                }
            }
        }
        public virtual bool WaittingDownloadComplete(ref DownloadDataInfo dataInfo, string path, int timeout = 10000)
        {
            #region [等待下载] 
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
                var model = new ParamDownloadModel();
                while (stopWatch.ElapsedMilliseconds <= timeout)
                {
                    model.id = dataInfo.Id;
                    var downloadDataInfos = this.TabPage.DownLoadSearchInfos(model);
                    if (downloadDataInfos != null || downloadDataInfos?.Count > 0)
                    {
                        foreach (var downloadDataInfo in downloadDataInfos)
                        {
                            if (downloadDataInfo == null)
                                break;
                            logger.LogDebug($"WaittingDownloadComplete by id:{downloadDataInfo.ToJson()}");

                            if (IsDownloadComplete(downloadDataInfo, path))
                            {
                                dataInfo = downloadDataInfo;
                                stopWatch.Stop();
                                return true;
                            }
                        }
                    }
                    Thread.Sleep(500);
                }
                stopWatch.Stop();
            }
            catch (RPAException rpaEx)
            {
                throw new RPAException(rpaEx.ErrorMessageType, rpaEx);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            #endregion
            throw new RPAException(this.GuiType, eErrorMessageType.Error_DownloadTimeOut);
        }

        /// <summary>
        /// https://stackoverflow.com/questions/1410127/c-sharp-test-if-user-has-write-access-to-a-folder
        /// </summary>
        /// <param name="DirectoryPath"></param>
        /// <param name="AccessRight"></param>
        /// <returns></returns>
        //public static bool DirectoryHasPermission(string DirectoryPath)
        //{
        //    if (string.IsNullOrEmpty(DirectoryPath)) return false;

        //    try
        //    {
        //        bool writeable = false;
        //        WindowsPrincipal principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
        //        DirectorySecurity security = Directory.GetAccessControl(DirectoryPath);
        //        AuthorizationRuleCollection authRules = security.GetAccessRules(true, true, typeof(SecurityIdentifier));

        //        foreach (FileSystemAccessRule accessRule in authRules)
        //        {

        //            if (principal.IsInRole(accessRule.IdentityReference as SecurityIdentifier))
        //            {
        //                if ((FileSystemRights.Write & accessRule.FileSystemRights) == FileSystemRights.Write)
        //                {
        //                    if (accessRule.AccessControlType == AccessControlType.Allow)
        //                    {
        //                        writeable = true;
        //                    }
        //                    else if (accessRule.AccessControlType == AccessControlType.Deny)
        //                    {
        //                        return false;
        //                    }

        //                }
        //            }
        //        }
        //        return writeable;
        //    }
        //    catch (UnauthorizedAccessException)
        //    {
        //        return false;
        //    }
        //}
        public virtual void VerifyFilePath(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    logger.LogDebug($"路径{path}不存在，进行创建");
                    Directory.CreateDirectory(path);
                }
                else
                {
                    if (!DirectoryHasPermission(path))
                    {
                        throw new RPAException(eErrorMessageType.Error_PathNoAccess_Error);
                    }
                }
            }
            catch (RPAException rpaEx)
            {
                throw rpaEx;
            }
            catch (Exception ex)
            {
                if (ex is UnauthorizedAccessException)
                    throw new RPAException(eErrorMessageType.Error_PathNoAccess_Error, ex);
                throw new RPAException(GuiType, eErrorMessageType.Error_CreateDirectory_Error, ex);
            }
        }

        public static bool DirectoryHasPermission(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath))
                return false;
            try
            {
                // 原判断文件夹是否有操作权限方式不够严谨（覆盖不了共享路径以及FileSystemRights.Write并不能完全判定是否就有权限）
                string testFile = Path.Combine(directoryPath, Guid.NewGuid().ToString() + ".tmp");
                using (FileStream fs = File.Create(testFile, 1, FileOptions.DeleteOnClose))
                {
                    fs.WriteByte(0xFF);
                }
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (IOException)
            {
                // 磁盘已满等其他IO错误不应视为权限问题
                return true;
            }
            catch
            {
                return false;
            }
        }

        public virtual void VerifyFileName(string name)
        {
            var pattern = @"\/:*?”<>|";
            var match = Regex.Match(name, pattern);
            if (!match.Success)
                throw new RPAException(GuiType, eErrorMessageType.Error_FileNameNotSupport_Error);

        }

        /// <summary>
        /// 等待下载任务开始
        /// 解决问题: 根据注册下载事件，无法获取到下载文件路径
        /// 需要等到下载任务实际开启，才能获取到下载文件路径
        /// </summary>
        /// <param name="downloadId"></param>
        /// <param name="timeout ">暂定两秒</param>
        /// <returns></returns>
        public DownloadDataInfo WaittingDownload(int timeout = 10000)
        {
            var model = new ParamDownloadModel();
            DownloadDataInfo downloadFirst = null;
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var dataInfo = this.TabPage.DownloadDataInfo()?.FirstOrDefault();
            logger.LogDebug($"Latest download:{dataInfo.ToJson()}");
            while (stopWatch.ElapsedMilliseconds <= timeout)
            {
                try
                {
                    if (dataInfo != null)
                    {
                        model.id = dataInfo.Id;
                        downloadFirst = this.TabPage.DownLoadSearchInfos(model).FirstOrDefault();
                        if (downloadFirst != null && (downloadFirst.State == "in_progress" || downloadFirst.State == "complete"))
                            break;
                    }
                    else
                    {
                        //补偿获取下载信息
                        dataInfo = this.TabPage.DownloadDataInfo()?.FirstOrDefault();
                    }
                    Thread.Sleep(500);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex);
                }
            }
            stopWatch.Stop();
            logger.LogDebug($"Data in download:{downloadFirst.ToJson()}");
            return downloadFirst;
        }
        public virtual string HandleSaveDialog(IntPtr handle, string saveFile, string fileName, int interval, InputMode inputMode)
        {
            return this.AutomaticOperationDialog(handle, saveFile, fileName, interval, inputMode);
        }
        public virtual string HandleSaveDialog(IntPtr handle, string saveFile, string fileName, int interval, InputMode inputMode, SaveMode saveMode)
        {
            //return this.AutomaticOperationDialog(handle, saveFile, fileName, interval, inputMode);
            return DialogHelper.SaveDialogWindow(handle, saveFile, fileName, interval, inputMode, NativeMethods.SAVE_BUTTON_ID, saveMode);
        }
        public virtual void HandleSaveUploadDialog(IntPtr browserHandle, IntPtr handle, string filePath, int interval, InputMode inputMode, UploadType uploadType)
        {
            this.AutomaticOperationUploadDialog(browserHandle, handle, filePath, interval, inputMode, uploadType);
        }

        public virtual string FormattedFilePath(string saveFile, string fileName, string defaultDirectory)
        {
            return FileHelper.FormattedFilePath(saveFile, fileName, defaultDirectory);
        }
        private bool RemainingMemory(string dirveName, out long size)
        {
            DriveInfo drive = new DriveInfo(dirveName);
            if (drive.IsReady)
            {
                long freeSpace = drive.TotalFreeSpace;
                size = freeSpace;
                return true;
            }
            else
            {
                size = long.MinValue;
                return false;
            }
        }
        protected void VerifyDiskSize(string path, long fileSize)
        {
            if (fileSize > 0)
            {
                if (!string.IsNullOrEmpty(path))
                {
                    if (!path.StartsWith(@"\\"))
                    {
                        var drive = Path.GetPathRoot(path);
                        if (RemainingMemory(drive, out long size))
                        {
                            if (fileSize >= size)
                                throw new RPAException(eErrorMessageType.Error_OutOfMemory);
                        }
                    }
                }
                else
                {
                    logger.LogDebug($"download filename{path}");
                }
            }
            else
            {
                logger.LogDebug($"download size{fileSize}");
            }
        }
        public string GetDefaultDirectory(DownloadDataInfo downloadData)
        {
            var defaultDirectory = string.Empty;
            if (string.IsNullOrEmpty(downloadData.Filename))
            {
                defaultDirectory = PathAlternativeOffer();
            }
            else
            {
                defaultDirectory = downloadData.Filename;
            }
            return defaultDirectory;
        }
        public virtual string Downloader(string saveFile, string fileName, int interval, InputMode inputMode, SaveMode saveMode, int timeout, int waitDialogTime)
        {
            string path = string.Empty;
            var startTime = Stopwatch.GetTimestamp();

            if (this.VerifyBrowserDownloadMode())
            {
                var handle = this.WaittingSaveDialog(waitDialogTime);// 超时时间不该计算在下载任务中 ，但是目前无专门参数控制 暂时计算进来
                path = this.HandleSaveDialog(handle, saveFile, fileName, interval, inputMode, saveMode);
                if (saveMode != SaveMode.Skip)
                {
                    //加上组件的超时时间-避免部分下载点击后浏览器需要等待一段时间后才接收到下载任务场景
                    DownloadDataInfo downloadData = this.WaittingDownload(timeout);
                    if (downloadData != null)
                    {
                        VerifyDiskSize(saveFile, downloadData.FileSize);
                        VerifyDiskSize(downloadData.Filename, downloadData.FileSize);
                    }
                    else
                    {
                        throw new RPAException(eErrorMessageType.Error_NotDownloadData);
                    }
                    var endTime = Stopwatch.GetTimestamp();
                    timeout -= Convert.ToInt32((endTime - startTime) * 1000D / Stopwatch.Frequency);
                    if (this.WaittingDownloadComplete(ref downloadData, path, timeout))
                    {
                        string pathExtentsion = Path.GetExtension(path);
                        string downloadPath = GetDefaultDirectory(downloadData);
                        if (!string.IsNullOrEmpty(pathExtentsion))
                        {
                            string downloadPathExtentsion = Path.GetExtension(downloadPath);
                            if (!pathExtentsion.EqualIgnoreCase(downloadPathExtentsion))
                            {
                                if (File.Exists(downloadPath))
                                {
                                    if (File.Exists(path))
                                    {
                                        File.Delete(path);
                                    }
                                    File.Move(downloadPath, path);
                                }
                            }
                        }
                        else
                        {
                            path = downloadPath;
                        }
                    }
                }
            }
            else
            {
                DownloadDataInfo downloadData = this.WaittingDownload(timeout);
                if (downloadData != null)
                {
                    if (string.IsNullOrEmpty(downloadData.Filename) && string.IsNullOrEmpty(fileName))
                        throw new RPAException(eErrorMessageType.Error_UnavailableDownloadDefaultFileName);
                    VerifyDiskSize(saveFile, downloadData.FileSize);
                    VerifyDiskSize(downloadData.Filename, downloadData.FileSize);
                }
                else
                {
                    throw new RPAException(eErrorMessageType.Error_NotDownloadData);
                }

                if (this.WaittingDownloadComplete(ref downloadData, null, timeout))
                {
                    string defaultDirectory = GetDefaultDirectory(downloadData);
                    path = FormattedFilePath(saveFile, fileName, defaultDirectory);
                    if (path != defaultDirectory)
                    {
                        var endTime = Stopwatch.GetTimestamp();
                        timeout -= Convert.ToInt32((endTime - startTime) * 1000D / Stopwatch.Frequency);
                        path = this.Save(saveMode, defaultDirectory, path, timeout);
                    }
                }
            }
            return path;
        }

        public virtual string DownloadDialog(string saveFile, string fileName, int interval, InputMode inputMode, SaveMode saveMode, int timeout, int waitDialogTime, int buttonType)
        {
            string path = string.Empty;
            var startTime = Stopwatch.GetTimestamp();

            var handle = this.WaittingSaveDialog(waitDialogTime);// 超时时间不该计算在下载任务中 ，但是目前无专门参数控制 暂时计算进来
            path = DialogHelper.SaveDialogWindow(handle, saveFile, fileName, interval, inputMode, buttonType, saveMode);
            if (buttonType == NativeMethods.CANCEL_BUTTON_ID)
            {
                //如果选择的是取消按钮，点击完后直接返回
                return path;
            }
            var endTime = Stopwatch.GetTimestamp();
            timeout -= Convert.ToInt32((endTime - startTime) * 1000D / Stopwatch.Frequency);
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            while (stopWatch.ElapsedMilliseconds <= timeout)
            {
                try
                {
                    if (!FileHelper.Exists(path))
                    {
                        Thread.Sleep(200);
                        continue;
                    }

                    var size1 = FileHelper.FileSize(path);
                    Thread.Sleep(500);
                    var size2 = FileHelper.FileSize(path);

                    if (size1 == size2 && size1 > 0)
                        return path;

                    // 文件大小还在变化，继续等待

                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"判断本地文件是否存在异常");
                }
            }
            throw new RPAException(eErrorMessageType.Error_NotDownloadData);
        }

        #region 上传文件/对话框

        public virtual void FileUploader(UploadType uploadType, string filePath, int interval, InputMode inputMode, int waitDialogTime)
        {
            try
            {
                //获取保存文件对话框句柄，下载文件的获取代码复用
                var handle = this.WaittingUploadSaveDialog(waitDialogTime);// 超时时间不该计算在下载任务中 ，但是目前无专门参数控制 暂时计算进来
                var browserHandle = this.TabPage.BrowserWindowInfo.Handle;
                this.HandleSaveUploadDialog(browserHandle, handle, filePath, interval, inputMode, uploadType);
            }
            catch (Exception e)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, $"上传文件异常");
                throw e;
            }
        }
        public virtual void UploaderDialog(UploadType uploadType, string filePath, int interval, InputMode inputMode, int waitDialogTime, int buttonType)
        {
            try
            {
                //获取保存文件对话框句柄，下载文件的获取代码复用
                var handle = this.WaittingUploadSaveDialog(waitDialogTime);
                var browserHandle = this.TabPage.BrowserWindowInfo.Handle;
                DialogHelper.SaveUploadDialogWindow(browserHandle, handle, filePath, interval, inputMode, uploadType, buttonType);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        #endregion
    }
}

