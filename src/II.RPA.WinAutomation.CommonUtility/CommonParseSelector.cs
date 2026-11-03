using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.WinAutomation.CommonUtility
{
    public static class CommonParseSelector
    {
        private static ILogger logger = Logger.OfContext();
        public static string ParseSelector(string selector, out string selectorType)
        {
            selector = RecoverViewData.RestoreViewData(selector);
            List<JObject> listTemp = new List<JObject>();
            selectorType = string.Empty;
            try
            {
                JArray ja = JArray.Parse(selector);
                foreach (JObject obj in ja)
                {
                    listTemp.Add(obj);
                    if (obj.ContainsKey("selector"))
                    {
                        selectorType = obj.Value<string>("selector").ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex.Message);
            }
            return selector;
        }
        public static string ParseSelectorV1(string selector, out string selectorType, out string version)
        {
            return ParseSelectorV1(selector, out selectorType, out version, out _);
        }
        public static string ParseSelectorV1(string selector, out string selectorType, out string version, out string processName)
        {
            return ParseSelectorV1(selector, out selectorType, out version, out processName, out _);
        }
        public static string ParseSelectorV1(string selector, out string selectorType, out string version, out string processName, out ExtendedTags extendedTags)
        {
            return ParseSelectorV1(selector, out selectorType, out version, out processName, out extendedTags, out _);
        }
        public static string ParseSelectorV1(string selector, out string selectorType, out string version, out string processName, out ExtendedTags extendedTags, out string discernHidden)
        {
            return ParseSelectorV1(selector, out selectorType, out version, out processName, out extendedTags, out discernHidden, out _);
        }
        public static string ParseSelectorV1(string selector, out string selectorType, out string version, out string processName, out ExtendedTags extendedTags, out string discernHidden, out CommonProps commonProps)
        {
            try
            {
                ExtractViewData.SelectCommonProps(selector, out selectorType, out version, out processName, out extendedTags, out discernHidden, out commonProps);
                if (version.StartsWith("1.") && selectorType != "None")
                {
                    return selector;
                }
                else
                {
                    selector = ParseSelector(selector, out selectorType);
                    return selector;
                }
            }
            catch (Exception e)
            {
                logger.LogWarning($"analysis error:{e.Message}{Environment.NewLine}{selector}");
                version = "";
                processName = "";
                extendedTags = ExtendedTags.None;
                discernHidden = "false";
                commonProps = null;
                return ParseSelector(selector, out selectorType);
            }
        }

    }
}
