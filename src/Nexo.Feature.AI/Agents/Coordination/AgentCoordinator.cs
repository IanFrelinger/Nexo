using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Agents.Specialized;
using Nexo.Feature.AI.Interfaces;

namespace Nexo.Feature.AI.Agents.Coordination;

/// <summary>
/// Coordinates multiple specialized agents for complex tasks
/// </summary>
public class AgentCoordinator : IAgentCoordinator
{
    private readonly IEnumerable<ISpecializedAgent> _agents;
    private readonly IAgentWorkflowPlanner _workflowPlanner;
    private readonly IAgentCommunicationHub _communicationHub;
    private readonly IModelOrchestrator _modelOrchestrator;
    private readonly ILogger<AgentCoordinator> _logger;
    
    public AgentCoordinator(
        IEnumerable<ISpecializedAgent> agents,
        IAgentWorkflowPlanner workflowPlanner,
        IAgentCommunicationHub communicationHub,
        IModelOrchestrator modelOrchestrator,
        ILogger<AgentCoordinator> logger)
    {
        _agents = agents ?? throw new ArgumentNullException(nameof(agents));
        _workflowPlanner = workflowPlanner ?? throw new ArgumentNullException(nameof(workflowPlanner));
        _communicationHub = communicationHub ?? throw new ArgumentNullException(nameof(communicationHub));
        _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<AgentResponse> CoordinateComplexTaskAsync(ComplexAgentRequest request)
    {
        try
        {
            _logger.LogInformation("Starting coordination for complex task: {Description}", request.Description);
            
            // Step 1: Analyze request and identify required specializations
            var requiredSpecializations = await AnalyzeRequiredSpecializations(request);
            _logger.LogDebug("Identified required specializations: {Specializations}", 
                string.Join(", ", requiredSpecializations));
            
            // Step 2: Select optimal agents for each specialization
            var selectedAgents = await SelectOptimalAgentsAsync(requiredSpecializations, request);
            _logger.LogDebug("Selected {Count} agents for coordination", selectedAgents.Count());
            
            // Step 3: Create coordination workflow
            var workflow = await _workflowPlanner.CreateWorkflowAsync(selectedAgents, request);
            _logger.LogDebug("Created workflow with {StepCount} steps", workflow.Steps.Count());
            
            // Step 4: Execute coordinated workflow
            var results = await ExecuteCoordinatedWorkflowAsync(workflow);
            _logger.LogDebug("Workflow execution completed with {ResponseCount} responses", 
                results.Responses.Count);
            
            // Step 5: Synthesize final response
            var finalResponse = await SynthesizeFinalResponse(results, request);
            _logger.LogInformation("Complex task coordination completed successfully");
            
            return finalResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during complex task coordination");
            return new AgentResponse
            {
                Success = false,
                ErrorMessage = $"Coordination failed: {ex.Message}",
                Confidence = 0.0
            };
        }
    }
    
    public async Task<IEnumerable<ISpecializedAgent>> SelectOptimalAgentsAsync(
        IEnumerable<AgentSpecialization> requiredSpecializations, 
        ComplexAgentRequest request)
    {
        var selectedAgents = new List<ISpecializedAgent>();
        
        foreach (var specialization in requiredSpecializations)
        {
            var candidates = _agents.Where(a => a.Specialization.HasFlag(specialization));
            
            if (!candidates.Any())
            {
                _logger.LogWarning("No agents found for specialization: {Specialization}", specialization);
                continue;
            }
            
            var assessments = await Task.WhenAll(
                candidates.Select(async agent => new
                {
                    Agent = agent,
                    Assessment = await agent.AssessCapabilityAsync(request.ToAgentRequest())
                }));
            
            var bestAgent = assessments
                .Where(a => a.Assessment.CanHandleRequest)
                .OrderByDescending(a => a.Assessment.CapabilityScore)
                .FirstOrDefault()?.Agent;
            
            if (bestAgent != null)
            {
                selectedAgents.Add(bestAgent);
                _logger.LogDebug("Selected agent {AgentId} for specialization {Specialization}", 
                    bestAgent.AgentId, specialization);
            }
        }
        
        return selectedAgents;
    }
    
    public async Task<AgentWorkflow> CreateWorkflowAsync(
        IEnumerable<ISpecializedAgent> agents, 
        ComplexAgentRequest request)
    {
        return await _workflowPlanner.CreateWorkflowAsync(agents, request);
    }
    
    private async Task<IEnumerable<AgentSpecialization>> AnalyzeRequiredSpecializations(ComplexAgentRequest request)
    {
        var analysisPrompt = $"""
        Analyze this complex code generation request and identify the required agent specializations:
        
        Description: {request.Description}
        Target Platforms: {string.Join(", ", request.TargetPlatforms)}
        Performance Requirements: {request.PerformanceRequirements?.PrimaryTarget}
        Security Requirements: {request.SecurityRequirements?.Level}
        Quality Requirements: {request.QualityRequirements?.MinimumCodeQuality}
        
        Identify which of these specializations are needed:
        - PerformanceOptimization: For performance-critical code
        - SecurityAnalysis: For security-sensitive applications
        - PlatformSpecific: For platform-specific optimizations
        - ArchitecturalDesign: For complex system design
        - TestGeneration: For comprehensive testing
        - DocumentationGeneration: For detailed documentation
        - CodeQuality: For code quality improvements
        - DatabaseDesign: For database-related code
        - NetworkingOptimization: For network-related code
        - UIUXGeneration: For user interface code
        - GameDevelopment: For game-specific code
        - WebDevelopment: For web applications
        - MobileDevelopment: For mobile applications
        - DevOpsIntegration: For deployment and operations
        
        Return only the specializations that are clearly needed, separated by commas.
        """;
        
        var modelRequest = new Models.ModelRequest
        {
            Input = analysisPrompt,
            Temperature = 0.3,
            MaxTokens = 500
        };
        
        var response = await _modelOrchestrator.ExecuteAsync(modelRequest);
        
        if (!response.Success)
        {
            _logger.LogWarning("Failed to analyze specializations, using defaults");
            return new[] { AgentSpecialization.CodeQuality, AgentSpecialization.PlatformSpecific };
        }
        
        return ParseSpecializations(response.Response);
    }
    
    private IEnumerable<AgentSpecialization> ParseSpecializations(string response)
    {
        var specializations = new List<AgentSpecialization>();
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            if (Enum.TryParse<AgentSpecialization>(line.Trim(), true, out var specialization))
            {
                specializations.Add(specialization);
            }
        }
        
        // Default specializations if none were parsed
        if (!specializations.Any())
        {
            specializations.AddRange(new[]
            {
                AgentSpecialization.CodeQuality,
                AgentSpecialization.PlatformSpecific
            });
        }
        
        return specializations;
    }
    
