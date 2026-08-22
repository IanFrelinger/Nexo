using Ashlar.Core.Application.Adaptation.Models;

namespace Ashlar.Core.Application.Certification.Models;

/// <summary>
/// A certified brick available for wiring. Carries atom-cert reference, not witness values.
/// </summary>
/// <param name="BrickId">Certified brick identifier.</param>
/// <param name="Inputs">Input port names and types from the brick interface.</param>
/// <param name="Outputs">Output port names and types from the brick interface.</param>
/// <param name="AtomCertificationReference">Reference to the atom-level certification record.</param>
public sealed record CertifiedBrickCatalogEntry(
    string BrickId,
    IReadOnlyList<WitnessIoField> Inputs,
    IReadOnlyList<WitnessIoField> Outputs,
    string AtomCertificationReference);
