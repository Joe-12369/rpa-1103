using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Params
{
    public class ParamElementExecutejsModel : ParamBase
    {
        public ParamElementExecutejsModel()
        {
            ExecutionWorld = ExecutionWorld.MAIN;
        }
        public string scriptCode { get; set; }
        public string @params { get; set; }
        public string webData { get; set; }
        [JsonIgnore]
        public string ExecutionEnvironment { get => ExecutionWorld.ToString(); }
        public bool isAsyncCode { get; set; }
        public float waitResultTimeout { get; set; }
        public bool catchException { get; set; }

        /// <summary>
        /// tabId插件内的流转参数
        /// </summary>
        public int TabId { get; set; } = -1;


        [JsonProperty("executionEnvironment")]
        [JsonConverter(typeof(ExecutionWorldConverter))]
        public ExecutionWorld ExecutionWorld { get; set; }
    }

    public enum ExecutionWorld
    {
        ISOLATED,
        MAIN
    }

    public class ExecutionWorldConverter : JsonConverter<ExecutionWorld>
    {
        public override ExecutionWorld ReadJson(JsonReader reader, Type objectType, ExecutionWorld existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var value = reader.Value as string;
            if (string.IsNullOrEmpty(value))
            {
                throw new JsonSerializationException("ExecutionWorld value is null or empty.");
            }

            switch (value.ToUpperInvariant())
            {
                case "MAIN":
                    return ExecutionWorld.MAIN;
                case "EXTENSION":
                    return ExecutionWorld.ISOLATED;
                default:
                    throw new JsonSerializationException($"Unknown execution world: {reader.Value}");
            }
        }

        public override void WriteJson(JsonWriter writer, ExecutionWorld value, JsonSerializer serializer)
        {
            switch (value)
            {
                case ExecutionWorld.MAIN:
                    writer.WriteValue("MAIN");
                    break;
                case ExecutionWorld.ISOLATED:
                    writer.WriteValue("EXTENSION");
                    break;
                default:
                    throw new JsonSerializationException($"Unknown ExecutionWorld value: {value}");
            }
        }
    }

}
