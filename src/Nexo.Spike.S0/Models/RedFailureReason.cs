namespace Nexo.Spike.S0.Models;

/// <summary>
/// Classification of why tests failed during RED validation.
/// </summary>
public enum RedFailureReason
{
    /// <summary>Tests pass against the trivial stub — tautological suite.</summary>
    PassedAgainstStub,

    /// <summary>Compile error, missing symbol, or NotImplementedException.</summary>
    Unimplemented,

    /// <summary>Tests fail because assertions disagree with the present-but-wrong stub.</summary>
    AssertionFailure,

    /// <summary>Build or test runner failed for an unexpected reason.</summary>
    Unknown
}
