using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Entities;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Plugin.GUI.Commons;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class GetSapTableData : GuiComponentBase<ParamGetTableDataModel>
    {
        const int SUCCESS_STATUS = 0;
        const int FAIL_STATUS = 1;
        const int CANCEL_STATUS = -1;
        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.GetTableData;
        protected override eErrorMessageType OperationTimeoutMsgType => eErrorMessageType.Error_GetTableData_OperationTomeout_Error;

        protected override JMessage Execute(ParamGetTableDataModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            logger.LogDebug($"获取到selectorType：{paramModel.SelectorType}");
            JMessage msg = new JMessage();

            if (paramModel.SelectorType.ToLower() == "sap")
            {
                GuiElement element = null;
                try
                {
                    Dictionary<string, object> hashtable = new Dictionary<string, object>{
                        {"processId",paramModel.processId }
                    };

                    //element = GuiElement.FromSelector(selector, selectorType, false, extendParam: hashtable);
                    var param = new GetElementFromSelectorParams
                    {
                        Selector = paramModel.Selector,
                        SelectorType = paramModel.SelectorType,
                        FindOnce = false,
                        ExtendParam = hashtable,
                        MatchType = paramModel.MatchType,
                        RunParam = paramModel.RunParam,
                        WindowInfo = paramModel.WindowInfo
                    };
                    element = GuiElement.FromSelector(param);
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
                        throw new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.GetTableData);
                    }
                    NotifyCoreServerElementFound(paramModel.RunUniqueID);
                    ParamTableModel tm = new ParamTableModel();
                    tm.DataScope = paramModel.scopeType;
                    tm.ColumnNo = paramModel.columnNo;
                    tm.RowNo = paramModel.rowNo;
                    tm.ContainHeader = paramModel.containHeader;
                    string input = JsonHelper.JsonByNewtonJson(tm);
                    string result = element.GetTableData(input);
                    msg.Info = result;
                    return msg;
                }
                catch (Exception e)
                {
                    Exception(e, eErrorMessageType.Error_GetTableDataError, ref msg);
                    return msg;
                }
            }
            else
            {
                GuiElement element = null;
                try
                {
                    Dictionary<string, object> hashtable = new Dictionary<string, object>{
                        {"processId",paramModel.processId }
                    };

                    element = GuiElement.FromSelector(paramModel.Selector, paramModel.SelectorType, false, extendParam: hashtable);
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
                        throw new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.GetTableData);
                    }
                    NotifyCoreServerElementFound(paramModel.RunUniqueID);
                    ParamTableModel tm = new ParamTableModel();
                    tm.DataScope = paramModel.scopeType;
                    tm.ColumnNo = paramModel.columnNo;
                    tm.RowNo = paramModel.rowNo;
                    tm.ContainHeader = paramModel.containHeader;
                    string input = JsonHelper.JsonByNewtonJson(tm);
                    string result = element.GetTableData(input);
                    if (!result.StartsWith("[["))
                        Exception(eErrorMessageType.Error_GetTableDataError, ref msg);
                    else
                        msg.Info = result;
                    return msg;
                }
                catch (Exception e)
                {
                    Exception(e, eErrorMessageType.Error_GetTableDataError, ref msg);
                    return msg;
                }
            }
            return msg;
        }

        protected override void ValidateParameter(ParamGetTableDataModel paramModel) {
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
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"GetTableData参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
    }
}

