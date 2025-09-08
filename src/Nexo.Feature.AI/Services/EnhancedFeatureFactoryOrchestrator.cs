using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Agents.Coordination;
using Nexo.Feature.AI.Agents.Specialized;
using Nexo.Feature.AI.Interfaces;

namespace Nexo.Feature.AI.Services;

/// <summary>
/// Enhanced Feature Factory orchestrator that uses specialized AI agents for complex feature generation
/// </summary>
public class EnhancedFeatureFactoryOrchestrator : IEnhancedFeatureFactoryOrchestrator
{
    private readonly IAgentCoordinator _agentCoordinator;
    private readonly ISpecializedAgentRegistry _agentRegistry;
    private readonly IModelOrchestrator _modelOrchestrator;
    private readonly ILogger<EnhancedFeatureFactoryOrchestrator> _logger;
    
    public EnhancedFeatureFactoryOrchestrator(
        IAgentCoordinator agentCoordinator,
        ISpecializedAgentRegistry agentRegistry,
        IModelOrchestrator modelOrchestrator,
        ILogger<EnhancedFeatureFactoryOrchestrator> logger)
    {
        _agentCoordinator = agentCoordinator ?? throw new ArgumentNullException(nameof(agentCoordinator));
        _agentRegistry = agentRegistry ?? throw new ArgumentNullException(nameof(agentRegistry));
        _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<FeatureGenerationResponse> GenerateFeatureAsync(FeatureGenerationRequest request)
    {
        try
        {
            _logger.LogInformation("Starting enhanced feature generation for: {Description}", request.Description);
            
            // Analyze request complexity to determine if specialized coordination is needed
            var complexityAnalysis = await AnalyzeRequestComplexity(request);
            
            if (complexityAnalysis.RequiresSpecializedCoordination)
            {
                _logger.LogInformation("Using specialized agent coordination for complex request");
                return await GenerateWithSpecializedCoordination(request, complexityAnalysis);
            }
            else
            {
                _logger.LogInformation("Using standard feature generation for simple request");
                return await GenerateStandardFeature(request);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during enhanced feature generation");
            return new FeatureGenerationResponse
            {
                Success = false,
                ErrorMessage = $"Feature generation failed: {ex.Message}",
                GeneratedCode = string.Empty
            };
        }
    }
    
    public async Task<FeatureGenerationResponse> GenerateWithSpecializedCoordination(
        FeatureGenerationRequest request, 
        RequestComplexityAnalysis complexityAnalysis)
    {
        try
        {
            // Create complex agent request
            var complexRequest = new ComplexAgentRequest
            {
                Description = request.Description,
                TargetPlatforms = request.TargetPlatforms,
                PerformanceRequirements = request.PerformanceRequirements,
                SecurityRequirements = request.SecurityRequirements,
                QualityRequirements = request.QualityRequirements,
                Context = new Dictionary<string, object>
                {
                    ["ComplexityLevel"] = complexityAnalysis.ComplexityLevel,
                    ["RequiredSpecializations"] = complexityAnalysis.RequiredSpecializations,
                    ["EstimatedEffort"] = complexityAnalysis.EstimatedEffort
                }
            };
            
            // Use specialized agent coordination
            var coordinatedResponse = await _agentCoordinator.CoordinateComplexTaskAsync(complexRequest);
            
            if (!coordinatedResponse.Success)
            {
                return new FeatureGenerationResponse
                {
                    Success = false,
                    ErrorMessage = coordinatedResponse.ErrorMessage ?? "Specialized coordination failed",
                    GeneratedCode = string.Empty
                };
            }
            
            // Extract quality metrics from coordinated response
            var qualityMetrics = ExtractQualityMetrics(coordinatedResponse);
            var performanceOptimizations = ExtractPerformanceOptimizations(coordinatedResponse);
            var securityValidation = ExtractSecurityValidation(coordinatedResponse);
            
            return new FeatureGenerationResponse
            {
                Success = true,
                GeneratedCode = coordinatedResponse.Result,
                QualityMetrics = qualityMetrics,
                PerformanceOptimizations = performanceOptimizations,
                SecurityValidation = securityValidation,
                CrossPlatformCompatibility = ExtractPlatformCompatibility(coordinatedResponse),
                CoordinationDetails = new CoordinationDetails
                {
                    UsedSpecializedCoordination = true,
                    AgentCount = GetAgentCountFromMetadata(coordinatedResponse),
                    CoordinationStrategy = GetCoordinationStrategyFromMetadata(coordinatedResponse),
                    ExecutionTime = GetExecutionTimeFromMetadata(coordinatedResponse)
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in specialized coordination");
            return new FeatureGenerationResponse
            {
                Success = false,
                ErrorMessage = $"Specialized coordination failed: {ex.Message}",
                GeneratedCode = string.Empty
            };
        }
    }
    
    public async Task<FeatureGenerationResponse> GenerateStandardFeature(FeatureGenerationRequest request)
    {
        try
        {
            _logger.LogInformation("Generating standard feature for: {Description}", request.Description);
            
            // Use standard AI generation without specialized coordination
            var modelRequest = new Models.ModelRequest
            {
                Input = BuildStandardFeaturePrompt(request),
                Temperature = 0.7,
                MaxTokens = 2000
            };
            
            var response = await _modelOrchestrator.ExecuteAsync(modelRequest);
            
            if (!response.Success)
            {
                return new FeatureGenerationResponse
                {
                    Success = false,
                    ErrorMessage = response.ErrorMessage ?? "Standard generation failed",
                    GeneratedCode = string.Empty
                };
            }
            
            return new FeatureGenerationResponse
            {
                Success = true,
                GeneratedCode = response.Response,
                QualityMetrics = new QualityMetrics
                {
                    CodeQuality = 0.8,
                    TestCoverage = 0.7,
                    DocumentationQuality = 0.6,
                    MaintainabilityScore = 0.8
                },
                CoordinationDetails = new CoordinationDetails
                {
                    UsedSpecializedCoordination = false,
                    AgentCount = 1,
                    CoordinationStrategy = "Standard",
                    ExecutionTime = TimeSpan.FromMilliseconds(response.ProcessingTimeMs)
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in standard feature generation");
            return new FeatureGenerationResponse
            {
                Success = false,
                ErrorMessage = $"Standard generation failed: {ex.Message}",
                GeneratedCode = string.Empty
            };
        }
    }
    
    public async Task<RequestComplexityAnalysis> AnalyzeRequestComplexity(FeatureGenerationRequest request)
    {
        try
        {
            var analysisPrompt = $"""
            Analyze the complexity of this feature generation request:
            
            Description: {request.Description}
            Target Platforms: {string.Join(", ", request.TargetPlatforms)}
            Performance Requirements: {request.PerformanceRequirements?.PrimaryTarget}
            Security Requirements: {request.SecurityRequirements?.Level}
            Quality Requirements: {request.QualityRequirements?.MinimumCodeQuality}
            
            Determine:
            1. Complexity level (Low, Medium, High, Very High)
            2. Required specializations (Performance, Security, Platform-specific, etc.)
            3. Estimated effort (1-10 scale)
            4. Whether specialized agent coordination is needed
            
            Consider factors like:
            - Number of target platforms
            - Performance requirements
            - Security requirements
            - Code complexity
            - Integration requirements
            - Testing requirements
            
            Provide a detailed complexity analysis.
            """;
            
            var modelRequest = new Models.ModelRequest
            {
                Input = analysisPrompt,
                Temperature = 0.3,
                MaxTokens = 1000
            };
            
            var response = await _modelOrchestrator.ExecuteAsync(modelRequest);
            
            if (!response.Success)
            {
                _logger.LogWarning("Failed to analyze request complexity, using default analysis");
                return CreateDefaultComplexityAnalysis(request);
            }
            
            return ParseComplexityAnalysis(response.Response, request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing request complexity");
            return CreateDefaultComplexityAnalysis(request);
        }
    }
    
    private string BuildStandardFeaturePrompt(FeatureGenerationRequest request)
    {
        return $"""
        Generate a complete, production-ready feature based on this description:
        
        {request.Description}
        
        Requirements:
        - Target Platforms: {string.Join(", ", request.TargetPlatforms)}
        - Performance: {request.PerformanceRequirements?.PrimaryTarget}
        - Security: {request.SecurityRequirements?.Level}
        - Quality: {request.QualityRequirements?.MinimumCodeQuality}
        
        Include:
        1. Complete implementation code
        2. Unit tests
        3. Documentation
        4. Error handling
        5. Performance optimizations
        6. Security best practices
        
        Generate production-ready code following Clean Architecture principles.
        """;
    }
    
    private RequestComplexityAnalysis ParseComplexityAnalysis(string response, FeatureGenerationRequest request)
    {
        var complexityLevel = ComplexityLevel.Medium;
        var requiredSpecializations = new List<AgentSpecialization>();
        var estimatedEffort = 5;
        var requiresSpecializedCoordination = false;
        
        // Simple parsing - in a real implementation, this would be more sophisticated
        var lowerResponse = response.ToLowerInvariant();
        
        if (lowerResponse.Contains("high") || lowerResponse.Contains("complex"))
        {
            complexityLevel = ComplexityLevel.High;
            requiresSpecializedCoordination = true;
            estimatedEffort = 8;
        }
        else if (lowerResponse.Contains("very high") || lowerResponse.Contains("very complex"))
        {
            complexityLevel = ComplexityLevel.VeryHigh;
            requiresSpecializedCoordination = true;
            estimatedEffort = 10;
        }
        else if (lowerResponse.Contains("low") || lowerResponse.Contains("simple"))
        {
            complexityLevel = ComplexityLevel.Low;
            estimatedEffort = 3;
        }
        
        // Determine required specializations based on request
        if (request.PerformanceRequirements?.PrimaryTarget == OptimizationTarget.Performance)
        {
            requiredSpecializations.Add(AgentSpecialization.PerformanceOptimization);
        }
        
        if (request.SecurityRequirements?.Level == SecurityLevel.High || 
            request.SecurityRequirements?.Level == SecurityLevel.Critical)
        {
            requiredSpecializations.Add(AgentSpecialization.SecurityAnalysis);
        }
        
        if (request.TargetPlatforms.Count() > 1)
        {
            requiredSpecializations.Add(AgentSpecialization.PlatformSpecific);
        }
        
        if (request.QualityRequirements?.RequiresTests == true)
        {
            requiredSpecializations.Add(AgentSpecialization.TestGeneration);
        }
        
        if (request.QualityRequirements?.RequiresDocumentation == true)
        {
            requiredSpecializations.Add(AgentSpecialization.DocumentationGeneration);
        }
        
        return new RequestComplexityAnalysis
        {
            ComplexityLevel = complexityLevel,
            RequiredSpecializations = requiredSpecializations,
            EstimatedEffort = estimatedEffort,
            RequiresSpecializedCoordination = requiresSpecializedCoordination
        };
    }
    
    private RequestComplexityAnalysis CreateDefaultComplexityAnalysis(FeatureGenerationRequest request)
    {
        var requiredSpecializations = new List<AgentSpecialization>();
        
        // Add specializations based on request requirements
        if (request.PerformanceRequirements != null)
        {
            requiredSpecializations.Add(AgentSpecialization.PerformanceOptimization);
        }
        
        if (request.SecurityRequirements != null)
        {
            requiredSpecializations.Add(AgentSpecialization.SecurityAnalysis);
        }
        
        if (request.TargetPlatforms.Count() > 1)
        {
            requiredSpecializations.Add(AgentSpecialization.PlatformSpecific);
        }
        
        var requiresSpecializedCoordination = requiredSpecializations.Count > 1 || 
                                            request.TargetPlatforms.Count() > 2 ||
                                            request.QualityRequirements?.MinimumCodeQuality > 0.8;
        
        return new RequestComplexityAnalysis
        {
            ComplexityLevel = requiresSpecializedCoordination ? ComplexityLevel.High : ComplexityLevel.Medium,
            RequiredSpecializations = requiredSpecializations,
            EstimatedEffort = requiresSpecializedCoordination ? 7 : 5,
            RequiresSpecializedCoordination = requiresSpecializedCoordination
        };
    }
    
    private QualityMetrics ExtractQualityMetrics(AgentResponse response)
    {
        var metadata = response.Metadata ?? new Dictionary<string, object>();
        
        return new QualityMetrics
        {
            CodeQuality = GetDoubleFromMetadata(metadata, "CodeQuality", 0.8),
            TestCoverage = GetDoubleFromMetadata(metadata, "TestCoverage", 0.7),
            DocumentationQuality = GetDoubleFromMetadata(metadata, "DocumentationQuality", 0.6),
            MaintainabilityScore = GetDoubleFromMetadata(metadata, "MaintainabilityScore", 0.8)
        };
    }
    
    private IEnumerable<PerformanceOptimization> ExtractPerformanceOptimizations(AgentResponse response)
    {
        var metadata = response.Metadata ?? new Dictionary<string, object>();
        
        if (metadata.TryGetValue("PerformanceOptimizations", out var optimizations) && 
            optimizations is IEnumerable<PerformanceOptimization> perfOpts)
        {
            return perfOpts;
        }
        
        return Enumerable.Empty<PerformanceOptimization>();
    }
    
    private SecurityValidation ExtractSecurityValidation(AgentResponse response)
    {
        var metadata = response.Metadata ?? new Dictionary<string, object>();
        
        if (metadata.TryGetValue("SecurityValidation", out var security) && 
            security is SecurityValidation secValidation)
        {
            return secValidation;
        }
        
        return new SecurityValidation
        {
            IsSecure = true,
            AchievedLevel = SecurityLevel.Standard,
            Vulnerabilities = Array.Empty<string>(),
            SecurityImprovements = Array.Empty<string>(),
            ComplianceStatus = "Standard"
        };
    }
    
    private PlatformCompatibility ExtractPlatformCompatibility(AgentResponse response)
    {
        var metadata = response.Metadata ?? new Dictionary<string, object>();
        
        if (metadata.TryGetValue("PlatformCompatibility", out var compatibility) && 
            compatibility is PlatformCompatibility platformCompat)
        {
            return platformCompat;
        }
        
        return PlatformCompatibility.All;
    }
    
    private int GetAgentCountFromMetadata(AgentResponse response)
    {
        var metadata = response.Metadata ?? new Dictionary<string, object>();
        return GetIntFromMetadata(metadata, "AgentCount", 1);
    }
    
    private string GetCoordinationStrategyFromMetadata(AgentResponse response)
    {
        var metadata = response.Metadata ?? new Dictionary<string, object>();
        return GetStringFromMetadata(metadata, "CoordinationStrategy", "Standard");
    }
    
    private TimeSpan GetExecutionTimeFromMetadata(AgentResponse response)
    {
        var metadata = response.Metadata ?? new Dictionary<string, object>();
        var executionTimeMs = GetIntFromMetadata(metadata, "ExecutionTimeMs", 1000);
        return TimeSpan.FromMilliseconds(executionTimeMs);
    }
    
    private double GetDoubleFromMetadata(Dictionary<string, object> metadata, string key, double defaultValue)
    {
        return metadata.TryGetValue(key, out var value) && value is double d ? d : defaultValue;
    }
    
    private int GetIntFromMetadata(Dictionary<string, object> metadata, string key, int defaultValue)
    {
        return metadata.TryGetValue(key, out var value) && value is int i ? i : defaultValue;
    }
    
    private string GetStringFromMetadata(Dictionary<string, object> metadata, string key, string defaultValue)
    {
        return metadata.TryGetValue(key, out var value) && value is string s ? s : defaultValue;
    }
}

/// <summary>
/// Interface for enhanced Feature Factory orchestrator
/// </summary>
public interface IEnhancedFeatureFactoryOrchestrator
{
    Task<FeatureGenerationResponse> GenerateFeatureAsync(FeatureGenerationRequest request);
    Task<FeatureGenerationResponse> GenerateWithSpecializedCoordination(FeatureGenerationRequest request, RequestComplexityAnalysis complexityAnalysis);
    Task<FeatureGenerationResponse> GenerateStandardFeature(FeatureGenerationRequest request);
    Task<RequestComplexityAnalysis> AnalyzeRequestComplexity(FeatureGenerationRequest request);
}

/// <summary>
/// Feature generation request
/// </summary>
public record FeatureGenerationRequest
{
    public string Description { get; init; } = string.Empty;
    public IEnumerable<PlatformCompatibility> TargetPlatforms { get; init; } = [];
    public PerformanceProfile? PerformanceRequirements { get; init; }
    public SecurityRequirements? SecurityRequirements { get; init; }
    public QualityRequirements? QualityRequirements { get; init; }
    public Dictionary<string, object>? Context { get; init; }
}

/// <summary>
/// Feature generation response
/// </summary>
public record FeatureGenerationResponse
{
    public bool Success { get; init; }
    public string GeneratedCode { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
    public QualityMetrics? QualityMetrics { get; init; }
    public IEnumerable<PerformanceOptimization> PerformanceOptimizations { get; init; } = [];
    public SecurityValidation SecurityValidation { get; init; } = new();
    public PlatformCompatibility CrossPlatformCompatibility { get; init; } = PlatformCompatibility.None;
    public CoordinationDetails? CoordinationDetails { get; init; }
}

/// <summary>
/// Request complexity analysis
/// </summary>
public record RequestComplexityAnalysis
{
    public ComplexityLevel ComplexityLevel { get; init; }
    public IEnumerable<AgentSpecialization> RequiredSpecializations { get; init; } = [];
    public int EstimatedEffort { get; init; }
    public bool RequiresSpecializedCoordination { get; init; }
}

/// <summary>
/// Complexity levels
/// </summary>
public enum ComplexityLevel
{
    Low,
    Medium,
    High,
    VeryHigh
}

/// <summary>
/// Quality metrics
/// </summary>
public record QualityMetrics
{
    public double CodeQuality { get; init; }
    public double TestCoverage { get; init; }
    public double DocumentationQuality { get; init; }
    public double MaintainabilityScore { get; init; }
}

/// <summary>
/// Coordination details
/// </summary>
public record CoordinationDetails
{
    public bool UsedSpecializedCoordination { get; init; }
    public int AgentCount { get; init; }
    public string CoordinationStrategy { get; init; } = string.Empty;
    public TimeSpan ExecutionTime { get; init; }
}

/// <summary>
/// Interface for specialized agent registry
/// </summary>
public interface ISpecializedAgentRegistry
{
    IEnumerable<ISpecializedAgent> GetAllAgents();
    IEnumerable<ISpecializedAgent> GetAgentsBySpecialization(AgentSpecialization specialization);
    ISpecializedAgent? GetAgentById(string agentId);
}
