using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.GoogleSheets.Params;
using II.RPA.Plugin.GoogleSheets.SheetsWrapper;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GoogleSheets.Plugins
{
    public class GetAllSheetsName : PluginAbilityBase<ParamGetAllSheetsNameModel>
    {
        private static ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.None;

        protected override Core.JMessage Execute(ParamGetAllSheetsNameModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            try
            {
                SheetAdapter sheetHelper =Utils.GetAdapter(paramModel.url);
                List<string> sheets = sheetHelper.GetAllTabNames();
                msg.Code = 0;
                msg.Message = string.Empty;
                msg.Info = JsonHelper.JsonByNewtonJson(sheets);
            }
            catch (Exception ex)
            {
               logger.LogError(ex);
                //Exception(ex, eErrorMessageType.Error_UNKOWN, ref msg);
                //返回具体的异常信息
                msg.Code = 500;
                msg.Message = ex.Message;
            }
            return msg;
        }

        protected override void ValidateParameter(ParamGetAllSheetsNameModel paramModel)
        {
            try
            {
                if(!Utils.IsValidGoogleSheetUrl(paramModel.url))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError, eCommonMessageType.G_GoogleUrlNotCorrect.GetMessage());
                }
                return;
            }
            catch (Exception)
            {
                logger.LogInformation($"GetAllSheetsName 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError, eCommonMessageType.G_ParamFormatNotCorrect.GetMessage());
            }
        }
    }
}
