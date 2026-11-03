using Newtonsoft.Json;

namespace II.RPA.Core.Contracts.Talker.Zdesktop.Message;

public class StartZdesktopMessage
{
    [JsonProperty("exe_path")]
    public string? ExePath { get; set; }

    [JsonProperty("command_line_json")]
    public string? CommandLine { get; set; }

    [JsonProperty("mode")]
    public int Mode { get; set; }

    [JsonProperty("windowmode")]
    public int? WindowMode { get; set; }
}

public class StartZdesktopAction : ZdesktopMessage<StartZdesktopMessage>
{
    public StartZdesktopAction(StartZdesktopMessage content)
        : base(ZdesktopMessageActions.Start, content)
    {
    }
}
