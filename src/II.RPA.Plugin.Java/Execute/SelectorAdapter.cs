using System;
using System.Collections.Generic;
using System.Linq;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace II.RPA.Plugin.Java.Execute
{
    public interface ISelectorContext
    {
        string Flag { get; }
        string Selector { get; set; }

        string Version { get; set; }
    }

    public class SelectorAdapter
    {
        public const string APP_TAG = "app";
        public const string COMMON_PROP_TAG = "commonProps";
        public const string VERSTION_TAG = "Version";
        public const string WIN_CTRL = "winCtrl";

        public static string Convert2EditIndexFormat(string selector)
        {
            string retSelector = selector.Replace(ConstFields.SELECTOR_INDEX_IN_PARENT, ConstFields.SELECTOR_INDEX_IN_PARENT_NEW);
            return retSelector;
        }

        public static string Convert2RunIndexFormat(string selector)
        {
            string retSelector = selector.Replace(ConstFields.SELECTOR_INDEX_IN_PARENT_NEW, ConstFields.SELECTOR_INDEX_IN_PARENT);
            return retSelector;
        }

        public static ISelectorContext GetSelectorJson(string selectorJson)
        {
            string selector = Convert2RunIndexFormat(selectorJson);

            JToken token = JToken.Parse(selector);
            if (token is JObject jobj)
            {
                JToken commonProp = null;
                if (jobj.TryGetValue(COMMON_PROP_TAG, StringComparison.OrdinalIgnoreCase, out commonProp))
                {
                    JToken verToken = null;
                    if ((commonProp as JObject).TryGetValue(VERSTION_TAG, StringComparison.OrdinalIgnoreCase, out verToken))
                    {
                        if (!string.IsNullOrEmpty(verToken.Value<string>()))
                        {
                            SelectorContext context = new SelectorContext()
                            {
                                Version = verToken.Value<string>()
                            };

                            JToken flagToken = null;
                            if ((commonProp as JObject).TryGetValue(ConstFields.PROP_NAME_FLAG, StringComparison.OrdinalIgnoreCase, out flagToken))
                            {
                                context.Flag = flagToken.Value<string>();
                            }
                            context.Selector = jobj.GetValue(ConstFields.SELECTOR_ELEMENT_SELECTOR_TAG).ToString();
                            #region 删除第一级节点 Accurate="false"的节点
                            var secProps = JsonConvert.DeserializeObject<List<SelectorProps>>(context.Selector);
                            secProps.RemoveAll(s => s.Accurate.EqualIgnoreCase("false"));
                            secProps.ForEach(s =>
                            {
                                s.Props.RemoveAll(p => p.Accurate.EqualIgnoreCase("false"));
                            });
                            #endregion
                            context.Selector = JsonConvert.SerializeObject(secProps);
                            return context;
                        }
                    }
                }
            }
            else
            {
                if (token is JArray arrayObj)
                {
                    return Convert2NewFormatJson(arrayObj);
                }
            }

            throw new NotSupportedException("The Java selector format is not supported!");
        }

        private static SelectorContext Convert2NewFormatJson(JArray jArray)
        {
            RPACommon rpaRoot = new RPACommon();
            string fileNameValue = string.Empty;
            string processNameValue = string.Empty;
            var rootObj = jArray[0] as JObject;

            CommonProps commonProps = new CommonProps();
            JToken fileName = null;
            JToken processName = null;
            JToken subType = null;
            JToken filePath = null;
            JToken captureMode = null;
            JToken flag = null;
            SelectorContext context = new SelectorContext();
            if (rootObj.TryGetValue(ConstFields.SELECTOR_ROOT_FILE, StringComparison.OrdinalIgnoreCase, out fileName))
            {
                fileNameValue = fileName.Value<string>();
            }

            if (rootObj.TryGetValue(ConstFields.SELECTOR_ROOT_PROCESSNAME, StringComparison.OrdinalIgnoreCase, out processName))
            {
                processNameValue = processName.Value<string>();
            }

            if (rootObj.TryGetValue(ConstFields.SELECTOR_ROOT_SELECTOR, StringComparison.OrdinalIgnoreCase, out subType))
            {
                commonProps.GUIType = Enum.Parse(typeof(eGuiType), subType.Value<string>()).ToString();
            }

            if (rootObj.TryGetValue("filePath", StringComparison.OrdinalIgnoreCase, out filePath))
            {
                commonProps.filePath = filePath.Value<string>();
            }

            if (rootObj.TryGetValue("captureMode", StringComparison.OrdinalIgnoreCase, out captureMode))
            {
                commonProps.captureMode = captureMode.Value<int>();
            }

            if (rootObj.TryGetValue(ConstFields.PROP_NAME_FLAG, StringComparison.OrdinalIgnoreCase, out flag))
            {
                context.Flag = flag.Value<string>();
            }

            commonProps.Version = ConstFields.SELECTOR_OLD_FORMAT_VERSION;

            rpaRoot.CommonProps = commonProps;

            List<SelectorProps> selectorPropItems = new List<SelectorProps>();

            if (jArray.Count > 1)
            {
                for (int i = 1; i < jArray.Count; i++)
                {
                    JObject elementItem = jArray[i] as JObject;

                    SelectorProps selectorProps = new SelectorProps() { Props = new List<Props>() };
                    JToken nameToken = null;
                    selectorProps.Name = string.Empty;
                    if (elementItem.TryGetValue(ConstFields.SELECTOR_ID, StringComparison.OrdinalIgnoreCase, out nameToken))
                    {
                        IdParts idValue = GetRoleInfoInIdStr(nameToken.Value<string>());
                        selectorProps.Name = idValue.Role;

                        if (!string.IsNullOrEmpty(idValue.Name))
                        {
                            Props pItem = new Props();
                            pItem.Name = ConstFields.SELECTOR_NAME;
                            pItem.Value = idValue.Name;
                            pItem.Pattern = ConstFields.PATTERN_EQUAL;
                            pItem.Accurate = ConstFields.PROP_ACCURATE_TURE;
                            selectorProps.Props.Add(pItem);
                        }

                        if (!string.IsNullOrEmpty(idValue.Index))
                        {
                            Props pItem = new Props();
                            pItem.Name = ConstFields.SELECTOR_INDEX_IN_PARENT;
                            pItem.Value = idValue.Index;
                            pItem.Pattern = ConstFields.PATTERN_EQUAL;
                            pItem.Accurate = ConstFields.PROP_ACCURATE_TURE;
                            selectorProps.Props.Add(pItem);
                        }
                    }

                    foreach (JProperty jp in elementItem.Properties())
                    {
                        if (!selectorProps.Props.Exists(x => x.Name.EqualIgnoreCase(jp.Name)) &&
                            !jp.Name.EqualIgnoreCase(ConstFields.SELECTOR_ID) &&
                            !jp.Name.EqualIgnoreCase(ConstFields.SELECTOR_ROLE))
                        {
                            Props pItem = new Props();
                            pItem.Name = jp.Name.ToLower();
                            pItem.Value = jp.Value.ToString();
                            pItem.Pattern = ConstFields.PATTERN_EQUAL;
                            pItem.Accurate = ConstFields.PROP_ACCURATE_FALSE;
                            selectorProps.Props.Add(pItem);
                        }
                    }
                    selectorPropItems.Add(selectorProps);
                }

                var flagProp = selectorPropItems[1].Props.FirstOrDefault(x => x.Name.Equals(ConstFields.PROP_NAME_FLAG, StringComparison.OrdinalIgnoreCase));
                if (flagProp != null)
                {
                    context.Flag = flagProp.Value;
                }
            }
            rpaRoot.Selector = JArray.FromObject(selectorPropItems);
            context.Selector = rpaRoot.Selector.ToString();
            context.Version = rpaRoot.CommonProps.Version;
            return context;
        }

        public static IdParts GetRoleInfoInIdStr(string id)
        {
            if (id.Contains("#"))
            {
                string[] items = id.Split(new char[] { '#' }, StringSplitOptions.RemoveEmptyEntries);
                return new IdParts() { Role = items[0], Name = string.Join(string.Empty, items.Skip(1)) };
            }
            else if (id.Contains("(") && id.EndsWith(")"))
            {
                string[] items = id.Split(new char[] { '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
                return new IdParts() { Role = items[0], Index = items[1] };
            }
            else
            {
                return new IdParts() { Role = id };
            }
        }

        /// <summary>
        /// 根据两个selector 计算相似元素selector。
        /// 规则：
        /// - commonProps.GUIType 不同 -> 抛出 Error_ElementPlacement_Error
        /// - selector 数组长度不同 -> 抛出 Error_ElementLevel_Error
        /// - 任一对应节点 name 不同 -> 抛出 Error_ElementNode_Error
        /// - 仅保留两个节点中“同名”的 props；若同名但值/模式/准确度不同，仅将该项 accurate 置为 "false"
        /// 返回完整的 selector 字符串
        /// </summary>
        public static string BuildSimilarSelector(string selectorJson1, string selectorJson2)
        {
            if (string.IsNullOrWhiteSpace(selectorJson1) || string.IsNullOrWhiteSpace(selectorJson2))
            {
                throw new ArgumentException("selector json is null or empty.");
            }

            var s1 = JsonConvert.DeserializeObject<RPACommon>(selectorJson1);
            var s2 = JsonConvert.DeserializeObject<RPACommon>(selectorJson2);


            var guiType1 = s1?.CommonProps?.GUIType ?? string.Empty;
            var guiType2 = s2?.CommonProps?.GUIType ?? string.Empty;

            if (!guiType1.Equals(guiType2, StringComparison.OrdinalIgnoreCase))
            {
                throw new RPAException(eErrorMessageType.Error_ElementPlacement_Error);
            }

            var list1 = s1?.Selector?.ToObject<List<SelectorProps>>() ?? new List<SelectorProps>();
            var list2 = s2?.Selector?.ToObject<List<SelectorProps>>() ?? new List<SelectorProps>();

            if (list1.Count != list2.Count)
            {
                throw new RPAException(eErrorMessageType.Error_ElementLevel_Error);
            }

            var resultList = new List<SelectorProps>(list1.Count);

            for (int i = 0; i < list1.Count; i++)
            {
                var n1 = list1[i] ?? new SelectorProps { Props = new List<Props>() };
                var n2 = list2[i] ?? new SelectorProps { Props = new List<Props>() };

                if (!string.Equals(n1.Name, n2.Name, StringComparison.Ordinal))
                {
                    throw new RPAException(eErrorMessageType.Error_ElementNode_Error);
                }

                var resNode = new SelectorProps
                {
                    Name = n1.Name,
                    Type = n1.Type,
                    Accurate = n1.Accurate,
                    Props = new List<Props>()
                };

                var n2PropsByName = (n2.Props ?? new List<Props>()).ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

                foreach (var p1 in n1.Props ?? Enumerable.Empty<Props>())
                {
                    if (!n2PropsByName.TryGetValue(p1.Name, out var p2))
                    {
                        // 只保留两者共同拥有（同名）的 props
                        continue;
                    }

                    // 基于 p1 复制
                    var pr = new Props
                    {
                        Name = p1.Name,
                        Value = p1.Value,
                        Pattern = p1.Pattern,
                        Accurate = p1.Accurate,
                        TextValue = p1.TextValue,
                        ExpressionValue = p1.ExpressionValue
                    };

                    // 如果同名但任一字段不同，则仅将 accurate 置为 false
                    bool same =
                        string.Equals(p1.Value, p2.Value) &&
                        string.Equals(p1.Pattern, p2.Pattern) &&
                        string.Equals(p1.Accurate, p2.Accurate);

                    if (!same)
                    {
                        pr.Accurate = ConstFields.PROP_ACCURATE_FALSE;

                        //if (i - 1 >= 0 && resultList.Count >= i)
                        //{
                        //    var parent = resultList[i - 1];
                        //    if (parent?.Props != null)
                        //    {
                        //        var parentIndex = parent.Props.FirstOrDefault(x =>
                        //            x.Name.Equals(ConstFields.SELECTOR_INDEX_IN_PARENT, StringComparison.OrdinalIgnoreCase)
                        //            || x.Name.Equals(ConstFields.SELECTOR_INDEX_IN_PARENT_NEW, StringComparison.OrdinalIgnoreCase));
                        //        if (parentIndex != null)
                        //        {
                        //            parentIndex.Accurate = ConstFields.PROP_ACCURATE_TURE;
                        //        }
                        //    }
                        //}

                    }


                    resNode.Props.Add(pr);
                }

                resultList.Add(resNode);
            }

            // 结果沿用第一个的 commonProps，仅替换 selector
            s1.Selector = JArray.FromObject(resultList);
            return JsonConvert.SerializeObject(s1);
        }

        public static string BuildSimilarSelector(string selector, List<string> nodePathIndex)
        {
            if (string.IsNullOrWhiteSpace(selector))
                throw new ArgumentException("selector json is null or empty.");
            if (nodePathIndex == null || nodePathIndex.Count < 1)
                throw new ArgumentException("nodePathIndex is null or empty.");

            var rpa = JsonConvert.DeserializeObject<RPACommon>(selector);
            if (rpa == null)
                throw new ArgumentException("selector json is invalid.");

            var nodes = rpa.Selector?.ToObject<List<SelectorProps>>() ?? new List<SelectorProps>();
            if (nodes.Count == 0)
                return selector;

            // 首节点不处理；后续节点需要 index
            if (nodePathIndex.Count < nodes.Count - 1)
                throw new ArgumentException("nodePathIndex count does not match selector depth (excluding root).");

            for (int i = 1; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node == null)
                    continue;

                var props = node.Props ?? new List<Props>();

                var cellIndexProp = props.FirstOrDefault(p =>
                  p.Name.Equals(ConstFields.ROLE_VALUE_TABLE_CELLINDEX, StringComparison.OrdinalIgnoreCase));
                if (cellIndexProp == null)
                {
                   var indexProp = props.FirstOrDefault(p =>
                   p.Name.Equals(ConstFields.SELECTOR_INDEX_IN_PARENT, StringComparison.OrdinalIgnoreCase) ||
                   p.Name.Equals(ConstFields.SELECTOR_INDEX_IN_PARENT_NEW, StringComparison.OrdinalIgnoreCase));

                    if (indexProp == null)
                    {
                        indexProp = new Props
                        {
                            Name = ConstFields.SELECTOR_INDEX_IN_PARENT,
                            Pattern = ConstFields.PATTERN_EQUAL,
                            Accurate = ConstFields.PROP_ACCURATE_TURE
                        };
                    }

                    indexProp.Value = nodePathIndex[i];
                    indexProp.TextValue = nodePathIndex[i];
                    indexProp.ExpressionValue = nodePathIndex[i];
                    indexProp.Pattern = ConstFields.PATTERN_EQUAL;
                    indexProp.Accurate = ConstFields.PROP_ACCURATE_TURE;

                    node.Props = new List<Props> { indexProp };
                }
                else
                {

                    cellIndexProp.Value = nodePathIndex[i];
                    cellIndexProp.TextValue = nodePathIndex[i];
                    cellIndexProp.ExpressionValue = nodePathIndex[i];
                    cellIndexProp.Pattern = ConstFields.PATTERN_EQUAL;
                    cellIndexProp.Accurate = ConstFields.PROP_ACCURATE_TURE;

                    node.Props = new List<Props> { cellIndexProp };

                }

            }

            rpa.Selector = JArray.FromObject(nodes);
            return JsonConvert.SerializeObject(rpa);
        }

    }

    public class SelectorContext : ISelectorContext
    {
        public SelectorContext()
        {
        }

        public string Flag { get; set; } = string.Empty;

        public string Selector { get; set; } = string.Empty;

        public string Version { get; set; } = string.Empty;
    }

    public class IdParts
    {
        public string Role { get; set; }

        public string Name { get; set; }

        public string Index { get; set; }
    }
}