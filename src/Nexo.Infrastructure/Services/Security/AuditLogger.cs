using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Interfaces.Security;

namespace Nexo.Infrastructure.Services.Security
{
    /// <summary>
    /// Audit logger implementation for security and compliance logging.
    /// Part of Phase 3.3 security and compliance features.
    /// </summary>
    public class AuditLogger : IAuditLogger
    {
        private readonly string _logDirectory;
        private readonly object _lock = new object();

        public AuditLogger(string? logDirectory = null)
        {
            _logDirectory = logDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Nexo", "AuditLogs");
            Directory.CreateDirectory(_logDirectory);
        }

        /// <summary>
        /// Logs an audit event.
        /// </summary>
        public async Task LogAuditEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            await LogEventAsync(auditEvent, "audit", cancellationToken);
        }

        /// <summary>
        /// Logs a security event.
        /// </summary>
        public async Task LogSecurityEventAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default)
        {
            await LogEventAsync(securityEvent, "security", cancellationToken);
        }

        /// <summary>
        /// Logs a compliance event.
        /// </summary>
        public async Task LogComplianceEventAsync(ComplianceEvent complianceEvent, CancellationToken cancellationToken = default)
        {
            await LogEventAsync(complianceEvent, "compliance", cancellationToken);
        }

        /// <summary>
        /// Gets audit events for a specific time range.
        /// </summary>
        public async Task<IEnumerable<AuditEvent>> GetAuditEventsAsync(
            DateTimeOffset startTime, 
            DateTimeOffset endTime, 
            CancellationToken cancellationToken = default)
        {
            return await GetEventsAsync<AuditEvent>("audit", startTime, endTime, cancellationToken);
        }

        /// <summary>
        /// Gets security events for a specific time range.
        /// </summary>
        public async Task<IEnumerable<SecurityEvent>> GetSecurityEventsAsync(
            DateTimeOffset startTime, 
            DateTimeOffset endTime, 
            CancellationToken cancellationToken = default)
        {
            return await GetEventsAsync<SecurityEvent>("security", startTime, endTime, cancellationToken);
        }

        /// <summary>
        /// Gets compliance events for a specific time range.
        /// </summary>
        public async Task<IEnumerable<ComplianceEvent>> GetComplianceEventsAsync(
            DateTimeOffset startTime, 
            DateTimeOffset endTime, 
            CancellationToken cancellationToken = default)
        {
            return await GetEventsAsync<ComplianceEvent>("compliance", startTime, endTime, cancellationToken);
        }

        /// <summary>
        /// Generates an audit report for a specific time range.
        /// </summary>
        public async Task<AuditReport> GenerateAuditReportAsync(
            DateTimeOffset startTime, 
            DateTimeOffset endTime, 
            CancellationToken cancellationToken = default)
        {
            var auditEvents = await GetAuditEventsAsync(startTime, endTime, cancellationToken);
            var securityEvents = await GetSecurityEventsAsync(startTime, endTime, cancellationToken);
            var complianceEvents = await GetComplianceEventsAsync(startTime, endTime, cancellationToken);

            var allEvents = auditEvents.Cast<BaseAuditEvent>()
                .Concat(securityEvents.Cast<BaseAuditEvent>())
                .Concat(complianceEvents.Cast<BaseAuditEvent>())
                .ToList();

            var report = new AuditReport
            {
                GeneratedAt = DateTimeOffset.UtcNow,
                StartTime = startTime,
                EndTime = endTime,
                TotalEvents = allEvents.Count,
                AuditEvents = auditEvents.Count(),
                SecurityEvents = securityEvents.Count(),
                ComplianceEvents = complianceEvents.Count(),
                EventsByType = allEvents
                    .OfType<AuditEvent>()
                    .GroupBy(e => e.EventType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                EventsBySeverity = allEvents
                    .OfType<AuditEvent>()
                    .GroupBy(e => e.Severity)
                    .ToDictionary(g => g.Key, g => g.Count()),
                TopUsers = allEvents
                    .GroupBy(e => e.UserId)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .Select(g => g.Key)
                    .ToList(),
                TopResources = allEvents
                    .GroupBy(e => e.Resource)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .Select(g => g.Key)
                    .ToList(),
                RecentEvents = auditEvents
                    .OrderByDescending(e => e.Timestamp)
                    .Take(50)
                    .ToList()
            };

            return report;
        }

        /// <summary>
        /// Logs an event to the appropriate log file.
        /// </summary>
        private async Task LogEventAsync<T>(T auditEvent, string eventType, CancellationToken cancellationToken) where T : BaseAuditEvent
        {
            var logEntry = new LogEntry<T>
            {
                Timestamp = DateTimeOffset.UtcNow,
                EventType = eventType,
                Event = auditEvent
            };

            var logFileName = GetLogFileName(eventType, auditEvent.Timestamp);
            var logFilePath = Path.Combine(_logDirectory, logFileName);

            var json = JsonSerializer.Serialize(logEntry, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            lock (_lock)
            {
                File.AppendAllText(logFilePath, json + Environment.NewLine);
            }

            await Task.Yield(); // Simulate async operation
        }

        /// <summary>
        /// Gets events from log files for a specific time range.
        /// </summary>
        private async Task<IEnumerable<T>> GetEventsAsync<T>(
            string eventType, 
            DateTimeOffset startTime, 
            DateTimeOffset endTime, 
            CancellationToken cancellationToken) where T : BaseAuditEvent
        {
            var events = new List<T>();
            var logFiles = GetLogFilesInRange(eventType, startTime, endTime);

            foreach (var logFile in logFiles)
            {
                if (!File.Exists(logFile)) continue;

                var lines = await File.ReadAllLinesAsync(logFile, cancellationToken);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    try
                    {
                        var logEntry = JsonSerializer.Deserialize<LogEntry<T>>(line);
                        if (logEntry?.Event != null && 
                            logEntry.Event.Timestamp >= startTime && 
                            logEntry.Event.Timestamp <= endTime)
                        {
                            events.Add(logEntry.Event);
                        }
                    }
                    catch (JsonException)
                    {
                        // Skip invalid JSON lines
                        continue;
                    }
                }
            }

            return events.OrderBy(e => e.Timestamp);
        }

        /// <summary>
        /// Gets log file name for a specific event type and timestamp.
        /// </summary>
        private static string GetLogFileName(string eventType, DateTimeOffset timestamp)
        {
            return $"{eventType}_{timestamp:yyyy-MM-dd}.json";
        }

        /// <summary>
        /// Gets log files in a specific time range.
        /// </summary>
        private IEnumerable<string> GetLogFilesInRange(string eventType, DateTimeOffset startTime, DateTimeOffset endTime)
        {
            var files = new List<string>();
            var currentDate = startTime.Date;

            while (currentDate <= endTime.Date)
            {
                var fileName = GetLogFileName(eventType, currentDate);
                var filePath = Path.Combine(_logDirectory, fileName);
                if (File.Exists(filePath))
                {
                    files.Add(filePath);
                }
                currentDate = currentDate.AddDays(1);
            }

            return files;
        }

        /// <summary>
        /// Log entry wrapper for JSON serialization.
        /// </summary>
        private class LogEntry<T> where T : BaseAuditEvent
        {
            public DateTimeOffset Timestamp { get; set; }
            public string EventType { get; set; } = string.Empty;
            public T Event { get; set; } = default(T)!;
        }
    }
}