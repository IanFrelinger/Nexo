using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Feature.AI.Agents.Specialized;

namespace Nexo.Feature.AI.Agents.Coordination;

/// <summary>
/// Complex agent request that requires coordination
/// </summary>
public record ComplexAgentRequest
{
    public string Description { get; init; } = string.Empty;
    public IEnumerable<PlatformCompatibility> TargetPlatforms { get; init; } = [];
    public PerformanceProfile? PerformanceRequirements { get; init; }
    public SecurityRequirements? SecurityRequirements { get; init; }
    public QualityRequirements? QualityRequirements { get; init; }
    public Dictionary<string, object>? Context { get; init; }
    
    public AgentRequest ToAgentRequest()
    {
        return new AgentRequest
        {
            Input = Description,
            Context = Context?.ToString() ?? string.Empty,
            PerformanceRequirements = ConvertToPerformanceRequirements(PerformanceRequirements)
        };
    }
    
    private static Nexo.Core.Domain.Entities.Infrastructure.PerformanceRequirements ConvertToPerformanceRequirements(PerformanceProfile? profile)
    {
        if (profile == null)
            return new Nexo.Core.Domain.Entities.Infrastructure.PerformanceRequirements();
            
        return new Nexo.Core.Domain.Entities.Infrastructure.PerformanceRequirements
        {
            MaxExecutionTimeMs = 5000, // Default 5 seconds
            MaxMemoryUsageMB = profile.MinimumAcceptableLevel switch
            {
                PerformanceLevel.Low => 200,
                PerformanceLevel.Medium => 100,
                PerformanceLevel.High => 50,
                PerformanceLevel.Critical => 25,
                _ => 100
            },
            RequiresRealTime = profile.SupportsRealTimeOptimization,
            PreferParallel = profile.PrimaryTarget == OptimizationTarget.Performance,
            MemoryCritical = profile.PrimaryTarget == OptimizationTarget.Memory
        };
    }
}

/// <summary>
/// Security requirements for agent coordination
/// </summary>
public record SecurityRequirements
{
    public SecurityLevel Level { get; init; } = SecurityLevel.Standard;
    public string[] RequiredCompliance { get; init; } = [];
    public bool RequiresEncryption { get; init; } = false;
    public bool RequiresAuthentication { get; init; } = false;
    public bool RequiresAuthorization { get; init; } = false;
}

/// <summary>
/// Quality requirements for agent coordination
/// </summary>
public record QualityRequirements
{
    public double MinimumCodeQuality { get; init; } = 0.8;
    public bool RequiresTests { get; init; } = true;
    public bool RequiresDocumentation { get; init; } = true;
    public bool RequiresCodeReview { get; init; } = false;
    public string[] CodingStandards { get; init; } = [];
}

/// <summary>
/// Security levels
/// </summary>
public enum SecurityLevel
{
    Basic,
    Standard,
    High,
    Critical
}

/// <summary>
/// Agent workflow for coordination
/// </summary>
public record AgentWorkflow
{
    public string WorkflowId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public IEnumerable<WorkflowStep> Steps { get; init; } = [];
    public Dictionary<string, object>? Context { get; init; }
}

/// <summary>
/// Individual step in an agent workflow
/// </summary>
public record WorkflowStep
{
    public string Name { get; init; } = string.Empty;
    public ISpecializedAgent AssignedAgent { get; init; } = null!;
    public AgentRequest Request { get; init; } = null!;
    public IEnumerable<ISpecializedAgent> Collaborators { get; init; } = [];
    public bool RequiresCoordination { get; init; } = false;
    public Dictionary<string, object>? Dependencies { get; init; }
}

/// <summary>
/// Coordinated response from multiple agents
/// </summary>
public record CoordinatedResponse
{
    public Dictionary<string, AgentResponse> Responses { get; init; } = [];
    public ExecutionContext ExecutionContext { get; init; } = new();
    public string SynthesizedResult { get; init; } = string.Empty;
    public QualityAssessment QualityAssessment { get; init; } = new();
    public IEnumerable<PerformanceOptimization> PerformanceOptimizations { get; init; } = [];
    public SecurityValidation SecurityValidation { get; init; } = new();
    public PlatformCompatibility PlatformCompatibility { get; init; } = PlatformCompatibility.None;
}

/// <summary>
/// Execution context for workflow coordination
/// </summary>
public class ExecutionContext
{
    public Dictionary<string, object> Variables { get; } = new();
    public List<string> CompletedSteps { get; } = new();
    public Dictionary<string, object> SharedResults { get; } = new();
    
    public void UpdateFromResponse(string stepName, AgentResponse response)
    {
        CompletedSteps.Add(stepName);
        if (response.Metadata != null)
        {
            foreach (var kvp in response.Metadata)
            {
                SharedResults[kvp.Key] = kvp.Value;
            }
        }
    }
    
    public T? GetVariable<T>(string key) where T : class
    {
        return Variables.TryGetValue(key, out var value) ? value as T : null;
    }
    
    public void SetVariable(string key, object value)
    {
        Variables[key] = value;
    }
}

/// <summary>
/// Quality assessment for coordinated responses
/// </summary>
public record QualityAssessment
{
    public double OverallScore { get; init; } = 0.0;
    public double CodeQuality { get; init; } = 0.0;
    public double PerformanceScore { get; init; } = 0.0;
    public double SecurityScore { get; init; } = 0.0;
    public double MaintainabilityScore { get; init; } = 0.0;
    public string[] Recommendations { get; init; } = [];
}

/// <summary>
/// Performance optimization result
/// </summary>
public record PerformanceOptimization
{
    public string Type { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public double ImprovementFactor { get; init; } = 1.0;
    public Dictionary<string, object>? Metrics { get; init; }
}

/// <summary>
/// Security validation result
/// </summary>
public record SecurityValidation
{
    public bool IsSecure { get; init; } = true;
    public SecurityLevel AchievedLevel { get; init; } = SecurityLevel.Standard;
    public string[] Vulnerabilities { get; init; } = [];
    public string[] SecurityImprovements { get; init; } = [];
    public string ComplianceStatus { get; init; } = string.Empty;
}

/// <summary>
/// Agent coordinator interface
/// </summary>
public interface IAgentCoordinator
{
    Task<AgentResponse> CoordinateComplexTaskAsync(ComplexAgentRequest request);
    Task<IEnumerable<ISpecializedAgent>> SelectOptimalAgentsAsync(
        IEnumerable<AgentSpecialization> requiredSpecializations, 
        ComplexAgentRequest request);
    Task<AgentWorkflow> CreateWorkflowAsync(
        IEnumerable<ISpecializedAgent> agents, 
        ComplexAgentRequest request);
}

/// <summary>
/// Agent workflow planner interface
/// </summary>
public interface IAgentWorkflowPlanner
{
    Task<AgentWorkflow> CreateWorkflowAsync(
        IEnumerable<ISpecializedAgent> agents, 
        ComplexAgentRequest request);
    Task<AgentWorkflow> OptimizeWorkflowAsync(AgentWorkflow workflow);
}

/// <summary>
/// Agent communication hub interface
/// </summary>
public interface IAgentCommunicationHub
{
    Task BroadcastMessageAsync(string message, IEnumerable<ISpecializedAgent> agents);
    Task<T> RequestFromAgentAsync<T>(ISpecializedAgent agent, string request);
    Task<Dictionary<string, T>> RequestFromMultipleAgentsAsync<T>(
        IEnumerable<ISpecializedAgent> agents, 
        string request);
}
