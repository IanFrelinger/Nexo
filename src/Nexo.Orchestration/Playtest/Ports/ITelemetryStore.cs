using Nexo.Orchestration.Playtest.Models;

namespace Nexo.Orchestration.Playtest.Ports;

/// <summary>
/// Port for storing and retrieving telemetry events.
/// 
/// Defines the contract for telemetry storage adapters:
/// - Store telemetry events from playtest sessions
/// - Retrieve events by session ID and type
/// - Clear session data
/// 
/// Implementations (InMemoryTelemetryStore, etc.) provide specific storage logic.
/// Used by playtest agents to record and analyze gameplay telemetry.
/// </summary>
public interface ITelemetryStore
{
    /// <summary>
    /// Stores a telemetry event.
    /// </summary>
    Task StoreAsync(TelemetryEvent evt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all events for a session.
    /// </summary>
    Task<IReadOnlyList<TelemetryEvent>> GetEventsAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets events of a specific type.
    /// </summary>
    Task<IReadOnlyList<TelemetryEvent>> GetEventsByTypeAsync(string sessionId, string eventType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all events for a session.
    /// </summary>
    Task ClearAsync(string sessionId, CancellationToken cancellationToken = default);
}

