using System.Globalization;
using System.Text.RegularExpressions;

namespace Nexo.Bricks.DepExtract.Gui;

/// <summary>
/// Post-install golden-file oracle: raises the bar above MinEvents=1 with
/// multi-sample agreement and basic field-level sanity on CSV extract results.
/// </summary>
public static class OracleEvaluator
{
    /// <summary>Default minimum data rows; overridable via <c>NEXO_ORACLE_MIN_EVENTS</c>.</summary>
    public static int DefaultMinEvents
    {
        get
        {
            if (int.TryParse(Environment.GetEnvironmentVariable("NEXO_ORACLE_MIN_EVENTS"), out var n) && n > 0)
                return n;
            return 5;
        }
    }

    public sealed record SampleResult(string Sample, bool Ok, int Rows, string Detail);

    public sealed record Verdict(
        bool AllPassed,
        int MinEvents,
        IReadOnlyList<SampleResult> Samples,
        bool FieldSanityOk,
        string? FieldSanityDetail,
        string Summary);

    /// <summary>
    /// Evaluate oracle against one or more smoke detail strings (from InstallExecutor)
    /// and optional raw CSV bodies. All samples must meet <paramref name="minEvents"/>;
    /// when CSV is provided, timestamps must be monotonic/in-range and row count in band.
    /// </summary>
    public static Verdict Evaluate(
        IReadOnlyList<(string Sample, string? SmokeDetail, string? Csv)> samples,
        int minEvents,
        int? expectedRowBandMin = null,
        int? expectedRowBandMax = null)
    {
        minEvents = Math.Max(DefaultMinEvents, minEvents);
        var results = new List<SampleResult>();
        var fieldOk = true;
        string? fieldDetail = null;

        foreach (var (sample, smoke, csv) in samples)
        {
            var rows = TryParseRows(smoke);
            if (csv is { Length: > 0 })
            {
                var csvRows = CountDataRows(csv);
                if (rows is null || csvRows > rows) rows = csvRows;
                var sanity = CheckFieldSanity(csv, expectedRowBandMin ?? minEvents, expectedRowBandMax);
                if (!sanity.Ok)
                {
                    fieldOk = false;
                    fieldDetail = (fieldDetail is null ? "" : fieldDetail + "; ") + $"{sample}: {sanity.Detail}";
                }
                else if (fieldDetail is null)
                {
                    fieldDetail = sanity.Detail;
                }
            }

            var rowCount = rows ?? 0;
            var ok = rowCount >= minEvents;
            results.Add(new SampleResult(
                sample,
                ok,
                rowCount,
                ok ? $"rows={rowCount} >= min={minEvents}" : $"rows={rowCount} < min={minEvents}"));
        }

        var allSamplesOk = results.Count > 0 && results.All(r => r.Ok);
        // Multi-sample agreement: every sample must independently clear the bar.
        if (results.Count > 1)
        {
            var distinctOk = results.Select(r => r.Ok).Distinct().Count() == 1 && allSamplesOk;
            allSamplesOk = distinctOk;
        }

        var allPassed = allSamplesOk && fieldOk;
        var summary = allPassed
            ? $"Oracle PASS: {results.Count} sample(s), minEvents={minEvents}, fieldSanity=ok"
            : $"Oracle FAIL: samples=[{string.Join("; ", results.Select(r => $"{r.Sample}:{r.Detail}"))}] fieldSanity={fieldDetail ?? "n/a"}";

        return new Verdict(allPassed, minEvents, results, fieldOk, fieldDetail, summary);
    }

    public static int? TryParseRows(string? smokeDetail)
    {
        if (string.IsNullOrWhiteSpace(smokeDetail)) return null;
        var m = Regex.Match(smokeDetail, @"rows=(\d+)");
        return m.Success && int.TryParse(m.Groups[1].Value, out var n) ? n : null;
    }

    public static int CountDataRows(string csv) =>
        csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Skip(1)
            .Count();

    public static (bool Ok, string Detail) CheckFieldSanity(
        string csv,
        int bandMin,
        int? bandMax)
    {
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length < 2)
            return (false, "csv has no data rows");

        var header = lines[0].Split(',').Select(h => h.Trim().Trim('"')).ToArray();
        var dataRows = lines.Length - 1;
        if (dataRows < bandMin)
            return (false, $"row band: {dataRows} < min {bandMin}");
        if (bandMax is { } max && dataRows > max)
            return (false, $"row band: {dataRows} > max {max}");

        var tsIdx = Array.FindIndex(header, h =>
            h.Contains("time", StringComparison.OrdinalIgnoreCase)
            || h.Equals("ts", StringComparison.OrdinalIgnoreCase)
            || h.Equals("timestamp", StringComparison.OrdinalIgnoreCase)
            || h.Equals("SystemTime", StringComparison.OrdinalIgnoreCase));

        if (tsIdx < 0)
            return (true, $"rows={dataRows} (no timestamp column; row-band ok)");

        DateTimeOffset? prev = null;
        var minAllowed = DateTimeOffset.UtcNow.AddYears(-50);
        var maxAllowed = DateTimeOffset.UtcNow.AddYears(5);
        for (var i = 1; i < lines.Length; i++)
        {
            var cols = SplitCsvLine(lines[i]);
            if (tsIdx >= cols.Length) continue;
            if (!TryParseTime(cols[tsIdx], out var ts))
                return (false, $"unparseable timestamp at row {i}: {cols[tsIdx]}");
            if (ts < minAllowed || ts > maxAllowed)
                return (false, $"timestamp out of range at row {i}: {ts:o}");
            if (prev is { } p && ts < p)
                return (false, $"timestamps not monotonic at row {i}: {ts:o} < {p:o}");
            prev = ts;
        }

        return (true, $"rows={dataRows}, timestamps monotonic/in-range");
    }

    static string[] SplitCsvLine(string line)
    {
        // Lightweight split: good enough for oracle sanity (quoted commas rare in our extracts).
        return line.Split(',').Select(c => c.Trim().Trim('"')).ToArray();
    }

    static bool TryParseTime(string raw, out DateTimeOffset ts)
    {
        raw = raw.Trim().Trim('"');
        if (DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out ts))
            return true;
        if (long.TryParse(raw, out var unix))
        {
            // Heuristic: seconds vs ms
            ts = unix > 10_000_000_000
                ? DateTimeOffset.FromUnixTimeMilliseconds(unix)
                : DateTimeOffset.FromUnixTimeSeconds(unix);
            return true;
        }
        ts = default;
        return false;
    }
}
