using Nexo.Core.Application.Adaptation.Models;

namespace Nexo.Core.Application.Certification.Models;

/// <summary>
/// Target composition I/O contract — names and types only, no witness cases.
/// </summary>
public sealed record CompositionTargetSignature(
    string CompositionId,
    IReadOnlyList<CompositionPort> Inputs,
    IReadOnlyList<CompositionPort> Outputs);

/// <summary>
/// A certified brick available for wiring. Carries atom-cert reference, not witness values.
/// </summary>
public sealed record CertifiedBrickCatalogEntry(
    string BrickId,
    IReadOnlyList<WitnessIoField> Inputs,
    IReadOnlyList<WitnessIoField> Outputs,
    string AtomCertificationReference);

/// <summary>
/// Untrusted proposer input: target signature + certified-brick catalog only.
/// Structurally cannot carry witness cases (no field of witness-bearing types).
/// </summary>
public sealed record CompositionProposerInput(
    CompositionTargetSignature Target,
    IReadOnlyList<CertifiedBrickCatalogEntry> CertifiedBricks);

/// <summary>
/// Output of an untrusted composition proposer — wiring only, witness remains external.
/// </summary>
public sealed record ProposedComposition(
    CompositionSpec Spec,
    string Provenance);
