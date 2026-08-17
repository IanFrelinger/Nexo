using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Nexo.Core.Application.Autonomy;
using Nexo.Core.Application.Certification.Models;

namespace Nexo.BackgroundAgents.Autonomy;

/// <summary>
/// Loads the two human-authored artifacts that sit beside an objective in the store:
/// its witness and (for replayed proposals) its recorded candidate. Both live next to
/// the objective markdown, following the store's existing file convention:
/// <c>{objectivesRoot}/{status}/{id}.witness.json</c> and <c>{id}.proposal.json</c>.
///
/// <para><b>Everything here is fail-closed.</b> A missing or unparseable witness means the
/// objective does not run — never that it runs unwitnessed. An objective with no
/// acceptance criteria is not a small problem to warn about; a certificate minted without
/// one would be a claim about nothing, which is precisely the failure the whole gate
/// exists to prevent.</para>
/// </summary>
[Experimental(AutonomyExperimental.DiagnosticId, UrlFormat = AutonomyExperimental.UrlFormat)]
public static class ObjectiveArtifacts
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>Witness file for an objective, or null when absent.</summary>
    public static WitnessSpec? LoadWitness(string objectiveFilePath)
    {
        var path = SiblingPath(objectiveFilePath, ".witness.json");
        if (path is null || !File.Exists(path))
            return null;

        var dto = JsonSerializer.Deserialize<WitnessDto>(File.ReadAllText(path), Json);
        if (dto?.Cases is null || dto.Cases.Count == 0 || string.IsNullOrWhiteSpace(dto.BrickId))
            return null;

        var cases = new List<WitnessCase>(dto.Cases.Count);
        foreach (var c in dto.Cases)
        {
            if (c.Input is null || c.ExpectedOutput is null)
                return null;
            cases.Add(new WitnessCase(Unwrap(c.Input), Unwrap(c.ExpectedOutput)));
        }

        return new WitnessSpec(dto.BrickId!, cases);
    }

    /// <summary>Recorded proposal for an objective, or null when absent.</summary>
    public static ProposedSource? LoadRecordedProposal(string objectiveFilePath)
    {
        var path = SiblingPath(objectiveFilePath, ".proposal.json");
        if (path is null || !File.Exists(path))
            return null;

        var dto = JsonSerializer.Deserialize<ProposalDto>(File.ReadAllText(path), Json);
        if (dto is null || string.IsNullOrWhiteSpace(dto.SourceCode) || string.IsNullOrWhiteSpace(dto.TypeName))
            return null;

        return new ProposedSource(
            dto.SourceCode!,
            dto.TypeName!,
            string.IsNullOrWhiteSpace(dto.ProposerSignature) ? "recorded:unsigned" : dto.ProposerSignature!);
    }

    private static string? SiblingPath(string objectiveFilePath, string suffix)
    {
        if (string.IsNullOrWhiteSpace(objectiveFilePath))
            return null;
        var dir = Path.GetDirectoryName(objectiveFilePath);
        var id = Path.GetFileNameWithoutExtension(objectiveFilePath);
        return dir is null ? null : Path.Combine(dir, id + suffix);
    }

    // Witness values arrive as JsonElement; the comparers downstream handle JsonElement,
    // but unwrapping the common scalars here keeps recorded witnesses readable in failure
    // messages instead of printing as raw JSON.
    private static IReadOnlyDictionary<string, object> Unwrap(Dictionary<string, JsonElement> raw)
    {
        var result = new Dictionary<string, object>(raw.Count, StringComparer.Ordinal);
        foreach (var (key, value) in raw)
        {
            result[key] = value.ValueKind switch
            {
                JsonValueKind.String => value.GetString()!,
                JsonValueKind.Number when value.TryGetInt64(out var l) => l,
                JsonValueKind.Number => value.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => value.GetRawText(),
            };
        }

        return result;
    }

    private sealed class WitnessDto
    {
        public string? BrickId { get; set; }
        public List<CaseDto>? Cases { get; set; }
    }

    private sealed class CaseDto
    {
        public Dictionary<string, JsonElement>? Input { get; set; }
        public Dictionary<string, JsonElement>? ExpectedOutput { get; set; }
    }

    private sealed class ProposalDto
    {
        public string? SourceCode { get; set; }
        public string? TypeName { get; set; }
        public string? ProposerSignature { get; set; }
    }
}
