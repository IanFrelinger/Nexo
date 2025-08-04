using Nexo.Feature.API.Enums;

namespace Nexo.Feature.API.Models;

/// <summary>
/// Service registration information
/// </summary>
public class ServiceRegistration
{
    /// <summary>
    /// Service information
    /// </summary>
    public ServiceInfo Service { get; set; } = new();

    /// <summary>
    /// Registration timestamp
    /// </summary>
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Registration metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Service registration result
/// </summary>
public class ServiceRegistrationResult
{
    /// <summary>
    /// Whether registration was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Service identifier
    /// </summary>
    public string ServiceId { get; set; } = string.Empty;

    /// <summary>
    /// Error message if registration failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Registration timestamp
    /// </summary>
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Service unregistration result
/// </summary>
public class ServiceUnregistrationResult
{
    /// <summary>
    /// Whether unregistration was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Service identifier
    /// </summary>
    public string ServiceId { get; set; } = string.Empty;

    /// <summary>
    /// Error message if unregistration failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Unregistration timestamp
    /// </summary>
    public DateTime UnregisteredAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Service health status
/// </summary>
public class ServiceHealthStatus
{
    /// <summary>
    /// Service identifier
    /// </summary>
    public string ServiceId { get; set; } = string.Empty;

    /// <summary>
    /// Health status
    /// </summary>
    public Enums.ServiceHealthStatus Status { get; set; }

    /// <summary>
    /// Health check timestamp
    /// </summary>
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Response time in milliseconds
    /// </summary>
    public long ResponseTimeMs { get; set; }

    /// <summary>
    /// Error message if health check failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Additional health metrics
    /// </summary>
    public Dictionary<string, object> Metrics { get; set; } = new();
}

/// <summary>
/// Service health update result
/// </summary>
public class ServiceHealthUpdateResult
{
    /// <summary>
    /// Whether update was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Service identifier
    /// </summary>
    public string ServiceId { get; set; } = string.Empty;

    /// <summary>
    /// Previous health status
    /// </summary>
    public Enums.ServiceHealthStatus PreviousStatus { get; set; }

    /// <summary>
    /// New health status
    /// </summary>
    public Enums.ServiceHealthStatus NewStatus { get; set; }

    /// <summary>
    /// Update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Error message if update failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Service discovery criteria
/// </summary>
public class ServiceDiscoveryCriteria
{
    /// <summary>
    /// Service tags to match
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Service name pattern
    /// </summary>
    public string? NamePattern { get; set; }

    /// <summary>
    /// Minimum health status
    /// </summary>
    public Enums.ServiceHealthStatus MinHealthStatus { get; set; } = Enums.ServiceHealthStatus.Unknown;

    /// <summary>
    /// Whether to include only healthy services
    /// </summary>
    public bool OnlyHealthy { get; set; } = false;

    /// <summary>
    /// Maximum number of services to return
    /// </summary>
    public int MaxResults { get; set; } = 100;
}

/// <summary>
/// Request validation result
/// </summary>
public class RequestValidationResult
{
    /// <summary>
    /// Whether validation was successful
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Validation errors
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Validation warnings
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Transformed request
    /// </summary>
    public APIGatewayRequest? TransformedRequest { get; set; }

    /// <summary>
    /// Validation timestamp
    /// </summary>
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// API Gateway statistics
/// </summary>
public class APIGatewayStatistics
{
    /// <summary>
    /// Total requests processed
    /// </summary>
    public long TotalRequests { get; set; }

    /// <summary>
    /// Successful requests
    /// </summary>
    public long SuccessfulRequests { get; set; }

    /// <summary>
    /// Failed requests
    /// </summary>
    public long FailedRequests { get; set; }

    /// <summary>
    /// Average response time in milliseconds
    /// </summary>
    public double AverageResponseTimeMs { get; set; }

    /// <summary>
    /// Number of registered services
    /// </summary>
    public int RegisteredServices { get; set; }

    /// <summary>
    /// Number of healthy services
    /// </summary>
    public int HealthyServices { get; set; }

    /// <summary>
    /// Number of unhealthy services
    /// </summary>
    public int UnhealthyServices { get; set; }

    /// <summary>
    /// Statistics timestamp
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
} 