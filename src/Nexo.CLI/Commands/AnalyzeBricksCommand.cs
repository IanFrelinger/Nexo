using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Analysis.Models;
using Nexo.Core.Application.Analysis.Ports;
using Nexo.Infrastructure.Analysis;

namespace Nexo.CLI.Commands;

/// <summary>
/// Analyzes brick/generated code for schema, safety, and performance.
/// Dogfood: run against Block 1 (Observation) code first.
/// </summary>
public sealed class AnalyzeBricksCommand : Command
{
    public AnalyzeBricksCommand() : base("bricks", "Analyze brick code (schema, safety, performance). Dogfood: defaults to Block 1 Observation path.")
    {
        var pathOpt = new Option<string?>("--path", "Path to analyze (dir or .cs file). Default: Nexo Observation folders.");
        AddOption(pathOpt);
        this.SetHandler(async (InvocationContext ctx) =>
        {
            var path = ctx.ParseResult.GetValueForOption(pathOpt);
            await ExecuteAsync(path);
        });
    }

    private static async Task ExecuteAsync(string? path)
    {
        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole())
            .AddCodeAnalyzers()
            .BuildServiceProvider();

        var analyzer = services.GetRequiredService<IBrickStaticAnalyzer>();
        var targetPath = path ?? FindBlock1ObservationPath();

        Console.WriteLine($"Brick static analyzer (Block 2)");
        Console.WriteLine($"  Target: {targetPath}");
        Console.WriteLine();

        var result = await analyzer.AnalyzeSourceAsync(targetPath).ConfigureAwait(false);

        if (result.Passed)
        {
            Console.WriteLine("No violations found.");
            Environment.ExitCode = 0;
            return;
        }

        Console.WriteLine($"Found {result.TotalViolations} violation(s):");
        foreach (var v in result.Violations)
        {
            var loc = v.LineNumber.HasValue ? $"{v.FilePath}:{v.LineNumber}" : v.FilePath;
            Console.WriteLine($"  [{v.Rule}] {loc}");
            Console.WriteLine($"    {v.Message}");
        }

        Environment.ExitCode = 1;
    }

    private static string FindBlock1ObservationPath()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Nexo.sln")))
            {
                var infraObs = Path.Combine(dir.FullName, "src", "Nexo.Infrastructure", "Observation");
                var bgObs = Path.Combine(dir.FullName, "src", "Nexo.BackgroundAgents", "Observation");
                if (Directory.Exists(infraObs))
                    return infraObs;
                if (Directory.Exists(bgObs))
                    return bgObs;
            }
            dir = dir.Parent;
        }
        return Directory.GetCurrentDirectory();
    }
}
