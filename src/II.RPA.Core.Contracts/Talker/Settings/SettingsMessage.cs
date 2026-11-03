namespace II.RPA.Core.Contracts.Talker.Settings;

/// <summary>
/// Settings transported over the Worker protocol. Values are restricted to the
/// primitive kinds represented by settings_service.proto.
/// </summary>
public sealed class SettingsMessage : IPayload
{
    public Dictionary<string, object> Settings { get; set; } = new();
}
