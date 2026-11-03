using II.RPA.Core.Common;
using System;

namespace II.RPA.Core.Runtime
{
/// <summary>
/// Compatibility adapter around legacy Core globals. New consumers depend only on
/// <see cref="IRpaRuntimeContext"/>, so this adapter can disappear with the old Core host.
/// </summary>
public sealed class GlobalRpaRuntimeContext : IRpaRuntimeContext
{
    public RpaClientKind ClientKind => Global.Client switch
    {
        ClientID.Factory => RpaClientKind.Factory,
        ClientID.Bot => RpaClientKind.Bot,
        ClientID.DeskTop => RpaClientKind.Desktop,
        ClientID.Admin => RpaClientKind.Admin,
        _ => RpaClientKind.Unknown
    };

    public string ClientId => Global.ClientIDString;
    public string ProducedClientId => Global.ProduceClientIDString;
    public string RootClientId => Global.RootClientString;
    public int CurrentProcessId => Global.CurrentPId;
    public int SessionId => Global.SessionId;
    public int ServerPort => Global.ServerPort;
    public int RootServerPort => Global.RootServerPort;
    public bool LoopAdb => Global.LoopADB;
    public bool IsMiniModeActive => Global.IsMiniModeActive;
    public bool ForceRestartHost => Global.ForceRestartHost;
    public string EngineExecutablePath => Global.EngineExePath;
    public string PythonEnvironmentVersion => Global.PythonEnvVersion;

    public string? UserInfoParameter
    {
        get => Global.UserInfoParam;
        set => Global.UserInfoParam = value;
    }

    public string BuildUserInfoParameter() => Global.BuildUserInforParam();

    public string FixZBrainUriForZDesktop(string parameter) => Global.FixZbrainUriForZdesktop(parameter);

    public void SetSetting(string key, object value) => Global.Settings.Set(key, value);

    public string? GetConfiguration(string commandLineName, string configName) =>
        CommandArgHelper.GetWithConfig(commandLineName, configName);

    public bool IsMobileProject(string packagePath) => PluginUtils.IsMoblieProject(packagePath);
}
}
