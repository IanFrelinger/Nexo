namespace Nexo.GameDomain.Rules.Weapons;

/// <summary>Authoritative weapon trigger modes.</summary>
public enum WeaponFireMode
{
    /// <summary>One shot per trigger press.</summary>
    SemiAutomatic = 0,

    /// <summary>Continuous fire while trigger is held.</summary>
    Automatic = 1,

    /// <summary>Fixed burst after a trigger press.</summary>
    Burst = 2
}
