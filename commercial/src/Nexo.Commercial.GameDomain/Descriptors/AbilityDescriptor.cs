namespace Nexo.GameDomain.Descriptors;

/// <summary>
/// Data-only descriptor for a player ability.
/// <para>
/// Abilities represent cooldown-gated actions such as dashes, shields, or area-of-effect
/// attacks. The host ability system reads these descriptors at runtime to wire up
/// input bindings, cooldown timers, and visual feedback.
/// </para>
/// </summary>
public sealed record AbilityDescriptor
{
    /// <summary>Stable identifier used to reference this ability across sessions.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Human-readable display name shown in the ability bar.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Minimum time in seconds between consecutive activations.</summary>
    public double CooldownSeconds { get; init; }

    /// <summary>How long the ability effect persists once activated, in seconds.</summary>
    public double Duration { get; init; }

    /// <summary>Energy resource consumed on activation.</summary>
    public double EnergyCost { get; init; }

    /// <summary>
    /// Broad functional category.
    /// Accepted values: <c>"movement"</c>, <c>"defense"</c>, <c>"offense"</c>, <c>"utility"</c>.
    /// </summary>
    public string EffectType { get; init; } = "utility";

    /// <summary>
    /// Ability-specific numeric parameters interpreted by the effect implementation
    /// (e.g. <c>"speed_multiplier"</c>, <c>"radius"</c>).
    /// </summary>
    public IReadOnlyDictionary<string, double> Parameters { get; init; } =
        new Dictionary<string, double>();

    /// <summary>Free-form tags for filtering, searching, and macro targeting.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];
}
