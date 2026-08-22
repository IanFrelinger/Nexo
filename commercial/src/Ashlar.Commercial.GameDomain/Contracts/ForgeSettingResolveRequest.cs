using Ashlar.Commercial.GameDomain.Aesthetics;
using Ashlar.Commercial.GameDomain.Scoping;

namespace Ashlar.Commercial.GameDomain.Contracts;

/// <summary>
/// Request to resolve all effective settings for a given context.
/// </summary>
/// <param name="PlayerId">Optional player identifier.</param>
/// <param name="TeamId">Optional team identifier.</param>
/// <param name="ZoneId">Optional zone identifier.</param>
/// <param name="ObjectId">Optional object identifier.</param>
/// <param name="ActiveMoment">Optional active moment identifier.</param>
public sealed record ForgeSettingResolveRequest(
    string? PlayerId,
    string? TeamId,
    string? ZoneId,
    string? ObjectId,
    string? ActiveMoment);
