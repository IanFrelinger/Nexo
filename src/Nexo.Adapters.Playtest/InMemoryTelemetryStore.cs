using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Playtest.Models;
using Nexo.Orchestration.Playtest.Ports;

namespace Nexo.Adapters.Playtest;

/// <summary>
/// In-memory telemetry store implementation for testing.
/// </summary>
public sealed class InMemoryTelemetryStore : ITelemetryStore
{
    private readonly Dictionary<string, List<TelemetryEvent>> _events = new();
    private readonly ILogger<InMemoryTelemetryStore> _logger;

    public InMemoryTelemetryStore(ILogger<InMemoryTelemetryStore> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task StoreAsync(TelemetryEvent evt, CancellationToken cancellationToken = default)
    {
        if (!_events.TryGetValue(evt.SessionId, out var sessionEvents))
        {
            sessionEvents = new List<TelemetryEvent>();
            _events[evt.SessionId] = sessionEvents;
        }

        sessionEvents.Add(evt);
        _logger.LogDebug("Stored telemetry event: {EventType} for session {SessionId}", evt.EventType, evt.SessionId);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<TelemetryEvent>> GetEventsAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (_events.TryGetValue(sessionId, out var sessionEvents))
        {
            return Task.FromResult<IReadOnlyList<TelemetryEvent>>(sessionEvents);
        }

        return Task.FromResult<IReadOnlyList<TelemetryEvent>>(Array.Empty<TelemetryEvent>());
    }

    public Task<IReadOnlyList<TelemetryEvent>> GetEventsByTypeAsync(string sessionId, string eventType, CancellationToken cancellationToken = default)
    {
        if (_events.TryGetValue(sessionId, out var sessionEvents))
        {
            var filtered = sessionEvents.Where(e => e.EventType == eventType).ToList();
            return Task.FromResult<IReadOnlyList<TelemetryEvent>>(filtered);
        }

        return Task.FromResult<IReadOnlyList<TelemetryEvent>>(Array.Empty<TelemetryEvent>());
    }

    public Task ClearAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _events.Remove(sessionId);
        _logger.LogInformation("Cleared telemetry for session {SessionId}", sessionId);
        return Task.CompletedTask;
    }
}

