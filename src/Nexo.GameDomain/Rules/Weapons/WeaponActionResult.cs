namespace Nexo.GameDomain.Rules.Weapons;

/// <summary>Outcome of a weapon action attempt.</summary>
public readonly struct WeaponActionResult
{
    /// <summary>Creates an action result.</summary>
    public WeaponActionResult(
        WeaponActionCode code,
        WeaponShotSample shot = default)
    {
        Code = code;
        Shot = shot;
    }

    /// <summary>Result code.</summary>
    public WeaponActionCode Code { get; }

    /// <summary>Shot sample when <see cref="Code"/> is <see cref="WeaponActionCode.Fired"/>.</summary>
    public WeaponShotSample Shot { get; }

    /// <summary>Whether the action produced a successful state change.</summary>
    public bool Succeeded
        => Code == WeaponActionCode.Fired ||
           Code == WeaponActionCode.ReloadStarted ||
           Code == WeaponActionCode.Equipped;
}
