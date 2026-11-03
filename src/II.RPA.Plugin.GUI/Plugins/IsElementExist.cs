using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.Core.Entities;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Plugin.GUI.Commons;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class IsElementExist : GuiComponentBase<ParamIsElementExistModel>
    {
        private static ILogger logger = Logger.OfContext();
        private readonly GuiHighlightService guiHighlightService;

        public IsElementExist(GuiHighlightService guiHighlightService)
        {
            this.guiHighlightService = guiHighlightService;
        }

        protected override eComponentType ComponentType => eComponentType.IsElementExist;

        protected override void ValidateParameter(ParamIsElementExistModel paramModel) {
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
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"IsElementExist 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
        protected override async Task<JMessage> ExecuteAsync(ParamIsElementExistModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            JMessage msg = new JMessage();
            msg.Info = "0";

            GuiElement element = null;
            Dictionary<string, object> hashtable = new Dictionary<string, object>{
                        {"processId",paramModel.processId }
                    };
            try
            {
                paramModel.cvExecutionServiceURL = GlobleConfigurations.ZBrainServicePath + CVRouteConstFields.CV_EXECUTION_SERVICE;
                //如果是校验，走普通+智能修复，正常打包这个逻辑非必要，主要是为了给测试工具使用
                if (paramModel.isHighlighting)
                {
                    paramModel.MatchType = ElementMatchType.AiRepair | ElementMatchType.Normal;
                }
                var param = new GetElementFromSelectorParams
                {
                    Selector = paramModel.Selector,
                    SelectorType = paramModel.SelectorType,
                    FindOnce = true,
                    CVExecutionServiceURL = paramModel.cvExecutionServiceURL,
                    ExtendParam = hashtable,
                    isActive = paramModel.isHighlighting,
                    MatchType = paramModel.MatchType,
                    RunParam = paramModel.RunParam,
                    WindowInfo = paramModel.WindowInfo

                };
                if (paramModel.isHighlighting && IsSapCollectionSelector(paramModel.SelectorType, paramModel.Selector))
                {
                    hashtable["resultAttrName"] = "bounding";
                    ElementMatchType matchType = ElementMatchType.Normal;
                    string rectangles = GuiElement.FromSelectorAll(param, out matchType);
                    List<Rectangle> rects = string.IsNullOrEmpty(rectangles)
                        ? new List<Rectangle>()
                        : rectangles.ToObject<List<Rectangle>>() ?? new List<Rectangle>();
                    List<Rectangle> visibleRects = rects
                        .Where(a => a.Width > 0 && a.Height > 0)
                        .ToList();

                    int elementCount = GetExtendInt(hashtable, "sapCollectionElementCount", rects.Count);
                    int visibleElementCount = GetExtendInt(hashtable, "sapCollectionVisibleCount", visibleRects.Count);
                    if (elementCount < visibleElementCount)
                    {
                        elementCount = visibleElementCount;
                    }

                    if (elementCount <= 0)
                    {
                        throw new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.GetElementSelectorAll);
                    }

                    int hiddenElementCount = GetExtendInt(
                        hashtable,
                        "sapCollectionHiddenElementCount",
                        Math.Max(0, elementCount - visibleElementCount));
                    GlobleConfigurations.IsAiRepaireOfVerify = matchType == ElementMatchType.AiRepair;
                    if (visibleRects.Count == 0 || await guiHighlightService.HighlightAsync(visibleRects, true).ConfigureAwait(false))
                    {
                        msg.Info = "1";
                        msg.Extend = new JObject()
                        {
                            { "ElementCount", elementCount },
                            { "VisibleElementCount", visibleElementCount },
                            { "HiddenElementCount", hiddenElementCount }
                        }.ToString();
                    }
                    else
                    {
                        Exception(null, eErrorMessageType.Error_ElementValidationFailed, ref msg);
                    }

                    msg.Code = 0;
                    msg.UnifyCode = "0";
                    return msg;
                }

                element = GuiElement.FromSelector(param);
            }
            catch (Exception e)
            {
                Exception(e, eErrorMessageType.Error_ElementDeterminationFailed, ref msg);
            }
            if (!paramModel.isHighlighting && element != null)
            {
                //等待元素组件使用 choose
                if (paramModel.choose == 0)
                {
                    msg.Code = 0;//非高亮显示时为元素判断，不能返回非0
                    msg.UnifyCode = "0";//非高亮显示时为元素判断，不能返回非0
                    msg.Info = element.IsShow() ? "1" : "0";
                }
                //判断元素存在组件，IsDiscernHidden=false和choose=0不会同时存在
                else if (!paramModel.IsDiscernHidden)
                {
                    if (!element.IsShow() ||
                        (element.BoundingRectangle.Width <= 0 && element.BoundingRectangle.Height <= 0))
                    {
                        Exception(null, eErrorMessageType.Error_ElementDeterminationFailed, ref msg);
                    }
                    else
                    {
                        msg.Code = 0;
                        msg.UnifyCode = "0";
                        msg.Info = element == null ? "0" : "1";
                    }
                }
                else
                {
                    msg.Code = 0;//非高亮显示时为元素判断，不能返回非0
                    msg.UnifyCode = "0";//非高亮显示时为元素判断，不能返回非0
                    msg.Info = element == null ? "0" : "1";
                }
            }
            if (paramModel.isHighlighting && element != null)
            {
                if (await guiHighlightService.HighlightAsync(element).ConfigureAwait(false))//GuiElement.Highlighting(element)
                {
                    msg.Info = "1";
                    msg.Extend = new JObject() { { "ElementCount", 1 } }.ToString();
                }
                else
                    Exception(null, eErrorMessageType.Error_ElementValidationFailed, ref msg);
            }
            //msg.info = "1" or "0" or ""
            msg.Code = 0;
            msg.UnifyCode = "0";
            return msg;
        }

        private static bool IsSapCollectionSelector(string selectorType, string selector)
        {
            if (!string.Equals(selectorType, eGuiType.SAP.ToString(), StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(selector))
            {
                return false;
            }

            try
            {
                JToken token = JToken.Parse(selector);
                return ContainsSapCollectionProp(token);
            }
            catch
            {
                return false;
            }
        }

        private static bool ContainsSapCollectionProp(JToken token)
        {
            if (token == null)
            {
                return false;
            }

            if (token.Type == JTokenType.Object)
            {
                JObject obj = (JObject)token;

                string propName = GetStringValue(obj, "Name");
                string propValue = GetStringValue(obj, "Value");
                if (IsSapCollectionProp(propName, propValue))
                {
                    return true;
                }

                foreach (JProperty property in obj.Properties())
                {
                    if (property.Name.Equals("columnName", StringComparison.OrdinalIgnoreCase)
                        || property.Name.Equals("rowIndex", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                    if (property.Name.Equals("ext", StringComparison.OrdinalIgnoreCase)
                        && IsSapCollectionExt(property.Value?.ToString()))
                    {
                        return true;
                    }
                    if (ContainsSapCollectionProp(property.Value))
                    {
                        return true;
                    }
                }
            }
            else if (token.Type == JTokenType.Array)
            {
                foreach (JToken item in token.Children())
                {
                    if (ContainsSapCollectionProp(item))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsSapCollectionProp(string propName, string propValue)
        {
            if (string.IsNullOrEmpty(propName))
            {
                return false;
            }

            if (propName.Equals("columnName", StringComparison.OrdinalIgnoreCase)
                || propName.Equals("rowIndex", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return propName.Equals("ext", StringComparison.OrdinalIgnoreCase)
                && IsSapCollectionExt(propValue);
        }

        private static bool IsSapCollectionExt(string ext)
        {
            if (string.IsNullOrWhiteSpace(ext))
            {
                return false;
            }

            string value = ext.Replace(" ", "");
            return value.StartsWith("Column(", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("Row(", StringComparison.OrdinalIgnoreCase);
        }

        private static int GetExtendInt(Dictionary<string, object> extendParam, string key, int defaultValue)
        {
            if (extendParam == null || !extendParam.TryGetValue(key, out object value) || value == null)
            {
                return defaultValue;
            }

            try
            {
                return Convert.ToInt32(value);
            }
            catch
            {
                return defaultValue;
            }
        }

        private static string GetStringValue(JObject obj, string propertyName)
        {
            JProperty property = obj.Properties()
                .FirstOrDefault(p => p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
            return property?.Value?.ToString();
        }


        protected override string HandlingErrorMessage(JMessage resultMsg)
        {
            //GuiElement.ChromeRelease();
            return base.HandlingErrorMessage(resultMsg);
        }
    }
}

