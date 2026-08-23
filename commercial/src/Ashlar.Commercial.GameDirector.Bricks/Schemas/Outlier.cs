namespace GameDirector.Bricks.Schemas;

public sealed record Outlier(
    string Id,
    string Stat,
    double Value,
    double BaselineValue,
    double DeltaPct);
