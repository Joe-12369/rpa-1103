using System;
using System.Collections.Generic;
using System.Linq;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace II.RPA.JavaBridgeHost
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

        public string Convert2EditIndexFormat(string selector)
        {
            string retSelector = selector.Replace(ConstFields.SELECTOR_INDEX_IN_PARENT, ConstFields.SELECTOR_INDEX_IN_PARENT_NEW);
            return retSelector;
        }

        public string Convert2RunIndexFormat(string selector)
        {
            string retSelector = selector.Replace(ConstFields.SELECTOR_INDEX_IN_PARENT_NEW, ConstFields.SELECTOR_INDEX_IN_PARENT);
            return retSelector;
        }

        public ISelectorContext GetSelectorJson(string selectorJson)
        {
            string selector = this.Convert2RunIndexFormat(selectorJson);

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
                    return this.Convert2NewFormatJson(arrayObj);
                }
            }

            throw new NotSupportedException("The Java selector format is not supported!");
        }

        private SelectorContext Convert2NewFormatJson(JArray jArray)
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
                        IdParts idValue = ConstFields.GetRoleInfoInIdStr(nameToken.Value<string>());
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