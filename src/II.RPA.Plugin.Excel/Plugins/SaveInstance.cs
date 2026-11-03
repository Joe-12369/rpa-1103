using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Core.Threading;
using II.RPA.Plugin.Excel.Actions.Software;
using II.RPA.Plugin.Excel.Config;
using II.RPA.Plugin.Excel.Params;
using II.RPA.Plugin.Excel.Utils;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;

namespace II.RPA.Plugin.Excel.Plugins
{
    /// <summary>
    /// 保存Excel实例
    /// </summary>
    public class SaveInstance : StaExcelAbilityBase<ParamSaveInstance>
    {
        private static readonly ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.ExecuteExcel;

        public SaveInstance(IStaDispatcher staDispatcher)
            : base(staDispatcher)
        {
        }

        protected override void ValidateParameter(ParamSaveInstance paramModel) { }

        protected override JMessage Execute(ParamSaveInstance paramModel, PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            logger.LogInformation("starting save excel instance...");
            try
            {
                var saveConfig = ExcelConfigurations.Info.GlobalSetting.SaveInstance.Equals("true");
                logger.LogInformation($"save config: {saveConfig}");
              
                if (!saveConfig)
                {
                    msg.Message = "未开启自动保存实例功能";
                    return msg;
                }
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
                msg.Message = "保存完成";
            }
            catch (System.Exception ex)
            {
                logger.LogInformation("save excel instance exception", ex.Message);
                msg.Message = $"保存失败：{ex.Message}";
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
            return msg;

        }
    }
}

