using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
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
    public class AddFilterView : PluginAbilityBase<ParamAddFilterViewModel>
    {
        private static ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.GetAllData;

        protected override Core.JMessage Execute(ParamAddFilterViewModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            Core.JMessage msg = new Core.JMessage();
            try
            {
                SheetAdapter sheetHelper = Utils.GetAdapter(paramModel.url);
                BatchUpdateSpreadsheetResponse obj = sheetHelper.AddFilterView();
                msg.Code = 0;
                msg.Message = string.Empty;
                if (obj.Replies.Count > 0 && obj.Replies[0].AddFilterView != null)
                    msg.Info = JsonHelper.JsonByNewtonJson(obj.Replies[0].AddFilterView.Filter.FilterViewId);//修改filter时需要用到Id
                else
                    throw new Exception("创建filterView失败".I18n("controlType_AddFilterView_84F27719"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                msg.Code = 500;
                msg.Message = ex.Message;
            }
            return msg;
        }

        protected override void ValidateParameter(ParamAddFilterViewModel paramModel)
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
                logger.LogInformation($"AddFilterView 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
    }
}
