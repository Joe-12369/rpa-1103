using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace II.RPA.Plugin.Core.Commons
{
    public static class GetPatternAttributeExtension
    {
        public static string GetPatternAttribute(this Pattern pattern)
        {
            FieldInfo fieldInfo = pattern.GetType().GetField(pattern.ToString());
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
        public string Type { get; set; } = "";
        [JsonProperty("accurate")]
        public string Accurate { get; set; } = "true";

        [JsonProperty("props")]
        public List<Props> Props { get; set; }

        public bool Equals(SelectorProps obj)
        {
            return this.Name == obj.Name &&
                this.Type == obj.Type &&
                this.Accurate == obj.Accurate &&
                this.Props.Count == obj.Props.Count &&
                this.Props.All(p1 => obj.Props.Any(p2 => p1.EqualsNameAndValue(p2)));
            //this.Props.SequenceEqual(obj.Props);
        }
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
        //纯文本内容
        [JsonProperty("textValue")]
        public string TextValue { get; set; } = "";
        //表达式内容
        [JsonProperty("expressionValue")]
        public string ExpressionValue { get; set; } = "";

        public bool Equals(Props obj)
        {
            if (!(obj is Props))
            {
                return false;
            }
            var prop = (Props)obj;
            return this.Name == prop.Name &&
                this.Value == prop.Value &&
                this.Pattern == prop.Pattern &&
                this.Accurate == prop.Accurate;
        }
        public bool EqualsNameAndValue(Props obj)
        {
            if (!(obj is Props))
            {
                return false;
            }
            var prop = (Props)obj;
            return this.Name == prop.Name &&
                this.Value == prop.Value;
        }
    }

    public class SelectorPropHelper
    {
        public static SelectorProps BuildSelectorNode(string nodeName, string accurate, string type, params Props[] props)
        {
            SelectorProps selectorProps = new SelectorProps()
            {
                Name = nodeName,
                Accurate = accurate,
                Type = type,
                Props = new List<Props>()
            };
            if (props?.Length > 0)
            {
                selectorProps.Props.AddRange(props);
            }
            return selectorProps;
        }
        public static Props BuildSelectorProp(string name, string value, string pattern, string accurate)
        {

            var prop = new Props
            {
                Name = name,
                Accurate = accurate,
                Pattern = pattern,
                Value = value
            };
            return prop;
        }
    }
}
