namespace Nexo.Orchestration.Playtest.Models;

/// <summary>
/// A telemetry event from game execution.
/// 
/// Represents a single event captured during playtesting:
/// - Event identification and type
/// - Session and player information
/// - Timestamp and event data
/// 
/// Used by playtest agents to record gameplay events for analysis.
/// </summary>
public sealed record TelemetryEvent
{
    public required string EventId { get; init; }
    public required string SessionId { get; init; }
    public required string EventType { get; init; }
    public required DateTimeOffset Timestamp { get; init; }

    public string? PlayerId { get; init; }
    public IReadOnlyDictionary<string, object> Data { get; init; } =
        new Dictionary<string, object>();
}

/// <summary>
/// Standard telemetry event types.
/// </summary>
public static class TelemetryEventTypes
{
    // Combat events
    public const string PlayerDamaged = "player.damaged";
    public const string PlayerKilled = "player.killed";
    public const string WeaponFired = "weapon.fired";
    public const string AbilityUsed = "ability.used";

    // Economy events
    public const string CurrencyEarned = "economy.earned";
    public const string CurrencySpent = "economy.spent";
    public const string ItemPurchased = "item.purchased";
    public const string ItemDropped = "item.dropped";

    // Progression events
    public const string LevelUp = "progression.levelup";
    public const string ObjectiveStarted = "objective.started";
    public const string ObjectiveCompleted = "objective.completed";
    public const string ObjectiveFailed = "objective.failed";

    // Technical events
    public const string FrameTime = "perf.frametime";
    public const string MemoryUsage = "perf.memory";
    public const string LoadTime = "perf.loadtime";
    public const string Error = "system.error";
    public const string Crash = "system.crash";
}

