using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Domain.Bricks;

namespace Ashlar.Infrastructure.Adaptation.Generation;

/// <summary>Compiled output from building a generated certification brick.</summary>
/// <param name="Brick">Loaded domain brick instance.</param>
/// <param name="BrickTypeName">Fully qualified generated brick type name.</param>
/// <param name="SourceCode">Generated brick source code.</param>
/// <param name="ProjectPath">Path to the generated brick project.</param>
/// <param name="CompilationReferences">Assembly references used during compilation.</param>
/// <param name="EmittedArtifact">Closed-world PE the gate will inspect, activate, and bind.</param>
public sealed record BuiltGeneratedBrick(
    DomainBrick Brick,
    string BrickTypeName,
    string SourceCode,
    string ProjectPath,
    IReadOnlyList<string> CompilationReferences,
    GateEmittedArtifact EmittedArtifact);
