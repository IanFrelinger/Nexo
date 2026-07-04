using Nexo.Core.Application.Adaptation.Models;

namespace Nexo.Core.Application.Certification.Models;

/// <summary>
/// Target composition I/O contract — names and types only, no witness cases.
/// </summary>
/// <param name="CompositionId">Desired composition identifier.</param>
/// <param name="Inputs">Required composition-level input ports.</param>
/// <param name="Outputs">Required composition-level output ports.</param>
public sealed record CompositionTargetSignature(
    string CompositionId,
    IReadOnlyList<CompositionPort> Inputs,
    IReadOnlyList<CompositionPort> Outputs);
