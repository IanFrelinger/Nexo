namespace Ashlar.BackgroundAgents.Extending;

/// <summary>
/// Per-extender bookkeeping behind <see cref="ExtensionCeiling"/>: cycles run since a human
/// last armed the agent, and the timestamps of recent cycles for the hourly rate. Kept by the
/// registry OUTSIDE the agent instance so that re-registering an agent (which agents can do to
/// themselves through <c>UpdateAgentConfigTool</c>) does not reset it — only
/// <see cref="Rearm"/> or a process restart does.
/// </summary>
public sealed class ExtensionLedger
{
    private readonly object _gate = new();
    private readonly Queue<DateTimeOffset> _recent = new();
    private readonly Func<DateTimeOffset> _clock;

    /// <summary>Creates a ledger over the given clock (UTC now by default).</summary>
    public ExtensionLedger(Func<DateTimeOffset>? clock = null)
    {
        _clock = clock ?? (static () => DateTimeOffset.UtcNow);
    }

    /// <summary>Extend cycles run since the last human arm (or process start).</summary>
    public int UnattendedCycles { get; private set; }

    /// <summary>Extend cycles in the trailing <see cref="ExtensionCeiling.RateWindow"/>.</summary>
    public int CyclesInWindow
    {
        get
        {
            lock (_gate)
            {
                Prune(_clock());
                return _recent.Count;
            }
        }
    }

    /// <summary>
    /// The reason this agent may not run another extend cycle under <paramref name="ceiling"/>,
    /// or null when it may. Lineage depth is the caller's to compute (it is a property of the
    /// registry's parent graph, not of this ledger).
    /// </summary>
    public string? Refusal(ExtensionCeiling ceiling, int lineageDepth)
    {
        ArgumentNullException.ThrowIfNull(ceiling);
        if (lineageDepth > ceiling.MaxLineageDepth)
        {
            return $"lineage depth {lineageDepth} exceeds MaxLineageDepth {ceiling.MaxLineageDepth} " +
                   "(a machine-spawned extender this far below a human root may not extend)";
        }

        lock (_gate)
        {
            var now = _clock();
            Prune(now);
            if (_recent.Count >= ceiling.MaxCyclesPerHour)
            {
                return $"{_recent.Count} extend cycle(s) in the trailing hour reach MaxCyclesPerHour {ceiling.MaxCyclesPerHour}";
            }

            if (UnattendedCycles >= ceiling.MaxUnattendedCycles)
            {
                return $"{UnattendedCycles} unattended extend cycle(s) reach MaxUnattendedCycles {ceiling.MaxUnattendedCycles}; " +
                       "holding until a human re-arms (restart, or RearmExtension)";
            }
        }

        return null;
    }

    /// <summary>Records that an extend cycle was actually handed to the runner.</summary>
    public void RecordCycle()
    {
        lock (_gate)
        {
            var now = _clock();
            Prune(now);
            _recent.Enqueue(now);
            UnattendedCycles++;
        }
    }

    /// <summary>
    /// A human re-arms the agent: the unattended count resets. The hourly rate does not — a
    /// human's blessing does not make the last hour's cycles un-happen.
    /// </summary>
    /// <returns>The unattended count that was cleared.</returns>
    public int Rearm()
    {
        lock (_gate)
        {
            var cleared = UnattendedCycles;
            UnattendedCycles = 0;
            return cleared;
        }
    }

    private void Prune(DateTimeOffset now)
    {
        var horizon = now - ExtensionCeiling.RateWindow;
        while (_recent.Count > 0 && _recent.Peek() < horizon)
            _recent.Dequeue();
    }
}
