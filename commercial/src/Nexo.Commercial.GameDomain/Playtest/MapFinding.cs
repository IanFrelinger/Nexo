namespace Nexo.Commercial.GameDomain.Playtest;

/// <summary>Map finding record.</summary>
public sealed record MapFinding
{
    /// <summary>Area name value.</summary>
    public string AreaName { get; init; } = string.Empty;
    /// <summary>Severity value.</summary>
    public string Severity { get; init; } = "ok"; // ok, warning, critical
    /// <summary>Death percentage value.</summary>
    public double DeathPercentage { get; init; }
    /// <summary>Finding type value.</summary>
    public string FindingType { get; init; } = "choke_point"; // choke_point, dead_zone, spawn_camp, sightline_issue
    /// <summary>Summary value.</summary>
    public string Summary { get; init; } = string.Empty;
}
