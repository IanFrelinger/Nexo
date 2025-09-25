using System;

namespace Nexo.Feature.Pipeline.Models;

/// <summary>
/// Type of pipeline execution.
/// </summary>
public enum PipelineType
{
    ApplicationDevelopment,
    CodeAnalysis,
    PerformanceOptimization,
    PlatformIntegration
}

/// <summary>
/// Type of analysis to perform.
/// </summary>
public enum AnalysisType
{
    CodeQuality,
    Architecture,
    Performance,
    Security
}

/// <summary>
/// Target for performance optimization.
/// </summary>
public enum OptimizationTarget
{
    MemoryUsage,
    CpuUsage,
    NetworkUsage,
    BatteryLife
}

/// <summary>
/// Mode for feature detection.
/// </summary>
public enum FeatureDetectionMode
{
    Automatic,
    Manual,
    Selective
}

/// <summary>
/// Optimization level for pipeline execution.
/// </summary>
public enum OptimizationLevel
{
    None,
    Basic,
    Balanced,
    Aggressive,
    Maximum
}

/// <summary>
/// Implementation complexity levels.
/// </summary>
public enum ImplementationComplexity
{
    Low,
    Medium,
    High,
    VeryHigh
}

/// <summary>
/// Optimization types for performance improvements.
/// </summary>
public enum OptimizationType
{
    CpuOptimization,
    MemoryOptimization,
    DiskOptimization,
    NetworkOptimization,
    AlgorithmOptimization,
    Parallelization,
    Caching,
    ResourceManagement
}
