using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Nexo.API.Security;

/// <summary>
/// Optional built-in authorization middleware for Nexo API routes.
/// Supports API key, bearer token, and basic auth modes configured via <see cref="NexoSecurityOptions"/>.
/// </summary>
public sealed class NexoApiKeyAuthMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const string DefaultApiKeyHeaderName = "X-Nexo-Api-Key";
    private const string DefaultAuthorizationHeaderName = "Authorization";
    private const string DefaultBearerScheme = "Bearer";
    private const string DefaultBasicScheme = "Basic";
    private readonly RequestDelegate _next;
    private readonly NexoSecurityOptions _options;

    public NexoApiKeyAuthMiddleware(RequestDelegate next, IOptions<NexoSecurityOptions> optionsAccessor)
    {
        _next = next;
        _options = optionsAccessor.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var mode = ResolveAuthorizationMode(out var isLegacyApiKeyMode);
        if (!ShouldProtect(context.Request, mode))
        {
            await _next(context);
            return;
        }

        if (!TryAuthorize(context.Request, mode, isLegacyApiKeyMode, out var failureDetail))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            var payload = JsonSerializer.Serialize(new
            {
                title = "Unauthorized",
                detail = failureDetail
            }, JsonOptions);
            await context.Response.WriteAsync(payload);
            return;
        }

        await _next(context);
    }

    private NexoAuthorizationMode ResolveAuthorizationMode(out bool isLegacyApiKeyMode)
    {
        isLegacyApiKeyMode = false;

        if (Enum.TryParse<NexoAuthorizationMode>(_options.AuthorizationMode, true, out var configuredMode)
            && configuredMode != NexoAuthorizationMode.None)
        {
            return configuredMode;
        }

        if (_options.RequireApiKeyForMutatingEndpoints)
        {
            isLegacyApiKeyMode = true;
            return NexoAuthorizationMode.ApiKey;
        }

        return NexoAuthorizationMode.None;
    }

    private bool ShouldProtect(HttpRequest request, NexoAuthorizationMode mode)
    {
        if (mode == NexoAuthorizationMode.None)
            return false;

        if (!request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (IsExcludedPath(request.Path))
        {
            return false;
        }

        var scope = ResolveAuthorizationScope();
        if (scope == NexoAuthorizationScope.AllApi)
        {
            return true;
        }

        var method = request.Method;
        return HttpMethods.IsPost(method) ||
               HttpMethods.IsPut(method) ||
               HttpMethods.IsPatch(method) ||
               HttpMethods.IsDelete(method);
    }

    private bool TryAuthorize(
        HttpRequest request,
        NexoAuthorizationMode mode,
        bool isLegacyApiKeyMode,
        out string failureDetail)
    {
        return mode switch
        {
            NexoAuthorizationMode.ApiKey => TryAuthorizeApiKey(request, isLegacyApiKeyMode, out failureDetail),
            NexoAuthorizationMode.BearerToken => TryAuthorizeBearerToken(request, out failureDetail),
            NexoAuthorizationMode.Basic => TryAuthorizeBasic(request, out failureDetail),
            NexoAuthorizationMode.ApiKeyOrBearerToken => TryAuthorizeAny(
                [() => TryAuthorizeApiKey(request, isLegacyApiKeyMode, out _), () => TryAuthorizeBearerToken(request, out _)],
                "Missing or invalid API key or bearer token credentials.",
                out failureDetail),
            NexoAuthorizationMode.ApiKeyOrBasic => TryAuthorizeAny(
                [() => TryAuthorizeApiKey(request, isLegacyApiKeyMode, out _), () => TryAuthorizeBasic(request, out _)],
                "Missing or invalid API key or basic auth credentials.",
                out failureDetail),
            NexoAuthorizationMode.BearerTokenOrBasic => TryAuthorizeAny(
                [() => TryAuthorizeBearerToken(request, out _), () => TryAuthorizeBasic(request, out _)],
                "Missing or invalid bearer token or basic auth credentials.",
                out failureDetail),
            NexoAuthorizationMode.Any => TryAuthorizeAny(
                [() => TryAuthorizeApiKey(request, isLegacyApiKeyMode, out _), () => TryAuthorizeBearerToken(request, out _), () => TryAuthorizeBasic(request, out _)],
                "Missing or invalid credentials for all configured built-in auth types (API key, bearer token, basic auth).",
                out failureDetail),
            _ => TryAuthorizeApiKey(request, isLegacyApiKeyMode, out failureDetail)
        };
    }

    private static bool TryAuthorizeAny(
        IReadOnlyList<Func<bool>> validators,
        string failureMessage,
        out string failureDetail)
    {
        foreach (var validator in validators)
        {
            if (validator())
            {
                failureDetail = string.Empty;
                return true;
            }
        }

        failureDetail = failureMessage;
        return false;
    }

    private bool TryAuthorizeApiKey(HttpRequest request, bool allowWhenUnconfigured, out string failureDetail)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            if (allowWhenUnconfigured)
            {
                failureDetail = string.Empty;
                return true;
            }

            failureDetail = "API key authorization is enabled, but Nexo:Security:ApiKey is not configured.";
            return false;
        }

        var headerName = string.IsNullOrWhiteSpace(_options.ApiKeyHeaderName)
            ? DefaultApiKeyHeaderName
            : _options.ApiKeyHeaderName.Trim();

        var configured = _options.ApiKey.Trim();
        var provided = request.Headers[headerName].ToString().Trim();
        if (!SecureEquals(configured, provided))
        {
            failureDetail = $"Missing or invalid API key header '{headerName}'.";
            return false;
        }

        failureDetail = string.Empty;
        return true;
    }

    private bool TryAuthorizeBearerToken(HttpRequest request, out string failureDetail)
    {
        if (string.IsNullOrWhiteSpace(_options.BearerToken))
        {
            failureDetail = "Bearer token authorization is enabled, but Nexo:Security:BearerToken is not configured.";
            return false;
        }

        var headerName = string.IsNullOrWhiteSpace(_options.BearerTokenHeaderName)
            ? DefaultAuthorizationHeaderName
            : _options.BearerTokenHeaderName.Trim();
        var providedHeader = request.Headers[headerName].ToString().Trim();
        if (string.IsNullOrWhiteSpace(providedHeader))
        {
            failureDetail = $"Missing bearer token header '{headerName}'.";
            return false;
        }

        var configuredToken = _options.BearerToken.Trim();
        var scheme = string.IsNullOrWhiteSpace(_options.BearerTokenScheme)
            ? DefaultBearerScheme
            : _options.BearerTokenScheme.Trim();

        string providedToken;
        if (string.Equals(headerName, DefaultAuthorizationHeaderName, StringComparison.OrdinalIgnoreCase))
        {
            if (!TryReadAuthorizationToken(providedHeader, scheme, out providedToken))
            {
                failureDetail = $"Authorization header must use '{scheme} <token>' format.";
                return false;
            }
        }
        else if (TryReadAuthorizationToken(providedHeader, scheme, out var prefixedToken))
        {
            providedToken = prefixedToken;
        }
        else
        {
            providedToken = providedHeader;
        }

        if (!SecureEquals(configuredToken, providedToken))
        {
            failureDetail = "Missing or invalid bearer token.";
            return false;
        }

        failureDetail = string.Empty;
        return true;
    }

    private bool TryAuthorizeBasic(HttpRequest request, out string failureDetail)
    {
        if (string.IsNullOrWhiteSpace(_options.BasicAuthUsername) || string.IsNullOrWhiteSpace(_options.BasicAuthPassword))
        {
            failureDetail = "Basic authorization is enabled, but Nexo:Security:BasicAuthUsername or BasicAuthPassword is not configured.";
            return false;
        }

        var headerName = string.IsNullOrWhiteSpace(_options.BasicAuthHeaderName)
            ? DefaultAuthorizationHeaderName
            : _options.BasicAuthHeaderName.Trim();
        var headerValue = request.Headers[headerName].ToString().Trim();
        if (string.IsNullOrWhiteSpace(headerValue))
        {
            failureDetail = $"Missing basic authorization header '{headerName}'.";
            return false;
        }

        string encodedCredentials;
        if (string.Equals(headerName, DefaultAuthorizationHeaderName, StringComparison.OrdinalIgnoreCase))
        {
            if (!TryReadAuthorizationToken(headerValue, DefaultBasicScheme, out encodedCredentials))
            {
                failureDetail = "Authorization header must use 'Basic <base64(username:password)>' format.";
                return false;
            }
        }
        else if (TryReadAuthorizationToken(headerValue, DefaultBasicScheme, out var prefixedCredentials))
        {
            encodedCredentials = prefixedCredentials;
        }
        else
        {
            encodedCredentials = headerValue;
        }

        if (!TryDecodeBasicCredentials(encodedCredentials, out var username, out var password))
        {
            failureDetail = "Invalid basic authorization credentials encoding.";
            return false;
        }

        var configuredUsername = _options.BasicAuthUsername.Trim();
        var configuredPassword = _options.BasicAuthPassword.Trim();
        if (!SecureEquals(configuredUsername, username) || !SecureEquals(configuredPassword, password))
        {
            failureDetail = "Missing or invalid basic authorization credentials.";
            return false;
        }

        failureDetail = string.Empty;
        return true;
    }

    private bool IsExcludedPath(PathString path)
    {
        var excluded = (_options.ExcludedAuthorizationPaths ?? [])
            .Concat(_options.ExcludedApiKeyPaths ?? [])
            .Where(prefix => !string.IsNullOrWhiteSpace(prefix))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return excluded.Any(prefix => path.StartsWithSegments(prefix.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private NexoAuthorizationScope ResolveAuthorizationScope()
    {
        if (Enum.TryParse<NexoAuthorizationScope>(_options.AuthorizationScope, true, out var scope))
        {
            return scope;
        }

        return NexoAuthorizationScope.MutatingApi;
    }

    private static bool TryReadAuthorizationToken(string headerValue, string scheme, out string token)
    {
        token = string.Empty;
        if (string.IsNullOrWhiteSpace(headerValue) || string.IsNullOrWhiteSpace(scheme))
        {
            return false;
        }

        var expectedPrefix = $"{scheme.Trim()} ";
        if (!headerValue.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        token = headerValue[expectedPrefix.Length..].Trim();
        return !string.IsNullOrWhiteSpace(token);
    }

    private static bool TryDecodeBasicCredentials(string encoded, out string username, out string password)
    {
        username = string.Empty;
        password = string.Empty;

        try
        {
            var bytes = Convert.FromBase64String(encoded);
            var combined = Encoding.UTF8.GetString(bytes);
            var separator = combined.IndexOf(':');
            if (separator <= 0)
            {
                return false;
            }

            username = combined[..separator];
            password = combined[(separator + 1)..];
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool SecureEquals(string left, string right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}

public static class NexoApiKeyAuthMiddlewareExtensions
{
    public static IApplicationBuilder UseNexoApiKeyAuth(this IApplicationBuilder app)
    {
        return app.UseMiddleware<NexoApiKeyAuthMiddleware>();
    }
}
