using II.RPA.Plugin.Excel.Actions.NonSoftware;
using II.RPA.Plugin.Excel.Utils;
using II.RPA.WinAutomation.CommonUtility;
using System.IO;

namespace II.RPA.Plugin.Excel.Filter.Executed
{
    /// <summary>
    /// 序列化保存由于超时机制丢失的对象
    /// </summary>
    internal class TimeoutFilter : ActionFilter
    {
        public TimeoutFilter(FilterType filterType, int order = 0) : base(filterType, order) { }
        public override void OnExecution(ActionContext context)
        {
            if (context.ActionName.Equals("Close"))
            {
                ExcelPlugin.Instance.TimeoutCache.Remove(context.ActionParams["workbook"]);
            }
            if (context.ActionName == "New" || context.ActionName == "NewV6" ||
                context.ActionName == "Open" || context.ActionName == "OpenV5")
            {              
                ExcelPlugin.Instance.TimeoutCache[Path.GetFullPath(context.Result.Value<string>("workbook"))] = context.ActionParams;              
            }
        }
    }
}
