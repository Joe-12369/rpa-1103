namespace II.RPA.Core.Talker.Talkers;

internal readonly record struct CompensatedRunTerminal(
    string RequestId,
    string ActionId);

internal sealed class RunTerminalTracker
{
    private const int RetainedTerminalClaims = 64;
    private readonly object _gate = new();
    private readonly Dictionary<string, RunState> _runs =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, long> _terminalClaims =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _committedTerminals = new(StringComparer.OrdinalIgnoreCase);
    private long _claimSequence;

    public void StartRun(string requestId, string actionId, long generation = 0)
    {
        if (string.IsNullOrWhiteSpace(requestId) ||
            string.IsNullOrWhiteSpace(actionId))
        {
            return;
        }

        lock (_gate)
        {
            _runs[requestId] = new RunState(requestId, actionId, generation);
        }
    }

    public CompensatedRunTerminal[] SnapshotPendingRuns(long? generation = null)
    {
        lock (_gate)
        {
            return _runs.Values.Where(state => !state.TerminalForwarded &&
                (generation == null || state.Generation == generation))
                .Select(state => new CompensatedRunTerminal(state.RequestId, state.ActionId))
                .ToArray();
        }
    }

    public CompensatedRunTerminal[] SnapshotExitingRuns(long generation)
    {
        lock (_gate)
        {
            // Include committed runs so Finish-before-terminal records are cleaned on exit too.
            return _runs.Values.Where(state => state.Generation == generation)
                .Select(state => new CompensatedRunTerminal(state.RequestId, state.ActionId))
                .ToArray();
        }
    }

    public void MarkDone(string requestId)
    {
        if (string.IsNullOrWhiteSpace(requestId))
        {
            return;
        }

        lock (_gate)
        {
            if (_runs.TryGetValue(requestId, out var state))
            {
                state.DoneReceived = true;
            }
        }
    }

    public bool MarkBanished(string requestId)
    {
        if (string.IsNullOrWhiteSpace(requestId))
        {
            return false;
        }

        lock (_gate)
        {
            if (!_runs.TryGetValue(requestId, out var state))
            {
                return false;
            }

            state.BanishedReceived = true;
            return true;
        }
    }

    public bool TryClaimTerminal(string requestId)
    {
        if (string.IsNullOrWhiteSpace(requestId))
        {
            return true;
        }

        lock (_gate)
        {
            if (!TryClaim(requestId))
            {
                return false;
            }

            return true;
        }
    }

    public void CommitTerminal(string requestId)
    {
        if (string.IsNullOrWhiteSpace(requestId))
        {
            return;
        }

        lock (_gate)
        {
            if (_runs.TryGetValue(requestId, out var state))
            {
                state.TerminalForwarded = true;
            }
            _committedTerminals.Add(requestId);
        }
    }

    public bool HasTerminalForwarded(string requestId)
    {
        if (string.IsNullOrWhiteSpace(requestId))
        {
            return false;
        }

        lock (_gate)
        {
            return _committedTerminals.Contains(requestId);
        }
    }

    public bool ShouldReportInterruptedRun(string requestId, bool completed)
    {
        return !HasTerminalForwarded(requestId);
    }

    public bool TryClaimCompensation(
        string requestId,
        int? exitCode,
        out CompensatedRunTerminal terminal)
    {
        terminal = default;
        if (string.IsNullOrWhiteSpace(requestId) || exitCode != 0)
        {
            return false;
        }

        lock (_gate)
        {
            if (!_runs.TryGetValue(requestId, out var state) ||
                !state.DoneReceived ||
                !state.BanishedReceived ||
                state.TerminalForwarded ||
                !TryClaim(requestId))
            {
                return false;
            }

            terminal = new CompensatedRunTerminal(state.RequestId, state.ActionId);
            return true;
        }
    }

    public void RollbackTerminal(string requestId)
    {
        if (string.IsNullOrWhiteSpace(requestId))
        {
            return;
        }

        lock (_gate)
        {
            _terminalClaims.Remove(requestId);
        }
    }

    public void ClearActiveRun(string requestId)
    {
        if (string.IsNullOrWhiteSpace(requestId))
        {
            return;
        }

        lock (_gate)
        {
            _runs.Remove(requestId);
        }
    }

    private bool TryClaim(string requestId)
    {
        if (_terminalClaims.ContainsKey(requestId))
        {
            return false;
        }

        _terminalClaims[requestId] = ++_claimSequence;
        TrimTerminalClaims();
        return true;
    }

    private void TrimTerminalClaims()
    {
        while (_terminalClaims.Count > RetainedTerminalClaims)
        {
            var completed = _terminalClaims.Where(pair =>
                !_runs.TryGetValue(pair.Key, out var state) || state.TerminalForwarded).ToArray();
            if (completed.Length == 0)
                break;
            var oldest = completed.MinBy(pair => pair.Value);
            _terminalClaims.Remove(oldest.Key);
            _committedTerminals.Remove(oldest.Key);
        }
    }

    private sealed class RunState
    {
        public RunState(string requestId, string actionId, long generation)
        {
            RequestId = requestId;
            ActionId = actionId;
            Generation = generation;
        }

        public long Generation { get; }

        public string RequestId { get; }

        public string ActionId { get; }

        public bool DoneReceived { get; set; }

        public bool BanishedReceived { get; set; }

        public bool TerminalForwarded { get; set; }
    }
}
