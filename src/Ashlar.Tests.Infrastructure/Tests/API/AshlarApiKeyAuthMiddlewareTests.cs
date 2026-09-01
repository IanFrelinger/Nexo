using System.IO;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Ashlar.API.Security;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.API;

/// <summary>Tests for ashlar api key auth middleware.</summary>
public sealed class AshlarApiKeyAuthMiddlewareTests
{
    private static readonly RequestDelegate Next = _ => Task.CompletedTask;

    [Fact]
    public async Task InvokeAsync_WhenApiKeyRequiredAndMissing_ReturnsUnauthorized()
    {
        var options = CreateOptions(apiKey: "secret-key", required: true);
        var middleware = new AshlarApiKeyAuthMiddleware(Next, options);
        var context = CreateContext("/api/orchestrate", method: "POST");

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenApiKeyRequiredAndValidHeader_AllowsRequest()
    {
        var nextCalled = false;
        var middleware = new AshlarApiKeyAuthMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, CreateOptions(apiKey: "secret-key", required: true));

        var context = CreateContext("/api/orchestrate", method: "POST");
        context.Request.Headers["X-Ashlar-Api-Key"] = "secret-key";

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WhenReadOnlyEndpoint_WithoutApiKey_AllowsRequest()
    {
        var nextCalled = false;
        var middleware = new AshlarApiKeyAuthMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, CreateOptions(apiKey: "secret-key", required: true));

        var context = CreateContext("/api/status", method: "GET");

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue("status endpoint is read-only and should remain accessible");
    }

    [Fact]
    public async Task InvokeAsync_WhenApiKeyRequiredButNotConfigured_RejectsMutatingRequest()
    {
        var nextCalled = false;
        var middleware = new AshlarApiKeyAuthMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, CreateOptions(apiKey: null, required: true));

        var context = CreateContext("/api/orchestrate", method: "POST");

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeFalse("legacy RequireApiKeyForMutatingEndpoints with no key must fail closed, not open");
        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task InvokeAsync_WhenApiKeyRequiredButNotConfigured_StillAllowsReadOnlyRequest()
    {
        var nextCalled = false;
        var middleware = new AshlarApiKeyAuthMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, CreateOptions(apiKey: null, required: true));

