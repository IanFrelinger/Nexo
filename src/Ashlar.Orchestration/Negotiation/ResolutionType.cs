using System.Text.Json;
using Ashlar.Orchestration.Negotiation.Models;

namespace Ashlar.Orchestration.Negotiation;

/// <summary>
/// Type of resolution achieved.
/// 
/// Defines resolution types:
/// - Automatic: Resolved automatically without agent input
/// - Negotiated: Resolved through agent negotiation
/// - Synthesized: Resolved through creative synthesis
/// - Escalated: Could not be resolved - escalated to human
/// 
/// Used by NegotiationResult to indicate how a conflict was resolved.
/// </summary>
public enum ResolutionType
{
    /// <summary>Resolved automatically without agent input.</summary>
    Automatic,

    /// <summary>Resolved through agent negotiation.</summary>
    Negotiated,

    /// <summary>Resolved through creative synthesis.</summary>
    Synthesized,

    /// <summary>Could not be resolved - escalated to human.</summary>
    Escalated
}
