namespace Ashlar.Contracts;

/// <summary>Result of an agent invocation.</summary>
/// <param name="Success">Whether the agent completed successfully.</param>
/// <param name="Message">Human-readable status or error message.</param>
/// <param name="Output">Structured or primitive output payload.</param>
public sealed record AgentResponse(bool Success, string? Message, object? Output);
