using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Certification.Models;

namespace Nexo.Core.Application.Adaptation;

/// <summary>
/// Derives a witness I/O signature without exposing expected output values to the generator.
/// </summary>
public static class WitnessSignatureBuilder
{
    /// <summary>
    /// Derives input and output field names and types from the first witness case.
    /// </summary>
    /// <param name="witness">Witness specification with at least one case.</param>
    /// <returns>I/O signature without expected output values.</returns>
    /// <exception cref="InvalidOperationException">When the witness contains no cases.</exception>
    public static WitnessSignature FromWitness(WitnessSpec witness)
    {
        if (witness.Cases.Count == 0)
            throw new InvalidOperationException("Witness must contain at least one case to derive a signature.");

        var probe = witness.Cases[0];
        var inputs = probe.Input
            .Select(kvp => new WitnessIoField(kvp.Key, InferType(kvp.Value)))
            .ToList();
        var outputs = probe.ExpectedOutput
            .Select(kvp => new WitnessIoField(kvp.Key, InferType(kvp.Value)))
            .ToList();

        return new WitnessSignature(witness.BrickId, inputs, outputs);
    }

    private static string InferType(object value) => value switch
    {
        int or long or short or byte => "int",
        bool => "bool",
        string => "string",
        double or float or decimal => "double",
        _ => "object"
    };
}
