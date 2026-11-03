namespace II.RPA.Core.Contracts.Talker.Zdesktop.Message;

public class UserInfoMessage : IPayload
{
    public string Action { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;
}
