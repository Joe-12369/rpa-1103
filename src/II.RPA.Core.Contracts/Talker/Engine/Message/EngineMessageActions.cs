namespace II.RPA.Core.Contracts.Talker.Engine.Message;

public static class EngineMessageActions
{
    public const string Ping = "ping";
    public const string Pong = "pong";
    public const string Close = "close";
    public const string GetCommanderConnect = "GetCommanderConnect";
    public const string GetServiceInfo = "GetServiceInfo";
    public const string SetServiceInfo = "SetServiceInfo";
    public const string GetUserInfo = "GetUserInfo";
    public const string Log = "log";
    public const string TraceLog = "trace_log";
    public const string Run = "run";
    public const string ChildRunOver = "childrunover";
    public const string Stop = "stop";
    public const string Error = "error";
    public const string UninstallDll = "uninstallDll";
    public const string VerifyTriggerConfig = "verifyTriggerConfig";
    public const string Trigger = "trigger";
    public const string SetLogLevel = "setLogLevel";
    public const string OpenVirtualDesktop = "openVirtualDesktop";
    public const string InitBrainPlugins = "InitBrainPlugins";
    public const string PowerStatusChanged = "PowerStatusChanged";
    public const string ExecutorTalkerStatusChanged = "ExecutorTalkerStatusChanged";
    public const string InitBrainPluginsRt = "InitBrainPluginsRt";
    public const string Pause = "pause";
    public const string Resume = "resume";
    public const string Reset = "reset";
    public const string Unload = "unload";
    public const string Message = "message";
    public const string MCPRun = "mcpRun";
    public const string OperatorClientWin = "operatorClientWin";
    public const string AiEnhancedLocator = "Element.aiEnhancedLocator";
}
