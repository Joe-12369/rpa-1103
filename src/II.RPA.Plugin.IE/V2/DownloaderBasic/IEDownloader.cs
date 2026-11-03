using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.Plugin.GUI.Commons.Win32Tools;
using II.RPA.Plugin.IE.V2.Automation.Core;
using II.RPA.Plugin.IE.V2.Automation.Provider;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.DownloaderBasic;
using II.RPA.WebBrowser.Common.Utils;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Diagnostics;
using System.IO;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System.Linq;
using II.RPA.Plugin.Core.EnumWindow;

namespace II.RPA.Plugin.IE.V2.DownloaderBasic
{
    public class IEDownloader : ElementDownloader
    {
        protected ILogger logger = Logger.OfContext();
        public override eGuiType GuiType => eGuiType.IE;
        public override Browser Browser { get; }
        /// <summary>
        /// 0-点击元素触发的另存为对话框 1-手动Ctrl+s触发的对话框
        /// </summary>
        private int dialogType = 0;
        public IEDownloader(BrowserTabPage tabPage) : base(tabPage)
        {
            this.Browser = tabPage.Browser;
        }
        public override void AddCreatedEvent(ParamDownloaderModel param)
        {
            //防止异常
        }
        public override void RemoveCreatedEvent()
        {
            //防止异常
        }
        public override string GetBrowserPreferencesPath()
        {
            return string.Empty;
        }
        public override IntPtr WaittingSaveDialog(int timeout = 20000)
        {
            return WaittingDialog(true, timeout);
        }
        public override IntPtr WaittingUploadSaveDialog(int timeout = 20000)
        {
            return WaittingDialog(false, timeout);
        }
        public new IntPtr WaittingDialog(bool isDowload, int timeout = 30000)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            Win32Window wnd = null;
            while (stopWatch.ElapsedMilliseconds <= timeout)
            {
                var wndDesktop = Win32Window.GetDesktopWindow();
                WindowHandleInfo windowHandleInfo = new WindowHandleInfo(wndDesktop.Handle, new System.Collections.Generic.List<string>() { "#32770" });
                var listHandle = windowHandleInfo.GetAllChildHandles();
                foreach (var handle in listHandle)
                {
                    wnd = new Win32Window(handle);
                    if (wnd.Handle != IntPtr.Zero
                      && wnd.IsVisible
                      && wnd.IsEnabled)
                    {
                        logger.LogDebug($"wnd{wnd.GetText()}");
                        //提取对话框逻辑修改了下，原来的只适应中文系统下的下载另存为，修改后可以兼容多语言操作系统下下载另存为和上传文件对话框，自测是无问题
                        if (wnd.ClassName.Equals("#32770"))
                        {
                            //IE 浏览器下的另存为窗口为非模态框
                            //if (!wnd.IsModal)
                            dialogType = wnd.IsModal ? 1 : 0;
                            //如果另存为对话框处于未激活状态，将其激活
                            if (!wnd.IsActive())
                            {
                                wnd.WinActivate();
                            }
                            return wnd.Handle;
                        }
                    }
                }
            }
            throw new RPAException(GuiType, isDowload ? eErrorMessageType.Error_WaittingSaveDialog_Timeout_Error : eErrorMessageType.Error_WaittingUploadSaveDialog_Timeout_Error);
        }
        public override string Downloader(string saveFile, string fileName, int interval, InputMode inputMode, SaveMode saveMode, int timeout, int waitDialogTime)
        {
            string path = string.Empty;
            //var browser = Browser.AcquireCurrentActiveTab();
            var ieMode = VerifyIEVersion();

            if (ieMode == IEMode.IE8)
            {
                IEHelper.HandleIEDownloadNotificationWindow((IntPtr)this.TabPage.TabId);
            }
            else
            {
                IEHelper.HandleIEDownloadNotificationBar((IntPtr)this.TabPage.TabId);
            }
            var handleDialog = this.WaittingSaveDialog();
            path = this.HandleSaveDialog(handleDialog, saveFile, fileName, interval, inputMode, saveMode);
            if (saveMode != SaveMode.Skip)
            {
                DownloadDataInfo dataInfo = new DownloadDataInfo();
                dataInfo.Filename = path;
                dataInfo.Handle = (IntPtr)this.TabPage.TabId;
                // this.WaittingDownloadComplete(ref dataInfo, path, timeout);
                if (this.WaittingDownloadComplete(ref dataInfo, path, timeout))
                {
                    string pathExtentsion = Path.GetExtension(path);
                    string downloadPath = GetDefaultDirectory(dataInfo);
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
                    }
                    else
                    {
                        path = downloadPath;
                    }
                }
            }
            return path;
        }
        public override string DownloadDialog(string saveFile, string fileName, int interval, InputMode inputMode, SaveMode saveMode, int timeout, int waitDialogTime, int buttonType)
        {
            string path = string.Empty;
            //IE 浏览器下载的另存为对话框前置弹窗处理逻辑去除（区别于下载文件，只处理对话框）
            //var ieMode = VerifyIEVersion();

            //if (ieMode == IEMode.IE8)
            //{
            //    IEHelper.HandleIEDownloadNotificationWindow((IntPtr)this.TabPage.TabId);
            //}
            //else
            //{
            //    IEHelper.HandleIEDownloadNotificationBar((IntPtr)this.TabPage.TabId);
            //}
            var handleDialog = this.WaittingSaveDialog(waitDialogTime);
            //path = this.HandleSaveDialog(handleDialog, saveFile, fileName, interval, inputMode);
            path = DialogHelper.SaveDialogWindow(handleDialog, saveFile, fileName, interval, inputMode, buttonType, saveMode);
            if (buttonType == NativeMethods.CANCEL_BUTTON_ID)
            {
                //如果选择的是取消按钮，点击完后直接返回
                return path;
            }
            DownloadDataInfo dataInfo = new DownloadDataInfo();
            dataInfo.Filename = path;
            dataInfo.Handle = (IntPtr)this.TabPage.TabId;
            //this.WaittingDownloadComplete(ref dataInfo, path, timeout);
            if (dialogType == 1)
            {
                return path;
            }
            if (this.WaittingDownloadComplete(ref dataInfo, path, timeout))
            {
                string pathExtentsion = Path.GetExtension(path);
                string downloadPath = GetDefaultDirectory(dataInfo);
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
                }
                else
                {
                    path = downloadPath;
                }
            }
            return path;
        }
        private bool WaittingClose(IntPtr handle)
        {
            logger.LogDebug($"WaittingClose");
            var ieMode = VerifyIEVersion();
            if (ieMode == IEMode.IE8)
            {
                return IEHelper.HandleDownloadNotificationWindowFinish(handle);
            }
            else
            {
                return IEHelper.HandleDownloadNotificationBarFinish(handle);
            }
        }
        public override bool WaittingDownloadComplete(ref DownloadDataInfo dataInfo, string path, int timeout = 10000)
        {
            #region [等待下载]
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
                logger.LogDebug($"path {dataInfo.Filename} handle {dataInfo.Handle}");
                while (stopWatch.ElapsedMilliseconds <= timeout)
                {
                    if (FileHelper.Exists(dataInfo.Filename) && WaittingClose(dataInfo.Handle))
                    {
                        stopWatch.Stop();
                        return true;
                    }
                }
                stopWatch.Stop();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            #endregion
            throw new RPAException(this.GuiType, eErrorMessageType.Error_DownloadTimeOut);
        }
        public override string AutomaticOperationDialog(IntPtr handle, string saveFile, string fileName, int interval, InputMode inputMode)
        {
            return DialogHelper.SaveDialogWindow(handle, saveFile, fileName, interval, inputMode);
        }
        public IEMode VerifyIEVersion()
        {
            var version = IEHelper.GetInternetExplorerVersion();
            Version ver = new Version(version);
            logger.LogDebug($"version {ver.Major}");
            if (ver.Major <= 8)
            {
                return IEMode.IE8;
            }
            else
            {
                return IEMode.IE11;
            }
        }
    }
}
