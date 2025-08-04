using System;
using System.Collections.Generic;
using System.Net.Http;
using Nexo.Feature.API.Enums;

namespace Nexo.Feature.API.Models
{
    /// <summary>
    /// Represents an incoming API request.
    /// </summary>
    public class APIRequest
    {
        /// <summary>
        /// Unique identifier for the request.
        /// </summary>
        public string RequestId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// The HTTP method (GET, POST, PUT, DELETE, etc.).
        /// </summary>
        public string Method { get; set; } = string.Empty;

        /// <summary>
        /// The request path/URL.
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Request headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Query parameters.
        /// </summary>
        public Dictionary<string, string> QueryParameters { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Request body content.
        /// </summary>
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// Content type of the request.
        /// </summary>
        public string ContentType { get; set; } = "application/json";

        /// <summary>
        /// Timestamp when the request was received.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Client IP address.
        /// </summary>
        public string ClientIP { get; set; } = string.Empty;

        /// <summary>
        /// User agent string.
        /// </summary>
        public string UserAgent { get; set; } = string.Empty;

        /// <summary>
        /// Authentication token if present.
        /// </summary>
        public string? AuthorizationToken { get; set; }
    }

    /// <summary>
    /// Represents an API response.
    /// </summary>
    public class APIResponse
    {
        /// <summary>
        /// HTTP status code.
        /// </summary>
        public int StatusCode { get; set; } = 200;

        /// <summary>
        /// Response headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Response body content.
        /// </summary>
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// Content type of the response.
        /// </summary>
        public string ContentType { get; set; } = "application/json";

        /// <summary>
        /// Timestamp when the response was generated.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Processing time in milliseconds.
        /// </summary>
        public long ProcessingTimeMs { get; set; }

        /// <summary>
        /// Error message if the request failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Request ID for correlation.
        /// </summary>
        public string RequestId { get; set; } = string.Empty;
    }





    /// <summary>
    /// Represents the health status of the API Gateway.
    /// </summary>
    public class GatewayHealthStatus
    {
        /// <summary>
        /// Overall health status.
        /// </summary>
        public HealthStatus Status { get; set; } = HealthStatus.Healthy;

        /// <summary>
        /// Health check timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Uptime in seconds.
        /// </summary>
        public long UptimeSeconds { get; set; }

        /// <summary>
        /// Memory usage in MB.
        /// </summary>
        public long MemoryUsageMB { get; set; }

        /// <summary>
        /// CPU usage percentage.
        /// </summary>
        public double CpuUsagePercentage { get; set; }

        /// <summary>
        /// Number of active connections.
        /// </summary>
        public int ActiveConnections { get; set; }

        /// <summary>
        /// Health check details.
        /// </summary>
        public Dictionary<string, object> Details { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents API Gateway metrics and statistics.
    /// </summary>
    public class GatewayMetrics
    {
        /// <summary>
        /// Total number of requests processed.
        /// </summary>
        public long TotalRequests { get; set; }

        /// <summary>
        /// Number of successful requests.
        /// </summary>
        public long SuccessfulRequests { get; set; }

        /// <summary>
        /// Number of failed requests.
        /// </summary>
        public long FailedRequests { get; set; }

        /// <summary>
        /// Average response time in milliseconds.
        /// </summary>
        public double AverageResponseTimeMs { get; set; }

        /// <summary>
        /// Requests per second.
        /// </summary>
        public double RequestsPerSecond { get; set; }

        /// <summary>
        /// Error rate percentage.
        /// </summary>
        public double ErrorRatePercentage { get; set; }

        /// <summary>
        /// Metrics timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Service-specific metrics.
        /// </summary>
        public Dictionary<string, ServiceMetrics> ServiceMetrics { get; set; } = new Dictionary<string, ServiceMetrics>();
    }

    /// <summary>
    /// Represents metrics for a specific service.
    /// </summary>
    public class ServiceMetrics
    {
        /// <summary>
        /// Service name.
        /// </summary>
        public string ServiceName { get; set; } = string.Empty;

        /// <summary>
        /// Number of requests to this service.
        /// </summary>
        public long RequestCount { get; set; }

        /// <summary>
        /// Average response time for this service.
        /// </summary>
        public double AverageResponseTimeMs { get; set; }

        /// <summary>
        /// Error count for this service.
        /// </summary>
        public long ErrorCount { get; set; }

        /// <summary>
        /// Last request timestamp.
        /// </summary>
        public DateTime LastRequestTime { get; set; } = DateTime.UtcNow;
    }
} 