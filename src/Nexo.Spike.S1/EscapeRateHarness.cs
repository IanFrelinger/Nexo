using System.Diagnostics;
using Nexo.Spike.S0;
using Nexo.Spike.S1.Adversary;
using Nexo.Spike.S1.Reporting;
using Nexo.Spike.S1.Transforms;

namespace Nexo.Spike.S1;

public sealed class EscapeRateHarnessOptions
{
    public required int Seeds { get; init; }
    public required int MutationSample { get; init; }
    public required int BudgetMinutes { get; init; }
    public required string OutputDirectory { get; init; }
    public double MutationThresholdPercent { get; init; } = 80;
    public IAdversarialGenerator? Generator { get; init; }
}

public sealed class EscapeRateHarness
{
    private readonly PropertyGate _propertyGate = new();
    private readonly MutationGate _mutationGate = new();

    public async Task<EscapeRateReport> RunAsync(EscapeRateHarnessOptions options, CancellationToken ct = default)
    {
        var generator = options.Generator ?? AdversarialGeneratorFactory.Create();
        var adversaryMode = Environment.GetEnvironmentVariable("NEXO_S1_ADVERSARY") ?? AdversarialGeneratorFactory.OfflineMode;
        var tools = await ProbeToolsAsync(ct).ConfigureAwait(false);

        if (!tools.DotnetAvailable)
            throw new InvalidOperationException("dotnet SDK is required but was not found on PATH.");

        var workRoot = Path.Combine(options.OutputDirectory, "workspaces");
        Directory.CreateDirectory(workRoot);
        var reportRoot = Path.GetFullPath(options.OutputDirectory);

        var wrongImplCandidates = generator.GenerateWrongImplCandidates(options.Seeds);
        var wrongImplBaselines = Enumerable.Range(0, options.Seeds)
            .Select(generator.GenerateHonestBaseline)
            .ToList();

        var wrongImplOutcomes = new List<CandidateOutcome>(wrongImplCandidates.Count + wrongImplBaselines.Count);
        var survivingExamples = new List<SurvivingExample>();

        foreach (var candidate in wrongImplCandidates)
        {
            ct.ThrowIfCancellationRequested();
            var (outcome, workspace, detail) = await EvaluateWrongImplAsync(candidate, workRoot, ct)
                .ConfigureAwait(false);
            wrongImplOutcomes.Add(outcome);
            if (outcome.Kind == CandidateOutcomeKind.Escape)
            {
                survivingExamples.Add(new SurvivingExample(
                    "wrong-impl",
                    candidate.Tag.ToString(),
                    candidate.Seed,
                    ToReportRelativePath(workspace, reportRoot),
                    detail));
            }
        }

        foreach (var baseline in wrongImplBaselines)
        {
            ct.ThrowIfCancellationRequested();
            var (outcome, _, _) = await EvaluateWrongImplAsync(baseline, workRoot, ct).ConfigureAwait(false);
            wrongImplOutcomes.Add(outcome);
        }

        var wrongImplReport = EscapeRateTally.BuildDimensionReport(
            "PropertyGate",
            "completed",
            wrongImplOutcomes.Where(o => o.Family == TransformFamily.WrongImpl).ToList(),
            wrongImplOutcomes.Where(o => o.Family == TransformFamily.HonestBaseline).ToList(),
            TransformCatalog.WrongImplTags);

        DimensionReport weakTestReport;
        if (options.MutationSample <= 0)
        {
            weakTestReport = SkippedDimension("MutationGate", "mutation-sample-zero");
        }
        else if (!tools.StrykerAvailable)
        {
            weakTestReport = SkippedDimension("MutationGate", "stryker-unavailable");
        }
        else
        {
            var deadline = DateTimeOffset.UtcNow.AddMinutes(options.BudgetMinutes);
            var weakCandidates = generator.GenerateWeakTestCandidates(options.Seeds)
                .Take(options.MutationSample)
                .ToList();
            var weakBaselines = new List<AdversarialCandidate>();
            if (weakCandidates.Count > 0)
                weakBaselines.Add(generator.GenerateHonestBaseline(weakCandidates[0].Seed));

            var weakOutcomes = new List<CandidateOutcome>(weakCandidates.Count + weakBaselines.Count);
            var budgetExceeded = false;

            foreach (var candidate in weakCandidates)
            {
                ct.ThrowIfCancellationRequested();
                if (DateTimeOffset.UtcNow >= deadline)
                {
                    budgetExceeded = true;
                    break;
                }

                var (outcome, workspace, detail) = await EvaluateWeakTestAsync(
                        candidate,
                        workRoot,
                        options.MutationThresholdPercent,
                        ct)
                    .ConfigureAwait(false);
                weakOutcomes.Add(outcome);
                if (outcome.Kind == CandidateOutcomeKind.Escape)
                {
                    survivingExamples.Add(new SurvivingExample(
                        "weak-test",
                        candidate.Tag.ToString(),
                        candidate.Seed,
                        ToReportRelativePath(workspace, reportRoot),
                        detail));
                }
            }

            if (!budgetExceeded && weakBaselines.Count > 0)
            {
                foreach (var baseline in weakBaselines)
                {
                    if (DateTimeOffset.UtcNow >= deadline)
                    {
                        budgetExceeded = true;
                        break;
                    }

                    var (outcome, _, _) = await EvaluateWeakTestAsync(
                            baseline,
                            workRoot,
                            options.MutationThresholdPercent,
                            ct)
                        .ConfigureAwait(false);
                    weakOutcomes.Add(outcome);
                }
            }

            var status = budgetExceeded ? "completed-budget-truncated" : "completed";
            weakTestReport = EscapeRateTally.BuildDimensionReport(
                "MutationGate",
                status,
                weakOutcomes.Where(o => o.Family == TransformFamily.WeakTest).ToList(),
                weakOutcomes.Where(o => o.Family == TransformFamily.HonestBaseline).ToList(),
                TransformCatalog.WeakTestTags);
        }

        return new EscapeRateReport(
            EscapeRateReport.Version,
            adversaryMode,
            options.Seeds,
            options.MutationSample,
            options.BudgetMinutes,
            tools,
            wrongImplReport,
            weakTestReport,
            survivingExamples);
    }

