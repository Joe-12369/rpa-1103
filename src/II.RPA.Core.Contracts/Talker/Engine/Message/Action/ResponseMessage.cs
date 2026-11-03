using Newtonsoft.Json;

namespace II.RPA.Core.Contracts.Talker.Engine.Message.Action;

public class Response
{
    public const string SuccessStatus = "success";
    public const string ErrorStatus = "error";
    public const string StopStatus = "stopped";
    public const string StartedStatus = "started";
    public const string ConsoleLogRun = "runError";
    public const string ConsoleLogSystem = "system";

    private static readonly string[] EndStatuses = { ErrorStatus, StopStatus, SuccessStatus };

    [JsonProperty("status")]
    public string? Status { get; set; }

    [JsonProperty("msg")]
    public string? Message { get; set; }

    [JsonProperty("video")]
    public string? Vlog { get; set; }

    [JsonProperty("desktopStatus")]
    public int? DesktopStatus { get; set; }

    [JsonProperty("consoleLogShowLevel")]
    public string? ConsoleLogShowLevel { get; set; }

    [JsonProperty("text")]
    public string? Text { get; set; }

    [JsonProperty("id")]
    public string? Id { get; set; }

    [JsonIgnore]
    public bool? IsSuccess => Status == SuccessStatus;

    [JsonIgnore]
    public bool? IsStop => Status == StopStatus;

    [JsonIgnore]
    public bool? IsStarted => Status == StartedStatus;

    [JsonIgnore]
    public bool? IsError => Status == ErrorStatus;

    [JsonIgnore]
    public bool IsPending => !EndStatuses.Contains(Status);
}

public class ResponseMessage : EngineMessage<Response>
{
    public ResponseMessage()
        : base(string.Empty, new Response())
    {
    }

    public ResponseMessage(string action, Response content)
        : base(action, content)
    {
    }
}
