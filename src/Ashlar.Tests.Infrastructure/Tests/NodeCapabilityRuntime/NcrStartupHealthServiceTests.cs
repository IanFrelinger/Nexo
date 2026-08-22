using Ashlar.Agents.TestKit;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.Infrastructure.NodeCapabilityRuntime;
using Ashlar.Infrastructure.NodeCapabilityRuntime.Backends;
using Ashlar.Infrastructure.NodeCapabilityRuntime.Policies;
using System.Net;
using System.Text;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.NodeCapabilityRuntime;

/// <summary>Tests for ncr startup health service.</summary>
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
        using var httpClient = new HttpClient(StubHttpMessageHandler.Always(HttpStatusCode.OK, """{"models":[]}"""));
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
        using var httpClient = new HttpClient(StubHttpMessageHandler.Always(HttpStatusCode.ServiceUnavailable));
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
        using var httpClient = new HttpClient(StubHttpMessageHandler.Throws(new HttpRequestException("No route to host")));
        var service = new NcrStartupHealthService(
            new OllamaModelServingBackend(
                httpClient,
                Microsoft.Extensions.Options.Options.Create(new OllamaBackendOptions { BaseUrl = "http://127.0.0.1:11434" })),
            new LinuxPolicy(),
            NullLogger<NcrStartupHealthService>.Instance);

        var act = async () => await service.StartAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    /// <summary>Tests for throwing handler.</summary>

    /// <summary>Tests for tags ok handler.</summary>

    /// <summary>Tests for status handler.</summary>
}
