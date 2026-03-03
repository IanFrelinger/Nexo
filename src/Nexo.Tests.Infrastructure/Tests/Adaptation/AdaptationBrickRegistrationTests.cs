using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Execution;
using Nexo.Bricks.Owasp.Security;
using Nexo.Infrastructure;
using Nexo.Infrastructure.Adaptation;
using Nexo.Infrastructure.Execution;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Adaptation;

[Trait("Category", "Adaptation")]
public sealed class AdaptationBrickRegistrationTests
{
    [Fact]
    public void AddAdaptationBricks_RegistersBrickInRegistry()
    {
        var storePath = Path.Combine(Path.GetTempPath(), $"nexo-adapt-reg-{Guid.NewGuid():N}.db");
        try
        {
            var services = new ServiceCollection()
                .AddLogging(b => b.AddConsole())
                .AddSingleton<IProviderFactory, ProviderFactory>()
                .AddAdaptationInfrastructure(storePath)
                .AddAdaptationBricks(typeof(OWASPScannerBrick))
                .BuildServiceProvider();

            var registry = services.GetRequiredService<Nexo.Core.Domain.Execution.IBrickRegistry>();
            var brick = registry.GetBrick("owasp-scanner");

            brick.Should().NotBeNull();
            brick!.Id.Should().Be("owasp-scanner");
        }
        finally
        {
            if (File.Exists(storePath))
                File.Delete(storePath);
        }
    }

    [Fact]
    public void AddAdaptationBricks_EmptyTypes_DoesNotThrow()
    {
        var services = new ServiceCollection()
            .AddAdaptationInfrastructure()
            .AddAdaptationBricks()
            .BuildServiceProvider();

        var registry = services.GetRequiredService<Nexo.Core.Domain.Execution.IBrickRegistry>();
        registry.GetAllBricks().Should().BeEmpty();
    }
}
