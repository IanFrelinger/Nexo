using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Nexo.Orchestration.Resilience;

/// <summary>
/// Exception thrown when circuit breaker is open.
/// </summary>
public sealed class CircuitBreakerOpenException : Exception
{
    public CircuitBreakerOpenException(string message) : base(message) { }
}
