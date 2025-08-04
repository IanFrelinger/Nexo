namespace Nexo.Feature.API.Enums;

/// <summary>
/// Defines the scope of rate limiting
/// </summary>
public enum RateLimitScope
{
    /// <summary>
    /// Rate limiting applies to individual users
    /// </summary>
    User = 0,

    /// <summary>
    /// Rate limiting applies to individual services
    /// </summary>
    Service = 1,

    /// <summary>
    /// Rate limiting applies globally across all requests
    /// </summary>
    Global = 2,

    /// <summary>
    /// Rate limiting applies to specific IP addresses
    /// </summary>
    IPAddress = 3,

    /// <summary>
    /// Rate limiting applies to specific API keys
    /// </summary>
    APIKey = 4,

    /// <summary>
    /// Rate limiting applies to specific endpoints
    /// </summary>
    Endpoint = 5
} 