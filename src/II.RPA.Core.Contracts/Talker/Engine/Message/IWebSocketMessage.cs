namespace II.RPA.Core.Contracts.Talker.Engine.Message;

public interface IWebSocketMessage
{
    string RequestId { get; set; }

    string? ResponseFor { get; set; }

    string MessageType { get; set; }
}
