namespace II.RPA.Plugin.Abstractions;

public sealed class PluginExecutionContext
{
    public PluginExecutionContext(PluginRoute route, string jsonParam, string invocationId)
    {
        Route = route ?? throw new ArgumentNullException(nameof(route));
        JsonParam = jsonParam ?? throw new ArgumentNullException(nameof(jsonParam));

        if (string.IsNullOrWhiteSpace(invocationId)
            || !string.Equals(invocationId, invocationId.Trim(), StringComparison.Ordinal))
        {
            throw new ArgumentException("The invocation identifier cannot be empty or contain leading or trailing whitespace.", nameof(invocationId));
        }

        InvocationId = invocationId;
    }

    public PluginRoute Route { get; }

    public string JsonParam { get; }

    public string InvocationId { get; }
}
