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
    public class CloseConnection : DataBaseComponentBase<ParamCloseConnectionModel>
    {
        private static ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.DbCloseConnection;

        protected override void ValidateParameter(ParamCloseConnectionModel paramModel)
        {
            try
            {
                if (string.IsNullOrEmpty(paramModel.ConnectionID))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError, "数据库链接参数不可为空".I18n("controlType_CloseConnection_38EB2493"));
                }
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"CloseConnection 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }

        protected override JMessage Execute(ParamCloseConnectionModel paramModel, PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            try
            {
                DbHelper.Instance.CloseConnection(paramModel.ConnectionID);
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_Database_CloseConnectionError, ref msg);
                if (!string.IsNullOrEmpty(ex?.Message))
                {
                    msg.Message = ex.Message;
                }
            }
            return msg;
        }
    }
}

