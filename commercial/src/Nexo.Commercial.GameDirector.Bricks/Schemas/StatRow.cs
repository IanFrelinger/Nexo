namespace GameDirector.Bricks.Schemas;

/// <summary>Stat row value.</summary>
/// <param name="Id">Id.</param>
/// <param name="Stats">Stats.</param>
public sealed record StatRow(string Id, Dictionary<string, double> Stats);
