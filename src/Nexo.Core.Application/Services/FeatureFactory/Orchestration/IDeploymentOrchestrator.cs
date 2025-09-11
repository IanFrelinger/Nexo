using Nexo.Core.Domain.Entities.FeatureFactory.NaturalLanguage;
using Nexo.Core.Domain.Entities.FeatureFactory.ApplicationLogic;
using Nexo.Core.Domain.Entities.FeatureFactory.Deployment;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.FeatureFactory.Orchestration
{
    /// <summary>
    /// Interface for orchestrating complete deployment workflows
    /// </summary>
    public interface IDeploymentOrchestrator
    {
        /// <summary>
        /// Orchestrates complete deployment from natural language requirements
        /// </summary>
        Task<DeploymentOrchestrationResult> OrchestrateCompleteDeploymentAsync(NaturalLanguageResult requirements, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deploys application to multiple environments
        /// </summary>
        Task<DeploymentOrchestrationResult> DeployToMultipleEnvironmentsAsync(ApplicationLogicResult applicationLogic, List<DeploymentTarget> targets, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates deployment before going live
        /// </summary>
        Task<DeploymentOrchestrationResult> ValidateDeploymentAsync(ApplicationLogicResult applicationLogic, DeploymentTarget target, CancellationToken cancellationToken = default);

        /// <summary>
        /// Completes the entire Feature Factory pipeline
        /// </summary>
        Task<DeploymentOrchestrationResult> CompleteFeatureFactoryPipelineAsync(NaturalLanguageResult requirements, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets orchestration status
        /// </summary>
        Task<OrchestrationStatus> GetOrchestrationStatusAsync(Guid sessionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancels an orchestration session
        /// </summary>
        Task<CancellationResult> CancelOrchestrationAsync(Guid sessionId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result of deployment orchestration
    /// </summary>
    public class DeploymentOrchestrationResult
    {
        public Guid SessionId { get; set; } = Guid.NewGuid();
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public OrchestrationStatus Status { get; set; } = new();
        public NaturalLanguageResult NaturalLanguageResult { get; set; } = new();
        public DomainLogicResult DomainLogicResult { get; set; } = new();
        public ApplicationLogicResult ApplicationLogicResult { get; set; } = new();
        public List<DeploymentResult> DeploymentResults { get; set; } = new();
        public List<IntegrationResult> IntegrationResults { get; set; } = new();
        public List<MonitoringResult> MonitoringResults { get; set; } = new();
        public PipelineMetrics Metrics { get; set; } = new();
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Orchestration status information
    /// </summary>
    public class OrchestrationStatus
    {
        public Guid SessionId { get; set; } = Guid.NewGuid();
        public OrchestrationState State { get; set; } = OrchestrationState.Pending;
        public string Message { get; set; } = string.Empty;
        public int Progress { get; set; }
        public List<OrchestrationStep> Steps { get; set; } = new();
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Orchestration step information
    /// </summary>
    public class OrchestrationStep
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public StepStatus Status { get; set; } = StepStatus.Pending;
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Pipeline metrics
    /// </summary>
    public class PipelineMetrics
    {
        public TimeSpan TotalDuration { get; set; }
        public TimeSpan NaturalLanguageProcessingDuration { get; set; }
        public TimeSpan DomainLogicGenerationDuration { get; set; }
        public TimeSpan ApplicationLogicGenerationDuration { get; set; }
        public TimeSpan DeploymentDuration { get; set; }
        public TimeSpan IntegrationDuration { get; set; }
        public TimeSpan MonitoringSetupDuration { get; set; }
        public int TotalSteps { get; set; }
        public int CompletedSteps { get; set; }
        public int FailedSteps { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of cancellation operations
    /// </summary>
    public class CancellationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public Guid SessionId { get; set; }
        public OrchestrationState FinalState { get; set; } = OrchestrationState.Cancelled;
        public DateTime CancelledAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    // Supporting classes for orchestration

    /// <summary>
    /// Natural language result
    /// </summary>
    public class NaturalLanguageResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<UserStory> UserStories { get; set; } = new();
        public List<BusinessTerm> BusinessTerms { get; set; } = new();
        public List<TechnicalRequirement> TechnicalRequirements { get; set; } = new();
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Domain logic result
    /// </summary>
    public class DomainLogicResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<DomainEntity> Entities { get; set; } = new();
        public List<ValueObject> ValueObjects { get; set; } = new();
        public List<BusinessRule> BusinessRules { get; set; } = new();
        public List<DomainService> DomainServices { get; set; } = new();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Deployment result
    /// </summary>
    public class DeploymentResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string DeploymentId { get; set; } = string.Empty;
        public DeploymentTarget Target { get; set; } = new();
        public DeploymentPackage Package { get; set; } = new();
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Integration result
    /// </summary>
    public class IntegrationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string IntegrationId { get; set; } = string.Empty;
        public IntegrationType Type { get; set; } = IntegrationType.API;
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Monitoring result
    /// </summary>
    public class MonitoringResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string MonitoringId { get; set; } = string.Empty;
        public MonitoringType Type { get; set; } = MonitoringType.Health;
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    // Supporting model classes

    public class UserStory
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Description { get; set; } = string.Empty;
        public string AcceptanceCriteria { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class BusinessTerm
    {
        public string Name { get; set; } = string.Empty;
        public string Definition { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class TechnicalRequirement
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class DomainEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<EntityProperty> Properties { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class EntityProperty
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class ValueObject
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<ValueObjectProperty> Properties { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class ValueObjectProperty
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class BusinessRule
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Expression { get; set; } = string.Empty;
        public BusinessRuleType Type { get; set; } = BusinessRuleType.Validation;
        public BusinessRulePriority Priority { get; set; } = BusinessRulePriority.Medium;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class DomainService
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<ServiceMethod> Methods { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class ServiceMethod
    {
        public string Name { get; set; } = string.Empty;
        public string ReturnType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<MethodParameter> Parameters { get; set; } = new();
        public bool IsAsync { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class MethodParameter
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    // Enums for orchestration

    /// <summary>
    /// Orchestration states
    /// </summary>
    public enum OrchestrationState
    {
        Pending,
        InProgress,
        Completed,
        Failed,
        Cancelled,
        Paused
    }

    /// <summary>
    /// Step status
    /// </summary>
    public enum StepStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed,
        Skipped
    }

    /// <summary>
    /// Business rule types
    /// </summary>
    public enum BusinessRuleType
    {
        Validation,
        Calculation,
        Authorization,
        Workflow,
        Constraint
    }

    /// <summary>
    /// Business rule priorities
    /// </summary>
    public enum BusinessRulePriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Integration types
    /// </summary>
    public enum IntegrationType
    {
        API,
        Database,
        MessageQueue,
        Enterprise,
        RealTimeSync
    }

    /// <summary>
    /// Monitoring types
    /// </summary>
    public enum MonitoringType
    {
        Health,
        Performance,
        Alerting,
        Logging,
        Dashboard
    }
}
