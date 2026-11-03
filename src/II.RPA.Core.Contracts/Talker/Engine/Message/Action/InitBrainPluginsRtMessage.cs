namespace II.RPA.Core.Contracts.Talker.Engine.Message.Action;

public class InitBrainPluginsRt
{
    public InitBrainPluginsRt(string packagePath, string source = EngineMessage.BotSource)
    {
        this.packagePath = packagePath;
        this.source = source;
    }

    public string packagePath { get; set; }

    public string source { get; set; }
}

public class InitBrainPluginsRtMessage : EngineMessage<InitBrainPluginsRt>
{
    public InitBrainPluginsRtMessage(InitBrainPluginsRt content)
        : base(EngineMessageActions.InitBrainPluginsRt, content)
    {
    }
}
