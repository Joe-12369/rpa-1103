using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Abstractions;
using II.RPA.WinAutomation.CommonUtility;
using System;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Plugin.Database.Params;

namespace II.RPA.Plugin.Database.Plugins
{
    public class ExecuteSql : DataBaseComponentBase<ParamExecuteSqlModel>
    {
        private static ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.DbExecuteSql;

        protected override void ValidateParameter(ParamExecuteSqlModel paramModel)
        {
            try
            {
                if (string.IsNullOrEmpty(paramModel.ConnectionID))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError, "数据库链接参数不可为空".I18n("controlType_ExecuteSql_38EB2493"));
                }
                if (string.IsNullOrEmpty(paramModel.Sql))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError, "SQL语句参数不可为空".I18n("controlType_ExecuteSql_BCEFF9F7"));
                }
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"ExecuteSql 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }

        protected override JMessage Execute(ParamExecuteSqlModel paramModel, PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            try
            {
                int timeOut = paramModel.RunParam.RemainTime == -1 || paramModel.RunParam.RemainTime == 0 ? 600 :
                             paramModel.RunParam.RemainTime / 1000;
                int result = DbHelper.Instance.ExecuteSql(paramModel.ConnectionID, paramModel.Sql, paramModel.Paras, timeOut);
                msg.Info = result.ToString();
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_Database_ExecuteSqlError, ref msg);
                if (!string.IsNullOrEmpty(ex?.Message))
                {
                    msg.Message = ex.Message;
                }
            }
            return msg;
        }
    }
}

