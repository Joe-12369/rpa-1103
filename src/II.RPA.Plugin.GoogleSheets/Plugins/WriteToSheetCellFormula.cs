using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.GoogleSheets.Params;
using II.RPA.Plugin.GoogleSheets.SheetsWrapper;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using II.RPA.Logging;

namespace II.RPA.Plugin.GoogleSheets.Plugins
{
    public class WriteToSheetCellFormula : PluginAbilityBase<ParamWriteToSheetCellModel>
    {
        private static ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.None;

        protected override Core.JMessage Execute(ParamWriteToSheetCellModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            Core.JMessage msg = new Core.JMessage();
            try
            {
                SheetAdapter sheetHelper = Utils.GetAdapter(paramModel.url);
                var ret = sheetHelper.UpdateOneCellFormula(paramModel.sheetName, paramModel.cellName, paramModel.value);
                msg.Code = 0;
                msg.Message = string.Empty;
                msg.Info = JsonHelper.JsonByNewtonJson(ret.UpdatedCells);
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

        protected override void ValidateParameter(ParamWriteToSheetCellModel paramModel)
        {
            try
            {
                if (!Utils.IsValidGoogleSheetUrl(paramModel.url))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError, eCommonMessageType.G_GoogleUrlNotCorrect.GetMessage());
                }
                return;
            }
            catch (Exception)
            {
                logger.LogInformation($"WriteToSheetCellFormula 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError, eCommonMessageType.G_ParamFormatNotCorrect.GetMessage());
            }
        }
    }
}
