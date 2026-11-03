using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.Core.Entities;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.Windows.OwnerDraw;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace II.RPA.Plugin.Windows
{
    public interface ISelectorContext
    {
        string Flag { get; }
        string Selector { get; }
        string SourceSelector { get; }
        string Version { get; }
        string SimilarKey { get; }
        int SimilarIndex { get; }
        string SubKey { get; set; }
        int SubIndex { get; set; }
        RunParamModel RunParam { get; set; }
        bool DiscernHidden { get; set; }
        bool GetFilterHidden(bool defVal);

    }

    public class SelectorContext : ISelectorContext
    {
        public SelectorContext()
        {
        }
        public string Flag { get; set; } = string.Empty;
        public string Selector { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string SimilarKey { get; set; } = string.Empty;
        public int SimilarIndex { get; set; } = -1;
        public string SubKey { get; set; } = string.Empty;
        public int SubIndex { get; set; } = -1;
        public RunParamModel RunParam { get; set; }
        public bool DiscernHidden { get; set; } = false;

        public string SourceSelector { get; set; } = string.Empty;

        public bool GetFilterHidden(bool defVal)
        {
            if (!double.TryParse(this.Version, out var version) || version < 1.3)
            {
                return defVal;
            }
            return !this.DiscernHidden;
        }
    }

    public class SelectorAdapter
    {
        public const string COMMON_PROP_TAG = "commonProps";
        public const string VERSTION_TAG = "Version";
        public const string APP_TAG = "app";
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

        public ISelectorContext GetSelectorJson(GetElementFromSelectorParams param)
        {
            string selector = this.Convert2RunIndexFormat(param.Selector);

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
                            string versionValue = verToken.Value<string>();

                            SelectorContext context = new SelectorContext();
                            context.SourceSelector = param.Selector;
                            JToken flagToken = null;
                            if ((commonProp as JObject).TryGetValue(ConstFields.PROP_NAME_FLAG, StringComparison.OrdinalIgnoreCase, out flagToken))
                            {
                                if (param.isActive)
                                    context.Flag = "Foreground";
                                else
                                    context.Flag = flagToken.Value<string>();
                            }

                            context.Selector = jobj.GetValue(ConstFields.SELECTOR_ELEMENT_SELECTOR_TAG).ToString();

                            var secProps = JsonConvert.DeserializeObject<List<SelectorProps>>(context.Selector);
                            if (secProps.Count > 0)
                            {
                                if (secProps[0].Props.Any(p => p.Name == ConstFields.PROP_NAME_SIMILARKEY))
                                {
                                    context.SimilarKey = secProps[0].Props.Where(p => p.Name == ConstFields.PROP_NAME_SIMILARKEY).First().Value;
                                }
                                if (secProps[0].Props.Any(p => p.Name == ConstFields.PROP_NAME_SIMILARINDEX))
                                {
                                    context.SimilarIndex = int.Parse(secProps[0].Props.Where(p => p.Name == ConstFields.PROP_NAME_SIMILARINDEX).First().Value);
                                }
                                if (secProps[0].Props.Any(p => p.Name == ConstFields.PROP_NAME_SUBKEY))
                                {
                                    context.SubKey = secProps[0].Props.Where(p => p.Name == ConstFields.PROP_NAME_SUBKEY).First().Value;
                                }
                                if (secProps[0].Props.Any(p => p.Name == ConstFields.PROP_NAME_SUBINDEX))
                                {
                                    context.SubIndex = int.Parse(secProps[0].Props.Where(p => p.Name == ConstFields.PROP_NAME_SUBINDEX).First().Value);
                                }
                            }
                            //secProps.RemoveAll(s => s.Accurate.EqualIgnoreCase("false"));
                            RemoveHeadDisableNode(secProps);
                            RemoveTailDisableNoe(secProps);
                            secProps.ForEach(s =>
                            {
                                // Keep the owner-draw sentinel even when somebody
                                // disables it in the selector editor.  The replay
                                // decoder will then reject the malformed virtual
                                // leaf instead of silently matching its host Pane.
                                s.Props.RemoveAll(p => p.Accurate.EqualIgnoreCase("false")
                                    && !IsOwnerDrawSentinel(p));
                            });
                            context.Selector = JsonConvert.SerializeObject(secProps);
                            context.Version = versionValue;
                            context.RunParam = param.RunParam;
                            context.DiscernHidden = param.DiscernHidden;
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

            throw new WindowsException(eErrorMessageType.Error_CheckSelector);
        }
        /// <summary>
        /// 删除尾部 Accurate = "false"的节点
        /// </summary>
        /// <param name="secProps"></param>
        private static void RemoveTailDisableNoe(List<SelectorProps> secProps)
        {
            for (int i = secProps.Count - 1; i >= 0; i--)
            {
                if (!secProps[i].Accurate.EqualIgnoreCase("false"))
                {
                    break;
                }
                if (ContainsOwnerDrawSentinel(secProps[i]))
                {
                    // Preserve the disabled virtual leaf so FindAll can fail closed.
                    break;
                }
                secProps.RemoveAt(i);
            }
        }

        private static bool ContainsOwnerDrawSentinel(SelectorProps selector)
        {
            return selector != null && selector.Props != null
                && selector.Props.Any(IsOwnerDrawSentinel);
        }

        private static bool IsOwnerDrawSentinel(Props property)
        {
            return property != null
                && OwnerDrawSelectorCodec.IsReservedSelectorPropertyName(
                    property.Name);
        }
        /// <summary>
        /// 删除头部 Accurate="false"的节点
        /// </summary>
        /// <param name="secProps"></param>
        private static void RemoveHeadDisableNode(List<SelectorProps> secProps)
        {
            // Retain the legacy iteration/removal behavior for ordinary selectors;
            // this integration must only add the owner-draw fail-closed guard.
            for (int i = 0; i < secProps.Count; i++)
            {
                if (!secProps[i].Accurate.EqualIgnoreCase("false"))
                {
                    break;
                }
                if (ContainsOwnerDrawSentinel(secProps[i]))
                {
                    // A malformed/moved virtual leaf must reach the owner-draw
                    // decoder so replay fails closed instead of matching the host.
                    break;
                }
                secProps.RemoveAt(i);
            }
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
            //SelectorProps rootProps = new SelectorProps() { Props = new List<Props>() };
            //rootProps.Name = ConstFields.SELECTOR_ELEMENT_ROOT_TAG;
            //rootProps.Type = "UIA3";
            //rootProps.Props = new List<Props>();
            //rootProps.Props.Add(new Props() { Name = ConstFields.SELECTOR_ROOT_FILE, Value = fileNameValue, Accurate = ConstFields.PROP_ACCURATE_FALSE, Pattern = ConstFields.PATTERN_EQUAL });
            //rootProps.Props.Add(new Props() { Name = ConstFields.SELECTOR_ROOT_PROCESSNAME, Value = processNameValue, Accurate = ConstFields.PROP_ACCURATE_FALSE, Pattern = ConstFields.PATTERN_EQUAL });
            //selectorPropItems.Add(rootProps);

            if (jArray.Count > 1)
            {
                for (int i = 1; i < jArray.Count; i++)
                {
                    JObject elementItem = jArray[i] as JObject;

                    SelectorProps selectorProps = new SelectorProps() { Props = new List<Props>() };
                    JToken nameToken = null;
                    selectorProps.Name = string.Empty;
                    selectorProps.Type = "UIA3";
                    if (elementItem.TryGetValue(ConstFields.SELECTOR_CONTROL_TYPE, StringComparison.OrdinalIgnoreCase, out nameToken))
                    {
                        selectorProps.Name = nameToken.Value<string>();
                    }


                    foreach (JProperty jp in elementItem.Properties())
                    {
                        if (!jp.Name.Equals(ConstFields.SELECTOR_CONTROL_TYPE, StringComparison.OrdinalIgnoreCase))
                        {
                            Props pItem = new Props();
                            pItem.Name = jp.Name.ToLower();
                            pItem.Value = jp.Value.ToString();
                            pItem.Pattern = ConstFields.PATTERN_EQUAL;
                            pItem.Accurate = ConstFields.PROP_ACCURATE_TURE;
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
            var rootProps = selectorPropItems.First();
            rootProps.Props.Add(new Props() { Name = ConstFields.SELECTOR_ROOT_FILE, Value = fileNameValue, Accurate = ConstFields.PROP_ACCURATE_TURE, Pattern = ConstFields.PATTERN_EQUAL });
            rootProps.Props.Add(new Props() { Name = ConstFields.SELECTOR_ROOT_PROCESSNAME, Value = processNameValue, Accurate = ConstFields.PROP_ACCURATE_TURE, Pattern = ConstFields.PATTERN_EQUAL });

            rpaRoot.Selector = JArray.FromObject(selectorPropItems);
            context.Selector = rpaRoot.Selector.ToString();
            context.Version = rpaRoot.CommonProps.Version;
            return context;
        }

    }
}
