using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Abstractions;
using II.RPA.WinAutomation.CommonUtility;
using System;
using II.RPA.Plugin.Database.Params;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.Database.Plugins
{
    public class TestDbConnection : DataBaseComponentBase<ParamTestDbConnectionModel>
    {
        private static ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.TestDbConnection;

        protected override void ValidateParameter(ParamTestDbConnectionModel paramModel)
        {
            try
            {
                if (string.IsNullOrEmpty(paramModel.ConnectStr))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError, "数据库链接配置不可为空".I18n("controlType_TestDbConnection_FF73DA0"));
                }
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"TestDbConnection 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }

        protected override JMessage Execute(ParamTestDbConnectionModel paramModel, PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            try
            {
                int timeOut = paramModel.RunParam.RemainTime == -1 || paramModel.RunParam.RemainTime == 0 ? 30 :
                              paramModel.RunParam.RemainTime / 1000;
                DbHelper.Instance.TestDbConnection(paramModel.DbType, paramModel.ConnectStr, timeOut);
            }
            catch (Exception ex)
            {
                string erroMsg = ex.Message;
                Exception(ex, eErrorMessageType.Error_Database_TestDbConnectionError, ref msg);
                if (ex is AggregateException)
                {
                    if (!string.IsNullOrEmpty(ex.InnerException?.Message))
                    {
                        erroMsg = ex.InnerException.Message;
                    }
                }
                if (!string.IsNullOrEmpty(erroMsg))
                {
                    msg.Message = erroMsg;
                }
            }
            return msg;
        }
    }
}

