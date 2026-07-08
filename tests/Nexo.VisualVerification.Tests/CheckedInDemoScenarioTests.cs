using System.Text.Json;
using FluentAssertions;
using Nexo.VisualVerification;
using Nexo.VisualVerification.Providers;
using Nexo.VisualVerification.Tests.Support;
using Xunit;

namespace Nexo.VisualVerification.Tests;

[Trait("Category", "VisualVerification")]
public sealed class CheckedInDemoScenarioTests
{
    [Fact]
    public async Task CheckedInDemoScenario_ProducesDeterministicExactMatch()
    {
        var scenarioPath = Path.Combine(AppContext.BaseDirectory, "visual-verification", "demo-scenario.json");
        var json = await File.ReadAllTextAsync(scenarioPath);
        var payload = JsonSerializer.Deserialize<DemoScenarioPayload>(json, CanonicalJson.Options)
            ?? throw new InvalidOperationException("Demo scenario missing.");

        var scenario = new ScenarioDefinition(
            payload.Seed,
            payload.FixedTimestepSeconds,
            payload.TotalSteps,
            payload.InputScript.Select(e => new ScriptedInputEvent(e.Step, e.Channel, e.Payload)).ToArray());

        var storageRoot = Path.Combine(Path.GetTempPath(), $"nexo-eye-demo-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(storageRoot);

        var record = await HarnessTestSupport.RunCertifiedSessionAsync(scenario, storageRoot);
        record.Verdict.Tier.Should().Be(VerdictTier.ExactMatch);
    }

    private sealed record DemoScenarioPayload(
        ulong Seed,
        double FixedTimestepSeconds,
        int TotalSteps,
        IReadOnlyList<ScriptedInputEvent> InputScript);
}
