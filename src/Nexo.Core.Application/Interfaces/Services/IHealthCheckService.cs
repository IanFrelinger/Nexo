using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.Monitoring
{
    /// <summary>
    /// Interface for health check services
    /// </summary>
    public interface IHealthCheckService
    {
        Task<HealthStatus> CheckHealthAsync();
        Task<Dictionary<string, HealthStatus>> CheckDependenciesAsync();
        Task<bool> RegisterHealthCheckAsync(string name, Func<Task<bool>> checkFunction);
        Task<List<HealthCheckResult>> GetHealthHistoryAsync();
        Task InitializeAsync();
        Task<HealthCheckResult> CheckSystemHealthAsync();
        Task<HealthCheckResult> CheckApplicationHealthAsync();
        Task<HealthCheckResult> CheckDatabaseHealthAsync();
        Task<HealthCheckResult> CheckExternalServicesHealthAsync();
    }

    public enum HealthStatus
    {
        Healthy,
        Unhealthy,
        Degraded,
        Critical,
        Unknown
    }

    public class HealthCheckResult
    {
        public string Name { get; set; } = string.Empty;
        public HealthStatus Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public TimeSpan Duration { get; set; }
        public HealthStatus OverallHealth { get; set; }
        public List<HealthCheckResult> HealthChecks { get; set; } = new();
        public DateTime CheckedAt { get; set; }
        public bool Success { get; set; }
    }
}
