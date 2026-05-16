using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.ParallelTesting.Models;
using Nexo.Core.Application.ParallelTesting.Ports;
using Nexo.Core.Application.Paths;
using Nexo.Infrastructure;
using Nexo.Infrastructure.ParallelTesting;

namespace Nexo.CLI.Commands;

/// <summary>
/// Block 8: Run parallel test matrix; aggregate results; discover golden path.
/// </summary>
public sealed class TestParallelCommand : Command
{
    public TestParallelCommand() : base("parallel", "Run parallel test matrix (Block 8).")
    {
        var pathOpt = new Option<string?>("--path", "Solution or project path. Default: Nexo.Tests.Infrastructure.");
        var countOpt = new Option<int>("--count", () => 3, "Number of parallel instances");
        var filterOpt = new Option<string[]>("--filter", "Test filters to try");

        AddOption(pathOpt);
        AddOption(countOpt);
        AddOption(filterOpt);

        this.SetHandler(async (InvocationContext ctx) =>
        {
            var path = ctx.ParseResult.GetValueForOption(pathOpt);
            var count = ctx.ParseResult.GetValueForOption(countOpt);
            var filters = ctx.ParseResult.GetValueForOption(filterOpt) ?? Array.Empty<string>();
            await ExecuteAsync(path, count, filters);
        });
    }

    public static Command CreateCommand() => new TestParallelCommand();

    private static async Task ExecuteAsync(string? path, int count, string[] filters)
    {
        var repoRoot = RepoPathResolver.FindRepoRoot();
        var targetPath = path ?? Path.Combine(repoRoot, "src", "Nexo.Tests.Infrastructure", "Nexo.Tests.Infrastructure.csproj");
        if (!File.Exists(targetPath))
            targetPath = Path.Combine(repoRoot, "Nexo.sln");

        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole())
            .AddParallelTestingInfrastructure()
            .BuildServiceProvider();

        var spawner = services.GetRequiredService<IInstanceSpawner>();
        var matrixGen = services.GetRequiredService<IParameterMatrixGenerator>();
        var collector = services.GetRequiredService<IResultCollector>();

        var scenario = new Scenario
        {
            SolutionOrProjectPath = targetPath,
            Filters = filters.Length > 0 ? filters.ToList() : new List<string> { "" },
        };
        var paramSets = await matrixGen.GenerateAsync(scenario).ConfigureAwait(false);

        Console.WriteLine("Block 8: Parallel test matrix");
        Console.WriteLine($"  Target: {targetPath}");
        Console.WriteLine($"  Instances: {Math.Min(count, paramSets.Count)}");
        Console.WriteLine();

        var instances = await spawner.SpawnAsync(count, paramSets, targetPath).ConfigureAwait(false);
        var aggregated = await collector.CollectAsync(instances).ConfigureAwait(false);

        Console.WriteLine($"Results: {aggregated.Results.Count(i => i.Passed)}/{aggregated.Results.Count} passed");
        if (aggregated.BestCandidate != null)
            Console.WriteLine($"  Best: {aggregated.BestCandidate.InstanceId} (filter: {aggregated.BestCandidate.ParameterSet.Overrides.GetValueOrDefault("filter", "-")})");
        Environment.ExitCode = aggregated.AllPassed ? 0 : 1;
    }
}
