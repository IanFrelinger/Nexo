namespace CsvColumnInferrer;

public enum ColumnType
{
    String,
    Integer,
    Decimal,
    Boolean,
    Date
}

/// <summary>Wrong impl: maps yes/no to Boolean silently.</summary>
public static class ColumnTypeInferrer
{
    public static ColumnType InferType(IReadOnlyList<string> values)
    {
        var nonEmpty = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
        if (nonEmpty.Count == 0)
            return ColumnType.String;

        if (nonEmpty.All(v => v.Equals("true", StringComparison.OrdinalIgnoreCase)
                              || v.Equals("false", StringComparison.OrdinalIgnoreCase)
                              || v.Equals("yes", StringComparison.OrdinalIgnoreCase)
                              || v.Equals("no", StringComparison.OrdinalIgnoreCase)))
            return ColumnType.Boolean;

        return ColumnType.String;
    }
}
