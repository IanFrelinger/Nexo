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
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure;
using Nexo.Core.Application.SelfContext.Ports;
using Nexo.Infrastructure.Adaptation;
using Nexo.Infrastructure.Analysis;
using Nexo.Infrastructure.Observation;
using Nexo.Infrastructure.SelfContext;

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
        var autonomyOpt = new Option<string>("--autonomy", () => "supervised", "Autonomy level: supervised | semi | full");
        var yesOpt = new Option<bool>("--yes", () => false, "Auto-approve all prompts (non-interactive, for CI/tests)");
        var skipRegressionOpt = new Option<bool>("--skip-regression", () => false, "Skip regression test after source fix (for CI/tests when fix is outside solution)");
        var storePathOpt = new Option<string?>("--store-path", "Directory for nexo dbs (default: repo root)");

        AddOption(pathOpt);
        AddOption(dryRunOpt);
        AddOption(autonomyOpt);
        AddOption(yesOpt);
        AddOption(skipRegressionOpt);
        AddOption(storePathOpt);

        this.SetHandler(async (InvocationContext ctx) =>
        {
            var path = ctx.ParseResult.GetValueForOption(pathOpt);
            var dryRun = ctx.ParseResult.GetValueForOption(dryRunOpt);
            var autonomy = ctx.ParseResult.GetValueForOption(autonomyOpt) ?? "supervised";
            var yes = ctx.ParseResult.GetValueForOption(yesOpt);
            var skipRegression = ctx.ParseResult.GetValueForOption(skipRegressionOpt);
            var storePathOverride = ctx.ParseResult.GetValueForOption(storePathOpt);
            await ExecuteAsync(path, dryRun, autonomy, yes, skipRegression, storePathOverride);
        });
    }

    private static async Task ExecuteAsync(string? path, bool dryRun, string autonomy = "supervised", bool yes = false, bool skipRegression = false, string? storePathOverride = null)
    {
        var repoRoot = RepoPathResolver.FindRepoRoot();
        var storePath = !string.IsNullOrWhiteSpace(storePathOverride)
            ? Path.Combine(Path.GetFullPath(storePathOverride), "nexo-patterns.db")
            : Path.Combine(repoRoot, "nexo-patterns.db");
        var targetPath = path ?? RepoPathResolver.FindBlock1ObservationPath(repoRoot);

        var serviceCollection = new ServiceCollection()
            .AddLogging(b => b.AddConsole())
            .AddCodeAnalyzers()
            .AddAdaptationInfrastructure(storePath)
            .AddSelfContextInfrastructure(storePath);

        if (yes)
            serviceCollection.AddSingleton<IUserFeedbackCapture>(new AutoApproveUserFeedbackCapture());

        var services = serviceCollection.BuildServiceProvider();

        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger<ImproveCommand>();
        var analyzer = services.GetRequiredService<IBrickStaticAnalyzer>();
        var decomposer = services.GetRequiredService<IBrickDecomposer>();
        var fixGenerator = services.GetRequiredService<IFixGenerator>();
        var recompiler = services.GetRequiredService<IBrickRecompiler>();
        var sourceFixer = services.GetRequiredService<ISourceCodeFixer>();
        var regressionRunner = services.GetRequiredService<IRegressionTestRunner>();
        var adaptationLog = services.GetRequiredService<IAdaptationLog>();
        var adaptationPromoter = services.GetRequiredService<IAdaptationPromoter>();
        var rollbackHelper = services.GetRequiredService<AdaptationRollbackHelper>();
        var userFeedback = services.GetRequiredService<IUserFeedbackCapture>();
        var executionTracer = services.GetRequiredService<IExecutionTracer>();
        var auditLog = services.GetRequiredService<IAdaptationAuditLog>();
        var envelope = new PermissionEnvelope(autonomy);

        Console.WriteLine("Block 4: Closed-loop improve");
        Console.WriteLine($"  Path: {targetPath}");
        Console.WriteLine($"  Mode: {(dryRun ? "dry-run (report only)" : "analyze + adapt")}");
        Console.WriteLine($"  Autonomy: {autonomy}");
        Console.WriteLine();

        await executionTracer.TraceAsync("improve.start", new Dictionary<string, object> { ["path"] = targetPath, ["dryRun"] = dryRun }, path: targetPath).ConfigureAwait(false);

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

        // Source-level fixes (e.g. EmptyCatch) with validate-before-promote
        int sourceFixes = 0;
        if (!dryRun)
        {
            var solutionPath = Path.Combine(repoRoot, "Nexo.sln");
            foreach (var v in analysisResult.Violations)
            {
                var suggestion = $"Fix {v.Rule} in {Path.GetFileName(v.FilePath)}";
                if (!envelope.CanApplySourceFix && !await userFeedback.ApproveAsync(suggestion).ConfigureAwait(false))
                    continue;

                rollbackHelper.Snapshot(v.FilePath);
                if (!await sourceFixer.TryFixAsync(v).ConfigureAwait(false))
                {
                    rollbackHelper.Clear();
                    continue;
                }
                var regResult = skipRegression
                    ? new RegressionTestResult { AllPassed = true, PassedCount = 0, FailedCount = 0, Summary = "Skipped" }
                    : await regressionRunner.RunAsync(solutionPath).ConfigureAwait(false);
                if (regResult.AllPassed)
                {
                    var promoted = envelope.CanPromoteWithoutApproval || await userFeedback.ApproveAsync($"Regression passed. Promote: {suggestion}?").ConfigureAwait(false);
                    if (promoted)
                    {
                        sourceFixes++;
                        var record = new AdaptationRecord
                        {
                            Id = Guid.NewGuid().ToString("N"),
                            Timestamp = DateTimeOffset.UtcNow,
                            BrickId = ViolationToBrickMapper.GetBrickIdForFile(v.FilePath),
                            FailureType = v.Rule,
                            FixApplied = AdaptationFixType.Source,
                            FilePath = v.FilePath,
                            RegressionPassed = true,
                            Promoted = true,
                            Message = $"Fixed {v.Rule} in {Path.GetFileName(v.FilePath)}",
                        };
                        await adaptationPromoter.PromoteAsync(record).ConfigureAwait(false);
                        await auditLog.LogAsync(new AdaptationAuditEntry
                        {
                            Id = record.Id,
                            Timestamp = record.Timestamp,
                            AutonomyLevel = autonomy,
                            Outcome = "Promoted",
                            BrickId = record.BrickId,
                            FailureType = record.FailureType,
                            FilePath = record.FilePath,
                            RegressionPassed = true,
                            Promoted = true,
                            Message = record.Message,
                        }).ConfigureAwait(false);
                    }
                    else
                    {
                        rollbackHelper.Rollback(v.FilePath);
                        await auditLog.LogAsync(new AdaptationAuditEntry
                        {
                            Id = Guid.NewGuid().ToString("N"),
                            Timestamp = DateTimeOffset.UtcNow,
                            AutonomyLevel = autonomy,
                            Outcome = "Rejected",
                            BrickId = ViolationToBrickMapper.GetBrickIdForFile(v.FilePath),
                            FailureType = v.Rule,
                            FilePath = v.FilePath,
                            RegressionPassed = true,
                            Promoted = false,
                            Message = "User rejected promotion",
                        }).ConfigureAwait(false);
                    }
                    rollbackHelper.Clear();
                }
                else
                {
                    rollbackHelper.Rollback(v.FilePath);
                    var record = new AdaptationRecord
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        Timestamp = DateTimeOffset.UtcNow,
                        BrickId = ViolationToBrickMapper.GetBrickIdForFile(v.FilePath),
                        FailureType = v.Rule,
                        FixApplied = AdaptationFixType.Source,
                        FilePath = v.FilePath,
                        RegressionPassed = false,
                        Promoted = false,
                        Message = $"Rollback: regression failed after {v.Rule} fix",
                    };
                    await adaptationLog.LogAsync(record).ConfigureAwait(false);
                    await auditLog.LogAsync(new AdaptationAuditEntry
                    {
                        Id = record.Id,
                        Timestamp = record.Timestamp,
                        AutonomyLevel = autonomy,
                        Outcome = "Rollback",
                        BrickId = record.BrickId,
                        FailureType = record.FailureType,
                        FilePath = record.FilePath,
                        RegressionPassed = false,
                        Promoted = false,
                        Message = record.Message,
                    }).ConfigureAwait(false);
                }
            }
            if (sourceFixes > 0)
            {
                Console.WriteLine($"Applied {sourceFixes} source-level fix(s).");
                await executionTracer.TraceAsync("improve.end", null, targetPath, "source_fixes").ConfigureAwait(false);
                Environment.ExitCode = 0;
                return;
            }
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

        var brickRegistry = services.GetRequiredService<IBrickRegistry>();
        int brickAdapted = 0;
        foreach (var (brickId, violations) in violationsByBrick)
        {
            var brick = brickRegistry.GetBrick(brickId);
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

        var outcome = analysisResult.Passed ? "passed" : "violations";
        await executionTracer.TraceAsync("improve.end", null, targetPath, outcome).ConfigureAwait(false);
        Environment.ExitCode = analysisResult.Passed ? 0 : 1;
    }
}
