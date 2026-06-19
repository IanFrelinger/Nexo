using Nexo.Spike.S1.Adversary;

namespace Nexo.Spike.S1.IntentDensity;

public enum ProbePinStatus
{
    Pinned,
    Unpinned
}

public sealed record ProbeClass(
    string Id,
    string Description,
    IReadOnlyList<string> WitnessInputs,
    TransformTag DivergentTransform,
    bool ExpectedPinned);

public static class ProbeCorpus
{
    public const string ProbeCorpusVersion = "s1.3-v1";

    public static IReadOnlyList<ProbeClass> All { get; } =
    [
        new(
            "zero-one-literals",
            "Numeric 0/1 string literals vs boolean classification",
            ["0", "1"],
            TransformTag.SemanticTypePrecedenceZeroOneBool,
            ExpectedPinned: true),
        new(
            "leading-zeros",
            "Leading-zero numeric strings (e.g. 007)",
            ["007"],
            TransformTag.SemanticFormatLeadingZeros,
            ExpectedPinned: true),
        new(
            "thousands-separator",
            "Thousands-separated numerics (e.g. 1,000)",
            ["1,000"],
            TransformTag.SemanticFormatThousands,
            ExpectedPinned: true),
        new(
            "locale-comma-decimal",
            "Locale comma decimal literals (e.g. 1,5)",
            ["1,5"],
            TransformTag.SemanticFormatLocaleComma,
            ExpectedPinned: true),
        new(
            "signed-zero",
            "Signed-zero literals (+0 / -0)",
            ["+0", "-0"],
            TransformTag.SemanticFormatSignedZero,
            ExpectedPinned: true),
        new(
            "boolean-yes-no",
            "Colloquial yes/no boolean tokens",
            ["yes", "no"],
            TransformTag.SemanticBooleanYesNo,
            ExpectedPinned: true),
        new(
            "boolean-yn",
            "Single-letter Y/N boolean tokens",
            ["Y", "N"],
            TransformTag.SemanticBooleanYn,
            ExpectedPinned: true),
        new(
            "whitespace-only",
            "Whitespace-only cells treated as empty",
            ["   "],
            TransformTag.SemanticEmptyWhitespaceRetained,
            ExpectedPinned: true),
        new(
            "scientific-notation",
            "Scientific notation numerics (control: pinned)",
            ["1e3"],
            TransformTag.SemanticFormatScientific,
            ExpectedPinned: true),
        new(
            "decimal-first-precedence",
            "Integer vs decimal precedence (control: pinned)",
            ["1", "2", "3"],
            TransformTag.SemanticTypePrecedenceDecimalFirst,
            ExpectedPinned: true),
        new(
            "sampling-window-widening",
            "Inference limited to first N cells (control: pinned)",
            ["1", "2", "hello"],
            TransformTag.SemanticSamplingWindow,
            ExpectedPinned: true),
        new(
            "heterogeneous-fallback",
            "Mixed column fallback to partial numeric (control: pinned)",
            ["1", "hello"],
            TransformTag.SemanticHeterogeneousFallback,
            ExpectedPinned: true)
    ];
}
