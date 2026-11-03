using II.RPA.Core.Contracts.Talker.Engine.Message;

namespace II.RPA.Core.Contracts.Talker.Engine;

public class ActionMessage : IPayload
{
    public string Action { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public bool WithinZdesktop { get; set; }

    public string InnerAction { get; set; } = string.Empty;

    public EngineMessage Message => EngineMessage.Parse(Payload);
}
