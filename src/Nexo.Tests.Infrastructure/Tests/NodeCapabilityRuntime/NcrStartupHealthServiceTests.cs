using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Infrastructure.NodeCapabilityRuntime;
using Nexo.Infrastructure.NodeCapabilityRuntime.Backends;
using Nexo.Infrastructure.NodeCapabilityRuntime.Policies;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.NodeCapabilityRuntime;

public sealed class NcrStartupHealthServiceTests
{
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
    public async Task StartAsync_DoesNotThrow_WhenOllamaUnavailable()
    {
        var service = new NcrStartupHealthService(
            new ThrowingOllamaBackend(),
            new LinuxPolicy(),
            NullLogger<NcrStartupHealthService>.Instance);

        var act = async () => await service.StartAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    private sealed class ThrowingOllamaBackend : OllamaModelServingBackend
    {
        public ThrowingOllamaBackend()
            : base(new HttpClient(new ThrowingHandler()), Microsoft.Extensions.Options.Options.Create(new OllamaBackendOptions()))
        {
        }

        private sealed class ThrowingHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => throw new HttpRequestException("No route to host");
        }
    }
}
