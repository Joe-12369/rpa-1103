namespace II.RPA.Plugin.Abstractions;

public interface IPluginInvoker
{
    Task<string> InvokeAsync(
        PluginRoute targetRoute,
        string jsonParam);
}
