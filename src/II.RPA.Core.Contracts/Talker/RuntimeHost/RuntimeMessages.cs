namespace II.RPA.Core.Contracts.Talker.RuntimeHost;

public static class RuntimeHostActions
{
    public const string TransferPluginAction = "transferPluginAction";
}

public interface IRuntimeMessage
{
    object Payload { get; }
}

public class RuntimeMessage<T> : IPayload, IRuntimeMessage where T : class
{
    public RuntimeMessage(T payload)
    {
        Payload = payload;
    }

    public T Payload { get; }

    object IRuntimeMessage.Payload => Payload;
}

public class RunPluginFromClientRequest : IPayload
{
    public string ClientId { get; set; } = string.Empty;

    public PayloadRunFromClient PayloadRunFromClient { get; set; } = new();

    public bool WithinZdesktop { get; set; }
}

public class PayloadRunFromClient : IPayload
{
    public string PackagePath { get; set; } = string.Empty;

    public string PluginAction { get; set; } = string.Empty;

    public string PluginParam { get; set; } = string.Empty;

    public string PluginVersion { get; set; } = string.Empty;
}

public class ClientResponse : IPayload
{
    public int Code { get; set; }

    public string Message { get; set; } = string.Empty;

    public string Result { get; set; } = string.Empty;

    public string PluginAction { get; set; } = string.Empty;
}

public class SharpRuntimeMessageProxy<T> : IPayload
{
    public SharpRuntimeMessageProxy(T payload)
    {
        Payload = payload;
    }

    public T Payload { get; }
}
