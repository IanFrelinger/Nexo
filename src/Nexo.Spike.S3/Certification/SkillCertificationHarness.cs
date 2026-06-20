using System.Text.RegularExpressions;
using Nexo.Spike.S0;
using Nexo.Spike.S0.Guards;
using Nexo.Spike.S0.Models;
using Nexo.Spike.S0.Roles;
using Nexo.Spike.S1;
using Nexo.Spike.S1.IntentDensity;
using Nexo.Spike.S1.Reporting;
using Nexo.Spike.S1.Transforms;
using Nexo.Spike.S2.Gates;
using Nexo.Spike.S3.Models;
using Nexo.Spike.S3.Registry;

namespace Nexo.Spike.S3.Certification;

/// <summary>
/// Runs the S0→S2 certification envelope and emits a signed promotion-contract record.
/// </summary>
public sealed class SkillCertificationHarness : ISkillCertificationHarness
{
    private static readonly Regex MutationScoreRegex = new(
        @"mutation score (?:of |is )(\d+(?:\.\d+)?)\s*%",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly FullGateStack _gateStack = new();
    private readonly double _mutationThreshold;
    private readonly bool _requireMutation;
    private readonly int _densitySeeds;
    private readonly int _escapeSeeds;

    public SkillCertificationHarness(
        double mutationThresholdPercent = 80,
        bool requireMutation = true,
        int densitySeeds = 4,
        int escapeSeeds = 2)
    {
        _mutationThreshold = mutationThresholdPercent;
        _requireMutation = requireMutation;
        _densitySeeds = densitySeeds;
        _escapeSeeds = escapeSeeds;
    }

    public async Task<CertificationResult> CertifyAsync(SkillCandidate candidate, CancellationToken ct = default)
    {
        var spec = await BrickSpecLoader.LoadAsync(candidate.IntentSpecPath, ct).ConfigureAwait(false);

        if (!AttestRoleSeparation(spec))
        {
            return Reject("role-separation attestation failed");
        }

        var redReason = await VerifyRedDisciplineAsync(candidate.IntentSpecPath, ct).ConfigureAwait(false);
        if (redReason != RedFailureReason.AssertionFailure)
        {
            return Reject($"RED discipline requires AssertionFailure (observed {redReason})");
        }

        var workRoot = Path.Combine(Path.GetTempPath(), "nexo-s3-cert", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workRoot);
        var workspace = Path.Combine(workRoot, "workspace");

        try
        {
            SpikeWorkspaceScaffold.CreateFresh(workspace, overwrite: true);
            await BrickSpecLoader.WriteFrozenAsync(workspace, spec, ct).ConfigureAwait(false);

            File.WriteAllText(
                Path.Combine(workspace, "CsvColumnInferrer", "ColumnTypeInferrer.cs"),
                candidate.ImplementationSource);
            File.WriteAllText(
                Path.Combine(workspace, "CsvColumnInferrer.Tests", "ColumnTypeInferrerRedTests.cs"),
                HonestFixtures.Tests);

            var gateResult = await _gateStack
                .EvaluateAsync(workspace, _mutationThreshold, _requireMutation, ct)
                .ConfigureAwait(false);

            if (!gateResult.AllPassed)
            {
                return Reject(
                    $"gate stack failed at {gateResult.FailedGate}: {Trim(gateResult.Detail)}");
            }

            if (gateResult.MutationSkipped)
            {
                return Reject("mutation gate skipped — promotion contract requires mutation score");
            }

            var mutationScore = ParseMutationScore(gateResult.Detail);
            if (mutationScore < _mutationThreshold)
            {
                return Reject(
                    $"mutation score {mutationScore:F1}% below threshold {_mutationThreshold:F1}%");
            }

            var densityReport = await new IntentDensityAnalyzer().AnalyzeAsync(
                new IntentDensityOptions
                {
                    Seeds = _densitySeeds,
                    CertificationThreshold = 0.95,
                    IntentPath = candidate.IntentSpecPath
                },
                ct).ConfigureAwait(false);

            var escapeHarness = new EscapeRateHarness();
            var escapeReport = await escapeHarness.RunAsync(
                new EscapeRateHarnessOptions
                {
                    Seeds = _escapeSeeds,
                    MutationSample = 0,
                    BudgetMinutes = 10,
                    OutputDirectory = Path.Combine(workRoot, "escape"),
                    IntentPath = candidate.IntentSpecPath
                },
                ct).ConfigureAwait(false);

            var equivalence = CertificationEquivalence.Evaluate(densityReport, escapeReport);
            if (!equivalence.EquivalenceHolds || densityReport.IntentDensity < 1.0 - 1e-9)
            {
                return Reject(
                    $"certification equivalence failed: density={densityReport.IntentDensity:P1}, " +
                    $"escapes={escapeReport.WrongImpl.Escapes}");
            }

            var unsigned = new SignedCertificationRecord(
                RecordId: Guid.NewGuid().ToString("N"),
                RedReason: redReason,
                MutationScore: mutationScore,
                MutationThreshold: _mutationThreshold,
                MutationSkipped: false,
                IntentDensity: densityReport.IntentDensity,
                EscapeRate: escapeReport.WrongImpl.EscapeRate,
                RoleSeparationAttested: true,
                CatalogVersion: TransformCatalog.CatalogVersion,
                ProbeCorpusVersion: ProbeCorpus.ProbeCorpusVersion,
                CertifiedAt: DateTimeOffset.UtcNow,
                Signature: string.Empty);

            var signed = CertificationRecordSigner.WithSignature(unsigned);
            return new CertificationResult(true, signed, null);
        }
        finally
        {
            try
            {
                if (Directory.Exists(workRoot))
                    Directory.Delete(workRoot, recursive: true);
            }
            catch
            {
                // best-effort cleanup
            }
        }
    }

    private static bool AttestRoleSeparation(BrickSpec spec)
    {
        var testAuthor = RoleScope.ForTestAuthor(spec);
        var implementer = RoleScope.ForImplementer(spec);
        return testAuthor.CanEditTests
               && !testAuthor.CanEditImplementation
               && implementer.CanEditImplementation
               && !implementer.CanEditTests
               && testAuthor.VisibleTestAuthorPrompt
               && !testAuthor.VisibleImplementerPrompt
               && implementer.VisibleImplementerPrompt
               && !implementer.VisibleTestAuthorPrompt;
    }

    private static async Task<RedFailureReason> VerifyRedDisciplineAsync(
        string intentPath,
        CancellationToken ct)
    {
        var workspace = Path.Combine(Path.GetTempPath(), "nexo-s3-red", Guid.NewGuid().ToString("N"));
        try
        {
            SpikeWorkspaceScaffold.CreateFresh(workspace, overwrite: true);
            var spec = await BrickSpecLoader.LoadAsync(intentPath, ct).ConfigureAwait(false);
            await BrickSpecLoader.WriteFrozenAsync(workspace, spec, ct).ConfigureAwait(false);

            File.WriteAllText(
                Path.Combine(workspace, "CsvColumnInferrer.Tests", "ColumnTypeInferrerRedTests.cs"),
                HonestFixtures.Tests);

            var (code, stdout, stderr, timedOut) =
                await SpikeWorkspaceScaffold.TestWithRebuildAsync(workspace, ct).ConfigureAwait(false);

            return RedReasonClassifier.Classify(code, stdout, stderr, timedOut);
        }
        finally
        {
            try
            {
                if (Directory.Exists(workspace))
                    Directory.Delete(workspace, recursive: true);
            }
            catch
            {
                // best-effort cleanup
            }
        }
    }

    private static double ParseMutationScore(string detail)
    {
        var match = MutationScoreRegex.Match(detail);
        return match.Success && double.TryParse(match.Groups[1].Value, out var score)
            ? score
            : 0;
    }

    private static CertificationResult Reject(string reason) =>
        new(false, null, reason);

    private static string Trim(string value, int max = 300) =>
        value.Length <= max ? value : value[..max] + "...";
}
