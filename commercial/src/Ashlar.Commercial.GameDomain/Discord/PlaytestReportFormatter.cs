namespace Ashlar.Commercial.GameDomain.Discord;
using Ashlar.Commercial.GameDomain.Playtest;

/// <summary>
/// Formats <see cref="PlaytestReport"/> data into Discord-ready embeds and messages.
/// </summary>
public static class PlaytestReportFormatter
{
    /// <summary>Constant value for green.</summary>
    public const int Green = 0x22C55E;
    /// <summary>Constant value for yellow.</summary>
    public const int Yellow = 0xF59E0B;
    /// <summary>Constant value for red.</summary>
    public const int Red = 0xEF4444;
    /// <summary>Constant value for blue.</summary>
    public const int Blue = 0x3B82F6;

    /// <summary>
    /// Builds a summary embed for the playtest report. Color reflects overall severity:
    /// green (no issues), yellow (warnings only), red (any critical).
    /// </summary>
    public static DiscordEmbed FormatSummaryEmbed(PlaytestReport report)
    {
        var hasCritical = report.BalanceFindings.Any(f => f.Severity == "critical")
                       || report.MapFindings.Any(f => f.Severity == "critical");
        var hasWarning = report.BalanceFindings.Any(f => f.Severity == "warning")
                      || report.MapFindings.Any(f => f.Severity == "warning");

        var color = hasCritical ? Red : hasWarning ? Yellow : Green;

        var fields = new List<DiscordEmbedField>
        {
            new()
            {
                Name = "Duration",
                Value = $"{report.DurationSeconds:F0}s",
                Inline = true
            },
            new()
            {
                Name = "Issues",
                Value = report.TotalIssues.ToString(),
                Inline = true
            },
            new()
            {
                Name = "Video Clips",
                Value = report.VideoClips.Count.ToString(),
                Inline = true
            }
        };

        if (report.BalanceFindings.Count > 0)
        {
            var balanceSummary = string.Join("\n", report.BalanceFindings
                .Where(f => f.Severity != "ok")
                .Select(f => $"• **{f.ItemName}** ({f.Severity}): {f.Summary}"));
            if (!string.IsNullOrEmpty(balanceSummary))
                fields.Add(new DiscordEmbedField { Name = "Balance", Value = balanceSummary });
        }

        if (report.MapFindings.Count > 0)
        {
            var mapSummary = string.Join("\n", report.MapFindings
                .Where(f => f.Severity != "ok")
                .Select(f => $"• **{f.AreaName}** ({f.FindingType}): {f.Summary}"));
            if (!string.IsNullOrEmpty(mapSummary))
                fields.Add(new DiscordEmbedField { Name = "Map", Value = mapSummary });
        }

        var perf = report.Performance;
        fields.Add(new DiscordEmbedField
        {
            Name = "Performance",
            Value = $"Avg FPS: {perf.AverageFps:F1} | P99: {perf.P99Fps:F1} | Draw Calls: {perf.DrawCalls} | Mem: {perf.MemoryMb:F0} MB | Particles: {perf.ActiveParticles}"
        });

        return new DiscordEmbed
        {
            Title = $"Playtest Report: {report.ScenarioName}",
            Description = $"Session `{report.SessionId}` completed at {report.CompletedAt:u}",
            Color = color,
            Fields = fields,
            Footer = new DiscordEmbedFooter { Text = "Ashlar Forge — Automated Playtest" },
            Timestamp = report.CompletedAt.ToString("o")
        };
    }

    /// <summary>
    /// Formats proposed fixes as a numbered list with reaction instructions.
    /// </summary>
    public static string FormatFixProposals(IReadOnlyList<ProposedFix> fixes)
    {
        if (fixes.Count == 0)
            return "No fixes proposed — everything looks good! ✅";

        var lines = new List<string>
        {
            "**Proposed Fixes** — React with the number to auto-apply:\n"
        };

        foreach (var fix in fixes)
        {
            var emoji = fix.Severity == "critical" ? "🔴" : "🟡";
            lines.Add($"{fix.FixNumber}️⃣ {emoji} **{fix.TargetSystem}** — {fix.Description}");
            lines.Add($"   `{fix.IterateCommand}`\n");
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Returns a description string for a video clip upload.
    /// </summary>
    public static string FormatClipMessage(VideoClip clip)
    {
        var duration = clip.EndSeconds - clip.StartSeconds;
        var sizeMb = clip.FileSizeBytes / (1024.0 * 1024.0);
        return $"🎬 **{clip.Description}** ({duration:F1}s, {sizeMb:F1} MB)\n`{clip.FileName}`";
    }
}
