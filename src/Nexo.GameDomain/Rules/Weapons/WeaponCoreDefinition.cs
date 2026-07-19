using System;

namespace Nexo.GameDomain.Rules.Weapons;

/// <summary>Immutable deterministic weapon mechanics definition.</summary>
public sealed class WeaponCoreDefinition
{
    /// <summary>Creates a weapon definition.</summary>
    public WeaponCoreDefinition(
        string id,
        WeaponFireMode fireMode,
        int baseDamage,
        uint fireIntervalTicks,
        int magazineCapacity,
        int startingReserveAmmo,
        uint reloadDurationTicks,
        int burstCount = 3,
        uint burstIntervalTicks = 2,
        int effectiveRangeMillimeters = 50_000,
        int maximumRangeMillimeters = 200_000,
        int minimumDamageBasisPoints = 5_000,
        int headMultiplierBasisPoints = 20_000,
        int armMultiplierBasisPoints = 8_000,
        int legMultiplierBasisPoints = 7_500,
        int hipSpreadMilliDegrees = 1_500,
        int adsSpreadMilliDegrees = 200,
        int verticalRecoilMilliDegrees = 800,
        int horizontalRecoilMilliDegrees = 300)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Weapon id is required.", nameof(id));
        if (baseDamage < 0)
            throw new ArgumentOutOfRangeException(nameof(baseDamage));
        if (fireIntervalTicks == 0)
            throw new ArgumentOutOfRangeException(nameof(fireIntervalTicks));
        if (magazineCapacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(magazineCapacity));
        if (startingReserveAmmo < 0)
            throw new ArgumentOutOfRangeException(nameof(startingReserveAmmo));
        if (reloadDurationTicks == 0)
            throw new ArgumentOutOfRangeException(nameof(reloadDurationTicks));
        if (burstCount <= 0 || burstIntervalTicks == 0)
            throw new ArgumentOutOfRangeException(nameof(burstCount));
        if (effectiveRangeMillimeters < 0 ||
            maximumRangeMillimeters <= effectiveRangeMillimeters)
            throw new ArgumentOutOfRangeException(nameof(maximumRangeMillimeters));
        if (minimumDamageBasisPoints < 0 || minimumDamageBasisPoints > 10_000)
            throw new ArgumentOutOfRangeException(nameof(minimumDamageBasisPoints));

        Id = id;
        FireMode = fireMode;
        BaseDamage = baseDamage;
        FireIntervalTicks = fireIntervalTicks;
        MagazineCapacity = magazineCapacity;
        StartingReserveAmmo = startingReserveAmmo;
        ReloadDurationTicks = reloadDurationTicks;
        BurstCount = burstCount;
        BurstIntervalTicks = burstIntervalTicks;
        EffectiveRangeMillimeters = effectiveRangeMillimeters;
        MaximumRangeMillimeters = maximumRangeMillimeters;
        MinimumDamageBasisPoints = minimumDamageBasisPoints;
        HeadMultiplierBasisPoints = headMultiplierBasisPoints;
        ArmMultiplierBasisPoints = armMultiplierBasisPoints;
        LegMultiplierBasisPoints = legMultiplierBasisPoints;
        HipSpreadMilliDegrees = Math.Max(0, hipSpreadMilliDegrees);
        AdsSpreadMilliDegrees = Math.Max(0, adsSpreadMilliDegrees);
        VerticalRecoilMilliDegrees = Math.Max(0, verticalRecoilMilliDegrees);
        HorizontalRecoilMilliDegrees = Math.Max(0, horizontalRecoilMilliDegrees);
    }

    /// <summary>Stable weapon id.</summary>
    public string Id { get; }

    /// <summary>Trigger mode.</summary>
    public WeaponFireMode FireMode { get; }

    /// <summary>Base damage before falloff/region.</summary>
    public int BaseDamage { get; }

    /// <summary>Ticks between shots (non-burst).</summary>
    public uint FireIntervalTicks { get; }

    /// <summary>Magazine size.</summary>
    public int MagazineCapacity { get; }

    /// <summary>Reserve ammo granted on equip.</summary>
    public int StartingReserveAmmo { get; }

    /// <summary>Reload duration in ticks.</summary>
    public uint ReloadDurationTicks { get; }

    /// <summary>Burst length.</summary>
    public int BurstCount { get; }

    /// <summary>Ticks between burst shots.</summary>
    public uint BurstIntervalTicks { get; }

    /// <summary>Range before damage falloff starts (mm).</summary>
    public int EffectiveRangeMillimeters { get; }

    /// <summary>Maximum range (mm).</summary>
    public int MaximumRangeMillimeters { get; }

    /// <summary>Minimum damage after falloff, in basis points of base.</summary>
    public int MinimumDamageBasisPoints { get; }

    /// <summary>Head multiplier in basis points.</summary>
    public int HeadMultiplierBasisPoints { get; }

    /// <summary>Arm multiplier in basis points.</summary>
    public int ArmMultiplierBasisPoints { get; }

    /// <summary>Leg multiplier in basis points.</summary>
    public int LegMultiplierBasisPoints { get; }

    /// <summary>Hip-fire spread magnitude (millidegrees).</summary>
    public int HipSpreadMilliDegrees { get; }

    /// <summary>ADS spread magnitude (millidegrees).</summary>
    public int AdsSpreadMilliDegrees { get; }

    /// <summary>Vertical recoil magnitude (millidegrees).</summary>
    public int VerticalRecoilMilliDegrees { get; }

    /// <summary>Horizontal recoil magnitude (millidegrees).</summary>
    public int HorizontalRecoilMilliDegrees { get; }
}
