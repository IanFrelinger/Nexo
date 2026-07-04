namespace Nexo.Core.Application.Trust.Models;

/// <summary>
/// Runtime view of the currently active policy pack.
/// </summary>
/// <param name="Id">Active pack identifier.</param>
/// <param name="Version">Active pack version.</param>
/// <param name="ActivatedAtUtc">UTC timestamp when the pack was activated.</param>
public sealed record ActiveTrustPolicyPack(
    string Id,
    string Version,
    DateTimeOffset ActivatedAtUtc);
