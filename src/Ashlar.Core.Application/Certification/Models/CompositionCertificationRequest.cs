namespace Ashlar.Core.Application.Certification.Models;

/// <summary>Input to certify a proposed multi-brick composition.</summary>
public sealed record CompositionCertificationRequest
{
    /// <summary>Composition graph specification under certification.</summary>
    public required CompositionSpec Spec { get; init; }

    /// <summary>Witness cases used to validate composition behavior.</summary>
    public required CompositionWitnessSpec Witness { get; init; }

    /// <summary>Optional wiring metadata captured from the composer or generator.</summary>
    public string? WiringMetadata { get; init; }
}
