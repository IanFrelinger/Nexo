namespace Ashlar.Core.Application.Observation.Ports;

/// <summary>
/// Tracks which patterns have been processed by the self-improvement loop.
/// Prevents re-triggering improvement on the same pattern indefinitely.
/// </summary>
public interface IPatternProcessedStore
{
    /// <summary>Mark a pattern as processed after attempt (success or failure).</summary>
    Task MarkProcessedAsync(string patternId, CancellationToken cancellationToken = default);

    /// <summary>Returns true if the pattern has been processed.</summary>
    Task<bool> IsProcessedAsync(string patternId, CancellationToken cancellationToken = default);
}
