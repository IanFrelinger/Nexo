using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Ashlar.Orchestration.Resilience;

/// <summary>
/// State of a circuit breaker.
/// </summary>
public enum CircuitStateType
{
    Closed,   // Normal operation
    Open,     // Failing, reject requests
    HalfOpen  // Testing if service recovered
}
