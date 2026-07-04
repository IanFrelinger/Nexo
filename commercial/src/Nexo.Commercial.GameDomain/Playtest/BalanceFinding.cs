namespace Nexo.Commercial.GameDomain.Playtest;

/// <summary>Balance finding record.</summary>
public sealed record BalanceFinding
{
    /// <summary>Item name value.</summary>
    public string ItemName { get; init; } = string.Empty;
    /// <summary>Item type value.</summary>
    public string ItemType { get; init; } = "weapon"; // weapon, ability, game_rule
    /// <summary>Severity value.</summary>
    public string Severity { get; init; } = "ok"; // ok, warning, critical
    /// <summary>Kill rate multiplier value.</summary>
    public double KillRateMultiplier { get; init; } = 1.0;
    /// <summary>Threshold value.</summary>
    public double Threshold { get; init; } = 2.0;
    /// <summary>Summary value.</summary>
    public string Summary { get; init; } = string.Empty;
}
