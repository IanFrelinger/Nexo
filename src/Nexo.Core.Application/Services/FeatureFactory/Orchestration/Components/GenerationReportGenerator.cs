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
    public partial class GenerationReportGenerator
    {
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, GenerationReport> _completedSessions;

        public GenerationReportGenerator(ILogger logger, ConcurrentDictionary<string, GenerationReport> completedSessions)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _completedSessions = completedSessions ?? throw new ArgumentNullException(nameof(completedSessions));
        }

        public async Task<GenerationReport> GetReportAsync(string sessionId)
        {
            try
            {
                if (_completedSessions.TryGetValue(sessionId, out var report))
                {
                    return report;
                }

                _logger.LogWarning("Report not found for session {SessionId}", sessionId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get report for session {SessionId}", sessionId);
                return null;
            }
        }

        public async Task<List<GenerationReport>> GetAllReportsAsync()
        {
            try
            {
                return _completedSessions.Values.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all reports");
                return new List<GenerationReport>();
            }
        }

        public async Task<List<GenerationReport>> GetReportsByStatusAsync(GenerationStatus status)
        {
            try
            {
                return _completedSessions.Values
                    .Where(r => r.Status == status)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get reports by status {Status}", status);
                return new List<GenerationReport>();
            }
        }

        public async Task<List<GenerationReport>> GetReportsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                return _completedSessions.Values
                    .Where(r => r.StartedAt >= startDate && r.StartedAt <= endDate)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get reports by date range");
                return new List<GenerationReport>();
            }
        }

        public async Task<GenerationReport> GetLatestReportAsync()
        {
            try
            {
                return _completedSessions.Values
                    .OrderByDescending(r => r.StartedAt)
                    .FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get latest report");
                return null;
            }
        }

        public async Task<GenerationReport> GetReportBySessionIdAsync(string sessionId)
        {
            try
            {
                return await GetReportAsync(sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get report by session ID {SessionId}", sessionId);
                return null;
            }
        }

        public async Task<GenerationReport> CreateReportAsync(string sessionId, GenerationStatus status, DomainLogicGenerationResult result = null)
        {
            try
            {
                var report = new GenerationReport
                {
                    SessionId = sessionId,
                    Status = status,
                    StartedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow,
                    Duration = TimeSpan.Zero,
                    Result = result,
                    Steps = new List<GenerationStep>()
                };

                _completedSessions.TryAdd(sessionId, report);
                
                _logger.LogInformation("Created report for session {SessionId} with status {Status}", sessionId, status);
                
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create report for session {SessionId}", sessionId);
                return null;
            }
        }

        public async Task<bool> UpdateReportAsync(string sessionId, Action<GenerationReport> updateAction)
        {
            try
            {
                if (_completedSessions.TryGetValue(sessionId, out var report))
                {
                    updateAction(report);
                    _logger.LogDebug("Updated report for session {SessionId}", sessionId);
                    return true;
                }

                _logger.LogWarning("Report not found for session {SessionId}", sessionId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update report for session {SessionId}", sessionId);
                return false;
            }
        }

        public async Task<bool> DeleteReportAsync(string sessionId)
        {
            try
            {
                if (_completedSessions.TryRemove(sessionId, out var report))
                {
                    _logger.LogInformation("Deleted report for session {SessionId}", sessionId);
                    return true;
                }

                _logger.LogWarning("Report not found for session {SessionId}", sessionId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete report for session {SessionId}", sessionId);
                return false;
            }
        }

        public async Task<GenerationReportSummary> GetReportSummaryAsync()
        {
            try
            {
                var reports = _completedSessions.Values.ToList();
                
                var summary = new GenerationReportSummary
                {
                    TotalReports = reports.Count,
                    CompletedReports = reports.Count(r => r.Status == GenerationStatus.Completed),
                    FailedReports = reports.Count(r => r.Status == GenerationStatus.Failed),
                    CancelledReports = reports.Count(r => r.Status == GenerationStatus.Cancelled),
                    AverageDuration = reports.Where(r => r.Duration.HasValue).Average(r => r.Duration.Value.TotalMilliseconds),
                    TotalDuration = reports.Where(r => r.Duration.HasValue).Sum(r => r.Duration.Value.TotalMilliseconds)
                };

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get report summary");
                return new GenerationReportSummary();
            }
        }

        public async Task<List<GenerationReport>> GetReportsByDurationAsync(TimeSpan minDuration, TimeSpan maxDuration)
        {
            try
            {
                return _completedSessions.Values
                    .Where(r => r.Duration.HasValue && r.Duration.Value >= minDuration && r.Duration.Value <= maxDuration)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get reports by duration");
                return new List<GenerationReport>();
            }
        }

        public async Task<GenerationReport> GetLongestRunningReportAsync()
        {
            try
            {
                return _completedSessions.Values
                    .Where(r => r.Duration.HasValue)
                    .OrderByDescending(r => r.Duration.Value)
                    .FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get longest running report");
                return null;
            }
        }

        public async Task<GenerationReport> GetShortestRunningReportAsync()
        {
            try
            {
                return _completedSessions.Values
                    .Where(r => r.Duration.HasValue)
                    .OrderBy(r => r.Duration.Value)
                    .FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get shortest running report");
                return null;
            }
        }
    }

    public partial class GenerationReportSummary
    {
        public int TotalReports { get; set; }
        public int CompletedReports { get; set; }
        public int FailedReports { get; set; }
        public int CancelledReports { get; set; }
        public double AverageDuration { get; set; }
        public double TotalDuration { get; set; }
    }
}
