namespace Nexo.Core.Application.Certification.Models;

/// <summary>
/// A single input→expected-output witness case for brick certification.
/// </summary>
public sealed record WitnessCase(
    IReadOnlyDictionary<string, object> Input,
    IReadOnlyDictionary<string, object> ExpectedOutput);

/// <summary>
/// Witness specification for a brick under certification.
/// </summary>
public sealed record WitnessSpec(
    string BrickId,
    IReadOnlyList<WitnessCase> Cases);
