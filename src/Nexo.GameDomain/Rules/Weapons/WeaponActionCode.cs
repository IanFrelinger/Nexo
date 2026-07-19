namespace Nexo.GameDomain.Rules.Weapons;

/// <summary>Result code for a weapon action attempt.</summary>
public enum WeaponActionCode
{
    Fired = 0,
    ReloadStarted = 1,
    Equipped = 2,
    TriggerNotActive = 3,
    Cooldown = 4,
    Reloading = 5,
    EmptyMagazine = 6,
    MagazineFull = 7,
    NoReserveAmmo = 8,
    Disabled = 9
}
