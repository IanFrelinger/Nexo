using Ashlar.Commercial.GameDomain.Aesthetics;
using Ashlar.Commercial.GameDomain.Scoping;

namespace Ashlar.Commercial.GameDomain.Contracts;

/// <summary>
/// Request to apply a full custom <see cref="AestheticPack"/> (cross-engine bindings) at a scope.
/// </summary>
public sealed record ForgeApplyCustomAestheticPackRequest(
    AestheticPack Pack,
    SettingScope? Scope = null,
    bool? RequireKnownEngineIds = null);
