namespace II.RPA.Core.Contracts.Talker.Engine.Message.Action;

public class InitBrainPlugins
{
    public InitBrainPlugins(string packagePath, string source = EngineMessage.BotSource)
    {
        this.packagePath = packagePath;
        this.source = source;
    }

    public string packagePath { get; set; }

    public string source { get; set; }
}

public class InitBrainPluginsMessage : EngineMessage<InitBrainPlugins>
{
    public InitBrainPluginsMessage(InitBrainPlugins content)
        : base(EngineMessageActions.InitBrainPlugins, content)
    {
    }
}
