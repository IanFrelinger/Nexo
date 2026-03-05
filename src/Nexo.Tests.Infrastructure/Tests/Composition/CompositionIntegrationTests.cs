using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Composition.Ports;
using Nexo.Infrastructure;
using Nexo.Tests.Infrastructure.Helpers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Composition;

/// <summary>
/// Integration tests for composition engine with AddCompositionInfrastructure.
/// </summary>
[Trait("Category", "Integration")]
public sealed class CompositionIntegrationTests
{
    [Fact(Timeout = TestTimeouts.Integration)]
    public async Task CompositionEngine_WithRegistry_ReturnsComposedAgent()
    {
        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole())
            .AddCompositionInfrastructure()
            .BuildServiceProvider();

        var engine = services.GetRequiredService<ICompositionEngine>();
        var composed = await engine.ComposeAsync("test Nexo CLI", []);

        Assert.NotNull(composed);
        Assert.Contains("perception", composed.ComponentIds);
        Assert.Contains("validation", composed.ComponentIds);
        Assert.Contains("reporting", composed.ComponentIds);
    }
}
