using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Infrastructure.NodeCapabilityRuntime;
using Nexo.Infrastructure.NodeCapabilityRuntime.Backends;
using Nexo.Infrastructure.NodeCapabilityRuntime.Policies;
using System.Net;
using System.Text;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.NodeCapabilityRuntime;

public sealed class NcrStartupHealthServiceTests
{
    [Fact]
    public void Constructor_rejects_null_dependencies()
    {
        var backend = new NullModelServingBackend();
        var policy = new LinuxPolicy();
        var logger = NullLogger<NcrStartupHealthService>.Instance;

        var actBackend = () => new NcrStartupHealthService(null!, policy, logger);
        var actPolicy = () => new NcrStartupHealthService(backend, null!, logger);
        var actLogger = () => new NcrStartupHealthService(backend, policy, null!);

        actBackend.Should().Throw<ArgumentNullException>();
        actPolicy.Should().Throw<ArgumentNullException>();
        actLogger.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task StartAsync_DoesNotThrow_ForNonOllamaBackend()
    {
        var service = new NcrStartupHealthService(
            new NullModelServingBackend(),
            new LinuxPolicy(),
            NullLogger<NcrStartupHealthService>.Instance);

        var act = async () => await service.StartAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StartAsync_DoesNotThrow_WhenOllamaAvailable()
    {
        using var httpClient = new HttpClient(new TagsOkHandler());
        var service = new NcrStartupHealthService(
            new OllamaModelServingBackend(
                httpClient,
                Microsoft.Extensions.Options.Options.Create(new OllamaBackendOptions { BaseUrl = "http://127.0.0.1:11434" })),
            new LinuxPolicy(),
            NullLogger<NcrStartupHealthService>.Instance);

        await service.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_DoesNotThrow_WhenOllamaReturnsUnavailableStatus()
    {
        using var httpClient = new HttpClient(new StatusHandler(HttpStatusCode.ServiceUnavailable));
        var service = new NcrStartupHealthService(
            new OllamaModelServingBackend(
                httpClient,
                Microsoft.Extensions.Options.Options.Create(new OllamaBackendOptions { BaseUrl = "http://127.0.0.1:11434" })),
            new LinuxPolicy(),
            NullLogger<NcrStartupHealthService>.Instance);

        await service.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StopAsync_Completes()
    {
        var service = new NcrStartupHealthService(
            new NullModelServingBackend(),
            new LinuxPolicy(),
            NullLogger<NcrStartupHealthService>.Instance);

        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_DoesNotThrow_WhenOllamaUnavailable()
    {
        using var httpClient = new HttpClient(new ThrowingHandler());
        var service = new NcrStartupHealthService(
            new OllamaModelServingBackend(
                httpClient,
                Microsoft.Extensions.Options.Options.Create(new OllamaBackendOptions { BaseUrl = "http://127.0.0.1:11434" })),
            new LinuxPolicy(),
            NullLogger<NcrStartupHealthService>.Instance);

        var act = async () => await service.StartAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new HttpRequestException("No route to host");
    }

    private sealed class TagsOkHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"models":[]}""", Encoding.UTF8, "application/json"),
            });
    }

    private sealed class StatusHandler(HttpStatusCode status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status));
    }
}
