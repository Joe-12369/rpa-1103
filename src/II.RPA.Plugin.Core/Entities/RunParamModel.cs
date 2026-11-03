using II.RPA.Core.Runtime;
using Newtonsoft.Json;
using System;

namespace II.RPA.Plugin.Core.Entities
{
    /// <summary>
    /// Compatibility model for legacy plugin APIs. New runtime boundaries use
    /// <see cref="RuntimeRunParameters"/> directly.
    /// </summary>
    public struct RunParamModel
    {
        public int Timeout => ((RuntimeRunParameters)this).Timeout;

        [JsonProperty("start")]
        public string StartTimeStamp { get; set; }

        [JsonProperty("total")]
        public int TotalTime { get; set; }

        [JsonProperty("remain")]
        public int RemainTime { get; set; }

        [JsonProperty("attempt")]
        public int CurrentAttempt { get; set; }

        [JsonIgnore]
        public bool EnableAIRepair { get; set; }

        [JsonIgnore]
        public bool EnableNormal { get; set; }

        [JsonIgnore]
        public DateTime EntryTime { get; set; }

        public static RunParamModel DefaultValue => (RunParamModel)RuntimeRunParameters.DefaultValue;

        public static implicit operator RuntimeRunParameters(RunParamModel value)
        {
            return new RuntimeRunParameters
            {
                StartTimeStamp = value.StartTimeStamp,
                TotalTime = value.TotalTime,
                RemainTime = value.RemainTime,
                CurrentAttempt = value.CurrentAttempt,
                EnableAIRepair = value.EnableAIRepair,
                EnableNormal = value.EnableNormal,
                EntryTime = value.EntryTime
            };
        }

        public static implicit operator RunParamModel(RuntimeRunParameters value)
        {
            return new RunParamModel
            {
                StartTimeStamp = value.StartTimeStamp,
                TotalTime = value.TotalTime,
                RemainTime = value.RemainTime,
                CurrentAttempt = value.CurrentAttempt,
                EnableAIRepair = value.EnableAIRepair,
                EnableNormal = value.EnableNormal,
                EntryTime = value.EntryTime
            };
        }
    }
}
