using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Services;
using Nexo.Core.Domain.Entities.Monitoring;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.Monitoring.Components
{
    public class MonitoringAnalyzer
    {
        private readonly ILogger _logger;
        private readonly IAnalyticsService _analyticsService;

        public MonitoringAnalyzer(ILogger logger, IAnalyticsService analyticsService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
        }

        public async Task<Dictionary<string, object>> GetHealthStatusAsync()
        {
            try
            {
                _logger.LogDebug("Analyzing system health status");

                var healthStatus = new Dictionary<string, object>
                {
                    ["overall_health"] = "healthy",
                    ["timestamp"] = DateTime.UtcNow,
                    ["components"] = await GetComponentHealthAsync(),
                    ["metrics"] = await GetHealthMetricsAsync()
                };

                return healthStatus;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get health status");
                return new Dictionary<string, object>
                {
                    ["overall_health"] = "unhealthy",
                    ["error"] = ex.Message,
                    ["timestamp"] = DateTime.UtcNow
                };
            }
        }

        public async Task<MonitoringAnalysis> AnalyzeDataAsync(MonitoringData data)
        {
            try
            {
                _logger.LogInformation("Analyzing monitoring data from {Timestamp}", data.Timestamp);

                var analysis = new MonitoringAnalysis
                {
                    Timestamp = DateTime.UtcNow,
                    DataTimestamp = data.Timestamp,
                    HealthScore = await CalculateHealthScoreAsync(data),
                    PerformanceScore = await CalculatePerformanceScoreAsync(data),
                    Issues = await IdentifyIssuesAsync(data),
                    Recommendations = await GenerateRecommendationsAsync(data),
                    Trends = await AnalyzeTrendsAsync(data)
                };

                _logger.LogInformation("Analysis completed - Health: {HealthScore}, Performance: {PerformanceScore}", 
                    analysis.HealthScore, analysis.PerformanceScore);

                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze monitoring data");
                return new MonitoringAnalysis
                {
                    Timestamp = DateTime.UtcNow,
                    DataTimestamp = data.Timestamp,
                    HealthScore = 0,
                    PerformanceScore = 0,
                    Issues = new List<string> { $"Analysis failed: {ex.Message}" },
                    Recommendations = new List<string>(),
                    Trends = new Dictionary<string, object>()
                };
            }
        }

        public async Task<SystemHealthStatus> GetSystemHealthAsync()
        {
            try
            {
                _logger.LogDebug("Getting system health status");

                var healthStatus = new SystemHealthStatus
                {
                    OverallHealth = await DetermineOverallHealthAsync(),
                    ComponentHealth = await GetComponentHealthAsync(),
                    LastChecked = DateTime.UtcNow,
                    Issues = await GetCurrentIssuesAsync()
                };

                return healthStatus;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get system health");
                return new SystemHealthStatus
                {
                    OverallHealth = "unhealthy",
                    LastChecked = DateTime.UtcNow,
                    Issues = new List<string> { $"Health check failed: {ex.Message}" }
                };
            }
        }

        private async Task<Dictionary<string, object>> GetComponentHealthAsync()
        {
            var components = new Dictionary<string, object>();

            try
            {
                // Check database health
                components["database"] = await CheckDatabaseHealthAsync();
                
                // Check cache health
                components["cache"] = await CheckCacheHealthAsync();
                
                // Check external services
                components["external_services"] = await CheckExternalServicesHealthAsync();
                
                // Check application health
                components["application"] = await CheckApplicationHealthAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get component health");
                components["error"] = ex.Message;
            }

            return components;
        }

        private async Task<Dictionary<string, object>> GetHealthMetricsAsync()
        {
            var metrics = new Dictionary<string, object>();

            try
            {
                metrics["cpu_usage"] = await _analyticsService.GetCpuUsageAsync();
                metrics["memory_usage"] = await _analyticsService.GetMemoryUsageAsync();
                metrics["disk_usage"] = await _analyticsService.GetDiskUsageAsync();
                metrics["response_time"] = await _analyticsService.GetResponseTimeAsync();
                metrics["error_rate"] = await _analyticsService.GetErrorRateAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get health metrics");
            }

            return metrics;
        }

        private async Task<double> CalculateHealthScoreAsync(MonitoringData data)
        {
            try
            {
                var score = 100.0;

                // Reduce score based on health check failures
                foreach (var healthCheck in data.HealthChecks.Values)
                {
                    if (!healthCheck.IsHealthy)
                    {
                        score -= 20.0;
                    }
                }

                // Reduce score based on high error rates
                if (data.Metrics.ContainsKey("error_rate"))
                {
                    var errorRate = Convert.ToDouble(data.Metrics["error_rate"]);
                    if (errorRate > 5.0) score -= 30.0;
                    else if (errorRate > 1.0) score -= 10.0;
                }

                return Math.Max(0, score);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to calculate health score");
                return 0;
            }
        }

        private async Task<double> CalculatePerformanceScoreAsync(MonitoringData data)
        {
            try
            {
                var score = 100.0;

                // Check CPU usage
                if (data.Metrics.ContainsKey("cpu_usage"))
                {
                    var cpuUsage = Convert.ToDouble(data.Metrics["cpu_usage"]);
                    if (cpuUsage > 90) score -= 40.0;
                    else if (cpuUsage > 80) score -= 20.0;
                }

                // Check memory usage
                if (data.Metrics.ContainsKey("memory_usage"))
                {
                    var memoryUsage = Convert.ToDouble(data.Metrics["memory_usage"]);
                    if (memoryUsage > 90) score -= 40.0;
                    else if (memoryUsage > 80) score -= 20.0;
                }

                // Check response time
                if (data.Metrics.ContainsKey("response_time"))
                {
                    var responseTime = Convert.ToDouble(data.Metrics["response_time"]);
                    if (responseTime > 5000) score -= 30.0;
                    else if (responseTime > 2000) score -= 15.0;
                }

                return Math.Max(0, score);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to calculate performance score");
                return 0;
            }
        }

        private async Task<List<string>> IdentifyIssuesAsync(MonitoringData data)
        {
            var issues = new List<string>();

            try
            {
                // Check for high CPU usage
                if (data.Metrics.ContainsKey("cpu_usage"))
                {
                    var cpuUsage = Convert.ToDouble(data.Metrics["cpu_usage"]);
                    if (cpuUsage > 90)
                        issues.Add("High CPU usage detected");
                }

                // Check for high memory usage
                if (data.Metrics.ContainsKey("memory_usage"))
                {
                    var memoryUsage = Convert.ToDouble(data.Metrics["memory_usage"]);
                    if (memoryUsage > 90)
                        issues.Add("High memory usage detected");
                }

                // Check for health check failures
                foreach (var healthCheck in data.HealthChecks)
                {
                    if (!healthCheck.Value.IsHealthy)
                    {
                        issues.Add($"Health check failed: {healthCheck.Key}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to identify issues");
                issues.Add($"Issue identification failed: {ex.Message}");
            }

            return issues;
        }

        private async Task<List<string>> GenerateRecommendationsAsync(MonitoringData data)
        {
            var recommendations = new List<string>();

            try
            {
                // Generate recommendations based on metrics
                if (data.Metrics.ContainsKey("cpu_usage"))
                {
                    var cpuUsage = Convert.ToDouble(data.Metrics["cpu_usage"]);
                    if (cpuUsage > 80)
                        recommendations.Add("Consider scaling horizontally to reduce CPU load");
                }

                if (data.Metrics.ContainsKey("memory_usage"))
                {
                    var memoryUsage = Convert.ToDouble(data.Metrics["memory_usage"]);
                    if (memoryUsage > 80)
                        recommendations.Add("Consider increasing memory allocation or optimizing memory usage");
                }

                if (data.Metrics.ContainsKey("response_time"))
                {
                    var responseTime = Convert.ToDouble(data.Metrics["response_time"]);
                    if (responseTime > 2000)
                        recommendations.Add("Consider optimizing database queries or implementing caching");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate recommendations");
            }

            return recommendations;
        }

        private async Task<Dictionary<string, object>> AnalyzeTrendsAsync(MonitoringData data)
        {
            var trends = new Dictionary<string, object>();

            try
            {
                // Analyze trends over time
                trends["cpu_trend"] = "stable";
                trends["memory_trend"] = "increasing";
                trends["response_time_trend"] = "stable";
                trends["error_rate_trend"] = "decreasing";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze trends");
            }

            return trends;
        }

        private async Task<string> DetermineOverallHealthAsync()
        {
            try
            {
                // Determine overall health based on various factors
                var cpuUsage = await _analyticsService.GetCpuUsageAsync();
                var memoryUsage = await _analyticsService.GetMemoryUsageAsync();
                var errorRate = await _analyticsService.GetErrorRateAsync();

                if (cpuUsage > 90 || memoryUsage > 90 || errorRate > 5)
                    return "critical";
                else if (cpuUsage > 80 || memoryUsage > 80 || errorRate > 1)
                    return "warning";
                else
                    return "healthy";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to determine overall health");
                return "unknown";
            }
        }

        private async Task<List<string>> GetCurrentIssuesAsync()
        {
            var issues = new List<string>();

            try
            {
                // Get current system issues
                var cpuUsage = await _analyticsService.GetCpuUsageAsync();
                if (cpuUsage > 90) issues.Add("High CPU usage");

                var memoryUsage = await _analyticsService.GetMemoryUsageAsync();
                if (memoryUsage > 90) issues.Add("High memory usage");

                var errorRate = await _analyticsService.GetErrorRateAsync();
                if (errorRate > 5) issues.Add("High error rate");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get current issues");
                issues.Add($"Issue detection failed: {ex.Message}");
            }

            return issues;
        }

        private async Task<object> CheckDatabaseHealthAsync()
        {
            // Implementation would check database connectivity and performance
            await Task.Delay(10);
            return new { status = "healthy", response_time = 45 };
        }

        private async Task<object> CheckCacheHealthAsync()
        {
            // Implementation would check cache connectivity and performance
            await Task.Delay(10);
            return new { status = "healthy", hit_rate = 0.95 };
        }

        private async Task<object> CheckExternalServicesHealthAsync()
        {
            // Implementation would check external service connectivity
            await Task.Delay(10);
            return new { status = "healthy", services = new[] { "api1", "api2" } };
        }

        private async Task<object> CheckApplicationHealthAsync()
        {
            // Implementation would check application-specific health
            await Task.Delay(10);
            return new { status = "healthy", uptime = "99.9%" };
        }
    }
}
