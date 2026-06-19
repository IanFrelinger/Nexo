using System.Diagnostics;
using Nexo.Spike.S0;
using Nexo.Spike.S1.Adversary;
using Nexo.Spike.S1.IntentDensity;
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
    public IReadOnlyList<double>? ThresholdSweep { get; init; }
    public IAdversarialGenerator? Generator { get; init; }
    public string? IntentPath { get; init; }
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
        var deadline = DateTimeOffset.UtcNow.AddMinutes(options.BudgetMinutes);

        var allAttributions = new List<CandidateOutcome>();
        var survivingExamples = new List<SurvivingExample>();

        var wrongImplCandidates = generator.GenerateWrongImplCandidates(options.Seeds);
        var wrongImplBaselines = Enumerable.Range(0, options.Seeds)
            .Select(generator.GenerateHonestBaseline)
            .ToList();

        var wrongImplOutcomes = new List<CandidateOutcome>(wrongImplCandidates.Count + wrongImplBaselines.Count);

        foreach (var candidate in wrongImplCandidates)
        {
            ct.ThrowIfCancellationRequested();
            var outcome = await EvaluateWrongImplAsync(candidate, workRoot, options.IntentPath, ct).ConfigureAwait(false);
            wrongImplOutcomes.Add(outcome);
            allAttributions.Add(outcome);
            if (outcome.Kind == CandidateOutcomeKind.Escape)
                survivingExamples.Add(ToSurvivingExample("wrong-impl", outcome, workRoot, reportRoot));
        }

        foreach (var baseline in wrongImplBaselines)
        {
            ct.ThrowIfCancellationRequested();
            var outcome = await EvaluateWrongImplAsync(baseline, workRoot, options.IntentPath, ct).ConfigureAwait(false);
            wrongImplOutcomes.Add(outcome);
            allAttributions.Add(outcome);
        }

        var wrongImplReport = EscapeRateTally.BuildDimensionReport(
            "PropertyGate",
            "completed",
            wrongImplOutcomes.Where(o => o.Family == TransformFamily.WrongImpl).ToList(),
            wrongImplOutcomes.Where(o => o.Family == TransformFamily.HonestBaseline).ToList(),
            TransformCatalog.WrongImplTags);

        var weakSampleCandidates = options.MutationSample <= 0
            ? new List<AdversarialCandidate>()
            : TransformCatalog.WeakTestTags
                .SelectMany(tag => generator.GenerateWeakTestCandidates(options.Seeds).Where(c => c.Tag == tag).Take(1))
                .ToList();

        var distinctWrongImplTrials = wrongImplCandidates
            .Select(c => (c.Tag, c.ImplementationSource))
            .Distinct()
            .Count();
        var distinctWeakTestTrials = weakSampleCandidates
            .Select(c => (c.Tag, c.TestSource))
            .Distinct()
            .Count();

        DimensionReport weakTestReport;
        ThresholdSensitivityReport? thresholdSensitivity = null;

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
        var weakCandidates = TransformCatalog.WeakTestTags
            .Select(tag => weakSampleCandidates.First(c => c.Tag == tag))
            .Take(options.MutationSample)
            .ToList();
            var weakBaselines = weakCandidates.Count > 0
                ? [generator.GenerateHonestBaseline(weakCandidates[0].Seed)]
                : new List<AdversarialCandidate>();

            var weakOutcomes = new List<CandidateOutcome>();
            var sweepWorkspaces = new Dictionary<TransformTag, string>();
            var budgetExceeded = false;
            var sensitivityComplete = false;

            foreach (var candidate in weakCandidates)
            {
                ct.ThrowIfCancellationRequested();
                if (DateTimeOffset.UtcNow >= deadline)
                {
                    budgetExceeded = true;
                    break;
                }

                var (outcome, workspace) = await EvaluateWeakTestAsync(
                        candidate,
                        workRoot,
                        options.IntentPath,
                        options.MutationThresholdPercent,
                        ct)
                    .ConfigureAwait(false);
                weakOutcomes.Add(outcome);
                allAttributions.Add(outcome);
                if (outcome.Kind == CandidateOutcomeKind.Escape)
                    survivingExamples.Add(ToSurvivingExample("weak-test", outcome, workRoot, reportRoot));
                if (outcome.Kind is CandidateOutcomeKind.Escape or CandidateOutcomeKind.Caught)
                    sweepWorkspaces[candidate.Tag] = workspace;
            }

            if (!budgetExceeded)
            {
                foreach (var baseline in weakBaselines)
                {
                    if (DateTimeOffset.UtcNow >= deadline)
                    {
                        budgetExceeded = true;
                        break;
                    }

                    var (outcome, _) = await EvaluateWeakTestAsync(
                            baseline,
                            workRoot,
                            options.IntentPath,
                            options.MutationThresholdPercent,
                            ct)
                        .ConfigureAwait(false);
                    weakOutcomes.Add(outcome);
                    allAttributions.Add(outcome);
                }
            }

            if (!budgetExceeded && sweepWorkspaces.Count > 0)
            {
                var thresholds = options.ThresholdSweep ?? TransformCatalog.WeakTestThresholdSweep;
                var sweepResults = new Dictionary<TransformTag, IReadOnlyList<CandidateOutcomeKind>>();

                foreach (var (tag, workspace) in sweepWorkspaces)
                {
                    if (DateTimeOffset.UtcNow >= deadline)
                    {
                        budgetExceeded = true;
                        break;
                    }

                    var kinds = new List<CandidateOutcomeKind>();
                    foreach (var threshold in thresholds)
                    {
                        if (DateTimeOffset.UtcNow >= deadline)
                        {
                            budgetExceeded = true;
                            break;
                        }

                        kinds.Add(await RunMutationThresholdAsync(workspace, threshold, ct).ConfigureAwait(false));
                    }

                    sweepResults[tag] = kinds;
                }

                if (sweepResults.Count == sweepWorkspaces.Count)
                    sensitivityComplete = true;

                thresholdSensitivity = EscapeRateTally.BuildThresholdSensitivity(thresholds, sweepResults);
            }

            var status = budgetExceeded
                ? sensitivityComplete ? "completed-budget-truncated" : "completed-budget-truncated-before-sensitivity"
                : "completed";
            weakTestReport = EscapeRateTally.BuildDimensionReport(
                "MutationGate",
                status,
                weakOutcomes.Where(o => o.Family == TransformFamily.WeakTest).ToList(),
                weakOutcomes.Where(o => o.Family == TransformFamily.HonestBaseline).ToList(),
                TransformCatalog.WeakTestTags);
        }

        return new EscapeRateReport(
            EscapeRateReport.Version,
            TransformCatalog.CatalogVersion,
            adversaryMode,
            options.Seeds,
            options.MutationSample,
            options.BudgetMinutes,
            distinctWrongImplTrials,
            distinctWeakTestTrials,
            wrongImplCandidates.Count,
            weakSampleCandidates.Count,
            tools,
            wrongImplReport,
            weakTestReport,
            thresholdSensitivity,
            allAttributions,
            survivingExamples);
    }

    internal async Task<CandidateOutcome> EvaluateWrongImplAsync(
        AdversarialCandidate candidate,
        string workRoot,
        string? intentPath,
        CancellationToken ct)
    {
        var workspace = Path.Combine(workRoot, $"wrong-impl-{candidate.Seed:D4}-{candidate.Tag}");
        var definition = TransformAttribution.Get(candidate.Tag);
        var diff = TransformCatalog.BuildImplDiff(candidate.Tag, candidate.Seed);

        try
        {
            await MaterializeWorkspaceAsync(candidate, workspace, intentPath, ct).ConfigureAwait(false);

            var (buildCode, buildOut, buildErr, buildTimedOut) =
                await SpikeWorkspaceScaffold.BuildAsync(workspace, ct).ConfigureAwait(false);
            if (buildCode != 0 || buildTimedOut)
            {
                return Blocked(
                    candidate,
                    definition,
                    diff,
                    $"build failed: {TrimForReport(buildOut, buildErr)}");
            }

            var propertyResult = await _propertyGate.RunAsync(workspace, ct).ConfigureAwait(false);
            if (candidate.Family == TransformFamily.HonestBaseline)
            {
                return propertyResult.Passed
                    ? Accepted(candidate, definition)
                    : FalseReject(candidate, definition, propertyResult.RawOutput);
            }

            if (propertyResult.Passed)
            {
                if (!NegativeControlStripping.IsNegativeControlIntent(intentPath))
                {
                    var intent = ResolveHonestIntentPath(intentPath);
                    var identical = await BehavioralWitnessChecker.IsBehaviorallyIdenticalAsync(
                        HonestFixtures.Implementation,
                        candidate.ImplementationSource,
                        intent,
                        ct).ConfigureAwait(false);

                    if (identical)
                    {
                        return Caught(
                            candidate,
                            definition,
                            diff,
                            "vacuous: identical outputs on frozen witnesses",
                            propertyResult.RawOutput);
                    }
                }

                return Escape(
                    candidate,
                    definition,
                    diff,
                    TransformAttribution.ResolveMissingRelation(definition));
            }

            return Caught(
                candidate,
                definition,
                diff,
                TransformAttribution.ResolveCaughtBy(propertyResult.RawOutput, definition),
                propertyResult.RawOutput);
        }
        catch (Exception ex)
        {
            return Blocked(candidate, definition, diff, ex.Message);
        }
    }

    internal async Task<(CandidateOutcome Outcome, string WorkspacePath)> EvaluateWeakTestAsync(
        AdversarialCandidate candidate,
        string workRoot,
        string? intentPath,
        double mutationThresholdPercent,
        CancellationToken ct)
    {
        var workspace = Path.Combine(workRoot, $"weak-test-{candidate.Seed:D4}-{candidate.Tag}");
        var definition = TransformAttribution.Get(candidate.Tag);
        var diff = TransformCatalog.BuildTestDiff(candidate.Tag, candidate.Seed);

        try
        {
            await MaterializeWorkspaceAsync(candidate, workspace, intentPath, ct).ConfigureAwait(false);

            var (buildCode, buildOut, buildErr, buildTimedOut) =
                await SpikeWorkspaceScaffold.BuildAsync(workspace, ct).ConfigureAwait(false);
            if (buildCode != 0 || buildTimedOut)
            {
                return (Blocked(candidate, definition, diff, $"build failed: {TrimForReport(buildOut, buildErr)}"), workspace);
            }

            var (testCode, testOut, testErr, testTimedOut) =
                await SpikeWorkspaceScaffold.TestWithRebuildAsync(workspace, ct).ConfigureAwait(false);
            if (testCode != 0 || testTimedOut)
            {
                return (Blocked(candidate, definition, diff, $"tests failed: {TrimForReport(testOut, testErr)}"), workspace);
            }

            var mutationResult = await _mutationGate.RunAsync(workspace, mutationThresholdPercent, ct)
                .ConfigureAwait(false);

            if (candidate.Family == TransformFamily.HonestBaseline)
            {
                return mutationResult.Passed
                    ? (Accepted(candidate, definition), workspace)
                    : (FalseReject(candidate, definition, mutationResult.RawOutput), workspace);
            }

            if (mutationResult.Passed)
            {
                return (Escape(
                    candidate,
                    definition,
                    diff,
                    $"mutation score {mutationResult.MutationScore:F1}% >= {mutationThresholdPercent:F0}% threshold"), workspace);
            }

            return (Caught(
                candidate,
                definition,
                diff,
                TransformAttribution.ResolveCaughtBy(mutationResult.RawOutput, definition),
                mutationResult.RawOutput), workspace);
        }
        catch (Exception ex)
        {
            return (Blocked(candidate, definition, diff, ex.Message), workspace);
        }
    }

    private async Task<CandidateOutcomeKind> RunMutationThresholdAsync(
        string workspace,
        double threshold,
        CancellationToken ct)
    {
        var reportsDir = Path.Combine(workspace, "mutation-reports");
        if (Directory.Exists(reportsDir))
            Directory.Delete(reportsDir, recursive: true);

        var mutationResult = await _mutationGate.RunAsync(workspace, threshold, ct).ConfigureAwait(false);
        return mutationResult.Passed ? CandidateOutcomeKind.Escape : CandidateOutcomeKind.Caught;
    }

    private static CandidateOutcome Escape(
        AdversarialCandidate candidate,
        TransformDefinition definition,
        string diff,
        string missingRelation) =>
        new(
            candidate.Tag,
            candidate.Family,
            candidate.Seed,
            CandidateOutcomeKind.Escape,
            "gate passed",
            definition.Hypothesis,
            null,
            missingRelation,
            diff);

    private static CandidateOutcome Caught(
        AdversarialCandidate candidate,
        TransformDefinition definition,
        string diff,
        string caughtBy,
        string rawOutput) =>
        new(
            candidate.Tag,
            candidate.Family,
            candidate.Seed,
            CandidateOutcomeKind.Caught,
            TrimForReport(rawOutput),
            definition.Hypothesis,
            caughtBy,
            null,
            diff);

    private static CandidateOutcome Accepted(AdversarialCandidate candidate, TransformDefinition definition) =>
        new(
            candidate.Tag,
            candidate.Family,
            candidate.Seed,
            CandidateOutcomeKind.Accepted,
            "accepted",
            definition.Hypothesis,
            definition.ExpectedRelation,
            null,
            null);

    private static CandidateOutcome FalseReject(
        AdversarialCandidate candidate,
        TransformDefinition definition,
        string rawOutput) =>
        new(
            candidate.Tag,
            candidate.Family,
            candidate.Seed,
            CandidateOutcomeKind.FalseReject,
            TrimForReport(rawOutput),
            definition.Hypothesis,
            null,
            definition.ExpectedRelation,
            null);

    private static CandidateOutcome Blocked(
        AdversarialCandidate candidate,
        TransformDefinition definition,
        string diff,
        string detail) =>
        new(
            candidate.Tag,
            candidate.Family,
            candidate.Seed,
            CandidateOutcomeKind.Blocked,
            detail,
            definition.Hypothesis,
            null,
            null,
            diff);

    private static SurvivingExample ToSurvivingExample(
        string dimension,
        CandidateOutcome outcome,
        string workRoot,
        string reportRoot)
    {
        var workspace = Path.Combine(
            workRoot,
            $"{dimension}-{outcome.Seed:D4}-{outcome.Tag}");
        return new SurvivingExample(
            dimension,
            outcome.Tag.ToString(),
            outcome.Seed,
            ToReportRelativePath(workspace, reportRoot),
            outcome.Hypothesis,
            outcome.MissingRelation ?? outcome.CaughtBy ?? "n/a",
            outcome.DiffSummary ?? "(no diff)");
    }

    private static async Task MaterializeWorkspaceAsync(
        AdversarialCandidate candidate,
        string workspace,
        string? intentPath,
        CancellationToken ct)
    {
        SpikeWorkspaceScaffold.CreateFresh(workspace, overwrite: true);
        NegativeControlStripping.ApplyIfNeeded(workspace, intentPath);
        var intent = ResolveHonestIntentPath(intentPath);
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

    private static string ResolveHonestIntentPath(string? overridePath = null)
    {
        if (!string.IsNullOrWhiteSpace(overridePath) && File.Exists(overridePath))
            return overridePath;

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
