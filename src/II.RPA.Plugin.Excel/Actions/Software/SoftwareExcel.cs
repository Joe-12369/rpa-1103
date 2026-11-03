using II.RPA.WinAutomation.CommonUtility;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Logging;
using II.RPA.Plugin.Excel.Config;
using II.RPA.Plugin.Excel.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace II.RPA.Plugin.Excel.Actions.Software
{
    /// <summary>
    /// 全局对象，应用相关事件方法
    /// </summary>
    public partial class SoftwareExcel : IExcel
    {
        private readonly ILogger logger = Logger.OfContext();
        private static readonly Lazy<SoftwareExcel> instance = new Lazy<SoftwareExcel>(() => new SoftwareExcel());
        /// <summary>
        /// 接口实例
        /// </summary>
        public static IExcel Instance { get { return instance.Value; } }
        /// <summary>
        /// 对象实例
        /// </summary>
        public static SoftwareExcel ContextInstance { get { return instance.Value; } }
        public ActionContext ActionContext { get; set; }
        public readonly Dictionary<string, object> Workbooks = new Dictionary<string, object>();
        private readonly Dictionary<string, Tuple<string, string>> WorkbookPasswords = new Dictionary<string, Tuple<string, string>>();
        private readonly Dictionary<int, object> Applications = new Dictionary<int, object>();
        private object ExcelApplication, WpsApplication, RootApplication;
        private Action<object> selectRuleCallback;
        public Action<object> SelectRuleCallback
        {
            get
            {
                return selectRuleCallback;
            }
            set
            {
                selectRuleCallback = value;
                if (value == null)
                {
                    RuleFunc = null;
                }
            }
        }
        private Tuple<string, object> InteropArgs;
        private object CopiedRange = null;
        private string SourceWorkbook = null;

        /// <summary>
        //流程执行完控制释放
        /// </summary>
        internal bool CloseIfDispose = false;

        private static bool ContainsSameReference(List<object> objects, object candidate)
        {
            foreach (var item in objects)
            {
                if (ReferenceEquals(item, candidate))
                {
                    return true;
                }
            }
            return false;
        }

        private void ReleaseComObjectOnce(List<object> releasedObjects, object comObject, bool finalRelease = false)
        {
            if (comObject == null || !Marshal.IsComObject(comObject) || ContainsSameReference(releasedObjects, comObject))
            {
                return;
            }

            if (finalRelease)
            {
                Marshal.FinalReleaseComObject(comObject);
            }
            else
            {
                Marshal.ReleaseComObject(comObject);
            }

            releasedObjects.Add(comObject);
        }

        internal void ReleaseAllComReferences()
        {
            try
            {
                var releasedObjects = new List<object>();

                ReleaseComObjectOnce(releasedObjects, CopiedRange, true);
                ReleaseComObjectOnce(releasedObjects, InteropArgs?.Item2, true);

                foreach (var wb in Workbooks.Values)
                {
                    ReleaseComObjectOnce(releasedObjects, wb, true);
                }

                ReleaseComObjectOnce(releasedObjects, ExcelApplication, true);
                ReleaseComObjectOnce(releasedObjects, WpsApplication, true);
                ReleaseComObjectOnce(releasedObjects, RootApplication, true);

                foreach (var app in Applications.Values)
                {
                    ReleaseComObjectOnce(releasedObjects, app, true);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning($"ReleaseAllComReferences exception: {ex.Message}");
            }
            finally
            {
                CopiedRange = null;
                InteropArgs = null;
                ExcelApplication = null;
                WpsApplication = null;
                RootApplication = null;
                Workbooks.Clear();
                WorkbookPasswords.Clear();
                Applications.Clear();
            }
        }

        private void SetWorkbookPasswords(string workbook, string password1, string password2)
        {
            if (string.IsNullOrWhiteSpace(password1) && string.IsNullOrWhiteSpace(password2))
            {
                WorkbookPasswords.Remove(workbook);
                return;
            }

            WorkbookPasswords[workbook] = Tuple.Create(password1, password2);
        }

        private string GetWorkbookOpenPassword(string workbook)
        {
            if (WorkbookPasswords.TryGetValue(workbook, out Tuple<string, string> passwords) &&
                !string.IsNullOrWhiteSpace(passwords.Item1))
            {
                return ExcelUtils.DecodeBase64(passwords.Item1);
            }

            return null;
        }

        private bool CheckApplicationAvailable(string software)
        {
            object app = software.Equals("wps") ? WpsApplication : ExcelApplication;
            try
            {
                if (app == null)
                {
                    return false;
                }
                //excel 可以直接用 not null 判定是不是可用，WPS 必须实现设置属性值才能生效，所以用下面这种
                _ = ComLateBinding.GetProp(app, "Visible");
                return true;
            }
            catch
            {
                logger.LogWarning("excel com object not available!");
                app = null;
                return false;
            }
        }
        private object CreateExcelApplication(string software)
        {
            var appType = ExcelUtils.GetCurrentSoftwareType(software);
            if (!CheckApplicationAvailable(appType))
            {
                try
                {
                    InitializeExcelApplication(appType);
                }
                catch (ExcelException ee)
                {
                    throw ee;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex);
                    throw new ExcelException(Localize($"{software} 软件初始化异常！", "SoftwareExcel_1AA06B9C"));
                }
            }
            RootApplication = appType.Equals("wps") ? WpsApplication : ExcelApplication;
            ComLateBinding.SetProp(RootApplication, "DisplayAlerts", ExcelConfigurations.Info.GetSetting<string>("Open", "DisplayAlerts") == "true");
            return RootApplication;

        }
        private void InitializeExcelApplication(string appType)
        {
            if (appType.Equals("wps"))
            {
                SetWPSRegistry();
                var progID = ExcelConfigurations.Info.GlobalSetting.WPSClass ?? "KET.Application";
                var type = Type.GetTypeFromProgID(progID);
                if (type == null)
                {
                    throw new ExcelException("当前计算机未安装 WPS Office，请选择 Microsoft Office 或 不使用软件 选项打开".I18n("SoftwareExcel_569E655C"));
                }
                WpsApplication = Activator.CreateInstance(type);
            }
            else
            {
                var type = Type.GetTypeFromProgID("Excel.Application");
                if (type == null)
                {
                    throw new ExcelException(Localize($"当前计算机未安装 Microsoft Office，请选择 WPS Office 或 不使用软件 选项打开", "SoftwareExcel_9212A0A2"));
                }
                ExcelApplication = Activator.CreateInstance(type);
            }
        }
        public void Application_WorkbookBeforeClose(object Wb, ref bool Cancel)
        {
            Instance.Close(Convert.ToString(ComLateBinding.GetProp(Wb, "FullName")), "是".I18n("SoftwareExcel_73EDDB14"), "否".I18n("SoftwareExcel_FBDDAA81"));
        }
        public void Application_SheetSelectionChange(object Sh, object Target)
        {
            if (RuleFunc != null)
            {
                try
                {
                    using (var com = new ComScope())
                    {
                        com.Track(Sh);
                        com.Track(Target);
                        var rule = RuleFunc(Sh, Target);
                        SelectRuleCallback?.Invoke(rule);
                    }
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "sheet selection callback error!");

                }

            }
        }
        private bool AttachToRunningExcel(string file, out object workbook)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            var absolute = ExcelUtils.IsAbsolutePath(file);
            var isEmpty = string.IsNullOrEmpty(file);
            workbook = null;
            var excel = Process.GetProcessesByName("EXCEL");
            var wps = Process.GetProcessesByName("wps");
            var et = Process.GetProcessesByName("et");//定制版本的wps 只有et 进程
            foreach (var item in excel.Concat(wps).Concat(et))
            {
                try
                {
                    if (isEmpty && Applications.ContainsKey(item.Id))
                    {
                        continue;
                    }
                    if (item.MainWindowHandle == IntPtr.Zero)
                    {
                        // item.Kill();
                        continue;
                    }
                    IntPtr handle = item.MainWindowHandle;

                    using (var com = new ComScope())
                    {
                        object window = com.Track(TryGetInternalExcelWindow(handle));

                        if (window != null)
                        {
                            object excelApp = GetExcelProperty(window, "Application");
                            //item.EnableRaisingEvents = true;
                            //item.Exited += Process_Exited;
                            if (!Applications.ContainsKey(item.Id))
                            {
                                Applications.Add(item.Id, excelApp);
                            }
                            object workbooks = com.Track(GetExcelProperty(excelApp, "Workbooks"));
                            int workbooksCount = Convert.ToInt32(GetExcelProperty(workbooks, "Count"));
                            for (int i = 1; i <= workbooksCount; i++)
                            {
                                object _workbook = GetExcelItem(workbooks, i);
                                if (_workbook == null)
                                {
                                    continue;
                                }
                                string filePath = Convert.ToString(GetExcelProperty(_workbook, "FullName"));
                                if (Workbooks.TryGetValue(filePath, out object existingWorkbook))
                                {
                                    try
                                    {
                                        _ = ComLateBinding.GetProp(existingWorkbook, "Name");
                                    }
                                    catch (Exception)
                                    {
                                        // 重新添加
                                        Workbooks[filePath] = _workbook;
                                    }
                                }
                                else
                                {
                                    //添加进缓存集合
                                    Workbooks[filePath] = _workbook;
                                }
                                if (!isEmpty)
                                {
                                    var matchResult = absolute ? filePath.Equals(file, StringComparison.InvariantCultureIgnoreCase) : Path.GetFileNameWithoutExtension(filePath).Equals(Path.GetFileNameWithoutExtension(file), StringComparison.InvariantCultureIgnoreCase);
                                    if (matchResult)
                                    {
                                        workbook = _workbook;
                                        RootApplication = excelApp;
                                        if (item.ProcessName.Equals("EXCEL"))
                                        {
                                            ExcelApplication = excelApp;
                                        }
                                        else
                                        {
                                            WpsApplication = excelApp;
                                        }
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"attch running excel error,pid {item.Id}");
                    continue;
                }
                finally
                {
                    logger.LogInformation($"AttachToRunningExcel Time:{stopwatch.ElapsedMilliseconds}");
                }

            }
            return false;
        }
        public string GetActiveExcel()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            var excel = Process.GetProcessesByName("EXCEL");
            var wps = Process.GetProcessesByName("wps");
            var et = Process.GetProcessesByName("et");//定制版本的wps 只有et 进程
            Dictionary<string, int> openedWorkbook = new Dictionary<string, int>();
            foreach (var item in excel.Concat(wps).Concat(et))
            {
                try
                {
                    if (item.MainWindowHandle == IntPtr.Zero)
                    {
                        continue;
                    }
                    IntPtr handle = item.MainWindowHandle;

                    using (var com = new ComScope())
                    {
                        object window = com.Track(TryGetInternalExcelWindow(handle));
                        if (window != null)
                        {
                            object excelApp = GetExcelProperty(window, "Application");
                            if (!Applications.ContainsKey(item.Id))
                            {
                                Applications.Add(item.Id, excelApp);
                            }
                            object activeWorkbook = com.Track(GetExcelProperty(excelApp, "ActiveWorkbook"));
                            if (activeWorkbook != null)
                            {
                                string excelPath = Convert.ToString(GetExcelProperty(activeWorkbook, "FullName"));
                                openedWorkbook.Add(excelPath, item.Id);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"attch running excel error,pid {item.Id}");
                    continue;
                }
            }
            logger.LogInformation($"AttachToRunningExcel Time:{stopwatch.ElapsedMilliseconds}");
            IntPtr foregroundWindowHandle = NativeUtils.GetForegroundWindow();
            NativeUtils.GetWindowThreadProcessId((int)foregroundWindowHandle, out int foregroundProcessId);
            foreach (var wb in openedWorkbook)
            {
                if (foregroundProcessId == wb.Value)
                {
                    return wb.Key;
                }
            }
            return openedWorkbook.OrderByDescending(x => x.Value)?.FirstOrDefault().Key;
        }
        private object GetRunningExcelInstance(object excelApp, string file)
        {
            try
            {
                var absolute = ExcelUtils.IsAbsolutePath(file);
                using (var com = new ComScope())
                {
                    object workbooks = com.Track(GetExcelProperty(excelApp, "Workbooks"));
                    int workbooksCount = Convert.ToInt32(GetExcelProperty(workbooks, "Count"));
                    for (int i = 1; i <= workbooksCount; i++)
                    {
                        object _workbook = GetExcelItem(workbooks, i);
                        if (_workbook == null)
                        {
                            continue;
                        }
                        string filePath = Convert.ToString(GetExcelProperty(_workbook, "FullName"));

                        var _absolute = absolute ? filePath.Equals(file, StringComparison.InvariantCultureIgnoreCase) : Path.GetFileNameWithoutExtension(filePath).Equals(Path.GetFileNameWithoutExtension(file), StringComparison.InvariantCultureIgnoreCase);
                        if (_absolute)
                        {
                            return _workbook;
                        }
                        ReleaseComObject(_workbook);
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex);
                return null;
            }

        }

        private void Process_Exited(object sender, EventArgs e)
        {
            try
            {
                using (var excel = sender as Process)
                {
                    logger.LogInformation($"{excel.ProcessName} {excel.Id} exited!");
                    Applications.Remove(excel.Id);
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "excel exited error!");
            }

        }
        public Dictionary<int, object> GetOpenExcelInstances()
        {
            try
            {
                AttachToRunningExcel("", out _);
                return Applications;
            }
            catch (Exception ex)
            {
                ExcelPlugin.logger.LogError(ex);
                return null;
            }

        }
        private bool TryGetAliveWorkbookFromCache(string file, out object workbook)
        {
            workbook = null;
            if (!Workbooks.TryGetValue(file, out object cachedWorkbook))
            {
                return false;
            }

            try
            {
                _ = ComLateBinding.GetProp(cachedWorkbook, "FullName");
                workbook = cachedWorkbook;
                return true;
            }
            catch (Exception ex) when (ComLateBinding.IsRetryableComException(ex))
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"{file} cached workbook RCW is busy, keeping cache.");
                workbook = cachedWorkbook;
                return true;
            }
            catch (Exception ex) when (ex is COMException || ex is InvalidComObjectException)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"{file} cached workbook RCW invalid, reattaching.");
                Workbooks.Remove(file);
                return false;
            }
        }
        private bool TryGetWorkbook(string file, out object workbook)
        {
            try
            {
                if (TryGetAliveWorkbookFromCache(file, out workbook))
                {
                    return true;
                }
                else
                {
                    return AttachToRunningExcel(file, out workbook);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex);

                workbook = null;
                return false;
            }
        }
        private object GetWorkbook(string file)
        {
            try
            {
                if (TryGetAliveWorkbookFromCache(file, out object workbook))
                {
                    return workbook;
                }

                throw new KeyNotFoundException(file);
            }
            catch (COMException ce)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ce, $"{file} has been closed!");
                throw new ExcelException(Localize($"{file} 未打开或已被关闭", "SoftwareExcel_109CEAE9"));
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"{file} not exist!");
                if (AttachToRunningExcel(file, out object existWorkbook))
                {
                    return existWorkbook;
                }
                else
                {
                    throw new ExcelException(Localize($"{file} 未打开或已被关闭", "SoftwareExcel_109CEAE9"));
                }
            }
        }
        private object GetWorksheetByName(string workbook, string sheetName)
        {
            object wb = GetWorkbook(workbook);
            var activeSheetRequest = string.IsNullOrWhiteSpace(sheetName);
            object activeSheet = null;
            object worksheets = null;
            object ws = null;
            bool available = false;
            bool exist = false;
            try
            {
                _ = ComLateBinding.GetProp(wb, "FullName");
                available = true;

                if (activeSheetRequest)
                {
                    activeSheet = ComLateBinding.GetProp(wb, "ActiveSheet");
                    sheetName = Convert.ToString(ComLateBinding.GetProp(activeSheet, "Name"));
                    exist = true;
                }

                worksheets = activeSheetRequest ? ComLateBinding.GetProp(wb, "Sheets") : ComLateBinding.GetProp(wb, "Worksheets");
                ws = GetExcelItemByName(worksheets, sheetName);
                exist = true;

                if (!activeSheetRequest)
                {
                    ComLateBinding.Invoke(ws, "Activate");
                }

                return ws;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex);
                if (!available)
                {
                    throw new ExcelException(Localize($"{workbook} 不可用，请确认是否打开", "SoftwareExcel_C8E68B28"));
                }
                else if (!exist)
                {
                    throw new ExcelException(Localize($"{workbook}中名称为 {sheetName} 工作表不存在", "SoftwareExcel_1287C7D5"));
                }
                else
                {
                    throw new ExcelException(ex.Message);
                }

            }
            finally
            {
                ReleaseComObject(worksheets);
                if (!ReferenceEquals(activeSheet, ws))
                {
                    ReleaseComObject(activeSheet);
                }
            }
        }
        private object GetWorksheetByIndex(string workbook, int index)
        {
            try
            {
                using (var com = new ComScope())
                {
                    object worksheets = com.Track(ComLateBinding.GetProp(GetWorkbook(workbook), "Worksheets"));
                    return GetExcelItem(worksheets, index);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex);
                throw new ExcelException(Localize($"工作簿中索引为 {index} 位置工作表不存在", "SoftwareExcel_ABDB6030"));
            }

        }
        private bool Exist(string workbook, string sheetName)
        {
            try
            {
                using (var com = new ComScope())
                {
                    object worksheets = com.Track(ComLateBinding.GetProp(GetWorkbook(workbook), "Worksheets"));
                    object worksheet = com.Track(GetExcelItemByName(worksheets, sheetName));
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

    }
}

