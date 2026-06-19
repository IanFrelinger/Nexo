using System.Text;
using System.Text.RegularExpressions;
using Nexo.Spike.S1.Adversary;
using Nexo.Spike.S1.IntentDensity;

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
    public const string CatalogVersion = "s1.4-v1";

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

    public static string ApplyImplTransform(TransformTag tag, string source) =>
        ApplyImplTransform(tag, source, 0);

    public static string ApplyImplTransform(TransformTag tag, string source, int seed)
    {
        if (tag == TransformTag.HonestNoOp)
            return source;

        var transformed = ApplyImplTransformCore(tag, source, seed);
        return $"{transformed.TrimEnd()}\n// seed-perturb-{seed}\n";
    }

    private static string ApplyImplTransformCore(TransformTag tag, string source, int seed) => tag switch
    {
        TransformTag.HonestNoOp => source,
        TransformTag.OffByOne => ImplTransforms.OffByOne(source, seed),
        TransformTag.BoundaryInclusive => ImplTransforms.BoundaryInclusive(source, seed),
        TransformTag.NegatedCondition => ImplTransforms.NegatedCondition(source, seed),
        TransformTag.DroppedBranch => ImplTransforms.DroppedBranch(source, seed),
        TransformTag.ConstantReturn => ImplTransforms.ConstantReturn(source, seed),
        TransformTag.SwappedOperands => ImplTransforms.SwappedOperands(source, seed),
        TransformTag.SemanticTypePrecedenceDecimalFirst => SemanticImplTransforms.TypePrecedenceDecimalFirst(source, seed),
        TransformTag.SemanticTypePrecedenceZeroOneBool => SemanticImplTransforms.TypePrecedenceZeroOneBool(source, seed),
        TransformTag.SemanticEmptyWhitespaceRetained => SemanticImplTransforms.EmptyWhitespaceRetained(source, seed),
        TransformTag.SemanticFormatLeadingZeros => SemanticImplTransforms.FormatLeadingZeros(source, seed),
        TransformTag.SemanticFormatThousands => SemanticImplTransforms.FormatThousands(source, seed),
        TransformTag.SemanticFormatScientific => SemanticImplTransforms.FormatScientific(source, seed),
        TransformTag.SemanticFormatLocaleComma => SemanticImplTransforms.FormatLocaleComma(source, seed),
        TransformTag.SemanticFormatSignedZero => SemanticImplTransforms.FormatSignedZero(source, seed),
        TransformTag.SemanticSamplingWindow => SemanticImplTransforms.SamplingWindow(source, seed),
        TransformTag.SemanticBooleanYesNo => SemanticImplTransforms.BooleanYesNo(source, seed),
        TransformTag.SemanticBooleanYn => SemanticImplTransforms.BooleanYn(source, seed),
        TransformTag.SemanticHeterogeneousFallback => SemanticImplTransforms.HeterogeneousFallback(source, seed),
        _ => throw new ArgumentOutOfRangeException(nameof(tag), tag, "Not an impl transform")
    };

    public static string ApplyTestTransform(TransformTag tag, string source) =>
        ApplyTestTransform(tag, source, 0);

    public static string ApplyTestTransform(TransformTag tag, string source, int seed)
    {
        if (tag == TransformTag.HonestNoOp)
            return source;

        var transformed = ApplyTestTransformCore(tag, source, seed);
        return $"{transformed.TrimEnd()}\n// seed-perturb-{seed}\n";
    }

    private static string ApplyTestTransformCore(TransformTag tag, string source, int seed) => tag switch
    {
        TransformTag.HonestNoOp => source,
        TransformTag.AssertionRemoved => TestTransforms.AssertionRemoved(source, seed),
        TransformTag.TautologyReplacement => TestTransforms.TautologyReplacement(source, seed),
        TransformTag.OverNarrowDomain => TestTransforms.OverNarrowDomain(source, seed),
        TransformTag.TypeOnlyAssert => TestTransforms.TypeOnlyAssert(source, seed),
        _ => throw new ArgumentOutOfRangeException(nameof(tag), tag, "Not a test transform")
    };

    public static string BuildImplDiff(TransformTag tag, int seed = 0)
    {
        var honest = Implementation;
        var defective = ApplyImplTransform(tag, honest, seed);
        return ImplDiffBuilder.Build(honest, defective);
    }

    public static string BuildTestDiff(TransformTag tag, int seed = 0)
    {
        var honest = Tests;
        var defective = ApplyTestTransform(tag, honest, seed);
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

internal static class TestTransforms
{
    private static readonly Regex ShouldBeRegex = new(
        @"ColumnTypeInferrer\.InferType\(([^)]*)\)\.Should\(\)\.Be\(ColumnType\.\w+\);",
        RegexOptions.Compiled);

    public static string AssertionRemoved(string source, int seed) =>
        ShouldBeRegex.Replace(source, $"ColumnTypeInferrer.InferType($1); /* removed seed {seed} */");

    public static string TautologyReplacement(string source, int seed) =>
        ShouldBeRegex.Replace(
            source,
            $"var __t{seed} = ColumnTypeInferrer.InferType($1); __t{seed}.Should().Be(__t{seed});");

    public static string OverNarrowDomain(string source, int seed)
    {
        var facts = new[]
        {
            """
    [Fact]
    public void Integer_column_is_inferred_as_Integer()
    {
        ColumnTypeInferrer.InferType(["1", "2", "3"]).Should().Be(ColumnType.Integer);
    }
""",
            """
    [Fact]
    public void Decimal_column_is_inferred_as_Decimal()
    {
        ColumnTypeInferrer.InferType(["1.5", "2.0"]).Should().Be(ColumnType.Decimal);
    }
""",
            """
    [Fact]
    public void Boolean_column_is_inferred_as_Boolean()
    {
        ColumnTypeInferrer.InferType(["true", "false"]).Should().Be(ColumnType.Boolean);
    }
""",
            """
    [Fact]
    public void Date_column_is_inferred_as_Date()
    {
        ColumnTypeInferrer.InferType(["2024-01-15"]).Should().Be(ColumnType.Date);
    }
""",
            """
    [Fact]
    public void Text_column_is_inferred_as_String()
    {
        ColumnTypeInferrer.InferType(["hello", "world"]).Should().Be(ColumnType.String);
    }
""",
            """
    [Fact]
    public void Mixed_numeric_and_text_is_String()
    {
        ColumnTypeInferrer.InferType(["1", "hello"]).Should().Be(ColumnType.String);
    }
"""
        };

        var keep = facts[seed % facts.Length];
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

    public static string TypeOnlyAssert(string source, int seed)
    {
        source = EnsureSystemUsing(source);
        return ShouldBeRegex.Replace(
            source,
            match =>
                $"{{ var __r{seed} = ColumnTypeInferrer.InferType({match.Groups[1].Value}); __r{seed}.Should().BeOneOf(Enum.GetValues<ColumnType>()); /* type-only seed {seed} */ }}");
    }

    private static string EnsureSystemUsing(string source)
    {
        if (source.Contains("using System;", StringComparison.Ordinal))
            return source;

        return source.Replace(
            "using FluentAssertions;",
            "using System;\nusing FluentAssertions;",
            StringComparison.Ordinal);
    }
}
