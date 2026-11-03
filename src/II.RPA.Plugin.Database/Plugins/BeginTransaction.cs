using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Database.Params;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Database.Plugins
{
    public class BeginTransaction : DataBaseComponentBase<ParamBeginTransactionModel>
    {
        private static ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.BeginTransaction;

        protected override JMessage Execute(ParamBeginTransactionModel paramModel, PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            try
            {
                var result = DbHelper.Instance.BeginTransaction(paramModel.ConnectionID);
                msg.Info = result.ToString();
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_DataBase_BatchInsert_Error, ref msg);
                if (!string.IsNullOrEmpty(ex?.Message))
                {
                    msg.Message = ex.Message;
                }
            }
            return msg;
        }

        protected override void ValidateParameter(ParamBeginTransactionModel paramModel)
        {
            try
            {
                if (string.IsNullOrEmpty(paramModel.ConnectionID))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError, "数据库链接参数不可为空".I18n("controlType_BeginTransaction_38EB2493"));
                }
                return;
            }
            catch (Exception ex)
            {
                logger.LogInformation($"BeginTransaction 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
    }
}
