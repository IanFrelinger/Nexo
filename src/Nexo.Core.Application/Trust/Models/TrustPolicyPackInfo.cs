namespace Nexo.Core.Application.Trust.Models;

/// <summary>
/// Lightweight list item for CLI/API status surfaces.
/// </summary>
/// <param name="Id">Pack identifier.</param>
/// <param name="Version">Pack version.</param>
/// <param name="DisplayName">Human-readable name.</param>
/// <param name="Description">Short description of the pack.</param>
/// <param name="RecommendedFor">Optional audience hint.</param>
public sealed record TrustPolicyPackInfo(
    string Id,
    string Version,
    string DisplayName,
    string Description,
    string? RecommendedFor = null);
