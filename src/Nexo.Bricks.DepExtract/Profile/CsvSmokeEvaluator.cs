using System.Globalization;
using System.Text.RegularExpressions;
using Nexo.Core.Domain.Bricks.Ports;

namespace Nexo.Bricks.DepExtract.Profile;

/// <summary>Tunables for <see cref="CsvSmokeEvaluator"/>.</summary>
public sealed class CsvSmokeEvaluatorOptions
{
    /// <summary>
    /// Minimum data rows a captured extract must contain. Never 1 — a single row
    /// proves the pipe is open, not that the adapter reads the format correctly.
    /// Overridable via <c>NEXO_ORACLE_MIN_EVENTS</c>.
    /// </summary>
    public int MinRows { get; init; } = DefaultMinRows;

    /// <summary>Optional upper bound; a wild over-count is as suspect as an under-count.</summary>
    public int? MaxRows { get; init; }

    /// <summary>Every data row must have the same column count as the header.</summary>
    public bool RequireConsistentColumns { get; init; } = true;

    /// <summary>Timestamps (when a time column exists) must be monotonic and in range.</summary>
    public bool RequireMonotonicTimestamps { get; init; } = true;

    /// <summary>Columns that must be present in the header.</summary>
    public IReadOnlyList<string> RequiredColumns { get; init; } = Array.Empty<string>();

    /// <summary>Default row floor, honoring <c>NEXO_ORACLE_MIN_EVENTS</c>.</summary>
    public static int DefaultMinRows =>
        int.TryParse(Environment.GetEnvironmentVariable("NEXO_ORACLE_MIN_EVENTS"), out var n) && n > 0
            ? n
            : 5;
}

/// <summary>
/// C++/EVTX profile acceptance evaluator. PURE: reads only the CSV already captured
/// by the deployment target and decides ship/rollback. Replaces the rows&gt;=1 oracle
/// with row-count sanity plus field/shape checks, and gets multi-sample agreement for
/// free — every captured sample must independently clear the bar.
/// </summary>
public sealed class CsvSmokeEvaluator : IAcceptanceEvaluator
{
    private static readonly Regex RowsInDetail = new(@"rows=(\d+)", RegexOptions.Compiled);

    private readonly CsvSmokeEvaluatorOptions _options;

    /// <summary>Creates the evaluator with default tunables.</summary>
    public CsvSmokeEvaluator() : this(new CsvSmokeEvaluatorOptions()) { }

    /// <summary>Creates the evaluator.</summary>
    public CsvSmokeEvaluator(CsvSmokeEvaluatorOptions options) =>
        _options = options ?? throw new ArgumentNullException(nameof(options));

    /// <inheritdoc />
    public AcceptanceResult Evaluate(AcceptanceContext context)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        var smoke = context.Smoke;
        if (!smoke.Ran)
            return AcceptanceResult.Ship("no smoke configured", new[] { "smoke-not-run" });

        if (!smoke.Succeeded)
        {
            return AcceptanceResult.Rollback(
                "smoke did not complete: " + (smoke.Detail ?? "no detail"),
                new[] { "smoke-failed" });
        }

        // Preferred evidence: the captured extract bodies themselves.
        if (smoke.Outputs.Count > 0)
            return EvaluateSamples(smoke.Outputs);

        // Fallback: the target reported a row count but not the body.
        var reported = RowsInDetail.Match(smoke.Detail ?? "");
        if (reported.Success && int.TryParse(reported.Groups[1].Value, out var rows))
        {
            return rows >= _options.MinRows
                ? AcceptanceResult.Ship(
                    $"reported rows={rows} >= floor={_options.MinRows}",
                    new[] { "rows-from-detail" })
                : AcceptanceResult.Rollback(
                    $"reported rows={rows} below floor={_options.MinRows}",
                    new[] { "row-floor", "rows-from-detail" });
        }

