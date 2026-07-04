namespace GameDirector.Bricks.Schemas;

public sealed record BalanceAnalysisOutput(
    string AuditId,
    IReadOnlyList<Outlier> Flagged,
    double TtkSpread,
    IReadOnlyList<Delta> Suggestions);
