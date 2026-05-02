namespace Nexo.GameDomain.Descriptors;

/// <summary>
/// Data-only descriptor for an AI combatant behaviour profile.
/// <para>
/// The Unity AI controller reads these descriptors to configure perception, decision-making,
/// and engagement parameters for non-player characters during prototype playtesting.
/// </para>
/// </summary>
public sealed record AiBehaviorDescriptor
{
    /// <summary>Stable identifier used to reference this behaviour profile across sessions.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Human-readable display name shown in the bot configuration panel.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// High-level combat archetype that drives the behaviour tree selection.
    /// Accepted values: <c>"patrol"</c>, <c>"aggressive"</c>, <c>"defensive"</c>,
    /// <c>"sniper"</c>, <c>"support"</c>.
    /// </summary>
    public string Archetype { get; init; } = "patrol";

    /// <summary>
    /// Willingness to engage targets, normalised to <c>[0, 1]</c>.
    /// <c>0</c> = purely evasive; <c>1</c> = relentless pursuit.
    /// </summary>
    public double Aggression { get; init; } = 0.5;

    /// <summary>
    /// Aim precision factor, normalised to <c>[0, 1]</c>.
    /// <c>0</c> = maximum spread; <c>1</c> = perfect accuracy.
    /// </summary>
    public double Accuracy { get; init; } = 0.5;

    /// <summary>Time in seconds the AI takes to react to a new threat stimulus.</summary>
    public double ReactionTimeSeconds { get; init; } = 0.3;

    /// <summary>
    /// Ideal engagement distance in world units. The AI will attempt to
    /// maintain this distance from its current target.
    /// </summary>
    public double PreferredRange { get; init; } = 15.0;

    /// <summary>Free-form tags for filtering, searching, and macro targeting.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];
}
