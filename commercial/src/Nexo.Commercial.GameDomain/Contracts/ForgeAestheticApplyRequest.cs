using Nexo.Commercial.GameDomain.Aesthetics;
using Nexo.Commercial.GameDomain.Scoping;

namespace Nexo.Commercial.GameDomain.Contracts;

/// <summary>
/// Request to apply an aesthetic pack at a given scope.
/// </summary>
/// <param name="AestheticId">Identifier of the aesthetic pack to apply.</param>
/// <param name="Scope">Scope at which the aesthetic should take effect.</param>
/// <param name="MapRenderingProfile">Optional override for geographic map pipeline; see <see cref="Aesthetics.MapRenderingProfiles"/>.</param>
public sealed record ForgeAestheticApplyRequest(
    string AestheticId,
    SettingScope Scope,
    string? MapRenderingProfile = null);
