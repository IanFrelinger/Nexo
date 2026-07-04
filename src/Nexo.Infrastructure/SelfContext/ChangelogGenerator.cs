using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Application.SelfContext.Ports;

namespace Nexo.Infrastructure.SelfContext;

/// <summary>
/// Generates changelog markdown from promoted adaptation records. Phase F.
/// </summary>
public sealed class ChangelogGenerator : IChangelogGenerator
{
    private readonly IAdaptationLog _adaptationLog;

    /// <summary>Initializes a new changelog generator.</summary>
    public ChangelogGenerator(IAdaptationLog adaptationLog)
    {
        _adaptationLog = adaptationLog ?? throw new ArgumentNullException(nameof(adaptationLog));
    }

    /// <inheritdoc />
    public async Task<string> GenerateAsync(DateTimeOffset? since = null, DateTimeOffset? until = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sinceVal = since ?? DateTimeOffset.UtcNow.AddDays(-7);
        var untilVal = until ?? DateTimeOffset.UtcNow;
        var records = await _adaptationLog.QueryAsync(sinceVal, untilVal, brickId: null, cancellationToken).ConfigureAwait(false);
        var promoted = records.Where(r => r.Promoted).OrderByDescending(r => r.Timestamp).ToList();

        if (promoted.Count == 0)
            return "# Changelog\n\nNo promoted changes in the selected period.\n";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# Changelog");
        sb.AppendLine();
        sb.AppendLine($"## {sinceVal:yyyy-MM-dd} – {untilVal:yyyy-MM-dd}");
        sb.AppendLine();
        foreach (var r in promoted)
        {
            sb.AppendLine($"- **{r.BrickId ?? "unknown"}**: {r.Message ?? r.FailureType} ({r.Timestamp:yyyy-MM-dd HH:mm})");
        }
        sb.AppendLine();
        return sb.ToString();
    }
}