        var context = CreateContext("/api/status", method: "GET");

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue("MutatingApi scope leaves GET routes open regardless of key state");
    }

    [Fact]
    public async Task InvokeAsync_WhenApiKeyConfigured_ProtectsSupportDiagnosticsGet()
    {
        var nextCalled = false;
        var middleware = new AshlarApiKeyAuthMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, CreateOptions(apiKey: "secret-key", required: false, authMode: AshlarAuthorizationMode.ApiKey));

        var context = CreateContext("/api/support/diagnostics", method: "GET");

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeFalse("the diagnostics bundle must not be reachable unauthenticated when a key is configured");
        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task InvokeAsync_WhenApiKeyConfiguredAndValidHeader_AllowsSupportDiagnosticsGet()
    {
        var nextCalled = false;
        var middleware = new AshlarApiKeyAuthMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, CreateOptions(apiKey: "secret-key", required: false, authMode: AshlarAuthorizationMode.ApiKey));

        var context = CreateContext("/api/support/diagnostics", method: "GET");
        context.Request.Headers["X-Ashlar-Api-Key"] = "secret-key";

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue("a valid key must reach the diagnostics endpoint");
    }

    [Fact]
    public async Task InvokeAsync_WhenAuthModeNone_LeavesSupportDiagnosticsOpen()
    {
        var nextCalled = false;
        var middleware = new AshlarApiKeyAuthMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, CreateOptions(apiKey: null, required: false, authMode: AshlarAuthorizationMode.None));

        var context = CreateContext("/api/support/diagnostics", method: "GET");

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue("diagnostics stays open only when no built-in auth mode is configured");
    }

    [Fact]
    public async Task InvokeAsync_WhenSupportDiagnosticsExcluded_AllowsRequest()
    {
        var nextCalled = false;
        var middleware = new AshlarApiKeyAuthMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, CreateOptions(
            apiKey: "secret-key",
            required: false,
            authMode: AshlarAuthorizationMode.ApiKey,
            excludedAuth: ["/api/support/diagnostics"]));

        var context = CreateContext("/api/support/diagnostics", method: "GET");

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue("an explicit exclusion still opts the path out of enforcement");
    }

    [Fact]
    public async Task InvokeAsync_WhenEndpointExcluded_AllowsRequest()
    {
        var nextCalled = false;
        var middleware = new AshlarApiKeyAuthMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, CreateOptions(apiKey: "secret-key", required: true, excluded: ["/api/orchestrate"]));

        var context = CreateContext("/api/orchestrate", method: "POST");

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue("excluded path should bypass API key enforcement");
    }

    [Fact]
    public async Task InvokeAsync_WhenBearerModeAndValidAuthorizationHeader_AllowsRequest()
    {
        var nextCalled = false;
        var middleware = new AshlarApiKeyAuthMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, CreateOptions(
            apiKey: null,
            required: false,
            authMode: AshlarAuthorizationMode.BearerToken,
            bearerToken: "token-123"));

        var context = CreateContext("/api/orchestrate", method: "POST");
        context.Request.Headers["Authorization"] = "Bearer token-123";

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WhenBearerModeMissingToken_ReturnsUnauthorized()
    {
        var middleware = new AshlarApiKeyAuthMiddleware(Next, CreateOptions(
            apiKey: null,
            required: false,
            authMode: AshlarAuthorizationMode.BearerToken,
            bearerToken: "token-123"));
        var context = CreateContext("/api/orchestrate", method: "POST");

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenBasicModeAndValidAuthorizationHeader_AllowsRequest()
    {
        var nextCalled = false;
        var middleware = new AshlarApiKeyAuthMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, CreateOptions(
            apiKey: null,
            required: false,
            authMode: AshlarAuthorizationMode.Basic,
            basicUsername: "ashlar",
            basicPassword: "secret"));

        var context = CreateContext("/api/orchestrate", method: "POST");
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes("ashlar:secret"));
        context.Request.Headers["Authorization"] = $"Basic {encoded}";

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WhenAuthScopeAllApi_ProtectsGetRoutes()
    {
        var middleware = new AshlarApiKeyAuthMiddleware(Next, CreateOptions(
            apiKey: null,
            required: false,
            authMode: AshlarAuthorizationMode.BearerToken,
            authScope: AshlarAuthorizationScope.AllApi,
            bearerToken: "token-123"));
        var context = CreateContext("/api/status", method: "GET");

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenAuthScopeAllApiAndExcludedPath_AllowsRequest()
    {
        var nextCalled = false;
        var middleware = new AshlarApiKeyAuthMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, CreateOptions(
            apiKey: null,
            required: false,
            authMode: AshlarAuthorizationMode.BearerToken,
            authScope: AshlarAuthorizationScope.AllApi,
            bearerToken: "token-123",
            excludedAuth: ["/api/status"]));
        var context = CreateContext("/api/status", method: "GET");

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WhenAuthModeApiKeyOrBearer_AllowsEitherCredential()
    {
        var nextCalled = false;
        var middleware = new AshlarApiKeyAuthMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, CreateOptions(
            apiKey: "api-secret",
            required: false,
            authMode: AshlarAuthorizationMode.ApiKeyOrBearerToken,
            bearerToken: "token-123"));
        var context = CreateContext("/api/orchestrate", method: "POST");
        context.Request.Headers["Authorization"] = "Bearer token-123";

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    private static IOptions<AshlarSecurityOptions> CreateOptions(
        string? apiKey,
        bool required,
        string[]? excluded = null,
        AshlarAuthorizationMode authMode = AshlarAuthorizationMode.None,
        AshlarAuthorizationScope authScope = AshlarAuthorizationScope.MutatingApi,
        string? bearerToken = null,
        string? basicUsername = null,
        string? basicPassword = null,
        string[]? excludedAuth = null)
        => Options.Create(new AshlarSecurityOptions
        {
            ApiKey = apiKey,
            RequireApiKeyForMutatingEndpoints = required,
            ExcludedApiKeyPaths = excluded ?? [],
            AuthorizationMode = authMode.ToString(),
            AuthorizationScope = authScope.ToString(),
            BearerToken = bearerToken,
            BasicAuthUsername = basicUsername,
            BasicAuthPassword = basicPassword,
            ExcludedAuthorizationPaths = excludedAuth ?? []
        });

    private static DefaultHttpContext CreateContext(string path, string method)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = method;
        context.Response.Body = new MemoryStream();
        return context;
    }
}
