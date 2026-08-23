namespace Ashlar.Core.Application.Adaptation.Models;

/// <summary>
/// Human- or intent-provided specification for brick generation. Does not include witness cases.
/// </summary>
/// <param name="IntentId">Stable intent identifier.</param>
/// <param name="Description">Natural-language description of desired brick behavior.</param>
/// <param name="BrickId">Optional target brick identifier.</param>
/// <param name="Name">Optional display name for the generated brick.</param>
public sealed record IntentSpec(
    string IntentId,
    string Description,
    string? BrickId = null,
    string? Name = null);
