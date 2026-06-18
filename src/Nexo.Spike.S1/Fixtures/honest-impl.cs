namespace CsvColumnInferrer;

public enum ColumnType
{
    String,
    Integer,
    Decimal,
    Boolean,
    Date
}

public static class ColumnTypeInferrer
{
    public static ColumnType InferType(IReadOnlyList<string> values)
    {
        if (values.Count == 0)
            return ColumnType.String;

        var nonEmpty = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
        if (nonEmpty.Count == 0)
            return ColumnType.String;

        if (nonEmpty.All(IsBoolean))
            return ColumnType.Boolean;

        if (nonEmpty.All(IsDate))
            return ColumnType.Date;

        if (nonEmpty.All(v => int.TryParse(v, out _)))
            return ColumnType.Integer;

        if (nonEmpty.All(v => decimal.TryParse(v, out _)))
            return ColumnType.Decimal;

        return ColumnType.String;
    }

    private static bool IsBoolean(string value) =>
        value.Equals("true", StringComparison.OrdinalIgnoreCase)
        || value.Equals("false", StringComparison.OrdinalIgnoreCase);

    private static bool IsDate(string value) =>
        DateOnly.TryParse(value, out _);
}
