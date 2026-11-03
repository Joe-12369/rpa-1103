namespace II.RPA.Core.Contracts.Talker.Engine.Message.Action;

public class ExecutorStatusChanged
{
    public ExecutorStatusChanged(int? pid, bool available)
    {
        Pid = pid;
        Available = available;
    }

    public bool Available { get; }

    public int? Pid { get; }
}

public class ExecutorStatusChangedMessage : EngineMessage<ExecutorStatusChanged>
{
    public ExecutorStatusChangedMessage(ExecutorStatusChanged content)
        : base(EngineMessageActions.ExecutorTalkerStatusChanged, content)
    {
    }
}
