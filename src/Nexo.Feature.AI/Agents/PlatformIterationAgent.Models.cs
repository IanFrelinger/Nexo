using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Services.Iteration;
using Nexo.Core.Domain.Entities.Iteration;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Interfaces.Infrastructure;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Agents.Specialized;

namespace Nexo.Feature.AI.Agents;

/// <summary>
/// Data models for platform iteration analysis
/// </summary>
public partial class PlatformIterationAgent
{
}

/// <summary>
/// Analysis of platform-specific requirements
/// </summary>
public record PlatformAnalysis
{
    /// <summary>
    /// Platform type
    /// </summary>
    public PlatformType PlatformType { get; init; }
    
    /// <summary>
    /// Whether platform optimization is required
    /// </summary>
    public bool RequiresPlatformOptimization { get; init; }
    
    /// <summary>
    /// Platform constraints
    /// </summary>
    public string PlatformConstraints { get; init; } = string.Empty;
    
    /// <summary>
    /// Performance characteristics
    /// </summary>
    public string PerformanceCharacteristics { get; init; } = string.Empty;
    
    /// <summary>
    /// Platform-specific APIs
    /// </summary>
    public string PlatformSpecificAPIs { get; init; } = string.Empty;
    
    /// <summary>
    /// Memory constraints
    /// </summary>
    public string MemoryConstraints { get; init; } = string.Empty;
    
    /// <summary>
    /// CPU constraints
    /// </summary>
    public string CPUConstraints { get; init; } = string.Empty;
    
    /// <summary>
    /// Best practices
    /// </summary>
    public string BestPractices { get; init; } = string.Empty;
    
    /// <summary>
    /// Platform optimizations
    /// </summary>
    public List<string> PlatformOptimizations { get; init; } = new();
    
    /// <summary>
    /// Iteration context
    /// </summary>
    public IterationContext? IterationContext { get; init; }
}
