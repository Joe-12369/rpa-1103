using II.RPA.WinAutomation.CommonUtility;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using DevExpress.XtraPrinting;
using II.RPA.Plugin.Excel.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace II.RPA.Plugin.Excel.Actions.Software
{
    /// <summary>
    /// 使用软件自动化
    /// 设计器.文档操作类组件
    /// </summary>
    public partial class SoftwareExcel : IExcel
    {

        /// <summary>
        /// 新建Excel
        /// </summary>       
        /// <param name="path">文件路径</param>  
        /// <param name="name">文件名称</param>  
        /// <param name="software">使用软件["默认","MicrosoftExcel","WPS"]</param>  
        /// <param name="visible">是否可视化["是","否"]</param>  
        /// <param name="maximize">是否最大化["是","否"]</param>  
        /// <param name="show">是否显示软件["是","否"]</param>  
        void IExcel.New(string path, string name, string software, string visible, string maximize, string show)
        {
            var file = Instance.ActionContext["__IsShareFile"] ? $"{path.TrimEnd('\\')}\\{name}" : ExcelUtils.GetAbsolutePath(path, name);
            CreateWorkbook(file, software, visible, maximize, show);
        }

        private void CreateWorkbook(string file, string software, string visible, string maximize, string show)
        {
            if (string.IsNullOrEmpty(Path.GetExtension(file))) file += ".xlsx";

            var dir = Path.GetDirectoryName(file);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            if (File.Exists(file))
            {
                //add by wenyi :ZOCO-23882
                if (ActionContext.ActionParams.TryGetValue("override", out object _override) && Convert.ToInt32(_override) == 1)
                {
                    try
                    {
                        FileSystem.DeleteFile(
                            file,
                            UIOption.OnlyErrorDialogs,
                            RecycleOption.SendToRecycleBin
                        );
                    }
                    catch (Exception ex)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "文件无法被移除");
                        //文件无法被移除
                        throw new ExcelException(Localize($"{file} 已存在", "Workbook_AAC619A3"));
                    }
                    // 极端情况下 Shell 未删除成功
                    if (File.Exists(file))
                    {
                        throw new ExcelException(Localize($"{file} 已存在", "Workbook_AAC619A3"));
                    }
                }
                else
                {
                    throw new ExcelException(Localize($"{file} 已存在", "Workbook_AAC619A3"));
                }
            }

            using (var com = new ComScope())
            {
                var app = CreateExcelApplication(software);
                var _visible = ActionContext.GetActionArgs("visible");
                bool hidden;
                if (string.IsNullOrWhiteSpace(_visible))
                {
                    hidden = show.Equals("否");
                }
                else
                {
                    hidden = _visible == "false";
                }
                if (!hidden)
                {
                    if (maximize.Equals("是"))
                    {
                        SetExcelProperty(app, "WindowState", -4137);
                    }
                }

                object workbooks = com.Track(ComLateBinding.GetProp(app, "Workbooks"));
                object wb = ComLateBinding.Invoke(workbooks, "Add"); // 由于要缓存wb到Workbooks，不要对其进行 Track 释放
                ComLateBinding.SetProp(app, "Visible", !hidden);
                ComLateBinding.Invoke(wb, "SaveAs", file);
                Workbooks[file] = wb;
                BindBeforeCloseEvent(wb, file);
                ContextInstance.ActionContext.AddResult("workbook", file);
            }
        }

        void IExcel.NewV6(string path, string name, string software, string visible, string maximize, string show)
        {
            var file = ExcelUtils.GetAbsolutePathV2(path, name);
            CreateWorkbook(file, software, visible, maximize, show);
        }

        /// <summary>
        /// 打开Excel
        /// </summary>      
        /// <param name="file">文件路径</param>  
        /// <param name="software">使用软件["默认","MicrosoftExcel","WPS"]</param>  
        /// <param name="visible">是否可视化["是","否"]</param>  
        /// <param name="maximize">是否最大化["是","否"]</param>  
        /// <param name="password1">打开密码</param>  
        /// <param name="password2">编辑密码</param>  
        /// <param name="show">是否显示软件["是","否"]</param>  
        void IExcel.Open(string file, string software, string visible, string maximize, string password1, string password2, string show)
        {
            file = Instance.ActionContext["__IsShareFile"] ? file : ExcelUtils.GetAbsolutePath(null, file);
            OpenWorkbookFromPath(file, software, visible, maximize, password1, password2, show);
        }

        private void OpenWorkbookFromPath(string file, string software, string visible, string maximize, string password1, string password2, string show)
        {
            if (string.IsNullOrEmpty(Path.GetExtension(file))) file += ".xlsx";
            if (!File.Exists(file))
            {
                throw new ExcelException(Localize($"{file} 不存在", "Workbook_EAB43890"));
            }
            if (!TryGetWorkbook(file, out object wb))
            {
                using (var com = new ComScope())
                {
                    var _visible = ActionContext.GetActionArgs("visible");
                    bool hidden;
                    if (string.IsNullOrWhiteSpace(_visible))
                    {
                        hidden = show.Equals("否");
                    }
                    else
                    {
                        hidden = _visible == "false";
                    }
                    var app = CreateExcelApplication(software);
                    Instance.ActionContext.ActionParams.TryGetValue("autoUpdateLink", out object upateLink);
                    bool askToUpdateLinks = upateLink != null && Convert.ToInt32(upateLink) == 0;
                    ComLateBinding.SetProp(app, "AskToUpdateLinks", askToUpdateLinks);
                    object workbooks = com.Track(ComLateBinding.GetProp(app, "Workbooks"));

                    wb = OpenWorkbook(workbooks, file, password1, password2, upateLink);

                    // Normalize the cached workbook RCW to the instance returned from the
                    // live Workbooks collection. This matches the SaveAs path and avoids
                    // keeping the transient RCW returned directly by Workbooks.Open.
                    var openedWorkbook = GetRunningExcelInstance(app, file);
                    if (openedWorkbook != null)
                    {
                        wb = openedWorkbook;
                    }

                    SetExcelProperty(app, "Visible", !hidden);
                    if (InteropArgs != null)
                    {
                        SetExcelProperty(app, InteropArgs.Item1, InteropArgs.Item2);
                        InteropArgs = null;
                    }
                    if (!hidden)
                    {
                        SetWorkbookWindowState(wb, app, maximize.Equals("是"));
                    }
                    Workbooks[file] = wb;
                    SetWorkbookPasswords(file, password1, password2);
                    BindBeforeCloseEvent(wb, file);
                }
            }
            if (!string.IsNullOrWhiteSpace(password1) || !string.IsNullOrWhiteSpace(password2))
            {
                SetWorkbookPasswords(file, password1, password2);
            }
            ContextInstance.ActionContext.AddResult("workbook", file);
            Instance.ActionContext["__workbook"] = wb;
        }

        void IExcel.OpenV5(string file, string software, string visible, string maximize, string password1, string password2, string show)
        {
            file = ExcelUtils.GetAbsolutePathV2(null, file);
            OpenWorkbookFromPath(file, software, visible, maximize, password1, password2, show);
        }

        private static object OpenWorkbook(object workbooks, string file, string password1, string password2, object updateLinks)
        {
            string openPassword = string.IsNullOrWhiteSpace(password1) ? null : ExcelUtils.DecodeBase64(password1);
            string writePassword = string.IsNullOrWhiteSpace(password2) ? null : ExcelUtils.DecodeBase64(password2);
            object missing = Type.Missing;

            // Workbooks.Open named-argument dispatch can fail on some Excel COM type libraries.
            object[] args =
            {
                file,                              // Filename
                updateLinks ?? missing,            // UpdateLinks
                missing,                           // ReadOnly
                missing,                           // Format
                openPassword ?? missing,           // Password
                writePassword ?? missing,          // WriteResPassword
                missing,                           // IgnoreReadOnlyRecommended
                missing,                           // Origin
                missing,                           // Delimiter
                true                               // Editable
            };

            return ComLateBinding.Invoke(workbooks, "Open", args);
        }

        /// <summary>
        /// 设置当前工作簿所在的顶层窗口状态。
        /// WPS 的 Application.WindowState 不一定作用于实际的主窗口，
        /// 因此优先从 Workbook.Windows[1].Hwnd 定位工作簿窗口，
        /// 再沿父子关系找到顶层窗口并通过 Win32 设置状态。
        /// </summary>
        private void SetWorkbookWindowState(object workbook, object app, bool maximize)
        {
            const int xlMaximized = -4137;
            const uint gaRoot = 2;
            const uint windowPositionFlags = (uint)(NativeUtils.SWP_NOMOVE |
                                                    NativeUtils.SWP_NOSIZE |
                                                    NativeUtils.SWP_SHOWWINDOW);

            IntPtr windowHandle = IntPtr.Zero;
            using (var com = new ComScope())
            {
                try
                {
                    object windows = com.Track(ComLateBinding.GetProp(workbook, "Windows"));
                    object workbookWindow = com.Track(ComLateBinding.GetItem(windows, 1));

                    // WPS 是多窗口结构：Workbook.Window 是内部文档子窗口，应用主窗口在其外层。
                    // 内部工作簿窗口始终保持最大化；maximize 仅决定外层主窗口是否最小化。
                    // 这样用户手动恢复 WPS 主窗口时，内部工作表仍会填满客户区域。
                    SetExcelProperty(workbookWindow, "WindowState", xlMaximized);
                    windowHandle = new IntPtr(ComLateBinding.ToInt(ComLateBinding.GetProp(workbookWindow, "Hwnd")));
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "get workbook window handle failed, fallback to application window");
                }
            }

            if (windowHandle == IntPtr.Zero)
            {
                try
                {
                    windowHandle = new IntPtr(ComLateBinding.ToInt(ComLateBinding.GetProp(app, "Hwnd")));
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "get application window handle failed");
                }
            }

            if (windowHandle == IntPtr.Zero)
            {
                logger.LogWarning("cannot set workbook window state because its window handle was not found");
                return;
            }

            // WPS 可能接受 COM WindowState 设置却不更新内部子窗口，
            // 再对 Workbook.Window 句柄发送一次最大化命令作为保障。
            NativeUtils.ShowWindowAsync(windowHandle, (int)NativeUtils.ShowWindowCmds.SW_MAXIMIZE);
            new ExcelWindow(windowHandle).Maximize();

            IntPtr rootWindow = NativeUtils.GetAncestor(windowHandle, gaRoot);
            if (rootWindow != IntPtr.Zero)
            {
                windowHandle = rootWindow;
            }

            if (maximize)
            {
                // 这里的“置顶”是将窗口切换到当前前台，不是设置为“始终置顶”。
                // 先取消可能遗留的 TOPMOST 状态，保证后续打开的浏览器等窗口可以正常覆盖 Excel/WPS。
                NativeUtils.SetWindowPos(
                    windowHandle,
                    new IntPtr(NativeUtils.HWND_NOTOPMOST),
                    0,
                    0,
                    0,
                    0,
                    windowPositionFlags);
                NativeUtils.ShowWindowAsync(windowHandle, (int)NativeUtils.ShowWindowCmds.SW_MAXIMIZE);
                new ExcelWindow(windowHandle).BringToFront();
            }
            else
            {
                NativeUtils.SetWindowPos(
                    windowHandle,
                    new IntPtr(NativeUtils.HWND_NOTOPMOST),
                    0,
                    0,
                    0,
                    0,
                    windowPositionFlags);
                NativeUtils.ShowWindowAsync(windowHandle, (int)NativeUtils.ShowWindowCmds.SW_MINIMIZE);
            }
        }

        /// <summary>
        /// 获取已打开Excel
        /// </summary>       
        /// <param name="file">文件名称</param>  
        void IExcel.Load(string file)
        {
            string workbook;
            if (ActionContext.ActionParams.TryGetValue("option", out object option) && Convert.ToInt32(option) == 0)
            {
                try
                {

                    var activeWorkbook = GetActiveExcel();
                    if (string.IsNullOrEmpty(activeWorkbook))
                    {
                        throw new ExcelException(Localize($"没有激活的Excel文件，请使用【打开Excel】组件打开", "Workbook_161536E"));
                    }
                    if (TryGetWorkbook(activeWorkbook, out object wb))
                    {
                        BindBeforeCloseEvent(wb, activeWorkbook);
                    }
                    ContextInstance.ActionContext?.AddResult("workbook", activeWorkbook);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex);
                    throw new ExcelException(Localize($"没有激活的Excel文件，请使用【打开Excel】组件打开", "Workbook_161536E"));
                }

            }
            else
            {
                var openedByExcel = AttachToRunningExcel(file, out object excelWorkbook);
                if (openedByExcel)
                {
                    workbook = Convert.ToString(ComLateBinding.GetProp(excelWorkbook, "FullName"));
                    ContextInstance.ActionContext?.AddResult("workbook", workbook);
                    Workbooks[workbook] = excelWorkbook;
                    BindBeforeCloseEvent(excelWorkbook, workbook);
                }
                else
                {
                    throw new ExcelException(Localize($"{file} 未打开，请使用【打开Excel】组件打开", "Workbook_84F856F4"));
                }
            }
        }

        /// <summary>
        /// 保存Excel
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="close">保存后关闭["是","否"]</param>  
        void IExcel.Save(string workbook, string close)
        {
            if (Workbooks.TryGetValue(workbook, out object wb))
            {
                ComLateBinding.Invoke(wb, "Save");
                if (close.Equals("是"))
                    Instance.Close(workbook, "否".I18n("Workbook_FBDDAA81"), "否".I18n("Workbook_FBDDAA81"));
            }
        }

        void SaveAs(string workbook, string file, string close, string cover)
        {
            if (File.Exists(file))
            {
                if (cover.Equals("是"))
                    File.Delete(file);
                else
                    file = ExcelUtils.GetDuplicateFileName(file);
            }
            using (var com = new ComScope())
            {
                object wb = GetWorkbook(workbook);
                if (file.EndsWith(".xls"))
                {
                    ComLateBinding.Invoke(wb, "SaveAs", file, 56);
                }
                else if (file.EndsWith(".html") || file.EndsWith(".htm"))
                {
                    ComLateBinding.Invoke(wb, "SaveAs", file, 44);
                }
                else
                {
                    ComLateBinding.Invoke(wb, "SaveAs", file);
                }

                object app = com.Track(ComLateBinding.GetProp(wb, "Application"));
                object saveAsWorkbook = GetRunningExcelInstance(app, file);
                if (saveAsWorkbook == null)
                {
                    throw new ExcelException("另存为失败！".I18n("Workbook_2ED2DE16"));
                }
                Tuple<string, string> workbookPasswords = null;
                WorkbookPasswords.TryGetValue(workbook, out workbookPasswords);
                Workbooks.Remove(workbook);
                WorkbookPasswords.Remove(workbook);
                ExcelPlugin.Instance.ExcelSet.Remove(workbook);
                Workbooks[file] = saveAsWorkbook;
                if (workbookPasswords != null)
                {
                    WorkbookPasswords[file] = workbookPasswords;
                }
                ContextInstance.ActionContext.AddResult("workbook", file);
                ComLateBinding.Invoke(Workbooks[file], "Save");
            }
            if (close.Equals("是"))
            {
                Instance.Close(file, "否".I18n("Workbook_FBDDAA81"), "否");
            }
        }

        /// <summary>
        /// 另存为Excel
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="path">另存为路径</param>  
        /// <param name="name">文件名称</param>  
        /// <param name="close">保存后关闭["是","否"]</param>  
        /// <param name="cover">覆盖相同文件["是","否"]</param>  
        void IExcel.SaveAs(string workbook, string path, string name, string close, string cover)
        {
            var file = Instance.ActionContext["__IsShareFile"] ? $"{path.TrimEnd('\\')}\\{name}" : ExcelUtils.GetAbsolutePath(path, name);    
            var dir = Path.GetDirectoryName(file);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            SaveAs(workbook, file, close, cover);
        }

        void IExcel.SaveAsV2(string workbook, string path, string name, string close, string cover)
        {
            var file = ExcelUtils.GetAbsolutePathV2(path, name);
            var dir = Path.GetDirectoryName(file);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            SaveAs(workbook, file, close, cover);
        }

        /// <summary>
        /// 关闭Excel
        /// </summary>
        /// <param name="workbook">文件对象</param>            
        /// <param name="save">是否保存</param>
        /// <param name="close">关闭软件["是","否"]，第一次需求表示是否关闭软件，第二次需求变更为，否：关闭当前，是：关闭所有</param>  
        void IExcel.Close(string workbook, string save, string close)
        {
            internalClosing = true;
            try
            {
                if (Workbooks.TryGetValue(workbook, out object wb))
                {
                    using (var com = new ComScope())
                    {
                        object wbApp = com.Track(ComLateBinding.GetProp(wb, "Application"));
                        //流程执行完 不关闭使用软件且显示窗口的实例
                        if (CloseIfDispose && ComLateBinding.ToBool(ComLateBinding.GetProp(wbApp, "Visible"))) return;
                        if (save.Equals("是"))
                        {
                            ComLateBinding.Invoke(wb, "Save");
                        }
                        object _excelApp = com.Track(ComLateBinding.GetProp(wb, "Application"));
                        NativeUtils.GetWindowThreadProcessId(ComLateBinding.ToInt(ComLateBinding.GetProp(_excelApp, "Hwnd")), out int pid);
                        if (close.Equals("是"))
                        {
                            //全部              

                            for (int i = 0; i < Workbooks.Count; i++)
                            {
                                var w = Workbooks.ElementAt(i);
                                try
                                {
                                    if (Marshal.IsComObject(w.Value) && w.Value != null)
                                    {
                                        ComLateBinding.Invoke(w.Value, "Close", false);
                                        // ReleaseComObject(w.Value);
                                    }
                                }
                                catch (System.Exception ex)
                                {
                                    logger.LogError(ex);
                                    continue;
                                }
                                finally
                                {
                                    ExcelPlugin.Instance.Remove(w.Key);
                                    // 这里如果直接 Clear 会在循环中引发迭代器异常，这里保持原来的逻辑但可能存在隐患，暂不需重构时引发业务改变
                                    Workbooks.Clear();
                                    WorkbookPasswords.Clear();
                                }
                            }

                            var processes = Process.GetProcessesByName(Process.GetProcessById(pid).ProcessName);
                            for (int p = 0; p < processes.Length; p++)
                            {
                                try
                                {
                                    var _process = processes[p];
                                    var parent = ProcessUtils.GetParentProcess(_process.Id);
                                    _process.Kill();
                                    _process.WaitForExit(500);//等待退出
                                    if (parent != null && parent.ProcessName == "wps")
                                    {
                                        parent.Kill();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    logger.LogError(ex);
                                    continue;
                                }

                            }
                        }
                        else
                        {
                            //当前              
                            ExcelUtils.TryFuncIgnoreException(() =>
                            {
                                ComLateBinding.Invoke(wb, "Close", false);
                                object _workbooks = com.Track(ComLateBinding.GetProp(_excelApp, "Workbooks"));
                                int wbsCount = ComLateBinding.ToInt(ComLateBinding.GetProp(_workbooks, "Count"));
                                if (wbsCount == 0 && pid != 0)
                                {
                                    var process = Process.GetProcessById(pid);
                                    var parent = ProcessUtils.GetParentProcess(pid);
                                    process.Kill();
                                    process.WaitForExit(500);//等待退出
                                    if (parent != null && parent.ProcessName == "wps")
                                    {
                                        //有时父进程自动退出，有时不退出。已经退出再Kill 会抛异常
                                        parent.Kill();

                                    }
                                }
                            });
                            Workbooks.Remove(workbook);
                            WorkbookPasswords.Remove(workbook);
                            ExcelPlugin.Instance.Remove(workbook);
                        }
                    }
                }
            }
            finally
            {
                internalClosing = false;
            }
        }
        /// <summary>
        /// 关闭组件V2
        /// </summary>
        /// <param name="closeType">关闭类型（文件、软件）</param>
        /// <param name="workbook">文件</param>
        /// <param name="closeAction">关闭前操作（保存，不保存，另存为）</param>
        /// <param name="filePath">另存为路径</param>
        /// <param name="cover">是否覆盖</param>      
        /// <param name="software">关闭的软件</param>
        /// <param name="kill">杀掉进程</param>
        void IExcel.CloseV2(int closeType, string workbook, int closeAction, string filePath, int cover, int software, int kill)
        {
            if (closeAction == 0)
            {
                //保存
                Instance.Close(workbook, "是", "否");
            }
            else if (closeAction == 1)
            {
                //不保存
                Instance.Close(workbook, "否", "否");
            }
            else
            {
                //另存为             
                SaveAs(workbook, filePath, "是", cover == 1 ? "是" : "否");
            }
        }

        void IExcel.CloseV3(int closeType, string workbook, int closeAction, string filePath, int cover, int software, int kill)
        {
            if (closeAction == 0)
            {
                Instance.Close(workbook, "是", "否");
            }
            else if (closeAction == 1)
            {
                Instance.Close(workbook, "否", "否");
            }
            else
            {
                var file = ExcelUtils.GetAbsolutePathV2(null, filePath);
                var dir = Path.GetDirectoryName(file);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                SaveAs(workbook, file, "是", cover == 1 ? "是" : "否");
            }
        }


        /// <summary>
        /// 导出Excel为PDF
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="option">Sheet名称["全部","激活页"]</param>  
        /// <param name="path">导出路径</param>  
        /// <param name="name">文件名称</param>  
        /// <param name="cover">覆盖相同文件["是","否"]</param>  
        void IExcel.Export(string workbook, string option, string path, string name, string cover)
        {
            if (!name.EndsWith(".pdf"))
            {
                name += ".pdf";
            }
            var file = ExcelUtils.GetAbsolutePath(path, name);
            if (File.Exists(file))
            {
                if (cover.Equals("否"))
                    file = ExcelUtils.GetDuplicateFileName(file);
                else
                    File.Delete(file);
            }

            object excelWorkbook = GetWorkbook(workbook);
            const int xlTypePDF = 0;
            const int xlQualityStandard = 0;
            using (var com = new ComScope())
            {
                if (option.Equals("全部"))
                {
                    // 导出整个工作簿
                    ComLateBinding.Invoke(excelWorkbook, "ExportAsFixedFormat", xlTypePDF, file, xlQualityStandard, true, false, Type.Missing, Type.Missing, false);
                }
                else
                {
                    // 导出当前活动的工作表
                    object activeSheet = com.Track(ComLateBinding.GetProp(excelWorkbook, "ActiveSheet"));
                    ComLateBinding.Invoke(activeSheet, "ExportAsFixedFormat", xlTypePDF, file, xlQualityStandard, true, false, Type.Missing, Type.Missing, false);
                }
            }
        }

        void IExcel.ExportV2(string workbook, string option, string path, string name, string cover)
        {
            if (!name.EndsWith(".pdf"))
            {
                name += ".pdf";
            }
            var file = ExcelUtils.GetAbsolutePathV2(path, name);
            var dir = Path.GetDirectoryName(file);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            if (File.Exists(file))
            {
                if (cover.Equals("否"))
                    file = ExcelUtils.GetDuplicateFileName(file);
                else
                    File.Delete(file);
            }
            using (var com = new ComScope())
            {
                object excelWorkbook = GetWorkbook(workbook);
                int xlTypePDF = 0;
                int xlQualityStandard = 0;
                if (option.Equals("全部"))
                {
                    ComLateBinding.Invoke(excelWorkbook, "ExportAsFixedFormat", xlTypePDF, file, xlQualityStandard, true, false, Type.Missing, Type.Missing, false);
                }
                else
                {
                    object activeSheet = com.Track(ComLateBinding.GetProp(excelWorkbook, "ActiveSheet"));
                    ComLateBinding.Invoke(activeSheet, "ExportAsFixedFormat", xlTypePDF, file, xlQualityStandard, true, false, Type.Missing, Type.Missing, false);
                }
            }
        }

        /// <summary>
        /// 加密Excel
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="password1">打开密码</param>  
        /// <param name="password2">编辑密码</param>  

        void IExcel.Encrypt(string workbook, string password1, string password2)
        {
            object wb = GetWorkbook(workbook);
            if (!string.IsNullOrEmpty(password1))
            {
                ComLateBinding.SetProp(wb, "Password", ExcelUtils.DecodeBase64(password1));
            }
            if (!string.IsNullOrEmpty(password2))
            {
                ComLateBinding.Invoke(wb, "SetPasswordEncryptionOptions", "Microsoft RSA SChannel Cryptographic Provider", "RC4", 56, true);
                ComLateBinding.SetProp(wb, "WritePassword", ExcelUtils.DecodeBase64(password2));
            }
        }

        /// <summary>
        /// 获取Excel路径
        /// </summary>      
        /// <param name="workbook">文件对象</param>  
        void IExcel.GetPath(string workbook)
        {
            ContextInstance.ActionContext.AddResult("strPath", Convert.ToString(ComLateBinding.GetProp(GetWorkbook(workbook), "FullName")));
        }

        public delegate void WorkbookBeforeCloseDelegate(ref bool Cancel);
        private readonly HashSet<string> boundWorkbooks = new HashSet<string>();
        internal bool internalClosing = false;

        private void BindBeforeCloseEvent(object wb, string file)
        {
            try
            {
                if (boundWorkbooks.Contains(file)) return;
                boundWorkbooks.Add(file);

                WorkbookBeforeCloseDelegate handler = (ref bool cancel) =>
                {
                    try
                    {
                        if (internalClosing) return;

                        if (ContextInstance.Workbooks.TryGetValue(file, out object existWb))
                        {
                            ContextInstance.Workbooks.Remove(file);
                            ExcelPlugin.Instance.Remove(file);
                            ContextInstance.ReleaseComObject(existWb);
                        }
                        boundWorkbooks.Remove(file);

                        if (ContextInstance.Workbooks.Count == 0)
                        {
                            var releasedObjects = new List<object>();

                            ContextInstance.ReleaseComObjectOnce(releasedObjects, ContextInstance.ExcelApplication);
                            ContextInstance.ReleaseComObjectOnce(releasedObjects, ContextInstance.WpsApplication);
                            ContextInstance.ReleaseComObjectOnce(releasedObjects, ContextInstance.RootApplication);
                            ContextInstance.ExcelApplication = null;
                            ContextInstance.WpsApplication = null;
                            ContextInstance.RootApplication = null;
                            foreach (var app in ContextInstance.Applications.Values)
                            {
                                ContextInstance.ReleaseComObjectOnce(releasedObjects, app);
                            }
                            ContextInstance.Applications.Clear();
                        }

                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();
                    }
                    catch (Exception ex)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "Remove workbook error when triggered externally.");
                    }
                };
                var beforeCloseEvent = wb.GetType().GetEvent("BeforeClose");
                if (beforeCloseEvent != null)
                {
                    Delegate typedHandler = Delegate.CreateDelegate(beforeCloseEvent.EventHandlerType, handler.Target, handler.Method);
                    beforeCloseEvent.AddEventHandler(wb, typedHandler);
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "Failed to bind BeforeClose event.");
            }
        }
    }
}

