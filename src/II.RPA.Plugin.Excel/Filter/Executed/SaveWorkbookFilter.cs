using DevExpress.CodeParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Excel.Filter.Executed
{
    /// <summary>
    /// 保存 打开的Excel 实例
    /// </summary>
    internal class SaveWorkbookFilter : ActionFilter
    {
        public SaveWorkbookFilter(FilterType filterType, int order = 0) : base(filterType, order) { }
        public override void OnExecution(ActionContext context)
        {
            if (context.Result.ContainsKey("workbook") && !ExcelPlugin.Instance.ExcelSet.ContainsKey(context.Result.Value<string>("workbook")))
            {
                ExcelPlugin.Instance.ExcelSet[System.IO.Path.GetFullPath(context.Result.Value<string>("workbook"))] = context.ExcelInstance;
            }
        }
    }
}
