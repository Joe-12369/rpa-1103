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
    public class GetSiblingsElements : GuiComponentBase<ParamGetSiblingsElementsModel>
    {

        protected override eComponentType ComponentType => eComponentType.GetSiblingsElements;
        protected override eErrorMessageType OperationTimeoutMsgType => eErrorMessageType.Error_GetSiblingsElements_OperationTomeout_Error;


        protected override void ValidateParameter(ParamGetSiblingsElementsModel paramModel) {
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
                NLogger.Default.Info($"GetSiblingsElements 参数格式不符合要求：{paramModel.ToJson()}");
                throw ex;
            }
        }

        protected override void NormalizeParameter(ParamGetSiblingsElementsModel paramModel)
        {
            if (paramModel.UseAdvanced == (int)ElementMatchingMode.Advanced && !string.IsNullOrEmpty(paramModel.Filter))
            {
                var filterHelper = new FilterHelper(paramModel.Filter);
                paramModel.Filter = filterHelper.ToFilterNodesString();
            }
        }

        protected override JMessage Execute(ParamGetSiblingsElementsModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
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
                msg.Info = element.GetSiblings(new Commons.ParamGetSiblings
                {
                    Filter = paramModel.UseAdvanced == 0 ? string.Empty : paramModel.Filter,//普通模式 忽略 filter的使用
                    Position = paramModel.UseAdvanced == 0 ? paramModel.Position : -999999999
                });
            }
            catch (RPAException ex)
            {
                Exception(ex, ref msg);
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_GetSiblingsElements_Error, ref msg);
            }
            return msg;

        }
    }
}
