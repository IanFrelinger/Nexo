using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.FeatureFactory.DomainLogic;
using Nexo.Core.Domain.Results;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.FeatureFactory.Orchestration.Components
{
    public class GenerationSessionManager
    {
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, GenerationProgress> _activeSessions;
        private readonly ConcurrentDictionary<string, GenerationReport> _completedSessions;

        public GenerationSessionManager(
            ILogger logger,
            ConcurrentDictionary<string, GenerationProgress> activeSessions,
            ConcurrentDictionary<string, GenerationReport> completedSessions)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _activeSessions = activeSessions ?? throw new ArgumentNullException(nameof(activeSessions));
            _completedSessions = completedSessions ?? throw new ArgumentNullException(nameof(completedSessions));
        }

        public async Task<string> CreateSessionAsync(ValidatedRequirements requirements)
        {
            try
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

                _activeSessions.TryAdd(sessionId, progress);
                
                _logger.LogInformation("Created generation session {SessionId} for requirements {RequirementId}", 
                    sessionId, requirements.Id);
                
                return sessionId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create generation session");
                throw;
            }
        }

        public async Task CompleteSessionAsync(string sessionId, DomainLogicGenerationResult result)
        {
            try
            {
                if (_activeSessions.TryRemove(sessionId, out var progress))
                {
                    progress.Status = GenerationStatus.Completed;
                    progress.CompletedAt = DateTime.UtcNow;
                    
                    var report = new GenerationReport
                    {
                        SessionId = sessionId,
                        Status = GenerationStatus.Completed,
                        StartedAt = progress.StartedAt,
                        CompletedAt = progress.CompletedAt,
                        Duration = progress.CompletedAt.Value - progress.StartedAt,
                        Result = result,
                        Steps = progress.Steps
                    };
                    
                    _completedSessions.TryAdd(sessionId, report);
                    
                    _logger.LogInformation("Completed generation session {SessionId} in {Duration}ms", 
                        sessionId, report.Duration.TotalMilliseconds);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to complete session {SessionId}", sessionId);
            }
        }

        public async Task FailSessionAsync(string sessionId, Exception exception)
        {
            try
            {
                if (_activeSessions.TryRemove(sessionId, out var progress))
                {
                    progress.Status = GenerationStatus.Failed;
                    progress.CompletedAt = DateTime.UtcNow;
                    progress.ErrorMessage = exception.Message;
                    
                    var report = new GenerationReport
                    {
                        SessionId = sessionId,
                        Status = GenerationStatus.Failed,
                        StartedAt = progress.StartedAt,
                        CompletedAt = progress.CompletedAt,
                        Duration = progress.CompletedAt.Value - progress.StartedAt,
                        ErrorMessage = exception.Message,
                        Steps = progress.Steps
                    };
                    
                    _completedSessions.TryAdd(sessionId, report);
                    
                    _logger.LogError(exception, "Failed generation session {SessionId}", sessionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark session {SessionId} as failed", sessionId);
            }
        }

        public async Task<bool> CancelSessionAsync(string sessionId)
        {
            try
            {
                if (_activeSessions.TryRemove(sessionId, out var progress))
                {
                    progress.Status = GenerationStatus.Cancelled;
                    progress.CompletedAt = DateTime.UtcNow;
                    
                    var report = new GenerationReport
                    {
                        SessionId = sessionId,
                        Status = GenerationStatus.Cancelled,
                        StartedAt = progress.StartedAt,
                        CompletedAt = progress.CompletedAt,
                        Duration = progress.CompletedAt.Value - progress.StartedAt,
                        Steps = progress.Steps
                    };
                    
                    _completedSessions.TryAdd(sessionId, report);
                    
                    _logger.LogInformation("Cancelled generation session {SessionId}", sessionId);
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel session {SessionId}", sessionId);
                return false;
            }
        }

        public async Task<List<GenerationProgress>> GetActiveSessionsAsync()
        {
            try
            {
                return _activeSessions.Values.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get active sessions");
                return new List<GenerationProgress>();
            }
        }

        public async Task CleanupOldSessionsAsync(TimeSpan maxAge)
        {
            try
            {
                var cutoffTime = DateTime.UtcNow - maxAge;
                var sessionsToRemove = _completedSessions
                    .Where(kvp => kvp.Value.CompletedAt < cutoffTime)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var sessionId in sessionsToRemove)
                {
                    _completedSessions.TryRemove(sessionId, out _);
                }

                _logger.LogInformation("Cleaned up {Count} old sessions", sessionsToRemove.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup old sessions");
            }
        }
    }
}
