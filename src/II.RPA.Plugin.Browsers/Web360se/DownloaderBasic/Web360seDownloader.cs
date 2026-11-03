using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.EnumWindow;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.FacDialog;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.Plugin.GUI.Commons.Win32Tools;
using II.RPA.Plugin.Browsers.Web360se;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.DownloaderBasic;
using II.RPA.WebBrowser.Common.Basic.Params;
using II.RPA.WebBrowser.Common.Utils;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace II.RPA.Plugin.Browsers.Web360se.DownloaderBasic
{
    public class Web360seDownloader : ElementDownloader
    {
        protected new static ILogger logger = Logger.OfContext();
        public override eGuiType GuiType => eGuiType.Web360se;
        public override Browser Browser { get; }
        //public Web360seDownloader(PageElement pageElement) : base(pageElement)
        //{
        //}
        public Web360seDownloader(BrowserTabPage tabPage) : base(tabPage)
        {
            this.Browser = tabPage.Browser;
        }
        public override string GetBrowserPreferencesPath()
        {
            var localApplication = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var chromePath = @"360se6\User Data\Default\Preferences";
            return Path.Combine(localApplication, chromePath);
        }
        public override bool IsTurnOnAskSaveFile()
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
                    //如果silent=true,说明开启了静默下载，询问肯定是不生效的，直接返回
                    if (download.TryGetValue("silent", out JToken silentResult))
                    {
                        if (silentResult.As<bool>())
                        {
                            return false;
                        }
                        else
                        {
                            //如果有prompt_for_download属性，直接返回它的值即可
                            if (download.TryGetValue("prompt_for_download", out JToken result))
                            {
                                return result.As<bool>();
                            }
                            else
                            {
                                //观察下来，silent=true时，prompt_for_download属性不存在时，实际就是勾选的询问
                                return true;
                            }
                        }
                    }
                    else
                    {
                        //如果silent属性都没有，再判定为获取不到下载设置吧
                        throw new RPAException(GuiType, eErrorMessageType.Error_UnavailableDownloadWindow);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                return false;
            }
        }
        public IntPtr WaittingSaveDialog360(int timeout = 30000)
        {
            return this.WaittingDialog360(isDowload: true, timeout);
        }
        public IntPtr WaittingDialog360(bool isDowload, int timeout = 30000)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            while (stopWatch.ElapsedMilliseconds <= timeout)
            {
                var browserHandle = this.TabPage.BrowserWindowInfo.Handle;
                var browserWnd = UiCommon.UIAAutomation.FromHandle(browserHandle);
                List<ConditionBase> dialogCondition = new List<ConditionBase>();
                dialogCondition.Add(new PropertyCondition(UiCommon.UIAAutomation.PropertyLibrary.Element.ClassName, "Chrome_WidgetWin_2"));
                dialogCondition.Add(new PropertyCondition(UiCommon.UIAAutomation.PropertyLibrary.Element.IsDialog, true));
                AndCondition dialogAd = new AndCondition(dialogCondition);
                var dialogWnd = browserWnd.FindFirstChild(dialogAd);
                if (dialogWnd != null)
                {
                    stopWatch.Stop();
                    return dialogWnd.Properties.NativeWindowHandle;
                }
                Task.Delay(20).Wait();
            }
            throw new RPAException(GuiType, isDowload ? eErrorMessageType.Error_WaittingSaveDialog_Timeout_Error : eErrorMessageType.Error_WaittingUploadSaveDialog_Timeout_Error);
        }



        public override string Downloader(string saveFile, string fileName, int interval, InputMode inputMode, SaveMode saveMode, int timeout, int waitDialogTime)
        {
            string path = string.Empty;
            var startTime = Stopwatch.GetTimestamp();

            if (this.VerifyBrowserDownloadMode())
            {
                var handle = this.WaittingSaveDialog360(waitDialogTime);// 超时时间不该计算在下载任务中 ，但是目前无专门参数控制 暂时计算进来
                path = this.HandleSaveDialog(handle, saveFile, fileName, interval, inputMode, saveMode);
                if (saveMode != SaveMode.Skip)
                {
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
                            if (!Path.GetFileName(path).EqualIgnoreCase(Path.GetFileName(downloadPath)))
                            {
                                if (File.Exists(path))
                                {
                                    File.Delete(path);
                                }
                                File.Move(downloadPath, path);
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
                DownloadDataInfo downloadData = this.WaittingDownload();
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

        public override string DownloadDialog(string saveFile, string fileName, int interval, InputMode inputMode, SaveMode saveMode, int timeout, int waitDialogTime, int buttonType)
        {
            string path = string.Empty;
            var startTime = Stopwatch.GetTimestamp();

            try
            {
                var handle = this.WaittingSaveDialog360(waitDialogTime / 2);// 超时时间不该计算在下载任务中 ，但是目前无专门参数控制 暂时计算进来
                path = Web360seDownloader.SaveDialogWindow(handle, saveFile, fileName, interval, inputMode, buttonType, saveMode);
                if (buttonType == NativeMethods.CANCEL_BUTTON_ID)
                {
                    //如果选择的是取消按钮，点击完后直接返回
                    return path;
                }
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
                //this.WaittingDownloadComplete(ref downloadData, path, timeout);
                if (this.WaittingDownloadComplete(ref downloadData, path, timeout))
                {
                    string pathExtentsion = Path.GetExtension(path);
                    string downloadPath = GetDefaultDirectory(downloadData);
                    if (!string.IsNullOrEmpty(pathExtentsion))
                    {
                        string downloadPathExtentsion = Path.GetExtension(downloadPath);
                        if (!pathExtentsion.EqualIgnoreCase(downloadPathExtentsion))
                        {
                            if (File.Exists(path))
                            {
                                File.Delete(path);
                            }
                            File.Move(downloadPath, path);
                        }
                        if (!Path.GetFileName(path).EqualIgnoreCase(Path.GetFileName(downloadPath)))
                        {
                            if (File.Exists(path))
                            {
                                File.Delete(path);
                            }
                            File.Move(downloadPath, path);
                        }
                    }
                    else
                    {
                        path = downloadPath;
                    }
                }
                return path;
            }
            catch (RPAException rpaEx)
            {
                if (rpaEx.ErrorMessageType == eErrorMessageType.Error_WaittingSaveDialog_Timeout_Error)
                {
                    logger.LogWarning("360自定义的对话框未找到，进行系统级别对话框处理");
                    return base.DownloadDialog(saveFile, fileName, interval, inputMode, saveMode, timeout, waitDialogTime / 2, buttonType);
                }
                throw rpaEx;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public override string HandleSaveDialog(IntPtr handle, string saveFile, string fileName, int interval, InputMode inputMode, SaveMode saveMode)
        {
            return SaveDialogWindow(handle, saveFile, fileName, interval, inputMode, NativeMethods.SAVE_BUTTON_ID, saveMode);
        }
        public static string SaveDialogWindow(IntPtr handle, string saveFile, string fileName, int interval, InputMode inputMode, int buttonType, SaveMode saveMode)
        {
            string path = Path.Combine(saveFile, fileName);
            var wndDialog = UiCommon.UIAAutomation.FromHandle(handle);
            var cancelBtnTreePath = new int[] { 0, 0, 0, 0, 1, 2, 1 };
            var cancelBtn = FindElementByIndexPath(wndDialog, cancelBtnTreePath);
            //取消按钮
            if (buttonType != NativeMethods.SAVE_BUTTON_ID)
            {
                if (cancelBtn != null)
                {
                    cancelBtn.Click();
                }
                else
                {
                    //未找到取消按钮
                    logger.LogError("未找到取消按钮");
                    throw new RPAException(eErrorMessageType.Error_360se_TreeStructure_NotMatch);
                }
                return "";
            }
            //存在时直接跳过
            if (saveMode == SaveMode.Skip)
            {
                if (File.Exists(path))
                {
                    logger.LogDebug($"目标文件【{path}】已存在同名，进行跳过");
                    //点击取消按钮
                    cancelBtn?.Click();
                    return path;
                }
            }
            //副本
            else if (saveMode == SaveMode.Copy)
            {
                var origionName = Path.GetFileNameWithoutExtension(path);
                var fileExtension = Path.GetExtension(path);
                if (FileHelper.Exists(path))
                {
                    int index = 1;
                    do
                    {
                        var newFileName = $"{origionName}({index.ToString()})";
                        path = Path.Combine(saveFile, $"{newFileName}{fileExtension}");
                        fileName = newFileName;
                        logger.LogDebug($"进行文件副本的命名：{newFileName}");
                        index++;
                    }
                    while (FileHelper.Exists(path) && index < 50);
                }
            }
            //替换
            else if (saveMode == SaveMode.Replace)
            {
                //360浏览器下载对话框比较特殊，文件存在时不会alert框提醒是否替换还是取消，会默认生成副本
                logger.LogDebug($"文件存在时替换操作");
            }
            #region 输入文件存储路径
            var fileDirTreePath = new int[] { 0, 0, 0, 0, 1, 1, 1, 0, 0 };
            var fileDirInput = FindElementByIndexPath(wndDialog, fileDirTreePath);
            if (fileDirInput != null)
            {
                var point = fileDirInput.BoundingRectangle.Center();
                AutoItHelper.MouseClick("left", point);
                Thread.Sleep(200);
                UiCommon.SetClipBoard(saveFile);
                AutoItHelper.Send("^a");
                Thread.Sleep(100);
                AutoItHelper.Send("^v");
                Thread.Sleep(100);
                UiCommon.ClearClipBoard();
            }
            else
            {
                //未找到下载到目录对应的输入框
                logger.LogError("未找到下载到目录对应的输入框");
                throw new RPAException(eErrorMessageType.Error_360se_TreeStructure_NotMatch);
            }
            #endregion
            #region 输入文件名
            var fileNameTreePath = new int[] { 0, 0, 0, 0, 1, 0, 1, 1, 0 };
            var fileNameInput = FindElementByIndexPath(wndDialog, fileNameTreePath);
            if (fileNameInput != null)
            {
                var fileNameEdit = fileNameInput.AsTextBox();
                if (fileNameEdit != null)
                {
                    var defaultFileName = fileNameEdit.Text;
                    if (!string.IsNullOrEmpty(defaultFileName))
                    {
                        logger.LogDebug($"默认文件名为：{defaultFileName}");
                        path = FileHelper.FormattedFilePath(saveFile, fileName, defaultFileName);
                        fileName = Path.GetFileName(path);
                        logger.LogDebug($"最终指定的文件名为：{fileName}");
                    }
                }
                var point = fileNameInput.BoundingRectangle.Center();
                AutoItHelper.MouseClick("left", point);
                Thread.Sleep(200);
                UiCommon.SetClipBoard(fileName);
                AutoItHelper.Send("^a");
                Thread.Sleep(100);
                AutoItHelper.Send("^v");
                Thread.Sleep(100);
                UiCommon.ClearClipBoard();
            }
            else
            {
                //未找到文件名对应的输入框
                logger.LogError("未找到文件名对应的输入框");
                throw new RPAException(eErrorMessageType.Error_360se_TreeStructure_NotMatch);
            }
            #endregion
            //点击下载
            {
                var downloadBtnTreePath = new int[] { 0, 0, 0, 0, 1, 2, 0 };
                var downloadBtn = FindElementByIndexPath(wndDialog, downloadBtnTreePath);
                if (downloadBtn != null)
                {
                    downloadBtn.Click();
                }
                else
                {
                    //未找到下载按钮
                    logger.LogError("未找到下载按钮");
                    throw new RPAException(eErrorMessageType.Error_360se_TreeStructure_NotMatch);
                }
            }
            return path;
        }
        public override bool IsDownloadComplete(DownloadDataInfo dataInfo, string path)
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
            var origionName = Path.GetFileNameWithoutExtension(path);
            var finalName = Path.GetFileNameWithoutExtension(dataInfo.Filename);
            //下载记录中的文件名和来源文件名不一致
            if (!origionName.EqualIgnoreCase(finalName))
            {
                string escaped = Regex.Escape(origionName);
                string pattern = $"^{escaped} \\(\\d+\\)$";
                if (Regex.IsMatch(finalName, pattern))
                {
                    //可能是系统自动生成了副本
                    path = dataInfo.Filename;
                }
            }
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
        /// <summary>
        /// 通过层级索引路径精准定位控件
        /// </summary>
        /// <param name="rootElement"></param>
        /// <param name="indexPath">层级索引数组。例如 {0, 2, 1} 表示：下移到第0个 -> 下移到第2个 -> 拿到第1个</param>
        /// <returns></returns>
        public static AutomationElement FindElementByIndexPath(AutomationElement rootElement, int[] indexPath)
        {
            if (rootElement == null || indexPath == null || indexPath.Length == 0)
            {
                return null;
            }

            AutomationElement currentElement = rootElement;

            for (int i = 0; i < indexPath.Length; i++)
            {
                int targetIndex = indexPath[i];

                AutomationElement[] childrens = currentElement.FindAllChildren();

                if (childrens.Length > targetIndex)
                {
                    currentElement = childrens[targetIndex];
                }
                else
                {
                    logger.LogDebug($"定位失败：在探寻第 {i + 1} 层级（请求索引 {targetIndex}）时越界。实际可用子元素仅有 {childrens.Length} 个。");
                    return null;
                }
            }

            return currentElement;
        }

    }
}
