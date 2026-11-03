using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Plugin.GUI.Commons;


namespace II.RPA.Plugin.GUI.Plugins
{
    public class GetElementSelectorAll : GuiComponentBase<ParamGetElementSelectorAllModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.GetElementSelectorAll;

        protected override void ValidateParameter(ParamGetElementSelectorAllModel paramModel) {
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
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"GetElementSelectorAll 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
        protected override JMessage Execute(ParamGetElementSelectorAllModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            JMessage msg = new JMessage();
            try
            {
                if (!"chrome,edge,firefox,web360se,ie,sogou,windows,web360x,superbrowser,java,zexplorer,chromium".Contains(paramModel.SelectorType.ToLower()))
                {
                    Exception(eErrorMessageType.Error_ElementNonsupport_Error, ref msg);
                    return msg;
                }
                Dictionary<string, object> hashtable = new Dictionary<string, object>{
                        {"processId",paramModel.processId },
                        {"resultAttrName","selector" }
                    };
                paramModel.cvExecutionServiceURL = GlobleConfigurations.ZBrainServicePath + CVRouteConstFields.CV_EXECUTION_SERVICE;

                var param = new GetElementFromSelectorParams
                {
                    Selector = paramModel.Selector,
                    SelectorType = paramModel.SelectorType,
                    FindOnce = true,
                    CVExecutionServiceURL = paramModel.cvExecutionServiceURL,
                    ExtendParam = hashtable,
                    WindowInfo = paramModel.WindowInfo
                };
                var elementIds = GuiElement.FromSelectorAll(param, out _);


                if (string.IsNullOrEmpty(elementIds))
                    throw new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.GetElementSelectorAll);
                string elements;
                if (paramModel.SelectorType.ToLower() == "windows" || paramModel.SelectorVersion.StartsWith("1."))
                {
                    elements = elementIds;
                }
                //else if (version.StartsWith("1.") )
                //{
                //    QuerySelectorParams querySelectorParams = new QuerySelectorParams();
                //    querySelectorParams.ElementIds = elementIds;
                //    querySelectorParams.Selector = selector;
                //    var json = JsonConvert.SerializeObject(querySelectorParams);
                //    elements = GuiElement.BuildSelectors(json, selectorType);
                //}
                else
                {
                    JArray elementJArr = JArray.Parse(elementIds);
                    JArray selectorArray = new JArray();
                    foreach (var element in elementJArr)
                    {
                        var ele = ExtractViewData.AddCommonProps(element.ToString(), paramModel.SelectorType);
                        selectorArray.Add(ele);
                    }
                    elements = selectorArray.ToString();
                }
                if (string.IsNullOrEmpty(elements))
                    throw new RPAException(eErrorMessageType.Error_NotFoundElement);
                msg.Info = elements;
            }
            catch (RPAException e)
            {
                Exception(e, e.ErrorMessageType, ref msg);
            }
            catch (Exception e)
            {
                Exception(e, eErrorMessageType.Error_UNKOWN, ref msg);
            }
            return msg;

        }

        protected override string HandlingErrorMessage(JMessage resultMsg)
        {
            //GuiElement.ChromeRelease();
            return base.HandlingErrorMessage(resultMsg);
        }
    }
}

