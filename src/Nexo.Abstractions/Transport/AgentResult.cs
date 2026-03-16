namespace Nexo.Abstractions.Transport;

/// <summary>
/// Canonical result of transport-level agent invocation.
/// </summary>
/// <param name="Success">Whether invocation completed successfully.</param>
/// <param name="Output">Optional invocation output payload.</param>
/// <param name="ErrorMessage">Optional error message for failed invocations.</param>
/// <param name="Duration">Optional execution duration.</param>
/// <param name="Metadata">Optional response metadata.</param>
/// <param name="ErrorCode">Optional canonical error code.</param>
/// <param name="CorrelationId">Optional correlation id associated with the invocation.</param>
/// <param name="SpanId">Optional span id associated with the invocation.</param>
public sealed record AgentResult(
    bool Success,
    object? Output = null,
    string? ErrorMessage = null,
    TimeSpan? Duration = null,
    IReadOnlyDictionary<string, string>? Metadata = null,
    string? ErrorCode = null,
    string? CorrelationId = null,
    string? SpanId = null);
