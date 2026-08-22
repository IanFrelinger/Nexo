namespace GameDirector.Bricks.Schemas;

public sealed record BalanceAnalysisInput(
    string SheetId,
    IReadOnlyList<StatRow> Data,
    string? BaselineId);
