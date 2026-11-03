using II.RPA.WinAutomation.CommonUtility;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Excel.Filter.Executing
{
    /// <summary>
    /// 写入组件内容空值处理
    /// </summary>
    internal class WriteContentFilter : ActionFilter
    {
        public WriteContentFilter(FilterType filterType, int order = 0) : base(filterType, order) { }
        public override void OnExecution(ActionContext context)
        {
            if (context.ActionName.StartsWith("Write"))
            {
                if (string.IsNullOrWhiteSpace(context.ActionParams["column"]))
                {
                    throw new ExcelException("列名不能为空".I18n("WriteContentFilter_24631F0B"));
                }
                if (Convert.ToInt32(context.ActionParams["row"]) == 0)
                {
                    throw new ExcelException("行号不能为空".I18n("WriteContentFilter_23A052AC"));
                }

                //写入单元格允许写入空，其他不允许
                if (!context.ActionName.StartsWith("WriteCell"))
                {
                    if (context.ActionParams.ContainsKey("content") && string.IsNullOrWhiteSpace(context.ActionParams["content"]))
                    {
                        throw new ExcelException("写入内容不能为空".I18n("WriteContentFilter_15ADF0BB"));
                    }
                    if (context.ActionParams.ContainsKey("contents") && string.IsNullOrWhiteSpace(context.ActionParams["contents"]))
                    {
                        throw new ExcelException("写入内容不能为空".I18n("WriteContentFilter_15ADF0BB"));
                    }
                }
            }
        }
    }
}
