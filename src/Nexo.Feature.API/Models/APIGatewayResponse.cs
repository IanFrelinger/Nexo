namespace Nexo.Feature.API.Models;

/// <summary>
/// Represents the API Gateway's response to a request
/// </summary>
public class APIGatewayResponse
{
    /// <summary>
    /// Unique identifier for the request that generated this response
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// HTTP status code
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Response headers
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// Response body content
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Content type of the response
    /// </summary>
    public string ContentType { get; set; } = "application/json";

    /// <summary>
    /// Timestamp when the response was generated
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Processing time in milliseconds
    /// </summary>
    public long ProcessingTimeMs { get; set; }

    /// <summary>
    /// Target service that handled the request
    /// </summary>
    public string? TargetService { get; set; }

    /// <summary>
    /// Target endpoint that handled the request
    /// </summary>
    public string? TargetEndpoint { get; set; }

    /// <summary>
    /// Error message if the request failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Error code if the request failed
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Rate limiting information
    /// </summary>
    public RateLimitInfo? RateLimitInfo { get; set; }

    /// <summary>
    /// Request correlation ID for tracing
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Additional metadata for the response
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Creates a successful response
    /// </summary>
    /// <param name="requestId">Request identifier</param>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="body">Response body</param>
    /// <param name="contentType">Content type</param>
    /// <returns>API Gateway response</returns>
    public static APIGatewayResponse Success(string requestId, int statusCode, string? body = null, string contentType = "application/json")
    {
        return new APIGatewayResponse
        {
            RequestId = requestId,
            StatusCode = statusCode,
            Body = body,
            ContentType = contentType,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates an error response
    /// </summary>
    /// <param name="requestId">Request identifier</param>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="errorMessage">Error message</param>
    /// <param name="errorCode">Error code</param>
    /// <returns>API Gateway response</returns>
    public static APIGatewayResponse Error(string requestId, int statusCode, string errorMessage, string? errorCode = null)
    {
        return new APIGatewayResponse
        {
            RequestId = requestId,
            StatusCode = statusCode,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode,
            Timestamp = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Rate limiting information included in responses
/// </summary>
public class RateLimitInfo
{
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
    /// Whether the request was rate limited
    /// </summary>
    public bool IsRateLimited { get; set; }
} 