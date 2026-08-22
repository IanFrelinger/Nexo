using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ashlar.Core.Application.Composition.Ports;
using Ashlar.Infrastructure;
using Ashlar.Infrastructure.Composition;
using Ashlar.Tests.Application.Helpers;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Dogfood;

/// <summary>
/// Block 7 dogfood gate: compose an agent from capability components for a Ashlar-related problem.
/// Validates ICompositionEngine returns a pipeline for "test Ashlar CLI".
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "Dogfood")]
public sealed class DogfoodBlock7Tests : IDisposable
{
    private readonly IDisposable _tempDirCleanup;

    public DogfoodBlock7Tests()
    {
        (_, _tempDirCleanup) = TestHelpers.CreateTempDirectoryWithCleanup("ashlar-dogfood-block7");
    }

    public void Dispose() => _tempDirCleanup.Dispose();

    [Fact(Timeout = 15000)]
    public async Task CompositionEngine_ComposeForTestAshlarCli_ReturnsPipeline()
    {
        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .AddCompositionInfrastructure()
            .BuildServiceProvider();

        var engine = services.GetRequiredService<ICompositionEngine>();
        var capabilities = new[] { "perception", "validation", "reporting", "understanding", "code-analysis" };

        var composed = await engine.ComposeAsync("test Ashlar CLI", capabilities);

        Assert.NotNull(composed);
        Assert.Equal("test Ashlar CLI", composed.ProblemDescription);
        Assert.NotEmpty(composed.ComponentIds);
        Assert.Contains("perception", composed.ComponentIds, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("validation", composed.ComponentIds, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("reporting", composed.ComponentIds, StringComparer.OrdinalIgnoreCase);
    }
}
