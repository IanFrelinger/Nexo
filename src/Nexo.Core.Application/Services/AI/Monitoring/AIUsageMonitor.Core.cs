using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Monitoring
{
    /// <summary>
    /// Core AI usage monitoring operations
    /// </summary>
    public partial class AIUsageMonitor
    {
        /// <summary>
        /// Starts monitoring an AI operation
        /// </summary>
        public async Task<string> StartOperationAsync(string operationId, AIOperationContext context, string userId = "")
        {
            try
            {
                _logger.LogDebug("Starting AI operation monitoring for {OperationId}", operationId);

                var session = new AIUsageSession
                {
                    SessionId = Guid.NewGuid().ToString(),
                    OperationId = operationId,
                    UserId = userId,
                    StartTime = DateTime.UtcNow,
                    Context = context,
                    Status = AIOperationStatus.Running,
                    Events = new List<AIUsageEvent>()
                };

                lock (_lockObject)
                {
                    _activeSessions[operationId] = session;
                }

                // Log operation start event
                await LogUsageEventAsync(new AIUsageEvent
                {
                    EventId = Guid.NewGuid().ToString(),
                    SessionId = session.SessionId,
                    OperationId = operationId,
                    EventType = AIUsageEventType.OperationStarted,
                    Timestamp = DateTime.UtcNow,
                    UserId = userId,
                    Details = new Dictionary<string, object>
                    {
                        ["OperationType"] = context.OperationType.ToString(),
                        ["TargetPlatform"] = context.TargetPlatform.ToString(),
                        ["MaxTokens"] = context.MaxTokens,
                        ["Temperature"] = context.Temperature
                    }
                });

                _logger.LogInformation("AI operation monitoring started for {OperationId}", operationId);
                return session.SessionId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start AI operation monitoring for {OperationId}", operationId);
                throw;
            }
        }

        /// <summary>
        /// Updates an AI operation with progress information
        /// </summary>
        public async Task UpdateOperationAsync(string operationId, AIOperationStatus status, Dictionary<string, object>? details = null)
        {
            try
            {
                _logger.LogDebug("Updating AI operation {OperationId} with status {Status}", operationId, status);

                AIUsageSession? session;
                lock (_lockObject)
                {
                    _activeSessions.TryGetValue(operationId, out session);
                }

                if (session == null)
                {
                    _logger.LogWarning("No active session found for operation {OperationId}", operationId);
                    return;
                }

                session.Status = status;
                session.LastUpdateTime = DateTime.UtcNow;

                if (details != null)
                {
                    session.Details = details;
                }

                // Log operation update event
                await LogUsageEventAsync(new AIUsageEvent
                {
                    EventId = Guid.NewGuid().ToString(),
                    SessionId = session.SessionId,
                    OperationId = operationId,
                    EventType = AIUsageEventType.OperationUpdated,
                    Timestamp = DateTime.UtcNow,
                    UserId = session.UserId,
                    Details = details ?? new Dictionary<string, object>()
                });

                _logger.LogDebug("AI operation {OperationId} updated with status {Status}", operationId, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update AI operation {OperationId}", operationId);
            }
        }

        /// <summary>
        /// Completes an AI operation and stops monitoring
        /// </summary>
        public async Task CompleteOperationAsync(string operationId, bool success, string? errorMessage = null, Dictionary<string, object>? results = null)
        {
            try
            {
                _logger.LogDebug("Completing AI operation {OperationId} with success {Success}", operationId, success);

                AIUsageSession? session;
                lock (_lockObject)
                {
                    _activeSessions.TryGetValue(operationId, out session);
                    if (session != null)
                    {
                        _activeSessions.Remove(operationId);
                    }
                }

                if (session == null)
                {
                    _logger.LogWarning("No active session found for operation {OperationId}", operationId);
                    return;
                }

                session.Status = success ? AIOperationStatus.Completed : AIOperationStatus.Failed;
                session.EndTime = DateTime.UtcNow;
                session.Duration = session.EndTime.Value - session.StartTime;
                session.Success = success;
                session.ErrorMessage = errorMessage;

                if (results != null)
                {
                    session.Results = results;
                }

                // Log operation completion event
                await LogUsageEventAsync(new AIUsageEvent
                {
                    EventId = Guid.NewGuid().ToString(),
                    SessionId = session.SessionId,
                    OperationId = operationId,
                    EventType = success ? AIUsageEventType.OperationCompleted : AIUsageEventType.OperationFailed,
                    Timestamp = DateTime.UtcNow,
                    UserId = session.UserId,
                    Details = new Dictionary<string, object>
                    {
                        ["Success"] = success,
                        ["Duration"] = session.Duration?.TotalMilliseconds ?? 0,
                        ["ErrorMessage"] = errorMessage ?? "",
                        ["Results"] = results ?? new Dictionary<string, object>()
                    }
                });

                _logger.LogInformation("AI operation {OperationId} completed with success {Success} in {Duration}ms", 
                    operationId, success, session.Duration?.TotalMilliseconds ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to complete AI operation {OperationId}", operationId);
            }
        }

        /// <summary>
        /// Gets active AI operations
        /// </summary>
        public async Task<List<AIUsageSession>> GetActiveOperationsAsync()
        {
            try
            {
                List<AIUsageSession> activeSessions;
                lock (_lockObject)
                {
                    activeSessions = _activeSessions.Values.ToList();
                }

                await Task.Delay(10);
                return activeSessions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get active AI operations");
                throw;
            }
        }
    }
}
