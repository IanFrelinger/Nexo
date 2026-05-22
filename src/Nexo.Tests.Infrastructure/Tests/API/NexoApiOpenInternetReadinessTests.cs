using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Nexo.API.Endpoints;
using Nexo.API.Security;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.API;

public sealed class NexoApiOpenInternetReadinessTests
{
    [Fact]
    public void PublicExposureAdvisory_ContainsInternetHardeningHints()
    {
        var response = InvokeGetSecurityAdvisory(new NexoSecurityOptions
        {
            ExposureProfile = "Public"
        });

        response.ExposureProfile.Should().Be("Public");
        response.Hints.Should().Contain(hint => hint.Contains("TLS", StringComparison.OrdinalIgnoreCase));
        response.Hints.Should().Contain(hint => hint.Contains("auth", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("GET", "/api/status")]
    [InlineData("GET", "/api/capabilities")]
    [InlineData("GET", "/api/director/dailies")]
    [InlineData("GET", "/api/director/dailies/test-daily")]
    [InlineData("POST", "/api/orchestrate")]
    [InlineData("POST", "/api/agent")]
    [InlineData("POST", "/api/director/run")]
    public async Task BuiltInAuth_AllApiScope_RejectsUnauthenticatedRequests(string method, string path)
    {
        var middleware = new NexoApiKeyAuthMiddleware(
            _ => Task.CompletedTask,
            Options.Create(new NexoSecurityOptions
            {
                ExposureProfile = "Public",
                AuthorizationMode = nameof(NexoAuthorizationMode.BearerToken),
                AuthorizationScope = nameof(NexoAuthorizationScope.AllApi),
                BearerToken = "internet-token"
            }));

        var context = CreateContext(path, method);
        await middleware.InvokeAsync(context);

        Assert.True(
            context.Response.StatusCode == StatusCodes.Status401Unauthorized,
            $"{method} {path} should be protected when built-in auth is configured for all API routes (got {context.Response.StatusCode})");
    }

    [Theory]
    [InlineData("GET", "/api/status")]
    [InlineData("GET", "/api/capabilities")]
    [InlineData("GET", "/api/director/dailies")]
    [InlineData("GET", "/api/director/dailies/test-daily")]
    [InlineData("POST", "/api/orchestrate")]
    [InlineData("POST", "/api/agent")]
    [InlineData("POST", "/api/director/run")]
    public async Task BuiltInAuth_AllApiScope_AllowsAuthenticatedBearerRequests(string method, string path)
    {
        var nextCalled = false;
        var middleware = new NexoApiKeyAuthMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            Options.Create(new NexoSecurityOptions
            {
                ExposureProfile = "Public",
                AuthorizationMode = nameof(NexoAuthorizationMode.BearerToken),
                AuthorizationScope = nameof(NexoAuthorizationScope.AllApi),
                BearerToken = "internet-token"
            }));

        var context = CreateContext(path, method);
        context.Request.Headers["Authorization"] = "Bearer internet-token";

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue(
            "{0} {1} should allow requests when credentials are present for all-api auth mode",
            method,
            path);
    }

    [Fact]
    public async Task BuiltInAuth_MutatingApiScope_LeavesReadOnlyRoutesOpen()
    {
        var nextCalled = false;
        var middleware = new NexoApiKeyAuthMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            Options.Create(new NexoSecurityOptions
            {
                ExposureProfile = "Public",
                AuthorizationMode = nameof(NexoAuthorizationMode.BearerToken),
                AuthorizationScope = nameof(NexoAuthorizationScope.MutatingApi),
                BearerToken = "internet-token"
            }));

        var context = CreateContext("/api/status", HttpMethods.Get);
        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue("MutatingApi scope should not protect GET routes");
    }

    [Fact]
    public async Task BuiltInAuth_AllApiScope_AllowsConfiguredExclusions()
    {
        var middleware = new NexoApiKeyAuthMiddleware(
            _ => Task.CompletedTask,
            Options.Create(new NexoSecurityOptions
            {
                ExposureProfile = "Public",
                AuthorizationMode = nameof(NexoAuthorizationMode.BearerToken),
                AuthorizationScope = nameof(NexoAuthorizationScope.AllApi),
                BearerToken = "internet-token",
                ExcludedAuthorizationPaths = ["/api/status"]
            }));

        var excluded = CreateContext("/api/status", HttpMethods.Get);
        await middleware.InvokeAsync(excluded);
        Assert.NotEqual(StatusCodes.Status401Unauthorized, excluded.Response.StatusCode);

        var protectedContext = CreateContext("/api/capabilities", HttpMethods.Get);
        await middleware.InvokeAsync(protectedContext);
        Assert.Equal(StatusCodes.Status401Unauthorized, protectedContext.Response.StatusCode);
    }

    [Fact]
    public async Task BuiltInAuth_UnconfiguredCredentials_FailsClosed()
    {
        var middleware = new NexoApiKeyAuthMiddleware(
            _ => Task.CompletedTask,
            Options.Create(new NexoSecurityOptions
            {
                ExposureProfile = "Public",
                AuthorizationMode = nameof(NexoAuthorizationMode.Basic),
                AuthorizationScope = nameof(NexoAuthorizationScope.AllApi)
            }));

        var context = CreateContext("/api/orchestrate", HttpMethods.Post);
        await middleware.InvokeAsync(context);

        Assert.True(
            context.Response.StatusCode == StatusCodes.Status401Unauthorized,
            "when built-in auth is enabled but credentials are missing, middleware should fail closed");
    }

    private static DefaultHttpContext CreateContext(string path, string method)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = method;
        return context;
    }

    private static SecurityAdvisoryResponse InvokeGetSecurityAdvisory(NexoSecurityOptions options)
    {
        var method = typeof(NexoEndpoints).GetMethod(
            "GetSecurityAdvisory",
            BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull("security advisory endpoint handler should exist");

        var result = (IResult)method!.Invoke(
            null,
            [Options.Create(options)])!;

        var valueProperty = result.GetType().GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);
        valueProperty.Should().NotBeNull();

        var value = valueProperty!.GetValue(result);
        value.Should().BeOfType<SecurityAdvisoryResponse>();
        return (SecurityAdvisoryResponse)value!;
    }
}
