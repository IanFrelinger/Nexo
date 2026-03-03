using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Composition.Ports;
using Nexo.Infrastructure;
using Nexo.Infrastructure.Composition;
using Nexo.Tests.Application.Helpers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Dogfood;

/// <summary>
/// Block 7 dogfood gate: compose an agent from capability components for a Nexo-related problem.
/// Validates ICompositionEngine returns a pipeline for "test Nexo CLI".
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "Dogfood")]
public sealed class DogfoodBlock7Tests : IDisposable
{
    private readonly IDisposable _tempDirCleanup;

    public DogfoodBlock7Tests()
    {
        (_, _tempDirCleanup) = TestHelpers.CreateTempDirectoryWithCleanup("nexo-dogfood-block7");
    }

    public void Dispose() => _tempDirCleanup.Dispose();

    [Fact(Timeout = 15000)]
    public async Task CompositionEngine_ComposeForTestNexoCli_ReturnsPipeline()
    {
        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .AddCompositionInfrastructure()
            .BuildServiceProvider();

        var engine = services.GetRequiredService<ICompositionEngine>();
        var capabilities = new[] { "perception", "validation", "reporting", "understanding", "code-analysis" };

        var composed = await engine.ComposeAsync("test Nexo CLI", capabilities);

        Assert.NotNull(composed);
        Assert.Equal("test Nexo CLI", composed.ProblemDescription);
        Assert.NotEmpty(composed.ComponentIds);
        Assert.Contains("perception", composed.ComponentIds, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("validation", composed.ComponentIds, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("reporting", composed.ComponentIds, StringComparer.OrdinalIgnoreCase);
    }
}
