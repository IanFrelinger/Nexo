using FluentAssertions;
using Nexo.Spike.S0;
using Nexo.Spike.S0.Models;
using Xunit;

namespace Nexo.Tests.Spike.S0;

public sealed class PropertyGateTests
{
    [Fact]
    public async Task Property_gate_rejects_off_by_one_wrong_implementation()
    {
        var workspace = Path.Combine(Path.GetTempPath(), "nexo-s0-prop-" + Guid.NewGuid().ToString("N"));
        SpikeWorkspaceScaffold.CreateFresh(workspace, overwrite: false);

        var intent = ResolveIntent("adversarial-off-by-one.json");
        await BrickSpecLoader.WriteFrozenAsync(workspace, await BrickSpecLoader.LoadAsync(intent));

        File.Copy(
            Fixture("green/wrong-off-by-one-impl.cs"),
            Path.Combine(workspace, "CsvColumnInferrer", "ColumnTypeInferrer.cs"),
            overwrite: true);

        var gate = new PropertyGate();
        var result = await gate.RunAsync(workspace);

        result.Passed.Should().BeFalse();
        result.RawOutput.Should().Contain("acceptance criterion");
    }

    [Fact]
    public async Task Property_gate_accepts_honest_implementation_against_honest_spec()
    {
        var workspace = Path.Combine(Path.GetTempPath(), "nexo-s0-prop-" + Guid.NewGuid().ToString("N"));
        SpikeWorkspaceScaffold.CreateFresh(workspace, overwrite: false);

        var intent = ResolveIntent("honest-csv-inferrer.json");
        await BrickSpecLoader.WriteFrozenAsync(workspace, await BrickSpecLoader.LoadAsync(intent));

        File.Copy(
            Fixture("green/honest-impl.cs"),
            Path.Combine(workspace, "CsvColumnInferrer", "ColumnTypeInferrer.cs"),
            overwrite: true);

        var gate = new PropertyGate();
        var result = await gate.RunAsync(workspace);

        result.Passed.Should().BeTrue(because: result.RawOutput);
    }

    private static string Fixture(string relative) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Fixtures", relative));

    private static string ResolveIntent(string name)
    {
        var candidates = new[]
        {
            Fixture($"intents/{name}"),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "samples", "spike-s0", "intents", name)),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "samples", "spike-s0", "intents", name))
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
                return candidate;
        }

        throw new FileNotFoundException(name);
    }
}
