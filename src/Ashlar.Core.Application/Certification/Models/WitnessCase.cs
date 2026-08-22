namespace Ashlar.Core.Application.Certification.Models;

/// <summary>
/// A single input→expected-output witness case for brick certification.
/// </summary>
/// <param name="Input">Named input values for the witness case.</param>
/// <param name="ExpectedOutput">Named expected output values.</param>
public sealed record WitnessCase(
    IReadOnlyDictionary<string, object> Input,
    IReadOnlyDictionary<string, object> ExpectedOutput);
