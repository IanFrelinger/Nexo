using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ashlar.Core.Domain.Agents;
using Ashlar.Core.Domain.Execution;
using Ashlar.Bricks.Owasp.Security;
using Ashlar.Hosting;
using Ashlar.Hosting.Sdk;
using Ashlar.Infrastructure.Execution;
using Ashlar.Tests.Infrastructure.Helpers;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.SDK;
/// <summary>
/// P2.1: SDK registration tests.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AshlarSdkTests : TempDirTestBase
{
    private readonly string _storePath;

    public AshlarSdkTests() : base("ashlar-sdk")
    {
        _storePath = Path.Combine(TempDir, "store.db");
    }

    [Fact]
    public void AddAshlarSdk_RegisterBrick_BrickAppearsInRegistry()
    {
        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole())
            .AddSingleton<IProviderFactory, ProviderFactory>()
            .AddAshlarSdk(sdk => sdk.RegisterBrick<OWASPScannerBrick>())
            .AddAshlar(o => o.PatternStorePath = _storePath)
            .BuildServiceProvider();

        var registry = services.GetRequiredService<Ashlar.Core.Domain.Execution.IBrickRegistry>();
        var brick = registry.GetBrick("owasp-scanner");

        brick.Should().NotBeNull();
        brick!.Id.Should().Be("owasp-scanner");
    }

    [Fact]
    public void AddAshlarSdk_RegisterAgentCard_CardAppearsInAgentRegistry()
    {
        var card = new AgentCard
        {
            Id = "sdk-test-agent",
            Name = "SDK Test Agent",
            Domain = "test",
            Description = "Test agent card from SDK",
        };

        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole())
            .AddAshlarSdk(sdk => sdk.RegisterAgentCard(card))
            .AddAshlar(o => o.PatternStorePath = _storePath)
            .BuildServiceProvider();

        var registry = services.GetRequiredService<Ashlar.Core.Domain.Execution.IAgentRegistry>();
        var resolved = registry.GetAgent("sdk-test-agent");

        resolved.Should().NotBeNull();
        resolved!.Id.Should().Be("sdk-test-agent");
        resolved.Name.Should().Be("SDK Test Agent");
    }

    [Fact]
    public void AddAshlarSdk_BeforeAddAshlar_FullKernelResolves()
    {
        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole())
            .AddAshlarSdk(sdk => sdk.RegisterBrick<OWASPScannerBrick>())
            .AddAshlar(o => o.PatternStorePath = _storePath)
            .BuildServiceProvider();

        var validationService = services.GetRequiredService<Ashlar.Core.Application.Validation.Ports.IValidationService>();
        var brickRegistry = services.GetRequiredService<Ashlar.Core.Domain.Execution.IBrickRegistry>();

        validationService.Should().NotBeNull();
        brickRegistry.Should().NotBeNull();
        brickRegistry.GetBrick("owasp-scanner").Should().NotBeNull();
    }
}
