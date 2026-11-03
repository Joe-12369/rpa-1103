namespace II.RPA.Core.Contracts.Talker.Engine.Message.Action;

public class Stop
{
    public Stop(
        string? id = null,
        string? root = null,
        string? source = null,
        int desktopStatus = 0)
    {
        this.id = id;
        this.root = root;
        this.source = source;
        this.desktopStatus = desktopStatus;
    }

    public string? id { get; set; }
    public string? root { get; set; }
    public string? source { get; set; }
    public int desktopStatus { get; set; }
    public string? runtimeDirPath { get; set; }
}

public class StopMessage : EngineMessage<Stop>
{
    public StopMessage(Stop content)
        : base(EngineMessageActions.Stop, content)
    {
    }
}
