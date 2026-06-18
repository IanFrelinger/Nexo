using System.Text;
using System.Text.RegularExpressions;
using Nexo.Spike.S1.Adversary;

namespace Nexo.Spike.S1.Transforms;

public static class HonestFixtures
{
    private static string? _impl;
    private static string? _tests;

    public static string Implementation =>
        _impl ??= ReadFixture("honest-impl.cs");

    public static string Tests =>
        _tests ??= ReadFixture("honest-tests.cs");

    private static string ReadFixture(string name)
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Fixtures", name),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "Nexo.Spike.S1", "Fixtures", name)
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path))
                return File.ReadAllText(path);
        }

        throw new FileNotFoundException($"Fixture not found: {name}");
    }
}

public static class TransformCatalog
{
    public const string CatalogVersion = "s1.1-v1";

    public static IReadOnlyList<TransformTag> CoarseWrongImplTags { get; } =
    [
        TransformTag.OffByOne,
        TransformTag.BoundaryInclusive,
        TransformTag.NegatedCondition,
        TransformTag.DroppedBranch,
        TransformTag.ConstantReturn,
        TransformTag.SwappedOperands
    ];

    public static IReadOnlyList<TransformTag> SemanticWrongImplTags { get; } =
    [
        TransformTag.SemanticTypePrecedenceDecimalFirst,
        TransformTag.SemanticTypePrecedenceZeroOneBool,
        TransformTag.SemanticEmptyWhitespaceRetained,
        TransformTag.SemanticFormatLeadingZeros,
        TransformTag.SemanticFormatThousands,
        TransformTag.SemanticFormatScientific,
        TransformTag.SemanticFormatLocaleComma,
        TransformTag.SemanticFormatSignedZero,
        TransformTag.SemanticSamplingWindow,
        TransformTag.SemanticBooleanYesNo,
        TransformTag.SemanticBooleanYn,
        TransformTag.SemanticHeterogeneousFallback
    ];

    public static IReadOnlyList<TransformTag> WrongImplTags { get; } =
        CoarseWrongImplTags.Concat(SemanticWrongImplTags).ToList();

    public static IReadOnlyList<TransformTag> WeakTestTags { get; } =
    [
        TransformTag.AssertionRemoved,
        TransformTag.TautologyReplacement,
        TransformTag.OverNarrowDomain,
        TransformTag.TypeOnlyAssert
    ];

    public static IReadOnlyList<double> WeakTestThresholdSweep { get; } = [60, 75, 90];

    public static string ApplyImplTransform(TransformTag tag, string source) => tag switch
    {
        TransformTag.HonestNoOp => source,
        TransformTag.OffByOne => ImplTransforms.OffByOne(source),
        TransformTag.BoundaryInclusive => ImplTransforms.BoundaryInclusive(source),
        TransformTag.NegatedCondition => ImplTransforms.NegatedCondition(source),
        TransformTag.DroppedBranch => ImplTransforms.DroppedBranch(source),
        TransformTag.ConstantReturn => ImplTransforms.ConstantReturn(source),
        TransformTag.SwappedOperands => ImplTransforms.SwappedOperands(source),
        TransformTag.SemanticTypePrecedenceDecimalFirst => SemanticImplTransforms.TypePrecedenceDecimalFirst(source),
        TransformTag.SemanticTypePrecedenceZeroOneBool => SemanticImplTransforms.TypePrecedenceZeroOneBool(source),
        TransformTag.SemanticEmptyWhitespaceRetained => SemanticImplTransforms.EmptyWhitespaceRetained(source),
        TransformTag.SemanticFormatLeadingZeros => SemanticImplTransforms.FormatLeadingZeros(source),
        TransformTag.SemanticFormatThousands => SemanticImplTransforms.FormatThousands(source),
        TransformTag.SemanticFormatScientific => SemanticImplTransforms.FormatScientific(source),
        TransformTag.SemanticFormatLocaleComma => SemanticImplTransforms.FormatLocaleComma(source),
        TransformTag.SemanticFormatSignedZero => SemanticImplTransforms.FormatSignedZero(source),
        TransformTag.SemanticSamplingWindow => SemanticImplTransforms.SamplingWindow(source),
        TransformTag.SemanticBooleanYesNo => SemanticImplTransforms.BooleanYesNo(source),
        TransformTag.SemanticBooleanYn => SemanticImplTransforms.BooleanYn(source),
        TransformTag.SemanticHeterogeneousFallback => SemanticImplTransforms.HeterogeneousFallback(source),
        _ => throw new ArgumentOutOfRangeException(nameof(tag), tag, "Not an impl transform")
    };

