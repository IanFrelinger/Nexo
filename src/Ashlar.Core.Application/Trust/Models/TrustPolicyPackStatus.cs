namespace Ashlar.Core.Application.Trust.Models;

/// <summary>
/// Tracks the currently active trust policy pack.
/// </summary>
/// <param name="ActivePackId">Identifier of the active pack, if any.</param>
/// <param name="ActivePackVersion">Version of the active pack, if any.</param>
/// <param name="ActivatedAtUtc">UTC timestamp when the active pack was applied.</param>
public sealed record TrustPolicyPackStatus(
    string? ActivePackId = null,
    string? ActivePackVersion = null,
    DateTimeOffset? ActivatedAtUtc = null);
