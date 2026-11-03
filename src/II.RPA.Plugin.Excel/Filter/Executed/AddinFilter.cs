using II.RPA.WinAutomation.CommonUtility;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using Microsoft.Extensions.Logging;
using System.IO;

namespace II.RPA.Plugin.Excel.Filter.Executed
{
    internal class AddinFilter : ActionFilter
    {
        public AddinFilter(FilterType filterType, int order = 0) : base(filterType, order)
        {
        }

        public override void OnExecution(ActionContext context)
        {
            //插件请求          
            if (Equals(context["__fromExcelAddin"], true))
            {
                //检查是否启用插件
                try
                {
                    dynamic excelAddin = context["__workbook"].Application.COMAddIns.Item("IntelligenceIndeed.ExcelAutomation");
                    if (excelAddin.Connect)
                    {
                        //暂时有问题，换用临时文件                       
                        var _tmp = Path.GetTempPath();
                        File.Create($"{_tmp}\\II_RPA_ExcelAddin.txt").Close();
                    }
                    else
                    {
                        ExcelPlugin.logger.LogError($"Excel插件 未链接");
                        System.Windows.Forms.MessageBox.Show("Excel 插件未启用，请打开菜单【开发工具】-》加载项 -》选择 实在RPA 表格助手".I18n("AddinFilter_3B7F5503"), "实在RPA 表格助手".I18n("AddinFilter_31A327EA"));
                    }

                }
                catch (System.Exception ex)
                {
                    ExcelPlugin.logger.LogError($"启用Excel插件异常: {ex.Message}");
                    System.Windows.Forms.MessageBox.Show("Excel 插件未启用，请打开菜单【开发工具】-》加载项 -》选择 实在RPA 表格助手".I18n("AddinFilter_3B7F5503"), "实在RPA 表格助手".I18n("AddinFilter_31A327EA"));
                }
                context.ExcelInstance.ActionContext["__fromExcelAddin"] = false;
            }
        }
    }
}
