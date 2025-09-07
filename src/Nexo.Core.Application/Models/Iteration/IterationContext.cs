using System;
using System.Collections.Generic;
using Nexo.Core.Domain.Entities.Iteration;

namespace Nexo.Core.Application.Models.Iteration;

/// <summary>
/// Context for iteration strategy selection
/// </summary>
public record IterationContext
{
    public int DataSize { get; init; }
    public IterationRequirements Requirements { get; init; } = new();
    public RuntimeEnvironmentProfile EnvironmentProfile { get; init; } = new();
    public CodeGenerationContext? CodeGeneration { get; init; }
}

/// <summary>
/// Requirements and preferences for iteration behavior
/// </summary>
public record IterationRequirements
{
    public bool PrioritizeCpu { get; init; } = false;
    public bool PrioritizeMemory { get; init; } = false;
    public bool RequiresParallelization { get; init; } = false;
    public bool RequiresOrdering { get; init; } = true;
    public bool AllowSideEffects { get; init; } = true;
    public int? MaxDegreeOfParallelism { get; init; }
    public TimeSpan? Timeout { get; init; }
}

/// <summary>
/// Runtime environment characteristics for strategy optimization
/// </summary>
public record RuntimeEnvironmentProfile
{
    public PlatformCompatibility PlatformType { get; init; }
    public int CpuCores { get; init; }
    public long AvailableMemoryMB { get; init; }
    public bool IsDebugMode { get; init; }
    public string FrameworkVersion { get; init; } = "";
    public OptimizationLevel OptimizationLevel { get; init; }
}

public enum OptimizationLevel { Debug, Balanced, Aggressive }
