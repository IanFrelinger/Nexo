using System.Diagnostics.CodeAnalysis;

namespace Ashlar.Core.Application.Autonomy;

/// <summary>
/// Per-iteration budgets for the proposal loop (parent spec R4.6 / extension B11.2): a
/// hard wall-clock ceiling, tightened by a throughput guard once enough iterations have
/// completed — an iteration is allowed at most the guard factor times the median
/// completed duration, so one degenerate session cannot starve the cadence.
/// The median needs at least <see cref="MinCompletionsForMedian"/> completions before it
/// is meaningful (the integration doc's open question, resolved: median with the
/// absolute ceiling as the floor-less fallback).
/// </summary>
[Experimental(AutonomyExperimental.DiagnosticId, UrlFormat = AutonomyExperimental.UrlFormat)]
public sealed class ClusterBudget
{
    /// <summary>Completions required before the throughput guard engages.</summary>
    public const int MinCompletionsForMedian = 3;

    private readonly object _gate = new();
    private readonly List<TimeSpan> _completions = new();
    private readonly TimeSpan _maxIterationWallClock;
    private readonly double _throughputGuardFactor;

    /// <summary>Creates the budget.</summary>
    /// <param name="maxIterationWallClock">Absolute per-iteration ceiling (default 10 minutes).</param>
    /// <param name="throughputGuardFactor">Multiple of the median a single iteration may take (default 4).</param>
    public ClusterBudget(TimeSpan? maxIterationWallClock = null, double throughputGuardFactor = 4)
    {
        _maxIterationWallClock = maxIterationWallClock is { } max && max > TimeSpan.Zero
            ? max
            : TimeSpan.FromMinutes(10);
        _throughputGuardFactor = throughputGuardFactor > 1 ? throughputGuardFactor : 4;
    }

    /// <summary>The ceiling for the NEXT iteration: absolute, tightened by the throughput guard.</summary>
    public TimeSpan EffectiveCeiling()
    {
        lock (_gate)
        {
            if (_completions.Count < MinCompletionsForMedian)
                return _maxIterationWallClock;

            var ordered = _completions.OrderBy(d => d).ToArray();
            var median = ordered[ordered.Length / 2];
            var guarded = TimeSpan.FromTicks((long)(median.Ticks * _throughputGuardFactor));
            return guarded < _maxIterationWallClock && guarded > TimeSpan.Zero
                ? guarded
                : _maxIterationWallClock;
        }
    }

    /// <summary>Records a completed (non-budget-exhausted) iteration's duration.</summary>
    public void RecordCompletion(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
            return;

        lock (_gate)
        {
            _completions.Add(duration);
            // Bounded window: the guard tracks the recent cluster, not all history.
            if (_completions.Count > 32)
                _completions.RemoveAt(0);
        }
    }
}
