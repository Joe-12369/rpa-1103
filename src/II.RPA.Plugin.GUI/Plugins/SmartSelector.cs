using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Params;
using II.RPA.Plugin.GUI.Utils;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class SmartSelector : GuiComponentBase<ParamSmartSelectorModel>
    {
        private ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.SmartSelector;

        protected override JMessage Execute(ParamSmartSelectorModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
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
                    FindOnce = false,
                    ExtendParam = hashtable,
                    MatchType = paramModel.MatchType,
                    isActive = false,
                    RunParam = paramModel.RunParam,
                    WindowInfo = paramModel.WindowInfo
                };
                element = GuiElement.FromSelector(param);
            }
            catch (RPAException ex)
            {
                Exception(ex, ref msg);
                return msg;
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
                var validAttrValues = GetValidAttributeInfo(paramModel.Selector, paramModel.ExecuteMode);
                //var validAttrValues = DeduplicateByAttValue(attrs);
                if (validAttrValues.Count == 0)
                {
                    msg.Info = "";
                }
                else
                {
                    string selectorItems = element.BuildSmartSelector(validAttrValues);
                    msg.Info = SelectorShell.ReplaceSelector(selectorItems, paramModel.Selector, true)?.ToJsonNoneFormating();
                }
            }
            catch (RPAException ex)
            {
                Exception(ex, ref msg);
                if (ex.ErrorMessageType == eErrorMessageType.Error_SmartSelector_Insufficient_Resource_Points)
                {
                    msg.Code = 0;
                    msg.UnifyCode = "0";
                }
                return msg;
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_SmartSelector_BuildFail, ref msg);
            }
            return msg;
        }

        protected override void ValidateParameter(ParamSmartSelectorModel paramModel) {
            try
            {
                return;
            }
            catch
            {
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }

        /// <summary>
        /// 获取有效属性信息
        /// </summary>
        /// <param name="selector">selector JSON字符串</param>
        /// <param name="executeMode">passive设为默认值，默认计费</param>
        /// <returns>返回元素属性列表的JSON字符串，失败返回空字符串</returns>
        internal List<Dictionary<string, object>> GetValidAttributeInfo(string selector, string executeMode)
        {
            try
            {
                // 步骤1：提取节点属性信息
                List<Dictionary<string, object>> nodeAttributes = ExtractNodeAttributesFromSelector(selector);
                if (nodeAttributes == null || nodeAttributes.Count == 0)
                {
                    logger?.LogWarning("GetValidAttributeInfo: 提取节点属性信息为空");
                    throw new RPAException(eErrorMessageType.Error_SmartSelector_BuildFail);
                }
                // 步骤2：算法属性值筛选
                return ZbrainAPIService.FilterAttributesByAlgorithm(nodeAttributes, executeMode: executeMode);
            }
            catch (RPAException rpaEx)
            {
                throw rpaEx;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "GetValidAttributeInfo: 获取有效属性信息异常");
                return new List<Dictionary<string, object>>();
            }
        }
        public List<string> DeduplicateByAttValue(List<Dictionary<string, object>> items)
        {
            if (items == null || items.Count == 0)
                return new List<string>();

            var seen = new HashSet<string>();
            return items
                .Where(item =>
                    item.TryGetValue("attValue", out var attValue) &&
                    item.TryGetValue("match", out var match) &&
                    attValue != null &&
                    match != null &&
                    !string.IsNullOrWhiteSpace(attValue.ToString()) &&
                    match.Equals("equal")
                    )
                .Select(item => item["attValue"].ToString())
                .Where(value => seen.Add(value))
                .ToList();
        }
        /// <summary>
        /// 解析selector JSON字符串，提取name不是"tab"的所有层级的属性
        /// </summary>
        /// <param name="selectorJson">完整的selector JSON字符串</param>
        /// <returns>转换后的元素属性字典列表，格式：[{"tagName":"tag", "attrs": {"属性名":"属性值"}}]</returns>
        internal List<Dictionary<string, object>> ExtractNodeAttributesFromSelector(string selectorJson)
        {
            List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();

            try
            {
                if (string.IsNullOrEmpty(selectorJson))
                {
                    logger?.LogWarning("ExtractNodeAttributesFromSelector: selectorJson为空");
                    return result;
                }

                JObject jsonObj = JObject.Parse(selectorJson);

                // 检查是否存在selector字段
                if (!jsonObj.ContainsKey("selector") || jsonObj["selector"] == null)
                {
                    logger?.LogWarning("ExtractNodeAttributesFromSelector: JSON中未找到selector字段");
                    return result;
                }

                JArray selectorArray = jsonObj["selector"] as JArray;
                if (selectorArray == null)
                {
                    logger?.LogWarning("ExtractNodeAttributesFromSelector: selector字段不是数组类型");
                    return result;
                }

                // 遍历selector数组，过滤掉name为"tab"的元素
                foreach (JObject element in selectorArray)
                {
                    if (element == null) continue;

                    // 获取name字段
                    string name = element["name"]?.ToString();
                    if (string.IsNullOrEmpty(name))
                    {
                        continue;
                    }

                    // 过滤掉name为"tab"的元素
                    if (name.Equals("tab", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // 构建元素字典
                    Dictionary<string, object> elementDict = new Dictionary<string, object>();
                    elementDict["tagName"] = name;

                    // 提取props中的name和value，构建attrs字典
                    Dictionary<string, object> attrs = new Dictionary<string, object>();

                    if (element.ContainsKey("props") && element["props"] != null)
                    {
                        JArray propsArray = element["props"] as JArray;
                        if (propsArray != null)
                        {
                            foreach (JObject prop in propsArray)
                            {
                                if (prop == null) continue;

                                string propName = prop["name"]?.ToString();
                                JToken propValue = prop["value"];

                                if (!string.IsNullOrEmpty(propName) && propValue != null && propName != "@index")
                                {
                                    // 将value转换为合适的类型
                                    object value = null;
                                    if (propValue.Type == JTokenType.String)
                                    {
                                        value = propValue.ToString();
                                    }
                                    else if (propValue.Type == JTokenType.Integer)
                                    {
                                        value = propValue.ToObject<int>();
                                    }
                                    else if (propValue.Type == JTokenType.Boolean)
                                    {
                                        value = propValue.ToObject<bool>();
                                    }
                                    else if (propValue.Type == JTokenType.Float)
                                    {
                                        value = propValue.ToObject<double>();
                                    }
                                    else
                                    {
                                        value = propValue.ToString();
                                    }

                                    attrs[propName] = value;
                                }
                            }
                        }
                    }

                    elementDict["attrs"] = attrs;
                    result.Add(elementDict);
                }

                logger?.LogDebug($"ExtractNodeAttributesFromSelector: 成功解析{result.Count}个元素");
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "ExtractNodeAttributesFromSelector: 解析selector JSON异常");
                result.Clear();
            }

            return result;
        }
    }
}

