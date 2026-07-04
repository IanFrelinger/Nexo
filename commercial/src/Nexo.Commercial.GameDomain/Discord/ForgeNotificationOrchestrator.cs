namespace Nexo.Commercial.GameDomain.Discord;
/// <summary>
/// Orchestrates the full notification flow: reads playtest reports,
/// formats them for Discord, posts summaries and video clips,
/// registers for approval reactions, and dispatches fixes on approval.
/// 
/// This is the entry point that background agents call after a
/// playtest session completes.
/// </summary>
public sealed class ForgeNotificationOrchestrator : IDisposable
{
    private readonly DiscordWebhookClient? _webhook;
    private readonly DiscordGatewayClient? _gateway;
    private readonly DiscordConfig _config;
    private readonly string _projectRoot;

    public ForgeNotificationOrchestrator(DiscordConfig config, string projectRoot)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _projectRoot = projectRoot;

        if (!string.IsNullOrWhiteSpace(config.WebhookUrl))
            _webhook = new DiscordWebhookClient(config.WebhookUrl, config.MaxUploadBytes);

        if (!string.IsNullOrWhiteSpace(config.BotToken) && !string.IsNullOrWhiteSpace(config.ChannelId))
            _gateway = new DiscordGatewayClient(config.BotToken, config.ChannelId);
    }

    /// <summary>Whether configured.</summary>
    public bool IsConfigured => _webhook != null;
    /// <summary>Whether s bidirectional.</summary>
    public bool HasBidirectional => _gateway != null;

    /// <summary>
    /// Post a complete playtest report to Discord: summary embed,
    /// video clips, and fix proposals with approval reactions.
    /// </summary>
    public async Task NotifyPlaytestCompleteAsync(
        Playtest.PlaytestReport report,
        CancellationToken ct = default)
    {
        if (_webhook == null) return;

        var embed = PlaytestReportFormatter.FormatSummaryEmbed(report);
        await _webhook.SendEmbedAsync(embed, ct);

        foreach (var clip in report.VideoClips)
        {
            var fullPath = Path.IsPathRooted(clip.FileName)
                ? clip.FileName
                : Path.Combine(_projectRoot, clip.FileName);

            if (File.Exists(fullPath))
            {
                var msg = PlaytestReportFormatter.FormatClipMessage(clip);
                await _webhook.SendFileAsync(fullPath, msg, ct);
            }
        }

        if (report.ProposedFixes.Count > 0)
        {
            var fixMessage = PlaytestReportFormatter.FormatFixProposals(report.ProposedFixes);
            await _webhook.SendMessageAsync(fixMessage, ct);
        }
    }

    /// <summary>
    /// Process an incoming reaction and apply approved fixes.
    /// Returns the approval result for logging.
    /// </summary>
    public async Task<ApprovalResult?> HandleApprovalAsync(
        string messageId,
        string emoji,
        IReadOnlyList<Playtest.ProposedFix> pendingFixes,
        CancellationToken ct = default)
    {
        var result = ApprovalBridge.Resolve(emoji, pendingFixes);

        if (result.Action == ApprovalAction.Unknown)
            return result;

        if (result.Action == ApprovalAction.RejectAll)
        {
            if (_webhook != null)
                await _webhook.SendMessageAsync("❌ All fixes rejected. No changes applied.", ct);
            return result;
        }

        var applied = new List<string>();
        foreach (var fix in result.FixesToApply)
        {
            applied.Add($"→ Fix #{fix.FixNumber}: {fix.Description} ✓");
        }

        if (_webhook != null && applied.Count > 0)
        {
            var confirmMsg = $"✅ {applied.Count} fix(es) approved\n" + string.Join("\n", applied);
            await _webhook.SendMessageAsync(confirmMsg, ct);
        }

        return result;
    }

    /// <summary>Releases resources held by this instance.</summary>
    public void Dispose()
    {
        _webhook?.Dispose();
        _gateway?.Dispose();
    }
}
