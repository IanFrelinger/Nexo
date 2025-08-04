using System.Text.Json;

namespace Nexo.Feature.API.Models;

/// <summary>
/// Represents an incoming request to the API Gateway
/// </summary>
public class APIGatewayRequest
{
    /// <summary>
    /// Unique identifier for the request
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// HTTP method (GET, POST, PUT, DELETE, etc.)
    /// </summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Request path/URL
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Query string parameters
    /// </summary>
    public Dictionary<string, string> QueryParameters { get; set; } = new();

    /// <summary>
    /// Request headers
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// Request body content
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Content type of the request
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Client IP address
    /// </summary>
    public string ClientIP { get; set; } = string.Empty;

    /// <summary>
    /// User agent string
    /// </summary>
    public string UserAgent { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the request was received
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Authentication token if present
    /// </summary>
    public string? AuthToken { get; set; }

    /// <summary>
    /// User identifier if authenticated
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Request correlation ID for tracing
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Additional metadata for the request
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Creates a new API Gateway request from HTTP context
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>API Gateway request</returns>
    public static async Task<APIGatewayRequest> FromHttpContextAsync(Microsoft.AspNetCore.Http.HttpContext context)
    {
        var request = new APIGatewayRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            Method = context.Request.Method,
            Path = context.Request.Path,
            ContentType = context.Request.ContentType ?? string.Empty,
            ClientIP = context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            UserAgent = context.Request.Headers["User-Agent"].ToString(),
            Timestamp = DateTime.UtcNow,
            CorrelationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
        };

        // Extract query parameters
        foreach (var param in context.Request.Query)
        {
            request.QueryParameters[param.Key] = param.Value.ToString();
        }

        // Extract headers
        foreach (var header in context.Request.Headers)
        {
            request.Headers[header.Key] = header.Value.ToString();
        }

        // Extract authentication token
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            request.AuthToken = authHeader.Substring(7);
        }

        // Read request body if present
        if (context.Request.Body != null && context.Request.ContentLength > 0)
        {
            using var reader = new StreamReader(context.Request.Body);
            request.Body = await reader.ReadToEndAsync();
        }

        return request;
    }
} 