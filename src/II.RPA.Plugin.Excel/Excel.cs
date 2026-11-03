using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Excel.Actions.NonSoftware;
using II.RPA.Plugin.Excel.Actions.Software;
using II.RPA.Plugin.Excel.Filter;
using II.RPA.Plugin.Excel.Utils;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Excel
{
    public partial class ExcelPlugin
    {
        private ExcelPlugin()
        {
            RegisterFilters();
        }

        private static readonly Lazy<ExcelPlugin> instance = new Lazy<ExcelPlugin>(() => new ExcelPlugin());
        public static ExcelPlugin Instance => instance.Value;

        //存放打开 Excel 的实例
        public Dictionary<string, IExcel> ExcelSet = new Dictionary<string, IExcel>();
        //超时对象存储
        public Dictionary<string, object> TimeoutCache = new Dictionary<string, object>();

        public static ILogger logger = Logger.OfContext();

        /// <summary>
        /// 请求上下文
        /// </summary>
        public ActionContext RequestContext { get; private set; }


        /// <summary>
        /// 这些组件返回原始字符串，通常是文件地址或路径
        /// </summary>
        private string[] RawStringMethod = {
            "Filter",
            "CopyRange",
            "ReadColumn",
            "ReadRange",
            "ReadRangeV2",
            "ReadRow"
        };

        //是否超时还原
        public bool IsRestore = false;

        #region 注册Filter
        List<ActionFilter> Filters = new List<ActionFilter>();
        IEnumerable<ActionFilter> BeforeExecuteFilter
        {
            get
            {
                return Filters.Where(f => f.FilterType == FilterType.Executing).OrderByDescending(f => f.Order);
            }
        }
        IEnumerable<ActionFilter> ExecutedFilter
        {
            get
            {
                return Filters.Where(f => f.FilterType == FilterType.Executed).OrderByDescending(f => f.Order);
            }
        }
        IEnumerable<ActionFilter> ResultFilter
        {
            get
            {
                return Filters.Where(f => f.FilterType == FilterType.Result).OrderByDescending(f => f.Order);
            }
        }
        IEnumerable<ActionFilter> ExceptionFilter
        {
            get
            {
                return Filters.Where(f => f.FilterType == FilterType.Exception).OrderByDescending(f => f.Order);
            }
        }
        private void RegisterFilters()
        {
            //order大 先执行            
            Filters.Add(new Filter.Executing.InsertActionFilter(FilterType.Executing));
            Filters.Add(new Filter.Executing.MacroActionFilter(FilterType.Executing));
            Filters.Add(new Filter.Executing.NegativeColumnRowFilter(FilterType.Executing));
            Filters.Add(new Filter.Executing.WriteContentFilter(FilterType.Executing));
            Filters.Add(new Filter.Executing.ReadContentFilter(FilterType.Executing, 10));
            Filters.Add(new Filter.Executing.TimoutFilter(FilterType.Executing, 20));
            Filters.Add(new Filter.Executing.ExcelInstanceFilter(FilterType.Executing, 30));
            Filters.Add(new Filter.Executing.ActionParamsFilter(FilterType.Executing, 40));
            Filters.Add(new Filter.Executing.DisposeFilter(FilterType.Executing, 50));
            Filters.Add(new Filter.Executing.SaveInstanceFilter(FilterType.Executing, 60));
            Filters.Add(new Filter.Executing.CloseFilter(FilterType.Executing, 70));

            Filters.Add(new Filter.Executed.AddinFilter(FilterType.Executed));
            Filters.Add(new Filter.Executed.SaveActionFilter(FilterType.Executed));
            Filters.Add(new Filter.Executed.SaveWorkbookFilter(FilterType.Executed));
            Filters.Add(new Filter.Executed.TimeoutFilter(FilterType.Executed));

            Filters.Add(new Filter.Exception.ExceptionFilter(FilterType.Exception));
        }
        #endregion

        #region 实例对象获取
        public IExcel GetExcelInstance(ActionContext context)
        {
            if (context.SingleRun == true && context.ActionParams.TryGetValue("workbook", out dynamic excelPath))
            {
                string activeFile = SoftwareExcel.ContextInstance.GetActiveExcel();
                context.ActionParams["workbook"] = activeFile;
                return SoftwareExcel.Instance;
            }

            if (context["workbook"] == null)
            {
                throw new ExcelException("Excel 文件不能为空".I18n("Excel_6315DF13"));
            }
            context.ActionParams["workbook"] = System.IO.Path.GetFullPath(System.IO.Path.GetFullPath(context.ActionParams["workbook"]));
            if (ExcelSet.ContainsKey(context.ActionParams["workbook"]))
            {
                return ExcelSet[context.ActionParams["workbook"]];
            }
            else
            {
                #region 超时对象还原
                var cacheFile = Path.Combine(ExcelUtils.GetPackageDirectory(), "excel_timeout.cache");
                logger.LogInformation($"Excel 超时还原 - {GlobleConfigurations.IsReStart} 缓存文件地址： {cacheFile}");
                if (GlobleConfigurations.IsReStart && !IsRestore)
                {
                    //Debugger.Launch();
                    if (File.Exists(cacheFile))
                    {
                        var _cacheObj = File.ReadAllText(cacheFile);
                        logger.LogInformation($"Excel 超时还原 - 文件对象 {_cacheObj}");
                        var cacheDic = JsonHelper.JsonDesByNewtonJson<Dictionary<string, Dictionary<string, dynamic>>>(_cacheObj);

                        File.Delete(cacheFile);
                        IsRestore = true;
                        TimeoutCache.Clear();

                        foreach (var item in cacheDic)
                        {
                            try
                            {
                                Dictionary<string, dynamic> instanceParams = new Dictionary<string, dynamic>();
                                if (item.Value.ContainsKey("path"))
                                {
                                    instanceParams["file"] = Path.Combine(item.Value["path"], item.Value["name"]);
                                }
                                else
                                {
                                    instanceParams["file"] = item.Value["file"];
                                }

                                instanceParams["password1"] = item.Value.ContainsKey("password1") ? item.Value["password1"] : "";
                                instanceParams["password2"] = item.Value.ContainsKey("password2") ? item.Value["password2"] : "";
                                instanceParams["show"] = item.Value.ContainsKey("show") ? item.Value["show"] : "是";
                                instanceParams["visible"] = item.Value.ContainsKey("visible") ? item.Value["visible"] : "是";
                                instanceParams["maximize"] = item.Value.ContainsKey("maximize") ? item.Value["maximize"] : "是";
                                instanceParams["share"] = item.Value.ContainsKey("share") ? item.Value["share"] : 0;
                                instanceParams["autoUpdateLink"] = item.Value.ContainsKey("share") ? item.Value["share"] : 0;
                                var _context = new ActionContext();
                                _context.ActionName = "Open";
                                _context.ExcelInstance = instanceParams["visible"] == "是" ? SoftwareExcel.Instance : NonSoftware.Instance;
                                _context.ActionParams = instanceParams;
                                ExecuteWithTimeout(_context);
                                Filters.First(f => f.GetType().Name.Contains("SaveWorkbookFilter"))?.OnExecution(_context);
                                logger.LogInformation($"超时对象还原成功 {item.Key}");
                            }
                            catch (Exception ex)
                            {
                                logger.LogInformation($"超时对象还原失败 {item.Key}:{ex.Message}");
                                logger.LogError(ex.StackTrace);
                            }

                        }
                    }

                    if (ExcelSet.ContainsKey(context.ActionParams["workbook"]))
                    {
                        return ExcelSet[context.ActionParams["workbook"]];
                    }
                    else
                    {
                        logger.LogWarning($"还原后获取对象为空：{context.ActionParams["workbook"]}");
                    }
                }
                #endregion

                throw new ExcelException(Localize($"{context.ActionParams["workbook"]}文件未打开，请使用【打开Excel】组件打开", "Excel_56A7AD24"));
            }
        }
        #endregion

        #region 移除实例对象
        public void Clear()
        {
            ExcelSet.Clear();
        }
        public void Remove(string workbook)
        {
            if (ExcelSet.TryGetValue(workbook, out IExcel instance))
            {
                ExcelSet.Remove(workbook);
            }
        }
        #endregion

        //组件执行
        internal string ExecuteWithTimeout(ActionContext context)
        {
            RequestContext = null;
            RequestContext = context;
            try
            {
                foreach (var filter in BeforeExecuteFilter)
                {
                    filter.CancelAction = null;
                    filter.OnExecution(context);
                    if (filter.CancelAction == true)
                    {
                        return new JObject().ToJson();
                    }
                }
                logger.LogInformation($"Excel组件开始执行 组件名：{context.ActionDescription}，超时时间：{context.Timeout},执行前延时：{context.DelayBefore}");

                if (context.DelayBefore.HasValue && context.DelayBefore.Value > 0)
                {
                    System.Threading.Thread.Sleep(context.DelayBefore.Value);
                }

                Task task = Task.Run(() => Execute(context));
                if (!context.Timeout.HasValue || context.Timeout.Value == int.MaxValue || context.Timeout.Value < 0)
                {
                    task.Wait(-1);
                }
                else
                {
                    if (!task.Wait(context.Timeout.Value * 1000))
                    {
                        throw new ExcelException("组件执行超时，请重新配置超时时间 或 检查 Excel 软件是否有以下情况：1，有弹窗阻止执行；2，文件没有保护并可编辑；3，Excel 软件异常".I18n("Excel_436ED9BD"));
                    }
                }
                if (RawStringMethod.Contains(context.ActionName) && context.Result.ContainsKey("Raw_String"))
                {
                    return context.Result.Value<string>("Raw_String");
                }
                return context.Result?.ToString();
            }
            catch (Exception ex)
            {
                context.Exception = ex.InnerException ?? ex;
                ExceptionFilter.ForEach(f => { f.OnExecution(context); });
                throw context.Exception;
            }

        }
        internal void Execute(ActionContext context)
        {
            ExcelUtils.BeginTransfer();
            ExcelUtils.StartPerformance(context.ActionName, context.ExcelInstance?.GetType().Name);
            var perf = ExcelUtils.Performance;
            bool success = false;
            try
            {
                var method = typeof(IExcel).GetMethod(context.ActionName);
                if (method == null)
                {
                    throw new ExcelException("调用的方法不存在!".I18n("Excel_D4093208"));
                }

                var parameter = method.GetParameters();

                object[] invokeParameters = parameter.Select(p =>
                {
                    object value = context[p.Name];
                    if (p.Name.Equals("_override"))
                    {
                        value = context["override"];
                    }
                    return ExcelUtils.ConvertDynamicToDefaultValue(p.ParameterType, value);

                }).ToArray();
                long start = perf == null ? 0 : Stopwatch.GetTimestamp();
                method.Invoke(context.ExcelInstance, invokeParameters);
                perf?.Record("method", Stopwatch.GetTimestamp() - start);
                start = perf == null ? 0 : Stopwatch.GetTimestamp();
                ExecutedFilter.ForEach(f => { f.OnExecution(context); });
                perf?.Record("afterFilters", Stopwatch.GetTimestamp() - start);
                start = perf == null ? 0 : Stopwatch.GetTimestamp();
                ExcelUtils.CompleteTransfer();
                perf?.Record("publishCleanup", Stopwatch.GetTimestamp() - start);
                success = true;
            }
            catch (Exception ex)
            {
                logger.LogError((ex.InnerException ?? ex).StackTrace);
                throw ex.InnerException ?? ex;
            }
            finally
            {
                perf?.Finish(success, Config.ExcelConfigurations.Info.GlobalSetting.BatchSize);
                ExcelUtils.EndTransfer();
            }

        }
    }
}
