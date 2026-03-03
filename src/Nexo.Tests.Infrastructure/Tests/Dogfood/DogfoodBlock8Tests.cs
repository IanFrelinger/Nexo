using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.ParallelTesting.Models;
using Nexo.Core.Application.ParallelTesting.Ports;
using Nexo.Core.Application.Paths;
using Nexo.Infrastructure;
using Nexo.Infrastructure.ParallelTesting;
using Nexo.Tests.Application.Helpers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Dogfood;

/// <summary>
/// Block 8 dogfood gate: run parallel test matrix against Nexo tests.
/// Validates IInstanceSpawner, IParameterMatrixGenerator, IResultCollector work on Nexo.Tests.Infrastructure.
/// Uses count=1 and a fast filter to keep runtime reasonable.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "Dogfood")]
public sealed class DogfoodBlock8Tests : IDisposable
{
    private readonly IDisposable _tempDirCleanup;

    public DogfoodBlock8Tests()
    {
        (_, _tempDirCleanup) = TestHelpers.CreateTempDirectoryWithCleanup("nexo-dogfood-block8");
    }

    public void Dispose() => _tempDirCleanup.Dispose();

    [Fact(Timeout = 60000)]
    public async Task ParallelTestMatrix_RunAgainstNexoTests_CompletesAndAggregates()
    {
        var repoRoot = RepoPathResolver.FindRepoRoot();
        var targetPath = Path.Combine(repoRoot, "src", "Nexo.Tests.Infrastructure", "Nexo.Tests.Infrastructure.csproj");
        Assert.True(File.Exists(targetPath), "Nexo.Tests.Infrastructure.csproj not found (run from Nexo repo)");

        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .AddParallelTestingInfrastructure()
            .BuildServiceProvider();

        var spawner = services.GetRequiredService<IInstanceSpawner>();
        var matrixGen = services.GetRequiredService<IParameterMatrixGenerator>();
        var collector = services.GetRequiredService<IResultCollector>();

        var scenario = new Scenario
        {
            SolutionOrProjectPath = targetPath,
            Filters = ["FullyQualifiedName~DogfoodBlock1Tests"],
        };
        var paramSets = await matrixGen.GenerateAsync(scenario);
        Assert.NotEmpty(paramSets);

        var instances = await spawner.SpawnAsync(1, paramSets, targetPath);
        Assert.NotEmpty(instances);

        var aggregated = await collector.CollectAsync(instances);
        Assert.NotEmpty(aggregated.Results);
        Assert.True(aggregated.AllPassed, $"Expected all passed; got {aggregated.Results.Count(r => r.Passed)}/{aggregated.Results.Count}");
    }
}
