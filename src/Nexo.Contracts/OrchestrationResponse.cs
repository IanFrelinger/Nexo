namespace Nexo.Contracts;

/// <summary>Result of an orchestration run.</summary>
/// <param name="Success">Whether orchestration completed successfully.</param>
/// <param name="Summary">Human-readable outcome summary.</param>
/// <param name="Output">Structured orchestration output.</param>
/// <param name="Conflicts">Number of policy or routing conflicts detected.</param>
/// <param name="Escalations">Number of escalations triggered during the run.</param>
/// <param name="ErrorCode">Canonical error code when <paramref name="Success"/> is false.</param>
public sealed record OrchestrationResponse(
    bool Success,
    string? Summary,
    object? Output,
    int? Conflicts = null,
    int? Escalations = null,
    string? ErrorCode = null);
