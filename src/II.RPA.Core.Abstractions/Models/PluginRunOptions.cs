namespace II.RPA.Core.Models;

public class PluginRunOptions
{
    public bool IsAsync { get; set; }

    public bool NeedUiThread { get; set; } = true;

    public static PluginRunOptions Default { get; } = new()
    {
        IsAsync = false,
        NeedUiThread = true
    };
}
