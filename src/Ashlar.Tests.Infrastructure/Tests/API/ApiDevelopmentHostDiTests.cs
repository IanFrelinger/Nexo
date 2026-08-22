using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Ashlar.Tests.Infrastructure.Helpers;

namespace Ashlar.Tests.Infrastructure.Tests.API;

/// <summary>
/// Ensures the full API DI graph validates under Development (scoped/singleton rules, gRPC transport registration).
/// </summary>
[Trait("Category", "ProdStyle")]
public sealed class ApiDevelopmentHostDiTests
{
    [Fact(Timeout = TestTimeouts.HostTouching)]
    public async Task Development_host_di_graph_builds_without_scoped_singleton_violations()
    {
        await Task.CompletedTask;
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment(Microsoft.Extensions.Hosting.Environments.Development));

        // Building the root service provider runs Development-time DI validation (including GrpcAgentTransport).
        var services = factory.Services;
        services.Should().NotBeNull();
    }
}
