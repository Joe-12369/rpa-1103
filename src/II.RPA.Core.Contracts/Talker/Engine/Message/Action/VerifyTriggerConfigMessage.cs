using Newtonsoft.Json.Linq;

namespace II.RPA.Core.Contracts.Talker.Engine.Message.Action;

public class VerifyTriggerConfig
{
    public VerifyTriggerConfig(JArray? appIdParams = null)
    {
        this.appIdParams = appIdParams;
    }

    public JArray? appIdParams { get; set; }
}

public class VerifyTriggerConfigMessage : EngineMessage<VerifyTriggerConfig>
{
    public VerifyTriggerConfigMessage(VerifyTriggerConfig content)
        : base(EngineMessageActions.VerifyTriggerConfig, content)
    {
    }
}
