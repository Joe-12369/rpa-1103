using II.RPA.WinAutomation.CommonUtility;
using System;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Plugin.Core;
using Newtonsoft.Json;
using II.RPA.Plugin.GUI.Params;
using System.Collections.Generic;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.Core.Commons;
using Newtonsoft.Json.Linq;
using System.Linq;
using II.RPA.WebBrowser.Common.Utils;
using System.Windows.Interop;
using II.RPA.WebBrowser.Common.Basic;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class GetElement_V2 : GuiComponentBase<ParamGetElementModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.GetElement;

        protected override void ValidateParameter(ParamGetElementModel paramModel) {
            try
            {
                if (string.IsNullOrEmpty(paramModel.QueryMode))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
                if (string.IsNullOrEmpty(paramModel.Selector))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
                if (paramModel.LocateData == null)
                {
                    if (!Enum.TryParse(paramModel.browserType, true, out Commons.WindowMatch.WindowType wndType))
                    {
                        wndType = Commons.WindowMatch.WindowType.Chromium;
                        if (string.IsNullOrEmpty(paramModel.SelectorGuiType))
                        {
                            paramModel.SelectorGuiType = wndType.ToString();
                        }
                    }
                }
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"GetElement 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
        protected override JMessage Execute(ParamGetElementModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            JMessage message = new JMessage();
            string parentSelector = string.Empty;
            string version = string.Empty;
            GuiElement parentElement = null;
            ParamGetElementModel parentParamModel = null;
            try
            {
                if (paramModel.QueryMode != "default")
                {
                    bool isIeWithXpath = (paramModel.LocateData != null ?
                        (paramModel.LocateData.TryGetValue("type", out string browserType) && browserType.ToLower() == "ie")
                        : paramModel.browserType.ToLower() == "ie") && paramModel.QueryMode == "xpath";
                    if (isIeWithXpath)
                    {
                        throw new RPAException(eGuiType.IE, eErrorMessageType.Error_NotSupertXpath);
                    }
                    paramModel.Selector = BuildRPACommon(paramModel);
                    //兼容700-sp3(网页对象改造前)
                    if (paramModel.LocateData == null)
                    {
                        var title = string.IsNullOrEmpty(paramModel.webData) ? "" : JsonConvert.DeserializeObject<JObject>(paramModel.webData)["title"].ToString();
                        paramModel.WindowInfo = BrowserWindowProvider.GetFirstBrowserByZOrder((wndInfo) => wndInfo.Title.ContainsIgnoreCase(title), (eGuiType)Enum.Parse(typeof(eGuiType), paramModel.browserType, true));
                    }
                }
                else
                {
                    paramModel.Selector = this.GetSelectorInfo(paramModel);
                }
                logger.LogInformation($"版本号：{paramModel.SelectorVersion}");
                if (!string.IsNullOrEmpty(paramModel.relateSelector) && paramModel.b_isAssociationParent)
                {
                    parentParamModel = paramModel.Clone();
                    parentParamModel.Selector = paramModel.relateSelector;
                    //元素拾取模式时走根据Z序去找窗口处理
                    if (paramModel.QueryMode == "default")
                    {
                        parentParamModel.Selector = this.GetSelectorInfo(parentParamModel);
                    }

                }
                if (paramModel.b_isAssociationParent)
                {
                    ExtractViewData.GetQueryModeSelector(paramModel.Selector, out string queryMode);
                    if (!GuiElement.selectorType_Web.Contains(parentParamModel.SelectorType.ToLower()) && queryMode != "default")
                    {
                        Exception(eErrorMessageType.Error_NotSupportParentElement, ref message);
                        return message;
                    }

                    if (paramModel.SelectorType != parentParamModel.SelectorType && (!GuiElement.selectorType_Web.Contains(paramModel.SelectorType.ToLower()) || !GuiElement.selectorType_Web.Contains(parentParamModel.SelectorType.ToLower())))
                        throw new RPAException(eErrorMessageType.Error_NotParentElementType);
                    if (!GuiElement.selectorType_Win.Contains(parentParamModel.SelectorType.ToLower()))
                    {
                        if ((!GuiElement.selectorType_Web.Contains(paramModel.SelectorType.ToLower()) || !GuiElement.selectorType_Web.Contains(parentParamModel.SelectorType.ToLower())))
                            throw new RPAException(eErrorMessageType.Error_NotParentElementType);
                        var parentParam = new GetElementFromSelectorParams
                        {
                            Selector = parentParamModel.Selector,
                            SelectorType = parentParamModel.SelectorType,
                            CVExecutionServiceURL = GlobleConfigurations.ZBrainServicePath + CVRouteConstFields.CV_EXECUTION_SERVICE,
                            FindOnce = true,
                            MatchType = paramModel.MatchType,
                            RunParam = paramModel.RunParam,
                            WindowInfo = paramModel.WindowInfo
                        };
                        //默认取第一个
                        parentElement = GuiElement.FromSelector(parentParam);
                        if (parentElement == null)
                        {
                            throw new RPAException(eErrorMessageType.Error_NotFoundElement);
                        }
                        if (parentElement.IsGuiType(eGuiType.IE.ToString()) && paramModel.QueryMode == "xpath")
                        {
                            throw new RPAException(eErrorMessageType.Error_NotSupertXpath);
                        }
                        NotifyCoreServerElementFound(paramModel.RunUniqueID);
                    }
                }
                GuiElement guiElement;
                if (parentElement == null)
                {
                    try
                    {
                        Dictionary<string, object> hashtable = new Dictionary<string, object> { { "processId", paramModel.processId } };
                        paramModel.cvExecutionServiceURL = GlobleConfigurations.ZBrainServicePath + CVRouteConstFields.CV_EXECUTION_SERVICE;
                        var param = new GetElementFromSelectorParams
                        {
                            Selector = paramModel.Selector,
                            SelectorType = paramModel.SelectorType,
                            FindOnce = false,
                            CVExecutionServiceURL = paramModel.cvExecutionServiceURL,
                            ExtendParam = hashtable,
                            MatchType = paramModel.MatchType,
                            RunParam = paramModel.RunParam,
                            WindowInfo = paramModel.WindowInfo
                        };
                        param.ActionIEModeExcetpion = (ex) =>
                        {
                            if (ex.IsRPAException(eErrorMessageType.Error_EmbeddedIEMode, out _))
                            {
                                if (paramModel.QueryMode == "xpath")
                                {
                                    throw new RPAException(eErrorMessageType.Error_NotSupertXpath);
                                }
                            }
                        };
                        guiElement = GuiElement.FromSelector(param);
                    }
                    catch (RPAException e)
                    {
                        Exception(e, e.ErrorMessageType, ref message);
                        return message;
                    }
                    catch (Exception e)
                    {
                        Exception(e, eErrorMessageType.Error_UNKOWN, ref message);
                        return message;
                    }
                }
                else
                {
                    if (GuiElement.selectorType_Web.Contains(paramModel.SelectorType.ToLower())
                            || GuiElement.selectorType_Win.Contains(paramModel.SelectorType.ToLower()))
                    {
                        var queryParamModel = new Commons.Params.ParamQueryTargetSelectorAllModel
                        {
                            selector = paramModel.Selector,
                            RelateElementId = parentElement.UserDefinedID,
                            ElementRelateMode = paramModel.relateMode,
                            attributeName = "selector",
                            startIndex = 1,
                            endIndex = -1,
                            selectorVersion = paramModel.SelectorVersion,
                            discernHidden = paramModel.DiscernHidden
                        };
                        var elementAll = parentElement.QueryTargetSelectorAll(queryParamModel);
                        if (elementAll.Count == 0)
                            throw new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.GetElement);
                        guiElement = elementAll.First();
                    }
                    else
                    {
                        Exception(eErrorMessageType.Error_ElementNonsupport_Error, ref message);
                        return message;
                    }
                }
                string selector = string.Empty;
                try
                {
                    selector = guiElement.Selector;
                    var common = selector.ToObject<RPACommon>();
                    if (common?.CommonProps != null)
                    {
                        common.CommonProps.ExtendedTags |= ExtendedTags.SimilarElementItem;
                        selector = common.ToJsonNoneFormating();
                    }
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "反序列化赋值元素标识位失败，直接返回原结构");
                    selector = guiElement.Selector;
                }
                message.Info = selector;
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_NotFoundElement, ref message);
                return message;
            }
            return message;
        }
        private string BuildRPACommon(ParamGetElementModel paramModel)
        {
            RPACommon rpaCommon = new RPACommon();
            string title;
            string url;
            //兼容700-sp3(网页对象改造前)
            if (paramModel.LocateData != null)
            {
                paramModel.LocateData.TryGetValue("type", out string browserType);
                //rpaCommon.CommonProps.GUIType = browserType;
                rpaCommon.CommonProps.GUIType = string.IsNullOrEmpty(paramModel.SelectorGuiType) ? browserType : paramModel.SelectorGuiType;
                paramModel.LocateData.TryGetValue("title", out title);
                paramModel.LocateData.TryGetValue("url", out url);
            }
            else
            {
                rpaCommon.CommonProps.GUIType = string.IsNullOrEmpty(paramModel.SelectorGuiType) ? paramModel.browserType : paramModel.SelectorGuiType;
                title = string.IsNullOrEmpty(paramModel.webData) ? "" : JsonConvert.DeserializeObject<JObject>(paramModel.webData)["title"].ToString();
                url = string.IsNullOrEmpty(paramModel.webData) ? "" : JsonConvert.DeserializeObject<JObject>(paramModel.webData)["url"].ToString();
            }
            rpaCommon.CommonProps.QueryMode = paramModel.QueryMode;
            rpaCommon.CommonProps.CssSelector = paramModel.Selector;
            rpaCommon.CommonProps.DiscernHidden = paramModel.DiscernHidden.ToString().ToLower();
            rpaCommon.CommonProps.Version = GlobleConfigurations.CurrentSelectorVersion;
            paramModel.SelectorVersion = GlobleConfigurations.CurrentSelectorVersion;
            SelectorProps selectorProps = new SelectorProps();
            selectorProps.Accurate = "true";
            selectorProps.Name = "tab";
            selectorProps.Type = "web";
            selectorProps.Props = new List<Props>();
            selectorProps.Props.Add(BuildProps("title", title));
            selectorProps.Props.Add(BuildProps("url", url, accurate: "false"));
            paramModel.SelectorGuiType = rpaCommon.CommonProps.GUIType;
            List<SelectorProps> selectorPropsList = new List<SelectorProps>();
            selectorPropsList.Add(selectorProps);
            rpaCommon.Selector = JArray.Parse(JsonConvert.SerializeObject(selectorPropsList));
            return JsonConvert.SerializeObject(rpaCommon);
        }
        private Props BuildProps(string name, string value, string pattern = "equal", string accurate = "true")
        {
            Props props = new Props();
            props.Name = name;
            props.Value = value;
            props.Accurate = accurate;
            props.Pattern = pattern;
            return props;
        }
    }
}

