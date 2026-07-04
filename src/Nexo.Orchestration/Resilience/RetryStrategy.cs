using Microsoft.Extensions.Logging;

namespace Nexo.Orchestration.Resilience;

/// <summary>
/// Retry strategy types.
/// </summary>
public enum RetryStrategy
{
    Fixed,                        // Fixed delay between retries
    Linear,                       // Linear increase in delay
    ExponentialBackoff,           // Exponential increase in delay
    JitteredExponentialBackoff    // Exponential with random jitter
}
