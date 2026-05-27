namespace GameDirector.Bricks.Schemas;

public sealed record StatRow(string Id, Dictionary<string, double> Stats);

public sealed record Outlier(
    string Id,
    string Stat,
    double Value,
    double BaselineValue,
    double DeltaPct);

public sealed record Delta(
    string Id,
    string Stat,
    double SuggestedValue,
    string Rationale);

public sealed record BalanceAnalysisInput(
    string SheetId,
    IReadOnlyList<StatRow> Data,
    string? BaselineId);

public sealed record BalanceAnalysisOutput(
    string AuditId,
    IReadOnlyList<Outlier> Flagged,
    double TtkSpread,
    IReadOnlyList<Delta> Suggestions);
