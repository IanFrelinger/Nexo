using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Services;
using Nexo.Core.Domain.Entities.Monitoring;
using Nexo.Core.Domain.Enums.Monitoring;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.Monitoring.Components
{
    public partial class MonitoringReporter
    {
        private readonly ILogger _logger;
        private readonly IAlertingService _alertingService;

        public MonitoringReporter(ILogger logger, IAlertingService alertingService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _alertingService = alertingService ?? throw new ArgumentNullException(nameof(alertingService));
        }

        public async Task<List<string>> GetActiveAlertsAsync()
        {
            try
            {
                _logger.LogDebug("Getting active alerts");

                var alerts = new List<string>();

                // Get alerts from alerting service
                var alertingAlerts = await _alertingService.GetActiveAlertsAsync();
                alerts.AddRange(alertingAlerts);

                // Add system-generated alerts
                var systemAlerts = await GetSystemAlertsAsync();
                alerts.AddRange(systemAlerts);

                _logger.LogDebug("Found {AlertCount} active alerts", alerts.Count);
                return alerts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get active alerts");
                return new List<string> { $"Alert retrieval failed: {ex.Message}" };
            }
        }

        public async Task<bool> SendAlertAsync(string message, AlertLevel level)
        {
            try
            {
                _logger.LogInformation("Sending alert: {Message} (Level: {Level})", message, level);

                var alert = new Alert
                {
                    Message = message,
                    Level = level,
                    Timestamp = DateTime.UtcNow,
                    Source = "MonitoringReporter"
                };

                var result = await _alertingService.SendAlertAsync(alert);
                
                if (result)
                {
                    _logger.LogInformation("Alert sent successfully");
                }
                else
                {
                    _logger.LogWarning("Failed to send alert");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send alert");
                return false;
            }
        }

        public async Task<bool> SendContextualAlertAsync(string message, AlertLevel level, Dictionary<string, object> context)
        {
            try
            {
                _logger.LogInformation("Sending contextual alert: {Message} (Level: {Level})", message, level);

                var alert = new Alert
                {
                    Message = message,
                    Level = level,
                    Timestamp = DateTime.UtcNow,
                    Source = "MonitoringReporter",
                    Context = context
                };

                var result = await _alertingService.SendAlertAsync(alert);
                
                if (result)
                {
                    _logger.LogInformation("Contextual alert sent successfully");
                }
                else
                {
                    _logger.LogWarning("Failed to send contextual alert");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send contextual alert");
                return false;
            }
        }

        public async Task<MonitoringReport> GenerateReportAsync(MonitoringAnalysis analysis)
        {
            try
            {
                _logger.LogInformation("Generating monitoring report for analysis from {Timestamp}", analysis.DataTimestamp);

                var report = new MonitoringReport
                {
                    GeneratedAt = DateTime.UtcNow,
                    DataTimestamp = analysis.DataTimestamp,
                    HealthScore = analysis.HealthScore,
                    PerformanceScore = analysis.PerformanceScore,
                    Issues = analysis.Issues,
                    Recommendations = analysis.Recommendations,
                    Trends = analysis.Trends,
                    Summary = await GenerateReportSummaryAsync(analysis),
                    Details = await GenerateReportDetailsAsync(analysis)
                };

                _logger.LogInformation("Monitoring report generated successfully");
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate monitoring report");
                return new MonitoringReport
                {
                    GeneratedAt = DateTime.UtcNow,
                    DataTimestamp = analysis.DataTimestamp,
                    HealthScore = analysis.HealthScore,
                    PerformanceScore = analysis.PerformanceScore,
                    Issues = analysis.Issues,
                    Recommendations = analysis.Recommendations,
                    Summary = $"Report generation failed: {ex.Message}",
                    Details = new Dictionary<string, object>()
                };
            }
        }

        private async Task<List<string>> GetSystemAlertsAsync()
        {
            var systemAlerts = new List<string>();

            try
            {
                // Check for system-level alerts
                var systemHealth = await CheckSystemHealthAsync();
                if (!systemHealth.IsHealthy)
                {
                    systemAlerts.Add($"System health issue: {systemHealth.Message}");
                }

                // Check for performance alerts
                var performanceIssues = await CheckPerformanceIssuesAsync();
                systemAlerts.AddRange(performanceIssues);

                // Check for resource alerts
                var resourceIssues = await CheckResourceIssuesAsync();
                systemAlerts.AddRange(resourceIssues);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get system alerts");
                systemAlerts.Add($"System alert check failed: {ex.Message}");
            }

            return systemAlerts;
        }

        private async Task<string> GenerateReportSummaryAsync(MonitoringAnalysis analysis)
        {
            try
            {
                var summary = $"Monitoring Report - Generated at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}\n";
                summary += $"Data from: {analysis.DataTimestamp:yyyy-MM-dd HH:mm:ss}\n";
                summary += $"Health Score: {analysis.HealthScore:F1}/100\n";
                summary += $"Performance Score: {analysis.PerformanceScore:F1}/100\n";
                
                if (analysis.Issues.Count > 0)
                {
                    summary += $"\nIssues Found ({analysis.Issues.Count}):\n";
                    foreach (var issue in analysis.Issues)
                    {
                        summary += $"  • {issue}\n";
                    }
                }

                if (analysis.Recommendations.Count > 0)
                {
                    summary += $"\nRecommendations ({analysis.Recommendations.Count}):\n";
                    foreach (var recommendation in analysis.Recommendations)
                    {
                        summary += $"  • {recommendation}\n";
                    }
                }

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate report summary");
                return $"Summary generation failed: {ex.Message}";
            }
        }

        private async Task<Dictionary<string, object>> GenerateReportDetailsAsync(MonitoringAnalysis analysis)
        {
            var details = new Dictionary<string, object>();

            try
            {
                details["analysis_timestamp"] = analysis.Timestamp;
                details["data_timestamp"] = analysis.DataTimestamp;
                details["health_score"] = analysis.HealthScore;
                details["performance_score"] = analysis.PerformanceScore;
                details["issue_count"] = analysis.Issues.Count;
                details["recommendation_count"] = analysis.Recommendations.Count;
                details["trends"] = analysis.Trends;

                // Add trend analysis
                details["trend_analysis"] = await AnalyzeTrendsAsync(analysis);

                // Add health breakdown
                details["health_breakdown"] = await GetHealthBreakdownAsync(analysis);

                // Add performance breakdown
                details["performance_breakdown"] = await GetPerformanceBreakdownAsync(analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate report details");
                details["error"] = ex.Message;
            }

            return details;
        }

        private async Task<SystemHealthStatus> CheckSystemHealthAsync()
        {
            // Implementation would check overall system health
            await Task.Delay(10);
            return new SystemHealthStatus
            {
                OverallHealth = "healthy",
                LastChecked = DateTime.UtcNow,
                Issues = new List<string>()
            };
        }

        private async Task<List<string>> CheckPerformanceIssuesAsync()
        {
            var issues = new List<string>();

            try
            {
                // Check for performance issues
                // This would typically query performance metrics
                await Task.Delay(10);
                
                // Placeholder logic
                if (DateTime.Now.Second % 30 == 0) // Simulate occasional issues
                {
                    issues.Add("Performance degradation detected");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check performance issues");
                issues.Add($"Performance check failed: {ex.Message}");
            }

            return issues;
        }

        private async Task<List<string>> CheckResourceIssuesAsync()
        {
            var issues = new List<string>();

            try
            {
                // Check for resource issues
                // This would typically query resource usage metrics
                await Task.Delay(10);
                
                // Placeholder logic
                if (DateTime.Now.Second % 45 == 0) // Simulate occasional issues
                {
                    issues.Add("Resource usage high");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check resource issues");
                issues.Add($"Resource check failed: {ex.Message}");
            }

            return issues;
        }

        private async Task<Dictionary<string, object>> AnalyzeTrendsAsync(MonitoringAnalysis analysis)
        {
            var trends = new Dictionary<string, object>();

            try
            {
                trends["health_trend"] = analysis.HealthScore > 80 ? "improving" : "declining";
                trends["performance_trend"] = analysis.PerformanceScore > 80 ? "improving" : "declining";
                trends["issue_trend"] = analysis.Issues.Count > 5 ? "increasing" : "stable";
                trends["recommendation_trend"] = analysis.Recommendations.Count > 3 ? "action_required" : "stable";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze trends");
                trends["error"] = ex.Message;
            }

            return trends;
        }

        private async Task<Dictionary<string, object>> GetHealthBreakdownAsync(MonitoringAnalysis analysis)
        {
            var breakdown = new Dictionary<string, object>();

            try
            {
                breakdown["overall_health"] = analysis.HealthScore;
                breakdown["health_level"] = analysis.HealthScore > 90 ? "excellent" : 
                                          analysis.HealthScore > 70 ? "good" : 
                                          analysis.HealthScore > 50 ? "fair" : "poor";
                breakdown["issue_impact"] = analysis.Issues.Count * 10; // Each issue reduces health by 10 points
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get health breakdown");
                breakdown["error"] = ex.Message;
            }

            return breakdown;
        }

        private async Task<Dictionary<string, object>> GetPerformanceBreakdownAsync(MonitoringAnalysis analysis)
        {
            var breakdown = new Dictionary<string, object>();

            try
            {
                breakdown["overall_performance"] = analysis.PerformanceScore;
                breakdown["performance_level"] = analysis.PerformanceScore > 90 ? "excellent" : 
                                               analysis.PerformanceScore > 70 ? "good" : 
                                               analysis.PerformanceScore > 50 ? "fair" : "poor";
                breakdown["optimization_potential"] = 100 - analysis.PerformanceScore;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get performance breakdown");
                breakdown["error"] = ex.Message;
            }

            return breakdown;
        }
    }
}
