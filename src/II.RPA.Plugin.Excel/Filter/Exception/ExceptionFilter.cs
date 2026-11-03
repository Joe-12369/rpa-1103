using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Plugin.Excel.Config;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace II.RPA.Plugin.Excel.Filter.Exception
{
    /// <summary>
    /// 组件执行异常处理
    /// </summary>
    internal class ExceptionFilter : ActionFilter
    {
        public ExceptionFilter(FilterType filterType, int order = 0) : base(filterType, order) { }
        public override void OnExecution(ActionContext context)
        {
            if (context.Exception != null)
            {
                ExcelPlugin.logger.LogError(context.Exception);
                string message = null;
                if (context.Exception.Message.Contains("HRESULT") || context.Exception.Message.ToLower().Contains("com"))
                {
                    message = Localize($"Office返回消息：{context.Exception.Message}", "ExceptionFilter_D96A9073");
                }
                context.ActionName = ExcelConfigurations.Info.GetActionName(context.ActionName);
                context.Exception = new ExcelException(message ?? context.Exception.Message);
            }
        }
    }
}
