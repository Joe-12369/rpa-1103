using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.Plugin.GUI.Commons.Win32Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FlaUI.Core.AutomationElements;
using System.Diagnostics;
using Microsoft.Win32;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Globalization;
using II.RPA.Plugin.Core;
using System.IO;
using II.RPA.Plugin.GUI.Commons.WindowMatch;
using II.RPA.Plugin.Core.Commons;
using System.Threading.Tasks;
using System.Text;
using II.RPA.WinAutomation.CommonUtility;
using System.Runtime.CompilerServices;
using FlaUI.Core.Input;
using System.Drawing;
using FlaUI.Core.WindowsAPI;

namespace II.RPA.WebBrowser.Common.Utils
{
    public class DialogHelper
    {
        const int OUT_TIME = 3000;
        const int INTERVAL = 200;

        private static ILogger logger = Logger.OfContext();
        public static bool IsHideFileExtensions()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
                {
                    if (key != null)
                    {
                        object hideFileExtValue = key.GetValue("HideFileExt");

                        if (hideFileExtValue != null)
                        {
                            int hideFileExt = Convert.ToInt32(hideFileExtValue);
                            return hideFileExt == 1;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError("IsHideFileExtensions: " + ex.Message);
            }

            return false; // 默认情况下返回false，表示后缀名未被隐藏
        }
        private static IntPtr FindWindowHandle(string name)
        {
            var processes = UiCommon.GetProcessesByName(name, true);
            foreach (Process p in processes)
            {
                if (p.MainWindowHandle != IntPtr.Zero)
                {
                    return p.MainWindowHandle;
                }
            }
            return IntPtr.Zero;
        }

        public static void ConfirmDownload(string name, int timeout)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            while (stopWatch.ElapsedMilliseconds <= timeout)
            {
                IntPtr tabIntPtr = FindWindowHandle(name);
                var element = UiCommon.UIAAutomation.FromHandle(tabIntPtr);
                var childs = element.FindAllChildren();
                foreach (var item in childs)
                {
                    try
                    {
                        var isDowload = item.FindAllDescendants()?.Any(a => (a.Properties.Name.IsSupported && (a.Name == "下载" || a.Name == "Downloads")) && a.Properties.AriaRole.IsSupported && a.Properties.AriaRole == "dialog") == true;
                        if (isDowload)
                        {
                            var els = item.FindAllDescendants()?.FirstOrDefault(a => (a.Properties.Name.IsSupported && (a.Name == "下载" || a.Name == "Downloads")) && a.Properties.AriaRole.IsSupported && a.Properties.AriaRole == "dialog").FindAllDescendants();
                            foreach (var el in els)
                            {
                                //找到第一个打开按钮并点击开始下载
                                // 原先为打开
                                if (el.Properties.Name.IsSupported
                                    && (el.Name == "另存为" || el.Name == "Save as")
                                    && el.Properties.ControlType.IsSupported
                                    && el.ControlType == FlaUI.Core.Definitions.ControlType.Button)
                                {
                                    var openrect = el.BoundingRectangle.Center();
                                    Mouse.MoveTo(openrect.X, openrect.Y);
                                    Thread.Sleep(300);
                                    openrect = el.BoundingRectangle.Center();
                                    AutoItHelper.MouseClick("left", openrect);
                                    stopWatch.Stop();
                                    return;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"遍历目标确认对话框异常");
                        continue;
                    }
                }
            }
        }
        public static string SaveDialogWindow(IntPtr handle, string saveFile, string fileName, int interval, InputMode inputMode)
        {
            return SaveDialogWindow(handle, saveFile, fileName, interval, inputMode, NativeMethods.SAVE_BUTTON_ID, SaveMode.None);
        }
        public static string SaveDialogWindow(IntPtr handle, string saveFile, string fileName, int interval, InputMode inputMode, int buttonType, SaveMode saveMode)
        {
            //取消按钮
            if (buttonType != NativeMethods.SAVE_BUTTON_ID)
            {
                IntPtr hwndCancelButton = NativeMethods.GetDlgItem(handle, NativeMethods.CANCEL_BUTTON_ID);
                if (hwndCancelButton != IntPtr.Zero)
                {
                    NativeMethods.PostMessage(hwndCancelButton, (int)NativeMethods.BM_CLICK, IntPtr.Zero, IntPtr.Zero);
                }
                return "";
            }
            var wndDialog = UiCommon.UIAAutomation.FromHandle(handle);
            List<ConditionBase> comboboxCondition = new List<ConditionBase>();
            //comboboxCondition.Add(new PropertyCondition(UiCommon.UIAAutomation.PropertyLibrary.Element.Name, "文件名:"));
            comboboxCondition.Add(new PropertyCondition(UiCommon.UIAAutomation.PropertyLibrary.Element.ClassName, "AppControlHost"));
            AndCondition comboboxad = new AndCondition(comboboxCondition);
            var combox = RPAUtils.RetryWait(() => wndDialog.FindAll(TreeScope.Descendants, comboboxad).FirstOrDefault(), OUT_TIME, INTERVAL);
            var dialogFileName = combox.AsComboBox().EditableText;
            var path = FileHelper.FormattedFilePath(saveFile, fileName, dialogFileName);

            var point1 = combox.BoundingRectangle.Center();
            AutoItHelper.MouseClick("left", point1);
            Win32Action win32Action = new Win32Action(combox.Properties.NativeWindowHandle);
            win32Action.Input(path, inputMode, true, true, interval);

            Thread.Sleep(3000);
            IntPtr hwndSaveButton = NativeMethods.GetDlgItem(handle, NativeMethods.SAVE_BUTTON_ID);
            if (hwndSaveButton != IntPtr.Zero)
            {
                NativeMethods.PostMessage(hwndSaveButton, (int)NativeMethods.BM_CLICK, IntPtr.Zero, IntPtr.Zero);
            }
            //需要判断文件存在是否覆盖的情况；
            //Task.Run(() =>
            //{
            #region 原处理下载二次确认是否替换窗口处理逻辑
            //if (File.Exists(path))
            //{
            int copyIndex = 0;
            DesktopWindow desktopWindow = new DesktopWindow();

            var msgDialogWnd = RPAUtils.RetryWait(() =>
            {
                try
                {
                    return desktopWindow.FindAllSavaDialogWindow(wnd => NativeMethods.MatchAncestor(wnd.Handle, handle)).FirstOrDefault();
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "查找另存为确认对话框异常");
                    return null;
                }
            }, OUT_TIME, INTERVAL);

            if (msgDialogWnd != null)
            {
                //文件存在时跳过
                if (saveMode == SaveMode.Skip)
                {
                    //先找出确认框的否按钮，进行点击
                    var noBtnHandle = msgDialogWnd.FindAllChildWindowEx(wnd => wnd.ClassName == "Button", 99).Skip(1).FirstOrDefault();
                    if (noBtnHandle.Handle != IntPtr.Zero)
                    {
                        logger.LogInformation("找到确认是否替换还是取消对话框");
                        //NativeMethods.SendMessage(noBtnHandle.Handle, NativeMethods.BM_CLICK, IntPtr.Zero, IntPtr.Zero);
                        NativeMethods.PostMessage(noBtnHandle.Handle, (int)NativeMethods.BM_CLICK, IntPtr.Zero, IntPtr.Zero);
                    }
                    //再进行对话框取消按钮点击关闭对话框
                    IntPtr hwndCancelButton = NativeMethods.GetDlgItem(handle, NativeMethods.CANCEL_BUTTON_ID);
                    if (hwndCancelButton != IntPtr.Zero)
                    {
                        logger.LogInformation("另存为窗口取消按钮执行");
                        NativeMethods.PostMessage(hwndCancelButton, (int)NativeMethods.BM_CLICK, IntPtr.Zero, IntPtr.Zero);
                    }
                }
                //生成副本
                if (saveMode == SaveMode.Copy)
                {
                    //先找出确认框的否按钮，进行点击
                    var noBtnHandle = msgDialogWnd.FindAllChildWindowEx(wnd => wnd.ClassName == "Button", 99).Skip(1).FirstOrDefault();
                    Point noBtnPoint = Point.Empty;
                    //如果确认框一直在，说明一直文件存在
                    while (msgDialogWnd != null && msgDialogWnd.Handle != IntPtr.Zero)
                    {
                        copyIndex++;
                        if (noBtnPoint.Equals(Point.Empty))
                            noBtnPoint = noBtnHandle.AutomationElement.BoundingRectangle.Center();
                        Mouse.LeftClick(noBtnPoint);
                        string extension = Path.GetExtension(path);
                        string newPath = "";
                        if (string.IsNullOrEmpty(extension))
                        {
                            newPath = $"{path} ({copyIndex})";
                        }
                        else
                        {
                            newPath = $"{path.Substring(0, path.LastIndexOf(extension))} ({copyIndex}){extension}"; ;
                        }
                        AutoItHelper.MouseClick("left", point1);
                        win32Action.Input(newPath, inputMode, true, true, interval);

                        Thread.Sleep(500);
                        if (hwndSaveButton != IntPtr.Zero)
                        {
                            NativeMethods.PostMessage(hwndSaveButton, (int)NativeMethods.BM_CLICK, IntPtr.Zero, IntPtr.Zero);
                        }
                        int times = 0;
                        while ((msgDialogWnd == null || times == 0) && times < 3)
                        {
                            msgDialogWnd = RPAUtils.RetryWait(() => desktopWindow.FindAllSavaDialogWindow(wnd => NativeMethods.MatchAncestor(wnd.Handle, handle)).FirstOrDefault(), OUT_TIME, INTERVAL);
                            if (msgDialogWnd != null && msgDialogWnd.Handle != IntPtr.Zero)
                            {
                                break;
                            }
                            Thread.Sleep(1000);
                            times++;
                        }
                        if (msgDialogWnd == null || msgDialogWnd.Handle == IntPtr.Zero)
                            path = newPath;
                    }
                }
                //目前没传或者替换走原逻辑
                else
                {
                    var yesBtnHandle = msgDialogWnd.FindAllChildWindowEx(wnd => wnd.ClassName == "Button", 99).FirstOrDefault();
                    if (yesBtnHandle.Handle != IntPtr.Zero)
                    {
                        NativeMethods.SendMessage(yesBtnHandle.Handle, NativeMethods.BM_CLICK, IntPtr.Zero, IntPtr.Zero);
                    }
                }
            }
            else
            {
                logger.LogInformation("未找到【确认另存为】对话框");
            }
            //}
            #endregion

            //});
            return path;
        }

        #region 上传文件对话框

        public static void SaveUploadDialogWindow(IntPtr browserHandle, IntPtr handle, string filePath, int interval, InputMode inputMode, UploadType uploadType)
        {
            SaveUploadDialogWindow(browserHandle, handle, filePath, interval, inputMode, uploadType, NativeMethods.SAVE_BUTTON_ID);
        }
        public static void SaveUploadDialogWindow(IntPtr browserHandle, IntPtr handle, string filePath, int interval, InputMode inputMode, UploadType uploadType, int buttonType)
        {
            logger.LogDebug($"文件地址：{filePath}");
            bool isSetWorkDir = false;//是否设置工作目录
            bool isNeedEditFileText = true;//是否需要输入文件路径
            //取消按钮
            if (buttonType != NativeMethods.SAVE_BUTTON_ID)
            {
                IntPtr hwndCancelButton = NativeMethods.GetDlgItem(handle, NativeMethods.CANCEL_BUTTON_ID);
                if (hwndCancelButton != IntPtr.Zero)
                {
                    NativeMethods.PostMessage(hwndCancelButton, (int)NativeMethods.BM_CLICK, IntPtr.Zero, IntPtr.Zero);
                }
                return;
            }
            var wndDialog = UiCommon.UIAAutomation.FromHandle(handle);
            List<ConditionBase> pathEditCondition = new List<ConditionBase>();
            pathEditCondition.Add(new PropertyCondition(UiCommon.UIAAutomation.PropertyLibrary.Element.ClassName, "Edit"));
            AndCondition pathEditAd = new AndCondition(pathEditCondition);
            var pathEdit = RPAUtils.RetryWait(() => wndDialog.FindFirst(TreeScope.Descendants, pathEditAd), OUT_TIME, INTERVAL);
            //需要做文件、文件夹的存在性判定
            StringBuilder stringPathBuilder = new StringBuilder();
            string targetDirName = "";
            //如果是文件夹
            if (uploadType == UploadType.Folder)
            {
                if (filePath.Split(';').Length > 1)
                {
                    filePath.Split(';').ForEach(file =>
                    {
                        if (!Directory.Exists(file))
                        {
                            var rpaException = new RPAException(eErrorMessageType.Error_DirectoryNotSupportOrExist_Error);
                            rpaException.CustomMessage = $"【{file}】{rpaException.ErrorCodeSuffixMessage}";
                            throw rpaException;
                        }
                        if (stringPathBuilder.ToString() != "")
                        {
                            stringPathBuilder.Append($" \"{file}\"");
                        }
                        else
                        {
                            stringPathBuilder.Append($"\"{file}\"");
                        }
                    });
                }
                else
                {
                    if (!Directory.Exists(filePath))
                    {
                        var rpaException = new RPAException(eErrorMessageType.Error_DirectoryNotSupportOrExist_Error);
                        rpaException.CustomMessage = $"【{filePath}】{rpaException.ErrorCodeSuffixMessage}";
                        throw rpaException;
                    }
                    //如果字符长度超过255，则先定位工作目录
                    if (filePath.Length > 255)
                    {
                        targetDirName = filePath;
                        isNeedEditFileText = false;
                        isSetWorkDir = true;
                    }
                    else
                    {
                        stringPathBuilder.Append(filePath);
                    }
                }
            }
            else
            {
                logger.LogDebug($"上传文件");
                int fileCount = filePath.Split(';').Length;
                //批量文件
                if (fileCount > 1)
                {
                    int symbolLength = fileCount * 2;//新增的双引号的长度
                    if (filePath.Length + symbolLength > 255)
                    {
                        string dirName = "";
                        filePath.Split(';').ForEach(file =>
                        {
                            string currentDirName = Path.GetDirectoryName(file);
                            string fileName = Path.GetFileName(file);
                            if (dirName != "" && currentDirName != dirName)
                            {
                                var rpaException = new RPAException(eErrorMessageType.Error_DiffWorkDir_Error);
                                rpaException.CustomMessage = $"【{filePath}】{rpaException.ErrorCodeSuffixMessage}";
                                throw rpaException;
                            }
                            dirName = currentDirName;
                            if (stringPathBuilder.ToString() != "")
                            {
                                stringPathBuilder.Append($" \"{fileName}\"");
                            }
                            else
                            {
                                stringPathBuilder.Append($"\"{fileName}\"");
                            }
                        });
                        targetDirName = dirName;
                        isSetWorkDir = true;
                    }
                    else
                    {
                        filePath.Split(';').ForEach(file =>
                        {
                            if (!File.Exists(file))
                            {
                                if (!CheckLongPathFileExists(filePath))
                                {
                                    var rpaException = new RPAException(eErrorMessageType.Error_FilePathNotSupportOrExist_Error);
                                    rpaException.CustomMessage = $"【{file}】{rpaException.ErrorCodeSuffixMessage}";
                                    throw rpaException;
                                }
                            }
                            if (stringPathBuilder.ToString() != "")
                            {
                                stringPathBuilder.Append($" \"{file}\"");
                            }
                            else
                            {
                                stringPathBuilder.Append($"\"{file}\"");
                            }
                        });
                    }
                }
                //单文件
                else
                {
                    if (!FileHelper.Exists(filePath))
                    {
                        if (!CheckLongPathFileExists(filePath))
                        {
                            var rpaException = new RPAException(eErrorMessageType.Error_DirectoryNotSupportOrExist_Error);
                            rpaException.CustomMessage = $"【{filePath}】{rpaException.ErrorCodeSuffixMessage}";
                            throw rpaException;
                        }
                    }
                    if (filePath.Length > 255)
                    {
                        string directory = Path.GetDirectoryName(filePath);
                        string fileName = Path.GetFileName(filePath);
                        targetDirName = directory;
                        isSetWorkDir = true;
                        stringPathBuilder.Append(fileName);
                    }
                    else
                    {
                        stringPathBuilder.Append(filePath);
                    }
                }
            }
            if (stringPathBuilder.ToString().Length > 255)
            {
                var rpaException = new RPAException(eErrorMessageType.Error_PathCharTooLong_Error);
                rpaException.CustomMessage = $"【{filePath}】{rpaException.ErrorCodeSuffixMessage}";
                throw rpaException;
            }
            if (isSetWorkDir)
            {
                //先定位工作目录
                var msgDialogWnd = new WindowObject(handle);
                var breadcrumb = msgDialogWnd.FindAllChildWindowEx(wnd => wnd.ClassName == "Breadcrumb Parent", 99).FirstOrDefault();
                var breadcrumbRectangle = breadcrumb.WindowRect;
                //不能点击中心点，最保险的方式是点击输入栏的最右边往左一点
                var clickPoint = new Point(breadcrumbRectangle.X + breadcrumbRectangle.Width - 1, breadcrumbRectangle.Y + (breadcrumbRectangle.Height / 2));
                AutoItHelper.MouseClick("left", clickPoint);
                Win32Action breadcrumbAction = new Win32Action(breadcrumb.Handle);
                breadcrumbAction.Input(targetDirName, inputMode, true, true, interval);
                Thread.Sleep(1000);
                Keyboard.Press(VirtualKeyShort.ENTER);
                Thread.Sleep(100);
                Keyboard.Release(VirtualKeyShort.ENTER);
            }
            if (isNeedEditFileText)
            {
                var point1 = pathEdit.BoundingRectangle.Center();
                AutoItHelper.MouseClick("left", point1);
                Win32Action win32Action = new Win32Action(pathEdit.Properties.NativeWindowHandle);
                win32Action.Input(stringPathBuilder.ToString(), inputMode, true, true, interval);
                Thread.Sleep(1000);
            }
            IntPtr hwndSaveButton = NativeMethods.GetDlgItem(handle, NativeMethods.SAVE_BUTTON_ID);
            if (hwndSaveButton != IntPtr.Zero)
            {
                NativeMethods.PostMessage(hwndSaveButton, (int)NativeMethods.BM_CLICK, IntPtr.Zero, IntPtr.Zero);
            }
            //点击打开按钮后，判断对话框是否还在
            int retryTime = 0;
            while (retryTime < 3)
            {
                //主要用于判断是否因文件路径无效导致的无法上传
                logger.LogDebug($"点击打开后,第{retryTime + 1}次判断对话框窗口是否存在");
                if (UiCommon.IsWindow(handle) && NativeMethods.IsWindowVisible(handle))
                {
                    logger.LogDebug("对话框窗口还在");
                    if (retryTime == 2)
                    {
                        throw new RPAException(eErrorMessageType.Error_FilePathNotSupportOrExist_Error);
                    }
                }
                else
                {
                    break;
                }
                Thread.Sleep(500);
                retryTime++;
            }
            //判断当选择的是文件夹时，会出现是否上传文件夹中所有文件确认弹框
            if (uploadType == UploadType.Folder)
            {
                Thread.Sleep(500);
                //找出确认弹框的窗口信息
                var wndBrowser = UiCommon.UIAAutomation.FromHandle(browserHandle);
                //360se浏览器查找上传文件确认弹框容易被页面其他弹框干扰判断，只做浏览器窗口子级查找兼容
                var browserName = wndBrowser.Properties.ClassName.IsSupported ? wndBrowser.ClassName : "";
                var childWnds = browserName.Contains("360se") ? wndBrowser.FindAllChildren() : wndBrowser.FindAllDescendants();
                AutomationElement targetAlertWnd = null;
                foreach (var childWnd in childWnds)
                {
                    //目前发现几种浏览器的弹框确认基本都符合这两特征中的至少一种，先用此种方式作为判断确认窗口的手段，后续详细测试再看是否进一步优化
                    if (childWnd.Properties.AriaRole.IsSupported)
                    {
                        if (childWnd.Properties.AriaRole.Value.Contains("alert"))
                        {
                            targetAlertWnd = childWnd;
                            break;
                        }
                    }
                    if (childWnd.Properties.IsDialog.IsSupported)
                    {
                        if (childWnd.Properties.IsDialog.Value)
                        {
                            targetAlertWnd = childWnd;
                            break;
                        }
                    }
                    continue;
                }
                if (targetAlertWnd != null)
                {
                    //找到弹窗的第一个按钮元素，此处可能不是最优解
                    List<ConditionBase> buttonCondition = new List<ConditionBase>
                    {
                        new PropertyCondition(UiCommon.UIAAutomation.PropertyLibrary.Element.ControlType, ControlType.Button)
                    };
                    AndCondition buttonAd = new AndCondition(buttonCondition);
                    var buttonWnd = targetAlertWnd.FindAllDescendants(buttonAd)?.FirstOrDefault();
                    if (buttonWnd != null)
                    {
                        buttonWnd.Click();
                    }
                }
            }
        }

        /// <summary>
        /// 检查长路径文件是否存在
        /// </summary>
        public static bool CheckLongPathFileExists(string path)
        {
            path = Path.GetFullPath(path);
            if (string.IsNullOrEmpty(path))
                return false;
            if (path.StartsWith(@"\\?\"))
                return File.Exists(path);
            string longPath;
            if (path.StartsWith(@"\\"))
            {
                longPath = @"\\?\UNC\" + path.Substring(2);
            }
            else
            {
                longPath = @"\\?\" + path;
            }
            return File.Exists(longPath);
        }

        #endregion
    }
}

