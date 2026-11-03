namespace II.RPA.Plugin.Abstractions;

public interface IPluginAbility
{
    Task<string> RunAsync(PluginExecutionContext context);
}
