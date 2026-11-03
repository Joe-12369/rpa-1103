using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace II.RPA.Plugin.GUI.Commons
{
    public class SelectorShell
    {
        private static ILogger logger = Logger.OfContext();

        private const string SELECTOR_PROP_NAME_ELEVISIBLE = "elevisible";
        public static RPACommon RepackShell(string selector, eGuiType guiType)
        {
            JArray selectorPropsArr = string.IsNullOrEmpty(selector.ToString()) ? new JArray() : JArray.Parse(selector);
            return RepackShell(selectorPropsArr, guiType);
        }
        public static RPACommon RepackShell(JArray selectorPropsArr, eGuiType guiType)
        {
            RPACommon common = new RPACommon();
            common.CommonProps = new CommonProps();
            common.CommonProps.GUIType = guiType.ToString();
            common.CommonProps.Version = GlobleConfigurations.CurrentSelectorVersion;
            common.Selector = selectorPropsArr;
            SetDiscernHidden(common);
            return common;
        }
        public static RPACommon RepackWithSourceShell(RPACommon newRPACommon, string srcSelector)
        {
            RPACommon srcRPACommon = ToRPACommon(srcSelector);
            if (newRPACommon == null || srcRPACommon == null)
            {
                return newRPACommon;
            }
            newRPACommon.CommonProps.isFindbyIndex = srcRPACommon.CommonProps.isFindbyIndex;
            newRPACommon.CommonProps.isSmartControl = srcRPACommon.CommonProps.isSmartControl;
            //有原selector,以用户选择DiscernHidden为主
            newRPACommon.CommonProps.DiscernHidden = srcRPACommon.CommonProps.DiscernHidden;
            RemoveLastNodeDiscernHidden(newRPACommon, out _);
            return newRPACommon;

        }
        /// <summary>
        /// 根据源selector给新selector加结构壳，
        /// </summary>
        /// <param name="newSelector"></param>
        /// <param name="srcSelector"></param>
        /// <param name="resetDiscernHidden">根据拾取元素重设DiscernHidden，false 继承srcSelector DiscernHidden</param>
        /// <returns></returns>
        public static RPACommon RepackWithSourceShell(string newSelector, string srcSelector, bool resetDiscernHidden)
        {
            RPACommon newRPACommon = ToRPACommon(newSelector);
            RPACommon srcRPACommon = ToRPACommon(srcSelector);
            if (newRPACommon == null || srcRPACommon == null)
            {
                return null;
            }
            newRPACommon.CommonProps.isFindbyIndex = srcRPACommon.CommonProps.isFindbyIndex;
            newRPACommon.CommonProps.isSmartControl = srcRPACommon.CommonProps.isSmartControl;
            newRPACommon.CommonProps.XpathSelector = srcRPACommon.CommonProps.XpathSelector;
            // “拾取相似元素”应继承源Selector版本，保持原流程的匹配语义：
            // 源1.0仍为1.0，源1.5仍为1.5，避免新拾取结果被默认值降级。
            newRPACommon.CommonProps.Version = srcRPACommon.CommonProps.Version;
            if (resetDiscernHidden)
                SetDiscernHidden(srcRPACommon);
            else
            {
                newRPACommon.CommonProps.DiscernHidden = srcRPACommon.CommonProps.DiscernHidden;
                RemoveLastNodeDiscernHidden(newRPACommon, out _);
            }
            //一般情况下，版本号可以统一处理，但最好可以再依赖包内部做控制，避免一些依赖包需要定制使用特殊版本号
            //newRPACommon.CommonProps.Version = GlobleConfigurations.CurrentSelectorVersion;

            return newRPACommon;
        }
        /// <summary>
        /// 根据源selector给新selector 加结构壳，
        /// </summary>
        /// <param name="selectorItemArr"></param>
        /// <param name="srcSelector"></param>
        /// <param name="resetDiscernHidden">根据拾取元素重设DiscernHidden，false 继承srcSelector DiscernHidden</param>
        /// <returns></returns>
        public static RPACommon RepackWithSourceShell(JArray selectorItemArr, string srcSelector, bool resetDiscernHidden)
        {
            RPACommon srcRPACommon = ToRPACommon(srcSelector);
            if (srcRPACommon == null)
            {
                return null;
            }
            srcRPACommon.CommonProps.filePath = string.Empty;
            srcRPACommon.CommonProps.ResDirPath = string.Empty;
            srcRPACommon.CommonProps.QueryMode = "default";
            srcRPACommon.CommonProps.CssSelector = string.Empty;
            srcRPACommon.CommonProps.ModifyTime = DateTime.Now.ToLongTimeStamp();
            srcRPACommon.CommonProps.Version = GlobleConfigurations.CurrentSelectorVersion;
            srcRPACommon.Selector = selectorItemArr;
            if (resetDiscernHidden)
                SetDiscernHidden(srcRPACommon);
            else
            {
                RemoveLastNodeDiscernHidden(srcRPACommon, out _);
            }
            return srcRPACommon;
        }
        public static RPACommon ReplaceSelector(string selectorItemArrStr, string srcSelector, bool resetDiscernHidden)
        {
            JArray selectorPropsArr = string.IsNullOrEmpty(selectorItemArrStr) ? new JArray() : JArray.Parse(selectorItemArrStr);
            return ReplaceSelector(selectorPropsArr, srcSelector, resetDiscernHidden);
        }
        public static RPACommon ReplaceSelector(JArray selectorItemArr, string srcSelector, bool resetDiscernHidden)
        {
            if (selectorItemArr == null || !selectorItemArr.Any())
            {
                return null;
            }


            RPACommon srcRPACommon = ToRPACommon(srcSelector);
            if (srcRPACommon == null)
            {
                return null;
            }
            srcRPACommon.Selector = selectorItemArr;

            if (resetDiscernHidden)
                SetDiscernHidden(srcRPACommon);
            else
            {
                RemoveLastNodeDiscernHidden(srcRPACommon, out _);
            }
            srcRPACommon.CommonProps.Version = GlobleConfigurations.CurrentSelectorVersion;
            srcRPACommon.CommonProps.ModifyTime = DateTime.Now.ToLongTimeStamp();

            logger.LogDebug($"进行selector转换xpath");
            string elementXpath = SelectorPathToXPath.Build(JsonConvert.DeserializeObject<List<SelectorProps>>(srcRPACommon.Selector.ToString())) ?? srcRPACommon.CommonProps.XpathSelector;
            srcRPACommon.CommonProps.XpathSelector = elementXpath;

            return srcRPACommon;
        }
        public static void SetDiscernHidden(RPACommon rpaCommon)
        {
            if (RemoveLastNodeDiscernHidden(rpaCommon, out var elevisibleProp))
            {
                rpaCommon.CommonProps.DiscernHidden = (elevisibleProp?.Value) != "false" ? "false" : "true";
            }
        }
        private static bool RemoveLastNodeDiscernHidden(RPACommon rpaCommon, out Props elevisibleProp)
        {
            elevisibleProp = null;
            try
            {
                JArray selectorArr = (JArray)rpaCommon.Selector;

                // 先取最后一个节点的 elevsible 值
                JObject lastSel = selectorArr.LastOrDefault() as JObject;
                if (lastSel != null)
                {
                    JArray lastProps = lastSel["props"] as JArray;
                    if (lastProps != null)
                    {
                        var lastElevisible = lastProps.FirstOrDefault(p =>
                            (string)p["name"] == SELECTOR_PROP_NAME_ELEVISIBLE);
                        if (lastElevisible != null)
                        {
                            elevisibleProp = lastElevisible.ToObject<Props>();
                        }
                    }
                }

                // 遍历所有节点，移除全部 elevsible 属性
                foreach (JObject selNode in selectorArr)
                {
                    JArray props = selNode["props"] as JArray;
                    if (props != null)
                    {
                        var toRemoveList = props.Where(p =>
                            (string)p["name"] == SELECTOR_PROP_NAME_ELEVISIBLE).ToList();

                        foreach (var toRemove in toRemoveList)
                        {
                            toRemove.Remove();
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "处理元素默认识别隐藏元素属性异常，视为不处理此逻辑");
                return false;
            }
        }
        private static RPACommon ToRPACommon(string slector)
        {
            try
            {
                return slector.ToObject<RPACommon>();
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "流程使用的selector可能是教老版本的selector,格式转换失败");
                return null;
            }
        }
    }
}

