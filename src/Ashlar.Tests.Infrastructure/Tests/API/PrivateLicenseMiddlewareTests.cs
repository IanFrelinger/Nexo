using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Ashlar.API.Security;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.API;

/// <summary>Tests for private license middleware.</summary>
public sealed class PrivateLicenseMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenEnforcementDisabled_AllowsMutatingRequest()
    {
        var nextCalled = false;
        var middleware = new PrivateLicenseMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            Options.Create(new AshlarPrivateLicenseOptions { EnforceLicense = false }),
            new StubValidator(new PrivateLicenseStatus { State = PrivateLicenseState.Expired }));

        var context = CreateContext("/api/copilot/task", "POST");
        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WhenLicenseExpired_BlocksMutatingRequest()
    {
        var middleware = new PrivateLicenseMiddleware(
            _ => Task.CompletedTask,
            Options.Create(new AshlarPrivateLicenseOptions
            {
                EnforceLicense = true,
                AllowReadOnlyWhenExpired = true,
            }),
            new StubValidator(new PrivateLicenseStatus
            {
                State = PrivateLicenseState.Expired,
                Detail = "expired",
            }));

        var context = CreateContext("/api/copilot/task", "POST");
        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status402PaymentRequired);
    }

    [Fact]
    public async Task InvokeAsync_WhenLicenseExpired_AllowsDiagnosticsRead()
    {
        var nextCalled = false;
        var middleware = new PrivateLicenseMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            Options.Create(new AshlarPrivateLicenseOptions
            {
                EnforceLicense = true,
                AllowReadOnlyWhenExpired = true,
            }),
            new StubValidator(new PrivateLicenseStatus { State = PrivateLicenseState.Expired }));

        var context = CreateContext("/api/support/diagnostics", "GET");
        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    private static DefaultHttpContext CreateContext(string path, string method)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = method;
        context.Response.Body = new MemoryStream();
        return context;
    }

    /// <summary>Tests for stub validator.</summary>
    private sealed class StubValidator : IPrivateLicenseValidator
    {
        private readonly PrivateLicenseStatus _status;

        /// <summary>Stub validator.</summary>
        /// <param name="status">Status.</param>
        public StubValidator(PrivateLicenseStatus status) => _status = status;

        /// <summary>Gets status.</summary>
        public PrivateLicenseStatus GetStatus() => _status;
    }
}
