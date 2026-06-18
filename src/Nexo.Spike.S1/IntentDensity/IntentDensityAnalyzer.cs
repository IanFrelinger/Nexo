using Nexo.Spike.S0;
using Nexo.Spike.S1.Adversary;
using Nexo.Spike.S1.Reporting;
using Nexo.Spike.S1.Transforms;

namespace Nexo.Spike.S1.IntentDensity;

public sealed class IntentDensityAnalyzer
{
    private readonly PropertyGate _propertyGate = new();

    public async Task<IntentDensityReport> AnalyzeAsync(
        double certificationThreshold = 0.95,
        CancellationToken ct = default)
    {
        var probeResults = new List<ProbeClassResult>();

        foreach (var probe in ProbeCorpus.All)
        {
            ct.ThrowIfCancellationRequested();
            var honestPassed = await RunPropertyGateAsync(
                TransformTag.HonestNoOp,
                TransformFamily.HonestBaseline,
                seed: 0,
                ct).ConfigureAwait(false);

            var divergentPassed = await RunPropertyGateAsync(
                probe.DivergentTransform,
                TransformFamily.WrongImpl,
                seed: 0,
                ct).ConfigureAwait(false);

            var pinned = honestPassed && !divergentPassed;
            var definition = TransformAttribution.Get(probe.DivergentTransform);
            var deciding = pinned
                ? definition.ExpectedRelation
                : "silent";

            probeResults.Add(new ProbeClassResult(
                probe.Id,
                probe.Description,
                pinned ? ProbePinStatus.Pinned : ProbePinStatus.Unpinned,
                deciding,
                probe.DivergentTransform,
                honestPassed,
                divergentPassed));
        }

        var pinnedCount = probeResults.Count(p => p.Status == ProbePinStatus.Pinned);
        var unpinnedCount = probeResults.Count - pinnedCount;
        var density = probeResults.Count == 0 ? 0 : (double)pinnedCount / probeResults.Count;

        var certification = CertificationGate.Evaluate(
            density,
            probeResults,
            certificationThreshold);

        return new IntentDensityReport(
            IntentDensityReport.Version,
            ProbeCorpus.ProbeCorpusVersion,
            TransformCatalog.CatalogVersion,
            density,
            pinnedCount,
            unpinnedCount,
            probeResults.Count,
            certificationThreshold,
            certification,
            probeResults);
    }

    private async Task<bool> RunPropertyGateAsync(
        TransformTag tag,
        TransformFamily family,
        int seed,
        CancellationToken ct)
    {
        var workspace = Path.Combine(
            Path.GetTempPath(),
            $"nexo-s1-density-{tag}-{seed}-{Guid.NewGuid():N}");

        try
        {
            SpikeWorkspaceScaffold.CreateFresh(workspace, overwrite: true);
            var intent = ResolveHonestIntentPath();
            await BrickSpecLoader.WriteFrozenAsync(
                    workspace,
                    await BrickSpecLoader.LoadAsync(intent, ct).ConfigureAwait(false),
                    ct)
                .ConfigureAwait(false);

            var impl = TransformCatalog.ApplyImplTransform(tag, HonestFixtures.Implementation, seed);
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
                try
                {
                    Directory.Delete(workspace, recursive: true);
                }
                catch
                {
                    // best-effort cleanup
                }
            }
        }
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
                "all probe classes pinned by frozen oracle",
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
