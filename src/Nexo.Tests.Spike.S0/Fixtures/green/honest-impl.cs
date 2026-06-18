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

        if (nonEmpty.All(v => int.TryParse(v, out _)))
            return ColumnType.Integer;

        return ColumnType.String;
    }
}