    internal async Task<(CandidateOutcome Outcome, string WorkspacePath, string? Detail)> EvaluateWrongImplAsync(
        AdversarialCandidate candidate,
        string workRoot,
        CancellationToken ct)
    {
        var workspace = Path.Combine(workRoot, $"wrong-impl-{candidate.Seed:D4}-{candidate.Tag}");
        try
        {
            await MaterializeWorkspaceAsync(candidate, workspace, ct).ConfigureAwait(false);

            var (buildCode, buildOut, buildErr, buildTimedOut) =
                await SpikeWorkspaceScaffold.BuildAsync(workspace, ct).ConfigureAwait(false);
            if (buildCode != 0 || buildTimedOut)
            {
                return (
                    Blocked(candidate, $"build failed: {TrimForReport(buildOut, buildErr)}"),
                    workspace,
                    null);
            }

            var propertyResult = await _propertyGate.RunAsync(workspace, ct).ConfigureAwait(false);
            if (candidate.Family == TransformFamily.HonestBaseline)
            {
                return (
                    propertyResult.Passed
                        ? new CandidateOutcome(candidate.Tag, candidate.Family, candidate.Seed, CandidateOutcomeKind.Accepted, "accepted")
                        : new CandidateOutcome(
                            candidate.Tag,
                            candidate.Family,
                            candidate.Seed,
                            CandidateOutcomeKind.FalseReject,
                            TrimForReport(propertyResult.RawOutput)),
                    workspace,
                    null);
            }

            return (
                propertyResult.Passed
                    ? new CandidateOutcome(
                        candidate.Tag,
                        candidate.Family,
                        candidate.Seed,
                        CandidateOutcomeKind.Escape,
                        "PropertyGate passed")
                    : new CandidateOutcome(
                        candidate.Tag,
                        candidate.Family,
                        candidate.Seed,
                        CandidateOutcomeKind.Caught,
                        TrimForReport(propertyResult.RawOutput)),
                workspace,
                propertyResult.Passed ? "property-gate-pass" : null);
        }
        catch (Exception ex)
        {
            return (Blocked(candidate, ex.Message), workspace, null);
        }
    }

    internal async Task<(CandidateOutcome Outcome, string WorkspacePath, string? Detail)> EvaluateWeakTestAsync(
        AdversarialCandidate candidate,
        string workRoot,
        double mutationThresholdPercent,
        CancellationToken ct)
    {
        var workspace = Path.Combine(workRoot, $"weak-test-{candidate.Seed:D4}-{candidate.Tag}");
        try
        {
            await MaterializeWorkspaceAsync(candidate, workspace, ct).ConfigureAwait(false);

            var (buildCode, buildOut, buildErr, buildTimedOut) =
                await SpikeWorkspaceScaffold.BuildAsync(workspace, ct).ConfigureAwait(false);
            if (buildCode != 0 || buildTimedOut)
            {
                return (
                    Blocked(candidate, $"build failed: {TrimForReport(buildOut, buildErr)}"),
                    workspace,
                    null);
            }

            var (testCode, testOut, testErr, testTimedOut) =
                await SpikeWorkspaceScaffold.TestWithRebuildAsync(workspace, ct).ConfigureAwait(false);
            if (testCode != 0 || testTimedOut)
            {
                return (
                    Blocked(candidate, $"tests failed: {TrimForReport(testOut, testErr)}"),
                    workspace,
                    null);
            }

            var mutationResult = await _mutationGate.RunAsync(workspace, mutationThresholdPercent, ct)
                .ConfigureAwait(false);

            if (candidate.Family == TransformFamily.HonestBaseline)
            {
                return (
                    mutationResult.Passed
                        ? new CandidateOutcome(candidate.Tag, candidate.Family, candidate.Seed, CandidateOutcomeKind.Accepted, "accepted")
                        : new CandidateOutcome(
                            candidate.Tag,
                            candidate.Family,
                            candidate.Seed,
                            CandidateOutcomeKind.FalseReject,
                            TrimForReport(mutationResult.RawOutput)),
                    workspace,
                    null);
            }

            var survivors = mutationResult.SurvivingMutants.Count == 0
                ? $"score={mutationResult.MutationScore:F1}%"
                : string.Join("; ", mutationResult.SurvivingMutants.Take(3));

            return (
                mutationResult.Passed
                    ? new CandidateOutcome(
                        candidate.Tag,
                        candidate.Family,
                        candidate.Seed,
                        CandidateOutcomeKind.Escape,
                        $"MutationGate passed ({mutationResult.MutationScore:F1}%)")
                    : new CandidateOutcome(
                        candidate.Tag,
                        candidate.Family,
                        candidate.Seed,
                        CandidateOutcomeKind.Caught,
                        TrimForReport(mutationResult.RawOutput)),
                workspace,
                mutationResult.Passed ? survivors : null);
        }
        catch (Exception ex)
        {
            return (Blocked(candidate, ex.Message), workspace, null);
        }
    }

