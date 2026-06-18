using System.Globalization;

namespace Nexo.Spike.S1.Transforms;

internal static class SemanticImplTransforms
{
    public static string TypePrecedenceDecimalFirst(string source, int seed)
    {
        var result = TypePrecedenceDecimalFirst(source);
        return result.Replace(
            "return ColumnType.Integer;",
            $"return ColumnType.Integer; /* precedence-seed-{seed} */",
            StringComparison.Ordinal);
    }

    public static string TypePrecedenceZeroOneBool(string source, int seed)
    {
        var zero = SeedVariants.Pick(SeedVariants.ZeroOnePairs, seed);
        var one = zero == "0" ? "1" : "0";
        return source.Replace(
            """
    private static bool IsBoolean(string value) =>
        value.Equals("true", StringComparison.OrdinalIgnoreCase)
        || value.Equals("false", StringComparison.OrdinalIgnoreCase);
""",
            $"""
    private static bool IsBoolean(string value) =>
        value.Equals("true", StringComparison.OrdinalIgnoreCase)
        || value.Equals("false", StringComparison.OrdinalIgnoreCase)
        || value == "{zero}"
        || value == "{one}";
""",
            StringComparison.Ordinal);
    }

    public static string EmptyWhitespaceRetained(string source, int seed)
    {
        var cell = SeedVariants.Pick(SeedVariants.WhitespaceCells, seed);
        return source.Replace(
            "var nonEmpty = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();",
            $"var nonEmpty = values.Select(v => v ?? string.Empty).ToList(); /* witness:{cell} */",
            StringComparison.Ordinal);
    }

    public static string FormatLeadingZeros(string source, int seed)
    {
        var literal = SeedVariants.Pick(SeedVariants.LeadingZeroLiterals, seed);
        return source.Replace(
                "if (nonEmpty.All(v => int.TryParse(v, out _)))",
                "if (nonEmpty.All(v => int.TryParse(v, out _) && !HasLeadingZero(v)))",
                StringComparison.Ordinal)
            .Replace(
                "private static bool IsDate(string value) =>",
                $"""
    private static bool HasLeadingZero(string value) =>
        value.Length > 1 && value[0] == '0';

    private static bool IsDate(string value) => /* witness:{literal} */
""",
                StringComparison.Ordinal);
    }

    public static string FormatThousands(string source, int seed)
    {
        var literal = SeedVariants.Pick(SeedVariants.ThousandsLiterals, seed);
        return source
            .Replace("int.TryParse(v, out _)", "int.TryParse(v.Replace(\",\", \"\"), out _)", StringComparison.Ordinal)
            .Replace("decimal.TryParse(v, out _)", "decimal.TryParse(v.Replace(\",\", \"\"), out _)", StringComparison.Ordinal)
            .Replace(
                "if (nonEmpty.All(v => int.TryParse(v.Replace(\",\", \"\"), out _)))",
                $"if (nonEmpty.All(v => int.TryParse(v.Replace(\",\", \"\"), out _))) /* witness:{literal} */",
                StringComparison.Ordinal);
    }

    public static string FormatScientific(string source, int seed)
    {
        var literal = SeedVariants.Pick(SeedVariants.ScientificLiterals, seed);
        if (!source.Contains("using System.Globalization;", StringComparison.Ordinal))
        {
            source = source.Replace(
                "namespace CsvColumnInferrer;",
                "using System.Globalization;\n\nnamespace CsvColumnInferrer;",
                StringComparison.Ordinal);
        }

        return source.Replace(
            "if (nonEmpty.All(IsBoolean))",
            $"""
        if (nonEmpty.All(v => double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out _)))
            return ColumnType.Decimal; /* witness:{literal} */

        if (nonEmpty.All(IsBoolean))
""",
            StringComparison.Ordinal);
    }

    public static string FormatLocaleComma(string source, int seed)
    {
        var literal = SeedVariants.Pick(SeedVariants.LocaleCommaLiterals, seed);
        if (!source.Contains("using System.Globalization;", StringComparison.Ordinal))
        {
            source = source.Replace(
                "namespace CsvColumnInferrer;",
                "using System.Globalization;\n\nnamespace CsvColumnInferrer;",
                StringComparison.Ordinal);
        }

        return source.Replace(
            "decimal.TryParse(v, out _)",
            $"decimal.TryParse(v.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out _) /* witness:{literal} */",
            StringComparison.Ordinal);
    }

    public static string FormatSignedZero(string source, int seed)
    {
        var literal = SeedVariants.Pick(SeedVariants.SignedZeroLiterals, seed);
        return source.Replace(
            "if (nonEmpty.Count == 0)",
            $"""
        if (nonEmpty.Any(v => v is "+0" or "-0"))
            return ColumnType.String; /* witness:{literal} */

        if (nonEmpty.Count == 0)
""",
            StringComparison.Ordinal);
    }

    public static string SamplingWindow(string source, int seed)
    {
        var window = SeedVariants.PickInt(SeedVariants.SamplingWindowSizes, seed);
        return source.Replace(
            "if (nonEmpty.Count == 0)",
            $"""
        nonEmpty = nonEmpty.Take({window}).ToList();
        if (nonEmpty.Count == 0)
""",
            StringComparison.Ordinal);
    }

    public static string BooleanYesNo(string source, int seed)
    {
        var yes = SeedVariants.Pick(SeedVariants.YesNoLiterals, seed);
        var no = yes.Equals("yes", StringComparison.OrdinalIgnoreCase) ? "no" : "yes";
        return source.Replace(
            """
    private static bool IsBoolean(string value) =>
        value.Equals("true", StringComparison.OrdinalIgnoreCase)
        || value.Equals("false", StringComparison.OrdinalIgnoreCase);
""",
            $"""
    private static bool IsBoolean(string value) =>
        value.Equals("true", StringComparison.OrdinalIgnoreCase)
        || value.Equals("false", StringComparison.OrdinalIgnoreCase)
        || value.Equals("{yes}", StringComparison.OrdinalIgnoreCase)
        || value.Equals("{no}", StringComparison.OrdinalIgnoreCase);
""",
            StringComparison.Ordinal);
    }

    public static string BooleanYn(string source, int seed)
    {
        var y = SeedVariants.Pick(SeedVariants.YnLiterals, seed);
        var n = y.Equals("Y", StringComparison.OrdinalIgnoreCase) ? "N" : "Y";
        return source.Replace(
            """
    private static bool IsBoolean(string value) =>
        value.Equals("true", StringComparison.OrdinalIgnoreCase)
        || value.Equals("false", StringComparison.OrdinalIgnoreCase);
""",
            $"""
    private static bool IsBoolean(string value) =>
        value.Equals("true", StringComparison.OrdinalIgnoreCase)
        || value.Equals("false", StringComparison.OrdinalIgnoreCase)
        || value.Equals("{y}", StringComparison.OrdinalIgnoreCase)
        || value.Equals("{n}", StringComparison.OrdinalIgnoreCase);
""",
            StringComparison.Ordinal);
    }

    public static string HeterogeneousFallback(string source, int seed)
    {
        var result = HeterogeneousFallback(source);
        return result.Replace(
            "return ColumnType.String;",
            $"return ColumnType.String; /* heterogeneous-seed-{seed} */",
            StringComparison.Ordinal);
    }

    private static string TypePrecedenceDecimalFirst(string source)
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

    private static string HeterogeneousFallback(string source)
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
