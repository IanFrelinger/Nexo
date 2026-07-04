using Nexo.Core.Application.Adaptation.Models;

namespace Nexo.Core.Application.Certification.Models;

/// <summary>
/// Untrusted proposer input: target signature + certified-brick catalog only.
/// Structurally cannot carry witness cases (no field of witness-bearing types).
/// </summary>
/// <param name="Target">Desired composition I/O contract.</param>
/// <param name="CertifiedBricks">Catalog of admitted bricks available for wiring.</param>
public sealed record CompositionProposerInput(
    CompositionTargetSignature Target,
    IReadOnlyList<CertifiedBrickCatalogEntry> CertifiedBricks);
