using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Agents.Specialized;
using Nexo.Feature.AI.Interfaces;

namespace Nexo.Feature.AI.Agents.Coordination;

/// <summary>
/// Plans and optimizes agent workflows for complex tasks
/// </summary>
public class AgentWorkflowPlanner : IAgentWorkflowPlanner
{
    private readonly IModelOrchestrator _modelOrchestrator;
    private readonly ILogger<AgentWorkflowPlanner> _logger;
    
    public AgentWorkflowPlanner(
        IModelOrchestrator modelOrchestrator,
        ILogger<AgentWorkflowPlanner> logger)
    {
        _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<AgentWorkflow> CreateWorkflowAsync(
        IEnumerable<ISpecializedAgent> agents, 
        ComplexAgentRequest request)
    {
        try
        {
            _logger.LogInformation("Creating workflow for {AgentCount} agents", agents.Count());
            
            var workflowId = Guid.NewGuid().ToString();
            var steps = new List<WorkflowStep>();
            
            // Analyze the request to determine workflow structure
            var workflowStructure = await AnalyzeWorkflowStructure(agents, request);
            
            // Create steps based on the structure
            foreach (var structureStep in workflowStructure.Steps)
            {
                var agent = agents.FirstOrDefault(a => a.AgentId == structureStep.AgentId);
                if (agent == null)
                {
                    _logger.LogWarning("Agent {AgentId} not found in available agents", structureStep.AgentId);
                    continue;
                }
                
                var step = new WorkflowStep
                {
                    Name = structureStep.Name,
                    AssignedAgent = agent,
                    Request = CreateStepRequest(structureStep, request),
                    RequiresCoordination = structureStep.RequiresCoordination,
                    Collaborators = GetCollaborators(agents, structureStep.CollaboratorIds)
                };
                
                steps.Add(step);
            }
            
            var workflow = new AgentWorkflow
            {
                WorkflowId = workflowId,
                Name = $"Workflow-{workflowId[..8]}",
                Steps = steps,
                Context = new Dictionary<string, object>
                {
                    ["OriginalRequest"] = request.Description,
                    ["AgentCount"] = agents.Count(),
                    ["CreatedAt"] = DateTime.UtcNow
                }
            };
            
            _logger.LogDebug("Created workflow with {StepCount} steps", steps.Count);
            return workflow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating workflow");
            throw;
        }
    }
    
    public async Task<AgentWorkflow> OptimizeWorkflowAsync(AgentWorkflow workflow)
    {
        try
        {
            _logger.LogInformation("Optimizing workflow {WorkflowId}", workflow.WorkflowId);
            
            var optimizationPrompt = $"""
            Analyze and optimize this agent workflow for better performance and efficiency:
            
            Workflow: {workflow.Name}
            Steps: {workflow.Steps.Count()}
            
            Current Steps:
            """;
            
            foreach (var step in workflow.Steps)
            {
                optimizationPrompt += $"- {step.Name}: {step.AssignedAgent.AgentId} (Coordination: {step.RequiresCoordination})\n";
            }
            
            optimizationPrompt += """
            
            Suggest optimizations for:
            1. Step ordering and dependencies
            2. Parallel execution opportunities
            3. Coordination requirements
            4. Resource utilization
            5. Error handling and fallbacks
            
            Provide an optimized workflow structure.
            """;
            
            var modelRequest = new Models.ModelRequest
            {
                Input = optimizationPrompt,
                Temperature = 0.3,
                MaxTokens = 1000
            };
            
            var response = await _modelOrchestrator.ExecuteAsync(modelRequest);
            
            if (response.Success)
            {
                // Parse optimization suggestions and apply them
                var optimizedSteps = await ApplyOptimizations(workflow.Steps, response.Response);
                
                return workflow with { Steps = optimizedSteps };
            }
            
            _logger.LogWarning("Failed to optimize workflow, returning original");
            return workflow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing workflow");
            return workflow;
        }
    }
    
    private async Task<WorkflowStructure> AnalyzeWorkflowStructure(
        IEnumerable<ISpecializedAgent> agents, 
        ComplexAgentRequest request)
    {
        var analysisPrompt = $"""
        Analyze this complex request and create an optimal workflow structure:
        
        Request: {request.Description}
        Platforms: {string.Join(", ", request.TargetPlatforms)}
        Performance Requirements: {request.PerformanceRequirements?.PrimaryTarget}
        Security Requirements: {request.SecurityRequirements?.Level}
        
        Available Agents:
        """;
        
        foreach (var agent in agents)
        {
            analysisPrompt += $"- {agent.AgentId}: {agent.Specialization}\n";
        }
        
        analysisPrompt += """
        
        Create a workflow that:
        1. Determines the optimal order of agent execution
        2. Identifies which steps require coordination
        3. Minimizes dependencies and bottlenecks
        4. Ensures all requirements are addressed
        5. Optimizes for parallel execution where possible
        
        Return a structured workflow plan with step names, agent assignments, and coordination requirements.
        """;
        
        var modelRequest = new Models.ModelRequest
        {
            Input = analysisPrompt,
            Temperature = 0.4,
            MaxTokens = 1500
        };
        
        var response = await _modelOrchestrator.ExecuteAsync(modelRequest);
        
        if (!response.Success)
        {
            _logger.LogWarning("Failed to analyze workflow structure, using default");
            return CreateDefaultWorkflowStructure(agents);
        }
        
        return ParseWorkflowStructure(response.Response, agents);
    }
    
    private WorkflowStructure CreateDefaultWorkflowStructure(IEnumerable<ISpecializedAgent> agents)
    {
        var steps = new List<WorkflowStructureStep>();
        var agentList = agents.ToList();
        
        // Default workflow: Security -> Performance -> Platform -> Quality
        var securityAgent = agentList.FirstOrDefault(a => a.Specialization.HasFlag(AgentSpecialization.SecurityAnalysis));
        var performanceAgent = agentList.FirstOrDefault(a => a.Specialization.HasFlag(AgentSpecialization.PerformanceOptimization));
        var platformAgent = agentList.FirstOrDefault(a => a.Specialization.HasFlag(AgentSpecialization.PlatformSpecific));
        var qualityAgent = agentList.FirstOrDefault(a => a.Specialization.HasFlag(AgentSpecialization.CodeQuality));
        
        if (securityAgent != null)
        {
            steps.Add(new WorkflowStructureStep
            {
                Name = "SecurityAnalysis",
                AgentId = securityAgent.AgentId,
                RequiresCoordination = false
            });
        }
        
        if (performanceAgent != null)
        {
            steps.Add(new WorkflowStructureStep
            {
                Name = "PerformanceOptimization",
                AgentId = performanceAgent.AgentId,
                RequiresCoordination = platformAgent != null,
                CollaboratorIds = platformAgent != null ? new[] { platformAgent.AgentId } : []
            });
        }
        
        if (platformAgent != null)
        {
            steps.Add(new WorkflowStructureStep
            {
                Name = "PlatformOptimization",
                AgentId = platformAgent.AgentId,
                RequiresCoordination = false
            });
        }
        
        if (qualityAgent != null)
        {
            steps.Add(new WorkflowStructureStep
            {
                Name = "QualityAssurance",
                AgentId = qualityAgent.AgentId,
                RequiresCoordination = false
            });
        }
        
        return new WorkflowStructure { Steps = steps };
    }
    
    private WorkflowStructure ParseWorkflowStructure(string response, IEnumerable<ISpecializedAgent> agents)
    {
        // Simple parsing - in a real implementation, this would be more sophisticated
        var steps = new List<WorkflowStructureStep>();
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            if (line.Contains(":"))
            {
                var parts = line.Split(':', 2);
                var stepName = parts[0].Trim();
                var agentId = parts[1].Trim();
                
                var agent = agents.FirstOrDefault(a => a.AgentId == agentId);
                if (agent != null)
                {
                    steps.Add(new WorkflowStructureStep
                    {
                        Name = stepName,
                        AgentId = agentId,
                        RequiresCoordination = stepName.Contains("coordination", StringComparison.OrdinalIgnoreCase)
                    });
                }
            }
        }
        
        // Fallback to default if parsing failed
        if (!steps.Any())
        {
            return CreateDefaultWorkflowStructure(agents);
        }
        
        return new WorkflowStructure { Steps = steps };
    }
    
