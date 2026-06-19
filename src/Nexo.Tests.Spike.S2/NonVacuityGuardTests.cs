using FluentAssertions;
using Nexo.Spike.S2;
using Nexo.Spike.S2.Adversary;
using Nexo.Spike.S2.Gates;
using Nexo.Spike.S2.Oracle;
using Nexo.Spike.S2.Reporting;
using Xunit;

namespace Nexo.Tests.Spike.S2;

public sealed class NonVacuityGuardTests
{
    [Fact]
    public async Task Mock_candidates_classify_as_rejected_true_escape_and_benign_pass()
    {
        var oracle = await ReferenceOracleLoader.LoadAsync(FindRepoFile("artifacts/s2/reference-oracle.json"));
        var judge = new ReferenceOracleJudge();
        var gateStack = new FullGateStack();

        var workspaceRoot = Path.Combine(Path.GetTempPath(), $"nexo-s2-nv-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workspaceRoot);

        try
        {
            var rejected = await EvaluateAsync(
                MockCandidates.RejectedConstantReturn,
                workspaceRoot,
                "rejected",
                gateStack,
                judge,
                oracle);

            rejected.Outcome.Should().Be(AdaptiveOutcomeKind.Rejected);

            var trueEscape = await EvaluateAsync(
                MockCandidates.TrueEscapeHeldOut999,
                workspaceRoot,
                "true-escape",
                gateStack,
                judge,
                oracle,
                requireMutation: false);

            trueEscape.Outcome.Should().Be(AdaptiveOutcomeKind.TrueEscape);
            trueEscape.HeldOutWrong.Should().NotBeEmpty();

            var benign = await EvaluateAsync(
                MockCandidates.BenignPassHonest,
                workspaceRoot,
                "benign",
                gateStack,
                judge,
                oracle,
                requireMutation: false);

            benign.Outcome.Should().Be(AdaptiveOutcomeKind.BenignPass);
        }
        finally
        {
            if (Directory.Exists(workspaceRoot))
            {
                try { Directory.Delete(workspaceRoot, recursive: true); }
                catch { /* best-effort */ }
            }
        }
    }

    [Fact]
    public async Task Mock_harness_report_proves_non_vacuity_when_mutation_optional()
    {
        var output = Path.Combine(Path.GetTempPath(), $"nexo-s2-report-{Guid.NewGuid():N}");
        var harness = new AdaptiveEscapeHarness();

        var report = await harness.RunAsync(new AdaptiveEscapeHarnessOptions
        {
            Intents = 1,
            AttemptsPerIntent = 3,
            OutputDirectory = output,
            IntentPath = FindRepoFile(Path.Combine("src", "Nexo.Spike.S1", "Fixtures", "honest-csv-inferrer.json")),
            ReferenceOraclePath = FindRepoFile("artifacts/s2/reference-oracle.json"),
            RequireMutation = false,
            Adversary = new DeterministicMockAdversary()
        });

        report.NonVacuityProven.Should().BeTrue();
        report.TrueEscapeCount.Should().BeGreaterThan(0);
        report.BenignPassCount.Should().BeGreaterThan(0);
        report.RejectedCount.Should().BeGreaterThan(0);
        report.NewDefectBacklog.Should().NotBeEmpty();
        report.AdversaryBackend.Should().Be(AdaptiveAdversaryFactory.MockMode);
    }

    private static async Task<(AdaptiveOutcomeKind Outcome, IReadOnlyList<OracleDisagreement> HeldOutWrong)> EvaluateAsync(
        string impl,
        string workRoot,
        string label,
        FullGateStack gateStack,
        ReferenceOracleJudge judge,
        ReferenceOracle oracle,
        bool requireMutation = true)
    {
        var workspace = Path.Combine(workRoot, label);
        var intentPath = FindRepoFile(Path.Combine("src", "Nexo.Spike.S1", "Fixtures", "honest-csv-inferrer.json"));
        var spec = await Nexo.Spike.S0.BrickSpecLoader.LoadAsync(intentPath);
        Nexo.Spike.S0.SpikeWorkspaceScaffold.CreateFresh(workspace, overwrite: true);
        await Nexo.Spike.S0.BrickSpecLoader.WriteFrozenAsync(workspace, spec);
        await File.WriteAllTextAsync(
            Path.Combine(workspace, "CsvColumnInferrer", "ColumnTypeInferrer.cs"),
            impl);
        await File.WriteAllTextAsync(
            Path.Combine(workspace, "CsvColumnInferrer.Tests", "ColumnTypeInferrerRedTests.cs"),
            Nexo.Spike.S1.Transforms.HonestFixtures.Tests);

        var gate = await gateStack.EvaluateAsync(workspace, 80, requireMutation);
        if (!gate.AllPassed)
            return (AdaptiveOutcomeKind.Rejected, []);

        var judgment = await judge.JudgeAsync(impl, oracle);
        var heldOutWrong = judge.HeldOutDisagreements(judgment, oracle);
        if (heldOutWrong.Count > 0)
            return (AdaptiveOutcomeKind.TrueEscape, heldOutWrong);

        return (AdaptiveOutcomeKind.BenignPass, []);
    }

    private static string FindRepoFile(string relativePath)
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        throw new FileNotFoundException($"Repo file not found: {relativePath}");
    }
}
