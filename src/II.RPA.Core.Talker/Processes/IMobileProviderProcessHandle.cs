using II.RPA.Core.Talker.Models;

namespace II.RPA.Core.Talker.Processes;

internal interface IMobileProviderProcessHandle
{
    int? ProcessId { get; }

    ProcessStatus Status { get; }

    bool IsInitiativeStop { get; }

    event EventHandler<ProcessStatus>? OnProcessStatusChangedEvent;

    void Start();

    Task StopAsync();
}
