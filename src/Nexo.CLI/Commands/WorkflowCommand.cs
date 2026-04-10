using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Nexo.CLI.Runtime;
using Nexo.Orchestration.Models;

namespace Nexo.CLI.Commands;

/// <summary>
/// First-class workflow lab command for composing and stress-testing orchestrated agent workflows.
/// </summary>
public sealed class WorkflowCommand : Command
{
    internal delegate Task<ScenarioExecutionResult> ScenarioExecutor(
        string request,
        string runtimeSpecJson,
        string? provider,
        bool outputJson,
        bool verbose,
        CancellationToken cancellationToken);

    private readonly ScenarioExecutor _scenarioExecutor;

    public WorkflowCommand(Func<OrchestrateCommand> orchestrateFactory)
        : this(async (request, runtimeSpecJson, provider, outputJson, verbose, ct) =>
        {
            var orchestrate = orchestrateFactory();
            return await ExecuteScenarioAsync(orchestrate, request, runtimeSpecJson, provider, outputJson, verbose, ct).ConfigureAwait(false);
        })
    {
    }

    internal WorkflowCommand(ScenarioExecutor scenarioExecutor)
        : base("workflow", "Scaffold and stress-test agentic workflow compositions.")
    {
        _scenarioExecutor = scenarioExecutor ?? throw new ArgumentNullException(nameof(scenarioExecutor));
        ConfigureScaffoldCommand();
        ConfigureStressCommand();
        ConfigureHistoryCommand();
        ConfigureReportCommand();
    }

    private void ConfigureScaffoldCommand()
    {
        var scaffold = new Command("scaffold", "Write a workflow lab runtime spec template.");
        var outputOpt = new Option<string>(
            "--output",
            () => Path.Combine(Environment.CurrentDirectory, ".nexo", "workflow", "workflow_lab.runtime.json"),
            "Destination path for scaffolded workflow lab runtime spec.");
        var forceOpt = new Option<bool>("--force", () => false, "Overwrite output if it already exists.");
        var jsonOpt = new Option<bool>("--json", () => false, "Emit machine-readable JSON output.");
        scaffold.AddOption(outputOpt);
        scaffold.AddOption(forceOpt);
        scaffold.AddOption(jsonOpt);
        scaffold.SetHandler((InvocationContext ctx) =>
        {
            var exitCode = ExecuteScaffoldAsync(
                ctx.ParseResult.GetValueForOption(outputOpt) ?? string.Empty,
                ctx.ParseResult.GetValueForOption(forceOpt),
                ctx.ParseResult.GetValueForOption(jsonOpt)).GetAwaiter().GetResult();
            ctx.ExitCode = exitCode;
        });
        AddCommand(scaffold);
    }

    private void ConfigureStressCommand()
    {
        var stress = new Command("stress", "Execute a workflow composition/model stress matrix.");
        var requestOverrideOpt = new Option<string?>(
            "--request",
            () => null,
            "Optional request override used for all scenarios.");
        var specPathOpt = new Option<string?>(
            "--spec",
            () => null,
            "Path to workflow lab runtime spec JSON (defaults to .nexo/workflow/workflow_lab.runtime.json).");
        var specJsonOpt = new Option<string?>("--spec-json", () => null, "Inline workflow lab runtime spec JSON.");
        var providerOpt = new Option<string?>(
            "--provider",
            () => null,
            "Override provider for all profile entries unless explicitly set.");
        var preferOpt = new Option<string?>(
            "--prefer",
            () => null,
            "Override model preference for all profile entries (agentic|deterministic|auto).");
        var iterationsOverrideOpt = new Option<int?>(
            "--iterations",
            () => null,
            "Override iteration count from execution spec.");
        var benchmarkSetOpt = new Option<string?>(
            "--benchmark-set",
            () => null,
            "Benchmark set tag to persist in workflow lab history.");
        var persistHistoryOpt = new Option<bool?>(
            "--persist-history",
            () => null,
            "Persist workflow lab results in JSONL history.");
        var jsonOpt = new Option<bool>("--json", () => false, "Emit machine-readable JSON output.");
        var verboseOpt = new Option<bool>("--verbose", () => false, "Emit orchestrator progress output.");

        stress.AddOption(requestOverrideOpt);
        stress.AddOption(specPathOpt);
        stress.AddOption(specJsonOpt);
        stress.AddOption(providerOpt);
        stress.AddOption(preferOpt);
        stress.AddOption(iterationsOverrideOpt);
        stress.AddOption(benchmarkSetOpt);
        stress.AddOption(persistHistoryOpt);
        stress.AddOption(jsonOpt);
        stress.AddOption(verboseOpt);
        stress.SetHandler((InvocationContext ctx) =>
        {
            var exitCode = ExecuteStressAsync(
                ctx.ParseResult.GetValueForOption(requestOverrideOpt),
                ctx.ParseResult.GetValueForOption(specPathOpt),
                ctx.ParseResult.GetValueForOption(specJsonOpt),
                ctx.ParseResult.GetValueForOption(providerOpt),
                ctx.ParseResult.GetValueForOption(preferOpt),
                ctx.ParseResult.GetValueForOption(iterationsOverrideOpt),
                ctx.ParseResult.GetValueForOption(benchmarkSetOpt),
                ctx.ParseResult.GetValueForOption(persistHistoryOpt),
                ctx.ParseResult.GetValueForOption(jsonOpt),
                ctx.ParseResult.GetValueForOption(verboseOpt),
                ctx.GetCancellationToken()).GetAwaiter().GetResult();
            ctx.ExitCode = exitCode;
        });
        AddCommand(stress);
    }

