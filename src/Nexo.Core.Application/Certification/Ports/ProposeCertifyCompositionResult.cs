using Nexo.Core.Application.Certification.Models;

namespace Nexo.Core.Application.Certification.Ports;

/// <summary>Combined result of proposing and certifying a composition.</summary>
/// <param name="Proposal">Model-generated composition proposal.</param>
/// <param name="Decision">Certification gate decision for the proposal.</param>
public sealed record ProposeCertifyCompositionResult(
    ProposedComposition Proposal,
    CompositionCertificationDecision Decision);
