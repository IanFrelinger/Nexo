namespace GameDirector.Bricks.Schemas;

public sealed record Delta(
    string Id,
    string Stat,
    double SuggestedValue,
    string Rationale);
