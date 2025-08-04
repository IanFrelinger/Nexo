using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.Factory.Interfaces;

/// <summary>
/// Main interface for the Feature Factory pipeline that orchestrates all stages from natural language to production-ready code
/// </summary>
public interface IFeatureFactoryPipeline
{
    /// <summary>
    /// Processes a complete feature request from natural language to production-ready implementation
    /// </summary>
    /// <param name="request">Feature generation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Complete feature generation result</returns>
    Task<FeatureGenerationResult> GenerateFeatureAsync(FeatureGenerationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status of a feature generation process
    /// </summary>
    /// <param name="generationId">Generation identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Feature generation status</returns>
    Task<FeatureGenerationStatus> GetGenerationStatusAsync(string generationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels an ongoing feature generation process
    /// </summary>
    /// <param name="generationId">Generation identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cancellation result</returns>
    Task<FeatureCancellationResult> CancelGenerationAsync(string generationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pipeline performance metrics
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Pipeline performance metrics</returns>
    Task<PipelinePerformanceMetrics> GetPerformanceMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pipeline configuration
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Pipeline configuration</returns>
    Task<FeatureFactoryConfiguration> GetConfigurationAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Feature generation request
/// </summary>
public record FeatureGenerationRequest
{
    public string NaturalLanguageDescription { get; init; } = string.Empty;
    public string Domain { get; init; } = string.Empty;
    public string Industry { get; init; } = string.Empty;
    public List<string> TargetPlatforms { get; init; } = new();
    public FeaturePriority Priority { get; init; }
    public Dictionary<string, object> Context { get; init; } = new();
    public List<string> Constraints { get; init; } = new();
    public string? UserId { get; init; }
    public string? ProjectId { get; init; }
}

/// <summary>
/// Feature priority levels
/// </summary>
public enum FeaturePriority
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Feature generation result
/// </summary>
public record FeatureGenerationResult
{
    public string GenerationId { get; init; } = string.Empty;
    public bool IsSuccessful { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public TimeSpan TotalDuration { get; init; }
    public FeatureSpecification Specification { get; init; } = new();
    public DomainLogicResult DomainLogic { get; init; } = new();
    public ApplicationLogicResult ApplicationLogic { get; init; } = new();
    public PlatformImplementationResult PlatformImplementations { get; init; } = new();
    public List<string> Errors { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Feature generation status
/// </summary>
public record FeatureGenerationStatus
{
    public string GenerationId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int OverallProgress { get; init; }
    public List<PipelineStageStatus> StageStatuses { get; init; } = new();
    public DateTime LastUpdated { get; init; }
    public TimeSpan ElapsedTime { get; init; }
    public TimeSpan? EstimatedRemainingTime { get; init; }
    public string? CurrentStage { get; init; }
    public string? CurrentOperation { get; init; }
}

/// <summary>
/// Pipeline stage status
/// </summary>
public record PipelineStageStatus
{
    public string StageName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int Progress { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public TimeSpan Duration { get; init; }
    public string? ErrorMessage { get; init; }
    public Dictionary<string, object> StageData { get; init; } = new();
}

/// <summary>
/// Feature cancellation result
/// </summary>
public record FeatureCancellationResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public string GenerationId { get; init; } = string.Empty;
    public DateTime CancelledAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Pipeline performance metrics
/// </ summary>
public record PipelinePerformanceMetrics
{
    public DateTime GeneratedAt { get; init; }
    public TimeSpan AverageGenerationTime { get; init; }
    public int TotalFeaturesGenerated { get; init; }
    public int SuccessfulGenerations { get; init; }
    public int FailedGenerations { get; init; }
    public double SuccessRate { get; init; }
    public Dictionary<string, TimeSpan> StageAverageTimes { get; init; } = new();
    public Dictionary<string, int> StageSuccessRates { get; init; } = new();
    public List<PerformanceTrend> Trends { get; init; } = new();
    public ProductivityMetrics Productivity { get; init; } = new();
}

/// <summary>
/// Performance trend data
/// </summary>
public record PerformanceTrend
{
    public DateTime Date { get; init; }
    public TimeSpan AverageTime { get; init; }
    public int FeatureCount { get; init; }
    public double SuccessRate { get; init; }
}

/// <summary>
/// Productivity metrics
/// </summary>
public record ProductivityMetrics
{
    public double ProductivityMultiplier { get; init; }
    public TimeSpan TraditionalDevelopmentTime { get; init; }
    public TimeSpan FeatureFactoryTime { get; init; }
    public double TimeSavingsPercentage { get; init; }
    public double CostSavingsPercentage { get; init; }
    public int FeaturesPerDay { get; init; }
    public int FeaturesPerWeek { get; init; }
    public int FeaturesPerMonth { get; init; }
}

/// <summary>
/// Feature Factory configuration
/// </summary>
public record FeatureFactoryConfiguration
{
    public bool EnableAllStages { get; init; }
    public List<string> EnabledStages { get; init; } = new();
    public Dictionary<string, StageConfiguration> StageConfigurations { get; init; } = new();
    public PerformanceConfiguration Performance { get; init; } = new();
    public SecurityConfiguration Security { get; init; } = new();
    public QualityConfiguration Quality { get; init; } = new();
    public Dictionary<string, object> CustomSettings { get; init; } = new();
}

/// <summary>
/// Stage configuration
/// </summary>
public record StageConfiguration
{
    public bool IsEnabled { get; init; }
    public int MaxRetries { get; init; }
    public TimeSpan Timeout { get; init; }
    public bool EnableParallelProcessing { get; init; }
    public Dictionary<string, object> StageSpecificSettings { get; init; } = new();
}

/// <summary>
/// Performance configuration
/// </summary>
public record PerformanceConfiguration
{
    public int MaxConcurrentGenerations { get; init; }
    public TimeSpan DefaultTimeout { get; init; }
    public bool EnableCaching { get; init; }
    public TimeSpan CacheTimeout { get; init; }
    public bool EnableCompression { get; init; }
}

/// <summary>
/// Security configuration
/// </summary>
public record SecurityConfiguration
{
    public bool EnableAuditLogging { get; init; }
    public bool EnableInputValidation { get; init; }
    public bool EnableOutputSanitization { get; init; }
    public List<string> AllowedDomains { get; init; } = new();
    public List<string> RestrictedKeywords { get; init; } = new();
}

/// <summary>
/// Quality configuration
/// </summary>
public record QualityConfiguration
{
    public bool EnableAutomatedTesting { get; init; }
    public bool EnableCodeReview { get; init; }
    public bool EnableSecurityScanning { get; init; }
    public double MinimumConfidenceScore { get; init; }
    public bool RequireManualApproval { get; init; }
}

/// <summary>
/// Feature specification (Stage 1 output)
/// </summary>
public record FeatureSpecification
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<Requirement> Requirements { get; init; } = new();
    public List<UserStory> UserStories { get; init; } = new();
    public List<AcceptanceCriteria> AcceptanceCriteria { get; init; } = new();
    public List<BusinessRule> BusinessRules { get; init; } = new();
    public double ConfidenceScore { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Domain logic result (Stage 2 output)
/// </summary>
public record DomainLogicResult
{
    public List<DomainEntity> Entities { get; init; } = new();
    public List<ValueObject> ValueObjects { get; init; } = new();
    public List<DomainService> Services { get; init; } = new();
    public List<BusinessRule> BusinessRules { get; init; } = new();
    public List<DomainEvent> Events { get; init; } = new();
    public List<TestSuite> TestSuites { get; init; } = new();
    public double ConfidenceScore { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Application logic result (Stage 3 output)
/// </summary>
public record ApplicationLogicResult
{
    public List<UseCase> UseCases { get; init; } = new();
    public List<ApplicationService> Services { get; init; } = new();
    public List<Interface> Interfaces { get; init; } = new();
    public List<DataTransferObject> DTOs { get; init; } = new();
    public List<ValidationRule> ValidationRules { get; init; } = new();
    public double ConfidenceScore { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Platform implementation result (Stage 4 output)
/// </summary>
public record PlatformImplementationResult
{
    public Dictionary<string, PlatformCode> PlatformImplementations { get; init; } = new();
    public List<IntegrationPoint> IntegrationPoints { get; init; } = new();
    public List<DeploymentConfiguration> DeploymentConfigs { get; init; } = new();
    public List<TestSuite> PlatformTests { get; init; } = new();
    public double ConfidenceScore { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}

// Supporting data models (simplified for brevity)
public record Requirement { public string Id { get; init; } = string.Empty; public string Description { get; init; } = string.Empty; }
public record UserStory { public string Id { get; init; } = string.Empty; public string Description { get; init; } = string.Empty; }
public record AcceptanceCriteria { public string Id { get; init; } = string.Empty; public string Description { get; init; } = string.Empty; }
public record BusinessRule { public string Id { get; init; } = string.Empty; public string Description { get; init; } = string.Empty; }
public record DomainEntity { public string Name { get; init; } = string.Empty; public List<string> Properties { get; init; } = new(); }
public record ValueObject { public string Name { get; init; } = string.Empty; public List<string> Properties { get; init; } = new(); }
public record DomainService { public string Name { get; init; } = string.Empty; public string Description { get; init; } = string.Empty; }
public record DomainEvent { public string Name { get; init; } = string.Empty; public string Description { get; init; } = string.Empty; }
public record TestSuite { public string Name { get; init; } = string.Empty; public List<string> Tests { get; init; } = new(); }
public record UseCase { public string Name { get; init; } = string.Empty; public string Description { get; init; } = string.Empty; }
public record ApplicationService { public string Name { get; init; } = string.Empty; public string Description { get; init; } = string.Empty; }
public record Interface { public string Name { get; init; } = string.Empty; public string Description { get; init; } = string.Empty; }
public record DataTransferObject { public string Name { get; init; } = string.Empty; public List<string> Properties { get; init; } = new(); }
public record ValidationRule { public string Name { get; init; } = string.Empty; public string Description { get; init; } = string.Empty; }
public record PlatformCode { public string Platform { get; init; } = string.Empty; public string Code { get; init; } = string.Empty; }
public record IntegrationPoint { public string Name { get; init; } = string.Empty; public string Description { get; init; } = string.Empty; }
public record DeploymentConfiguration { public string Name { get; init; } = string.Empty; public string Configuration { get; init; } = string.Empty; } 