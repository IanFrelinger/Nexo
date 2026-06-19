using Nexo.Spike.S0;
using Nexo.Spike.S1.Adversary;
using Nexo.Spike.S1.Reporting;
using Nexo.Spike.S1.Transforms;

namespace Nexo.Spike.S1.IntentDensity;

public sealed class IntentDensityAnalyzer
{
    private readonly PropertyGate _propertyGate = new();
    private readonly string _honestImpl = HonestFixtures.Implementation;

    public Task<IntentDensityReport> AnalyzeAsync(
        double certificationThreshold = 0.95,
        CancellationToken ct = default) =>
        AnalyzeAsync(new IntentDensityOptions
        {
            Seeds = 8,
            CertificationThreshold = certificationThreshold
        }, ct);

    public async Task<IntentDensityReport> AnalyzeAsync(
        IntentDensityOptions options,
        CancellationToken ct = default)
    {
        var intentPath = ResolveHonestIntentPath(options.IntentPath);
        var probeResults = new List<ProbeClassResult>();

        foreach (var probe in ProbeCorpus.All)
        {
            ct.ThrowIfCancellationRequested();
            var seedWitnesses = new List<ProbeSeedWitnessResult>();
            var escapingSeeds = new List<int>();
            var vacuousSeeds = new List<int>();

            for (var seed = 0; seed < options.Seeds; seed++)
            {
                var honestPassed = await RunPropertyGateAsync(
                    TransformTag.HonestNoOp, seed, intentPath, ct).ConfigureAwait(false);

                var divergentPassed = await RunPropertyGateAsync(
                    probe.DivergentTransform, seed, intentPath, ct).ConfigureAwait(false);

                var identical = false;
                if (divergentPassed && !NegativeControlStripping.IsNegativeControlIntent(intentPath))
                {
                    var candidate = TransformCatalog.ApplyImplTransform(
                        probe.DivergentTransform, _honestImpl, seed);
                    identical = await BehavioralWitnessChecker.IsBehaviorallyIdenticalAsync(
                        _honestImpl, candidate, intentPath, ct).ConfigureAwait(false);
                    if (identical)
                        vacuousSeeds.Add(seed);
                }

                var pinnedForSeed = honestPassed && (!divergentPassed || identical);
                if (!pinnedForSeed)
                    escapingSeeds.Add(seed);

                seedWitnesses.Add(new ProbeSeedWitnessResult(
                    seed, honestPassed, divergentPassed, identical, pinnedForSeed));
            }

            var honestAll = seedWitnesses.All(s => s.HonestAccepted);
            var pinned = honestAll && escapingSeeds.Count == 0;
            var definition = TransformAttribution.Get(probe.DivergentTransform);
            var deciding = pinned
                ? definition.ExpectedRelation
                : escapingSeeds.Count > 0
                    ? $"unpinned seeds: {string.Join(",", escapingSeeds)}"
                    : "silent";

            probeResults.Add(new ProbeClassResult(
                probe.Id,
                probe.Description,
                pinned ? ProbePinStatus.Pinned : ProbePinStatus.Unpinned,
                deciding,
                probe.DivergentTransform,
                honestAll,
                seedWitnesses.Any(s => s.DivergentAccepted && !s.BehaviorallyIdentical),
                options.Seeds,
                escapingSeeds,
                vacuousSeeds,
                seedWitnesses));
        }

        var pinnedCount = probeResults.Count(p => p.Status == ProbePinStatus.Pinned);
        var density = probeResults.Count == 0 ? 0 : (double)pinnedCount / probeResults.Count;

        var certification = CertificationGate.Evaluate(
            density, probeResults, options.CertificationThreshold);

        return new IntentDensityReport(
            IntentDensityReport.Version,
            ProbeCorpus.ProbeCorpusVersion,
            TransformCatalog.CatalogVersion,
            options.Seeds,
            density,
            pinnedCount,
            probeResults.Count - pinnedCount,
            probeResults.Count,
            options.CertificationThreshold,
            certification,
            options.Equivalence,
            options.NegativeControl,
            probeResults);
    }

    private async Task<bool> RunPropertyGateAsync(
        TransformTag tag,
        int seed,
        string intentPath,
        CancellationToken ct)
    {
        var workspace = Path.Combine(
            Path.GetTempPath(),
            $"nexo-s1-density-{tag}-{seed}-{Guid.NewGuid():N}");

        try
        {
            SpikeWorkspaceScaffold.CreateFresh(workspace, overwrite: true);
            NegativeControlStripping.ApplyIfNeeded(workspace, intentPath);
            await BrickSpecLoader.WriteFrozenAsync(
                    workspace,
                    await BrickSpecLoader.LoadAsync(intentPath, ct).ConfigureAwait(false),
                    ct)
                .ConfigureAwait(false);

            var impl = TransformCatalog.ApplyImplTransform(tag, _honestImpl, seed);
            var tests = TransformCatalog.ApplyTestTransform(TransformTag.HonestNoOp, HonestFixtures.Tests, seed);

            File.WriteAllText(Path.Combine(workspace, "CsvColumnInferrer", "ColumnTypeInferrer.cs"), impl);
            File.WriteAllText(Path.Combine(workspace, "CsvColumnInferrer.Tests", "ColumnTypeInferrerRedTests.cs"), tests);

            var (buildCode, _, _, buildTimedOut) =
                await SpikeWorkspaceScaffold.BuildAsync(workspace, ct).ConfigureAwait(false);
            if (buildCode != 0 || buildTimedOut)
                return false;

            var propertyResult = await _propertyGate.RunAsync(workspace, ct).ConfigureAwait(false);
            return propertyResult.Passed;
        }
        finally
        {
            if (Directory.Exists(workspace))
            {
                try { Directory.Delete(workspace, recursive: true); }
                catch { /* best-effort */ }
            }
        }
    }

    private static string ResolveHonestIntentPath(string? overridePath)
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
}

public sealed class IntentDensityOptions
{
    public required int Seeds { get; init; }
    public double CertificationThreshold { get; init; } = 0.95;
    public string? IntentPath { get; init; }
    public CertificationEquivalenceReport? Equivalence { get; init; }
    public NegativeControlReport? NegativeControl { get; init; }
}

public static class CertificationGate
{
    public static CertificationResult Evaluate(
        double intentDensity,
        IReadOnlyList<ProbeClassResult> probeResults,
        double threshold)
    {
        var pinned = probeResults
            .Where(p => p.Status == ProbePinStatus.Pinned)
            .Select(p => p.ProbeClassId)
            .ToList();
        var unpinned = probeResults
            .Where(p => p.Status == ProbePinStatus.Unpinned)
            .Select(p => p.ProbeClassId)
            .ToList();

        if (intentDensity < threshold)
        {
            return new CertificationResult(
                CertificationVerdict.NotCertifiable,
                intentDensity,
                threshold,
                "acceptance criteria too sparse to certify",
                pinned,
                unpinned,
                unpinned);
        }

        if (unpinned.Count == 0)
        {
            return new CertificationResult(
                CertificationVerdict.Certifiable,
                intentDensity,
                threshold,
                "all probe classes pinned across all witness seeds",
                pinned,
                unpinned,
                []);
        }

        return new CertificationResult(
            CertificationVerdict.CertifiableWithScope,
            intentDensity,
            threshold,
            "certifiable within pinned scope; unpinned classes are explicit out-of-scope",
            pinned,
            unpinned,
            unpinned);
    }
}
