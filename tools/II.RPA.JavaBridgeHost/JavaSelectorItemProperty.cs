using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace II.RPA.JavaBridgeHost
{
    public class JavaSelectorItemProperty
    {
        public JavaSelectorItemProperty(string name, string value)
        {
            Name = name;
            Value = value;
            Enabled = (name != "ControlType");
            CanDisable = true;
        }

        /// <summary>
        /// 是否精准
        /// </summary>
        [JsonProperty("accurate")]
        public bool Accurate { get; set; }

        public bool CanDisable { get; set; }

        public bool Enabled { get; set; }

        [JsonIgnore]
        public JProperty json
        {
            get
            {
                if (!Enabled) return null;
                return new JProperty(Name, Value);
            }
        }

        public string SelectorVersion { get; set; }

        public string Name { get; set; }

        /// <summary>
        /// 匹配条件
        /// </summary>
        [JsonProperty("pattern")]
        public string Pattern { get; set; }

        public string Value { get; set; }

        public Props Convert2Props()
        {
            return new Props()
            {
                Accurate = this.Accurate ? ConstFields.PROP_ACCURATE_TURE : ConstFields.PROP_ACCURATE_FALSE,
                Name = this.Name,
                Pattern = this.Pattern,
                Value = this.Value
            };
        }

        public override string ToString()
        {
            return new JProperty(Name, Value).ToString();
        }
    }
}