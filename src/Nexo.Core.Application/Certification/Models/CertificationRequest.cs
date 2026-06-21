using Nexo.Core.Domain.Bricks;

namespace Nexo.Core.Application.Certification.Models;

/// <summary>
/// Input to the brick certification gate.
/// </summary>
public sealed record CertificationRequest
{
    public required Brick Brick { get; init; }
    public required WitnessSpec Witness { get; init; }
    public required string SourceCode { get; init; }
    public required string ProjectPath { get; init; }
    public IReadOnlyList<string> CompilationReferences { get; init; } = Array.Empty<string>();
    public string? BrickTypeName { get; init; }
}
