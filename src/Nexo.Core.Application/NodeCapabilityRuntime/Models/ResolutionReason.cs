namespace Nexo.Core.Application.NodeCapabilityRuntime.Models;

/// <summary>
/// Explains why a particular model and inference target were selected.
/// </summary>
public enum ResolutionReason
{
    /// <summary>Best local model match for the task constraints.</summary>
    BestLocalFit,

    /// <summary>Escalated because local memory is insufficient for any suitable model.</summary>
    EscalatedInsufficientMemory,

    /// <summary>Escalated because task quality requirements exceed local model capability.</summary>
    EscalatedQualityRequirement,

    /// <summary>Escalated because platform policy blocks local inference.</summary>
    EscalatedPolicyBlocked,

    /// <summary>Local inference forced by privacy or explicit routing policy.</summary>
    ForcedLocal
}
