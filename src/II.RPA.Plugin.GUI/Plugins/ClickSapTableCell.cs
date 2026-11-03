using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class ClickSapTableCell : GuiComponentBase<ParamClickTableCellModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.ClickSapTableCell;
        protected override eErrorMessageType OperationTimeoutMsgType => eErrorMessageType.Error_ClickSapTableCell_OperationTomeout_Error;

        const int SUCCESS_STATUS = 0;
        const int FAIL_STATUS = 1;
        const int CANCEL_STATUS = -1;
        protected override JMessage Execute(ParamClickTableCellModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            JMessage msg = new JMessage();

            if (paramModel.SelectorGuiType.ToLower() != "sap")
            {
                msg.Code = FAIL_STATUS;
                msg.UnifyCode = FAIL_STATUS.ToString();
                msg.Message = "获取失败,拾取的不是SAP表格元素".I18n("ClickSapTableCell_40181AAD");
                return msg;
            }
            else
            {
                GuiElement element = null;
                try
                {
                    element = GuiElement.FromSelector(paramModel.Selector, paramModel.SelectorGuiType, false);
                }
                catch (Exception ex)
                {
                    Exception(ex, eErrorMessageType.Error_NotFoundElement, ref msg);
                    return msg;
                }

                try
                {
                    if (element == null)
                    {
                        throw new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.ClickSapTableCell);
                    }
                    NotifyCoreServerElementFound(paramModel.RunUniqueID);
                    SapClickModel scm = new SapClickModel();
                    scm.ColumnNo = paramModel.columnNo;
                    scm.RowNo = paramModel.rowNo;
                    scm.ClickType = paramModel.clickType;
                    string input = JsonHelper.JsonByNewtonJson(scm);
                    string result = element.ClickTableCell(input);
                    msg.Info = result;
                    return msg;
                }
                catch (Exception e)
                {
                    Exception(e, eErrorMessageType.Error_GetTableDataError, ref msg);
                    return msg;
                }
            }
        }

        protected override void ValidateParameter(ParamClickTableCellModel paramModel) {
            try
            {
                if (string.IsNullOrEmpty(paramModel.Selector))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, "ClickSapTableCell参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
    }
}

