using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.FeatureFactory.DomainLogic;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.FeatureFactory.Orchestration.Components
{
    public partial class GenerationProgressTracker
    {
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, GenerationProgress> _activeSessions;

        public GenerationProgressTracker(ILogger logger, ConcurrentDictionary<string, GenerationProgress> activeSessions)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _activeSessions = activeSessions ?? throw new ArgumentNullException(nameof(activeSessions));
        }

        public async Task<GenerationProgress> GetProgressAsync(string sessionId)
        {
            try
            {
                if (_activeSessions.TryGetValue(sessionId, out var progress))
                {
                    return progress;
                }

                _logger.LogWarning("Session {SessionId} not found in active sessions", sessionId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get progress for session {SessionId}", sessionId);
                return null;
            }
        }

        public async Task UpdateStepStatusAsync(string sessionId, string stepName, GenerationStepStatus status)
        {
            try
            {
                if (_activeSessions.TryGetValue(sessionId, out var progress))
                {
                    var step = progress.Steps.FirstOrDefault(s => s.Name == stepName);
                    if (step != null)
                    {
                        step.Status = status;
                        step.UpdatedAt = DateTime.UtcNow;
                        
                        _logger.LogDebug("Updated step {StepName} status to {Status} for session {SessionId}", 
                            stepName, status, sessionId);
                    }
                    else
                    {
                        _logger.LogWarning("Step {StepName} not found for session {SessionId}", stepName, sessionId);
                    }
                }
                else
                {
                    _logger.LogWarning("Session {SessionId} not found when updating step {StepName}", sessionId, stepName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update step status for session {SessionId}, step {StepName}", sessionId, stepName);
            }
        }

        public async Task UpdateStepProgressAsync(string sessionId, string stepName, double progressPercentage)
        {
            try
            {
                if (_activeSessions.TryGetValue(sessionId, out var progress))
                {
                    var step = progress.Steps.FirstOrDefault(s => s.Name == stepName);
                    if (step != null)
                    {
                        step.ProgressPercentage = progressPercentage;
                        step.UpdatedAt = DateTime.UtcNow;
                        
                        _logger.LogDebug("Updated step {StepName} progress to {Progress}% for session {SessionId}", 
                            stepName, progressPercentage, sessionId);
                    }
                    else
                    {
                        _logger.LogWarning("Step {StepName} not found for session {SessionId}", stepName, sessionId);
                    }
                }
                else
                {
                    _logger.LogWarning("Session {SessionId} not found when updating step progress", sessionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update step progress for session {SessionId}, step {StepName}", sessionId, stepName);
            }
        }

        public async Task AddStepMessageAsync(string sessionId, string stepName, string message)
        {
            try
            {
                if (_activeSessions.TryGetValue(sessionId, out var progress))
                {
                    var step = progress.Steps.FirstOrDefault(s => s.Name == stepName);
                    if (step != null)
                    {
                        if (step.Messages == null)
                        {
                            step.Messages = new List<string>();
                        }
                        
                        step.Messages.Add($"{DateTime.UtcNow:HH:mm:ss} - {message}");
                        step.UpdatedAt = DateTime.UtcNow;
                        
                        _logger.LogDebug("Added message to step {StepName} for session {SessionId}: {Message}", 
                            stepName, sessionId, message);
                    }
                    else
                    {
                        _logger.LogWarning("Step {StepName} not found for session {SessionId}", stepName, sessionId);
                    }
                }
                else
                {
                    _logger.LogWarning("Session {SessionId} not found when adding step message", sessionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add step message for session {SessionId}, step {StepName}", sessionId, stepName);
            }
        }

        public async Task UpdateOverallProgressAsync(string sessionId, double overallProgress)
        {
            try
            {
                if (_activeSessions.TryGetValue(sessionId, out var progress))
                {
                    progress.OverallProgress = overallProgress;
                    progress.UpdatedAt = DateTime.UtcNow;
                    
                    _logger.LogDebug("Updated overall progress to {Progress}% for session {SessionId}", 
                        overallProgress, sessionId);
                }
                else
                {
                    _logger.LogWarning("Session {SessionId} not found when updating overall progress", sessionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update overall progress for session {SessionId}", sessionId);
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

        public async Task<GenerationProgress> GetSessionByIdAsync(string sessionId)
        {
            try
            {
                if (_activeSessions.TryGetValue(sessionId, out var progress))
                {
                    return progress;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get session {SessionId}", sessionId);
                return null;
            }
        }

        public async Task<bool> IsSessionActiveAsync(string sessionId)
        {
            try
            {
                return _activeSessions.ContainsKey(sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if session {SessionId} is active", sessionId);
                return false;
            }
        }

        public async Task<int> GetActiveSessionCountAsync()
        {
            try
            {
                return _activeSessions.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get active session count");
                return 0;
            }
        }

        public async Task<List<GenerationProgress>> GetSessionsByStatusAsync(GenerationStatus status)
        {
            try
            {
                return _activeSessions.Values
                    .Where(p => p.Status == status)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get sessions by status {Status}", status);
                return new List<GenerationProgress>();
            }
        }
    }
}
