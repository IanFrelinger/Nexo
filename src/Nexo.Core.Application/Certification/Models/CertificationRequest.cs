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
}
