using Nexo.Spike.S1.Adversary;

namespace Nexo.Spike.S1.Transforms;

public sealed record TransformDefinition(
    TransformTag Tag,
    TransformFamily Family,
    string Hypothesis,
    string ExpectedRelation);

public static class TransformAttribution
{
    public static TransformDefinition? TryGet(TransformTag tag) =>
        All.TryGetValue(tag, out var definition) ? definition : null;

    public static TransformDefinition Get(TransformTag tag) =>
        TryGet(tag) ?? throw new KeyNotFoundException($"No attribution for transform tag {tag}");

    public static IReadOnlyDictionary<TransformTag, TransformDefinition> All { get; } =
        Build().ToDictionary(d => d.Tag);

    private static IEnumerable<TransformDefinition> Build()
    {
        foreach (var tag in TransformCatalog.CoarseWrongImplTags)
        {
            yield return tag switch
            {
                TransformTag.OffByOne => new(
                    tag,
                    TransformFamily.WrongImpl,
                    "Integer branch requires too many cells (off-by-one on count threshold).",
                    "acceptance: [\"1\",\"2\",\"3\"] => Integer"),
                TransformTag.BoundaryInclusive => new(
                    tag,
                    TransformFamily.WrongImpl,
                    "Single-value columns treated as empty (inclusive boundary on row count).",
                    "acceptance: [\"2024-01-15\"] => Date"),
                TransformTag.NegatedCondition => new(
                    tag,
                    TransformFamily.WrongImpl,
                    "Boolean branch polarity inverted.",
                    "acceptance: [\"true\",\"false\"] => Boolean"),
                TransformTag.DroppedBranch => new(
                    tag,
                    TransformFamily.WrongImpl,
                    "Date branch removed entirely.",
                    "acceptance: [\"2024-01-15\"] => Date"),
                TransformTag.ConstantReturn => new(
                    tag,
                    TransformFamily.WrongImpl,
                    "All columns collapse to constant String return.",
                    "acceptance: [\"1\",\"2\",\"3\"] => Integer"),
                TransformTag.SwappedOperands => new(
                    tag,
                    TransformFamily.WrongImpl,
                    "Integer and decimal precedence swapped.",
                    "acceptance: [\"1\",\"2\",\"3\"] => Integer; [\"1.5\",\"2.0\"] => Decimal"),
                _ => new(tag, TransformFamily.WrongImpl, "Coarse defect.", "PropertyGate")
            };
        }

        foreach (var tag in TransformCatalog.SemanticWrongImplTags)
        {
            yield return tag switch
            {
                TransformTag.SemanticTypePrecedenceDecimalFirst => new(
                    tag,
                    TransformFamily.WrongImpl,
                    "Decimal branch runs before integer, misclassifying whole-number columns.",
                    "acceptance: [\"1\",\"2\",\"3\"] => Integer"),
                TransformTag.SemanticTypePrecedenceZeroOneBool => new(
                    tag,
                    TransformFamily.WrongImpl,
                    "Numeric 0/1 literals classified as Boolean instead of String.",
                    "none — gap: 0/1 numeric string literals not in frozen acceptance criteria"),
                TransformTag.SemanticEmptyWhitespaceRetained => new(
                    tag,
                    TransformFamily.WrongImpl,
                    "Whitespace-only cells kept in sample instead of treated as empty.",
                    "invariant: whitespace-only cells treated as empty"),
                TransformTag.SemanticFormatLeadingZeros => new(
                    tag,
                    TransformFamily.WrongImpl,
                    "Leading-zero numerics (e.g. \"007\") rejected from Integer branch.",
                    "none — gap: leading-zero format literals not in frozen acceptance criteria"),
                TransformTag.SemanticFormatThousands => new(
                    tag,
                    TransformFamily.WrongImpl,
                    "Thousands separators stripped before numeric parse (\"1,000\" -> Integer).",
                    "none — gap: thousands-separator format not in frozen acceptance criteria"),
                TransformTag.SemanticFormatScientific => new(
                    tag,
                    TransformFamily.WrongImpl,
                    "Scientific notation accepted via double parse (\"1e3\" -> Decimal).",
                    "none — gap: scientific notation literals not in frozen acceptance criteria"),
                TransformTag.SemanticFormatLocaleComma => new(
                    tag,
                    TransformFamily.WrongImpl,
                    "Locale comma decimals parsed (\"1,5\" -> Decimal).",
                    "none — gap: locale comma decimal format not in frozen acceptance criteria"),
                TransformTag.SemanticFormatSignedZero => new(
                    tag,
                    TransformFamily.WrongImpl,
                    "Signed-zero literals (\"-0\", \"+0\") forced to String.",
                    "none — gap: signed-zero format literals not in frozen acceptance criteria"),
                TransformTag.SemanticSamplingWindow => new(
                    tag,
                    TransformFamily.WrongImpl,
                    "Inference uses only first two non-empty cells, missing later type-widening values.",
                    "none — gap: sampling-window / column-length invariance not in frozen criteria"),
                TransformTag.SemanticBooleanYesNo => new(
                    tag,
                    TransformFamily.WrongImpl,
                    "Colloquial yes/no tokens classified as Boolean.",
                    "none — gap: yes/no boolean ambiguity not in frozen acceptance criteria"),
                TransformTag.SemanticBooleanYn => new(
                    tag,
                    TransformFamily.WrongImpl,
                    "Single-letter Y/N tokens classified as Boolean.",
                    "none — gap: Y/N boolean ambiguity not in frozen acceptance criteria"),
                TransformTag.SemanticHeterogeneousFallback => new(
                    tag,
                    TransformFamily.WrongImpl,
                    "Mixed columns fall back to partial numeric type instead of String.",
                    "acceptance: mixed numeric and text => String"),
                _ => new(tag, TransformFamily.WrongImpl, "Semantic defect.", "PropertyGate")
            };
        }

        foreach (var tag in TransformCatalog.WeakTestTags)
        {
            yield return tag switch
            {
                TransformTag.AssertionRemoved => new(
                    tag,
                    TransformFamily.WeakTest,
                    "Example assertions removed; tests compile but do not check outcomes.",
                    "mutation score >= threshold (example assertions killed)"),
                TransformTag.TautologyReplacement => new(
                    tag,
                    TransformFamily.WeakTest,
                    "Assertions replaced with tautology (actual compared to itself).",
                    "mutation score >= threshold (tautology detection / RED discipline)"),
                TransformTag.OverNarrowDomain => new(
                    tag,
                    TransformFamily.WeakTest,
                    "Only one example input domain covered.",
                    "mutation score >= threshold (breadth of example coverage)"),
                TransformTag.TypeOnlyAssert => new(
                    tag,
                    TransformFamily.WeakTest,
                    "Assertions weakened to type-only calls without value checks.",
                    "mutation score >= threshold (value assertions killed)"),
                _ => new(tag, TransformFamily.WeakTest, "Weak test defect.", "MutationGate")
            };
        }

        yield return new TransformDefinition(
            TransformTag.HonestNoOp,
            TransformFamily.HonestBaseline,
            "Honest implementation and tests (no transform).",
            "baseline acceptance");
    }

    public static string ResolveCaughtBy(string rawOutput, TransformDefinition definition)
    {
        if (rawOutput.Contains("acceptance criterion", StringComparison.OrdinalIgnoreCase))
            return definition.ExpectedRelation.StartsWith("acceptance:", StringComparison.Ordinal)
                ? definition.ExpectedRelation
                : "frozen acceptance criteria (spec-derived)";

        if (rawOutput.Contains("permutation", StringComparison.OrdinalIgnoreCase)
            || rawOutput.Contains("OrderPairs", StringComparison.Ordinal))
            return "metamorphic: value-order invariance";

        if (rawOutput.Contains("deterministic", StringComparison.OrdinalIgnoreCase))
            return "metamorphic: determinism";

        if (rawOutput.Contains("mutation score", StringComparison.OrdinalIgnoreCase))
            return definition.ExpectedRelation;

        return definition.ExpectedRelation;
    }

    public static string ResolveMissingRelation(TransformDefinition definition) =>
        definition.ExpectedRelation.StartsWith("none — gap", StringComparison.Ordinal)
            ? definition.ExpectedRelation
            : definition.ExpectedRelation;
}
