using Newtonsoft.Json;

namespace II.RPA.Core.Contracts.Talker.Engine.Message;

public class EngineResponse
{
    [JsonProperty("code")]
    public string Code { get; set; } = string.Empty;

    [JsonProperty("info")]
    public string Info { get; set; } = string.Empty;

    [JsonProperty("message")]
    public string Message { get; set; } = string.Empty;

    public static EngineResponse Success(string message)
    {
        return new EngineResponse
        {
            Code = "0",
            Info = "success",
            Message = message,
        };
    }

    public static EngineResponse Error(string message, string code = "1")
    {
        return new EngineResponse
        {
            Code = code,
            Info = "fail",
            Message = message,
        };
    }
}
