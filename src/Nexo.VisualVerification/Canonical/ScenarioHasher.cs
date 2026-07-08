using System.Text.Json;

namespace Nexo.VisualVerification;

/// <summary>Computes the content-addressed scenario hash.</summary>
public static class ScenarioHasher
{
    public static string ComputeHash(ScenarioDefinition scenario)
    {
        var canonical = BuildCanonicalJson(scenario);
        return ContentSha256.ComputeHex(canonical);
    }

    internal static string BuildCanonicalJson(ScenarioDefinition scenario)
    {
        var events = scenario.InputScript
            .OrderBy(e => e.Step)
            .ThenBy(e => e.Channel, StringComparer.Ordinal)
            .ThenBy(e => e.Payload, StringComparer.Ordinal)
            .Select(e => new CanonicalInputEvent(e.Step, e.Channel, e.Payload))
            .ToArray();

        var payload = new CanonicalScenario(
            scenario.Seed,
            scenario.FixedTimestepSeconds,
            scenario.TotalSteps,
            events);

        return JsonSerializer.Serialize(payload, CanonicalJson.Options);
    }

    private sealed record CanonicalScenario(
        ulong Seed,
        double FixedTimestepSeconds,
        int TotalSteps,
        IReadOnlyList<CanonicalInputEvent> InputScript);

    private sealed record CanonicalInputEvent(int Step, string Channel, string Payload);
}
