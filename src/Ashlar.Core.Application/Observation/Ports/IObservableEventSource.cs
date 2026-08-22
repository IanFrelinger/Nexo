using Ashlar.Core.Application.Observation.Models;

namespace Ashlar.Core.Application.Observation.Ports;

/// <summary>
/// A source of observable events. Implementations produce normalized events
/// from system sources (file system, process monitor, etc.).
/// </summary>
public interface IObservableEventSource
{
    /// <summary>
    /// Returns the source identifier (e.g. file-system, process-monitor).
    /// </summary>
    string SourceId { get; }

    /// <summary>
    /// Subscribe to the event stream. Caller must dispose of the returned
    /// enumerable or cancel the token to stop receiving events.
    /// </summary>
    IAsyncEnumerable<NormalizedEvent> SubscribeAsync(CancellationToken cancellationToken = default);
}