    private void ConfigureHistoryCommand()
    {
        var history = new Command("history", "Show recent workflow stress runs.");
        var repoRootOpt = new Option<string>("--repo-root", () => Environment.CurrentDirectory, "Repository root path.");
        var limitOpt = new Option<int>("--limit", () => 20, "Maximum history entries to return.");
        var benchmarkSetOpt = new Option<string?>("--benchmark-set", () => null, "Optional benchmark-set filter.");
        var jsonOpt = new Option<bool>("--json", () => false, "Emit machine-readable JSON output.");
        history.AddOption(repoRootOpt);
        history.AddOption(limitOpt);
        history.AddOption(benchmarkSetOpt);
        history.AddOption(jsonOpt);
        history.SetHandler((InvocationContext ctx) =>
        {
            var exitCode = ExecuteHistoryAsync(
                ctx.ParseResult.GetValueForOption(repoRootOpt) ?? Environment.CurrentDirectory,
                ctx.ParseResult.GetValueForOption(limitOpt),
                ctx.ParseResult.GetValueForOption(benchmarkSetOpt),
                ctx.ParseResult.GetValueForOption(jsonOpt)).GetAwaiter().GetResult();
            ctx.ExitCode = exitCode;
        });
        AddCommand(history);
    }

    private void ConfigureReportCommand()
    {
        var report = new Command("report", "Generate benchmark report from workflow stress history.");
        var repoRootOpt = new Option<string>("--repo-root", () => Environment.CurrentDirectory, "Repository root path.");
        var limitOpt = new Option<int>("--limit", () => 200, "Maximum history entries to analyze.");
        var benchmarkSetOpt = new Option<string?>("--benchmark-set", () => null, "Optional benchmark-set filter.");
        var outputOpt = new Option<string?>("--output", () => null, "Optional report output file path (.json, .md, .txt).");
        var jsonOpt = new Option<bool>("--json", () => false, "Emit machine-readable JSON output.");
        report.AddOption(repoRootOpt);
        report.AddOption(limitOpt);
        report.AddOption(benchmarkSetOpt);
        report.AddOption(outputOpt);
        report.AddOption(jsonOpt);
        report.SetHandler((InvocationContext ctx) =>
        {
            var exitCode = ExecuteReportAsync(
                ctx.ParseResult.GetValueForOption(repoRootOpt) ?? Environment.CurrentDirectory,
                ctx.ParseResult.GetValueForOption(limitOpt),
                ctx.ParseResult.GetValueForOption(benchmarkSetOpt),
                ctx.ParseResult.GetValueForOption(outputOpt),
                ctx.ParseResult.GetValueForOption(jsonOpt)).GetAwaiter().GetResult();
            ctx.ExitCode = exitCode;
        });
        AddCommand(report);
    }

