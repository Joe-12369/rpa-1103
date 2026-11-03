using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.SAP
{
    public class SapSelectorItemProperty : SelectorItemProperty
    {
        public SapSelectorItemProperty() : this(string.Empty, string.Empty)
        {
        }
        public SapSelectorItemProperty(string name, object value) : base(name, value)
        {
            this.Pattern = "equal";
            this.Accurate = true;
        }

        /// <summary>
        /// 匹配条件
        /// </summary>
        [JsonProperty("pattern")]
        public string Pattern { get; set; }

        /// <summary>
        /// 是否精准
        /// </summary>
        [JsonProperty("accurate")]
        [JsonConverter(typeof(BoolConverter))]
        public bool Accurate { get; set; }

        public Props Convert2Props()
        {
            return new Props()
            {
                Accurate = this.Accurate ? "true" : "false",
                Name = this.Name,
                Pattern = this.Pattern,
                Value = this.Value
            };
        }
    }
}
