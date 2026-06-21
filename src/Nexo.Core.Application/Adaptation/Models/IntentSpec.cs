namespace Nexo.Core.Application.Adaptation.Models;

/// <summary>
/// Human- or intent-provided specification for brick generation. Does not include witness cases.
/// </summary>
public sealed record IntentSpec(
    string IntentId,
    string Description,
    string? BrickId = null,
    string? Name = null);

/// <summary>
/// I/O contract derived from an independent witness (field names and types only — no expected values).
/// </summary>
public sealed record WitnessSignature(
    string BrickId,
    IReadOnlyList<WitnessIoField> Inputs,
    IReadOnlyList<WitnessIoField> Outputs);

public sealed record WitnessIoField(string Name, string Type);

/// <summary>
/// Untrusted model output with provenance marking.
/// </summary>
public sealed record GeneratedBrickSource(
    string SourceCode,
    string Provenance,
    string ClassName,
    string Namespace);
