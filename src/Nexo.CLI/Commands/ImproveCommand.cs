using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Application.Analysis.Models;
using Nexo.Core.Application.Analysis.Ports;
using Nexo.Core.Application.Observation.Ports;
using Nexo.Core.Application.Paths;
using Nexo.Core.Domain.Bricks;
using Nexo.Infrastructure;
using Nexo.Infrastructure.Adaptation;
using Nexo.Infrastructure.Analysis;
using Nexo.Infrastructure.Observation;

namespace Nexo.CLI.Commands;

/// <summary>
/// Block 4: Closed-loop improve. Runs analyze bricks → adapt from violations.
/// Dogfood: observe → analyze → adapt.
/// </summary>
public sealed class ImproveCommand : Command
{
    public ImproveCommand() : base("improve", "Analyze brick code, then run adaptation for each violation (Block 4 closed-loop).")
    {
        var pathOpt = new Option<string?>("--path", "Path to analyze. Default: Block 1 Observation folders.");
        var dryRunOpt = new Option<bool>("--dry-run", () => false, "Only report; do not run adaptation");

        AddOption(pathOpt);
        AddOption(dryRunOpt);

        this.SetHandler(async (InvocationContext ctx) =>
        {
            var path = ctx.ParseResult.GetValueForOption(pathOpt);
            var dryRun = ctx.ParseResult.GetValueForOption(dryRunOpt);
            await ExecuteAsync(path, dryRun);
        });
    }

    private static async Task ExecuteAsync(string? path, bool dryRun)
    {
        var repoRoot = RepoPathResolver.FindRepoRoot();
        var storePath = Path.Combine(repoRoot, "nexo-patterns.db");
        var targetPath = path ?? RepoPathResolver.FindBlock1ObservationPath(repoRoot);

        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole())
            .AddCodeAnalyzers()
            .AddAdaptationInfrastructure(storePath)
            .BuildServiceProvider();

        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger<ImproveCommand>();
        var analyzer = services.GetRequiredService<IBrickStaticAnalyzer>();
        var decomposer = services.GetRequiredService<IBrickDecomposer>();
        var fixGenerator = services.GetRequiredService<IFixGenerator>();
        var recompiler = services.GetRequiredService<IBrickRecompiler>();
        var sourceFixer = services.GetRequiredService<ISourceCodeFixer>();

        Console.WriteLine("Block 4: Closed-loop improve");
        Console.WriteLine($"  Path: {targetPath}");
        Console.WriteLine($"  Mode: {(dryRun ? "dry-run (report only)" : "analyze + adapt")}");
        Console.WriteLine();

        var analysisResult = await analyzer.AnalyzeSourceAsync(targetPath).ConfigureAwait(false);

        if (analysisResult.Passed)
        {
            Console.WriteLine("No violations found.");
            Environment.ExitCode = 0;
            return;
        }

        var violationsByBrick = new Dictionary<string, List<(string Rule, string FilePath, int? Line)>>(StringComparer.OrdinalIgnoreCase);

        foreach (var v in analysisResult.Violations)
        {
            var brickId = ViolationToBrickMapper.GetBrickIdForFile(v.FilePath);
            if (brickId == null) continue;

            if (!violationsByBrick.TryGetValue(brickId, out var list))
            {
                list = new List<(string, string, int?)>();
                violationsByBrick[brickId] = list;
            }
            list.Add((v.Rule, v.FilePath, v.LineNumber));
        }

        Console.WriteLine($"Found {analysisResult.TotalViolations} violation(s). {violationsByBrick.Sum(x => x.Value.Count)} in known bricks.");
        foreach (var kv in violationsByBrick)
        {
            Console.WriteLine($"  {kv.Key}: {kv.Value.Count} violation(s)");
            foreach (var (rule, filePath, line) in kv.Value.DistinctBy(x => (x.Rule, x.FilePath, x.Line)))
                Console.WriteLine($"    [{rule}] {Path.GetFileName(filePath)}{(line.HasValue ? $":{line}" : "")}");
        }
        Console.WriteLine();

        // Source-level fixes (e.g. EmptyCatch) apply to all files
        int sourceFixes = 0;
        if (!dryRun)
        {
            foreach (var v in analysisResult.Violations)
            {
                if (await sourceFixer.TryFixAsync(v).ConfigureAwait(false))
                    sourceFixes++;
            }
            if (sourceFixes > 0)
                Console.WriteLine($"Applied {sourceFixes} source-level fix(s).");
        }

        if (violationsByBrick.Count == 0)
        {
            Console.WriteLine(dryRun
                ? "(dry-run: skip source fixes)"
                : "No violations in known bricks. Add brick mapping in ViolationToBrickMapper to enable brick adaptation.");
            Environment.ExitCode = 1;
            return;
        }

        if (dryRun)
        {
            Console.WriteLine("(dry-run: skip brick adaptation)");
            Environment.ExitCode = 1;
            return;
        }

        int brickAdapted = 0;
        foreach (var (brickId, violations) in violationsByBrick)
        {
            Brick? brick = brickId == "observation.context"
                ? new ObservationContextBrick(services.GetRequiredService<IContextAssembler>())
                : null;

            if (brick == null) continue;

            var manifest = await decomposer.DecomposeAsync(brick).ConfigureAwait(false);
            var uniqueRules = violations.Select(x => x.Rule).Distinct().ToList();

            foreach (var rule in uniqueRules)
            {
                var context = new FailureContext
                {
                    FailureType = rule,
                    TargetId = brickId,
                    Message = $"From analysis: {rule}",
                };
                var fixes = await fixGenerator.GenerateFixesAsync(context, manifest).ConfigureAwait(false);
                if (fixes.Count == 0) continue;

                var recompiled = await recompiler.RecompileAsync(fixes[0]).ConfigureAwait(false);
                if (recompiled != null)
                {
                    brickAdapted++;
                    logger.LogInformation("Adapted {Brick} for {Rule}", brickId, rule);
                }
            }
        }

        if (brickAdapted > 0)
            Console.WriteLine($"Applied {brickAdapted} brick adaptation(s).");
        Environment.ExitCode = analysisResult.Passed ? 0 : 1;
    }
}
