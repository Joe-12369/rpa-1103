using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Core.Threading;
using II.RPA.Plugin.Excel.Actions.Software;
using II.RPA.Plugin.Excel.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace II.RPA.Plugin.Excel.Plugins
{
    /// <summary>
    /// 获取Excel中规则、配置 信息
    /// </summary>
    public class GetExcelRule : StaExcelAbilityBase<ParamRuleModel>
    {
        private static readonly ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.ExecuteExcel;

        public GetExcelRule(IStaDispatcher staDispatcher)
            : base(staDispatcher)
        {
        }

        protected override void ValidateParameter(ParamRuleModel paramModel) { }

        protected override JMessage Execute(ParamRuleModel paramModel, PluginExecutionContext context)
        {
            ActionContext actionContext = new ActionContext();

            JMessage msg = new JMessage();
            try
            {
                if (!string.IsNullOrEmpty(paramModel.ActionType))
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(paramModel.filePath))
                        {
                            msg.Code = 1;
                            msg.Info = "文件路径不能为空".I18n("GetExcelRule_DE6183BD");
                        }
                        else
                        {
                            var tmpFile = System.IO.Path.GetTempPath() + $"\\{System.IO.Path.GetFileName(paramModel.filePath)}";
                            System.IO.File.Copy(paramModel.filePath, tmpFile, true);
                            var dicParam = new Dictionary<string, dynamic> {
                                { "file", tmpFile},
                                { "software",new string[3]{ "MicrosoftExcel", "默认","WPS" }[paramModel.Software] },
                                { "visible","是"},
                                { "show","是"},
                                {"maximize","是" },
                                {"password1","" },
                                {"password2","" }
                            };
                            actionContext.ActionName = "Open";
                            actionContext.ActionParams = dicParam;
                            actionContext["__fromExcelAddin"] = true;
                            msg.Code = 0;
                            msg.Info = ExcelPlugin.Instance.ExecuteWithTimeout(actionContext);
                        }

                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Excel Addin error：{ex.Message}-{ex.StackTrace}");
                        msg.Message = Localize($"Excel 数据操作可视化运行出错：{ex.Message}", "GetExcelRule_CC5E5F4F");
                        msg.Code = -1;
                    }
                }
                else
                {
                    msg.Info = SoftwareExcel.Instance.GetExcelRule(paramModel);
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"GetExcelRule error：{ex.Message}-{ex.StackTrace}");
                msg.Message = ex.Message;
                msg.Code = -1;
            }
            return msg;
        }
    }
}

