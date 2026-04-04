using System.IO;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Nexo.API.Security;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.API;

public sealed class NexoApiKeyAuthMiddlewareTests
{
    private static readonly RequestDelegate Next = _ => Task.CompletedTask;

    [Fact]
    public async Task InvokeAsync_WhenApiKeyRequiredAndMissing_ReturnsUnauthorized()
    {
        var options = CreateOptions(apiKey: "secret-key", required: true);
        var middleware = new NexoApiKeyAuthMiddleware(Next, options);
        var context = CreateContext("/api/orchestrate", method: "POST");

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task InvokeAsync_WhenApiKeyRequiredAndValidHeader_AllowsRequest()
    {
        var nextCalled = false;
        var middleware = new NexoApiKeyAuthMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, CreateOptions(apiKey: "secret-key", required: true));

        var context = CreateContext("/api/orchestrate", method: "POST");
        context.Request.Headers["X-Nexo-Api-Key"] = "secret-key";

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WhenReadOnlyEndpoint_WithoutApiKey_AllowsRequest()
    {
        var nextCalled = false;
        var middleware = new NexoApiKeyAuthMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, CreateOptions(apiKey: "secret-key", required: true));

        var context = CreateContext("/api/status", method: "GET");

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue("status endpoint is read-only and should remain accessible");
    }

    [Fact]
    public async Task InvokeAsync_WhenApiKeyNotConfigured_AllowsRequest()
    {
        var nextCalled = false;
        var middleware = new NexoApiKeyAuthMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, CreateOptions(apiKey: null, required: true));

        var context = CreateContext("/api/orchestrate", method: "POST");

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue("missing configured API key means middleware runs in disabled mode");
    }

    [Fact]
    public async Task InvokeAsync_WhenEndpointExcluded_AllowsRequest()
    {
        var nextCalled = false;
        var middleware = new NexoApiKeyAuthMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, CreateOptions(apiKey: "secret-key", required: true, excluded: ["/api/orchestrate"]));

        var context = CreateContext("/api/orchestrate", method: "POST");

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue("excluded path should bypass API key enforcement");
    }

    private static IOptions<NexoSecurityOptions> CreateOptions(string? apiKey, bool required, string[]? excluded = null)
        => Options.Create(new NexoSecurityOptions
        {
            ApiKey = apiKey,
            RequireApiKeyForMutatingEndpoints = required,
            ExcludedApiKeyPaths = excluded ?? []
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