    internal Task<int> ExecuteScaffoldAsync(string outputPath, bool force, bool json)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            WriteScaffoldResult(new WorkflowScaffoldResult(false, "Output path is required."), json);
            return Task.FromResult(1);
        }

        var fullPath = Path.GetFullPath(outputPath);
        var parent = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(parent))
            Directory.CreateDirectory(parent);

        if (File.Exists(fullPath) && !force)
        {
            WriteScaffoldResult(new WorkflowScaffoldResult(false, $"File already exists: {fullPath}. Use --force to overwrite.", fullPath), json);
            return Task.FromResult(1);
        }

        var scaffold = WorkflowLabRuntimeSpec.Default();
        var payload = JsonSerializer.Serialize(scaffold, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(fullPath, payload);
        WriteScaffoldResult(new WorkflowScaffoldResult(true, "Workflow lab spec scaffolded successfully.", fullPath), json);
        return Task.FromResult(0);
    }

    internal Task<int> ExecuteHistoryAsync(string repoRoot, int limit, string? benchmarkSet, bool json)
    {
        var fullRepoRoot = Path.GetFullPath(string.IsNullOrWhiteSpace(repoRoot) ? Environment.CurrentDirectory : repoRoot);
        if (!Directory.Exists(fullRepoRoot))
        {
            WriteHistoryResult(new WorkflowHistoryResult(false, $"Repo root not found: {fullRepoRoot}"), json);
            return Task.FromResult(1);
        }

        var rows = WorkflowLabHistoryStore.ReadRecent(fullRepoRoot, Math.Max(1, limit));
        if (!string.IsNullOrWhiteSpace(benchmarkSet))
        {
            var normalized = benchmarkSet.Trim().ToLowerInvariant();
            rows = rows
                .Where(x => string.Equals(x.BenchmarkSet, normalized, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        var total = rows.Count;
        var successCount = rows.Count(x => x.Success);
        var best = rows
            .OrderByDescending(x => x.Success)
            .ThenByDescending(x => x.Score)
            .ThenBy(x => x.ElapsedMs)
            .FirstOrDefault();

        WriteHistoryResult(new WorkflowHistoryResult(
            true,
            $"Loaded {total} workflow stress history entries.",
            rows,
            new WorkflowHistorySummary(total, successCount, total - successCount, best?.ScenarioId, best?.Score)), json);
        return Task.FromResult(0);
    }

    internal Task<int> ExecuteReportAsync(
        string repoRoot,
        int limit,
        string? benchmarkSet,
        string? outputPath,
        bool json)
    {
        var fullRepoRoot = Path.GetFullPath(string.IsNullOrWhiteSpace(repoRoot) ? Environment.CurrentDirectory : repoRoot);
        if (!Directory.Exists(fullRepoRoot))
        {
            WriteReportResult(new WorkflowReportResult(
                false,
                $"Repo root not found: {fullRepoRoot}",
                new WorkflowBenchmarkReport(
                    DateTimeOffset.UtcNow,
                    0,
                    0,
                    0,
                    0d,
                    0,
                    0,
                    0d,
                    0d,
                    0d,
                    Array.Empty<WorkflowScenarioBenchmark>(),
                    Array.Empty<WorkflowScenarioBenchmark>())), json);
            return Task.FromResult(1);
        }

        var rows = WorkflowLabHistoryStore.ReadRecent(fullRepoRoot, Math.Max(1, limit));
        if (!string.IsNullOrWhiteSpace(benchmarkSet))
        {
            var normalized = benchmarkSet.Trim().ToLowerInvariant();
            rows = rows
                .Where(x => string.Equals(x.BenchmarkSet, normalized, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        var report = BuildBenchmarkReport(rows);
        var result = new WorkflowReportResult(
            report.TotalRuns > 0,
            report.TotalRuns > 0
                ? $"Benchmark report generated from {report.TotalRuns} run(s)."
                : "No workflow stress history found for the selected filters.",
            report);

        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            var fullOutputPath = Path.GetFullPath(outputPath);
            var directory = Path.GetDirectoryName(fullOutputPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            var content = RenderReportContent(result, json, fullOutputPath);
            File.WriteAllText(fullOutputPath, content);
            result = result with { OutputPath = fullOutputPath };
        }

        WriteReportResult(result, json);
        return Task.FromResult(result.Ok ? 0 : 1);
    }

    internal async Task<int> ExecuteStressAsync(
        string? requestOverride,
        string? specPath,
        string? specJson,
        string? providerOverride,
        string? preferOverride,
        int? iterationsOverride,
        string? benchmarkSetOverride,
        bool? persistHistoryOverride,
        bool json,
        bool verbose,
        CancellationToken ct)
    {
        var resolvedSpecPath = ResolveDefaultSpecPath(specPath);
        WorkflowLabRuntimeSpec spec;
        try
        {
            spec = WorkflowLabRuntimeSpecLoader.Load(resolvedSpecPath, specJson);
        }
        catch (Exception ex)
        {
            WriteStressResult(new WorkflowStressResult(false, $"Failed to load workflow lab spec: {ex.Message}"), json);
            return 1;
        }

        var repoRoot = Environment.CurrentDirectory;
        var requests = NormalizeRequests(spec.Requests);
        var compositions = NormalizeCompositions(spec.Compositions);
        var profiles = NormalizeProfiles(spec.ModelProfiles, providerOverride, preferOverride);
        if (requests.Length == 0 || compositions.Length == 0 || profiles.Length == 0)
        {
            WriteStressResult(new WorkflowStressResult(
                false,
                "Workflow stress spec must include at least one request, composition, and model profile."), json);
            return 1;
        }

        var benchmarkSet = NormalizeBenchmarkSet(benchmarkSetOverride, spec.Execution.BenchmarkSet);
        var persistHistory = persistHistoryOverride ?? spec.Execution.PersistHistory;
        var iterations = Math.Max(1, iterationsOverride ?? spec.Execution.Iterations);
        var sharedRequest = string.IsNullOrWhiteSpace(requestOverride) ? null : requestOverride.Trim();

        var runs = new List<WorkflowStressRunRecord>();
        foreach (var request in requests)
        {
            foreach (var composition in compositions)
            {
                foreach (var profile in profiles)
                {
                    for (var i = 1; i <= iterations; i++)
                    {
                        ct.ThrowIfCancellationRequested();
                        var scenarioId = BuildScenarioId(request.Id, composition.Id, profile.Id, i);
                        var runtime = BuildRuntimeSpec(composition, profile);
                        var runtimeJson = JsonSerializer.Serialize(runtime);
                        var runtimeExecutionRequest = BuildExecutionRequest(request, composition, profile, sharedRequest);
                        var startedAt = DateTimeOffset.UtcNow;
                        var sw = Stopwatch.StartNew();
                        ScenarioExecutionResult scenario;
                        try
                        {
                            scenario = await _scenarioExecutor(
                                runtimeExecutionRequest,
                                runtimeJson,
                                profile.Default.Provider,
                                true,
                                verbose,
                                ct).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            scenario = new ScenarioExecutionResult(
                                Ok: false,
                                Summary: $"Scenario executor failed: {ex.Message}",
                                ConflictCount: 0,
                                EscalationCount: 0);
                        }
                        var elapsedMs = sw.ElapsedMilliseconds;
                        var score = ComputeScore(scenario.Ok, elapsedMs, composition, profile);
                        runs.Add(new WorkflowStressRunRecord(
                            scenarioId,
                            request.Id,
                            composition.Id,
                            profile.Id,
                            i,
                            scenario.Ok,
                            composition.Roles.Count,
                            scenario.ConflictCount,
                            scenario.EscalationCount,
                            elapsedMs,
                            score,
                            scenario.Summary,
                            startedAt,
                            benchmarkSet));
                    }
                }
            }
        }

        var aggregates = runs
            .GroupBy(x => new { x.RequestId, x.CompositionId, x.ModelProfileId })
            .Select(group =>
            {
                var list = group.ToArray();
                var successCount = list.Count(x => x.Success);
                var avgElapsed = (long)Math.Round(list.Select(x => (double)x.ElapsedMs).DefaultIfEmpty(0d).Average());
                var avgScore = Math.Round(list.Select(x => x.Score).DefaultIfEmpty(0d).Average(), 3);
                return new WorkflowStressAggregate(
                    ScenarioGroupId: $"{group.Key.RequestId}::{group.Key.CompositionId}::{group.Key.ModelProfileId}",
                    group.Key.RequestId,
                    group.Key.CompositionId,
                    group.Key.ModelProfileId,
                    list.Length,
                    successCount,
                    list.Length - successCount,
                    avgElapsed,
                    avgScore);
            })
            .OrderByDescending(x => x.AverageScore)
            .ThenByDescending(x => x.Successes)
            .ThenBy(x => x.AverageElapsedMs)
            .ToArray();

        if (persistHistory)
        {
            foreach (var run in runs)
            {
                WorkflowLabHistoryStore.Append(
                    repoRoot,
                    new WorkflowLabStressHistoryRow
                    {
                        ScenarioId = run.ScenarioId,
                        RequestId = run.RequestId,
                        CompositionId = run.CompositionId,
                        ModelProfileId = run.ModelProfileId,
                        Iteration = run.Iteration,
                        StartedAtUtc = run.StartedAtUtc,
                        ElapsedMs = run.ElapsedMs,
                        Success = run.Success,
                        AgentCount = run.AgentCount,
                        ConflictCount = run.ConflictCount,
                        EscalationCount = run.EscalationCount,
                        Score = run.Score,
                        Summary = run.Summary,
                        BenchmarkSet = run.BenchmarkSet
                    });
            }
        }

        var allPassed = runs.All(run => run.Success);
        var failureCount = runs.Count(run => !run.Success);
        var best = aggregates.FirstOrDefault(x => x.Successes > 0);
        var result = new WorkflowStressResult(
            allPassed,
            allPassed
                ? $"Workflow stress completed: {runs.Count} run(s) across {aggregates.Length} scenario groups."
                : $"Workflow stress completed with {failureCount} failing run(s) out of {runs.Count}.",
            runs,
            aggregates,
            best,
            benchmarkSet,
            persistHistory);
        WriteStressResult(result, json);
        return result.Ok ? 0 : 1;
    }

    private static string BuildExecutionRequest(
        WorkflowLabRequestSpec request,
        WorkflowLabCompositionSpec composition,
        WorkflowLabModelProfileSpec profile,
        string? requestOverride)
    {
        if (!string.IsNullOrWhiteSpace(requestOverride))
            return requestOverride;

        var roleLines = composition.Roles.Select(role =>
        {
            var chain = role.CommandChain.Count == 0 ? string.Empty : $" chain={string.Join(">", role.CommandChain)}";
            var reportsTo = string.IsNullOrWhiteSpace(role.ReportsToAgentId) ? string.Empty : $" reportsTo={role.ReportsToAgentId}";
            var cluster = string.IsNullOrWhiteSpace(role.ClusterId) ? string.Empty : $" cluster={role.ClusterId}";
            var roleModel = ResolveRoleModel(profile, role);
            var modelDirective = string.IsNullOrWhiteSpace(roleModel) ? string.Empty : $" model={roleModel}";
            return
                $"- agentId={role.AgentId} role={role.Role} domain={role.Domain}{cluster}{reportsTo}{chain}{modelDirective} goal={role.Goal}";
        });
        return
            $"{request.Prompt}\n\n" +
            $"Use this workflow composition:\n{string.Join('\n', roleLines)}\n" +
            "Treat the composition as mandatory orchestration constraints.";
    }

    private static async Task<ScenarioExecutionResult> ExecuteScenarioAsync(
        OrchestrateCommand orchestrate,
        string request,
        string runtimeSpecJson,
        string? provider,
        bool json,
        bool verbose,
        CancellationToken ct)
    {
        var (stdOut, stdErr) = await CaptureConsoleAsync(
            () => orchestrate.ExecuteAsync(
                request,
                runtimeSpecPath: null,
                runtimeSpecJson,
                preferModel: null,
                provider,
                barrierLevel: null,
                preferredRegion: null,
                json,
                verbose),
            ct).ConfigureAwait(false);
        var combined = string.Join(
            Environment.NewLine,
            new[] { stdOut, stdErr }.Where(x => !string.IsNullOrWhiteSpace(x)));
        var parsed = TryParseOrchestratePayload(combined);
        if (parsed is null)
        {
            return new ScenarioExecutionResult(
                false,
                "Scenario failed: orchestrate output did not contain JSON payload.",
                0,
                0);
        }

        return new ScenarioExecutionResult(
            parsed.Ok,
            parsed.Summary,
            parsed.ConflictCount,
            parsed.EscalationCount);
    }

    private static async Task<(string StdOut, string StdErr)> CaptureConsoleAsync(
        Func<Task<int>> run,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var originalOut = Console.Out;
        var originalErr = Console.Error;
        using var outWriter = new StringWriter();
        using var errWriter = new StringWriter();
        try
        {
            Console.SetOut(outWriter);
            Console.SetError(errWriter);
            await run().ConfigureAwait(false);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalErr);
        }

        return (outWriter.ToString(), errWriter.ToString());
    }

    private static ParsedOrchestratePayload? TryParseOrchestratePayload(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return null;

        var idx = output.LastIndexOf('{');
        while (idx >= 0)
        {
            var candidate = output[idx..].Trim();
            try
            {
                using var doc = JsonDocument.Parse(candidate);
                var root = doc.RootElement;
                var ok = root.TryGetProperty("ok", out var okNode) && okNode.ValueKind == JsonValueKind.True;
                if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
                    return new ParsedOrchestratePayload(ok, "Orchestrate response missing data payload.", 0, 0);
                var summary = data.TryGetProperty("success", out var successNode) && successNode.ValueKind == JsonValueKind.True
                    ? "Orchestration run completed successfully."
                    : "Orchestration reported failure.";
                var conflictCount = data.TryGetProperty("conflicts", out var conflictsNode) ? conflictsNode.GetInt32() : 0;
                var escalationCount = data.TryGetProperty("escalations", out var escalationsNode) ? escalationsNode.GetInt32() : 0;
                return new ParsedOrchestratePayload(ok, summary, conflictCount, escalationCount);
            }
            catch
            {
                idx = idx > 0 ? output.LastIndexOf('{', idx - 1) : -1;
            }
        }

        return null;
    }

    private static OrchestrationRuntimeSpec BuildRuntimeSpec(
        WorkflowLabCompositionSpec composition,
        WorkflowLabModelProfileSpec profile)
    {
        var defaultModel = profile.Default;
        var byAgent = new Dictionary<string, ModelRuntimeSpec>(profile.Agents, StringComparer.OrdinalIgnoreCase);
        foreach (var role in composition.Roles)
        {
            if (string.IsNullOrWhiteSpace(role.AgentId))
                continue;
            if (byAgent.ContainsKey(role.AgentId))
                continue;

            var provider = string.IsNullOrWhiteSpace(role.Provider) ? defaultModel.Provider : role.Provider;
            var prefer = string.IsNullOrWhiteSpace(role.Prefer) ? defaultModel.Prefer : role.Prefer!;
            byAgent[role.AgentId] = new ModelRuntimeSpec
            {
                Prefer = string.IsNullOrWhiteSpace(prefer) ? "auto" : prefer,
                Provider = provider
            };
        }

        return new OrchestrationRuntimeSpec
        {
            Model = defaultModel,
            Domains = new Dictionary<string, ModelRuntimeSpec>(profile.Domains, StringComparer.OrdinalIgnoreCase),
            Agents = byAgent
        };
    }

    private static WorkflowLabRequestSpec[] NormalizeRequests(IReadOnlyList<WorkflowLabRequestSpec> requests)
    {
        return (requests ?? Array.Empty<WorkflowLabRequestSpec>())
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .Where(x => !string.IsNullOrWhiteSpace(x.Prompt))
            .Select(x => x with { Id = x.Id.Trim(), Prompt = x.Prompt.Trim() })
            .ToArray();
    }

    private static WorkflowLabCompositionSpec[] NormalizeCompositions(IReadOnlyList<WorkflowLabCompositionSpec> compositions)
    {
        var output = new List<WorkflowLabCompositionSpec>();
        foreach (var composition in compositions ?? Array.Empty<WorkflowLabCompositionSpec>())
        {
            if (string.IsNullOrWhiteSpace(composition.Id))
                continue;

            var normalizedRoles = (composition.Roles ?? Array.Empty<WorkflowLabAgentRoleSpec>())
                .Where(role => !string.IsNullOrWhiteSpace(role.AgentId))
                .Where(role => !string.IsNullOrWhiteSpace(role.Goal))
                .Select(role => role with
                {
                    AgentId = role.AgentId.Trim(),
                    Role = string.IsNullOrWhiteSpace(role.Role) ? "builder" : role.Role.Trim(),
                    Domain = string.IsNullOrWhiteSpace(role.Domain) ? "general" : role.Domain.Trim(),
                    Goal = role.Goal.Trim(),
                    CommandChain = (role.CommandChain ?? Array.Empty<string>())
                        .Where(entry => !string.IsNullOrWhiteSpace(entry))
                        .Select(entry => entry.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray()
                })
                .ToArray();
            if (normalizedRoles.Length == 0)
                continue;

            output.Add(composition with
            {
                Id = composition.Id.Trim(),
                Description = composition.Description?.Trim() ?? string.Empty,
                Roles = normalizedRoles
            });
        }

        return output.ToArray();
    }

    private static WorkflowLabModelProfileSpec[] NormalizeProfiles(
        IReadOnlyList<WorkflowLabModelProfileSpec> profiles,
        string? providerOverride,
        string? preferOverride)
    {
        var provider = string.IsNullOrWhiteSpace(providerOverride) ? null : providerOverride.Trim();
        var prefer = string.IsNullOrWhiteSpace(preferOverride) ? null : preferOverride.Trim();
        var output = new List<WorkflowLabModelProfileSpec>();
        foreach (var profile in profiles ?? Array.Empty<WorkflowLabModelProfileSpec>())
        {
            if (string.IsNullOrWhiteSpace(profile.Id))
                continue;

            var defaultRuntime = ApplyOverrides(profile.Default, provider, prefer);
            var domains = profile.Domains.ToDictionary(
                kvp => kvp.Key,
                kvp => ApplyOverrides(kvp.Value, provider, prefer),
                StringComparer.OrdinalIgnoreCase);
            var agents = profile.Agents.ToDictionary(
                kvp => kvp.Key,
                kvp => ApplyOverrides(kvp.Value, provider, prefer),
                StringComparer.OrdinalIgnoreCase);

            output.Add(profile with
            {
                Id = profile.Id.Trim(),
                Description = profile.Description?.Trim() ?? string.Empty,
                Default = defaultRuntime,
                Domains = domains,
                Agents = agents
            });
        }

        return output.ToArray();
    }

    private static ModelRuntimeSpec ApplyOverrides(ModelRuntimeSpec runtime, string? provider, string? prefer)
    {
        var updated = runtime with { Prefer = string.IsNullOrWhiteSpace(runtime.Prefer) ? "auto" : runtime.Prefer };
        if (!string.IsNullOrWhiteSpace(provider))
            updated = updated with { Provider = provider };
        if (!string.IsNullOrWhiteSpace(prefer))
            updated = updated with { Prefer = prefer };
        return updated;
    }

    private static string ResolveDefaultSpecPath(string? explicitPath)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
            return Path.GetFullPath(explicitPath);
        return Path.Combine(Environment.CurrentDirectory, ".nexo", "workflow", "workflow_lab.runtime.json");
    }

    private static string BuildScenarioId(string requestId, string compositionId, string profileId, int iteration)
        => $"{requestId}::{compositionId}::{profileId}::iter-{iteration}";

    private static string NormalizeBenchmarkSet(string? benchmarkSetOverride, string defaultValue)
    {
        var value = string.IsNullOrWhiteSpace(benchmarkSetOverride) ? defaultValue : benchmarkSetOverride;
        var normalized = (value ?? "workflow-lab").Trim().ToLowerInvariant();
        return string.IsNullOrWhiteSpace(normalized) ? "workflow-lab" : normalized;
    }

    private static WorkflowBenchmarkReport BuildBenchmarkReport(IReadOnlyList<WorkflowLabStressHistoryRow> rows)
    {
        var items = rows ?? Array.Empty<WorkflowLabStressHistoryRow>();
        var totalRuns = items.Count;
        var successRuns = items.Count(x => x.Success);
        var failedRuns = totalRuns - successRuns;
        var successRate = totalRuns == 0 ? 0d : Math.Round((double)successRuns / totalRuns, 4);
        var avgElapsed = totalRuns == 0 ? 0L : (long)Math.Round(items.Select(x => (double)x.ElapsedMs).DefaultIfEmpty(0d).Average());
        var p95Elapsed = ComputePercentile(items.Select(x => x.ElapsedMs), 0.95);
        var avgScore = totalRuns == 0 ? 0d : Math.Round(items.Select(x => x.Score).DefaultIfEmpty(0d).Average(), 3);
        var avgConflicts = totalRuns == 0 ? 0d : Math.Round(items.Select(x => (double)x.ConflictCount).DefaultIfEmpty(0d).Average(), 3);
        var avgEscalations = totalRuns == 0 ? 0d : Math.Round(items.Select(x => (double)x.EscalationCount).DefaultIfEmpty(0d).Average(), 3);

        var scenarioStats = items
            .GroupBy(x => $"{x.RequestId}::{x.CompositionId}::{x.ModelProfileId}", StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var data = g.ToArray();
                var groupSuccess = data.Count(x => x.Success);
                return new WorkflowScenarioBenchmark(
                    ScenarioGroupId: g.Key,
                    Runs: data.Length,
                    Successes: groupSuccess,
                    Failures: data.Length - groupSuccess,
                    SuccessRate: data.Length == 0 ? 0d : Math.Round((double)groupSuccess / data.Length, 4),
                    AverageElapsedMs: (long)Math.Round(data.Select(x => (double)x.ElapsedMs).DefaultIfEmpty(0d).Average()),
                    P95ElapsedMs: ComputePercentile(data.Select(x => x.ElapsedMs), 0.95),
                    AverageScore: Math.Round(data.Select(x => x.Score).DefaultIfEmpty(0d).Average(), 3),
                    AverageConflicts: Math.Round(data.Select(x => (double)x.ConflictCount).DefaultIfEmpty(0d).Average(), 3),
                    AverageEscalations: Math.Round(data.Select(x => (double)x.EscalationCount).DefaultIfEmpty(0d).Average(), 3))
                {
                    LastFailureSummary = data
                        .Where(x => !x.Success && !string.IsNullOrWhiteSpace(x.Summary))
                        .OrderByDescending(x => x.StartedAtUtc)
                        .Select(x => x.Summary)
                        .FirstOrDefault()
                };
            })
            .OrderByDescending(x => x.SuccessRate)
            .ThenByDescending(x => x.AverageScore)
            .ThenBy(x => x.AverageElapsedMs)
            .ToArray();

        var topScenarios = scenarioStats.Take(5).ToArray();
        var bottlenecks = scenarioStats
            .Where(x => x.Failures > 0 || x.P95ElapsedMs > avgElapsed * 1.5)
            .OrderByDescending(x => x.Failures)
            .ThenByDescending(x => x.P95ElapsedMs)
            .Take(5)
            .ToArray();

        return new WorkflowBenchmarkReport(
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            TotalRuns: totalRuns,
            SuccessRuns: successRuns,
            FailedRuns: failedRuns,
            SuccessRate: successRate,
            AverageElapsedMs: avgElapsed,
            P95ElapsedMs: p95Elapsed,
            AverageScore: avgScore,
            AverageConflicts: avgConflicts,
            AverageEscalations: avgEscalations,
            TopScenarios: topScenarios,
            Bottlenecks: bottlenecks);
    }

    private static long ComputePercentile(IEnumerable<long> source, double percentile)
    {
        var ordered = source.OrderBy(x => x).ToArray();
        if (ordered.Length == 0)
            return 0;
        var clamped = Math.Clamp(percentile, 0d, 1d);
        var index = (int)Math.Ceiling((ordered.Length - 1) * clamped);
        return ordered[index];
    }

    private static string RenderReportContent(WorkflowReportResult result, bool preferJson, string outputPath)
    {
        var extension = Path.GetExtension(outputPath).Trim().ToLowerInvariant();
        if (preferJson || extension == ".json")
        {
            return JsonSerializer.Serialize(new
            {
                ok = result.Ok,
                summary = result.Summary,
                report = result.Report
            }, new JsonSerializerOptions { WriteIndented = true });
        }

        if (extension == ".md")
        {
            return RenderReportMarkdown(result);
        }

        return RenderReportText(result);
    }

    private static string RenderReportMarkdown(WorkflowReportResult result)
    {
        var report = result.Report;
        var sb = new StringBuilder();
        sb.AppendLine("# Workflow Stress Benchmark Report");
        sb.AppendLine();
        sb.AppendLine($"Generated: {report.GeneratedAtUtc:O}");
        sb.AppendLine();
        sb.AppendLine("## Summary");
        sb.AppendLine($"- Total runs: {report.TotalRuns}");
        sb.AppendLine($"- Success runs: {report.SuccessRuns}");
        sb.AppendLine($"- Failed runs: {report.FailedRuns}");
        sb.AppendLine($"- Success rate: {report.SuccessRate:P1}");
        sb.AppendLine($"- Avg latency: {report.AverageElapsedMs} ms");
        sb.AppendLine($"- P95 latency: {report.P95ElapsedMs} ms");
        sb.AppendLine($"- Avg score: {report.AverageScore:F2}");
        sb.AppendLine($"- Avg conflicts: {report.AverageConflicts:F2}");
        sb.AppendLine($"- Avg escalations: {report.AverageEscalations:F2}");
        sb.AppendLine();
        sb.AppendLine("## Top Scenarios");
        foreach (var scenario in report.TopScenarios)
        {
            sb.AppendLine($"- `{scenario.ScenarioGroupId}` | success {scenario.Successes}/{scenario.Runs} ({scenario.SuccessRate:P1}) | score {scenario.AverageScore:F2} | p95 {scenario.P95ElapsedMs} ms");
        }
        sb.AppendLine();
        sb.AppendLine("## Bottlenecks");
        foreach (var bottleneck in report.Bottlenecks)
        {
            sb.AppendLine($"- `{bottleneck.ScenarioGroupId}` | failures {bottleneck.Failures} | p95 {bottleneck.P95ElapsedMs} ms | last failure: {bottleneck.LastFailureSummary ?? "n/a"}");
        }
        return sb.ToString();
    }

    private static string RenderReportText(WorkflowReportResult result)
    {
        var report = result.Report;
        var sb = new StringBuilder();
        sb.AppendLine("Workflow Stress Benchmark Report");
        sb.AppendLine($"Generated: {report.GeneratedAtUtc:O}");
        sb.AppendLine($"Total runs: {report.TotalRuns}");
        sb.AppendLine($"Success runs: {report.SuccessRuns}");
        sb.AppendLine($"Failed runs: {report.FailedRuns}");
        sb.AppendLine($"Success rate: {report.SuccessRate:P1}");
        sb.AppendLine($"Average latency: {report.AverageElapsedMs} ms");
        sb.AppendLine($"P95 latency: {report.P95ElapsedMs} ms");
        sb.AppendLine($"Average score: {report.AverageScore:F2}");
        sb.AppendLine($"Average conflicts: {report.AverageConflicts:F2}");
        sb.AppendLine($"Average escalations: {report.AverageEscalations:F2}");
        sb.AppendLine();
        sb.AppendLine("Top scenarios:");
        foreach (var scenario in report.TopScenarios)
        {
            sb.AppendLine($"- {scenario.ScenarioGroupId}: success {scenario.Successes}/{scenario.Runs}, score {scenario.AverageScore:F2}, p95 {scenario.P95ElapsedMs} ms");
        }
        sb.AppendLine();
        sb.AppendLine("Bottlenecks:");
        foreach (var bottleneck in report.Bottlenecks)
        {
            sb.AppendLine($"- {bottleneck.ScenarioGroupId}: failures {bottleneck.Failures}, p95 {bottleneck.P95ElapsedMs} ms, last failure={bottleneck.LastFailureSummary ?? "n/a"}");
        }
        return sb.ToString();
    }

    private static string? ResolveRoleModel(WorkflowLabModelProfileSpec profile, WorkflowLabAgentRoleSpec role)
    {
        if (!string.IsNullOrWhiteSpace(role.OllamaModel))
            return role.OllamaModel.Trim();
        if (profile.AgentModelHints.TryGetValue(role.AgentId, out var hint) && !string.IsNullOrWhiteSpace(hint))
            return hint.Trim();
        return null;
    }

    private static double ComputeScore(
        bool success,
        long elapsedMs,
        WorkflowLabCompositionSpec composition,
        WorkflowLabModelProfileSpec profile)
    {
        var successScore = success ? 100d : 0d;
        var latencyPenalty = Math.Min(60d, elapsedMs / 200d);
        var complexityBonus = Math.Min(20d, composition.Roles.Count * 2d);
        var diversityBonus = Math.Min(10d, profile.Agents.Count + profile.Domains.Count + profile.AgentModelHints.Count);
        var score = successScore - latencyPenalty + complexityBonus + diversityBonus;
        return Math.Round(score, 3);
    }

    private static void WriteScaffoldResult(WorkflowScaffoldResult result, bool json)
    {
        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                ok = result.Ok,
                summary = result.Summary,
                outputPath = result.OutputPath
            }, new JsonSerializerOptions { WriteIndented = true }));
            return;
        }

        Console.WriteLine($"workflow scaffold: {(result.Ok ? "ok" : "failed")}");
        Console.WriteLine(result.Summary);
        if (!string.IsNullOrWhiteSpace(result.OutputPath))
            Console.WriteLine($"  output={result.OutputPath}");
    }

    private static void WriteStressResult(WorkflowStressResult result, bool json)
    {
        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                ok = result.Ok,
                summary = result.Summary,
                benchmarkSet = result.BenchmarkSet,
                persistHistory = result.PersistHistory,
                runs = result.Runs,
                aggregates = result.Aggregates,
                best = result.Best
            }, new JsonSerializerOptions { WriteIndented = true }));
            return;
        }

        Console.WriteLine($"workflow stress: {(result.Ok ? "ok" : "failed")}");
        Console.WriteLine(result.Summary);
        if (!string.IsNullOrWhiteSpace(result.BenchmarkSet))
            Console.WriteLine($"  benchmark-set={result.BenchmarkSet} (persist-history={result.PersistHistory})");
        foreach (var aggregate in (result.Aggregates ?? Array.Empty<WorkflowStressAggregate>()).Take(5))
        {
            Console.WriteLine(
                $"  {aggregate.ScenarioGroupId}: success={aggregate.Successes}/{aggregate.TotalRuns}, avg-score={aggregate.AverageScore:F2}, avg-elapsed={aggregate.AverageElapsedMs}ms");
        }
        if (result.Best != null)
        {
            Console.WriteLine($"  best={result.Best.ScenarioGroupId} score={result.Best.AverageScore:F2}");
        }
    }

    private static void WriteHistoryResult(WorkflowHistoryResult result, bool json)
    {
        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                ok = result.Ok,
                summary = result.Summary,
                summaryStats = result.SummaryStats,
                items = result.Items
            }, new JsonSerializerOptions { WriteIndented = true }));
            return;
        }

        Console.WriteLine($"workflow history: {(result.Ok ? "ok" : "failed")}");
        Console.WriteLine(result.Summary);
        if (result.SummaryStats != null)
        {
            Console.WriteLine(
                $"  total={result.SummaryStats.Total}, success={result.SummaryStats.Success}, failed={result.SummaryStats.Failed}, best={result.SummaryStats.BestScenarioId ?? "n/a"}, best-score={result.SummaryStats.BestScore?.ToString("F2") ?? "n/a"}");
        }
    }

    private static void WriteReportResult(WorkflowReportResult result, bool json)
    {
        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                ok = result.Ok,
                summary = result.Summary,
                outputPath = result.OutputPath,
                report = result.Report
            }, new JsonSerializerOptions { WriteIndented = true }));
            return;
        }

        Console.WriteLine($"workflow report: {(result.Ok ? "ok" : "failed")}");
        Console.WriteLine(result.Summary);
        if (!string.IsNullOrWhiteSpace(result.OutputPath))
            Console.WriteLine($"  output={result.OutputPath}");
        Console.WriteLine($"  success-rate={result.Report.SuccessRate:P1}, avg-latency={result.Report.AverageElapsedMs}ms, p95={result.Report.P95ElapsedMs}ms");
        if (result.Report.TopScenarios.Count > 0)
            Console.WriteLine($"  best={result.Report.TopScenarios[0].ScenarioGroupId} score={result.Report.TopScenarios[0].AverageScore:F2}");
    }

    private sealed record WorkflowScaffoldResult(bool Ok, string Summary, string? OutputPath = null);

    private sealed record WorkflowStressRunRecord(
        string ScenarioId,
        string RequestId,
        string CompositionId,
        string ModelProfileId,
        int Iteration,
        bool Success,
        int AgentCount,
        int ConflictCount,
        int EscalationCount,
        long ElapsedMs,
        double Score,
        string Summary,
        DateTimeOffset StartedAtUtc,
        string BenchmarkSet);

    private sealed record WorkflowStressAggregate(
        string ScenarioGroupId,
        string RequestId,
        string CompositionId,
        string ModelProfileId,
        int TotalRuns,
        int Successes,
        int Failures,
        long AverageElapsedMs,
        double AverageScore);

    private sealed record WorkflowStressResult(
        bool Ok,
        string Summary,
        IReadOnlyList<WorkflowStressRunRecord>? Runs = null,
        IReadOnlyList<WorkflowStressAggregate>? Aggregates = null,
        WorkflowStressAggregate? Best = null,
        string? BenchmarkSet = null,
        bool? PersistHistory = null);

    private sealed record WorkflowHistorySummary(
        int Total,
        int Success,
        int Failed,
        string? BestScenarioId,
        double? BestScore);

    private sealed record WorkflowHistoryResult(
        bool Ok,
        string Summary,
        IReadOnlyList<WorkflowLabStressHistoryRow>? Items = null,
        WorkflowHistorySummary? SummaryStats = null);

    private sealed record WorkflowScenarioBenchmark(
        string ScenarioGroupId,
        int Runs,
        int Successes,
        int Failures,
        double SuccessRate,
        long AverageElapsedMs,
        long P95ElapsedMs,
        double AverageScore,
        double AverageConflicts,
        double AverageEscalations)
    {
        public string? LastFailureSummary { get; init; }
    }

    private sealed record WorkflowBenchmarkReport(
        DateTimeOffset GeneratedAtUtc,
        int TotalRuns,
        int SuccessRuns,
        int FailedRuns,
        double SuccessRate,
        long AverageElapsedMs,
        long P95ElapsedMs,
        double AverageScore,
        double AverageConflicts,
        double AverageEscalations,
        IReadOnlyList<WorkflowScenarioBenchmark> TopScenarios,
        IReadOnlyList<WorkflowScenarioBenchmark> Bottlenecks);

    private sealed record WorkflowReportResult(
        bool Ok,
        string Summary,
        WorkflowBenchmarkReport Report,
        string? OutputPath = null);

    internal sealed record ScenarioExecutionResult(
        bool Ok,
        string Summary,
        int ConflictCount,
        int EscalationCount);

    private sealed record ParsedOrchestratePayload(
        bool Ok,
        string Summary,
        int ConflictCount,
        int EscalationCount);
}
