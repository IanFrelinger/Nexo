namespace GameDirector.Bricks.Schemas;

public sealed record TileRow(int X, int Y, int TraversalOptions);

public sealed record SpawnPoint(string Id, int X, int Y);

public sealed record MapConfig(
    IReadOnlyList<IReadOnlyList<TileRow>> Tiles,
    IReadOnlyList<SpawnPoint> Spawns,
    Dictionary<string, object>? Metadata);

public sealed record MapFlowInput(string MapId, MapConfig Config, string? SessionId);

public sealed record MapFlowOutput(
    string AuditId,
    double ChokeDensity,
    double SpawnEquity,
    string SightlineReport,
    IReadOnlyList<string> Recommendations);
