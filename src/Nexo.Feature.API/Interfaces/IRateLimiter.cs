using Nexo.Feature.API.Models;
using Nexo.Feature.API.Enums;

namespace Nexo.Feature.API.Interfaces;

/// <summary>
/// Rate limiter interface for API usage control and throttling
/// </summary>
public interface IRateLimiter
{
    /// <summary>
    /// Checks if a request is allowed based on rate limiting rules
    /// </summary>
    /// <param name="request">Rate limit request information</param>
    /// <returns>Rate limit check result</returns>
    Task<RateLimitResult> CheckRateLimitAsync(RateLimitRequest request);

    /// <summary>
    /// Records a request for rate limiting purposes
    /// </summary>
    /// <param name="request">Request to record</param>
    /// <returns>Recording result</returns>
    Task<RateLimitRecordingResult> RecordRequestAsync(RateLimitRequest request);

    /// <summary>
    /// Gets current rate limit status for a specific identifier
    /// </summary>
    /// <param name="identifier">Rate limit identifier (user, service, etc.)</param>
    /// <param name="scope">Rate limit scope</param>
    /// <returns>Current rate limit status</returns>
    Task<RateLimitStatus> GetRateLimitStatusAsync(string identifier, RateLimitScope scope);

    /// <summary>
    /// Resets rate limit counters for a specific identifier
    /// </summary>
    /// <param name="identifier">Rate limit identifier</param>
    /// <param name="scope">Rate limit scope</param>
    /// <returns>Reset result</returns>
    Task<RateLimitResetResult> ResetRateLimitAsync(string identifier, RateLimitScope scope);

    /// <summary>
    /// Configures rate limiting rules
    /// </summary>
    /// <param name="configuration">Rate limiting configuration</param>
    /// <returns>Configuration result</returns>
    Task<RateLimitConfigurationResult> ConfigureRateLimitingAsync(RateLimitConfiguration configuration);

    /// <summary>
    /// Gets rate limiting statistics and metrics
    /// </summary>
    /// <returns>Rate limiting statistics</returns>
    Task<RateLimitStatistics> GetStatisticsAsync();
} 