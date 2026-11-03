using Google.Apis.Sheets.v4.Data;
using II.RPA.Logging;
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
    public class GetAllData : PluginAbilityBase<ParamGetAllDataModel>
    {
        private static ILogger logger = Logger.OfContext();
        //protected override eComponentType ComponentType => eComponentType.GetAllData;
        protected override eComponentType ComponentType => eComponentType.None;

        protected override Core.JMessage Execute(ParamGetAllDataModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            Core.JMessage msg = new Core.JMessage();
            try
            {
                SheetAdapter sheetHelper = Utils.GetAdapter(paramModel.url);
                SheetRange range = new SheetRange($"{paramModel.sheetName}!{paramModel.startPosition}:{paramModel.endPosition}");
                IList<IList<object>> obj = sheetHelper.GetRows(range);
                msg.Code = 0;
                msg.Message = string.Empty;
                msg.Info = JsonHelper.JsonByNewtonJson(obj);
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                //Exception(ex, eErrorMessageType.Error_UNKOWN, ref msg);
                msg.Code = 500;
                msg.Message = ex.Message;
            }
            return msg;
        }

        protected override void ValidateParameter(ParamGetAllDataModel paramModel)
        {
            try
            {
                if (!Utils.IsValidGoogleSheetUrl(paramModel.url))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError, eCommonMessageType.G_GoogleUrlNotCorrect.GetMessage());
                }
                return;
            }
            catch (Exception ex)
            {
                logger.LogInformation($"GetAllData 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError, eCommonMessageType.G_ParamFormatNotCorrect.GetMessage());
            }
        }
    }
}
