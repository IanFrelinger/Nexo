namespace Ashlar.Orchestration.Playtest.Models;

/// <summary>
/// Configuration for a playtest session.
/// </summary>
public sealed record PlaytestConfiguration
{
    /// <summary>Number of AI players to spawn.</summary>
    public int PlayerCount { get; init; } = 1;

    /// <summary>Maximum allowed session duration.</summary>
    public TimeSpan MaxDuration { get; init; } = TimeSpan.FromMinutes(30);

    /// <summary>Optional scenario or level to run.</summary>
    public string? Scenario { get; init; }

    /// <summary>Gameplay areas to prioritize during analysis.</summary>
    public IReadOnlyList<string> FocusAreas { get; init; } = Array.Empty<string>();

    /// <summary>
    /// AI player behavior profiles (aggressive, passive, explorer, etc.)
    /// </summary>
    public IReadOnlyList<string> PlayerProfiles { get; init; } = Array.Empty<string>();
}
