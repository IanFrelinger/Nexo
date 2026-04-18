namespace Nexo.GameDomain.Playtest;

/// <summary>
/// Defines a playtest scenario: which map, mode, bots, weapons, and duration.
/// The Unity PlaytestSessionRunner reads this to set up and execute a session.
/// </summary>
public sealed record PlaytestScenarioDescriptor
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..12];
    public string Name { get; init; } = string.Empty;
    public string MapId { get; init; } = string.Empty;
    public string GameModeId { get; init; } = string.Empty;
    public int BotCount { get; init; } = 10;
    public int TeamCount { get; init; } = 2;
    public double DurationSeconds { get; init; } = 480; // 8 minutes
    public IReadOnlyList<string> WeaponPool { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> AbilityPool { get; init; } = Array.Empty<string>();
    public string BotDifficulty { get; init; } = "medium"; // easy, medium, hard, mixed
    public bool RecordVideo { get; init; } = true;
    public string OutputDirectory { get; init; } = ".nexo/playtests";
}
