using System.Collections.Concurrent;
using System.Threading.RateLimiting;

namespace Nexo.API.Middleware;

/// <summary>
/// Simple rate limiting middleware.
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ConcurrentDictionary<string, RateLimiter> _limiters = new();
    private readonly int _requestsPerMinute;

    public RateLimitingMiddleware(RequestDelegate next, int requestsPerMinute = 60)
    {
        _next = next;
        _requestsPerMinute = requestsPerMinute;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientId = GetClientId(context);
        var limiter = _limiters.GetOrAdd(clientId, _ => 
            new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = _requestsPerMinute,
                ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                TokensPerPeriod = _requestsPerMinute,
                AutoReplenishment = true
            }));

        using var lease = await limiter.AcquireAsync();
        if (!lease.IsAcquired)
        {
            context.Response.StatusCode = 429;
            await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
            return;
        }

        await _next(context);
    }

    private static string GetClientId(HttpContext context)
    {
        // Use API key if available, otherwise use IP address
        if (context.Request.Headers.TryGetValue("X-API-Key", out var apiKey))
        {
            return $"api-key:{apiKey}";
        }
        return $"ip:{context.Connection.RemoteIpAddress}";
    }
}
