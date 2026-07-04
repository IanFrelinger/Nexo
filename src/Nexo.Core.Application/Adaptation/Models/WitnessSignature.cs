namespace Nexo.Core.Application.Adaptation.Models;

/// <summary>
/// I/O contract derived from an independent witness (field names and types only — no expected values).
/// </summary>
/// <param name="BrickId">Brick identifier for this signature.</param>
/// <param name="Inputs">Input field names and types.</param>
/// <param name="Outputs">Output field names and types.</param>
public sealed record WitnessSignature(
    string BrickId,
    IReadOnlyList<WitnessIoField> Inputs,
    IReadOnlyList<WitnessIoField> Outputs);
