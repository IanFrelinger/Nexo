using Nexo.Certification.Contracts;
using Nexo.Core.Domain.Bricks;

namespace Nexo.Core.Application.Certification.Models;

/// <summary>
/// Input to the brick certification gate.
/// </summary>
public sealed record CertificationRequest
{
    /// <summary>Brick definition under certification.</summary>
    public required DomainBrick Brick { get; init; }

    /// <summary>Witness cases for behavioral verification.</summary>
    public required WitnessSpec Witness { get; init; }

    /// <summary>Brick source code for compilation and mutation testing.</summary>
    public required string SourceCode { get; init; }

    /// <summary>Project path used for compilation context.</summary>
    public required string ProjectPath { get; init; }

    /// <summary>Additional assembly references required for compilation.</summary>
    public IReadOnlyList<string> CompilationReferences { get; init; } = Array.Empty<string>();

    /// <summary>Optional fully-qualified brick type name override.</summary>
    public string? BrickTypeName { get; init; }

    /// <summary>
    /// Extra hash-identified inputs to record on the certificate beyond the witness
    /// (trust-loop spec §2.1 <c>inputs</c>) — e.g. the assembled proposal context via
    /// <c>ProposalContext.ToCertificationInput</c>. Evidence, not a gate: entries are
    /// recorded under the v2 signature, in authored order after the witness input.
    /// </summary>
    public IReadOnlyList<CertificationInput> AdditionalInputs { get; init; } = Array.Empty<CertificationInput>();
}
