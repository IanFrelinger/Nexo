using Ashlar.Core.Application.Adaptation.Models;

namespace Ashlar.Core.Application.Certification.Models;

/// <summary>
/// Output of an untrusted composition proposer — wiring only, witness remains external.
/// </summary>
/// <param name="Spec">Proposed composition graph and port wiring.</param>
/// <param name="Provenance">Source identifier for the proposer output (model, heuristic, etc.).</param>
public sealed record ProposedComposition(
    CompositionSpec Spec,
    string Provenance);
