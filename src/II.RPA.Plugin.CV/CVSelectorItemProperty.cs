using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using Newtonsoft.Json;

namespace II.RPA.Plugin.CV
{
    public class CVSelectorItemProperty : SelectorItemProperty
    {
        public CVSelectorItemProperty() : this(string.Empty, string.Empty)
        {
        }

        public CVSelectorItemProperty(string name, object value) : base(name, value)
        {
            this.Pattern = ConstFields.PATTERN_EQUAL;
            this.Accurate = false;
        }

        /// <summary>
        /// 是否精准
        /// </summary>
        [JsonProperty("accurate")]
        [JsonConverter(typeof(BoolConverter))]
        public bool Accurate { get; set; }

        /// <summary>
        /// 匹配条件
        /// </summary>
        [JsonProperty("pattern")]
        public string Pattern { get; set; }

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
    }
}