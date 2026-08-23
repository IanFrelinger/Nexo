using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ashlar.Core.Domain.Execution;
using Ashlar.Bricks.Owasp.Security;
using Ashlar.Infrastructure;
using Ashlar.Infrastructure.Adaptation;
using Ashlar.Infrastructure.Execution;
using Ashlar.Tests.Infrastructure.Helpers;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Adaptation;

/// <summary>Tests for adaptation brick registration.</summary>
[Trait("Category", "Adaptation")]
public sealed class AdaptationBrickRegistrationTests : TempDirTestBase
{
    public AdaptationBrickRegistrationTests() : base("ashlar-adapt-reg") { }

    [Fact]
    public void AddAdaptationBricks_RegistersBrickInRegistry()
    {
        var storePath = Path.Combine(TempDir, "adapt.db");
        var services = new ServiceCollection()
                .AddLogging(b => b.AddConsole())
                .AddSingleton<IProviderFactory, ProviderFactory>()
                .AddAdaptationInfrastructure(storePath)
                .AddAdaptationBricks(typeof(OWASPScannerBrick))
                .BuildServiceProvider();

            var registry = services.GetRequiredService<Ashlar.Core.Domain.Execution.IBrickRegistry>();
            var brick = registry.GetBrick("owasp-scanner");

            brick.Should().NotBeNull();
            brick!.Id.Should().Be("owasp-scanner");
    }

    [Fact]
    public void AddAdaptationBricks_EmptyTypes_DoesNotThrow()
    {
        var services = new ServiceCollection()
            .AddAdaptationInfrastructure()
            .AddAdaptationBricks()
            .BuildServiceProvider();

        var registry = services.GetRequiredService<Ashlar.Core.Domain.Execution.IBrickRegistry>();
        registry.GetAllBricks().Should().BeEmpty();
    }
}
