namespace Nexo.Contracts;

/// <summary>Runtime health and agent fleet status snapshot.</summary>
/// <param name="Mode">Current host mode (e.g. dev, server, air-gapped).</param>
/// <param name="Message">Human-readable status line.</param>
/// <param name="TotalAgents">Total registered agents.</param>
/// <param name="ActiveAgents">Agents currently active or executing.</param>
/// <param name="ActivePackId">Active policy pack id, when trust is enabled.</param>
/// <param name="ActivePackVersion">Active policy pack version.</param>
public sealed record StatusResponse(
    string Mode,
    string Message,
    int TotalAgents,
    int ActiveAgents,
    string? ActivePackId = null,
    string? ActivePackVersion = null);