    private static CandidateOutcome Blocked(AdversarialCandidate candidate, string detail) =>
        new(candidate.Tag, candidate.Family, candidate.Seed, CandidateOutcomeKind.Blocked, detail);

    private static async Task MaterializeWorkspaceAsync(
        AdversarialCandidate candidate,
        string workspace,
        CancellationToken ct)
    {
        SpikeWorkspaceScaffold.CreateFresh(workspace, overwrite: true);
        var intent = ResolveHonestIntentPath();
        await BrickSpecLoader.WriteFrozenAsync(
                workspace,
                await BrickSpecLoader.LoadAsync(intent, ct).ConfigureAwait(false),
                ct)
            .ConfigureAwait(false);

        File.WriteAllText(
            Path.Combine(workspace, "CsvColumnInferrer", "ColumnTypeInferrer.cs"),
            candidate.ImplementationSource);
        File.WriteAllText(
            Path.Combine(workspace, "CsvColumnInferrer.Tests", "ColumnTypeInferrerRedTests.cs"),
            candidate.TestSource);
    }

    private static string ResolveHonestIntentPath()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Fixtures", "honest-csv-inferrer.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "Nexo.Spike.S1", "Fixtures", "honest-csv-inferrer.json"),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "samples", "spike-s0", "intents", "honest-csv-inferrer.json"))
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path))
                return path;
        }

        throw new FileNotFoundException("honest-csv-inferrer.json fixture not found");
    }

    private static DimensionReport SkippedDimension(string gate, string reason) =>
        new(
            $"skipped:{reason}",
            gate,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            new Dictionary<string, TransformBreakdown>());

    private static async Task<ToolAvailability> ProbeToolsAsync(CancellationToken ct)
    {
        var dotnetAvailable = await CommandExistsAsync("dotnet", "--version", ct).ConfigureAwait(false);
        var (strykerAvailable, strykerDetail) = await ProbeStrykerAsync(ct).ConfigureAwait(false);
        return new ToolAvailability(dotnetAvailable, strykerAvailable, strykerDetail);
    }

    private static async Task<(bool Available, string? Detail)> ProbeStrykerAsync(CancellationToken ct)
    {
        try
        {
            var psi = new ProcessStartInfo("dotnet", "stryker --help")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using var process = Process.Start(psi);
            if (process is null)
                return (false, "dotnet stryker process could not start");

            var stdout = await process.StandardOutput.ReadToEndAsync(ct).ConfigureAwait(false);
            var stderr = await process.StandardError.ReadToEndAsync(ct).ConfigureAwait(false);
            await process.WaitForExitAsync(ct).ConfigureAwait(false);

            if (process.ExitCode == 0)
                return (true, "dotnet stryker --help");

            return (false, TrimForReport(stdout, stderr));
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static async Task<bool> CommandExistsAsync(string fileName, string args, CancellationToken ct)
    {
        try
        {
            var psi = new ProcessStartInfo(fileName, args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using var process = Process.Start(psi);
            if (process is null)
                return false;

            await process.StandardOutput.ReadToEndAsync(ct).ConfigureAwait(false);
            await process.StandardError.ReadToEndAsync(ct).ConfigureAwait(false);
            await process.WaitForExitAsync(ct).ConfigureAwait(false);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static string TrimForReport(params string?[] parts)
    {
        var text = string.Join("\n", parts.Where(p => !string.IsNullOrWhiteSpace(p))).Trim();
        const int max = 400;
        return text.Length <= max ? text : text[..max] + "...";
    }

    private static string ToReportRelativePath(string workspace, string reportRoot)
    {
        var full = Path.GetFullPath(workspace);
        var relative = Path.GetRelativePath(reportRoot, full);
        return relative.Replace('\\', '/');
    }
}
