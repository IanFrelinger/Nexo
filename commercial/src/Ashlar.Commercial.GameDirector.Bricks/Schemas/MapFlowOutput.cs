namespace GameDirector.Bricks.Schemas;

public sealed record MapFlowOutput(
    string AuditId,
    double ChokeDensity,
    double SpawnEquity,
    string SightlineReport,
    IReadOnlyList<string> Recommendations);
