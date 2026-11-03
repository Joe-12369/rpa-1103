using Newtonsoft.Json.Linq;

namespace II.RPA.Core.Contracts.Talker.Engine.Message.Action;

public class Trigger
{
    public Trigger(
        object? appId = null,
        JArray? appIdParams = null,
        int? status = null,
        string? msg = null,
        string? message = null)
    {
        this.appId = appId;
        this.appIdParams = appIdParams;
        this.status = status;
        this.msg = msg;
        this.message = message;
    }

    public object? appId { get; set; }
    public JArray? appIdParams { get; set; }
    public int? status { get; set; }
    public string? msg { get; set; }
    public string? message { get; set; }
}

public class TriggerMessage : EngineMessage<Trigger>
{
    public TriggerMessage(Trigger content)
        : base(EngineMessageActions.Trigger, content)
    {
    }
}
