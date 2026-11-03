using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Entities;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections;
using System.Collections.Generic;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class GetParent : GuiComponentBase<ParamGetParentModel>
    {
        protected override eComponentType ComponentType => eComponentType.GetParent;
        protected override eErrorMessageType OperationTimeoutMsgType => eErrorMessageType.Error_GetParent_OperationTomeout_Error;


        protected override void ValidateParameter(ParamGetParentModel paramModel) {
            try
            {
                if (string.IsNullOrEmpty(paramModel.Selector))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
                if (paramModel.UseAdvanced == (int)ElementMatchingMode.Normal)
                {
                    if (!string.IsNullOrEmpty(paramModel.Level.Trim()))
                    {
                        if (!int.TryParse(paramModel.Level.Trim(), out var num) || num <= 0 || paramModel.Level.IndexOf(".") >= 0)
                        {
                            throw new RPAException(eErrorMessageType.Error_GetParentElements_LevelValFail_Error);
                        }
                    }
                    else
                    {
                        throw new RPAException(eErrorMessageType.Error_GetParentElements_LevelValFail_Error);
                    }
                }
                return;
            }
            catch (Exception ex)
            {
                NLogger.Default.Info($"GetParent 参数格式不符合要求：{paramModel.ToJson()}");
                throw ex;
            }
        }

        protected override void NormalizeParameter(ParamGetParentModel paramModel)
        {
            if (paramModel.UseAdvanced == (int)ElementMatchingMode.Advanced && !string.IsNullOrEmpty(paramModel.Filter))
            {
                var filterHelper = new FilterHelper(paramModel.Filter);
                paramModel.Filter = filterHelper.ToFilterNodesString();
            }
        }

        protected override JMessage Execute(ParamGetParentModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            JMessage msg = new JMessage();
            if (GuiElement.selectorType_supportSiblings.IndexOf($",{paramModel.SelectorType.ToLower()},") < 0)
            {
                Exception(eErrorMessageType.Error_NotImplemented, ref msg);
                return msg;
            }
            try
            {
                Dictionary<string, object> hashtable = new Dictionary<string, object>{
                        {"processId",paramModel.processId }
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
                msg.Info = element.GetParent(new Commons.ParamGetParent
                {
                    Filter = paramModel.UseAdvanced == 0 ? string.Empty : paramModel.Filter,//普通模式 忽略 filter的使用
                    Level = paramModel.UseAdvanced == 0 ? int.Parse(paramModel.Level) : -999999999
                });
            }
            catch (RPAException ex)
            {
                Exception(ex, ref msg);
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_GetParent_Error, ref msg);
            }
            return msg;
        }
    }
}
