namespace Nexo.Commercial.GameDomain.Playtest;
/// <summary>
/// Defines a playtest scenario: which map, mode, bots, weapons, and duration.
/// The Unity PlaytestSessionRunner reads this to set up and execute a session.
/// </summary>
public sealed record PlaytestScenarioDescriptor
{
    /// <summary>id value.</summary>
    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..12];
    /// <summary>Name value.</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>map id value.</summary>
    public string MapId { get; init; } = string.Empty;
    /// <summary>game mode id value.</summary>
    public string GameModeId { get; init; } = string.Empty;
    /// <summary>Bot count value.</summary>
    public int BotCount { get; init; } = 10;
    /// <summary>Team count value.</summary>
    public int TeamCount { get; init; } = 2;
    /// <summary>Duration seconds value.</summary>
    public double DurationSeconds { get; init; } = 480; // 8 minutes
    /// <summary>Weapon pool value.</summary>
    public IReadOnlyList<string> WeaponPool { get; init; } = Array.Empty<string>();
    /// <summary>Ability pool value.</summary>
    public IReadOnlyList<string> AbilityPool { get; init; } = Array.Empty<string>();
    /// <summary>Bot difficulty value.</summary>
    public string BotDifficulty { get; init; } = "medium"; // easy, medium, hard, mixed
    /// <summary>Record video value.</summary>
    public bool RecordVideo { get; init; } = true;
    /// <summary>Output directory value.</summary>
    public string OutputDirectory { get; init; } = ".nexo/playtests";
}
