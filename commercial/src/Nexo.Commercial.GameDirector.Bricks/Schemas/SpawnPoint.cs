namespace GameDirector.Bricks.Schemas;

/// <summary>Spawn point value.</summary>
/// <param name="Id">Id.</param>
/// <param name="X">X.</param>
/// <param name="Y">Y.</param>
public sealed record SpawnPoint(string Id, int X, int Y);
