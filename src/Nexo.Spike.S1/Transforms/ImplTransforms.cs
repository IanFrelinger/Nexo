namespace Nexo.Spike.S1.Transforms;

internal static class ImplTransforms
{
    public static string OffByOne(string source, int seed)
    {
        var threshold = SeedVariants.PickInt(SeedVariants.OffByOneThresholds, seed);
        return source.Replace(
            "if (nonEmpty.All(v => int.TryParse(v, out _)))",
            $"if (nonEmpty.Count >= {threshold} && nonEmpty.All(v => int.TryParse(v, out _)))",
            StringComparison.Ordinal);
    }

    public static string BoundaryInclusive(string source, int seed)
    {
        var limit = SeedVariants.PickInt(SeedVariants.BoundaryInclusiveLimits, seed);
        return source.Replace(
            "if (values.Count == 0)",
            $"if (values.Count <= {limit})",
            StringComparison.Ordinal);
    }

    public static string NegatedCondition(string source, int seed)
    {
        if (seed % 2 == 0)
        {
            return source.Replace(
                "if (nonEmpty.All(IsBoolean))",
                "if (!nonEmpty.All(IsBoolean))",
                StringComparison.Ordinal);
        }

        return source.Replace(
            "if (nonEmpty.All(IsDate))",
            "if (!nonEmpty.All(IsDate))",
            StringComparison.Ordinal);
    }

    public static string DroppedBranch(string source, int seed)
    {
        if (seed % 2 == 0)
        {
            const string block = """

        if (nonEmpty.All(IsDate))
            return ColumnType.Date;

""";
            return source.Replace(block, "\n", StringComparison.Ordinal);
        }

        const string booleanBlock = """

        if (nonEmpty.All(IsBoolean))
            return ColumnType.Boolean;

""";
        return source.Replace(booleanBlock, "\n", StringComparison.Ordinal);
    }

    public static string ConstantReturn(string source, int seed)
    {
        var returnType = (seed % 3) switch
        {
            0 => "String",
            1 => "Integer",
            _ => "Boolean"
        };

        const string marker = "public static ColumnType InferType(IReadOnlyList<string> values)";
        var idx = source.IndexOf(marker, StringComparison.Ordinal);
        if (idx < 0)
            return source;

        var brace = source.IndexOf('{', idx);
        if (brace < 0)
            return source;

        return source[..(brace + 1)] + $"\n        return ColumnType.{returnType};\n    }}\n}}\n";
    }

    public static string SwappedOperands(string source, int seed)
    {
        if (seed % 2 == 0)
        {
            return SwapIntegerDecimal(source);
        }

        const string boolean = """
        if (nonEmpty.All(IsBoolean))
            return ColumnType.Boolean;

""";
        const string date = """
        if (nonEmpty.All(IsDate))
            return ColumnType.Date;

""";
        if (!source.Contains(boolean, StringComparison.Ordinal) ||
            !source.Contains(date, StringComparison.Ordinal))
        {
            return SwapIntegerDecimal(source);
        }

        return source
            .Replace(boolean, "__DATE_PLACEHOLDER__", StringComparison.Ordinal)
            .Replace(date, boolean, StringComparison.Ordinal)
            .Replace("__DATE_PLACEHOLDER__", date, StringComparison.Ordinal);
    }

    private static string SwapIntegerDecimal(string source)
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
            .Replace(integer, "__DECIMAL_PLACEHOLDER__", StringComparison.Ordinal)
            .Replace(decimalBlock, integer, StringComparison.Ordinal)
            .Replace("__DECIMAL_PLACEHOLDER__", decimalBlock, StringComparison.Ordinal);
    }
}
