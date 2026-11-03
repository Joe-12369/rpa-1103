using II.RPA.Logging;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Diagnostics;

namespace II.RPA.Core.Runtime;

/// <summary>
/// Runtime-neutral execution budget and retry state passed to RuntimeHost.
/// </summary>
public struct RuntimeRunParameters
{
    private static readonly Microsoft.Extensions.Logging.ILogger Logger = II.RPA.Logging.Logger.OfContext();

    public int Timeout
    {
        get
        {
            if (TotalTime <= 0 || string.IsNullOrEmpty(StartTimeStamp))
            {
                return 5000;
            }

            long.TryParse(StartTimeStamp, out var result);
            var currentTimestamp = Stopwatch.GetTimestamp();
            var milliseconds = (currentTimestamp - result) * 1000D / Stopwatch.Frequency;
            var value = TotalTime - Convert.ToInt32(milliseconds);
            if (value > 0)
            {
                Logger.LogDebug($"插件运行中，TotalTime={TotalTime}, StartTimeStamp={StartTimeStamp}, currentTimestamp={currentTimestamp}, calculated value={value}");
                return value;
            }

            Logger.LogDebug($"插件运行超时，TotalTime={TotalTime}, StartTimeStamp={StartTimeStamp}, currentTimestamp={currentTimestamp}, calculated value={value}");
            throw new RPAException(eErrorMessageType.Error_PluginRun_TimeOut);
        }
    }

    [JsonProperty("start")]
    public string? StartTimeStamp { get; set; }

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

    public static RuntimeRunParameters DefaultValue => new()
    {
        StartTimeStamp = Stopwatch.GetTimestamp().ToString(),
        RemainTime = -1,
        CurrentAttempt = 1,
        TotalTime = -1,
        EnableAIRepair = false,
        EnableNormal = false,
        EntryTime = DateTime.Now
    };
}
