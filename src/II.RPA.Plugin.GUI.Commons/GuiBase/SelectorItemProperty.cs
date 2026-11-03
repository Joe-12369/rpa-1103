using System;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace II.RPA.Plugin.GUI.Commons
{
    [DebuggerDisplay("Name = {Name} Value = {OriginalValue}")]
    public class SelectorItemProperty
    {
        public SelectorItemProperty(string name, object value)
        {
            this.Name = name?.ToLower();
            this.Enabled = (name != "controltype");
            this.CanDisable = true;
            this.OriginalValue = value;
        }

        /// <summary>
        /// 是否可以禁用此条件
        /// </summary>
        [JsonProperty("candisable")]
        [JsonConverter(typeof(BoolConverter))]
        public bool CanDisable { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        [JsonProperty("enabled")]
        [JsonConverter(typeof(BoolConverter))]
        public bool Enabled { get; set; }
        /// <summary>
        /// Json对象
        /// </summary>

        [JsonIgnore]
        public JContainer JsonValue
        {
            get
            {
                if (!this.Enabled)
                {
                    return null;
                }
                else
                {
                    return JObject.FromObject(this);
                }
            }
        }

        /// <summary>
        /// 单属性名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 单属性值
        /// </summary>
        [JsonIgnore]
        public string Value
        {
            get
            {
                return (this.OriginalValue?.ToString()) ?? string.Empty;
            }
        }

        [JsonProperty("value")]
        public object OriginalValue { get; set; }
    }

    public class BoolConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(((bool)value) ? "true" : "false");
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return reader.Value.ToString().ToLower() == "true";
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(bool);
        }
    }
}