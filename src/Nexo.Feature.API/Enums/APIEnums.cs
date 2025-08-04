namespace Nexo.Feature.API.Enums
{
    /// <summary>
    /// Represents the status of a service.
    /// </summary>
    public enum ServiceStatus
    {
        /// <summary>
        /// Service is active and available.
        /// </summary>
        Active,

        /// <summary>
        /// Service is inactive or unavailable.
        /// </summary>
        Inactive,

        /// <summary>
        /// Service is in maintenance mode.
        /// </summary>
        Maintenance,

        /// <summary>
        /// Service is overloaded.
        /// </summary>
        Overloaded,

        /// <summary>
        /// Service is experiencing errors.
        /// </summary>
        Error
    }

    /// <summary>
    /// Represents the health status of a component.
    /// </summary>
    public enum HealthStatus
    {
        /// <summary>
        /// Component is healthy.
        /// </summary>
        Healthy,

        /// <summary>
        /// Component is degraded but functional.
        /// </summary>
        Degraded,

        /// <summary>
        /// Component is unhealthy.
        /// </summary>
        Unhealthy,

        /// <summary>
        /// Component status is unknown.
        /// </summary>
        Unknown
    }

    /// <summary>
    /// Represents the routing strategy for load balancing.
    /// </summary>
    public enum RoutingStrategy
    {
        /// <summary>
        /// Round-robin routing.
        /// </summary>
        RoundRobin,

        /// <summary>
        /// Least connections routing.
        /// </summary>
        LeastConnections,

        /// <summary>
        /// Weighted round-robin routing.
        /// </summary>
        WeightedRoundRobin,

        /// <summary>
        /// IP hash routing.
        /// </summary>
        IPHash,

        /// <summary>
        /// Random routing.
        /// </summary>
        Random
    }

    /// <summary>
    /// Represents the authentication method.
    /// </summary>
    public enum AuthenticationMethod
    {
        /// <summary>
        /// No authentication required.
        /// </summary>
        None,

        /// <summary>
        /// API key authentication.
        /// </summary>
        ApiKey,

        /// <summary>
        /// Bearer token authentication.
        /// </summary>
        BearerToken,

        /// <summary>
        /// OAuth 2.0 authentication.
        /// </summary>
        OAuth2,

        /// <summary>
        /// JWT token authentication.
        /// </summary>
        JWT
    }

    /// <summary>
    /// Represents the rate limiting strategy.
    /// </summary>
    public enum RateLimitStrategy
    {
        /// <summary>
        /// Fixed window rate limiting.
        /// </summary>
        FixedWindow,

        /// <summary>
        /// Sliding window rate limiting.
        /// </summary>
        SlidingWindow,

        /// <summary>
        /// Token bucket rate limiting.
        /// </summary>
        TokenBucket,

        /// <summary>
        /// Leaky bucket rate limiting.
        /// </summary>
        LeakyBucket
    }

    /// <summary>
    /// Represents the caching strategy.
    /// </summary>
    public enum CachingStrategy
    {
        /// <summary>
        /// No caching.
        /// </summary>
        None,

        /// <summary>
        /// Time-based caching.
        /// </summary>
        TimeBased,

        /// <summary>
        /// ETag-based caching.
        /// </summary>
        ETagBased,

        /// <summary>
        /// Cache-control based caching.
        /// </summary>
        CacheControlBased
    }

    /// <summary>
    /// Represents the request priority.
    /// </summary>
    public enum RequestPriority
    {
        /// <summary>
        /// Low priority request.
        /// </summary>
        Low,

        /// <summary>
        /// Normal priority request.
        /// </summary>
        Normal,

        /// <summary>
        /// High priority request.
        /// </summary>
        High,

        /// <summary>
        /// Critical priority request.
        /// </summary>
        Critical
    }

    /// <summary>
    /// Represents the logging level.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Debug level logging.
        /// </summary>
        Debug,

        /// <summary>
        /// Information level logging.
        /// </summary>
        Information,

        /// <summary>
        /// Warning level logging.
        /// </summary>
        Warning,

        /// <summary>
        /// Error level logging.
        /// </summary>
        Error,

        /// <summary>
        /// Critical level logging.
        /// </summary>
        Critical
    }
} 