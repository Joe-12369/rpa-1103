namespace II.RPA.Core.Contracts.Talker.Engine;

public enum ActionEventType
{
    Unknown = 0,
    Start = 1,
    Done = 2,
    Error = 3,
    Finish = 4,
}

public class ActionEvent : IPayload
{
    public string Action { get; set; } = string.Empty;

    public string RequestId { get; set; } = string.Empty;

    public ActionEventType Type { get; set; }
}
