using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Excel.Config;
using II.RPA.Plugin.Excel.Params;
using II.RPA.Plugin.Excel.Utils;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace II.RPA.Plugin.Excel.Plugins
{
    /// <summary>
    /// Excel 组件执行入口
    /// </summary>
    public class Run : PluginAbilityBase<ParamRunModel>
    {
        private static readonly ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.ExecuteExcel;

        protected override void ValidateParameter(ParamRunModel paramModel)
        {
            if (string.IsNullOrEmpty(paramModel.Method))
            {
                throw new RPAException(eErrorMessageType.Error_ParamsError, "调用的Excel 方法不存在".I18n("Run_FA48E652"));
            }
            if (string.IsNullOrEmpty(paramModel.Paras))
            {
                throw new RPAException(eErrorMessageType.Error_ParamsError, "Excel 组件参数不可为空".I18n("Run_1D881396"));
            }
        }

        protected override JMessage Execute(ParamRunModel paramModel, PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            ActionContext actionContext = new ActionContext();
            try
            {
                actionContext.ActionName = paramModel.Method;
                actionContext.ActionParams = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(paramModel.Paras);
                actionContext.SingleRun = paramModel.SingleRun;

                var result = ExcelPlugin.Instance.ExecuteWithTimeout(actionContext);
                msg.Info = result;
            }
            catch (JsonException je)
            {
                logger.LogError(je);
                Exception(je, eErrorMessageType.Error_ParamsError, ref msg);
                msg.Message = "参数校验失败，请检查传入的参数格式是否正确".I18n("Run_3EAD696E");
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_Excel_ExecuteError, ref msg);
                msg.Message = ex.Message;
            }
            return msg;
        }

    }
}

