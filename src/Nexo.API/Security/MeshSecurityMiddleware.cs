using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Nexo.API.Security;

/// <summary>
/// Enforces optional mesh service token, brick execute token, JSON body size cap, and per-IP rate limits for mesh and brick HTTP surfaces.
/// </summary>
public sealed class MeshSecurityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly MeshSecurityOptions _options;
    private readonly ILogger<MeshSecurityMiddleware>? _logger;

    private sealed record RateWindow(DateTimeOffset WindowStartUtc, int Count);

    private static readonly object RateGate = new();
    private static readonly Dictionary<string, RateWindow> RateBuckets = new(StringComparer.OrdinalIgnoreCase);

    public MeshSecurityMiddleware(
        RequestDelegate next,
        IOptions<MeshSecurityOptions> optionsAccessor,
        ILogger<MeshSecurityMiddleware>? logger = null)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _options = optionsAccessor?.Value ?? throw new ArgumentNullException(nameof(optionsAccessor));
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path;
        var isMesh = path.StartsWithSegments("/api/mesh", StringComparison.OrdinalIgnoreCase);
        var isBrickExecute = HttpMethods.IsPost(context.Request.Method) &&
                             path.StartsWithSegments("/api/bricks", StringComparison.OrdinalIgnoreCase) &&
                             path.Value?.Contains("/execute", StringComparison.OrdinalIgnoreCase) == true;

        if (!isMesh && !isBrickExecute)
        {
            await _next(context);
            return;
        }

        if (_options.MaxJsonBodyBytes > 0 &&
            (HttpMethods.IsPost(context.Request.Method) ||
             HttpMethods.IsPut(context.Request.Method) ||
             HttpMethods.IsPatch(context.Request.Method)))
        {
            var len = context.Request.ContentLength;
            if (len.HasValue && len.Value > _options.MaxJsonBodyBytes)
            {
                context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    "{\"title\":\"Payload Too Large\",\"detail\":\"Request body exceeds Nexo:Security:Mesh:MaxJsonBodyBytes.\"}",
                    context.RequestAborted);
                return;
            }
        }

        if (_options.RateLimitPermitLimit > 0 && _options.RateLimitWindowSeconds > 0 &&
            IsMutating(context.Request.Method))
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var key = $"{ip}|{(isMesh ? "mesh" : "brick")}";
            var now = DateTimeOffset.UtcNow;
            var window = TimeSpan.FromSeconds(Math.Max(1, _options.RateLimitWindowSeconds));
            var limit = Math.Max(1, _options.RateLimitPermitLimit);

            bool denied;
            lock (RateGate)
            {
                if (!RateBuckets.TryGetValue(key, out var existing) || now - existing.WindowStartUtc >= window)
                {
                    RateBuckets[key] = new RateWindow(now, 1);
                    denied = false;
                }
                else if (existing.Count >= limit)
                {
                    denied = true;
                }
                else
                {
                    RateBuckets[key] = new RateWindow(existing.WindowStartUtc, existing.Count + 1);
                    denied = false;
                }

                if (RateBuckets.Count > 10_000)
                    RateBuckets.Clear();
            }

            if (denied)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    "{\"title\":\"Too Many Requests\",\"detail\":\"Rate limit exceeded for mesh or brick execute endpoints.\"}",
                    context.RequestAborted);
                _logger?.LogWarning("mesh-security rate-limit key={Key}", key);
                return;
            }
        }

        if (isMesh && IsMutating(context.Request.Method))
        {
            if (!string.IsNullOrWhiteSpace(_options.MeshMutatingToken))
            {
                var header = string.IsNullOrWhiteSpace(_options.MeshTokenHeaderName)
                    ? "X-Nexo-Mesh-Token"
                    : _options.MeshTokenHeaderName.Trim();
                if (!TryMatchToken(context, header, _options.MeshMutatingToken))
                {
                    await WriteUnauthorizedAsync(context, $"Missing or invalid '{header}' for mesh mutating requests.");
                    return;
                }
            }
        }

        if (isBrickExecute)
        {
            var brickSecret = !string.IsNullOrWhiteSpace(_options.BrickExecuteToken)
                ? _options.BrickExecuteToken
                : _options.MeshMutatingToken;
            if (!string.IsNullOrWhiteSpace(brickSecret))
            {
                var brickHeader = string.IsNullOrWhiteSpace(_options.BrickExecuteTokenHeaderName)
                    ? "X-Nexo-Brick-Execute-Token"
                    : _options.BrickExecuteTokenHeaderName.Trim();
                var meshHeader = string.IsNullOrWhiteSpace(_options.MeshTokenHeaderName)
                    ? "X-Nexo-Mesh-Token"
                    : _options.MeshTokenHeaderName.Trim();
                var usingMeshSecretOnly = string.IsNullOrWhiteSpace(_options.BrickExecuteToken) &&
                                          !string.IsNullOrWhiteSpace(_options.MeshMutatingToken);
                var ok = TryMatchToken(context, brickHeader, brickSecret)
                         || (usingMeshSecretOnly && !string.Equals(brickHeader, meshHeader, StringComparison.OrdinalIgnoreCase) &&
                             TryMatchToken(context, meshHeader, brickSecret));
                if (!ok)
                {
                    await WriteUnauthorizedAsync(context,
                        $"Missing or invalid '{brickHeader}'{(usingMeshSecretOnly ? $" or '{meshHeader}'" : "")} for brick execute.");
                    return;
                }
            }
        }

        await _next(context);
    }

    private static bool IsMutating(string method) =>
        HttpMethods.IsPost(method) ||
        HttpMethods.IsPut(method) ||
        HttpMethods.IsPatch(method) ||
        HttpMethods.IsDelete(method);

    private static bool TryMatchToken(HttpContext context, string headerName, string expected)
    {
        var provided = context.Request.Headers[headerName].ToString().Trim();
        return SecureEquals(expected.Trim(), provided);
    }

    private static bool SecureEquals(string a, string b)
    {
        if (a.Length != b.Length)
            return false;
        var ab = Encoding.UTF8.GetBytes(a);
        var bb = Encoding.UTF8.GetBytes(b);
        return ab.Length == bb.Length && CryptographicOperations.FixedTimeEquals(ab, bb);
    }

    private static async Task WriteUnauthorizedAsync(HttpContext context, string detail)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        var escaped = detail.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
        await context.Response.WriteAsync($"{{\"title\":\"Unauthorized\",\"detail\":\"{escaped}\"}}", context.RequestAborted);
    }
}
