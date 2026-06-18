namespace CsvColumnInferrer;

public enum ColumnType
{
    String,
    Integer,
    Decimal,
    Boolean,
    Date
}

/// <summary>
/// Pure, deterministic CSV column type inferencer (S0 spike target brick).
/// Stub returns <see cref="ColumnType.String"/> for every column.
/// </summary>
public static class ColumnTypeInferrer
{
    public static ColumnType InferType(IReadOnlyList<string> values) => ColumnType.String;
}
