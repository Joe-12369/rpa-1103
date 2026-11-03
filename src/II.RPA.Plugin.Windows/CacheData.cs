using FlaUI.Core.AutomationElements;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Windows
{
    internal class CacheData
    {
        static CacheData()
        {
            NodeSelectorPropsCatch = new ConcurrentDictionary<AutomationElement, WindowsSelectorItem>();
            RootWndNodeProcessPropsCatch = new ConcurrentDictionary<IntPtr, List<WinSelectorItemProperty>>();
            GuiNodeCacahe = new ConcurrentDictionary<string, IGuiNode[]>();
        }
        internal static ConcurrentDictionary<IntPtr, List<WinSelectorItemProperty>> RootWndNodeProcessPropsCatch { get; set; }
        internal static ConcurrentDictionary<AutomationElement, WindowsSelectorItem> NodeSelectorPropsCatch { get; set; }
        internal static bool IsCanUpdateNodeSelectorPropsCatch { get; set; }
        /// <summary>
        /// 循环相似元素缓存
        /// </summary>
        internal static ConcurrentDictionary<string, IGuiNode[]> GuiNodeCacahe { get; set; }

        internal static List<string> BulidCacheSimilarSelector(List<IGuiNode> nodes, string fromSelector)
        {
            List<string> selectorList = new List<string>();
            string key = fromSelector.GetHashCode().ToString();
            CacheData.GuiNodeCacahe[key] = nodes.ToArray();
            var common = fromSelector.ToObject<RPACommon>();
            List<SelectorProps> propsList = common.Selector.ToObject<List<SelectorProps>>();
            if (propsList.Count == 0)
            {
                return new List<string>();
            }
            var similarKeyProp = new Props()
            {
                Name = ConstFields.PROP_NAME_SIMILARKEY,
                Value = key,
                Accurate = ConstFields.PROP_ACCURATE_FALSE
            };
            var similarIndexProp = new Props()
            {
                Name = ConstFields.PROP_NAME_SIMILARINDEX,
                Value = "-1",
                Accurate = ConstFields.PROP_ACCURATE_FALSE
            };
            propsList[0].Props.Add(similarKeyProp);
            propsList[0].Props.Add(similarIndexProp);
            for (int i = 0; i < nodes.Count; i++)
            {
                similarIndexProp.Value = i.ToString();
                common.Selector = propsList.ToObject<JArray>();
                var selector = common.ToJsonNoneFormating();
                ((UIANode)nodes[i]).cacheDataSelector = selector;
                selectorList.Add(selector);
            }
            return selectorList;
        }
        internal static List<string> BulidCacheSubElementSelector(List<IGuiNode> nodes, string fromSelector)
        {
            List<string> selectorList = new List<string>();
            string key = fromSelector.GetHashCode().ToString();
            CacheData.GuiNodeCacahe[key] = nodes.ToArray();
            var common = fromSelector.ToObject<RPACommon>();
            List<SelectorProps> propsList = common.Selector.ToObject<List<SelectorProps>>();
            if (propsList.Count == 0)
            {
                return new List<string>();
            }
            var subKeyProp = new Props()
            {
                Name = ConstFields.PROP_NAME_SUBKEY,
                Value = key,
                Accurate = ConstFields.PROP_ACCURATE_FALSE
            };
            var subIndexProp = new Props()
            {
                Name = ConstFields.PROP_NAME_SUBINDEX,
                Value = "-1",
                Accurate = ConstFields.PROP_ACCURATE_FALSE
            };
            propsList[0].Props.Add(subKeyProp);
            propsList[0].Props.Add(subIndexProp);
            for (int i = 0; i < nodes.Count; i++)
            {
                subIndexProp.Value = i.ToString();
                common.Selector = propsList.ToObject<JArray>();
                var selector = common.ToJsonNoneFormating();
                ((UIANode)nodes[i]).cacheDataSelector = selector;
                selectorList.Add(selector);
            }
            return selectorList;
        }

        internal static IGuiNode GetCacheElement(string key, int index, Action reBulidCache)
        {
            if (CacheData.GuiNodeCacahe.ContainsKey(key))
            {
                if (CacheData.GuiNodeCacahe[key].Count() <= index)
                {
                    return null;
                }
                return CacheData.GuiNodeCacahe[key][index];
            }
            else if (reBulidCache != null)
            {
                reBulidCache.Invoke();
                return GetCacheElement(key, index, null);
            }
            else
            {
                return null;
            }
        }
    }
}

