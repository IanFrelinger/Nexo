namespace Nexo.GameDomain.Descriptors;

/// <summary>
/// Data-only descriptor for a weapon archetype consumed by the host combat system.
/// <para>
/// Instances are purely declarative — they carry no behaviour and are safe to
/// serialise across the Nexo ↔ host boundary. The host weapon controller reads these
/// values at spawn time to configure fire rate, projectile type, reload timing, and VFX hooks.
/// </para>
/// </summary>
public sealed record WeaponDescriptor
{
    /// <summary>Stable identifier used to reference this weapon across sessions.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Human-readable display name shown in the HUD and loadout menus.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Base damage inflicted per single hit (before modifiers).</summary>
    public double DamagePerHit { get; init; }

    /// <summary>Rounds discharged per second at maximum fire rate.</summary>
    public double FireRatePerSecond { get; init; }

    /// <summary>Effective range in world units beyond which damage falls off.</summary>
    public double Range { get; init; }

    /// <summary>Number of rounds in a full magazine before a reload is required.</summary>
    public int MagazineSize { get; init; }

    /// <summary>Time in seconds to complete a full reload cycle.</summary>
    public double ReloadTimeSeconds { get; init; }

    /// <summary>
    /// Projectile delivery mechanism.
    /// Accepted values: <c>"hitscan"</c>, <c>"projectile"</c>, <c>"bouncing"</c>, <c>"beam"</c>.
    /// </summary>
    public string ProjectileType { get; init; } = "hitscan";

    /// <summary>
    /// Optional special effects triggered on hit or fire (e.g. <c>"freeze"</c>,
    /// <c>"burn"</c>, <c>"chain_lightning"</c>).
    /// </summary>
    public IReadOnlyList<string> SpecialEffects { get; init; } = [];

    /// <summary>Free-form tags for filtering, searching, and macro targeting.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];
}
