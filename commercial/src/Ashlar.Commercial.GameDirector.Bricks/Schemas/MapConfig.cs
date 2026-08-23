namespace GameDirector.Bricks.Schemas;

public sealed record MapConfig(
    IReadOnlyList<IReadOnlyList<TileRow>> Tiles,
    IReadOnlyList<SpawnPoint> Spawns,
    Dictionary<string, object>? Metadata);
