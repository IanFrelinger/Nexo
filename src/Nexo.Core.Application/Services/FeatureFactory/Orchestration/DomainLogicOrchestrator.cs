using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Services.FeatureFactory.DomainLogic;
using Nexo.Core.Application.Services.FeatureFactory.TestGeneration;
using Nexo.Core.Application.Services.FeatureFactory.Validation;
using Nexo.Core.Domain.Entities.FeatureFactory.DomainLogic;
using Nexo.Core.Domain.Results;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.FeatureFactory.Orchestration
{
    /// <summary>
    /// Service for orchestrating complete domain logic generation workflow
    /// </summary>
    public class DomainLogicOrchestrator : IDomainLogicOrchestrator
    {
        private readonly ILogger<DomainLogicOrchestrator> _logger;
        private readonly IDomainLogicGenerator _domainLogicGenerator;
        private readonly ITestSuiteGenerator _testSuiteGenerator;
        private readonly IDomainLogicValidator _domainLogicValidator;
        private readonly ConcurrentDictionary<string, GenerationProgress> _activeSessions;
        private readonly ConcurrentDictionary<string, GenerationReport> _completedSessions;

        public DomainLogicOrchestrator(
            ILogger<DomainLogicOrchestrator> logger,
            IDomainLogicGenerator domainLogicGenerator,
            ITestSuiteGenerator testSuiteGenerator,
            IDomainLogicValidator domainLogicValidator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _domainLogicGenerator = domainLogicGenerator ?? throw new ArgumentNullException(nameof(domainLogicGenerator));
            _testSuiteGenerator = testSuiteGenerator ?? throw new ArgumentNullException(nameof(testSuiteGenerator));
            _domainLogicValidator = domainLogicValidator ?? throw new ArgumentNullException(nameof(domainLogicValidator));
            _activeSessions = new ConcurrentDictionary<string, GenerationProgress>();
            _completedSessions = new ConcurrentDictionary<string, GenerationReport>();
        }

        /// <summary>
        /// Generates complete domain logic from validated requirements
        /// </summary>
        public async Task<DomainLogicGenerationResult> GenerateCompleteDomainLogicAsync(ValidatedRequirements requirements, CancellationToken cancellationToken = default)
        {
            var sessionId = Guid.NewGuid().ToString();
            var progress = new GenerationProgress
            {
                SessionId = sessionId,
                Status = GenerationStatus.InProgress,
                StartedAt = DateTime.UtcNow,
                Steps = new List<GenerationStep>
                {
                    new GenerationStep { Name = "DomainLogicGeneration", Description = "Generating domain logic", Status = GenerationStepStatus.Pending },
                    new GenerationStep { Name = "TestSuiteGeneration", Description = "Generating test suite", Status = GenerationStepStatus.Pending },
                    new GenerationStep { Name = "Validation", Description = "Validating generated code", Status = GenerationStepStatus.Pending },
                    new GenerationStep { Name = "Optimization", Description = "Optimizing generated code", Status = GenerationStepStatus.Pending }
                }
            };

            _activeSessions[sessionId] = progress;

            try
            {
                _logger.LogInformation("Starting domain logic generation for session {SessionId}", sessionId);

                var result = new DomainLogicGenerationResult
                {
                    SessionId = sessionId,
                    Success = true,
                    GeneratedAt = DateTime.UtcNow
                };

                // Step 1: Generate domain logic
                await UpdateProgressAsync(sessionId, "DomainLogicGeneration", GenerationStepStatus.InProgress, 0, cancellationToken);
                var domainLogicResult = await _domainLogicGenerator.GenerateDomainLogicAsync(requirements, cancellationToken);
                result.DomainLogic = domainLogicResult;
                await UpdateProgressAsync(sessionId, "DomainLogicGeneration", GenerationStepStatus.Completed, 100, cancellationToken);

                if (!domainLogicResult.Success)
                {
                    result.Success = false;
                    result.ErrorMessage = domainLogicResult.ErrorMessage;
                    await CompleteSessionAsync(sessionId, GenerationStatus.Failed, result, cancellationToken);
                    return result;
                }

                // Step 2: Generate test suite
                await UpdateProgressAsync(sessionId, "TestSuiteGeneration", GenerationStepStatus.InProgress, 0, cancellationToken);
                var testSuiteResult = await _testSuiteGenerator.GenerateTestSuiteAsync(domainLogicResult, cancellationToken);
                result.TestSuite = testSuiteResult;
                await UpdateProgressAsync(sessionId, "TestSuiteGeneration", GenerationStepStatus.Completed, 100, cancellationToken);

                if (!testSuiteResult.Success)
                {
                    _logger.LogWarning("Test suite generation failed for session {SessionId}: {ErrorMessage}", sessionId, testSuiteResult.ErrorMessage);
                }

                // Step 3: Validate generated code
                await UpdateProgressAsync(sessionId, "Validation", GenerationStepStatus.InProgress, 0, cancellationToken);
                var validationReport = await _domainLogicValidator.GenerateValidationReportAsync(domainLogicResult, cancellationToken);
                result.ValidationReport = validationReport;
                await UpdateProgressAsync(sessionId, "Validation", GenerationStepStatus.Completed, 100, cancellationToken);

                // Step 4: Optimize generated code
                await UpdateProgressAsync(sessionId, "Optimization", GenerationStepStatus.InProgress, 0, cancellationToken);
                var optimizationResult = await _domainLogicValidator.OptimizeDomainLogicAsync(domainLogicResult, cancellationToken);
                result.OptimizationResult = optimizationResult;
                await UpdateProgressAsync(sessionId, "Optimization", GenerationStepStatus.Completed, 100, cancellationToken);

                // Calculate metrics
                result.Metrics = CalculateGenerationMetrics(result);

                // Complete session
                await CompleteSessionAsync(sessionId, GenerationStatus.Completed, result, cancellationToken);

                _logger.LogInformation("Domain logic generation completed successfully for session {SessionId}. Generated {EntityCount} entities, {TestCount} tests", 
                    sessionId, result.Metrics.EntityCount, result.Metrics.UnitTestCount + result.Metrics.IntegrationTestCount);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Domain logic generation failed for session {SessionId}", sessionId);
                
                var failedResult = new DomainLogicGenerationResult
                {
                    SessionId = sessionId,
                    Success = false,
                    ErrorMessage = ex.Message,
                    GeneratedAt = DateTime.UtcNow
                };

                await CompleteSessionAsync(sessionId, GenerationStatus.Failed, failedResult, cancellationToken);
                return failedResult;
            }
        }

        /// <summary>
        /// Gets the current generation progress
        /// </summary>
        public async Task<GenerationProgress> GetGenerationProgressAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_activeSessions.TryGetValue(sessionId, out var progress))
                {
                    return progress;
                }

                if (_completedSessions.TryGetValue(sessionId, out var report))
                {
                    return new GenerationProgress
                    {
                        SessionId = sessionId,
                        Status = report.Status,
                        ProgressPercentage = 100,
                        CurrentStep = "Completed",
                        Steps = report.Steps,
                        StartedAt = report.StartedAt,
                        CompletedAt = report.CompletedAt,
                        EstimatedTimeRemaining = TimeSpan.Zero
                    };
                }

                _logger.LogWarning("Session {SessionId} not found", sessionId);
                return new GenerationProgress
                {
                    SessionId = sessionId,
                    Status = GenerationStatus.Failed,
                    ProgressPercentage = 0,
                    CurrentStep = "Not Found"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get generation progress for session {SessionId}", sessionId);
                throw;
            }
        }

        /// <summary>
        /// Gets the generation report for a session
        /// </summary>
        public async Task<GenerationReport> GetGenerationReportAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_completedSessions.TryGetValue(sessionId, out var report))
                {
                    return report;
                }

                if (_activeSessions.TryGetValue(sessionId, out var progress))
                {
                    return new GenerationReport
                    {
                        SessionId = sessionId,
                        Status = progress.Status,
                        Steps = progress.Steps,
                        StartedAt = progress.StartedAt,
                        CompletedAt = progress.CompletedAt,
                        TotalDuration = progress.CompletedAt?.Subtract(progress.StartedAt) ?? TimeSpan.Zero
                    };
                }

                _logger.LogWarning("Session {SessionId} not found", sessionId);
                return new GenerationReport
                {
                    SessionId = sessionId,
                    Status = GenerationStatus.Failed
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get generation report for session {SessionId}", sessionId);
                throw;
            }
        }

        /// <summary>
        /// Cancels an ongoing generation process
        /// </summary>
        public async Task<bool> CancelGenerationAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_activeSessions.TryGetValue(sessionId, out var progress))
                {
                    progress.Status = GenerationStatus.Cancelled;
                    progress.CompletedAt = DateTime.UtcNow;
                    progress.CurrentStep = "Cancelled";

                    // Move to completed sessions
                    var report = new GenerationReport
                    {
                        SessionId = sessionId,
                        Status = GenerationStatus.Cancelled,
                        Steps = progress.Steps,
                        StartedAt = progress.StartedAt,
                        CompletedAt = progress.CompletedAt,
                        TotalDuration = progress.CompletedAt?.Subtract(progress.StartedAt) ?? TimeSpan.Zero
                    };

                    _completedSessions[sessionId] = report;
                    _activeSessions.TryRemove(sessionId, out _);

                    _logger.LogInformation("Generation cancelled for session {SessionId}", sessionId);
                    return true;
                }

                _logger.LogWarning("Cannot cancel session {SessionId} - not found or not active", sessionId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel generation for session {SessionId}", sessionId);
                return false;
            }
        }

        /// <summary>
        /// Validates generated domain logic
        /// </summary>
        public async Task<DomainLogicValidationResult> ValidateGeneratedDomainLogicAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            try
            {
                var report = await GetGenerationReportAsync(sessionId, cancellationToken);
                if (report.Status != GenerationStatus.Completed)
                {
                    return new DomainLogicValidationResult
                    {
                        Success = false,
                        ErrorMessage = "Session not completed or not found"
                    };
                }

                var domainLogicResult = new Nexo.Core.Domain.Results.DomainLogicResult
                {
                    Entities = report.Result.DomainLogic.Entities,
                    ValueObjects = report.Result.DomainLogic.ValueObjects,
                    BusinessRules = report.Result.DomainLogic.BusinessRules,
                    DomainServices = report.Result.DomainLogic.DomainServices,
                    AggregateRoots = report.Result.DomainLogic.AggregateRoots,
                    DomainEvents = report.Result.DomainLogic.DomainEvents,
                    Repositories = report.Result.DomainLogic.Repositories,
                    Factories = report.Result.DomainLogic.Factories,
                    Specifications = report.Result.DomainLogic.Specifications
                };
                var validationResult = await _domainLogicValidator.GenerateValidationReportAsync(domainLogicResult, cancellationToken);
                
                return new DomainLogicValidationResult
                {
                    Success = true,
                    ValidationReport = validationResult,
                    ValidatedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate generated domain logic for session {SessionId}", sessionId);
                return new DomainLogicValidationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ValidatedAt = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Optimizes generated domain logic
        /// </summary>
        public async Task<DomainLogicOptimizationResult> OptimizeGeneratedDomainLogicAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            try
            {
                var report = await GetGenerationReportAsync(sessionId, cancellationToken);
                if (report.Status != GenerationStatus.Completed)
                {
                    return new DomainLogicOptimizationResult
                    {
                        Success = false,
                        ErrorMessage = "Session not completed or not found"
                    };
                }

                var optimizationResult = await _domainLogicValidator.OptimizeDomainLogicAsync(report.Result.DomainLogic, cancellationToken);
                
                return new DomainLogicOptimizationResult
                {
                    Success = true,
                    OptimizationResult = optimizationResult,
                    OptimizedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to optimize generated domain logic for session {SessionId}", sessionId);
                return new DomainLogicOptimizationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    OptimizedAt = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Generates test suite for domain logic
        /// </summary>
        public async Task<TestSuiteGenerationResult> GenerateTestSuiteAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            try
            {
                var report = await GetGenerationReportAsync(sessionId, cancellationToken);
                if (report.Status != GenerationStatus.Completed)
                {
                    return new TestSuiteGenerationResult
                    {
                        Success = false,
                        ErrorMessage = "Session not completed or not found"
                    };
                }

                var testSuiteResult = await _testSuiteGenerator.GenerateTestSuiteAsync(report.Result.DomainLogic, cancellationToken);
                
                return new TestSuiteGenerationResult
                {
                    Success = testSuiteResult.Success,
                    ErrorMessage = testSuiteResult.ErrorMessage,
                    TestSuite = testSuiteResult,
                    GeneratedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate test suite for session {SessionId}", sessionId);
                return new TestSuiteGenerationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    GeneratedAt = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Gets all active generation sessions
        /// </summary>
        public async Task<List<GenerationSession>> GetActiveSessionsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var sessions = new List<GenerationSession>();

                foreach (var kvp in _activeSessions)
                {
                    var progress = kvp.Value;
                    sessions.Add(new GenerationSession
                    {
                        SessionId = progress.SessionId,
                        Status = progress.Status,
                        StartedAt = progress.StartedAt,
                        CompletedAt = progress.CompletedAt,
                        Duration = progress.CompletedAt?.Subtract(progress.StartedAt)
                    });
                }

                foreach (var kvp in _completedSessions)
                {
                    var report = kvp.Value;
                    sessions.Add(new GenerationSession
                    {
                        SessionId = report.SessionId,
                        Status = report.Status,
                        StartedAt = report.StartedAt,
                        CompletedAt = report.CompletedAt,
                        Duration = report.TotalDuration
                    });
                }

                return sessions.OrderByDescending(s => s.StartedAt).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get active sessions");
                return new List<GenerationSession>();
            }
        }

        /// <summary>
        /// Cleans up completed or failed sessions
        /// </summary>
        public async Task<bool> CleanupSessionsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var cutoffTime = DateTime.UtcNow.AddHours(-24); // Keep sessions for 24 hours
                var sessionsToRemove = new List<string>();

                // Clean up completed sessions older than cutoff time
                foreach (var kvp in _completedSessions)
                {
                    if (kvp.Value.CompletedAt < cutoffTime)
                    {
                        sessionsToRemove.Add(kvp.Key);
                    }
                }

                foreach (var sessionId in sessionsToRemove)
                {
                    _completedSessions.TryRemove(sessionId, out _);
                }

                _logger.LogInformation("Cleaned up {Count} old sessions", sessionsToRemove.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup sessions");
                return false;
            }
        }

        // Private helper methods

        private async Task UpdateProgressAsync(string sessionId, string stepName, GenerationStepStatus status, double progressPercentage, CancellationToken cancellationToken)
        {
            if (_activeSessions.TryGetValue(sessionId, out var progress))
            {
                var step = progress.Steps.FirstOrDefault(s => s.Name == stepName);
                if (step != null)
                {
                    step.Status = status;
                    step.ProgressPercentage = progressPercentage;
                    if (status == GenerationStepStatus.InProgress)
                    {
                        step.StartedAt = DateTime.UtcNow;
                    }
                    else if (status == GenerationStepStatus.Completed || status == GenerationStepStatus.Failed)
                    {
                        step.CompletedAt = DateTime.UtcNow;
                    }
                }

                progress.CurrentStep = stepName;
                progress.ProgressPercentage = CalculateOverallProgress(progress.Steps);
            }

            await Task.Delay(10, cancellationToken); // Small delay to allow progress updates
        }

        private double CalculateOverallProgress(List<GenerationStep> steps)
        {
            if (!steps.Any())
                return 0;

            var totalProgress = steps.Sum(s => s.ProgressPercentage);
            return totalProgress / steps.Count;
        }

        private async Task CompleteSessionAsync(string sessionId, GenerationStatus status, DomainLogicGenerationResult result, CancellationToken cancellationToken)
        {
            if (_activeSessions.TryGetValue(sessionId, out var progress))
            {
                progress.Status = status;
                progress.CompletedAt = DateTime.UtcNow;
                progress.ProgressPercentage = 100;
                progress.CurrentStep = status == GenerationStatus.Completed ? "Completed" : "Failed";

                var report = new GenerationReport
                {
                    SessionId = sessionId,
                    Status = status,
                    Result = result,
                    Metrics = result.Metrics,
                    Steps = progress.Steps,
                    StartedAt = progress.StartedAt,
                    CompletedAt = progress.CompletedAt,
                    TotalDuration = progress.CompletedAt?.Subtract(progress.StartedAt) ?? TimeSpan.Zero
                };

                _completedSessions[sessionId] = report;
                _activeSessions.TryRemove(sessionId, out _);
            }

            await Task.Delay(10, cancellationToken);
        }

        private GenerationMetrics CalculateGenerationMetrics(DomainLogicGenerationResult result)
        {
            var metrics = new GenerationMetrics
            {
                EntityCount = result.DomainLogic.Entities.Count,
                ValueObjectCount = result.DomainLogic.ValueObjects.Count,
                BusinessRuleCount = result.DomainLogic.BusinessRules.Count,
                DomainServiceCount = result.DomainLogic.DomainServices.Count,
                AggregateRootCount = result.DomainLogic.AggregateRoots.Count,
                DomainEventCount = result.DomainLogic.DomainEvents.Count,
                RepositoryCount = result.DomainLogic.Repositories.Count,
                FactoryCount = result.DomainLogic.Factories.Count,
                SpecificationCount = result.DomainLogic.Specifications.Count,
                UnitTestCount = result.TestSuite.UnitTests.Count,
                IntegrationTestCount = result.TestSuite.IntegrationTests.Count,
                DomainTestCount = result.TestSuite.DomainTests.Count,
                TestFixtureCount = result.TestSuite.TestFixtures.Count,
                TestCoveragePercentage = result.TestSuite.Coverage.LineCoverage,
                ValidationScore = result.ValidationReport.OverallScore.Overall,
                OptimizationScore = result.OptimizationResult.Score.Overall,
                CalculatedAt = DateTime.UtcNow
            };

            // Calculate timing metrics (simulated)
            var totalTime = TimeSpan.FromMinutes(5); // Simulated total time
            metrics.TotalGenerationTime = totalTime;
            metrics.DomainLogicGenerationTime = TimeSpan.FromMinutes(2);
            metrics.TestGenerationTime = TimeSpan.FromMinutes(1.5);
            metrics.ValidationTime = TimeSpan.FromMinutes(1);
            metrics.OptimizationTime = TimeSpan.FromMinutes(0.5);

            return metrics;
        }
    }
}
