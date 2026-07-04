using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Agents;
using Nexo.Core.Domain.Execution;
using Nexo.Bricks.Owasp.Security;
using Nexo.Hosting;
using Nexo.Hosting.Sdk;
using Nexo.Infrastructure.Execution;
using Nexo.Tests.Infrastructure.Helpers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.SDK;
/// <summary>
/// P2.1: SDK registration tests.
/// </summary>
[Trait("Category", "Integration")]
public sealed class NexoSdkTests : TempDirTestBase
{
    private readonly string _storePath;

    public NexoSdkTests() : base("nexo-sdk")
    {
        _storePath = Path.Combine(TempDir, "store.db");
    }

    [Fact]
    public void AddNexoSdk_RegisterBrick_BrickAppearsInRegistry()
    {
        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole())
            .AddSingleton<IProviderFactory, ProviderFactory>()
            .AddNexoSdk(sdk => sdk.RegisterBrick<OWASPScannerBrick>())
            .AddNexo(o => o.PatternStorePath = _storePath)
            .BuildServiceProvider();

        var registry = services.GetRequiredService<Nexo.Core.Domain.Execution.IBrickRegistry>();
        var brick = registry.GetBrick("owasp-scanner");

        brick.Should().NotBeNull();
        brick!.Id.Should().Be("owasp-scanner");
    }

    [Fact]
    public void AddNexoSdk_RegisterAgentCard_CardAppearsInAgentRegistry()
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
            .AddNexoSdk(sdk => sdk.RegisterAgentCard(card))
            .AddNexo(o => o.PatternStorePath = _storePath)
            .BuildServiceProvider();

        var registry = services.GetRequiredService<Nexo.Core.Domain.Execution.IAgentRegistry>();
        var resolved = registry.GetAgent("sdk-test-agent");

        resolved.Should().NotBeNull();
        resolved!.Id.Should().Be("sdk-test-agent");
        resolved.Name.Should().Be("SDK Test Agent");
    }

    [Fact]
    public void AddNexoSdk_BeforeAddNexo_FullKernelResolves()
    {
        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole())
            .AddNexoSdk(sdk => sdk.RegisterBrick<OWASPScannerBrick>())
            .AddNexo(o => o.PatternStorePath = _storePath)
            .BuildServiceProvider();

        var validationService = services.GetRequiredService<Nexo.Core.Application.Validation.Ports.IValidationService>();
        var brickRegistry = services.GetRequiredService<Nexo.Core.Domain.Execution.IBrickRegistry>();

        validationService.Should().NotBeNull();
        brickRegistry.Should().NotBeNull();
        brickRegistry.GetBrick("owasp-scanner").Should().NotBeNull();
    }
}
