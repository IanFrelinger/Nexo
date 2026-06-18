namespace CsvColumnInferrer;

public enum ColumnType
{
    String,
    Integer,
    Decimal,
    Boolean,
    Date
}

/// <summary>Wrong impl: order-dependent — returns Integer if first cell parses.</summary>
public static class ColumnTypeInferrer
{
    public static ColumnType InferType(IReadOnlyList<string> values)
    {
        var nonEmpty = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
        if (nonEmpty.Count == 0)
            return ColumnType.String;

        if (int.TryParse(nonEmpty[0], out _))
            return ColumnType.Integer;

        return ColumnType.String;
    }
}
