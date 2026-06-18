namespace Nexo.Spike.S1.Adversary;

public enum TransformFamily
{
    HonestBaseline,
    WrongImpl,
    WeakTest
}

/// <summary>Catalog tags for attributable escape breakdown.</summary>
public enum TransformTag
{
    HonestNoOp,
    OffByOne,
    BoundaryInclusive,
    NegatedCondition,
    DroppedBranch,
    ConstantReturn,
    SwappedOperands,
    SemanticTypePrecedenceDecimalFirst,
    SemanticTypePrecedenceZeroOneBool,
    SemanticEmptyWhitespaceRetained,
    SemanticFormatLeadingZeros,
    SemanticFormatThousands,
    SemanticFormatScientific,
    SemanticFormatLocaleComma,
    SemanticFormatSignedZero,
    SemanticSamplingWindow,
    SemanticBooleanYesNo,
    SemanticBooleanYn,
    SemanticHeterogeneousFallback,
    AssertionRemoved,
    TautologyReplacement,
    OverNarrowDomain,
    TypeOnlyAssert
}

public sealed record AdversarialCandidate(
    TransformTag Tag,
    TransformFamily Family,
    int Seed,
    string ImplementationSource,
    string TestSource);

public interface IAdversarialGenerator
{
    IReadOnlyList<AdversarialCandidate> GenerateWrongImplCandidates(int seeds);

    IReadOnlyList<AdversarialCandidate> GenerateWeakTestCandidates(int seeds);

    AdversarialCandidate GenerateHonestBaseline(int seed);
}
