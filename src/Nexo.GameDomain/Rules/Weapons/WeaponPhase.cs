namespace Nexo.GameDomain.Rules.Weapons;

/// <summary>Tick-driven weapon readiness phase.</summary>
public enum WeaponPhase
{
    Holstered = 0,
    Equipping = 1,
    Ready = 2,
    Cooldown = 3,
    Reloading = 4,
    Disabled = 5
}