        // Health-only smoke: nothing to sanity-check. Say so rather than dressing a
        // liveness ping up as acceptance.
        return AcceptanceResult.Ship(
            "smoke passed but captured no row evidence (health-only)",
            new[] { "health-only" });
    }

    private AcceptanceResult EvaluateSamples(IReadOnlyDictionary<string, string> samples)
    {
        var signals = new List<string>();
        var failures = new List<string>();

        foreach (var name in samples.Keys.OrderBy(k => k, StringComparer.Ordinal))
        {
            var (ok, detail, signal) = InspectCsv(samples[name] ?? "");
            signals.Add($"{name}:{(ok ? "ok" : signal)}");
            if (!ok)
                failures.Add($"{name}: {detail}");
        }

        return failures.Count == 0
            ? AcceptanceResult.Ship(
                $"all {samples.Count} sample(s) cleared row floor {_options.MinRows} and shape checks",
                signals)
            : AcceptanceResult.Rollback(string.Join("; ", failures), signals);
    }

    private (bool Ok, string Detail, string Signal) InspectCsv(string csv)
    {
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length == 0)
            return (false, "empty output", "empty");

        var header = SplitLine(lines[0]);
        var dataRows = lines.Length - 1;

        foreach (var required in _options.RequiredColumns)
        {
            if (!header.Any(h => h.Equals(required, StringComparison.OrdinalIgnoreCase)))
                return (false, $"missing required column '{required}'", "missing-column");
        }

        if (dataRows < _options.MinRows)
            return (false, $"rows={dataRows} below floor={_options.MinRows}", "row-floor");

        if (_options.MaxRows is { } max && dataRows > max)
            return (false, $"rows={dataRows} above ceiling={max}", "row-ceiling");

        if (_options.RequireConsistentColumns)
        {
            for (var i = 1; i < lines.Length; i++)
            {
                var cols = SplitLine(lines[i]);
                if (cols.Length != header.Length)
                {
                    return (false,
                        $"row {i} has {cols.Length} column(s), header has {header.Length}",
                        "column-mismatch");
                }
            }
        }

        if (_options.RequireMonotonicTimestamps)
        {
            var tsIdx = Array.FindIndex(header, IsTimestampColumn);
            if (tsIdx >= 0)
            {
                DateTimeOffset? prev = null;
                var minAllowed = DateTimeOffset.UtcNow.AddYears(-50);
                var maxAllowed = DateTimeOffset.UtcNow.AddYears(5);

                for (var i = 1; i < lines.Length; i++)
                {
                    var cols = SplitLine(lines[i]);
                    if (tsIdx >= cols.Length) continue;
                    if (!TryParseTime(cols[tsIdx], out var ts))
                        return (false, $"unparseable timestamp at row {i}: {cols[tsIdx]}", "bad-timestamp");
                    if (ts < minAllowed || ts > maxAllowed)
                        return (false, $"timestamp out of range at row {i}: {ts:o}", "timestamp-range");
                    if (prev is { } p && ts < p)
                        return (false, $"timestamps not monotonic at row {i}", "monotonic");
                    prev = ts;
                }
            }
        }

        return (true, $"rows={dataRows}", "ok");
    }

    private static bool IsTimestampColumn(string h) =>
        h.Contains("time", StringComparison.OrdinalIgnoreCase)
        || h.Equals("ts", StringComparison.OrdinalIgnoreCase)
        || h.Equals("timestamp", StringComparison.OrdinalIgnoreCase);

    private static string[] SplitLine(string line) =>
        line.Split(',').Select(c => c.Trim().Trim('"')).ToArray();

    private static bool TryParseTime(string raw, out DateTimeOffset ts)
    {
        raw = raw.Trim().Trim('"');
        if (DateTimeOffset.TryParse(
                raw,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out ts))
            return true;

        if (long.TryParse(raw, out var unix))
        {
            ts = unix > 10_000_000_000
                ? DateTimeOffset.FromUnixTimeMilliseconds(unix)
                : DateTimeOffset.FromUnixTimeSeconds(unix);
            return true;
        }

        ts = default;
        return false;
    }
}
