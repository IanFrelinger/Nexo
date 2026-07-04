namespace Nexo.Core.Application.Certification.Models;

/// <summary>
/// Witness specification for a brick under certification.
/// </summary>
/// <param name="BrickId">Brick identifier under certification.</param>
/// <param name="Cases">Input→expected-output witness cases.</param>
public sealed record WitnessSpec(
    string BrickId,
    IReadOnlyList<WitnessCase> Cases);
