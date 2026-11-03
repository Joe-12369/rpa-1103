using II.RPA.Plugin.Core;
using II.RPA.Plugin.Excel.Actions.Software;
using II.RPA.Plugin.Excel.Config;
using II.RPA.Plugin.Excel.Utils;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;

namespace II.RPA.Plugin.Excel.Filter.Executing
{
    internal class SaveInstanceFilter : ActionFilter
    {
        public SaveInstanceFilter(FilterType filterType, int order = 0) : base(filterType, order) { }
        public override void OnExecution(ActionContext context)
        {

            if (context.ActionName == "SaveInstance")
            {
                var saveConfig = ExcelConfigurations.Info.GlobalSetting.SaveInstance.Equals("true");
                logger.LogInformation($"save config: {saveConfig}");

                CancelAction = true;
                if (!saveConfig)
                {
                    return;
                }
                logger.LogInformation("starting save excel instance...");
                try
                {
                    var existInstance = ExcelPlugin.Instance.TimeoutCache.Where(instance => ExcelPlugin.Instance.ExcelSet.ContainsKey(instance.Key));
                    logger.LogInformation($"save instance count:{existInstance?.Count()}");

                    SoftwareExcel.ContextInstance.CloseIfDispose = true;

                    var openedInstance = ExcelPlugin.Instance.ExcelSet.Select(e => e.Key);
                    openedInstance.ForEach(instance =>
                    {
                        try
                        {
                            ExcelPlugin.Instance.ExcelSet[instance].Close(instance, "是", "否");
                            logger.LogInformation($"{instance} saved");
                        }
                        catch (System.Exception ex)
                        {
                            logger.LogError($"{instance} save failed，{ex.Message}");
                        }
                    });
                }
                catch (System.Exception ex)
                {
                    logger.LogInformation("save excel instance exception", ex.Message);
                }
                finally
                {
                    logger.LogInformation("save excel instance finished...");

                    if (ExcelPlugin.Instance.TimeoutCache.Count > 0)
                    {
                        string tempPath = ExcelUtils.GetPackageDirectory();
                        if (!Directory.Exists(tempPath))
                        {
                            Directory.CreateDirectory(tempPath);
                        }
                        File.WriteAllText(Path.Combine(tempPath, "excel_timeout.cache"), JsonHelper.ToJson(ExcelPlugin.Instance.TimeoutCache));
                    }
                }
            }
        }
    }
}
