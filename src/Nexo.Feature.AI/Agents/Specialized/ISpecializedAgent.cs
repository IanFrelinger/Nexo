using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.AI.Agents.Specialized;

/// <summary>
/// Specialized AI agent with domain expertise and coordination capabilities
/// </summary>
public interface ISpecializedAgent : IAIAgent
{
    /// <summary>
    /// Agent's area of specialization
    /// </summary>
    AgentSpecialization Specialization { get; }
    
    /// <summary>
    /// Platforms this agent specializes in
    /// </summary>
    PlatformCompatibility PlatformExpertise { get; }
    
    /// <summary>
    /// Performance characteristics this agent optimizes for
    /// </summary>
    PerformanceProfile OptimizationProfile { get; }
    
    /// <summary>
    /// Can this agent handle the given request effectively?
    /// </summary>
    Task<AgentCapabilityAssessment> AssessCapabilityAsync(AgentRequest request);
    
    /// <summary>
    /// Coordinate with other agents for complex tasks
    /// </summary>
    Task<AgentResponse> CoordinateAsync(AgentRequest request, IEnumerable<ISpecializedAgent> collaborators);
    
    /// <summary>
    /// Learn from successful/failed attempts to improve future performance
    /// </summary>
    Task LearnFromResultAsync(AgentRequest request, AgentResponse response, PerformanceMetrics metrics);
}

/// <summary>
/// Base interface for all AI agents
/// </summary>
public interface IAIAgent
{
    /// <summary>
    /// Unique identifier for this agent
    /// </summary>
    string AgentId { get; }
    
    /// <summary>
    /// Process a request and return a response
    /// </summary>
    Task<AgentResponse> ProcessAsync(AgentRequest request);
}

/// <summary>
/// Agent specialization areas
/// </summary>
[Flags]
public enum AgentSpecialization
{
    None = 0,
    PerformanceOptimization = 1,
    SecurityAnalysis = 2,
    PlatformSpecific = 4,
    ArchitecturalDesign = 8,
    TestGeneration = 16,
    DocumentationGeneration = 32,
    CodeQuality = 64,
    DatabaseDesign = 128,
    NetworkingOptimization = 256,
    UIUXGeneration = 512,
    GameDevelopment = 1024,
    WebDevelopment = 2048,
    MobileDevelopment = 4096,
    DevOpsIntegration = 8192
}

/// <summary>
/// Platform compatibility for agents
/// </summary>
[Flags]
public enum PlatformCompatibility
{
    None = 0,
    All = 1,
    Unity = 2,
    Web = 4,
    Mobile = 8,
    Desktop = 16,
    Server = 32,
    Cloud = 64,
    IoT = 128,
    GameConsole = 256
}

/// <summary>
/// Performance profile for agent optimization
/// </summary>
public record PerformanceProfile
{
    public OptimizationTarget PrimaryTarget { get; init; } = OptimizationTarget.Balanced;
    public IEnumerable<PerformanceMetric> MonitoredMetrics { get; init; } = [];
    public PerformanceLevel MinimumAcceptableLevel { get; init; } = PerformanceLevel.Medium;
    public bool SupportsRealTimeOptimization { get; init; } = false;
}

/// <summary>
/// Optimization targets for agents
/// </summary>
public enum OptimizationTarget 
{ 
    Performance, 
    Memory, 
    Readability, 
    Maintainability, 
    Security, 
    Balanced 
}

/// <summary>
/// Performance metrics to monitor
/// </summary>
public enum PerformanceMetric
{
    ExecutionTime,
    MemoryUsage,
    CpuUtilization,
    FrameRate,
    GarbageCollection,
    DrawCalls,
    NetworkLatency,
    DatabaseQueries,
    CacheHitRate,
    ErrorRate
}

/// <summary>
/// Performance levels
/// </summary>
public enum PerformanceLevel
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Agent request model
/// </summary>
public record AgentRequest
{
    public string Input { get; init; } = string.Empty;
    public string? SystemPrompt { get; init; }
    public Dictionary<string, object>? Context { get; init; }
    public AgentSpecialization? RequiredSpecialization { get; init; }
    public PlatformCompatibility? TargetPlatform { get; init; }
    public PerformanceProfile? PerformanceRequirements { get; init; }
    
    public AgentRequest CreatePlatformSpecificRequest(PlatformCompatibility platform)
    {
        return this with { TargetPlatform = platform };
    }
}

/// <summary>
/// Agent response model
/// </summary>
public record AgentResponse
{
    public string Result { get; init; } = string.Empty;
    public bool Success { get; init; } = true;
    public string? ErrorMessage { get; init; }
    public double Confidence { get; init; } = 0.0;
    public Dictionary<string, object>? Metadata { get; init; }
    public bool ShouldTerminateWorkflow { get; init; } = false;
    
    public bool HasResult => !string.IsNullOrEmpty(Result);
    
    public T? GetMetadata<T>(string key) where T : class
    {
        return Metadata?.TryGetValue(key, out var value) == true ? value as T : null;
    }
    
    public static AgentResponse NoOptimizationNeeded => new() { Success = true, Confidence = 1.0 };
    public static AgentResponse SecureCodeGenerated => new() { Success = true, Confidence = 0.95 };
}

/// <summary>
/// Agent capability assessment
/// </summary>
public record AgentCapabilityAssessment
{
    public double CapabilityScore { get; init; } = 0.0;
    public string[] Strengths { get; init; } = [];
    public string[] Limitations { get; init; } = [];
    public bool CanHandleRequest { get; init; } = false;
    public string? Recommendation { get; init; }
}

/// <summary>
/// Performance metrics for learning
/// </summary>
public record PerformanceMetrics
{
    public TimeSpan ExecutionTime { get; init; }
    public long MemoryUsage { get; init; }
    public double CpuUsage { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Dictionary<string, object>? AdditionalMetrics { get; init; }
}
