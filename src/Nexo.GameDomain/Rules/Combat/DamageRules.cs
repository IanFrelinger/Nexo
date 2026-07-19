using Nexo.GameDomain.Contracts;

namespace Nexo.GameDomain.Rules.Combat;

/// <summary>An authoritative request to resolve one damage application.</summary>
public readonly struct DamageCommand
{
    /// <summary>Creates a damage command.</summary>
    public DamageCommand(
        EntityId source,
        EntityId target,
        int baseDamage,
        int criticalMultiplierPercent,
        bool isCritical)
    {
        Source = source;
        Target = target;
        BaseDamage = baseDamage;
        CriticalMultiplierPercent = criticalMultiplierPercent;
        IsCritical = isCritical;
    }

    /// <summary>Entity that caused the damage.</summary>
    public EntityId Source { get; }

    /// <summary>Entity receiving the damage.</summary>
    public EntityId Target { get; }

    /// <summary>Damage before critical and armor rules.</summary>
    public int BaseDamage { get; }

    /// <summary>Critical multiplier represented as a percentage.</summary>
    public int CriticalMultiplierPercent { get; }

    /// <summary>Whether the critical multiplier applies.</summary>
    public bool IsCritical { get; }
}

/// <summary>Target state needed by the pure damage rule.</summary>
public readonly struct DamageContext
{
    /// <summary>Creates target damage context.</summary>
    public DamageContext(int currentHealth, int armor)
    {
        CurrentHealth = currentHealth;
        Armor = armor;
    }

    /// <summary>Health before the hit.</summary>
    public int CurrentHealth { get; }

    /// <summary>Flat armor subtracted after critical scaling.</summary>
    public int Armor { get; }
}

/// <summary>Pure result of resolving a damage command.</summary>
public readonly struct DamageResult
{
    /// <summary>Creates a damage result.</summary>
    public DamageResult(int rawDamage, int appliedDamage, int remainingHealth)
    {
        RawDamage = rawDamage;
        AppliedDamage = appliedDamage;
        RemainingHealth = remainingHealth;
    }

    /// <summary>Damage after critical scaling but before armor.</summary>
    public int RawDamage { get; }

    /// <summary>Damage actually removed from health.</summary>
    public int AppliedDamage { get; }

    /// <summary>Health after applying damage.</summary>
    public int RemainingHealth { get; }

    /// <summary>Whether this result eliminates the target.</summary>
    public bool Eliminated => RemainingHealth == 0;
}

/// <summary>Allocation-free deterministic combat damage rules.</summary>
public static class DamageRules
{
    /// <summary>Resolves damage using integer-only arithmetic.</summary>
    public static DamageResult Resolve(in DamageCommand command, in DamageContext context)
    {
        var baseDamage = Math.Max(0, command.BaseDamage);
        var multiplier = Math.Max(0, command.CriticalMultiplierPercent);
        var health = Math.Max(0, context.CurrentHealth);
        var armor = Math.Max(0, context.Armor);

        long scaled = command.IsCritical
            ? (long)baseDamage * multiplier / 100
            : baseDamage;

        var rawDamage = (int)Math.Min(int.MaxValue, scaled);
        var postArmor = Math.Max(0, rawDamage - armor);
        var appliedDamage = Math.Min(health, postArmor);
        return new DamageResult(rawDamage, appliedDamage, health - appliedDamage);
    }
}
