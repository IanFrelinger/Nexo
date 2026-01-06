namespace Nexo.Infrastructure.Execution;

/// <summary>
/// Options for behavior execution.
/// </summary>
public class ExecutionOptions
{
    public bool IsAirGapped { get; init; }
    public bool AuditMode { get; init; }
    public string Provider { get; init; } = "openai";
}

