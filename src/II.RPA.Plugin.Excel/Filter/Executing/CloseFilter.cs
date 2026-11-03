using II.RPA.Plugin.Excel.Actions.Software;
using II.RPA.Plugin.Excel.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace II.RPA.Plugin.Excel.Filter.Executing
{
    /// <summary>
    /// 写入组件内容空值处理
    /// </summary>
    internal class CloseFilter : ActionFilter
    {
        public CloseFilter(FilterType filterType, int order = 0) : base(filterType, order) { }
        public override void OnExecution(ActionContext context)
        {

            if (context.ActionName.StartsWith("CloseV2") || context.ActionName.StartsWith("CloseV3"))
            {
                try
                {
                    //关闭软件
                    if (context["closeType"] == 1)
                    {
                        CancelAction = true;
                        var software = context["software"];
                        var instance = SoftwareExcel.ContextInstance.Workbooks.Where(w =>
                        {
                            try
                            {
                                object app = ComLateBinding.GetProp(w.Value, "Application");
                                bool isWPS = Convert.ToString(ComLateBinding.GetProp(app, "Caption")).Contains("WPS");
                                return (software == 1 && isWPS) || (software == 0 && !isWPS);
                            }
                            catch (System.Exception)
                            {
                                return false;
                            }

                        }).ToList();

                        object firstWorkbook = instance.FirstOrDefault().Value;
                        object excelApp = firstWorkbook == null ? null : ComLateBinding.GetProp(firstWorkbook, "Application");
                        if (excelApp != null)
                        {
                            NativeUtils.GetWindowThreadProcessId(ComLateBinding.ToInt(ComLateBinding.GetProp(excelApp, "Hwnd")), out int pid);
                            for (int i = 0; i < instance.Count; i++)
                            {
                                var item = instance.ElementAt(i);
                                try
                                {
                                    if (Marshal.IsComObject(item.Value) && item.Value != null)
                                    {
                                        ComLateBinding.Invoke(item.Value, "Close", false);
                                        //SoftwareExcel.ContextInstance.ReleaseComObject(item.Value);
                                    }
                                }
                                catch (System.Exception ex)
                                {
                                    logger.LogError(ex);
                                    continue;
                                }
                                finally
                                {
                                    SoftwareExcel.ContextInstance.Workbooks.Remove(item.Key);
                                    ExcelPlugin.Instance.ExcelSet.Remove(item.Key);
                                }
                            }
                        }

                        //杀掉进程                     
                        if (software == 1)
                        {
                            ExcelUtils.TryFuncIgnoreException(() =>
                            {
                                foreach (var item in Process.GetProcessesByName("wps"))
                                {
                                    item.Kill();
                                }
                                foreach (var item in Process.GetProcessesByName("et"))
                                {
                                    item.Kill();
                                }
                            });
                        }
                        else
                        {
                            ExcelUtils.TryFuncIgnoreException(() =>
                            {
                                foreach (var item in Process.GetProcessesByName("excel"))
                                {
                                    item.Kill();
                                }
                            });
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    logger.LogError(ex.StackTrace);
                }
            }
        }
    }
}
