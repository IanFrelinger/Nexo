namespace GameDirector.Bricks.Schemas;

/// <summary>Map flow input value.</summary>
/// <param name="MapId">Map id.</param>
/// <param name="Config">Config.</param>
/// <param name="SessionId">Session id.</param>
public sealed record MapFlowInput(string MapId, MapConfig Config, string? SessionId);
