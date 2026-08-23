using Ashlar.Core.Domain.Bricks.Ports;

namespace Ashlar.Bricks.SqlProfile;

/// <summary>Tunables for <see cref="ExplainAcceptanceEvaluator"/>.</summary>
public sealed class ExplainAcceptanceEvaluatorOptions
{
    /// <summary>Substrings that mark a plan as an engine error rather than a plan.</summary>
    public IReadOnlyList<string> ErrorMarkers { get; init; } = new[]
    {
        "error",
        "no such",
        "does not exist",
        "syntax",
        "unrecognized"
    };

    /// <summary>When true, a plan containing a full table scan is rejected.</summary>
    public bool RejectFullTableScan { get; init; }
}

/// <summary>
/// SQL profile acceptance evaluator. PURE and container-free: the deployment target
/// (or host) runs EXPLAIN and hands the captured plan text in; this only judges it.
/// Second evaluator proving <see cref="IAcceptanceEvaluator"/> is not shaped around
/// any one toolchain.
/// </summary>
public sealed class ExplainAcceptanceEvaluator : IAcceptanceEvaluator
{
    private readonly ExplainAcceptanceEvaluatorOptions _options;

    /// <summary>Creates the evaluator with default tunables.</summary>
    public ExplainAcceptanceEvaluator() : this(new ExplainAcceptanceEvaluatorOptions()) { }

    /// <summary>Creates the evaluator.</summary>
    public ExplainAcceptanceEvaluator(ExplainAcceptanceEvaluatorOptions options) =>
        _options = options ?? throw new ArgumentNullException(nameof(options));

    /// <inheritdoc />
    public AcceptanceResult Evaluate(AcceptanceContext context)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        var smoke = context.Smoke;
        if (!smoke.Ran)
            return AcceptanceResult.Ship("no plan check configured", new[] { "smoke-not-run" });

        if (!smoke.Succeeded)
        {
            return AcceptanceResult.Rollback(
                "plan check did not complete: " + (smoke.Detail ?? "no detail"),
                new[] { "smoke-failed" });
        }

        if (smoke.Outputs.Count == 0)
        {
            return AcceptanceResult.Ship(
                "statement check passed but captured no plan",
                new[] { "no-plan-captured" });
        }

        var signals = new List<string>();
        var failures = new List<string>();

        foreach (var name in smoke.Outputs.Keys.OrderBy(k => k, StringComparer.Ordinal))
        {
            var (ok, detail, signal) = InspectPlan(smoke.Outputs[name] ?? "");
            signals.Add(signal);
            if (!ok)
                failures.Add($"{name}: {detail}");
        }

        return failures.Count == 0
            ? AcceptanceResult.Ship($"all {smoke.Outputs.Count} plan(s) clean", signals)
            : AcceptanceResult.Rollback(string.Join("; ", failures), signals);
    }

    private (bool Ok, string Detail, string Signal) InspectPlan(string plan)
    {
        if (string.IsNullOrWhiteSpace(plan))
            return (false, "plan is empty", "empty-plan");

        foreach (var marker in _options.ErrorMarkers)
        {
            if (plan.Contains(marker, StringComparison.OrdinalIgnoreCase))
                return (false, $"plan reports '{marker}'", "plan-error");
        }

        if (_options.RejectFullTableScan && ContainsFullScan(plan))
            return (false, "plan performs a full table scan", "full-table-scan");

        return (true, "plan clean", "plan-ok");
    }

    private static bool ContainsFullScan(string plan) =>
        plan.Split('\n')
            .Select(l => l.Trim())
            .Any(l =>
                l.StartsWith("SCAN", StringComparison.OrdinalIgnoreCase)
                && !l.Contains("USING INDEX", StringComparison.OrdinalIgnoreCase)
                && !l.Contains("USING COVERING INDEX", StringComparison.OrdinalIgnoreCase));
}
