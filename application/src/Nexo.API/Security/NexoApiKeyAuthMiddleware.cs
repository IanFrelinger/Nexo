using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace Nexo.API.Security;

/// <summary>
/// Optional built-in authorization middleware for Nexo API routes.
/// Supports API key, bearer token, and basic auth modes configured via <see cref="NexoSecurityOptions"/>.
/// </summary>
public sealed class NexoApiKeyAuthMiddleware
{
    private const string DefaultApiKeyHeaderName = "X-Nexo-Api-Key";
    private const string DefaultAuthorizationHeaderName = "Authorization";
    private const string DefaultBearerScheme = "Bearer";
    private const string DefaultBasicScheme = "Basic";
    private const string McpPathPrefix = "/api/mcp";
    private const string A2APathPrefix = "/api/a2a";
    private const string RootAgentCardPath = "/.well-known/agent-card.json";
    private readonly RequestDelegate _next;
    private readonly NexoSecurityOptions _options;
    private readonly NexoProtocolIngressOptions _protocolOptions;

    /// <summary>Creates middleware that validates built-in Nexo API credentials.</summary>
    public NexoApiKeyAuthMiddleware(
        RequestDelegate next,
        IOptions<NexoSecurityOptions> optionsAccessor,
        IOptions<NexoProtocolIngressOptions>? protocolOptionsAccessor = null)
    {
        _next = next;
        _options = optionsAccessor.Value;
        _protocolOptions = protocolOptionsAccessor?.Value ?? new NexoProtocolIngressOptions();
    }

    /// <summary>Validates built-in credentials and assigns the resolved auth tier for protected API routes.</summary>
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
            var payload = "{\"title\":\"Unauthorized\",\"detail\":\"" + EscapeJsonString(failureDetail) + "\"}";
            await context.Response.WriteAsync(payload);
            return;
        }

        AssignAuthTier(context);
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
        // Agent-protocol surfaces (MCP, A2A) are protocol traffic on every verb — the MCP SSE
        // listen channel is a GET, and A2A discovery cards are GETs — so the mutating-verb scope
        // logic below must not apply to them. The root agent card also lives outside /api
        // (spec-mandated /.well-known path), which the /api prefix check would otherwise skip.
        var isProtocolIngressPath = IsProtocolIngressPath(request.Path);

        if (!request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) && !isProtocolIngressPath)
            return false;

        if (IsExcludedPath(request.Path))
            return false;

        if (mode == NexoAuthorizationMode.None)
            return false;

        if (isProtocolIngressPath)
        {
            // Cards may be operator-opted into anonymous ecosystem discovery; every other
            // protocol request stays credentialed on all verbs.
            return !(_protocolOptions.AllowAnonymousAgentCard && IsAgentCardPath(request.Path));
        }

        var scope = ResolveAuthorizationScope();
        var method = request.Method;

        if (_options.RequireAuthForCopilotReadApis &&
            HttpMethods.IsGet(method) &&
            IsCopilotTaskHistoryPath(request.Path))
            return true;

        if (scope == NexoAuthorizationScope.AllApi)
            return true;

        return HttpMethods.IsPost(method) ||
               HttpMethods.IsPut(method) ||
               HttpMethods.IsPatch(method) ||
               HttpMethods.IsDelete(method);
    }

    private static bool IsCopilotTaskHistoryPath(PathString path)
    {
        var value = path.Value ?? string.Empty;
        return value.StartsWith("/api/copilot/tasks", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsProtocolIngressPath(PathString path)
        => path.StartsWithSegments(McpPathPrefix, StringComparison.OrdinalIgnoreCase) ||
           path.StartsWithSegments(A2APathPrefix, StringComparison.OrdinalIgnoreCase) ||
           IsAgentCardPath(path);

    private static bool IsAgentCardPath(PathString path)
    {
        var value = path.Value ?? string.Empty;
        return string.Equals(value, RootAgentCardPath, StringComparison.OrdinalIgnoreCase) ||
               (value.StartsWith(A2APathPrefix, StringComparison.OrdinalIgnoreCase) &&
                value.EndsWith("/.well-known/agent-card.json", StringComparison.OrdinalIgnoreCase));
    }

    private void AssignAuthTier(HttpContext context)
    {
        var headerName = string.IsNullOrWhiteSpace(_options.ApiKeyHeaderName)
            ? DefaultApiKeyHeaderName
            : _options.ApiKeyHeaderName.Trim();
        var provided = context.Request.Headers[headerName].ToString().Trim();

        var primary = string.IsNullOrWhiteSpace(_options.ApiKey) ? null : _options.ApiKey.Trim();
        var copilot = string.IsNullOrWhiteSpace(_options.CopilotScopedApiKey) ? null : _options.CopilotScopedApiKey.Trim();

        if (!string.IsNullOrEmpty(provided) &&
            copilot != null &&
            SecureEquals(copilot, provided) &&
            (primary == null || !SecureEquals(primary, provided)))
        {
            context.Items[NexoAuthContextKeys.AuthTier] = NexoApiAuthTier.CopilotScoped;
            return;
        }

        context.Items[NexoAuthContextKeys.AuthTier] = NexoApiAuthTier.Full;
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
        var headerName = string.IsNullOrWhiteSpace(_options.ApiKeyHeaderName)
            ? DefaultApiKeyHeaderName
            : _options.ApiKeyHeaderName.Trim();

        var provided = request.Headers[headerName].ToString().Trim();

        var primary = string.IsNullOrWhiteSpace(_options.ApiKey) ? null : _options.ApiKey.Trim();
        var copilot = string.IsNullOrWhiteSpace(_options.CopilotScopedApiKey) ? null : _options.CopilotScopedApiKey.Trim();

        if (primary != null && SecureEquals(primary, provided))
        {
            failureDetail = string.Empty;
            return true;
        }

        if (copilot != null && SecureEquals(copilot, provided))
        {
            failureDetail = string.Empty;
            return true;
        }

        if (primary == null && copilot == null)
        {
            if (allowWhenUnconfigured)
            {
                failureDetail = string.Empty;
                return true;
            }

            failureDetail = "API key authorization is enabled, but Nexo:Security:ApiKey is not configured.";
            return false;
        }

        failureDetail = $"Missing or invalid API key header '{headerName}'.";
        return false;
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

    private static string EscapeJsonString(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
    }
}
