using II.RPA.WinAutomation.CommonUtility;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Plugin.Excel.Actions.NonSoftware;
using II.RPA.Plugin.Excel.Actions.Software;
using II.RPA.Plugin.Excel.Config;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.Excel.Filter.Executing
{
    /// <summary>
    /// 获取Excel执行实例
    /// </summary>
    internal class ExcelInstanceFilter : ActionFilter
    {
        public ExcelInstanceFilter(FilterType filterType, int order = 0) : base(filterType, order) { }
        public override void OnExecution(ActionContext context)
        {
            if ("Load,GetExcelRule,Interop".Contains(context.ActionName))
            {
                context.ExcelInstance = SoftwareExcel.Instance;
            }
            else if (context.ActionName == "New" || context.ActionName == "NewV6" ||
                     context.ActionName == "Open" || context.ActionName == "OpenV5")
            {
                if ((context.ActionName == "Open" || context.ActionName == "OpenV5") &&
                    ExcelPlugin.Instance.ExcelSet.ContainsKey(context["file"]))
                {
                    //处理相同文件 切换模式打开
                    var excelType = ExcelPlugin.Instance.ExcelSet[context["file"]] is SoftwareExcel ? "使用软件" : "不使用软件";
                    if (context.ActionParams["visible"].Equals("是") && excelType.Equals("不使用软件"))
                    {
                        if (NonSoftware.ContextInstance.Workbooks.ContainsKey(context["file"]))
                        {
                            NonSoftware.Instance.Close(context["file"], "是", "否");
                        }
                        else
                        {
                            ExcelPlugin.Instance.ExcelSet.Remove(context["file"]);
                            NonSoftware.ContextInstance.logger.LogInformation($"instance cache is invalid.");
                        }

                    }
                    if (context.ActionParams["visible"].Equals("否") && excelType.Equals("使用软件"))
                    {
                        throw new ExcelException(Localize($"{context["file"]} 已选择【使用软件】方式打开，请用【关闭Excel】组件关闭后重试或重新打开流程", "ExcelInstanceFilter_7DA18503"));
                    }
                }
                if (string.IsNullOrWhiteSpace(ExcelConfigurations.Info.GlobalSetting?.OpenMode))
                {
                    context.ExcelInstance = context.ActionParams["visible"].Equals("是") ? SoftwareExcel.Instance : NonSoftware.Instance;
                }
                else
                {
                    logger.LogInformation($"global config open mode: {ExcelConfigurations.Info.GlobalSetting?.OpenMode}");
                    //全局控制打开文件方式
                    context.ExcelInstance = ExcelConfigurations.Info.GlobalSetting?.OpenMode == "None" ? NonSoftware.Instance : SoftwareExcel.Instance;
                    context.ActionParams["software"] = ExcelConfigurations.Info.GlobalSetting?.OpenMode;
                }

            }
            else
            {
                context.ExcelInstance = ExcelPlugin.Instance.GetExcelInstance(context);
            }
            context.ExcelInstance.ActionContext = context;

        }
    }
}
