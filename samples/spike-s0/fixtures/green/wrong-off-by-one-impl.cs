namespace CsvColumnInferrer;

public enum ColumnType
{
    String,
    Integer,
    Decimal,
    Boolean,
    Date
}

/// <summary>Wrong impl: treats any single integer as Integer (boundary bug).</summary>
public static class ColumnTypeInferrer
{
    public static ColumnType InferType(IReadOnlyList<string> values)
    {
        var nonEmpty = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
        if (nonEmpty.Count == 0)
            return ColumnType.String;

        if (nonEmpty.All(v => int.TryParse(v, out _)))
            return ColumnType.Integer;

        return ColumnType.String;
    }
}
