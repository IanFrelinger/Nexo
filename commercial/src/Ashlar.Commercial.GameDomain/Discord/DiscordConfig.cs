namespace Ashlar.Commercial.GameDomain.Discord;
/// <summary>
/// Discord integration configuration. Loaded from .ashlar/discord.json
/// or environment variables (ASHLAR_DISCORD_*).
/// </summary>
public sealed record DiscordConfig
{
    /// <summary>Constant value for section name.</summary>
    public const string SectionName = "Ashlar:Discord";

    /// <summary>Webhook URL for posting reports (no bot token needed for one-way).</summary>
    public string? WebhookUrl { get; init; }

    /// <summary>Bot token for bidirectional interaction (reactions, commands).</summary>
    public string? BotToken { get; init; }

    /// <summary>Channel ID where the bot listens for reactions.</summary>
    public string? ChannelId { get; init; }

    /// <summary>Your Discord user ID for mentions.</summary>
    public string? OwnerUserId { get; init; }

    /// <summary>Max file size in bytes for uploads (Discord limit: 25MB for webhooks).</summary>
    public long MaxUploadBytes { get; init; } = 25 * 1024 * 1024;

    /// <summary>Whether to clip highlights from full recordings before uploading.</summary>
    public bool ClipHighlights { get; init; } = true;

    /// <summary>Max clip duration in seconds.</summary>
    public double MaxClipDurationSeconds { get; init; } = 15;

    /// <summary>Load operation.</summary>
    public static DiscordConfig Load(string projectRoot)
    {
        var path = System.IO.Path.Combine(projectRoot, ".ashlar", "discord.json");
        if (!System.IO.File.Exists(path))
            return new DiscordConfig
            {
                WebhookUrl = Environment.GetEnvironmentVariable("ASHLAR_DISCORD_WEBHOOK_URL"),
                BotToken = Environment.GetEnvironmentVariable("ASHLAR_DISCORD_BOT_TOKEN"),
                ChannelId = Environment.GetEnvironmentVariable("ASHLAR_DISCORD_CHANNEL_ID"),
                OwnerUserId = Environment.GetEnvironmentVariable("ASHLAR_DISCORD_OWNER_ID")
            };
        var json = System.IO.File.ReadAllText(path);
        return System.Text.Json.JsonSerializer.Deserialize<DiscordConfig>(json,
            new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase })
            ?? new DiscordConfig();
    }
}
