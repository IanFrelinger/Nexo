using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.API.Interfaces;
using Nexo.Feature.API.Models;
using Nexo.Feature.API.Enums;

namespace Nexo.Feature.API.Services
{
    /// <summary>
    /// Health status and metrics functionality
    /// </summary>
    public partial class APIGateway
    {
        public async Task<GatewayHealthStatus> GetHealthStatusAsync()
        {
            var process = Process.GetCurrentProcess();
            var uptime = (DateTime.UtcNow - _startTime).TotalSeconds;

            return new GatewayHealthStatus
            {
                Status = HealthStatus.Healthy, // Simplified for now
                Timestamp = DateTime.UtcNow,
                UptimeSeconds = (long)uptime,
                MemoryUsageMB = (long)(process.WorkingSet64 / 1024.0 / 1024.0),
                CpuUsagePercentage = 0, // Would need more complex implementation
                ActiveConnections = _registeredServices.Count,
                Details = new Dictionary<string, object>
                {
                    ["RegisteredServices"] = _registeredServices.Count,
                    ["TotalRequests"] = _totalRequests,
                    ["SuccessRate"] = _totalRequests > 0 ? (double)_successfulRequests / _totalRequests * 100 : 0
                }
            };
        }

        public async Task<GatewayMetrics> GetMetricsAsync()
        {
            lock (_lockObject)
            {
                var averageResponseTime = _responseTimes.Count > 0 ? _responseTimes.Average() : 0;
                var requestsPerSecond = _totalRequests > 0 ? 
                    (double)_totalRequests / (DateTime.UtcNow - _startTime).TotalSeconds : 0;
                var errorRate = _totalRequests > 0 ? (double)_failedRequests / _totalRequests * 100 : 0;

                return new GatewayMetrics
                {
                    TotalRequests = _totalRequests,
                    SuccessfulRequests = _successfulRequests,
                    FailedRequests = _failedRequests,
                    AverageResponseTimeMs = averageResponseTime,
                    RequestsPerSecond = requestsPerSecond,
                    ErrorRatePercentage = errorRate,
                    Timestamp = DateTime.UtcNow,
                    ServiceMetrics = new Dictionary<string, ServiceMetrics>(_serviceMetrics)
                };
            }
        }
    }
}
