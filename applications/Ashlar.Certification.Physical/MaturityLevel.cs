namespace Ashlar.Certification.Physical;

/// <summary>
/// Maturity of certification artifacts. Phase 0 outputs are <see cref="Prototype"/> unless promoted.
/// </summary>
public enum MaturityLevel
{
    Prototype = 0,
    Preview = 1,
    Stable = 2
}
