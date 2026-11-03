namespace II.RPA.Core.Runtime;

/// <summary>
/// Explicit snapshot/access boundary for process identity, configuration and the small
/// amount of mutable application state needed by the refactored Talker implementation.
/// </summary>
public interface IRpaRuntimeContext
{
    RpaClientKind ClientKind { get; }

    string ClientId { get; }

    string ProducedClientId { get; }

    string RootClientId { get; }

    int CurrentProcessId { get; }

    int SessionId { get; }

    int ServerPort { get; }

    int RootServerPort { get; }

    bool LoopAdb { get; }

    bool IsMiniModeActive { get; }

    bool ForceRestartHost { get; }

    string EngineExecutablePath { get; }

    string PythonEnvironmentVersion { get; }

    string? UserInfoParameter { get; set; }

    string BuildUserInfoParameter();

    string FixZBrainUriForZDesktop(string parameter);

    void SetSetting(string key, object value);

    string? GetConfiguration(string commandLineName, string configName);

    bool IsMobileProject(string packagePath);
}

public enum RpaClientKind
{
    Factory = 0,
    Bot = 1,
    Desktop = 2,
    Admin = 3,
    Unknown = 99
}
