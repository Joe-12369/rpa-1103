namespace II.RPA.Core.Contracts.Talker.Zdesktop;

public class ActionMessage : IPayload
{
    public string Action { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public string RequestId { get; set; } = string.Empty;

    public ZdesktopMessage Message => ZdesktopMessage.Parse(Payload);
}
