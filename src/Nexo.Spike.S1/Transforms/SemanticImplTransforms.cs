using System.Globalization;

namespace Nexo.Spike.S1.Transforms;

internal static class SemanticImplTransforms
{
    public static string TypePrecedenceDecimalFirst(string source)
    {
        const string integer = """
        if (nonEmpty.All(v => int.TryParse(v, out _)))
            return ColumnType.Integer;

""";
        const string decimalBlock = """
        if (nonEmpty.All(v => decimal.TryParse(v, out _)))
            return ColumnType.Decimal;

""";
        if (!source.Contains(integer, StringComparison.Ordinal) ||
            !source.Contains(decimalBlock, StringComparison.Ordinal))
        {
            return source;
        }

        return source
            .Replace(integer, "__INTEGER_PLACEHOLDER__", StringComparison.Ordinal)
            .Replace(decimalBlock, integer, StringComparison.Ordinal)
            .Replace("__INTEGER_PLACEHOLDER__", decimalBlock, StringComparison.Ordinal);
    }

    public static string TypePrecedenceZeroOneBool(string source) =>
        source.Replace(
            """
    private static bool IsBoolean(string value) =>
        value.Equals("true", StringComparison.OrdinalIgnoreCase)
        || value.Equals("false", StringComparison.OrdinalIgnoreCase);
""",
            """
    private static bool IsBoolean(string value) =>
        value.Equals("true", StringComparison.OrdinalIgnoreCase)
        || value.Equals("false", StringComparison.OrdinalIgnoreCase)
        || value == "0"
        || value == "1";
""",
            StringComparison.Ordinal);

    public static string EmptyWhitespaceRetained(string source) =>
        source.Replace(
            "var nonEmpty = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();",
            "var nonEmpty = values.Select(v => v ?? string.Empty).ToList();",
            StringComparison.Ordinal);

    public static string FormatLeadingZeros(string source) =>
        source.Replace(
            "if (nonEmpty.All(v => int.TryParse(v, out _)))",
            "if (nonEmpty.All(v => int.TryParse(v, out _) && !HasLeadingZero(v)))",
            StringComparison.Ordinal)
        .Replace(
            "private static bool IsDate(string value) =>",
            """
    private static bool HasLeadingZero(string value) =>
        value.Length > 1 && value[0] == '0';

    private static bool IsDate(string value) =>
""",
            StringComparison.Ordinal);

    public static string FormatThousands(string source) =>
        source
            .Replace("int.TryParse(v, out _)", "int.TryParse(v.Replace(\",\", \"\"), out _)", StringComparison.Ordinal)
            .Replace("decimal.TryParse(v, out _)", "decimal.TryParse(v.Replace(\",\", \"\"), out _)", StringComparison.Ordinal);

    public static string FormatScientific(string source)
    {
        if (!source.Contains("using System.Globalization;", StringComparison.Ordinal))
        {
            source = source.Replace(
                "namespace CsvColumnInferrer;",
                "using System.Globalization;\n\nnamespace CsvColumnInferrer;",
                StringComparison.Ordinal);
        }

        return source.Replace(
            "if (nonEmpty.All(IsBoolean))",
            """
        if (nonEmpty.All(v => double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out _)))
            return ColumnType.Decimal;

        if (nonEmpty.All(IsBoolean))
""",
            StringComparison.Ordinal);
    }

    public static string FormatLocaleComma(string source)
    {
        if (!source.Contains("using System.Globalization;", StringComparison.Ordinal))
        {
            source = source.Replace(
                "namespace CsvColumnInferrer;",
                "using System.Globalization;\n\nnamespace CsvColumnInferrer;",
                StringComparison.Ordinal);
        }

        return source.Replace(
            "decimal.TryParse(v, out _)",
            "decimal.TryParse(v.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out _)",
            StringComparison.Ordinal);
    }

    public static string FormatSignedZero(string source) =>
        source.Replace(
            "if (nonEmpty.Count == 0)",
            """
        if (nonEmpty.Any(v => v is "+0" or "-0"))
            return ColumnType.String;

        if (nonEmpty.Count == 0)
""",
            StringComparison.Ordinal);

    public static string SamplingWindow(string source) =>
        source.Replace(
            "if (nonEmpty.Count == 0)",
            """
        nonEmpty = nonEmpty.Take(2).ToList();
        if (nonEmpty.Count == 0)
""",
            StringComparison.Ordinal);

    public static string BooleanYesNo(string source) =>
        source.Replace(
            """
    private static bool IsBoolean(string value) =>
        value.Equals("true", StringComparison.OrdinalIgnoreCase)
        || value.Equals("false", StringComparison.OrdinalIgnoreCase);
""",
            """
    private static bool IsBoolean(string value) =>
        value.Equals("true", StringComparison.OrdinalIgnoreCase)
        || value.Equals("false", StringComparison.OrdinalIgnoreCase)
        || value.Equals("yes", StringComparison.OrdinalIgnoreCase)
        || value.Equals("no", StringComparison.OrdinalIgnoreCase);
""",
            StringComparison.Ordinal);

    public static string BooleanYn(string source) =>
        source.Replace(
            """
    private static bool IsBoolean(string value) =>
        value.Equals("true", StringComparison.OrdinalIgnoreCase)
        || value.Equals("false", StringComparison.OrdinalIgnoreCase);
""",
            """
    private static bool IsBoolean(string value) =>
        value.Equals("true", StringComparison.OrdinalIgnoreCase)
        || value.Equals("false", StringComparison.OrdinalIgnoreCase)
        || value.Equals("y", StringComparison.OrdinalIgnoreCase)
        || value.Equals("n", StringComparison.OrdinalIgnoreCase);
""",
            StringComparison.Ordinal);

    public static string HeterogeneousFallback(string source)
    {
        const string finalReturn = """
        if (nonEmpty.All(v => decimal.TryParse(v, out _)))
            return ColumnType.Decimal;

        return ColumnType.String;
""";
        const string replacement = """
        if (nonEmpty.All(v => decimal.TryParse(v, out _)))
            return ColumnType.Decimal;

        if (nonEmpty.Any(v => int.TryParse(v, out _)))
            return ColumnType.Integer;
        if (nonEmpty.Any(v => decimal.TryParse(v, out _)))
            return ColumnType.Decimal;
        return ColumnType.String;
""";
        return source.Replace(finalReturn, replacement, StringComparison.Ordinal);
    }
}
