using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Nexo.API.Security;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.API;

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
            Options.Create(new NexoPrivateLicenseOptions { EnforceLicense = false }),
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
            Options.Create(new NexoPrivateLicenseOptions
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
            Options.Create(new NexoPrivateLicenseOptions
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

    private sealed class StubValidator : IPrivateLicenseValidator
    {
        private readonly PrivateLicenseStatus _status;

        public StubValidator(PrivateLicenseStatus status) => _status = status;

        public PrivateLicenseStatus GetStatus() => _status;
    }
}
