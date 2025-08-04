namespace Nexo.Feature.API.Models;

/// <summary>
/// Represents information about a registered service
/// </summary>
public class ServiceInfo
{
    /// <summary>
    /// Unique identifier for the service
    /// </summary>
    public string ServiceId { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable name for the service
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Service version
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Service description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for the service
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Health check endpoint
    /// </summary>
    public string HealthCheckEndpoint { get; set; } = "/health";

    /// <summary>
    /// Service tags for categorization
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Service metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Registration timestamp
    /// </summary>
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last health check timestamp
    /// </summary>
    public DateTime? LastHealthCheck { get; set; }

    /// <summary>
    /// Current health status
    /// </summary>
    public Enums.ServiceHealthStatus HealthStatus { get; set; } = Enums.ServiceHealthStatus.Unknown;

    /// <summary>
    /// Service endpoints
    /// </summary>
    public List<ServiceEndpoint> Endpoints { get; set; } = new();

    /// <summary>
    /// Service configuration
    /// </summary>
    public ServiceConfiguration Configuration { get; set; } = new();

    /// <summary>
    /// Whether the service is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// Represents a service endpoint
/// </summary>
public class ServiceEndpoint
{
    /// <summary>
    /// Endpoint path
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// HTTP method
    /// </summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Endpoint description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Whether authentication is required
    /// </summary>
    public bool RequiresAuthentication { get; set; }

    /// <summary>
    /// Required permissions
    /// </summary>
    public List<string> RequiredPermissions { get; set; } = new();

    /// <summary>
    /// Rate limiting configuration
    /// </summary>
    public RateLimitConfiguration? RateLimitConfig { get; set; }
}

/// <summary>
/// Service configuration
/// </summary>
public class ServiceConfiguration
{
    /// <summary>
    /// Maximum concurrent requests
    /// </summary>
    public int MaxConcurrentRequests { get; set; } = 100;

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Retry configuration
    /// </summary>
    public RetryConfiguration RetryConfig { get; set; } = new();

    /// <summary>
    /// Circuit breaker configuration
    /// </summary>
    public CircuitBreakerConfiguration CircuitBreakerConfig { get; set; } = new();
}

/// <summary>
/// Retry configuration
/// </summary>
public class RetryConfiguration
{
    /// <summary>
    /// Maximum number of retries
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Retry delay in milliseconds
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Whether to use exponential backoff
    /// </summary>
    public bool UseExponentialBackoff { get; set; } = true;
}

/// <summary>
/// Circuit breaker configuration
/// </summary>
public class CircuitBreakerConfiguration
{
    /// <summary>
    /// Failure threshold
    /// </summary>
    public int FailureThreshold { get; set; } = 5;

    /// <summary>
    /// Recovery timeout in seconds
    /// </summary>
    public int RecoveryTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Whether circuit breaker is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;
} 