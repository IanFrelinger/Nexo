using Ashlar.Core.Domain.Bricks.Ports;

namespace Ashlar.Bricks.SqlProfile;

/// <summary>
/// Model/template path for SQL: wraps grounding into a SELECT, and applies
/// simple repairs from prior validator feedback (e.g. empty → SELECT 1).
/// </summary>
public sealed class SqlArtifactDrafter : IArtifactDrafter
{
    /// <inheritdoc />
    public Task<GeneratedArtifact> DraftAsync(
        GenerationRequest request,
        IReadOnlyList<string> priorFeedback,
        CancellationToken cancellationToken = default)
    {
        var grounding = request.Grounding?.Trim() ?? "";
        string sql;

        if (priorFeedback.Count > 0 && priorFeedback.Any(f => f.Contains("empty", StringComparison.OrdinalIgnoreCase)))
        {
            sql = "SELECT 1;";
        }
        else if (string.IsNullOrWhiteSpace(grounding))
        {
            sql = ""; // force validator failure → repair
        }
        else if (grounding.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)
                 || grounding.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase)
                 || grounding.StartsWith("EXPLAIN", StringComparison.OrdinalIgnoreCase))
        {
            sql = grounding.EndsWith(';') ? grounding : grounding + ";";
        }
        else
        {
            // Repair unbalanced parens feedback by stripping trailing junk.
            var body = grounding;
            if (priorFeedback.Any(f => f.Contains("parenthes", StringComparison.OrdinalIgnoreCase)))
                body = body.Replace("(", "").Replace(")", "");
            sql = $"SELECT * FROM {body};";
        }

        return Task.FromResult(new GeneratedArtifact { Content = sql });
    }
}
