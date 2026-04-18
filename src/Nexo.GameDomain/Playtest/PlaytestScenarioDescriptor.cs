namespace Nexo.GameDomain.Playtest;

/// <summary>
/// Describes a playtest scenario configuration.
/// Stub: will be replaced by Phase 1 implementation.
/// </summary>
public sealed record PlaytestScenarioDescriptor
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..12];
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string MapId { get; init; } = string.Empty;
    public string GameModeId { get; init; } = string.Empty;
    public int BotCount { get; init; } = 10;
    public int TeamCount { get; init; } = 2;
    public double DurationSeconds { get; init; } = 480;
    public IReadOnlyList<string> WeaponPool { get; init; } = [];
    public IReadOnlyList<string> AbilityPool { get; init; } = [];
    public string BotDifficulty { get; init; } = "medium";
    public bool RecordVideo { get; init; } = true;
    public string OutputDirectory { get; init; } = ".nexo/playtests";
}
