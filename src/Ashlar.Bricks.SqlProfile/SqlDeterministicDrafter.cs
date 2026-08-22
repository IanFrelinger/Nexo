using System.Text.RegularExpressions;
using Ashlar.Core.Domain.Bricks.Ports;

namespace Ashlar.Bricks.SqlProfile;

/// <summary>
/// Deterministic drafter: when grounding is a simple identifier, emit
/// <c>SELECT * FROM {id}</c>.
/// </summary>
public sealed class SqlDeterministicDrafter : IDeterministicDrafter
{
    private static readonly Regex Ident = new(
        @"^[A-Za-z_][A-Za-z0-9_]*$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    /// <inheritdoc />
    public bool TryDraft(GenerationRequest request, out GeneratedArtifact? artifact)
    {
        artifact = null;
        var grounding = request.Grounding?.Trim() ?? "";
        if (!Ident.IsMatch(grounding))
            return false;

        artifact = new GeneratedArtifact
        {
            Content = $"SELECT * FROM {grounding};"
        };
        return true;
    }
}
