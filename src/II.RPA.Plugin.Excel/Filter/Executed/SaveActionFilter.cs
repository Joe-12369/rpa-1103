using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Excel.Filter.Executed
{

    /// <summary>
    /// 组件执行后，统一执行保存操作
    /// </summary>
    internal class SaveActionFilter : ActionFilter
    {
        public SaveActionFilter(FilterType filterType, int order = 0) : base(filterType, order) { }
        public override void OnExecution(ActionContext context)
        {
            if (Equals(context["save"], "是"))
            {
                context.ExcelInstance.Save(workbook: context.ActionParams["workbook"], close: "否");
                if (context.ActionParams.ContainsKey("destWorkbook"))
                {
                    context.ExcelInstance.Save(workbook: context.ActionParams["destWorkbook"], close: "否");
                }
            }
        }
    }
}

