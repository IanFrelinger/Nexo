using Nexo.Feature.API.Enums;

namespace Nexo.Feature.API.Models;

/// <summary>
/// Rate limit request information
/// </summary>
public class RateLimitRequest
{
    /// <summary>
    /// Rate limit identifier (user ID, service ID, IP address, etc.)
    /// </summary>
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// Rate limit scope
    /// </summary>
    public RateLimitScope Scope { get; set; }

    /// <summary>
    /// Request timestamp
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Request weight (default is 1)
    /// </summary>
    public int Weight { get; set; } = 1;

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Rate limit check result
/// </summary>
public class RateLimitResult
{
    /// <summary>
    /// Whether the request is allowed
    /// </summary>
    public bool IsAllowed { get; set; }

    /// <summary>
    /// Current request count
    /// </summary>
    public int CurrentCount { get; set; }

    /// <summary>
    /// Maximum allowed requests
    /// </summary>
    public int MaxCount { get; set; }

    /// <summary>
    /// Time window in seconds
    /// </summary>
    public int TimeWindowSeconds { get; set; }

    /// <summary>
    /// Time until reset in seconds
    /// </summary>
    public int ResetInSeconds { get; set; }

    /// <summary>
    /// Rate limit identifier
    /// </summary>
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// Rate limit scope
    /// </summary>
    public RateLimitScope Scope { get; set; }

    /// <summary>
    /// Check timestamp
    /// </summary>
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Rate limit recording result
/// </summary>
public class RateLimitRecordingResult
{
    /// <summary>
    /// Whether recording was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Rate limit identifier
    /// </summary>
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// New request count after recording
    /// </summary>
    public int NewCount { get; set; }

    /// <summary>
    /// Recording timestamp
    /// </summary>
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Error message if recording failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Rate limit status
/// </summary>
public class RateLimitStatus
{
    /// <summary>
    /// Rate limit identifier
    /// </summary>
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// Rate limit scope
    /// </summary>
    public RateLimitScope Scope { get; set; }

    /// <summary>
    /// Current request count
    /// </summary>
    public int CurrentCount { get; set; }

    /// <summary>
    /// Maximum allowed requests
    /// </summary>
    public int MaxCount { get; set; }

    /// <summary>
    /// Time window in seconds
    /// </summary>
    public int TimeWindowSeconds { get; set; }

    /// <summary>
    /// Time until reset in seconds
    /// </summary>
    public int ResetInSeconds { get; set; }

    /// <summary>
    /// Whether currently rate limited
    /// </summary>
    public bool IsRateLimited { get; set; }

    /// <summary>
    /// Last request timestamp
    /// </summary>
    public DateTime? LastRequestAt { get; set; }

    /// <summary>
    /// Status timestamp
    /// </summary>
    public DateTime StatusAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Rate limit reset result
/// </summary>
public class RateLimitResetResult
{
    /// <summary>
    /// Whether reset was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Rate limit identifier
    /// </summary>
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// Previous request count
    /// </summary>
    public int PreviousCount { get; set; }

    /// <summary>
    /// Reset timestamp
    /// </summary>
    public DateTime ResetAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Error message if reset failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Rate limiting configuration
/// </summary>
public class RateLimitConfiguration
{
    /// <summary>
    /// Rate limit identifier
    /// </summary>
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// Rate limit scope
    /// </summary>
    public RateLimitScope Scope { get; set; }

    /// <summary>
    /// Maximum requests allowed
    /// </summary>
    public int MaxRequests { get; set; }

    /// <summary>
    /// Time window in seconds
    /// </summary>
    public int TimeWindowSeconds { get; set; }

    /// <summary>
    /// Whether this configuration is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Burst allowance (additional requests allowed in burst)
    /// </summary>
    public int BurstAllowance { get; set; } = 0;

    /// <summary>
    /// Configuration metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Rate limit configuration result
/// </summary>
public class RateLimitConfigurationResult
{
    /// <summary>
    /// Whether configuration was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Rate limit identifier
    /// </summary>
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// Configuration timestamp
    /// </summary>
    public DateTime ConfiguredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Error message if configuration failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Rate limiting statistics
/// </summary>
public class RateLimitStatistics
{
    /// <summary>
    /// Total rate limit checks
    /// </summary>
    public long TotalChecks { get; set; }

    /// <summary>
    /// Total rate limited requests
    /// </summary>
    public long TotalRateLimited { get; set; }

    /// <summary>
    /// Rate limiting percentage
    /// </summary>
    public double RateLimitPercentage => TotalChecks > 0 ? (double)TotalRateLimited / TotalChecks * 100 : 0;

    /// <summary>
    /// Active rate limit configurations
    /// </summary>
    public int ActiveConfigurations { get; set; }

    /// <summary>
    /// Statistics by scope
    /// </summary>
    public Dictionary<RateLimitScope, long> StatisticsByScope { get; set; } = new();

    /// <summary>
    /// Statistics timestamp
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
} 