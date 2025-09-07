using System;

namespace Nexo.Feature.Azure.Models;

/// <summary>
/// AKS command execution result
/// </summary>
public record AKSCommandResult
{
    public string PodName { get; init; } = string.Empty;
    public string ContainerName { get; init; } = string.Empty;
    public string Command { get; init; } = string.Empty;
    public string Output { get; init; } = string.Empty;
    public string Error { get; init; } = string.Empty;
    public int ExitCode { get; init; }
    public DateTime ExecutedAt { get; init; }
    public TimeSpan ExecutionTime { get; init; }
} 