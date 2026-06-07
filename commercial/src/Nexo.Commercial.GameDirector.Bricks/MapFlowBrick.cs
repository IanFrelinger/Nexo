using System.Text.Json;
using GameDirector.Bricks.Schemas;
using GameDirector.Domain;
using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.Core.Application.Trust.Ports;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace GameDirector.Bricks;

/// <summary>
/// map.flow — deterministic tile graph analysis; local-only sightline narrative.
/// </summary>
public sealed class MapFlowBrick : Brick
{
    private readonly IDataDecisionAuditLog _auditLog;
    private readonly IModel? _model;
    private readonly ILogger<MapFlowBrick> _logger;
    private readonly ITrustPolicyPackRegistry? _trustPacks;

    public MapFlowBrick(
        IDataDecisionAuditLog auditLog,
        ILogger<MapFlowBrick> logger,
        IModel? model = null,
        ITrustPolicyPackRegistry? trustPacks = null)
    {
        _auditLog = auditLog;
        _logger = logger;
        _model = model;
        _trustPacks = trustPacks;

        Id = "map.flow";
        Name = "Map Flow Analysis";
        Category = BrickCategory.Validation;
        Description = "Structural map health: choke density, spawn equity, sightlines.";
        Interface = new BrickInterface
        {
            Inputs =
            [
                new BrickInputDefinition("map_id", "string", required: true),
                new BrickInputDefinition("config", "object", "MapConfig tiles/spawns/metadata", required: true),
                new BrickInputDefinition("session_id", "string", required: false)
            ],
            Outputs =
            [
                new BrickOutputDefinition("audit_id", "string"),
                new BrickOutputDefinition("choke_density", "number"),
                new BrickOutputDefinition("spawn_equity", "number"),
                new BrickOutputDefinition("sightline_report", "string"),
                new BrickOutputDefinition("recommendations", "array")
            ]
        };
        Implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "map.flow.det",
                Name = "Deterministic map parse",
                Description = "Tile graph metrics without cloud LLM"
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
        var mapId = GetString(input, "map_id");
        var sessionId = input.Get<string?>("session_id", null);
        var config = ParseConfig(input);
        var auditId = GameDirectorAudit.NewAuditId();
        var trustPolicy = _trustPacks?.GetActivePack()?.Id ?? "internal-only";

        var tiles = config.Tiles.SelectMany(r => r).ToList();
        var chokeDensity = ComputeChokeDensity(tiles);
        var spawnEquity = ComputeSpawnEquity(config.Spawns);
        var recommendations = BuildRecommendations(chokeDensity, spawnEquity, config.Spawns.Count);

        var sightlineReport = $"Map {mapId}: choke density {chokeDensity:P0}, spawn equity {spawnEquity:P0}. " +
                              $"{tiles.Count} tiles analyzed.";
        if (_model is not null)
        {
            try
            {
                var prompt = $"Summarize map flow for {mapId}: choke={chokeDensity:F2}, equity={spawnEquity:F2}. One paragraph, local only.";
                var modelOut = await _model.CompleteAsync(new ModelInput([("user", prompt)]), cancellationToken)
                    .ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(modelOut.Text))
                    sightlineReport = modelOut.Text.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Sightline narrative fallback to heuristic");
            }
        }

        _ = sessionId;
        var inputHash = GameDirectorAudit.HashInput(new { mapId, tileCount = tiles.Count });
        GameDirectorAudit.LogToolExecution(
            _auditLog,
            auditId,
            "validate_map",
            inputHash,
            "local-only",
            trustPolicy,
            $"map={mapId} choke={chokeDensity:F2} equity={spawnEquity:F2}");

