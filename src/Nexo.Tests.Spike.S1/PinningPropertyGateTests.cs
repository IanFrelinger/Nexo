using FluentAssertions;
using Nexo.Spike.S0;
using Nexo.Spike.S1.Adversary;
using Nexo.Spike.S1.IntentDensity;
using Nexo.Spike.S1.Transforms;
using Xunit;

namespace Nexo.Tests.Spike.S1;

public sealed class PinningPropertyGateTests
{
    [Theory]
    [MemberData(nameof(NewlyPinnedProbes))]
    public async Task Honest_impl_passes_denser_oracle_for_pinned_probe(string probeId)
    {
        var probe = ProbeCorpus.All.Single(p => p.Id == probeId);
        var passed = await RunPropertyGateAsync(TransformTag.HonestNoOp);
        passed.Should().BeTrue(because: $"honest impl must pass densified oracle for {probeId}");
    }

    [Theory]
    [MemberData(nameof(NewlyPinnedProbes))]
    public async Task Divergent_transform_rejected_for_pinned_probe(string probeId)
    {
        var probe = ProbeCorpus.All.Single(p => p.Id == probeId);
        var passed = await RunPropertyGateAsync(probe.DivergentTransform);
        passed.Should().BeFalse(because: $"divergent {probe.DivergentTransform} must fail densified oracle for {probeId}");
    }

    public static IEnumerable<object[]> NewlyPinnedProbes() =>
    [
        ["zero-one-literals"],
        ["leading-zeros"],
        ["thousands-separator"],
        ["locale-comma-decimal"],
        ["signed-zero"],
        ["boolean-yes-no"],
        ["boolean-yn"],
        ["whitespace-only"],
        ["sampling-window-widening"]
    ];

    private static async Task<bool> RunPropertyGateAsync(TransformTag tag)
    {
        var workspace = Path.Combine(Path.GetTempPath(), $"nexo-s1-pin-{tag}-{Guid.NewGuid():N}");
        var gate = new PropertyGate();

        try
        {
            SpikeWorkspaceScaffold.CreateFresh(workspace, overwrite: true);
            var intent = ResolveHonestIntentPath();
            await BrickSpecLoader.WriteFrozenAsync(
                workspace,
                await BrickSpecLoader.LoadAsync(intent));

            var impl = TransformCatalog.ApplyImplTransform(tag, HonestFixtures.Implementation, seed: 0);
            File.WriteAllText(Path.Combine(workspace, "CsvColumnInferrer", "ColumnTypeInferrer.cs"), impl);
            File.WriteAllText(
                Path.Combine(workspace, "CsvColumnInferrer.Tests", "ColumnTypeInferrerRedTests.cs"),
                HonestFixtures.Tests);

            var (buildCode, _, _, buildTimedOut) = await SpikeWorkspaceScaffold.BuildAsync(workspace);
            if (buildCode != 0 || buildTimedOut)
                return false;

            var result = await gate.RunAsync(workspace);
            return result.Passed;
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

    private static string ResolveHonestIntentPath()
    {
        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "src", "Nexo.Spike.S1", "Fixtures", "honest-csv-inferrer.json")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Nexo.Spike.S1", "Fixtures", "honest-csv-inferrer.json"))
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path))
                return path;
        }

        throw new FileNotFoundException("honest-csv-inferrer.json fixture not found");
    }
}