    public static string ApplyTestTransform(TransformTag tag, string source) => tag switch
    {
        TransformTag.HonestNoOp => source,
        TransformTag.AssertionRemoved => TestTransforms.AssertionRemoved(source),
        TransformTag.TautologyReplacement => TestTransforms.TautologyReplacement(source),
        TransformTag.OverNarrowDomain => TestTransforms.OverNarrowDomain(source),
        TransformTag.TypeOnlyAssert => TestTransforms.TypeOnlyAssert(source),
        _ => throw new ArgumentOutOfRangeException(nameof(tag), tag, "Not a test transform")
    };

    public static string BuildImplDiff(TransformTag tag)
    {
        var honest = Implementation;
        var defective = ApplyImplTransform(tag, honest);
        return ImplDiffBuilder.Build(honest, defective);
    }

    public static string BuildTestDiff(TransformTag tag)
    {
        var honest = Tests;
        var defective = ApplyTestTransform(tag, honest);
        return ImplDiffBuilder.Build(honest, defective);
    }

    private static string Implementation => HonestFixtures.Implementation;
    private static string Tests => HonestFixtures.Tests;
}

internal static class ImplDiffBuilder
{
    public static string Build(string before, string after)
    {
        if (string.Equals(before, after, StringComparison.Ordinal))
            return "(no diff)";

        var beforeLines = before.Split('\n');
        var afterLines = after.Split('\n');
        var sb = new StringBuilder();
        var max = Math.Max(beforeLines.Length, afterLines.Length);
        for (var i = 0; i < max; i++)
        {
            var left = i < beforeLines.Length ? beforeLines[i] : null;
            var right = i < afterLines.Length ? afterLines[i] : null;
            if (left == right)
                continue;

            if (left is not null)
                sb.AppendLine($"- {left.TrimEnd()}");
            if (right is not null)
                sb.AppendLine($"+ {right.TrimEnd()}");
        }

        return sb.Length == 0 ? "(no diff)" : sb.ToString().TrimEnd();
    }
}

internal static class ImplTransforms
{
    public static string OffByOne(string source) =>
        source.Replace(
            "if (nonEmpty.All(v => int.TryParse(v, out _)))",
            "if (nonEmpty.Count >= 4 && nonEmpty.All(v => int.TryParse(v, out _)))",
            StringComparison.Ordinal);

    public static string BoundaryInclusive(string source) =>
        source.Replace(
            "if (values.Count == 0)",
            "if (values.Count <= 1)",
            StringComparison.Ordinal);

    public static string NegatedCondition(string source) =>
        source.Replace(
            "if (nonEmpty.All(IsBoolean))",
            "if (!nonEmpty.All(IsBoolean))",
            StringComparison.Ordinal);

    public static string DroppedBranch(string source)
    {
        const string block = """

        if (nonEmpty.All(IsDate))
            return ColumnType.Date;

""";
        return source.Replace(block, "\n", StringComparison.Ordinal);
    }

    public static string ConstantReturn(string source)
    {
        const string marker = "public static ColumnType InferType(IReadOnlyList<string> values)";
        var idx = source.IndexOf(marker, StringComparison.Ordinal);
        if (idx < 0)
            return source;

        var brace = source.IndexOf('{', idx);
        if (brace < 0)
            return source;

        return source[..(brace + 1)] + "\n        return ColumnType.String;\n    }\n}\n";
    }

    public static string SwappedOperands(string source)
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

internal static class TestTransforms
{
    private static readonly Regex ShouldBeRegex = new(
        @"ColumnTypeInferrer\.InferType\(([^)]*)\)\.Should\(\)\.Be\(ColumnType\.\w+\);",
        RegexOptions.Compiled);

    public static string AssertionRemoved(string source) =>
        ShouldBeRegex.Replace(source, "ColumnTypeInferrer.InferType($1);");

    public static string TautologyReplacement(string source) =>
        ShouldBeRegex.Replace(
            source,
            "var __t = ColumnTypeInferrer.InferType($1); __t.Should().Be(__t);");

    public static string OverNarrowDomain(string source)
    {
        const string keep = """
    [Fact]
    public void Integer_column_is_inferred_as_Integer()
    {
        ColumnTypeInferrer.InferType(["1", "2", "3"]).Should().Be(ColumnType.Integer);
    }
""";
        var sb = new StringBuilder();
        sb.AppendLine("using FluentAssertions;");
        sb.AppendLine("using CsvColumnInferrer;");
        sb.AppendLine("using Xunit;");
        sb.AppendLine();
        sb.AppendLine("namespace CsvColumnInferrer.Tests;");
        sb.AppendLine();
        sb.AppendLine("public sealed class ColumnTypeInferrerRedTests");
        sb.AppendLine("{");
        sb.AppendLine(keep);
        sb.AppendLine("}");
        return sb.ToString();
    }

    public static string TypeOnlyAssert(string source) =>
        ShouldBeRegex.Replace(source, "ColumnTypeInferrer.InferType($1);");
}
