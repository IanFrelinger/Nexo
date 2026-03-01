using Nexo.Core.Application.Observation.Models;

namespace Nexo.Core.Application.Observation.Ports;

/// <summary>
/// Assembles observation context for agent consumption.
/// </summary>
public interface IContextAssembler
{
    /// <summary>
    /// Assemble context from patterns and optionally recent events.
    /// </summary>
    /// <param name="projectPath">Optional project path filter.</param>
    /// <param name="since">Include patterns/events after this time.</param>
    /// <param name="until">Include patterns/events before this time.</param>
    /// <param name="maxPatterns">Maximum patterns to include.</param>
    /// <param name="includeRecentEvents">Whether to include recent events in context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ObservationContext> AssembleContextAsync(
        string? projectPath = null,
        DateTimeOffset? since = null,
        DateTimeOffset? until = null,
        int maxPatterns = 50,
        bool includeRecentEvents = false,
        CancellationToken cancellationToken = default);
}
