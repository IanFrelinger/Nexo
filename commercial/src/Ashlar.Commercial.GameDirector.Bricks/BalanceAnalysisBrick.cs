using System.Text.Json;
using GameDirector.Bricks.Schemas;
using GameDirector.Domain;
using Microsoft.Extensions.Logging;
using Ashlar.Abstractions;
using Ashlar.Core.Application.Trust.Ports;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace GameDirector.Bricks;

/// <summary>
/// balance.analysis — deterministic TTK/outlier parse + optional hybrid narrative via local model.
/// </summary>
public sealed class BalanceAnalysisBrick : DomainBrick
{
    private readonly IDataDecisionAuditLog _auditLog;
    private readonly IModel? _model;
    private readonly ILogger<BalanceAnalysisBrick> _logger;
    private readonly ITrustPolicyPackRegistry? _trustPacks;

    public BalanceAnalysisBrick(
        IDataDecisionAuditLog auditLog,
        ILogger<BalanceAnalysisBrick> logger,
        IModel? model = null,
        ITrustPolicyPackRegistry? trustPacks = null)
    {
        _auditLog = auditLog;
        _logger = logger;
        _model = model;
        _trustPacks = trustPacks;

        Id = "balance.analysis";
        Name = "Balance Analysis";
        Category = BrickCategory.Analysis;
        Description = "Ingest weapon/unit stat sheets; return outlier flags, TTK spread, and suggested deltas.";
        Interface = new BrickInterface
        {
            Inputs =
            [
                new BrickInputDefinition("sheet_id", "string", "Balance sheet identifier", required: true),
                new BrickInputDefinition("data", "array", "Stat rows { id, stats }", required: true),
                new BrickInputDefinition("baseline_id", "string", "Optional baseline reference", required: false)
            ],
            Outputs =
            [
                new BrickOutputDefinition("audit_id", "string", "Audit record id"),
                new BrickOutputDefinition("flagged", "array", "Outlier entries"),
                new BrickOutputDefinition("ttk_spread", "number", "TTK spread stddev (ms)"),
                new BrickOutputDefinition("suggestions", "array", "Suggested stat deltas")
            ]
        };
        Implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "balance.analysis.det",
                Name = "Deterministic balance parse",
                Description = "Stat parse and TTK spread without LLM"
            },
            Agentic = new AgenticImplementation
            {
                Id = "balance.analysis.hybrid",
                Name = "Hybrid balance analysis",
                Description = "Deterministic parse + local model rationale"
            }
        };
        DefaultImplementation = ImplementationType.Deterministic;
    }

    public override async Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var sheetId = GetString(input, "sheet_id");
        var baselineId = input.Get<string?>("baseline_id", null);
        var rows = ParseRows(input);
        var auditId = GameDirectorAudit.NewAuditId();
        var trustPolicy = _trustPacks?.GetActivePack()?.Id ?? "none";

        var flagged = ComputeOutliers(rows, baselineId);
        var ttkSpread = ComputeTtkSpread(flagged);
        var suggestions = flagged.Select(o => new Delta(
            o.Id,
            o.Stat,
            Math.Round(o.BaselineValue * (1 + o.DeltaPct / -100.0), 2),
            $"Reduce {o.Stat} drift ({o.DeltaPct:F1}% from baseline)")).ToList();

        if (implementation != ImplementationType.Deterministic && _model is not null)
        {
            try
            {
                var prompt = $"Balance sheet {sheetId}: {flagged.Count} outliers, TTK spread {ttkSpread:F0}ms. " +
                             "Suggest one-line rationale per weapon.";
                var modelOut = await _model.CompleteAsync(
                    new ModelInput([("user", prompt)]),
                    cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(modelOut.Text) && suggestions.Count > 0)
                {
                    suggestions[0] = suggestions[0] with { Rationale = modelOut.Text.Trim()[..Math.Min(200, modelOut.Text.Length)] };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Hybrid balance rationale skipped (local model unavailable)");
            }
        }

        var result = new BalanceAnalysisOutput(auditId, flagged, ttkSpread, suggestions);
        var inputHash = GameDirectorAudit.HashInput(new { sheetId, rowCount = rows.Count, baselineId });
        GameDirectorAudit.LogToolExecution(
            _auditLog,
            auditId,
            "analyze_balance",
            inputHash,
            implementation == ImplementationType.Deterministic ? "deterministic" : "local",
            trustPolicy,
            $"sheet={sheetId} flagged={flagged.Count} ttk_spread={ttkSpread:F0}");

        var output = new BrickOutput { Summary = $"Balance analysis for {sheetId}: {flagged.Count} outlier(s)." };
        output.Set("audit_id", auditId);
        output.Set("flagged", flagged);
        output.Set("ttk_spread", ttkSpread);
        output.Set("suggestions", suggestions);
        return output;
    }

    private static List<StatRow> ParseRows(BrickInput input)
    {
        if (!input.ToDictionary().TryGetValue("data", out var raw))
            return [];

        if (raw is JsonElement je)
        {
            if (je.ValueKind == JsonValueKind.Array)
                return je.EnumerateArray().Select(ParseRow).Where(r => r is not null).Cast<StatRow>().ToList();
            if (je.ValueKind == JsonValueKind.String)
                return ParseRowsFromJson(je.GetString());
        }

        if (raw is string json)
            return ParseRowsFromJson(json);

        if (raw is IEnumerable<object> list)
        {
            var rows = new List<StatRow>();
            foreach (var item in list)
            {
                switch (item)
                {
                    case JsonElement el when el.ValueKind == JsonValueKind.Object:
                        if (ParseRow(el) is { } row)
                            rows.Add(row);
                        break;
                    case IReadOnlyDictionary<string, object> dict:
                        if (ParseRowFromDictionary(dict) is { } drow)
                            rows.Add(drow);
                        break;
                }
            }
            return rows;
        }

        return [];
    }

    private static List<StatRow> ParseRowsFromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return [];
            return doc.RootElement.EnumerateArray().Select(ParseRow).Where(r => r is not null).Cast<StatRow>().ToList();
        }
        catch
        {
            return [];
        }
    }

    private static StatRow? ParseRowFromDictionary(IReadOnlyDictionary<string, object> dict)
    {
        if (!dict.TryGetValue("id", out var idObj))
            return null;
        var id = idObj?.ToString() ?? "";
        var stats = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        if (dict.TryGetValue("stats", out var statsObj))
        {
            if (statsObj is JsonElement statsJe && statsJe.ValueKind == JsonValueKind.Object)
            {
                foreach (var p in statsJe.EnumerateObject())
                {
                    if (p.Value.ValueKind == JsonValueKind.Number && p.Value.TryGetDouble(out var d))
                        stats[p.Name] = d;
                }
            }
            else if (statsObj is IReadOnlyDictionary<string, object> statsDict)
            {
                foreach (var (k, v) in statsDict)
                {
                    var d = CoerceDouble(v);
                    if (d.HasValue)
                        stats[k] = d.Value;
                }
            }
        }
        return new StatRow(id, stats);
    }

    private static double? CoerceDouble(object? value) => value switch
    {
        double d => d,
        float f => f,
        int i => i,
        long l => l,
        JsonElement je when je.ValueKind == JsonValueKind.Number && je.TryGetDouble(out var d) => d,
        _ => double.TryParse(value?.ToString(), out var parsed) ? parsed : null
    };

    private static StatRow? ParseRow(JsonElement el)
    {
        if (el.ValueKind != JsonValueKind.Object)
            return null;
        var id = el.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "";
        var stats = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        if (el.TryGetProperty("stats", out var statsProp) && statsProp.ValueKind == JsonValueKind.Object)
        {
            foreach (var p in statsProp.EnumerateObject())
            {
                if (p.Value.ValueKind == JsonValueKind.Number && p.Value.TryGetDouble(out var d))
                    stats[p.Name] = d;
            }
        }
        return new StatRow(id, stats);
    }

    private static List<Outlier> ComputeOutliers(IReadOnlyList<StatRow> rows, string? baselineId)
    {
        var baselines = LoadBaselines(baselineId);
        var flagged = new List<Outlier>();
        const double driftThreshold = 15.0;

        foreach (var row in rows)
        {
            foreach (var (stat, value) in row.Stats)
            {
                var baseline = baselines.TryGetValue($"{row.Id}:{stat}", out var b)
                    ? b
                    : baselines.TryGetValue(stat, out var g) ? g : value * 0.85;
                if (Math.Abs(baseline) < 0.001)
                    continue;
                var deltaPct = (value - baseline) / baseline * 100.0;
                if (Math.Abs(deltaPct) >= driftThreshold)
                {
                    flagged.Add(new Outlier(row.Id, stat, value, baseline, deltaPct));
                }
            }
        }

        return flagged;
    }

    private static Dictionary<string, double> LoadBaselines(string? baselineId)
    {
        // Halo-adjacent defaults for MVP mock data
        _ = baselineId;
        return new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["assault_rifle:damage"] = 12,
            ["assault_rifle:fire_rate"] = 600,
            ["assault_rifle:ttk_ms"] = 800,
            ["battle_rifle:damage"] = 18,
            ["battle_rifle:fire_rate"] = 360,
            ["battle_rifle:ttk_ms"] = 950,
            ["smg:damage"] = 9,
            ["smg:fire_rate"] = 900,
            ["smg:ttk_ms"] = 720,
            ["damage"] = 12,
            ["fire_rate"] = 600,
            ["ttk_ms"] = 800
        };
    }

    private static double ComputeTtkSpread(IReadOnlyList<Outlier> flagged)
    {
        var ttks = flagged
            .Where(o => o.Stat.Contains("ttk", StringComparison.OrdinalIgnoreCase))
            .Select(o => o.Value)
            .ToList();
        if (ttks.Count < 2)
            return 0;
        var mean = ttks.Average();
        var variance = ttks.Sum(t => (t - mean) * (t - mean)) / ttks.Count;
        return Math.Sqrt(variance);
    }

    private static string GetString(BrickInput input, string key)
    {
        if (!input.ToDictionary().TryGetValue(key, out var v))
            throw new KeyNotFoundException(key);
        return v switch
        {
            string s => s,
            JsonElement je when je.ValueKind == JsonValueKind.String => je.GetString() ?? "",
            _ => v?.ToString() ?? ""
        };
    }
}
