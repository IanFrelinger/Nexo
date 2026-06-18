using FluentAssertions;
using Nexo.Spike.S0;
using Nexo.Spike.S0.Agents;
using Nexo.Spike.S0.Models;
using Xunit;

namespace Nexo.Tests.Spike.S0;

[Collection("SpikeS0 serial")]
public sealed class TddLoopRunnerTests
{
    private static string Fixture(string relative) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Fixtures", relative));

    private static string ResolveIntentPath()
    {
        var candidates = new[]
        {
            Fixture("intents/honest-csv-inferrer.json"),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "samples", "spike-s0", "intents", "honest-csv-inferrer.json")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "samples", "spike-s0", "intents", "honest-csv-inferrer.json"))
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
                return candidate;
        }

        throw new FileNotFoundException("Could not locate honest-csv-inferrer.json intent.");
    }

    [Fact]
    public async Task Run_rejects_tautological_tests_at_RED()
    {
        var workspace = Path.Combine(Path.GetTempPath(), "nexo-s0-" + Guid.NewGuid().ToString("N"));
        var testAuthor = new ScriptedStageAgent("test-author", new Dictionary<SpikeStage, IReadOnlyList<(string, string)>>
        {
            [SpikeStage.Red] =
            [
                (Fixture("red/tautology-tests.cs"), "CsvColumnInferrer.Tests/ColumnTypeInferrerTests.cs")
            ]
        });
        var implementer = new ScriptedStageAgent("implementer", new Dictionary<SpikeStage, IReadOnlyList<(string, string)>>());

        var runner = new TddLoopRunner(testAuthor, implementer, new MutationGate());
        var log = await runner.RunAsync(new SpikeLoopOptions
        {
            IntentPath = ResolveIntentPath(),
            WorkspaceRoot = workspace,
            SkipMutation = true,
            MaxRedGreenBounces = 1
        });

        log.Outcome.Should().Be(SpikeRunOutcome.Rejected);
        log.RejectingGuard.Should().Be(GuardKind.Tautology);
        log.RejectedAtStage.Should().Be(SpikeStage.Red);
    }

    [Fact]
    public async Task Run_reaches_green_with_honest_tests_and_skip_mutation()
    {
        var workspace = Path.Combine(Path.GetTempPath(), "nexo-s0-" + Guid.NewGuid().ToString("N"));
        var testAuthor = new ScriptedStageAgent("test-author", new Dictionary<SpikeStage, IReadOnlyList<(string, string)>>
        {
            [SpikeStage.Red] =
            [
                (Fixture("red/honest-tests.cs"), "CsvColumnInferrer.Tests/ColumnTypeInferrerTests.cs")
            ]
        });
        var implementer = new ScriptedStageAgent("implementer", new Dictionary<SpikeStage, IReadOnlyList<(string, string)>>
        {
            [SpikeStage.Green] =
            [
                (Fixture("green/honest-impl.cs"), "CsvColumnInferrer/ColumnTypeInferrer.cs")
            ]
        });

        var runner = new TddLoopRunner(testAuthor, implementer, new MutationGate());
        var log = await runner.RunAsync(new SpikeLoopOptions
        {
            IntentPath = ResolveIntentPath(),
            WorkspaceRoot = workspace,
            SkipMutation = true,
            MaxRedGreenBounces = 1
        });

        log.Outcome.Should().Be(SpikeRunOutcome.Success);
        log.RejectingGuard.Should().Be(GuardKind.None);
    }
}
