using Nexo.Core.Domain.Entities.FeatureFactory.DomainLogic;
using Nexo.Core.Domain.Results;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.FeatureFactory.Orchestration
{
    /// <summary>
    /// Interface for orchestrating complete domain logic generation workflow
    /// </summary>
    public interface IDomainLogicOrchestrator
    {
        /// <summary>
        /// Generates complete domain logic from validated requirements
        /// </summary>
        Task<DomainLogicGenerationResult> GenerateCompleteDomainLogicAsync(ValidatedRequirements requirements, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current generation progress
        /// </summary>
        Task<GenerationProgress> GetGenerationProgressAsync(string sessionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the generation report for a session
        /// </summary>
        Task<GenerationReport> GetGenerationReportAsync(string sessionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancels an ongoing generation process
        /// </summary>
        Task<bool> CancelGenerationAsync(string sessionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates generated domain logic
        /// </summary>
        Task<DomainLogicValidationResult> ValidateGeneratedDomainLogicAsync(string sessionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Optimizes generated domain logic
        /// </summary>
        Task<DomainLogicOptimizationResult> OptimizeGeneratedDomainLogicAsync(string sessionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates test suite for domain logic
        /// </summary>
        Task<TestSuiteGenerationResult> GenerateTestSuiteAsync(string sessionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all active generation sessions
        /// </summary>
        Task<List<GenerationSession>> GetActiveSessionsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Cleans up completed or failed sessions
        /// </summary>
        Task<bool> CleanupSessionsAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result of complete domain logic generation
    /// </summary>
    public class DomainLogicGenerationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public DomainLogicResult DomainLogic { get; set; } = new();
        public TestSuiteResult TestSuite { get; set; } = new();
        public ValidationReport ValidationReport { get; set; } = new();
        public OptimizationResult OptimizationResult { get; set; } = new();
        public GenerationMetrics Metrics { get; set; } = new();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of domain logic validation
    /// </summary>
    public class DomainLogicValidationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public ValidationReport ValidationReport { get; set; } = new();
        public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of domain logic optimization
    /// </summary>
    public class DomainLogicOptimizationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public OptimizationResult OptimizationResult { get; set; } = new();
        public DateTime OptimizedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of test suite generation
    /// </summary>
    public class TestSuiteGenerationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public TestSuiteResult TestSuite { get; set; } = new();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Current generation progress
    /// </summary>
    public class GenerationProgress
    {
        public string SessionId { get; set; } = string.Empty;
        public GenerationStatus Status { get; set; } = GenerationStatus.Pending;
        public double ProgressPercentage { get; set; }
        public string CurrentStep { get; set; } = string.Empty;
        public List<GenerationStep> Steps { get; set; } = new();
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public TimeSpan? EstimatedTimeRemaining { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Generation report for a session
    /// </summary>
    public class GenerationReport
    {
        public string SessionId { get; set; } = string.Empty;
        public GenerationStatus Status { get; set; } = GenerationStatus.Pending;
        public DomainLogicGenerationResult Result { get; set; } = new();
        public GenerationMetrics Metrics { get; set; } = new();
        public List<GenerationStep> Steps { get; set; } = new();
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Generation session information
    /// </summary>
    public class GenerationSession
    {
        public string SessionId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public GenerationStatus Status { get; set; } = GenerationStatus.Pending;
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public TimeSpan? Duration { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Generation step information
    /// </summary>
    public class GenerationStep
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public GenerationStepStatus Status { get; set; } = GenerationStepStatus.Pending;
        public double ProgressPercentage { get; set; }
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public TimeSpan? Duration { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Generation metrics
    /// </summary>
    public class GenerationMetrics
    {
        public int EntityCount { get; set; }
        public int ValueObjectCount { get; set; }
        public int BusinessRuleCount { get; set; }
        public int DomainServiceCount { get; set; }
        public int AggregateRootCount { get; set; }
        public int DomainEventCount { get; set; }
        public int RepositoryCount { get; set; }
        public int FactoryCount { get; set; }
        public int SpecificationCount { get; set; }
        public int UnitTestCount { get; set; }
        public int IntegrationTestCount { get; set; }
        public int DomainTestCount { get; set; }
        public int TestFixtureCount { get; set; }
        public double TestCoveragePercentage { get; set; }
        public double ValidationScore { get; set; }
        public double OptimizationScore { get; set; }
        public TimeSpan TotalGenerationTime { get; set; }
        public TimeSpan DomainLogicGenerationTime { get; set; }
        public TimeSpan TestGenerationTime { get; set; }
        public TimeSpan ValidationTime { get; set; }
        public TimeSpan OptimizationTime { get; set; }
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Generation status
    /// </summary>
    public enum GenerationStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed,
        Cancelled
    }

    /// <summary>
    /// Generation step status
    /// </summary>
    public enum GenerationStepStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed,
        Skipped
    }
}
