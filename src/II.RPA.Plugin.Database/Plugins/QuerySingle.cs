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
    public class QuerySingle : DataBaseComponentBase<ParamQuerySingleModel>
    {
        private static ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.DbQuerySingle;

        protected override void ValidateParameter(ParamQuerySingleModel paramModel)
        {
            try
            {
                if (string.IsNullOrEmpty(paramModel.ConnectionID))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError, "数据库链接参数不可为空".I18n("controlType_QuerySingle_38EB2493"));
                }
                if (string.IsNullOrEmpty(paramModel.Sql))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError, "SQL语句参数不可为空".I18n("controlType_QuerySingle_BCEFF9F7"));
                }
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"ExecuteSql 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }

        protected override JMessage Execute(ParamQuerySingleModel paramModel, PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            try
            {
                int timeOut = paramModel.RunParam.RemainTime == -1 || paramModel.RunParam.RemainTime == 0 ? 600 :
                              paramModel.RunParam.RemainTime / 1000;
                string result = DbHelper.Instance.QuerySingle(paramModel.ConnectionID, paramModel.Sql, paramModel.Paras, paramModel.Showdic == 0 ? false : true, timeOut);
                msg.Info = result;
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_Database_QuerySingleError, ref msg);
                if (!string.IsNullOrEmpty(ex?.Message))
                {
                    msg.Message = ex.Message;
                }
            }
            return msg;
        }
    }
}

