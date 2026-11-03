using II.RPA.Logging;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.GoogleSheets.Params;
using II.RPA.Plugin.GoogleSheets.SheetsWrapper;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;

namespace II.RPA.Plugin.GoogleSheets.Plugins
{
    /// <summary>
    /// 
    /// </summary>
    public class CopyPasteSheet : PluginAbilityBase<ParamCopyPasteSheetModel>
    {
        private static ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.None;

        protected override Core.JMessage Execute(ParamCopyPasteSheetModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            Core.JMessage msg = new Core.JMessage();
            try
            {
                SheetAdapter sheetHelper = Utils.GetAdapter(paramModel.url);
                var ret = sheetHelper.CopyPaste(paramModel.sourceSheetName, paramModel.destSheetName, paramModel.sourceStart, paramModel.sourceEnd, paramModel.destStart, paramModel.destEnd, false);
                msg.Code = 0;
                msg.Message = string.Empty;
                msg.Info = ret.ETag;
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

        protected override void ValidateParameter(ParamCopyPasteSheetModel paramModel)
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
                logger.LogInformation($"CopyPasteSheet 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError, eCommonMessageType.G_ParamFormatNotCorrect.GetMessage());
            }
        }
    }
}
