using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ashlar.Core.Application.Composition.Ports;
using Ashlar.Core.Application.ParallelTesting.Models;
using Ashlar.Core.Application.Paths;
using Ashlar.Infrastructure;
using Ashlar.Infrastructure.Composition;
using Ashlar.Tests.Application.Helpers;
using Ashlar.Tests.Infrastructure.Helpers;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Dogfood;

/// <summary>
/// Phase D: Composition-driven testing. Validates Ashlar's test suite runs through a composed agent
/// (test-discovery → test-execution → result-aggregation) via IComposedTestRunner.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "Dogfood")]
public sealed class DogfoodBlock8ComposedTests : IDisposable
{
    private readonly IDisposable _tempDirCleanup;

    public DogfoodBlock8ComposedTests()
    {
        (_, _tempDirCleanup) = TestHelpers.CreateTempDirectoryWithCleanup("ashlar-dogfood-block8-composed");
    }

    public void Dispose() => _tempDirCleanup.Dispose();

    [NotOnCiFact("spawns nested dotnet test hosts against the repo checkout", Timeout = 60000)]
    public async Task ComposedTestRunner_RunAshlarTestsViaComposition_CompletesAndAggregates()
    {
        var repoRoot = RepoPathResolver.FindRepoRoot();
        var targetPath = Path.Combine(repoRoot, "src", "Ashlar.Tests.Infrastructure", "Ashlar.Tests.Infrastructure.csproj");
        Assert.True(File.Exists(targetPath), "Ashlar.Tests.Infrastructure.csproj not found (run from Ashlar repo)");

        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .AddCompositionInfrastructure()
            .AddParallelTestingInfrastructure()
            .AddComposedTestRunner()
            .BuildServiceProvider();

        var runner = services.GetRequiredService<IComposedTestRunner>();

        var scenario = new Scenario
        {
            SolutionOrProjectPath = targetPath,
            Filters = ["FullyQualifiedName~DogfoodBlock1Tests"],
        };

        var result = await runner.RunViaCompositionAsync("test Ashlar suite", scenario, instanceCount: 1);

        Assert.NotNull(result);
        Assert.NotEmpty(result.Results);
        Assert.True(result.AllPassed, $"Expected all passed; got {result.Results.Count(r => r.Passed)}/{result.Results.Count}");
        Assert.NotNull(result.BestCandidate);
    }

    /// <summary>
    /// D4: Validates composed agent integrates with parallel test infra; golden path (BestCandidate) is discovered.
    /// </summary>
    [NotOnCiFact("spawns nested dotnet test hosts against the repo checkout", Timeout = 90000)]
    public async Task ComposedTestRunner_WithMultipleInstances_AggregatesAndFindsGoldenPath()
    {
        var repoRoot = RepoPathResolver.FindRepoRoot();
        var targetPath = Path.Combine(repoRoot, "src", "Ashlar.Tests.Infrastructure", "Ashlar.Tests.Infrastructure.csproj");
        Assert.True(File.Exists(targetPath), "Ashlar.Tests.Infrastructure.csproj not found (run from Ashlar repo)");

        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .AddCompositionInfrastructure()
            .AddParallelTestingInfrastructure()
            .AddComposedTestRunner()
            .BuildServiceProvider();

        var runner = services.GetRequiredService<IComposedTestRunner>();

        var scenario = new Scenario
        {
            SolutionOrProjectPath = targetPath,
            Filters = ["FullyQualifiedName~DogfoodBlock1Tests"],
        };

        var result = await runner.RunViaCompositionAsync("test Ashlar suite", scenario, instanceCount: 2);

        Assert.NotNull(result);
        Assert.NotEmpty(result.Results);
        Assert.True(result.AllPassed);
        Assert.NotNull(result.BestCandidate);
        Assert.True(result.BestCandidate.Passed);
    }
}
