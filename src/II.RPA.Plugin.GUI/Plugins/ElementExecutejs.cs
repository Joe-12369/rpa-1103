using II.RPA.Plugin.Core;
using II.RPA.WinAutomation.CommonUtility;
using System;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Plugin.GUI.Params;
using II.RPA.Plugin.GUI.Commons;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class ElementExecutejs : GuiComponentBase<ParamElementExecutejsModel>
    {
        private static ILogger logger = Logger.OfContext();
        protected override RPA.WinAutomation.CommonUtility.eComponentType ComponentType => eComponentType.ElementExecutejs;
        protected override JMessage Execute(ParamElementExecutejsModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {

            JMessage msg = new JMessage();
            GuiElement element = null;
            try
            {
                Dictionary<string, object> hashtable = new Dictionary<string, object>{
                        {"processId",paramModel.processId }
                    };
                var param = new GetElementFromSelectorParams
                {
                    Selector = paramModel.Selector,
                    SelectorType = paramModel.SelectorType,
                    ExtendParam = hashtable,
                    MatchType = paramModel.MatchType,
                    RunParam = paramModel.RunParam,
                    WindowInfo = paramModel.WindowInfo,
                    //TabId = paramModel.TabId  //最终会根据元素成功新匹配到一个TabID 
                };

                if (param.WindowInfo != null && string.IsNullOrEmpty(param.Selector))
                {
                    param.Selector = JsonConvert.SerializeObject(new
                    {
                        commonProps = new
                        {
                            GUIType = paramModel.LocateData.GetValue<string>("type"),
                            version = "1.2",
                            isfindbyindex = "false",
                            issmartcontrol = "false",
                            queryMode = "default"
                        },
                        selector = new string[0],
                        elementDetail = new
                        {
                            CategoryIconFile = "",
                            categoryName = "",
                            elementName = "",
                            elementId = "",
                            windowName = ""
                        }
                    });
                    param.SelectorType = paramModel.SelectorGuiType;
                    param.isMove = false;
                    param.UserDefinedId = "0/2/DIV";
                }

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
                    throw new RPAException(eErrorMessageType.Error_NotFoundElement);
                }
                NotifyCoreServerElementFound(paramModel.RunUniqueID);
                msg.Info = element.ElementExecutejs(paramModel.scriptCode, paramModel.@params, paramModel.ExecutionEnvironment, paramModel.isAsyncCode, paramModel.waitResultTimeout, paramModel.catchException);
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_ExecutejsFail, ref msg);
            }
            return msg;
        }

        protected override void ValidateParameter(ParamElementExecutejsModel paramModel) {
            try
            {
                // 老版本执行js组件元素是必填项网页对象是非必填项，而新版本网页对象是必填项元素是非必填项，为了兼容新老版本的逻辑
                // 此处需要同时对两个属性都判空，同时为空时才需要返回参数错误
                if (string.IsNullOrEmpty(paramModel.Selector) && paramModel.LocateData == null)
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
                if (string.IsNullOrEmpty(paramModel.scriptCode))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
                //最终会根据元素成功新匹配到一个TabID 
                //if (!string.IsNullOrEmpty(paramModel.webData))
                //{
                //    JObject jObject = JObject.Parse(paramModel.webData);
                //    if (jObject.ContainsKey("id"))
                //    {
                //        paramModel.TabId = jObject.Value<int>("id");
                //    }
                //}
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"执行JS 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
    }
}
