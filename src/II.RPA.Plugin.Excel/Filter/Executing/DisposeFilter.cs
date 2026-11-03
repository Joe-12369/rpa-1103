using II.RPA.Plugin.Core;
using II.RPA.Plugin.Excel.Actions.Software;
using II.RPA.Plugin.Excel.Config;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace II.RPA.Plugin.Excel.Filter.Executing
{
    /// <summary>
    /// 释放Excel 实例
    /// </summary>
    internal class DisposeFilter : ActionFilter
    {
        public DisposeFilter(FilterType filterType, int order = 0) : base(filterType, order) { }
        public override void OnExecution(ActionContext context)
        {
            if (context.ActionName == "Dispose")
            {
                CancelAction = true;
                List<string> openedInstance = new List<string>();
                SoftwareExcel.ContextInstance.CloseIfDispose = true;
                ExcelPlugin.Instance.ExcelSet.ForEach(s => openedInstance.Add(s.Key));
                openedInstance.ForEach(instance =>
                {
                    try
                    {
                        ExcelPlugin.Instance.ExcelSet[instance].Close(instance, "否", "是");
                        ExcelPlugin.logger.LogInformation($"{instance} 已释放");
                    }
                    catch (System.Exception ex)
                    {
                        ExcelPlugin.logger.LogError($"{instance} 释放失败，{ex.Message}");
                    }
                });
                ExcelPlugin.Instance.Clear();

                // 释放 TimeoutCache 中可能遗留的 COM 对象
                if (ExcelPlugin.Instance.TimeoutCache != null)
                {
                    foreach (var item in ExcelPlugin.Instance.TimeoutCache.Values)
                    {
                        if (item != null && System.Runtime.InteropServices.Marshal.IsComObject(item))
                        {
                            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(item);
                        }
                    }
                    ExcelPlugin.Instance.TimeoutCache.Clear();
                }

                // 释放 ActionContext 参数中可能遗留的 COM 对象
                var ctx = SoftwareExcel.ContextInstance.ActionContext;
                if (ctx != null && ctx.ActionParams != null)
                {
                    foreach (var val in ctx.ActionParams.Values)
                    {
                        if (val != null && System.Runtime.InteropServices.Marshal.IsComObject(val))
                        {
                            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(val);
                        }
                    }
                }
                SoftwareExcel.ContextInstance.ActionContext = null;

                // 释放 SoftwareExcel 实例内部缓存的全局 COM 对象
                SoftwareExcel.ContextInstance.ReleaseAllComReferences();

                SoftwareExcel.ContextInstance.CloseIfDispose = false;
                context.Result = null;

                /**
                 * 737 版本 已对执行内存做了彻底优化，需求已做充分测试；不需要该配置，可以移除；
                 */

                //if (ExcelConfigurations.Info.GlobalSetting.GC?.Equals("true") == true)
                //{
                //    ExcelPlugin.logger.LogInformation($"start executing excel gc");
                //    Utils.ExcelUtils.TryFuncIgnoreException(() =>
                //    {
                //        GC.Collect();
                //        GC.WaitForPendingFinalizers();
                //        GC.Collect();
                //        GC.WaitForPendingFinalizers();
                //    });
                //    ExcelPlugin.logger.LogInformation($"excel gc execution finished");
                //}
            }
        }
    }
}
