using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace II.RPA.JavaBridgeHost
{
    public static class GetPatternAttributeExtension
    {
        public static string GetPatternAttribute(this Pattern pattern)
        {
            FieldInfo fieldInfo = Pattern.EQUAL.GetType().GetField(Pattern.EQUAL.ToString());
            DescriptionAttribute patternAttribute = fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as DescriptionAttribute;
            return patternAttribute.Description;
        }
    }

    public enum Pattern : int
    {
        [Description("equal")]
        EQUAL,//相等
        [Description("contain")]
        CONTAIN,//包含
        [Description("begin")]
        BEGIN,//开头
        [Description("end")]
        END,//结尾
        [Description("regular")]
        REGULAR,//正则
    }
    public class SelectorProps
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("type")]
        [JsonIgnore]
        public string Type { get; set; }
        [JsonProperty("props")]
        public List<Props> Props { get; set; }
        [JsonProperty("accurate")]
        public string Accurate { get; set; } = "true";
    }
    public class Props
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("value")]
        public string Value { get; set; } = "";
        [JsonProperty("pattern")]
        public string Pattern { get; set; }
        [JsonProperty("accurate")]
        public string Accurate { get; set; }
    }
}
