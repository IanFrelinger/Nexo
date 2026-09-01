using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Ashlar.Core.Application.Autonomy;
using Ashlar.Core.Application.Certification.Models;

namespace Ashlar.BackgroundAgents.Autonomy;

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

    /// <summary>Witness file for an objective, or null when absent or unusable.</summary>
    public static WitnessSpec? LoadWitness(string objectiveFilePath) =>
        LoadWitness(objectiveFilePath, out _);

    /// <summary>
    /// Witness file for an objective, distinguishing ABSENT from BROKEN.
    ///
    /// <para>Both return null — a witness that cannot be read is never a witness, and the
    /// objective does not run either way. But they are opposite operator situations: absent is
    /// the normal state of an objective nobody has written criteria for yet, while broken is a
    /// file someone DID write and that silently no longer counts. Collapsing them logs "no
    /// witness beside it" about a witness that is sitting right there, and the objective then
    /// never runs again with nothing said at any visible level. <paramref name="corruption"/> is
    /// null in the absent case and carries the reason in the broken one, so the caller can put
    /// the second in front of a human.</para>
    /// </summary>
    public static WitnessSpec? LoadWitness(string objectiveFilePath, out string? corruption)
    {
        corruption = null;
        var path = SiblingPath(objectiveFilePath, ".witness.json");
        if (path is null || !File.Exists(path))
            return null;

        // A MALFORMED artifact degrades to the same "not eligible" path as an ABSENT one: an
        // objective's corrupt sibling must not throw out of the sweep and wedge every objective
        // behind it. It is reported, not swallowed.
        WitnessDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<WitnessDto>(File.ReadAllText(path), Json);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            corruption = $"{path} could not be read as a witness ({ex.GetType().Name}: {ex.Message})";
            return null;
        }

        if (dto is null || dto.Cases is null || dto.Cases.Count == 0)
        {
            corruption = $"{path} declares no witness cases; a certificate minted against it would prove nothing";
            return null;
        }

        if (string.IsNullOrWhiteSpace(dto.BrickId))
        {
            corruption = $"{path} names no brickId, so there is nothing to witness against";
            return null;
        }

        var cases = new List<WitnessCase>(dto.Cases.Count);
        for (var i = 0; i < dto.Cases.Count; i++)
        {
            var c = dto.Cases[i];
            if (c.Input is null || c.ExpectedOutput is null)
            {
                corruption = $"{path} case {i} is missing input or expectedOutput";
                return null;
            }

            try
            {
                cases.Add(new WitnessCase(Unwrap(c.Input), Unwrap(c.ExpectedOutput)));
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException)
            {
                // A case whose shape cannot be unwrapped is an ineligible objective.
                corruption = $"{path} case {i} could not be read ({ex.GetType().Name}: {ex.Message})";
                return null;
            }
        }

        return new WitnessSpec(dto.BrickId!, cases);
    }

    /// <summary>Recorded proposal for an objective, or null when absent or unusable.</summary>
    public static ProposedSource? LoadRecordedProposal(string objectiveFilePath) =>
        LoadRecordedProposal(objectiveFilePath, out _);

    /// <summary>
    /// Recorded proposal for an objective, distinguishing ABSENT from BROKEN exactly as
    /// <see cref="LoadWitness(string, out string?)"/> does and for the same reason.
    /// </summary>
    public static ProposedSource? LoadRecordedProposal(string objectiveFilePath, out string? corruption)
    {
        corruption = null;
        var path = SiblingPath(objectiveFilePath, ".proposal.json");
        if (path is null || !File.Exists(path))
            return null;

        ProposalDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<ProposalDto>(File.ReadAllText(path), Json);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            corruption = $"{path} could not be read as a proposal ({ex.GetType().Name}: {ex.Message})";
            return null;
        }

        if (dto is null || string.IsNullOrWhiteSpace(dto.SourceCode) || string.IsNullOrWhiteSpace(dto.TypeName))
        {
            corruption = $"{path} is missing sourceCode or typeName";
            return null;
        }

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