    private async Task<CoordinatedResponse> ExecuteCoordinatedWorkflowAsync(AgentWorkflow workflow)
    {
        var responses = new Dictionary<string, AgentResponse>();
        var executionContext = new ExecutionContext();
        
        foreach (var step in workflow.Steps)
        {
            _logger.LogInformation("Executing workflow step: {StepName} with agent {AgentId}", 
                step.Name, step.AssignedAgent.AgentId);
            
            try
            {
                // Prepare request with context from previous steps
                var contextualRequest = PrepareContextualRequest(step.Request, responses, executionContext);
                
                // Execute step
                var response = step.RequiresCoordination 
                    ? await step.AssignedAgent.CoordinateAsync(contextualRequest, step.Collaborators)
                    : await step.AssignedAgent.ProcessAsync(contextualRequest);
                
                responses[step.Name] = response;
                executionContext.UpdateFromResponse(step.Name, response);
                
                _logger.LogDebug("Step {StepName} completed with confidence {Confidence}", 
                    step.Name, response.Confidence);
                
                // Check for early termination conditions
                if (response.ShouldTerminateWorkflow)
                {
                    _logger.LogWarning("Workflow terminated early at step {StepName}", step.Name);
                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing workflow step {StepName}", step.Name);
                responses[step.Name] = new AgentResponse
                {
                    Success = false,
                    ErrorMessage = $"Step execution failed: {ex.Message}",
                    Confidence = 0.0
                };
            }
        }
        
        return new CoordinatedResponse
        {
            Responses = responses,
            ExecutionContext = executionContext
        };
    }
    
    private AgentRequest PrepareContextualRequest(
        AgentRequest originalRequest, 
        Dictionary<string, AgentResponse> previousResponses,
        ExecutionContext executionContext)
    {
        var contextualInput = originalRequest.Input;
        
        // Add context from previous steps
        if (previousResponses.Any())
        {
            contextualInput += "\n\nPrevious Results:\n";
            foreach (var kvp in previousResponses)
            {
                if (kvp.Value.Success && !string.IsNullOrEmpty(kvp.Value.Result))
                {
                    contextualInput += $"{kvp.Key}: {kvp.Value.Result}\n";
                }
            }
        }
        
        // Add shared results from execution context
        if (executionContext.SharedResults.Any())
        {
            contextualInput += "\n\nShared Context:\n";
            foreach (var kvp in executionContext.SharedResults)
            {
                contextualInput += $"{kvp.Key}: {kvp.Value}\n";
            }
        }
        
        return originalRequest with { Input = contextualInput };
    }
    
    private async Task<AgentResponse> SynthesizeFinalResponse(CoordinatedResponse results, ComplexAgentRequest request)
    {
        var synthesisPrompt = $"""
        Synthesize the following coordinated agent responses into a final, cohesive result:
        
        Original Request: {request.Description}
        Target Platforms: {string.Join(", ", request.TargetPlatforms)}
        
        Agent Responses:
        """;
        
        foreach (var kvp in results.Responses)
        {
            if (kvp.Value.Success)
            {
                synthesisPrompt += $"\n{kvp.Key}:\n{kvp.Value.Result}\n";
            }
        }
        
        synthesisPrompt += """
        
        Create a final, integrated solution that:
        1. Combines the best aspects of each agent's contribution
        2. Ensures consistency across all components
        3. Addresses all requirements from the original request
        4. Maintains high code quality and performance
        5. Includes proper error handling and validation
        
        Provide the complete, production-ready solution.
        """;
        
        var modelRequest = new Models.ModelRequest
        {
            Input = synthesisPrompt,
            Temperature = 0.4,
            MaxTokens = 2000
        };
        
        var response = await _modelOrchestrator.ExecuteAsync(modelRequest);
        
        if (!response.Success)
        {
            _logger.LogError("Failed to synthesize final response");
            return new AgentResponse
            {
                Success = false,
                ErrorMessage = "Failed to synthesize coordinated results",
                Confidence = 0.0
            };
        }
        
        // Calculate overall confidence based on individual agent responses
        var averageConfidence = results.Responses.Values
            .Where(r => r.Success)
            .Average(r => r.Confidence);
        
        return new AgentResponse
        {
            Result = response.Response,
            Success = true,
            Confidence = Math.Min(averageConfidence, 0.95), // Cap at 95% for coordinated responses
            Metadata = new Dictionary<string, object>
            {
                ["CoordinatedResponses"] = results.Responses,
                ["WorkflowSteps"] = results.Responses.Keys.ToArray(),
                ["SynthesisMethod"] = "AI-Coordinated",
                ["AgentCount"] = results.Responses.Count
            }
        };
    }
}
