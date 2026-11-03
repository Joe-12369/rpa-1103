namespace II.RPA.Core.Contracts.Talker.Engine.Message.Action;

public class PingMessage : EngineMessage<object>
{
    public PingMessage()
        : base(EngineMessageActions.Ping, new object())
    {
    }
}
