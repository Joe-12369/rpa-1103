using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Entities;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WebBrowser.Common.Utils;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class GetSimilarElementList : GuiComponentBase<ParamGetSimilarElementListModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.GetSimilarElementList;
        protected override eErrorMessageType OperationTimeoutMsgType => eErrorMessageType.Error_GetSiblingsElements_OperationTomeout_Error;

        protected override JMessage Execute(ParamGetSimilarElementListModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            JMessage message = new JMessage();

            string parentSelector = string.Empty;

            string version = string.Empty;
            GuiElement parentElement = null;
            ParamGetSimilarElementListModel parentParamModel = null;
            string queryMode = "";
            try
            {
                ExtractViewData.GetQueryModeSelector(paramModel.Selector, out queryMode);
                if (!string.IsNullOrEmpty(paramModel.parentSelector) && paramModel.b_isAssociationParent)
                {
                    parentParamModel = paramModel.Clone();
                    parentParamModel.Selector = parentParamModel.parentSelector;
                    parentSelector = this.GetSelectorInfo(parentParamModel);
                }

                if (paramModel.b_isAssociationParent)
                {
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
                            Selector = parentSelector,
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
                        if (parentElement.IsGuiType(eGuiType.IE.ToString()) && queryMode == "xpath")
                        {
                            throw new RPAException(eErrorMessageType.Error_NotSupertXpath);
                        }
                        NotifyCoreServerElementFound(paramModel.RunUniqueID);
                    }
                }
                if (parentElement == null)
                {
                    if (GuiElement.selectorType_Web.Contains(paramModel.SelectorType.ToLower())
                        || GuiElement.selectorType_Win.Contains(paramModel.SelectorType.ToLower())
                        || paramModel.SelectorType.EqualIgnoreCase(eGuiType.SAP.ToString()))
                    {
                        try
                        {
                            Dictionary<string, object> hashtable = new Dictionary<string, object>{
                                {"processId",paramModel.processId },
                                {"resultAttrName", "uniqueId" },
                                {"startIndex",paramModel.startIndex },
                                {"endIndex",paramModel.endIndex },
                                {"isAntitone",paramModel.b_isAntitone}
                            };
                            paramModel.cvExecutionServiceURL = GlobleConfigurations.ZBrainServicePath + CVRouteConstFields.CV_EXECUTION_SERVICE;
                            var param = new GetElementFromSelectorParams
                            {
                                Selector = paramModel.Selector,
                                SelectorType = paramModel.SelectorType,
                                FindOnce = true,
                                CVExecutionServiceURL = paramModel.cvExecutionServiceURL,
                                ExtendParam = hashtable,
                                RunParam = paramModel.RunParam,
                                WindowInfo = paramModel.WindowInfo,
                                isMove = false,
                                VerifyMode = "selector"//相似元素列表获取不触发AI增强
                            };
                            param.ActionIEModeExcetpion = (ex) =>
                            {
                                if (ex.IsRPAException(eErrorMessageType.Error_EmbeddedIEMode, out _))
                                {
                                    if (queryMode == "xpath")
                                    {
                                        throw new RPAException(eErrorMessageType.Error_NotSupertXpath);
                                    }
                                }
                            };
                            var elementAll = GuiElement.QuerySelectorAll(param);
                            if (elementAll.Count == 0)
                                throw new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.GetElementSelectorAll);
                            elementAll = UtilsHelper.ConvertList(elementAll, paramModel.startIndex, paramModel.endIndex, paramModel.b_isAntitone);
                            if (elementAll.Count == 0)
                            {
                                message.Info = "[]";
                                return message;
                            }
                            message.Info = GetElementAttributeValue(elementAll, paramModel.SelectorType, paramModel.attributeName, paramModel.Selector, paramModel.b_isCompensationLink, paramModel.apiVersion);
                        }
                        catch (RPAException e)
                        {
                            Exception(e, e.ErrorMessageType, ref message);
                        }
                        catch (NotImplementedException nie)
                        {
                            Exception(nie, eErrorMessageType.Error_NotImplemented, ref message);
                        }
                        catch (Exception e)
                        {
                            Exception(e, eErrorMessageType.Error_UNKOWN, ref message);
                        }
                        return message;
                    }
                    else
                    {
                        Exception(eErrorMessageType.Error_ElementNonsupport_Error, ref message);
                        return message;
                    }
                }
                else
                {
                    var queryParamModel = new Commons.Params.ParamQueryTargetSelectorAllModel
                    {
                        selector = paramModel.Selector,
                        RelateElementId = parentElement.UserDefinedID,
                        ElementRelateMode = (ElementRelateMode)paramModel.isAssociationParent,
                        attributeName = paramModel.attributeName,
                        startIndex = paramModel.startIndex,
                        endIndex = paramModel.endIndex,
                        isCompensationLink = paramModel.b_isCompensationLink,
                        isAntitone = paramModel.b_isAntitone,
                        selectorVersion = string.IsNullOrEmpty(paramModel.SelectorVersion) ? GlobleConfigurations.CurrentSelectorVersion : paramModel.SelectorVersion,
                        discernHidden = paramModel.DiscernHidden
                    };
                    logger.LogInformation($"selector版本号：{queryParamModel.selectorVersion}");
                    var elementAll = parentElement.QueryTargetSelectorAll(queryParamModel);
                    logger.LogInformation($"elementAll数量：{elementAll.Count}");
                    if (elementAll.Count == 0)
                        throw new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.GetSimilarElementList);
                    elementAll = UtilsHelper.ConvertList(elementAll, paramModel.startIndex, paramModel.endIndex, paramModel.b_isAntitone);
                    if (elementAll.Count == 0)
                    {
                        message.Info = "[]";
                        return message;
                    }
                    message.Info = GetElementAttributeValue(elementAll, paramModel.SelectorType, paramModel.attributeName, paramModel.Selector, paramModel.b_isCompensationLink, paramModel.apiVersion);
                }
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_NotFoundElement, ref message);
                return message;
            }
            return message;
        }

        protected override void ValidateParameter(ParamGetSimilarElementListModel paramModel) {
            try
            {
                if (string.IsNullOrEmpty(paramModel.Selector))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
                if (paramModel.b_isAssociationParent)
                {
                    if (string.IsNullOrEmpty(paramModel.parentSelector))
                    {
                        throw new RPAException(eErrorMessageType.Error_ParamsError);
                    }
                }
                return;
            }
            catch (Exception ex)
            {
                logger.LogInformation($"GetSimilarElementList 参数格式不符合要求：{paramModel.ToJson()}");
                throw ex;
            }
        }
        private string GetElementAttributeValue(List<GuiElement> guiElements, string selectorType, string attributeName, string selector, bool isCompensationLink, string apiVersion)
        {
            var element = guiElements.FirstOrDefault();
            List<string> result = new List<string>();
            switch (attributeName)
            {
                case "selector":
                    result = element.GetSelectorAll(guiElements, selector);
                    for (int i = 0; i < result.Count; i++)
                    {
                        try
                        {
                            var common = result[i].ToObject<RPACommon>();
                            if (common?.CommonProps != null)
                            {
                                common.CommonProps.ExtendedTags |= ExtendedTags.SimilarElementItem;
                                result[i] = common.ToJsonNoneFormating();
                            }
                        }
                        catch (Exception ex)
                        {
                            Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "循环相似元素selector追加扩展标识失败");
                        }
                    }
                    break;
                case "bounding":
                    var rects = element.GetRegionAll(guiElements, 3);
                    foreach (var rect in rects)
                    {
                        Rect rc = new Rect();
                        rc.x = rect.X.ToString();
                        rc.y = rect.Y.ToString();
                        rc.width = rect.Width.ToString();
                        rc.height = rect.Height.ToString();
                        result.Add(JsonHelper.JsonByNewtonJson(rc));
                    }
                    break;
                case "gettext":
                    result = element.GetTextAll(guiElements);
                    break;
                case "code":
                    if (selectorType.ToLower() == "windows")
                    {
                        string[] strings = new string[guiElements.Count];
                        result = strings.ToList();
                    }
                    else
                    {
                        result = element.GetHtmlAll(guiElements);
                    }
                    break;
                case "absoluteurl":
                    if (selectorType.ToLower() == "windows")
                    {
                        string[] strings = new string[guiElements.Count];
                        result = strings.ToList();
                    }
                    else
                    {
                        var attrs = element.GetAttributeAll(guiElements, "absoluteurl", isCompensationLink, apiVersion);
                        if (isCompensationLink)
                        {
                            for (int i = 0; i < attrs.Count; i++)
                            {
                                attrs[i] = UtilsHelper.CompleteInformation(attrs[i]);
                            }
                        }
                        result = attrs;
                    }
                    break;
                case "pinselector":
                    result = guiElements.Select(guiEle => guiEle.PinSelector).ToList();
                    break;
                default:
                    result = element.GetAttributeAll(guiElements, attributeName, isCompensationLink, apiVersion);
                    break;
            }
            return result.ToJsonNoneFormating();
        }
    }
}