        var output = new BrickOutput
        {
            Summary = $"Map validation for {mapId}: choke {chokeDensity:P0}, equity {spawnEquity:P0}."
        };
        output.Set("audit_id", auditId);
        output.Set("choke_density", chokeDensity);
        output.Set("spawn_equity", spawnEquity);
        output.Set("sightline_report", sightlineReport);
        output.Set("recommendations", recommendations);
        return output;
    }

    private static double ComputeChokeDensity(IReadOnlyList<TileRow> tiles)
    {
        if (tiles.Count == 0)
            return 0;
        var chokes = tiles.Count(t => t.TraversalOptions < 2);
        return (double)chokes / tiles.Count;
    }

    private static double ComputeSpawnEquity(IReadOnlyList<SpawnPoint> spawns)
    {
        if (spawns.Count < 2)
            return 1.0;
        var centerX = spawns.Average(s => s.X);
        var centerY = spawns.Average(s => s.Y);
        var distances = spawns.Select(s => Math.Sqrt((s.X - centerX) * (s.X - centerX) + (s.Y - centerY) * (s.Y - centerY))).ToList();
        return 1.0 - Gini(distances);
    }

    private static double Gini(IReadOnlyList<double> values)
    {
        var sorted = values.OrderBy(v => v).ToArray();
        var n = sorted.Length;
        var sum = sorted.Sum();
        if (sum <= 0)
            return 0;
        double cumulative = 0;
        for (var i = 0; i < n; i++)
            cumulative += (2 * (i + 1) - n - 1) * sorted[i];
        return cumulative / (n * sum);
    }

    private static List<string> BuildRecommendations(double choke, double equity, int spawnCount)
    {
        var recs = new List<string>();
        if (choke > 0.25)
            recs.Add("Reduce choke points: add alternate routes near high-density choke tiles.");
        if (equity < 0.7)
            recs.Add("Rebalance spawn distances to objectives for fair team starts.");
        if (spawnCount < 4)
            recs.Add("Consider adding spawn points for 4+ player modes.");
        if (recs.Count == 0)
            recs.Add("Map flow metrics within acceptable ranges for playtest.");
        return recs;
    }

    private static MapConfig ParseConfig(BrickInput input)
    {
        if (!input.ToDictionary().TryGetValue("config", out var raw))
            return new MapConfig([], [], null);

        var je = raw switch
        {
            JsonElement el => el,
            string json when !string.IsNullOrWhiteSpace(json) => JsonDocument.Parse(json).RootElement,
            _ => JsonSerializer.SerializeToElement(raw)
        };

        var tiles = new List<IReadOnlyList<TileRow>>();
        if (je.TryGetProperty("tiles", out var tilesProp))
        {
            if (tilesProp.ValueKind == JsonValueKind.String)
                tilesProp = JsonDocument.Parse(tilesProp.GetString()!).RootElement;

            if (tilesProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var row in tilesProp.EnumerateArray())
            {
                var rowTiles = new List<TileRow>();
                if (row.ValueKind == JsonValueKind.Array)
                {
                    foreach (var cell in row.EnumerateArray())
                    {
                        rowTiles.Add(new TileRow(
                            cell.TryGetProperty("x", out var x) ? x.GetInt32() : 0,
                            cell.TryGetProperty("y", out var y) ? y.GetInt32() : 0,
                            cell.TryGetProperty("traversalOptions", out var t) ? t.GetInt32() : 4));
                    }
                }
                tiles.Add(rowTiles);
            }
            }
        }

        var spawns = new List<SpawnPoint>();
        if (je.TryGetProperty("spawns", out var spawnsProp))
        {
            if (spawnsProp.ValueKind == JsonValueKind.String)
                spawnsProp = JsonDocument.Parse(spawnsProp.GetString()!).RootElement;

            if (spawnsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var s in spawnsProp.EnumerateArray())
                {
                    spawns.Add(new SpawnPoint(
                        s.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "",
                        s.TryGetProperty("x", out var sx) ? sx.GetInt32() : 0,
                        s.TryGetProperty("y", out var sy) ? sy.GetInt32() : 0));
                }
            }
        }

        return new MapConfig(tiles, spawns, null);
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
