using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Entities;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections;
using System.Collections.Generic;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class GetSubElements : GuiComponentBase<ParamGetSubElementsModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.GetSubElements;
        protected override eErrorMessageType OperationTimeoutMsgType => eErrorMessageType.Error_GetSubElements_OperationTomeout_Error;

        protected override void ValidateParameter(ParamGetSubElementsModel paramModel) {
            try
            {
                if (string.IsNullOrEmpty(paramModel.Selector))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
                if (paramModel.UseAdvanced == (int)ElementMatchingMode.Normal)
                {
                    if (!string.IsNullOrEmpty(paramModel.Num.Trim()))
                    {
                        if (!int.TryParse(paramModel.Num.Trim(), out var num) || num <= 0)
                        {
                            throw new RPAException(eErrorMessageType.Error_GetSubElements_IndexValFail_Error);
                        }
                    }
                }
                return;
            }
            catch (Exception ex)
            {
                NLogger.Default.Info($"GetSubElements 参数格式不符合要求：{paramModel.ToJson()}");
                throw ex;
            }
        }

        protected override void NormalizeParameter(ParamGetSubElementsModel paramModel)
        {
            if (paramModel.UseAdvanced == (int)ElementMatchingMode.Advanced && !string.IsNullOrEmpty(paramModel.Filter))
            {
                var filterHelper = new FilterHelper(paramModel.Filter);
                paramModel.Filter = filterHelper.ToFilterNodesString();
            }
        }

        protected override JMessage Execute(ParamGetSubElementsModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            JMessage msg = new JMessage();
            try
            {
                Dictionary<string, object> hashtable = new Dictionary<string, object>{
                        {"processId",paramModel.processId },
                        {"useAdvanced",paramModel.UseAdvanced },
                        {"filter",paramModel.UseAdvanced ==(int)ElementMatchingMode.Normal?string.Empty: paramModel.Filter },//普通模式 忽略 filter的使用
                        {"relativeType",ElementRelativeType.Children },
                        {"positionNum",paramModel.UseAdvanced ==(int)ElementMatchingMode.Normal?paramModel.Num:null }
                    };
                var param = new GetElementFromSelectorParams
                {
                    Selector = paramModel.Selector,
                    SelectorType = paramModel.SelectorType,
                    FindOnce = true,
                    ExtendParam = hashtable,
                    MatchType = paramModel.MatchType,
                    RunParam = paramModel.RunParam,
                    WindowInfo = paramModel.WindowInfo
                };
                var element = GuiElement.FromSelector(param);
                if (element == null)
                {
                    throw new RPAException(eErrorMessageType.Error_NotFoundElement);
                }
                NotifyCoreServerElementFound(paramModel.RunUniqueID);
                msg.Info = element.GetSubElements(new Commons.ParamGetSubElements
                {
                    Filter = paramModel.UseAdvanced == 0 ? string.Empty : paramModel.Filter,//普通模式 忽略 filter的使用
                    IndexStr = paramModel.Num//paramModel.UseAdvanced == 0 ? paramModel.Num : -999999999
                });
            }
            catch (RPAException ex)
            {
                Exception(ex, ref msg);
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_GetSubElementError, ref msg);
            }
            return msg;
        }
    }
}
