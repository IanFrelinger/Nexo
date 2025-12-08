using Nexo.Orchestration.Playtest.Models;

namespace Nexo.Orchestration.Playtest.Ports;

/// <summary>
/// Port for storing and retrieving telemetry events.
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