    private AgentRequest CreateStepRequest(WorkflowStructureStep structureStep, ComplexAgentRequest request)
    {
        return new AgentRequest
        {
            Input = $"{request.Description}\n\nStep: {structureStep.Name}",
            Context = request.Context?.ToString() ?? string.Empty,
            PerformanceRequirements = ConvertToPerformanceRequirements(request.PerformanceRequirements),
            RequiredSpecialization = GetSpecializationForStep(structureStep.Name)
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
    
    private AgentSpecialization GetSpecializationForStep(string stepName)
    {
        return stepName.ToLowerInvariant() switch
        {
            var name when name.Contains("security") => AgentSpecialization.SecurityAnalysis,
            var name when name.Contains("performance") => AgentSpecialization.PerformanceOptimization,
            var name when name.Contains("platform") => AgentSpecialization.PlatformSpecific,
            var name when name.Contains("quality") => AgentSpecialization.CodeQuality,
            var name when name.Contains("test") => AgentSpecialization.TestGeneration,
            var name when name.Contains("documentation") => AgentSpecialization.DocumentationGeneration,
            _ => AgentSpecialization.None
        };
    }
    
    private IEnumerable<ISpecializedAgent> GetCollaborators(
        IEnumerable<ISpecializedAgent> agents, 
        IEnumerable<string> collaboratorIds)
    {
        return agents.Where(a => collaboratorIds.Contains(a.AgentId));
    }
    
    private async Task<IEnumerable<WorkflowStep>> ApplyOptimizations(
        IEnumerable<WorkflowStep> originalSteps, 
        string optimizationResponse)
    {
        // Simple optimization - in a real implementation, this would parse and apply specific optimizations
        _logger.LogDebug("Applying workflow optimizations");
        
        // For now, just return the original steps
        // In a full implementation, this would:
        // 1. Parse optimization suggestions
        // 2. Reorder steps for better parallelization
        // 3. Adjust coordination requirements
        // 4. Add error handling steps
        
        return originalSteps;
    }
}

/// <summary>
/// Internal workflow structure for planning
/// </summary>
internal record WorkflowStructure
{
    public IEnumerable<WorkflowStructureStep> Steps { get; init; } = [];
}

/// <summary>
/// Internal workflow step structure
/// </summary>
internal record WorkflowStructureStep
{
    public string Name { get; init; } = string.Empty;
    public string AgentId { get; init; } = string.Empty;
    public bool RequiresCoordination { get; init; } = false;
    public IEnumerable<string> CollaboratorIds { get; init; } = [];
}
