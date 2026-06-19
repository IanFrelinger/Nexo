using System.Text.Json;
using System.Text.Json.Serialization;
using Nexo.Spike.S0.Models;
using Nexo.Spike.S2.Adversary;

namespace Nexo.Spike.S2.Oracle;

public static class ReferenceOracleLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static async Task<ReferenceOracle> LoadAsync(string? path = null, CancellationToken ct = default)
    {
        var resolved = path ?? AdversaryAccessPolicy.ResolveReferenceOraclePath();
        await using var stream = File.OpenRead(resolved);
        var dto = await JsonSerializer.DeserializeAsync<ReferenceOracleDto>(stream, JsonOptions, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Reference oracle JSON is empty.");

        return new ReferenceOracle(
            dto.Version ?? ReferenceOracle.VersionId,
            dto.Provenance ?? string.Empty,
            dto.Labels?.Select(l => new ReferenceLabel(l.Inputs ?? [], l.ExpectedType ?? "String", l.Source ?? "held-out")).ToList()
            ?? []);
    }

    public static IReadOnlySet<string> NormalizeInputKey(IReadOnlyList<string> inputs) =>
        new HashSet<string>(inputs, StringComparer.Ordinal);

    public static string SerializeInputKey(IReadOnlyList<string> inputs) =>
        JsonSerializer.Serialize(inputs);

    public static IReadOnlySet<string> ExtractGatePinnedInputKeys(BrickSpec spec)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var criterion in spec.AcceptanceCriteria)
        {
            if (criterion.StartsWith("Mixed numeric and text", StringComparison.OrdinalIgnoreCase))
            {
                keys.Add(SerializeInputKey(["1", "hello"]));
                continue;
            }

            var parsed = ParseCriterion(criterion);
            if (parsed is not null)
                keys.Add(SerializeInputKey(parsed.Value.Inputs));
        }

        return keys;
    }

    public static (IReadOnlyList<string> Inputs, string ExpectedType)? ParseCriterion(string criterion)
    {
        var trimmed = criterion.Trim();
        var arrow = trimmed.IndexOf("=>", StringComparison.Ordinal);
        if (arrow < 0)
            return null;

        var left = trimmed[..arrow].Trim();
        var right = trimmed[(arrow + 2)..].Trim();
        if (!left.StartsWith('[') || !left.EndsWith(']'))
            return null;

        var valuesJson = left.Replace('\'', '"');
        try
        {
            var inputs = JsonSerializer.Deserialize<string[]>(valuesJson) ?? [];
            return (inputs, right);
        }
        catch
        {
            return null;
        }
    }

    public static void AssertStrictSuperset(ReferenceOracle oracle, BrickSpec gateSpec)
    {
        var gateKeys = ExtractGatePinnedInputKeys(gateSpec);
        var oracleKeys = oracle.Labels.Select(l => SerializeInputKey(l.Inputs)).ToHashSet(StringComparer.Ordinal);

        foreach (var gateKey in gateKeys)
        {
            if (!oracleKeys.Contains(gateKey))
            {
                throw new InvalidOperationException(
                    $"Reference oracle missing gate-pinned input key: {gateKey}");
            }
        }

        if (oracle.HeldOutLabels.Count == 0)
        {
            throw new InvalidOperationException(
                "Reference oracle must include held-out labels beyond gate-pinned acceptance.");
        }

        var heldOutOnly = oracle.HeldOutLabels
            .Where(l => !gateKeys.Contains(SerializeInputKey(l.Inputs)))
            .ToList();

        if (heldOutOnly.Count == 0)
        {
            throw new InvalidOperationException(
                "Reference oracle held-out labels must include inputs not pinned by gate acceptance criteria.");
        }
    }

    private sealed class ReferenceOracleDto
    {
        public string? Version { get; set; }
        public string? Provenance { get; set; }
        public List<ReferenceLabelDto>? Labels { get; set; }
    }

    private sealed class ReferenceLabelDto
    {
        public List<string>? Inputs { get; set; }
        public string? ExpectedType { get; set; }
        public string? Source { get; set; }
    }
}
