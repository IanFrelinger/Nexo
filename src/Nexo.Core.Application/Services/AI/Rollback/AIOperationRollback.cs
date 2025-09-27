using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Rollback
{
    /// <summary>
    /// AI operation rollback service for recovery and rollback capabilities
    /// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
    /// </summary>
    public partial class AIOperationRollback
    {
        private readonly ILogger<AIOperationRollback> _logger;
        private readonly Dictionary<string, OperationSnapshot> _snapshots;
        private readonly Dictionary<string, RollbackSession> _rollbackSessions;
        private readonly object _lockObject = new object();

        public AIOperationRollback(ILogger<AIOperationRollback> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _snapshots = new Dictionary<string, OperationSnapshot>();
            _rollbackSessions = new Dictionary<string, RollbackSession>();
        }

        /// <summary>
        /// Creates a snapshot of the current state before an AI operation
        /// </summary>
        public async Task<string> CreateSnapshotAsync(OperationSnapshotRequest request)
        {
            try
            {
                _logger.LogInformation("Creating operation snapshot for {OperationId}", request.OperationId);

                var snapshot = new OperationSnapshot
                {
                    SnapshotId = Guid.NewGuid().ToString(),
                    OperationId = request.OperationId,
                    CreatedAt = DateTime.UtcNow,
                    State = request.State,
                    Metadata = request.Metadata,
                    Dependencies = request.Dependencies,
                    Checkpoints = new List<Checkpoint>()
                };

                // Create checkpoints for each dependency
                foreach (var dependency in request.Dependencies)
                {
                    var checkpoint = await CreateCheckpointAsync(dependency);
                    snapshot.Checkpoints.Add(checkpoint);
                }

                lock (_lockObject)
                {
                    _snapshots[snapshot.SnapshotId] = snapshot;
                }

                _logger.LogInformation("Operation snapshot {SnapshotId} created successfully", snapshot.SnapshotId);
                return snapshot.SnapshotId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create snapshot for operation {OperationId}", request.OperationId);
                throw;
            }
        }

        /// <summary>
        /// Initiates a rollback operation
        /// </summary>
        public Task<RollbackSession> StartRollbackAsync(RollbackRequest request)
        {
            try
            {
                _logger.LogInformation("Starting rollback for operation {OperationId}", request.OperationId);

                var session = new RollbackSession
                {
                    SessionId = Guid.NewGuid().ToString(),
                    OperationId = request.OperationId,
                    SnapshotId = request.SnapshotId,
                    Reason = request.Reason,
                    Status = RollbackStatus.InProgress,
                    StartedAt = DateTime.UtcNow,
                    Steps = new List<RollbackStep>()
                };

                lock (_lockObject)
                {
                    _rollbackSessions[session.SessionId] = session;
                }

                // Start rollback process
                _ = Task.Run(() => ExecuteRollbackAsync(session));

                _logger.LogInformation("Rollback session {SessionId} started", session.SessionId);
                return Task.FromResult(session);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start rollback for operation {OperationId}", request.OperationId);
                throw;
            }
        }

        /// <summary>
        /// Gets rollback session status
        /// </summary>
        public Task<RollbackSession?> GetRollbackStatusAsync(string sessionId)
        {
            try
            {
                lock (_lockObject)
                {
                    _rollbackSessions.TryGetValue(sessionId, out var session);
                    return Task.FromResult(session);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get rollback status for session {SessionId}", sessionId);
                return Task.FromResult<RollbackSession?>(null);
            }
        }

        /// <summary>
        /// Cancels a rollback operation
        /// </summary>
        public Task<bool> CancelRollbackAsync(string sessionId)
        {
            try
            {
                lock (_lockObject)
                {
                    if (_rollbackSessions.TryGetValue(sessionId, out var session))
                    {
                        session.Status = RollbackStatus.Cancelled;
                        session.CompletedAt = DateTime.UtcNow;
                        _logger.LogInformation("Rollback session {SessionId} cancelled", sessionId);
                        return Task.FromResult(true);
                    }
                }
                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel rollback session {SessionId}", sessionId);
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Gets all snapshots for an operation
        /// </summary>
        public Task<List<OperationSnapshot>> GetSnapshotsAsync(string operationId)
        {
            try
            {
                lock (_lockObject)
                {
                    return Task.FromResult(_snapshots.Values
                        .Where(s => s.OperationId == operationId)
                        .OrderByDescending(s => s.CreatedAt)
                        .ToList());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get snapshots for operation {OperationId}", operationId);
                return Task.FromResult(new List<OperationSnapshot>());
            }
        }

        /// <summary>
        /// Gets rollback history
        /// </summary>
        public Task<List<RollbackSession>> GetRollbackHistoryAsync(string? operationId = null)
        {
            try
            {
                lock (_lockObject)
                {
                    var sessions = _rollbackSessions.Values.AsQueryable();
                    
                    if (!string.IsNullOrEmpty(operationId))
                    {
                        sessions = sessions.Where(s => s.OperationId == operationId);
                    }

                    return Task.FromResult(sessions
                        .OrderByDescending(s => s.StartedAt)
                        .ToList());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get rollback history");
                return Task.FromResult(new List<RollbackSession>());
            }
        }

        /// <summary>
        /// Validates if a rollback is possible
        /// </summary>
        public Task<RollbackValidationResult> ValidateRollbackAsync(string snapshotId)
        {
            try
            {
                _logger.LogDebug("Validating rollback for snapshot {SnapshotId}", snapshotId);

                var result = new RollbackValidationResult
                {
                    IsValid = true,
                    ValidationTime = DateTime.UtcNow,
                    Issues = new List<ValidationIssue>(),
                    Recommendations = new List<string>()
                };

                lock (_lockObject)
                {
                    if (!_snapshots.TryGetValue(snapshotId, out var snapshot))
                    {
                        result.IsValid = false;
                        result.Issues.Add(new ValidationIssue
                        {
                            Type = ValidationIssueType.SnapshotNotFound,
                            Severity = ValidationSeverity.Critical,
                            Message = "Snapshot not found",
                            Line = 0
                        });
                        return Task.FromResult(result);
                    }

                    // Validate snapshot age
                    var age = DateTime.UtcNow - snapshot.CreatedAt;
                    if (age > TimeSpan.FromHours(24))
                    {
                        result.Issues.Add(new ValidationIssue
                        {
                            Type = ValidationIssueType.SnapshotAge,
                            Severity = ValidationSeverity.Medium,
                            Message = $"Snapshot is {age.TotalHours:F1} hours old",
                            Line = 0
                        });
                    }

                    // Validate dependencies
                    foreach (var checkpoint in snapshot.Checkpoints)
                    {
                        if (!IsCheckpointValid(checkpoint))
                        {
                            result.Issues.Add(new ValidationIssue
                            {
                                Type = ValidationIssueType.DependencyInvalid,
                                Severity = ValidationSeverity.High,
                                Message = $"Dependency {checkpoint.DependencyId} is no longer valid",
                                Line = 0
                            });
                        }
                    }
                }

                // Determine overall validity
                result.IsValid = !result.Issues.Any(issue => issue.Severity == ValidationSeverity.Critical);

                // Generate recommendations
                result.Recommendations = GenerateValidationRecommendations(result.Issues);

                _logger.LogInformation("Rollback validation completed. Valid: {IsValid}, Issues: {IssueCount}", 
                    result.IsValid, result.Issues.Count);

                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate rollback for snapshot {SnapshotId}", snapshotId);
                return Task.FromResult(new RollbackValidationResult
                {
                    IsValid = false,
                    ValidationTime = DateTime.UtcNow,
                    Issues = new List<ValidationIssue>
                    {
                        new ValidationIssue
                        {
                            Type = ValidationIssueType.ValidationError,
                            Severity = ValidationSeverity.Critical,
                            Message = $"Validation failed: {ex.Message}",
                            Line = 0
                        }
                    },
                    Recommendations = new List<string> { "Review snapshot manually" }
                });
            }
        }

        /// <summary>
        /// Cleans up old snapshots and rollback sessions
        /// </summary>
        public Task<CleanupResult> CleanupAsync(TimeSpan? maxAge = null)
        {
            try
            {
                var cutoffTime = DateTime.UtcNow - (maxAge ?? TimeSpan.FromDays(7));
                var cleanedSnapshots = 0;
                var cleanedSessions = 0;

                _logger.LogInformation("Starting cleanup of snapshots and rollback sessions older than {CutoffTime}", cutoffTime);

                lock (_lockObject)
                {
                    // Clean up old snapshots
                    var oldSnapshots = _snapshots.Values
                        .Where(s => s.CreatedAt < cutoffTime)
                        .ToList();

                    foreach (var snapshot in oldSnapshots)
                    {
                        _snapshots.Remove(snapshot.SnapshotId);
                        cleanedSnapshots++;
                    }

                    // Clean up old rollback sessions
                    var oldSessions = _rollbackSessions.Values
                        .Where(s => s.StartedAt < cutoffTime)
                        .ToList();

                    foreach (var session in oldSessions)
                    {
                        _rollbackSessions.Remove(session.SessionId);
                        cleanedSessions++;
                    }
                }

                var result = new CleanupResult
                {
                    CleanedSnapshots = cleanedSnapshots,
                    CleanedSessions = cleanedSessions,
                    CleanupTime = DateTime.UtcNow
                };

                _logger.LogInformation("Cleanup completed. Removed {SnapshotCount} snapshots and {SessionCount} sessions", 
                    cleanedSnapshots, cleanedSessions);

                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup old snapshots and sessions");
                throw;
            }
        }
    }

    /// <summary>
    /// Operation snapshot request
    /// </summary>
    public class OperationSnapshotRequest
    {
        public string OperationId { get; set; } = string.Empty;
        public Dictionary<string, object> State { get; set; } = new();
        public List<DependencyInfo> Dependencies { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Operation snapshot
    /// </summary>
    public class OperationSnapshot
    {
        public string SnapshotId { get; set; } = string.Empty;
        public string OperationId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public Dictionary<string, object> State { get; set; } = new();
        public List<DependencyInfo> Dependencies { get; set; } = new();
        public List<Checkpoint> Checkpoints { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Dependency information
    /// </summary>
    public class DependencyInfo
    {
        public string DependencyId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public Dictionary<string, object> State { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Checkpoint
    /// </summary>
    public class Checkpoint
    {
        public string CheckpointId { get; set; } = string.Empty;
        public string DependencyId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public Dictionary<string, object> State { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Rollback request
    /// </summary>
    public class RollbackRequest
    {
        public string OperationId { get; set; } = string.Empty;
        public string SnapshotId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Rollback session
    /// </summary>
    public class RollbackSession
    {
        public string SessionId { get; set; } = string.Empty;
        public string OperationId { get; set; } = string.Empty;
        public string SnapshotId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public RollbackStatus Status { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public TimeSpan? Duration { get; set; }
        public string? ErrorMessage { get; set; }
        public List<RollbackStep> Steps { get; set; } = new();
    }

    /// <summary>
    /// Rollback step
    /// </summary>
    public class RollbackStep
    {
        public string StepId { get; set; } = string.Empty;
        public string CheckpointId { get; set; } = string.Empty;
        public RollbackStepStatus Status { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Result { get; set; }
    }

    /// <summary>
    /// Rollback validation result
    /// </summary>
    public class RollbackValidationResult
    {
        public bool IsValid { get; set; }
        public DateTime ValidationTime { get; set; }
        public List<ValidationIssue> Issues { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }

    /// <summary>
    /// Validation issue
    /// </summary>
    public class ValidationIssue
    {
        public ValidationIssueType Type { get; set; }
        public ValidationSeverity Severity { get; set; }
        public string Message { get; set; } = string.Empty;
        public int Line { get; set; }
    }

    /// <summary>
    /// Cleanup result
    /// </summary>
    public class CleanupResult
    {
        public int CleanedSnapshots { get; set; }
        public int CleanedSessions { get; set; }
        public DateTime CleanupTime { get; set; }
    }

    // Enums
    public enum RollbackStatus { InProgress, Completed, Failed, Cancelled, PartiallyCompleted }
    public enum RollbackStepStatus { Pending, Running, Completed, Failed }
    public enum ValidationIssueType { SnapshotNotFound, SnapshotAge, DependencyInvalid, ValidationError }
    public enum ValidationSeverity { Low, Medium, High, Critical }
}