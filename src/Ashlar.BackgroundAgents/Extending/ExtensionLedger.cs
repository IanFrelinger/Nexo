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
    ///
    /// <para>This is the OBSERVING form: it answers without spending anything, for digests and
    /// operator surfaces. Anything about to actually run a cycle must use
    /// <see cref="TryBeginCycle"/> instead — deciding here and counting later is the
    /// check-then-act race the ceiling cannot survive.</para>
    /// </summary>
    public string? Refusal(ExtensionCeiling ceiling, int lineageDepth)
    {
        ArgumentNullException.ThrowIfNull(ceiling);
        var depth = DepthRefusal(ceiling, lineageDepth);
        if (depth is not null)
            return depth;

        lock (_gate)
        {
            return BudgetRefusal(ceiling, _clock());
        }
    }

    /// <summary>
    /// Atomically decides AND reserves one extend cycle: returns the refusal reason when the
    /// ceiling forbids it, or null having already counted the cycle.
    ///
    /// <para>This exists because <see cref="Refusal"/> followed by <see cref="RecordCycle"/> is a
    /// check-then-act race: the scheduler may invoke cycles for one agent concurrently (timer
    /// overlap), and every concurrent caller read the same not-yet-spent count, so N cycles were
    /// handed to the runner under a ceiling of one. A ceiling that only holds when nothing races
    /// is not a ceiling — the check and the count must happen under a single lock.</para>
    ///
    /// <para>A depth refusal costs nothing: it returns before the gate is ever taken, so refusing
    /// an over-deep extender never spends the budget it was refused.</para>
    /// </summary>
    public string? TryBeginCycle(ExtensionCeiling ceiling, int lineageDepth)
    {
        ArgumentNullException.ThrowIfNull(ceiling);
        var depth = DepthRefusal(ceiling, lineageDepth);
        if (depth is not null)
            return depth;

        lock (_gate)
        {
            var now = _clock();
            var refusal = BudgetRefusal(ceiling, now);
            if (refusal is not null)
                return refusal;

            // Reserve inside the same lock that decided: a concurrent caller now sees this cycle.
            _recent.Enqueue(now);
            UnattendedCycles++;
            return null;
        }
    }

    // The single copy of invariant D. Both the observing form and the reserving form route
    // through these, so a ceiling can never be tightened in one and silently missed in the
    // other — and the regression suite covers production's policy whichever form it drives.
    private static string? DepthRefusal(ExtensionCeiling ceiling, int lineageDepth) =>
        lineageDepth > ceiling.MaxLineageDepth
            ? $"lineage depth {lineageDepth} exceeds MaxLineageDepth {ceiling.MaxLineageDepth} " +
              "(a machine-spawned extender this far below a human root may not extend)"
            : null;

    /// <summary>Budget half of invariant D. CALLER MUST HOLD <c>_gate</c>: it prunes the window
    /// and reads the counts that a reserving caller then spends under that same lock.</summary>
    private string? BudgetRefusal(ExtensionCeiling ceiling, DateTimeOffset now)
    {
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

        return null;
    }

    /// <summary>Records that an extend cycle was actually handed to the runner. Prefer
    /// <see cref="TryBeginCycle"/>, which decides and reserves atomically; this remains for
    /// callers that have already decided by other means.</summary>
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
