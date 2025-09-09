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
    ErrorRate,
    BatteryUsage
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
    public string Context { get; init; } = string.Empty;
    public Dictionary<string, object> Parameters { get; init; } = new();
    public string RequestId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string TargetPlatform { get; init; } = string.Empty;
    public Nexo.Core.Domain.Entities.Infrastructure.PerformanceRequirements PerformanceRequirements { get; init; } = new();
    public AgentSpecialization RequiredSpecialization { get; init; } = AgentSpecialization.None;
    
    /// <summary>
    /// Create a platform-specific request
    /// </summary>
    public AgentRequest CreatePlatformSpecificRequest(string platform)
    {
        return this with { TargetPlatform = platform };
    }
}

/// <summary>
/// Agent response model
/// </summary>
public record AgentResponse
{
    public string Output { get; init; } = string.Empty;
    public bool Success { get; init; } = true;
    public string ErrorMessage { get; init; } = string.Empty;
    public Dictionary<string, object> Metadata { get; init; } = new();
    public string RequestId { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public TimeSpan ProcessingTime { get; init; } = TimeSpan.Zero;
    public string Result { get; init; } = string.Empty;
    public double Confidence { get; init; } = 0.0;
    public bool ShouldTerminateWorkflow { get; init; } = false;
    public bool HasResult { get; init; } = false;
    
    /// <summary>
    /// Get metadata value
    /// </summary>
    public T? GetMetadata<T>(string key)
    {
        if (Metadata.TryGetValue(key, out var value) && value is T tValue)
            return tValue;
        return default;
    }
    
    /// <summary>
    /// Static response for no action needed
    /// </summary>
    public static AgentResponse NoAction => new() { Success = true, Result = "No action needed" };
    
    /// <summary>
    /// Static response for no optimization needed
    /// </summary>
    public static AgentResponse NoOptimizationNeeded => new() { Success = true, Result = "No optimization needed" };
    
    /// <summary>
    /// Static response for secure code generated
    /// </summary>
    public static AgentResponse SecureCodeGenerated => new() { Success = true, Result = "Secure code generated" };
}

/// <summary>
/// Agent capabilities for AI agents
/// </summary>
[Flags]
public enum AgentCapabilities
{
    None = 0,
    CodeGeneration = 1,
    PerformanceAnalysis = 2,
    PlatformOptimization = 4,
    SecurityAnalysis = 8,
    MobileOptimization = 16,
    WebOptimization = 32,
    UnityOptimization = 64,
    All = CodeGeneration | PerformanceAnalysis | PlatformOptimization | SecurityAnalysis | MobileOptimization | WebOptimization | UnityOptimization
}

/// <summary>
/// Agent capabilities record for AI agents
/// </summary>
public record AgentCapabilitiesRecord
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public AgentSpecialization Specialization { get; init; } = AgentSpecialization.None;
    public PlatformCompatibility PlatformSupport { get; init; } = PlatformCompatibility.None;
    public PerformanceProfile PerformanceProfile { get; init; } = new();
    public bool SupportsAsync { get; init; } = true;
    public bool SupportsParallelization { get; init; } = false;
    public int MaxConcurrentRequests { get; init; } = 1;
    public TimeSpan DefaultTimeout { get; init; } = TimeSpan.FromMinutes(5);
}

/// <summary>
/// Agent coordinator interface for managing multiple agents
/// </summary>
public interface IAgentCoordinator
{
    Task<AgentResponse> ProcessRequestAsync(AgentRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<AgentResponse>> ProcessBatchAsync(IEnumerable<AgentRequest> requests, CancellationToken cancellationToken = default);
    Task<AgentResponse> ProcessWithSpecializationAsync(AgentRequest request, AgentSpecialization specialization, CancellationToken cancellationToken = default);
    Task<bool> IsAgentAvailableAsync(AgentSpecialization specialization, CancellationToken cancellationToken = default);
    Task<IEnumerable<ISpecializedAgent>> GetAvailableAgentsAsync(CancellationToken cancellationToken = default);
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
